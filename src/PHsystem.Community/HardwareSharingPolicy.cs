using System.Text.RegularExpressions;

namespace PHsystem.Community;

public static class HardwareSharingPolicy
{
    private static readonly Regex Sensitive = new(@"(?:[A-Za-z]:[\\/]|\\\\|/home/|/Users/|https?://|[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}|\b(?:\d{1,3}\.){3}\d{1,3}\b|\b(?:[0-9A-F]{2}[:-]){5}[0-9A-F]{2}\b|\b[0-9A-F]{8}-(?:[0-9A-F]{4}-){3}[0-9A-F]{12}\b|\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+|\b(?:password|senha|token|secret|serial|hostname|uuid)\s*[:=])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50));
    public static bool SafeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 200 || value.Any(char.IsControl) || value.Contains('<') || value.Contains('>')) return false;
        try { return !Sensitive.IsMatch(value) && !System.Net.IPAddress.TryParse(value, out _); }
        catch (RegexMatchTimeoutException) { return false; }
    }
}
