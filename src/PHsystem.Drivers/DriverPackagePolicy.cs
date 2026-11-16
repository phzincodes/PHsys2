namespace PHsystem.Drivers;

public sealed record DriverTarget(string HardwareId, string OsVersion, string Architecture);
public sealed record DriverPackageEvidence(Uri Source, string HardwareId, string OsVersion, string Architecture,
    bool SignatureValid, string? Publisher, string ExpectedSha256, string ActualSha256, bool DownloadComplete);
public sealed record DriverDecision(bool CanProceed, IReadOnlyList<string> Reasons);

/// <summary>Validation-only domain policy. It never downloads or installs a package.</summary>
public sealed class DriverPackagePolicy(IEnumerable<string> officialHosts)
{
    private readonly HashSet<string> hosts = officialHosts.ToHashSet(StringComparer.OrdinalIgnoreCase);
    public DriverDecision Evaluate(DriverTarget target, DriverPackageEvidence package)
    {
        var reasons = new List<string>();
        if (package.Source.Scheme != "https" || !string.IsNullOrEmpty(package.Source.UserInfo) || !hosts.Contains(package.Source.IdnHost)) reasons.Add("Origem oficial HTTPS não comprovada.");
        if (!string.Equals(target.HardwareId, package.HardwareId, StringComparison.OrdinalIgnoreCase)
            || target.OsVersion != package.OsVersion || target.Architecture != package.Architecture) reasons.Add("Compatibilidade exata não comprovada.");
        if (!package.SignatureValid || string.IsNullOrWhiteSpace(package.Publisher)) reasons.Add("Assinatura/editor não verificados.");
        if (!package.DownloadComplete) reasons.Add("Download incompleto.");
        if (package.ExpectedSha256.Length != 64 || !package.ExpectedSha256.All(char.IsAsciiHexDigit)
            || !package.ExpectedSha256.Equals(package.ActualSha256, StringComparison.OrdinalIgnoreCase)) reasons.Add("Hash ausente ou divergente.");
        return new(reasons.Count == 0, reasons);
    }
}
