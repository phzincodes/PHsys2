namespace PHsystem.Server;
public static class AccountEmailContent
{
    public static string Create(string userId, string token, bool reset, IConfiguration configuration)
    {
        var prefix = $"ID: {userId}\n" + (reset ? "Token válido por 30 minutos: " : "Token de confirmação: ") + token;
        var address = configuration["Email:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(address)) return prefix; // Legacy development flow only; release requires a public origin.
        if (!Uri.TryCreate(address, UriKind.Absolute, out var origin) || origin.Scheme != "https" || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 || origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("Origem pública de email inválida.");
        if (origin.IsLoopback && configuration["ASPNETCORE_ENVIRONMENT"] != "Development") throw new InvalidOperationException("Email de produção não pode usar localhost.");
        return prefix + "\n\n" + (reset ? "Redefinir senha: " : "Confirmar conta: ") + new Uri(origin, (reset ? "redefinir" : "confirmar") + "#userId=" + Uri.EscapeDataString(userId) + "&token=" + Uri.EscapeDataString(token))
            + "\n\nEste link é pessoal. Expira em 30 minutos. Se você não solicitou esta operação, ignore a mensagem.";
    }
}
