using System.Collections.Immutable;
using System.Text.Json;
using PHsystem.Core;

namespace PHsystem.LocalData;

/// <summary>Reads only catalogued, bounded local files. Does not decode or execute assets.</summary>
public sealed class LocalAssetProvider : IAssetProvider
{
    private const int MaxAssetBytes = 4 * 1024 * 1024;
    private const int MaxCacheBytes = 16 * 1024 * 1024;
    private readonly string root;
    private readonly Dictionary<string, Entry> entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CachedAsset> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object gate = new();
    private int cachedBytes;

    public LocalAssetProvider(string assetsDirectory)
    {
        root = Path.GetFullPath(assetsDirectory);
        var manifestPath = Path.Combine(root, "assets-manifest.json");
        EnsureNoLinks(manifestPath);
        using var stream = File.OpenRead(manifestPath);
        if (stream.Length > 256 * 1024)
            throw new InvalidDataException("Catálogo de assets excede o limite.");
        var manifest = JsonSerializer.Deserialize<Manifest>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (manifest is null || manifest.Version != 1 || manifest.Assets is null || manifest.Assets.Length > 512)
            throw new InvalidDataException("Catálogo de assets inválido.");

        foreach (var entry in manifest.Assets)
        {
            if (entry is null || !IsSafePath(entry.Path) || string.IsNullOrWhiteSpace(entry.Fallback)
                || entry.MaxBytes is <= 0 or > MaxAssetBytes || !entries.TryAdd(entry.Path!, entry))
                throw new InvalidDataException("Entrada de asset inválida ou duplicada.");
        }
    }

    public AssetResult Get(string path)
    {
        if (!IsSafePath(path) || !entries.TryGetValue(path, out var entry))
            return new(AssetState.Rejected, "texto ou geometria neutra", []);

        lock (gate)
        {
            try
            {
                var fullPath = Path.Combine(root, entry.Path!.Replace('/', Path.DirectorySeparatorChar));
                EnsureNoLinks(fullPath);
                var info = new FileInfo(fullPath);
                if (cache.TryGetValue(path, out var cached))
                {
                    if (info.Length == cached.Length && info.LastWriteTimeUtc == cached.LastWriteUtc) return cached.Result;
                    cachedBytes -= cached.Result.Bytes.Length;
                    cache.Remove(path);
                }
                using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (stream.Length == 0 || stream.Length > entry.MaxBytes)
                    return new(AssetState.Rejected, entry.Fallback!, []);
                var bytes = new byte[(int)stream.Length];
                stream.ReadExactly(bytes);
                if (!AssetContentValidator.IsValid(path, bytes)) return new(AssetState.Rejected, entry.Fallback!, []);
                var result = new AssetResult(AssetState.Available, entry.Fallback!, ImmutableArray.CreateRange(bytes));
                if (cachedBytes + bytes.Length > MaxCacheBytes) ClearCache();
                cache.Add(path, new(result, info.Length, info.LastWriteTimeUtc));
                cachedBytes += bytes.Length;
                return result;
            }
            catch (FileNotFoundException) { return new(AssetState.Missing, entry.Fallback!, []); }
            catch (DirectoryNotFoundException) { return new(AssetState.Missing, entry.Fallback!, []); }
            catch (IOException) { return new(AssetState.Rejected, entry.Fallback!, []); }
            catch (UnauthorizedAccessException) { return new(AssetState.Rejected, entry.Fallback!, []); }
        }
    }

    public void ClearCache()
    {
        lock (gate)
        {
            cache.Clear();
            cachedBytes = 0;
        }
    }

    private static bool IsSafePath(string? path) => !string.IsNullOrWhiteSpace(path)
        && path.Split('/').All(segment => segment.Length > 0 && segment is not "." and not ".."
            && !IsDeviceName(segment)
            && segment.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))
        && Path.GetExtension(path).ToLowerInvariant() is ".png" or ".svg" or ".webp" or ".wav";

    private static bool IsDeviceName(string segment)
    {
        var name = segment.Split('.')[0].ToUpperInvariant();
        return name is "CON" or "PRN" or "AUX" or "NUL" ||
            (name.Length == 4 && (name.StartsWith("COM", StringComparison.Ordinal) ||
                name.StartsWith("LPT", StringComparison.Ordinal)) && name[3] is >= '1' and <= '9');
    }

    private static void EnsureNoLinks(string path)
    {
        for (string? current = path; current is not null; current = Path.GetDirectoryName(current))
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Links não são permitidos no caminho de assets.");
        }
    }

    private sealed record Manifest(int Version, Entry?[]? Assets);
    private sealed record Entry(string? Path, bool Required, string? Fallback, int MaxBytes);
    private sealed record CachedAsset(AssetResult Result, long Length, DateTime LastWriteUtc);
}
