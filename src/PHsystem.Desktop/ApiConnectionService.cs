using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text.Json;
using PHsystem.SharedContracts.V1;

namespace PHsystem.Desktop;

public enum ApiConnectionState { NotChecked, Unconfigured, Checking, Available, Unavailable, CertificateError, Incompatible, RateLimited, AccessDenied }
public sealed record ApiConnectionResult(ApiConnectionState State, ApiHealthDto? Health = null);

public sealed class ApiConnectionService(CommunityClient client, IApiEndpointSource source)
{
    public const string UnavailableMessage = "Não foi possível conectar aos serviços do PHsystem.";
    private readonly SemaphoreSlim gate = new(1, 1);
    public ApiConnectionResult Current { get; private set; } = new(ApiConnectionState.NotChecked);
    public event EventHandler? Changed;
    private DateTimeOffset checkedAt;
    public bool Available => Current.State == ApiConnectionState.Available;
    public string Detail => Current.State switch
    {
        ApiConnectionState.Checking => "Verificando os serviços…",
        ApiConnectionState.NotChecked => "Os serviços online ainda não foram consultados. O diagnóstico local não depende deles.",
        ApiConnectionState.RateLimited => "O serviço limitou as consultas. Aguarde o prazo informado antes de tentar novamente.",
        ApiConnectionState.AccessDenied => "O serviço recusou acesso; isto não significa que esteja fora do ar.",
        ApiConnectionState.CertificateError => "Não foi possível validar a conexão segura. Nenhuma credencial foi enviada.",
        ApiConnectionState.Incompatible => "Esta versão não conseguiu validar os serviços. Verifique se há uma atualização oficial.",
        ApiConnectionState.Unconfigured => "Os serviços ainda não foram configurados nesta distribuição.",
        ApiConnectionState.Available when Current.Health?.RegistrationAvailable != true => "Cadastro e recuperação temporariamente indisponíveis.",
        ApiConnectionState.Available => "Conexão segura disponível.",
        _ => "Tente novamente em instantes. Seus diagnósticos permanecem neste computador."
    };
    public async Task<bool> EnsureAsync(CancellationToken token = default)
    {
        var configured = source.Resolve().Address;
        if (Available && configured is not null && client.ApiAddress == configured.AbsoluteUri && DateTimeOffset.UtcNow - checkedAt < TimeSpan.FromSeconds(30)) return true;
        return (await CheckAsync(token)).State == ApiConnectionState.Available;
    }
    public async Task<ApiConnectionResult> CheckAsync(CancellationToken token = default)
    {
        await gate.WaitAsync(token);
        try
        {
            Set(new(ApiConnectionState.Checking));
            var endpoint = source.Resolve();
            if (endpoint.Address is null) { Set(new(ApiConnectionState.Unconfigured)); return Current; }
            client.Connect(endpoint.Address.AbsoluteUri);
            // Bounded retry for health GET only. Credentials and all mutations are sent once.
            using var total = CancellationTokenSource.CreateLinkedTokenSource(token);
            total.CancelAfter(TimeSpan.FromSeconds(11));
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(total.Token);
                    timeout.CancelAfter(TimeSpan.FromSeconds(3));
                    var (status, report, _) = await client.CheckHealthAsync(timeout.Token);
                    if (status == HttpStatusCode.TooManyRequests) { Set(new(ApiConnectionState.RateLimited)); return Current; }
                    if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) { Set(new(ApiConnectionState.AccessDenied)); return Current; }
                    if (report is not null && report.Service == "PHsystem.Next" && report.ProtocolVersion == 1)
                    {
                        if (status == HttpStatusCode.OK && report.LoginAvailable && report.Status is "ready" or "degraded")
                        { checkedAt = DateTimeOffset.UtcNow; Set(new(ApiConnectionState.Available, report)); return Current; }
                    }
                    else if (status == HttpStatusCode.OK) { Set(new(ApiConnectionState.Incompatible)); return Current; }
                    if (status is not (HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout or HttpStatusCode.RequestTimeout)) break;
                }
                catch (HttpRequestException ex) when (IsCertificateFailure(ex)) { Set(new(ApiConnectionState.CertificateError)); return Current; }
                catch (JsonException) { Set(new(ApiConnectionState.Incompatible)); return Current; }
                catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
                { if (token.IsCancellationRequested) throw; if (total.IsCancellationRequested) break; }
                if (attempt < 2) await Task.Delay(TimeSpan.FromMilliseconds(250 * (attempt + 1)), total.Token);
            }
            Set(new(ApiConnectionState.Unavailable)); return Current;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { Set(new(ApiConnectionState.Unavailable)); return Current; }
        catch (OperationCanceledException) { Set(new(ApiConnectionState.Unavailable)); throw; }
        finally { gate.Release(); }
    }
    public void ReportFailure(Exception exception) => Set(new(exception is HttpRequestException request && IsCertificateFailure(request) ? ApiConnectionState.CertificateError : ApiConnectionState.Unavailable));
    private void Set(ApiConnectionResult result) { Current = result; Changed?.Invoke(this, EventArgs.Empty); }
    public static bool IsCertificateFailure(HttpRequestException exception)
    {
        if (exception.HttpRequestError == HttpRequestError.SecureConnectionError) return true;
        for (Exception? current = exception.InnerException; current is not null; current = current.InnerException) if (current is AuthenticationException) return true;
        return false;
    }
}
