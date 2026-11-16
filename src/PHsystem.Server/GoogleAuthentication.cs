using PHsystem.SharedContracts.V1;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
namespace PHsystem.Server;
public static class GoogleAuthentication
{
    public static bool Configured(IConfiguration c)=>!string.IsNullOrWhiteSpace(c["Authentication:Google:ClientId"])&&!string.IsNullOrWhiteSpace(c["Authentication:Google:ClientSecret"]);
    public static bool NativeEnabled(IConfiguration c)=>Configured(c)&&c.GetValue("Authentication:Google:EnableNativeBroker",false);
    public static void AddGoogleAuthentication(this IServiceCollection services,IConfiguration config)
    {
        services.AddSingleton(TimeProvider.System);services.AddSingleton<NativeLoginBroker>();
        if(!Configured(config))return;
        services.AddAuthentication().AddGoogle(options=>
        {
            options.ClientId=config["Authentication:Google:ClientId"]!;options.ClientSecret=config["Authentication:Google:ClientSecret"]!;
            options.SignInScheme=IdentityConstants.ExternalScheme;options.SaveTokens=false;options.UsePkce=true;
            options.UserInformationEndpoint="https://openidconnect.googleapis.com/v1/userinfo";
            options.Scope.Clear();options.Scope.Add("openid");options.Scope.Add("email");options.ClaimActions.Clear();options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier,"sub");options.ClaimActions.MapJsonKey(ClaimTypes.Email,"email");
            options.Events.OnCreatingTicket=context=>
            {
                var verified=context.User.TryGetProperty("email_verified",out var value)&&value.ValueKind==System.Text.Json.JsonValueKind.True;
                context.Identity!.AddClaim(new Claim("ph:email_verified",verified?"true":"false"));return Task.CompletedTask;
            };
            options.Events.OnRedirectToAuthorizationEndpoint=context=> { context.Response.Redirect(QueryHelpers.AddQueryString(context.RedirectUri,"prompt","select_account"));return Task.CompletedTask; };
            options.Events.OnRemoteFailure=context=> { context.HandleResponse();context.Response.Redirect("/entrar?external=failed");return Task.CompletedTask; };
        });
    }
    public static void MapGoogleAuthentication(this WebApplication app)
    {
        app.MapGet("/api/v1/auth/providers",(IConfiguration c)=>Results.Ok(new{google=Configured(c),nativeGoogle=NativeEnabled(c)}));
        app.MapGet("/api/v1/auth/google",(HttpContext context,SignInManager<CommunityUser> signIn,IConfiguration c,NativeLoginBroker broker)=>
        {
            if(!Configured(c)||!context.Request.IsHttps)return Results.Problem("Google não configurado neste ambiente.",statusCode:503);
            if(context.Request.Query["acceptTerms"]!="true")return Results.BadRequest(new{message="Confirme o aceite dos termos antes de continuar."});
            var props=signIn.ConfigureExternalAuthenticationProperties("Google","/api/v1/auth/google/callback");
            if(context.Request.Query.TryGetValue("flow",out var flow))
            { if(!NativeEnabled(c)||!Guid.TryParse(flow,out var id)||!broker.Exists(id))return Results.BadRequest();props.Items["nativeFlow"]=id.ToString("D"); }
            return Results.Challenge(props,["Google"]);
        });
        app.MapPost("/api/v1/auth/native/start",(NativeStartRequest request,IConfiguration c,NativeLoginBroker broker)=>
        {
            if(!NativeEnabled(c))return Results.Problem("Google para desktop não configurado.",statusCode:503);
            try{return Results.Ok(broker.Start(request.Challenge));}catch(ArgumentException){return Results.BadRequest();}catch(InvalidOperationException){return Results.StatusCode(429);}
        });
        app.MapPost("/api/v1/auth/native/redeem",async(NativeRedeemRequest request,IConfiguration c,NativeLoginBroker broker,UserManager<CommunityUser> users,SignInManager<CommunityUser> signIn)=>
        {
            if(!NativeEnabled(c))return Results.StatusCode(503);
            string? userId;try{userId=broker.Consume(request);}catch(InvalidOperationException){return Results.BadRequest();}
            if(userId is null)return Results.Ok(new{status="pending"});
            var user=await users.FindByIdAsync(userId);
            if(user is null||!user.EmailConfirmed||await users.IsLockedOutAsync(user)||user.TwoFactorEnabled||await IsStaff(users,user))return Results.Unauthorized();
            await SignIn(signIn,user,request.RememberSession);return Results.Ok(new{status="authenticated"});
        });
        app.MapGet("/api/v1/auth/google/callback",async(HttpContext context,UserManager<CommunityUser> users,SignInManager<CommunityUser> signIn,CommunityDb db,NativeLoginBroker broker)=>
        {
            try
            {
                var ticket=await context.AuthenticateAsync(IdentityConstants.ExternalScheme);var info=await signIn.GetExternalLoginInfoAsync();
                if(info is null||info.LoginProvider!="Google"||info.ProviderKey.Length is < 1 or > 255||!info.Principal.HasClaim("ph:email_verified","true"))return Results.Redirect("/entrar?external=unverified");
                var user=await users.FindByLoginAsync("Google",info.ProviderKey);
                if(user is null)
                {
                    var email=info.Principal.FindFirstValue(ClaimTypes.Email);
                    if(email is null||!System.Net.Mail.MailAddress.TryCreate(email,out _)||email.Length>254)return Results.Unauthorized();
                    // Email equality is not authority to link an existing account.
                    if(await users.FindByEmailAsync(email) is not null)return Results.Redirect("/entrar?external=account-exists");
                    var name="G_"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(info.ProviderKey)))[..18];
                    user=new CommunityUser {UserName=name,Email=email,EmailConfirmed=true};
                    await using var transaction=await db.Database.BeginTransactionAsync(context.RequestAborted);
                    if(!(await users.CreateAsync(user)).Succeeded)return Results.Conflict();
                    if(!(await users.AddLoginAsync(user,info)).Succeeded)return Results.Conflict();
                    db.Audit.Add(new(){ActorId=user.Id,TargetId=user.Id,Action="google-provisioned;terms-accepted:2026-11-15"});await db.SaveChangesAsync(context.RequestAborted);await transaction.CommitAsync(context.RequestAborted);
                }
                if(!user.EmailConfirmed||await users.IsLockedOutAsync(user)||user.TwoFactorEnabled||await IsStaff(users,user))return Results.Redirect("/entrar?external=blocked");
                if(ticket.Properties?.Items.TryGetValue("nativeFlow",out var native)==true)
                {
                    if(!Guid.TryParse(native,out var id)||!broker.Authorize(id,user.Id))return Results.BadRequest();
                    return Results.Redirect("/entrar?external=desktop-complete");
                }
                await SignIn(signIn,user,false);return Results.Redirect("/comunidade");
            }
            finally {await context.SignOutAsync(IdentityConstants.ExternalScheme);}
        });
    }
    private static async Task<bool> IsStaff(UserManager<CommunityUser> users,CommunityUser user)=>(await users.GetRolesAsync(user)).Any(r=>r is "Admin" or "Moderator" or "TrustedReviewer");
    private static Task SignIn(SignInManager<CommunityUser> signIn,CommunityUser user,bool persistent)=>signIn.SignInWithClaimsAsync(user,persistent,
        new[]{new Claim("amr","google"),new Claim("auth_time",DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))});
}
