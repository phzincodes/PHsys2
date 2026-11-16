using PHsystem.Core;
using PHsystem.SharedContracts.V1;

namespace PHsystem.Desktop;

public sealed partial class MainViewModel
{
    private static readonly BuildIdentity DesktopIdentity = BuildIdentity.From(typeof(MainViewModel).Assembly);
    public string ApplicationVersion => DesktopIdentity.Version + " · desenvolvimento";
    public string DesktopBuildId => DesktopIdentity.BuildId;
    public string SourceCommit => DesktopIdentity.SourceCommit;
    public string ContractVersion => $"API v{ApiProtocol.Version}";
}

public sealed partial class CommunityViewModel
{
    public string ConnectionSummary => connection.Detail;
    public string ApiVersionSummary => connection.Current.Health is { } health
        ? $"{health.ApiVersion ?? "Versão não informada"} · build {health.BuildId ?? "não informado"} · protocolo {health.ProtocolVersion}"
        : "API não consultada ou indisponível";
    public string MigrationSummary => connection.Current.Health?.MigrationState switch { "current" => "Atualizadas", "pending" => "Pendentes", _ => "Não verificado" };
    private void ConnectionChanged(object? sender, EventArgs e)
    { Changed(nameof(ConnectionSummary)); Changed(nameof(ApiVersionSummary)); Changed(nameof(MigrationSummary)); }
}
