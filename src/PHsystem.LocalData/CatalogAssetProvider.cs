using System.Diagnostics;
using System.Text.Json;
using PHsystem.Core;

namespace PHsystem.LocalData;

public sealed class CatalogAssetProvider : IAssetProvider
{
    private readonly LocalAssetProvider? configured;
    private readonly LocalAssetProvider? installed;
    public CatalogAssetProvider(string installedDirectory, string? configuredDirectory = null)
    {
        installed = TryOpen(installedDirectory);
        if (!string.IsNullOrWhiteSpace(configuredDirectory)) configured = TryOpen(configuredDirectory);
    }
    public AssetResult Get(string path)
    {
        var result = configured?.Get(path);
        if (result?.State == AssetState.Available) return result;
        result = installed?.Get(path) ?? new(AssetState.Missing, "texto ou geometria neutra", []);
        if (result.State != AssetState.Available) Trace.TraceInformation("Asset local indisponível; fallback gráfico utilizado.");
        return result;
    }
    public void ClearCache() { configured?.ClearCache(); installed?.ClearCache(); }
    private static LocalAssetProvider? TryOpen(string directory)
    {
        try { return new(directory); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        { Trace.TraceWarning("Catálogo local inválido ou ausente; fallback gráfico utilizado."); return null; }
    }
}
