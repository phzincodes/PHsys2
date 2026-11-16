using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PHsystem.Server;
using System.Data.Common;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"])) builder.WebHost.UseUrls("http://127.0.0.1:5080");
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.Items["PHsystem.CorrelationId"]);
builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.Cookie.SameSite = SameSiteMode.Strict; });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.OnRejected = (context, _) =>
    {
        var retry = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value) ? Math.Max(1, (int)Math.Ceiling(value.TotalSeconds)) : 60;
        context.HttpContext.Response.Headers.RetryAfter = retry.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return ValueTask.CompletedTask;
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ =>
            new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        (context.Connection.RemoteIpAddress?.ToString() ?? "local") + ":" + context.Request.Path.Value?.ToLowerInvariant(),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var connection = builder.Configuration.GetConnectionString("Community");
var configured = !string.IsNullOrWhiteSpace(connection);
builder.Services.AddDbContext<CommunityDb>(options => options.UseNpgsql(connection ?? "Host=127.0.0.1;Database=unconfigured"));
builder.Services.AddIdentity<CommunityUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<CommunityDb>().AddDefaultTokenProviders()
    .AddPasswordValidator<CompromisedPasswordValidator>();
builder.Services.Configure<PasswordHasherOptions>(options => options.IterationCount = 600000);
builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromMinutes(30));
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
    options.OnRefreshingPrincipal = context =>
    {
        if (context.CurrentPrincipal is { } current && context.NewPrincipal?.Identity is ClaimsIdentity identity)
            foreach (var claim in current.Claims.Where(c => c.Type is "amr" or "auth_time")) identity.AddClaim(claim);
        return Task.CompletedTask;
    };
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-PHsystem";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(4);
    options.SlidingExpiration = false;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Staff", policy => policy.RequireAuthenticatedUser().RequireRole("Moderator", "Admin").RequireClaim("amr", "mfa"));
    options.AddPolicy("Review", policy => policy.RequireAuthenticatedUser().RequireRole("TrustedReviewer").RequireClaim("amr", "mfa"));
    options.AddPolicy("Admin", policy => policy.RequireAuthenticatedUser().RequireRole("Admin").RequireClaim("amr", "mfa")
        .RequireAssertion(context => long.TryParse(context.User.FindFirstValue("auth_time"), out var issued)
            && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - issued is >= 0 and <= 600));
});
builder.Services.AddScoped<IAccountEmail, SmtpAccountEmail>();
builder.Services.AddSignalR();
builder.Services.AddGoogleAuthentication(builder.Configuration);
var app = builder.Build();
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    if (!configured) throw new InvalidOperationException("PostgreSQL não configurado; nenhuma migração executada.");
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<CommunityDb>().Database.MigrateAsync();
    return;
}
app.UseApiErrors();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (DbUpdateConcurrencyException) when (!context.Response.HasStarted) { context.Response.Clear(); await Results.Problem("Conteúdo alterado. Atualize antes de reenviar.",statusCode:409).ExecuteAsync(context); }
    catch (Exception ex) when (!context.Response.HasStarted && context.Request.Path.StartsWithSegments("/api")
        && ex is DbException or DbUpdateException or TimeoutException or System.Net.Mail.SmtpException)
    {
        context.Response.Clear();
        await Results.Problem("Serviço temporariamente indisponível.", statusCode: 503).ExecuteAsync(context);
    }
});
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    if (context.Request.Path.StartsWithSegments("/api") && !configured && context.Request.Path.Value is not "/api/v1/releases/current" and not "/api/v1/auth/providers")
    {
        await Results.Problem("Comunidade não configurada. Defina PostgreSQL, migrações, email e triagem de senhas.", statusCode: 503).ExecuteAsync(context);
        return;
    }
    await next(context);
});
app.UseRateLimiter();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if(context.Request.Path.StartsWithSegments("/api")&&context.Request.Headers.TryGetValue("X-PHsystem-Actor",out var expectedActor))
    {
        if(expectedActor.Count!=1||string.IsNullOrWhiteSpace(expectedActor[0])||expectedActor[0]!.Length>128){context.Response.StatusCode=400;return;}
        if(context.User.Identity?.IsAuthenticated!=true||!string.Equals(expectedActor[0],context.User.FindFirstValue(ClaimTypes.NameIdentifier),StringComparison.Ordinal)){context.Response.StatusCode=401;return;}
    }
    if (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method))
    {
        if (!context.Request.IsHttps) { context.Response.StatusCode = 426; return; }
        try { await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { context.Response.StatusCode = 400; return; }
    }
    await next(context);
});
app.UseAuthorization();
app.MapPublicWeb();
app.MapReleaseDownloads();
app.MapServiceHealth();
app.MapGet("/api/v1/security/csrf", (HttpContext context, IAntiforgery antiforgery) => Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }));
app.MapIdentityEndpoints();
app.MapGoogleAuthentication();
app.MapCommunityEndpoints();
app.MapSocialEndpoints();
app.MapCommunityExtensions();
app.MapPublicProfiles();
app.MapAdministration();
app.MapHub<NotificationHub>("/api/v1/notifications").RequireAuthorization();
app.Run();
public partial class Program { }
