using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace PHsystem.Server;

public interface IAccountEmail
{
    bool Configured { get; }
    Task SendAsync(string email, string subject, string body, CancellationToken token);
}
public sealed class SmtpAccountEmail(IConfiguration configuration) : IAccountEmail
{
    public bool Configured => !string.IsNullOrWhiteSpace(configuration["Email:Host"]) && !string.IsNullOrWhiteSpace(configuration["Email:From"])
        && (configuration["ASPNETCORE_ENVIRONMENT"] == "Development" ||
            (Uri.TryCreate(configuration["Email:PublicBaseUrl"], UriKind.Absolute, out var origin) && origin.Scheme == "https" && !origin.IsLoopback && origin.AbsolutePath == "/" && origin.UserInfo.Length == 0 && origin.Query.Length == 0 && origin.Fragment.Length == 0 && configuration.GetValue("Email:EnableSsl", true)));
    public async Task SendAsync(string email, string subject, string body, CancellationToken token)
    {
        if (!Configured) throw new InvalidOperationException("Provedor de email não configurado.");
        using var client = new SmtpClient(configuration["Email:Host"], configuration.GetValue("Email:Port", 587))
        {
            EnableSsl = configuration.GetValue("Email:EnableSsl", true), Timeout = 10000
        };
        if (configuration["Email:Username"] is { Length: > 0 } username)
            client.Credentials = new NetworkCredential(username, configuration["Email:Password"]);
        using var message = new MailMessage(configuration["Email:From"]!, email, subject, body) { IsBodyHtml = false };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(body, Encoding.UTF8, "text/plain"));
        var encoded = WebUtility.HtmlEncode(body).Replace("\n", "<br>");
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString("<!doctype html><html lang=pt-BR><body style=\"background:#07080B;color:#F3F4F6;font-family:Arial;padding:32px\"><h1 style=\"color:#FF3347\">PHsystem</h1><h2>Red Eclipse / sua conta</h2><p>" + encoded + "</p></body></html>", Encoding.UTF8, "text/html"));
        await client.SendMailAsync(message, token);
    }
}

public sealed class CompromisedPasswordValidator(IConfiguration configuration) : IPasswordValidator<CommunityUser>
{
    public static bool IsConfigured(IConfiguration configuration)
    {
        try
        {
            var path = configuration["Security:CompromisedPasswordHashes"];
            return !string.IsNullOrWhiteSpace(path) && new FileInfo(path) is { Exists: true, Length: >= 64 and <= 33554432 };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { return false; }
    }
    public Task<IdentityResult> ValidateAsync(UserManager<CommunityUser> manager, CommunityUser user, string? password)
    {
        if (password is null || password.Length > 128) return Task.FromResult(Failure("PasswordLength", "A senha deve ter entre 8 e 128 caracteres."));
        var file = configuration["Security:CompromisedPasswordHashes"];
        if (!IsConfigured(configuration))
            return Task.FromResult(Failure("PasswordScreeningUnavailable", "Verificação de senhas comprometidas não configurada."));
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        // SHA-256 here is only an offline denylist lookup. Identity owns salted PBKDF2 password storage.
        if (File.ReadLines(file!).Any(line => line.Trim().Equals(digest, StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(Failure("CompromisedPassword", "Escolha uma senha que não esteja na lista de comprometidas."));
        return Task.FromResult(IdentityResult.Success);
    }
    private static IdentityResult Failure(string code, string description) => IdentityResult.Failed(new IdentityError { Code = code, Description = description });
}
