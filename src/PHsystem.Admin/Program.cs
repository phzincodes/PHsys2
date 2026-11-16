using System.Net;
using System.Text.RegularExpressions;
var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"])) builder.WebHost.UseUrls("http://127.0.0.1:5081");
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);
builder.Services.AddHttpClient("backend").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false });
var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self'; script-src 'self'; frame-ancestors 'none'; base-uri 'none'";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next(context);
});
app.UseExceptionHandler(error=>error.Run(async context=>{context.Response.StatusCode=503;await context.Response.WriteAsync("Não foi possível conectar à API por HTTPS. Revise o serviço e o certificado.");}));
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/health", () => new { status = "admin-shell", configured = !string.IsNullOrWhiteSpace(app.Configuration["Backend:BaseUrl"]) });
app.MapMethods("/gateway/{**path}", ["GET", "POST"], async (string path, HttpContext context, IHttpClientFactory factory) =>
{
    string[] allowed = ["api/v1/security/csrf", "api/v1/account/me", "api/v1/account/login", "api/v1/account/logout", "api/v1/admin/audit", "api/v1/admin/stats", "api/v1/admin/users", "api/v1/staff/reports", "api/v1/staff/queue"];
    var critical=Regex.IsMatch(path,@"^api/v1/admin/users/[A-Za-z0-9_-]{1,128}/(role|suspension)$");
    var moderation=Regex.IsMatch(path,@"^api/v1/staff/(reports/[0-9a-fA-F-]{36}/resolve|posts/[0-9a-fA-F-]{36}/(hide|publish)|replies/[0-9a-fA-F-]{36}/moderate)$");
    if (!allowed.Contains(path, StringComparer.Ordinal)&&!critical&&!moderation) { context.Response.StatusCode = 404; return; }
    if (!Uri.TryCreate(app.Configuration["Backend:BaseUrl"], UriKind.Absolute, out var backend) || backend.Scheme != "https"
        || !string.IsNullOrEmpty(backend.UserInfo) || backend.AbsolutePath != "/")
    { context.Response.StatusCode = 503; await context.Response.WriteAsync("Backend HTTPS não configurado."); return; }
    if (!context.Request.IsHttps) { context.Response.StatusCode = 426; return; }
    using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), new Uri(backend, path+context.Request.QueryString.Value));
    if (context.Request.Headers.TryGetValue("Cookie", out var cookies)) request.Headers.TryAddWithoutValidation("Cookie", cookies.ToArray());
    if(context.Request.Headers.TryGetValue("X-PHsystem-Actor",out var expectedActor))request.Headers.TryAddWithoutValidation("X-PHsystem-Actor",expectedActor.ToArray());
    if (context.Request.Headers.TryGetValue("X-CSRF-TOKEN", out var csrf)) request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", csrf.ToArray());
    if (HttpMethods.IsPost(context.Request.Method))
    {
        request.Content = new StreamContent(context.Request.Body);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
    }
    using var response = await factory.CreateClient("backend").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
    context.Response.StatusCode = (int)response.StatusCode;
    context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
    if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        foreach (var cookie in setCookies) context.Response.Headers.Append("Set-Cookie", cookie);
    await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
});
app.Run();
