using System.IO;
using System.Text.Json;

namespace PHsystem.Desktop;

public sealed record ApiEndpoint(Uri? Address, string Environment);
public interface IApiEndpointSource { ApiEndpoint Resolve(); }

/// <summary>Distribution-owned configuration, never an end-user form or a port scan.</summary>
public sealed class ApiEndpointConfiguration(string configurationFile, string discoveryFile, bool developmentBuild,
    Func<string, string?>? environment = null) : IApiEndpointSource
{
    private sealed record Settings(int SchemaVersion, string Environment, string? ApiBaseUrl);
    private sealed record Discovery(int SchemaVersion, string ApiBaseUrl, DateTimeOffset ExpiresAt);
    public static ApiEndpointConfiguration Installed()
    {
#if DEBUG
        const bool development = true;
#else
        const bool development = false;
#endif
        return new(Path.Combine(AppContext.BaseDirectory, "services.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PHsystemNext", "development", "api-discovery.json"), development);
    }
    public ApiEndpoint Resolve()
    {
        try
        {
            var settings = Read<Settings>(configurationFile);
            if (settings is not { SchemaVersion: 1 } || settings.Environment is not ("Auto" or "Development" or "Production")) return new(null, "Unconfigured");
            var development = developmentBuild && settings.Environment != "Production";
            var mode = development ? "Development" : "Production";
            if (development)
            {
                var configured = (environment ?? System.Environment.GetEnvironmentVariable)("PHSYSTEM_API_URL");
                if (!string.IsNullOrWhiteSpace(configured)) return new(Validate(configured, true), mode);
                var discovery = Read<Discovery>(discoveryFile);
                if (discovery is { SchemaVersion: 1 } && discovery.ExpiresAt > DateTimeOffset.UtcNow && discovery.ExpiresAt < DateTimeOffset.UtcNow.AddDays(2))
                    return new(Validate(discovery.ApiBaseUrl, true, loopbackOnly: true), mode);
            }
            return new(Validate(settings.ApiBaseUrl, development), mode);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
        { return new(null, "Unconfigured"); }
    }
    public static Uri? Validate(string? address, bool development, bool loopbackOnly = false)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || uri.AbsolutePath != "/"
            || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0) return null;
        if (loopbackOnly && !uri.IsLoopback) return null;
        if (!development && (uri.IsLoopback || uri.HostNameType != UriHostNameType.Dns || uri.IdnHost.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || uri.IdnHost.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase))) return null;
        return uri;
    }
    private static T? Read<T>(string file)
    {
        if (!File.Exists(file) || new FileInfo(file).Length > 16384) return default;
        return JsonSerializer.Deserialize<T>(File.ReadAllText(file), JsonSerializerOptions.Web);
    }
}
