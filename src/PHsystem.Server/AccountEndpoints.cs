using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PHsystem.Community;
using PHsystem.SharedContracts.V1;

namespace PHsystem.Server;

public static class AccountEndpoints
{
    public static void MapIdentityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/account").RequireRateLimiting("auth");
        group.MapPost("/register", async (RegisterRequest request, UserManager<CommunityUser> users, IAccountEmail email, IConfiguration configuration, CommunityDb db, CancellationToken token) =>
        {
            if (!request.AcceptTerms) return Results.BadRequest(new { error = "Aceite os termos e a política de privacidade." });
            if (!email.Configured) return Results.Problem("Email não configurado; registro indisponível.", statusCode: 503);
            if (!CompromisedPasswordValidator.IsConfigured(configuration)) return Results.Problem("Triagem de senhas comprometidas não configurada.", statusCode: 503);
            if (!CommunityPolicy.ValidUsername(request.Username) || string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254
                || !System.Net.Mail.MailAddress.TryCreate(request.Email, out _) || request.Password is null || request.Password.Length > 128)
                return Results.BadRequest(new { error = "Revise os campos. Nome: 3–24 caracteres ASCII; senha: 8–128 caracteres com maiúscula, minúscula, número e símbolo." });
            var user = new CommunityUser { UserName = request.Username, Email = request.Email, AcceptDirectMessages = false };
            IdentityResult result;
            try { result = await users.CreateAsync(user, request.Password); }
            catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
            { return Results.Accepted(value: new { message = "Se os dados forem elegíveis, você receberá instruções por email." }); }
            if (result.Succeeded)
            {
                db.Audit.Add(new() { ActorId = user.Id, Action = "terms-privacy-accepted:2026-11-15", TargetId = user.Id });
                await db.SaveChangesAsync(token);
                var confirmation = await users.GenerateEmailConfirmationTokenAsync(user);
                await email.SendAsync(user.Email!, "PHsystem: confirmação de email", AccountEmailContent.Create(user.Id, confirmation, false, configuration), token);
            }
            // Duplicate account and password rejection use the same public response to limit enumeration.
            return Results.Accepted(value: new { message = "Se os dados forem elegíveis, você receberá instruções por email." });
        });
        group.MapPost("/resend-confirmation", async (ForgotPasswordRequest request, UserManager<CommunityUser> users, IAccountEmail email, IConfiguration configuration, CancellationToken token) =>
        {
            if (!email.Configured) return Results.Problem("Serviço temporariamente indisponível.", statusCode: 503);
            if (request.Email is { Length: <= 254 } && System.Net.Mail.MailAddress.TryCreate(request.Email, out _))
            {
                var user = await users.FindByEmailAsync(request.Email);
                if (user is { EmailConfirmed: false })
                    await email.SendAsync(user.Email!, "PHsystem: confirmação de email", AccountEmailContent.Create(user.Id, await users.GenerateEmailConfirmationTokenAsync(user), false, configuration), token);
            }
            return Results.Accepted(value: new { message = "Se a conta for elegível, as instruções serão enviadas." });
        });
        group.MapPost("/confirm-email", async (ConfirmEmailRequest request, UserManager<CommunityUser> users) =>
        {
            if (request.UserId is null || request.Token is null || request.Token.Length > 2048) return Results.BadRequest();
            var user = await users.FindByIdAsync(request.UserId);
            if (user is null || user.EmailConfirmed) return Results.BadRequest(new { error = "Token inválido ou expirado." });
            return (await users.ConfirmEmailAsync(user, request.Token)).Succeeded ? Results.NoContent() : Results.BadRequest(new { error = "Token inválido ou expirado." });
        });
        group.MapPost("/login", async (LoginRequest request, UserManager<CommunityUser> users, SignInManager<CommunityUser> signIn, CommunityDb db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length > 24 || request.Password is null || request.Password.Length > 128)
                return Results.Unauthorized();
            var user = await users.FindByNameAsync(request.Username);
            if (user is null || !user.EmailConfirmed) return Results.Unauthorized();
            if (await users.IsLockedOutAsync(user)) return Results.Unauthorized();
            if (!await users.CheckPasswordAsync(user, request.Password))
            {
                await users.AccessFailedAsync(user);
                return Results.Unauthorized();
            }
            var staff = (await users.GetRolesAsync(user)).Any(role => role is "Admin" or "Moderator" or "TrustedReviewer");
            if (staff && !user.TwoFactorEnabled) return Results.Unauthorized();
            if (user.TwoFactorEnabled)
            {
                if (request.TwoFactorCode is null || request.TwoFactorCode.Length != 6 || !request.TwoFactorCode.All(char.IsAsciiDigit)
                    || !await users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, request.TwoFactorCode))
                {
                    await users.AccessFailedAsync(user);
                    return Results.Unauthorized();
                }
                var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(user.Id + ":" + request.TwoFactorCode)));
                await db.MfaUses.Where(m => m.UsedAt < DateTime.UtcNow.AddMinutes(-15)).ExecuteDeleteAsync();
                if (await db.MfaUses.AnyAsync(m => m.UserId == user.Id && m.Digest == digest)) return Results.Unauthorized();
                db.MfaUses.Add(new() { UserId = user.Id, Digest = digest });
                try { await db.SaveChangesAsync(); }
                catch (DbUpdateException) { return Results.Unauthorized(); }
            }
            await users.ResetAccessFailedCountAsync(user);
            var claims = new List<Claim> { new("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)) };
            if (user.TwoFactorEnabled) claims.Add(new("amr", "mfa"));
            await signIn.SignInWithClaimsAsync(user, isPersistent: request.RememberSession, claims);
            return Results.NoContent();
        });
        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, UserManager<CommunityUser> users, IAccountEmail email, IConfiguration configuration, CancellationToken token) =>
        {
            if (!email.Configured) return Results.Problem("Email não configurado.", statusCode: 503);
            if (request.Email is { Length: <= 254 })
            {
                var user = await users.FindByEmailAsync(request.Email);
                if (user is { EmailConfirmed: true })
                    await email.SendAsync(user.Email!, "PHsystem: redefinição de senha", AccountEmailContent.Create(user.Id, await users.GeneratePasswordResetTokenAsync(user), true, configuration), token);
            }
            return Results.Accepted(value: new { message = "Se a conta for elegível, as instruções serão enviadas." });
        });
        group.MapPost("/reset-password", async (ResetPasswordRequest request, UserManager<CommunityUser> users, IConfiguration configuration) =>
        {
            if (!CompromisedPasswordValidator.IsConfigured(configuration)) return Results.Problem("Triagem de senhas comprometidas não configurada.", statusCode: 503);
            if (request.UserId is null || request.Token is null || request.Token.Length > 2048 || request.NewPassword is null || request.NewPassword.Length > 128) return Results.BadRequest();
            var user = await users.FindByIdAsync(request.UserId);
            if (user is null) return Results.BadRequest(new { error = "Token inválido, expirado ou senha não aceita." });
            var result = await users.ResetPasswordAsync(user, request.Token, request.NewPassword);
            return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { error = "Token inválido, expirado ou senha não aceita." });
        });
        group.MapPost("/refresh-session", async (HttpContext context) =>
        {
            var current = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            if (!current.Succeeded || current.Principal is null || current.Properties is null
                || !long.TryParse(current.Principal.FindFirstValue("auth_time"), out var timestamp)) return Results.Unauthorized();
            var now = DateTimeOffset.UtcNow;
            var absoluteExpiration = DateTimeOffset.FromUnixTimeSeconds(timestamp).AddHours(12);
            if (absoluteExpiration <= now) return Results.Unauthorized();
            current.Properties.IssuedUtc = now;
            current.Properties.ExpiresUtc = now.AddHours(4) < absoluteExpiration ? now.AddHours(4) : absoluteExpiration;
            // Preserve the original auth_time: renewal must not grant fresh administrative reauthentication.
            await context.SignInAsync(IdentityConstants.ApplicationScheme, current.Principal, current.Properties);
            return Results.NoContent();
        }).RequireAuthorization();
        group.MapPost("/logout", async (SignInManager<CommunityUser> signIn) => { await signIn.SignOutAsync(); return Results.NoContent(); }).RequireAuthorization();
        group.MapPost("/revoke-sessions", async (ClaimsPrincipal principal, UserManager<CommunityUser> users, SignInManager<CommunityUser> signIn) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            await users.UpdateSecurityStampAsync(user);
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();
        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<CommunityUser> users, HttpContext context) =>
        {
            var user = await users.GetUserAsync(principal);
            var ticket = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            return user is null ? Results.Unauthorized() : Results.Ok(new { user.Id, user.UserName, user.Email, user.TwoFactorEnabled, user.AcceptDirectMessages, hasPassword=await users.HasPasswordAsync(user), expiresAt = ticket.Properties?.ExpiresUtc });
        }).RequireAuthorization();
        group.MapPost("/mfa/setup", async (PasswordConfirmation request, ClaimsPrincipal principal, UserManager<CommunityUser> users, SignInManager<CommunityUser> signIn) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null || user.TwoFactorEnabled || request.Password is null || request.Password.Length > 128
                || !await users.CheckPasswordAsync(user, request.Password)) return Results.BadRequest();
            await users.ResetAuthenticatorKeyAsync(user);
            var key = await users.GetAuthenticatorKeyAsync(user);
            await signIn.RefreshSignInAsync(user);
            return Results.Ok(new { secret = key, issuer = "PHsystem Next", account = user.UserName });
        }).RequireAuthorization();
        group.MapPost("/mfa/enable", async (MfaCodeRequest request, ClaimsPrincipal principal, UserManager<CommunityUser> users, SignInManager<CommunityUser> signIn) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null || user.TwoFactorEnabled || request.Code is null || request.Code.Length != 6
                || !await users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, request.Code)) return Results.BadRequest();
            var enabled = await users.SetTwoFactorEnabledAsync(user, true);
            if (!enabled.Succeeded) return Results.Conflict();
            var recoveryCodes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            await users.UpdateSecurityStampAsync(user);
            await signIn.SignOutAsync();
            return Results.Ok(new { recoveryCodes, message = "Guarde os códigos em local seguro. Entre novamente com senha e TOTP." });
        }).RequireAuthorization();
        group.MapGet("/export", async (ClaimsPrincipal principal, UserManager<CommunityUser> users, CommunityDb db, CancellationToken token) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            return Results.Ok(new
            {
                account = new { user.Id, user.UserName, user.Email, user.AcceptDirectMessages },
                externalLogins = await users.GetLoginsAsync(user),
                healthSharing = await db.PublicHealthShares.AsNoTracking().SingleOrDefaultAsync(h=>h.UserId==user.Id,token),
                followedPosts = await db.FollowedPosts.AsNoTracking().Where(f=>f.UserId==user.Id).ToArrayAsync(token),
                replyRevisions = await db.ReplyRevisions.AsNoTracking().Where(r=>r.AuthorId==user.Id).ToArrayAsync(token),
                socialProfile = await db.SocialProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == user.Id, token),
                votes = await db.PostVotes.AsNoTracking().Where(v => v.UserId == user.Id).ToListAsync(token),
                savedPosts = await db.SavedPosts.AsNoTracking().Where(v => v.UserId == user.Id).ToListAsync(token),
                followedCategories = await db.FollowedCategories.AsNoTracking().Where(v => v.UserId == user.Id).ToListAsync(token),
                notifications = await db.SocialNotifications.AsNoTracking().Where(v => v.UserId == user.Id).ToListAsync(token),
                revisions = await db.PostRevisions.AsNoTracking().Where(v => v.AuthorId == user.Id).ToListAsync(token),
                publicHardware = await db.PublicHardware.AsNoTracking().Where(f => f.UserId == user.Id).ToDictionaryAsync(f => f.Field, f => f.Value, token),
                posts = await db.Posts.AsNoTracking().Where(p => p.AuthorId == user.Id).Select(p => new { p.Id, p.Title, p.Body, p.CreatedAt }).ToListAsync(token),
                replies = await db.Replies.AsNoTracking().Where(p => p.AuthorId == user.Id).Select(p => new { p.Id, p.Body, p.CreatedAt }).ToListAsync(token),
                messages = await db.Messages.AsNoTracking().Where(m => m.SenderId == user.Id || m.RecipientId == user.Id)
                    .Select(m => new { m.Id, m.SenderId, m.RecipientId, m.Body, m.CreatedAt }).ToListAsync(token)
            });
        }).RequireAuthorization();
        group.MapPost("/delete", async (PasswordConfirmation request, ClaimsPrincipal principal, UserManager<CommunityUser> users,
            SignInManager<CommunityUser> signIn, CommunityDb db, CancellationToken token) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            var hasPassword=await users.HasPasswordAsync(user);
            var recentGoogle=principal.HasClaim("amr","google") && long.TryParse(principal.FindFirstValue("auth_time"),out var authTime) && DateTimeOffset.UtcNow.ToUnixTimeSeconds()-authTime is >=0 and <=300 && !user.TwoFactorEnabled && (await users.GetLoginsAsync(user)).Any(l=>l.LoginProvider=="Google");
            if(hasPassword?(request.Password is null||request.Password.Length>128||!await users.CheckPasswordAsync(user,request.Password)):!recentGoogle)return Results.Unauthorized();
            if ((await users.GetRolesAsync(user)).Any(role => role != "User"))
                return Results.Conflict(new { error = "Contas da equipe exigem transferência de responsabilidade antes da exclusão." });
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            await db.UserLogins.Where(l=>l.UserId==user.Id).ExecuteDeleteAsync(token);
            await db.UserTokens.Where(t=>t.UserId==user.Id).ExecuteDeleteAsync(token);
            await db.UserClaims.Where(c=>c.UserId==user.Id).ExecuteDeleteAsync(token);
            await db.UserRoles.Where(r=>r.UserId==user.Id).ExecuteDeleteAsync(token);
            await db.PublicHealthShares.Where(f=>f.UserId==user.Id).ExecuteDeleteAsync(token);
            await db.FollowedPosts.Where(f=>f.UserId==user.Id).ExecuteDeleteAsync(token);
            await db.ReplyRevisions.Where(r=>r.AuthorId==user.Id).ExecuteDeleteAsync(token);
            await db.PublicHardware.Where(f => f.UserId == user.Id).ExecuteDeleteAsync(token);
            await db.Messages.Where(m => m.SenderId == user.Id || m.RecipientId == user.Id).ExecuteDeleteAsync(token);
            await db.Blocks.Where(b => b.OwnerId == user.Id || b.BlockedId == user.Id).ExecuteDeleteAsync(token);
            await db.MfaUses.Where(m => m.UserId == user.Id).ExecuteDeleteAsync(token);
            await db.PostVotes.Where(v => v.UserId == user.Id).ExecuteDeleteAsync(token);
            await db.SavedPosts.Where(v => v.UserId == user.Id).ExecuteDeleteAsync(token);
            await db.FollowedCategories.Where(v => v.UserId == user.Id).ExecuteDeleteAsync(token);
            await db.SocialProfiles.Where(v => v.UserId == user.Id).ExecuteDeleteAsync(token);
            await db.SocialNotifications.Where(v => v.UserId == user.Id).ExecuteDeleteAsync(token);
            await db.PostRevisions.Where(v => v.AuthorId == user.Id).ExecuteDeleteAsync(token);
            // Keep forum integrity with a tombstone account; remove identity/profile content.
            foreach (var post in await db.Posts.Where(p => p.AuthorId == user.Id).ToListAsync(token)) { post.Title = "Conteúdo removido pelo titular"; post.Body = ""; post.Hidden = true; }
            foreach (var reply in await db.Replies.Where(p => p.AuthorId == user.Id).ToListAsync(token)) { reply.Body = ""; reply.Hidden = true; reply.ReviewState = "Unverified"; }
            foreach (var report in await db.Reports.Where(r => r.ReporterId == user.Id).ToListAsync(token)) report.Reason = "Detalhes removidos pelo titular";
            user.UserName = "deleted_" + Guid.NewGuid().ToString("N");
            user.NormalizedUserName = user.UserName.ToUpperInvariant();
            user.Email = null; user.NormalizedEmail = null; user.EmailConfirmed = false;
            user.PasswordHash = null; user.PhoneNumber = null; user.TwoFactorEnabled = false; user.AcceptDirectMessages = false;
            await users.RemoveAuthenticationTokenAsync(user, "[AspNetUserStore]", "AuthenticatorKey");
            await users.RemoveAuthenticationTokenAsync(user, "[AspNetUserStore]", "RecoveryCodes");
            await users.UpdateSecurityStampAsync(user);
            db.Audit.Add(new() { ActorId = user.Id, Action = "account-anonymized", TargetId = user.Id });
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();
        group.MapPut("/direct-messages/{enabled:bool}", async (bool enabled, ClaimsPrincipal principal, UserManager<CommunityUser> users) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            user.AcceptDirectMessages = enabled;
            var result = await users.UpdateAsync(user);
            return result.Succeeded ? Results.NoContent() : Results.Conflict();
        }).RequireAuthorization();
        // Uploads remain explicitly unavailable until scanning, decoding and isolated storage are configured.
        app.MapPost("/api/v1/uploads", () => Results.Problem("Uploads indisponíveis: storage isolado e processamento seguro não configurados.", statusCode: 503)).RequireAuthorization();
    }
}

