using System.Text.RegularExpressions;

namespace PHsystem.Community;

public static partial class CommunityPolicy
{
    public static readonly string[] Categories = ["hardware", "cpu", "gpu", "windows", "games", "drivers", "network", "storage", "crashes", "bluescreen", "build", "optimization"];
    public static readonly string[] PostKinds = ["discussion", "question", "tutorial", "suggestion", "bug", "announcement"];
    public static readonly string[] Roles = ["User", "TrustedReviewer", "Moderator", "Admin"];
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
        { "Zphzin", "admin", "administrator", "moderator", "system", "phsystem", "support", "trustedreviewer" };
    public static bool ValidUsername(string? username) => username is not null && UsernamePattern().IsMatch(username) && !Reserved.Contains(username);
    public static bool ValidBody(string? body) => !string.IsNullOrWhiteSpace(body) && body.Length <= 8000 && !body.Contains('\0');
    public static bool RequiresReview(string body) => RiskPattern().IsMatch(body) || body.Contains('<') || body.Contains('>');
    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{2,23}$", RegexOptions.CultureInvariant)]
    private static partial Regex UsernamePattern();
    [GeneratedRegex(@"(?i)(powershell|cmd\s*/|invoke-expression|iex\b|encodedcommand|format\s+[a-z]:|reg\s+(add|delete)|disable.{0,30}(defender|firewall)|desativ.{0,30}(defender|firewall|antiv)|https?://|\.exe\b|\.ps1\b|\.bat\b|senha|password|serial|mac\s*address)", RegexOptions.CultureInvariant)]
    private static partial Regex RiskPattern();
}
