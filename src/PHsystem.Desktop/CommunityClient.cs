using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using PHsystem.SharedContracts.V1;

namespace PHsystem.Desktop;

public sealed class CommunityClient : IDisposable
{
    private HttpClient? client;
    private CookieContainer cookies = new();
    private bool remember;
    private DateTimeOffset? retryUntil;
    public string UserId {get;private set;}="";
    public string Username { get; private set; } = "";
    public string ApiAddress => client?.BaseAddress?.AbsoluteUri ?? "";
    public DateTimeOffset? SessionExpiresAt { get; private set; }
    public event EventHandler? SessionStarted;
    private readonly SessionVault vault;
    private readonly Func<CookieContainer, HttpMessageHandler>? transportFactory;
    public CommunityClient() : this(new SessionVault(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PHsystemNext", "session.bin"))) { }
    public CommunityClient(SessionVault vault) => this.vault = vault;
    internal CommunityClient(SessionVault vault, Func<CookieContainer, HttpMessageHandler> transportFactory) : this(vault) => this.transportFactory = transportFactory;
    public void RememberSession(bool enabled) { remember = enabled; if (enabled) SaveSession(); else vault.Clear(); }
    public bool HasRememberedSession => vault.Load() is not null;
    private void SaveSession()
    {
        if (!remember || client?.BaseAddress is not { } origin) return;
        var authCookies = cookies.GetCookies(origin).Cast<Cookie>().Where(c => c.Name.StartsWith("__Host-PHsystem", StringComparison.Ordinal)).ToArray();
        if (authCookies.Length == 0) return;
        vault.Save(new StoredSession(origin.AbsoluteUri, string.Join("; ", authCookies.Select(c => c.Name + "=" + c.Value)),
            authCookies.Min(c => c.Expires == DateTime.MinValue ? DateTimeOffset.UtcNow.AddHours(4) : new DateTimeOffset(c.Expires.ToUniversalTime()))));
    }
    public bool RestoreSession()
    {
        var saved = vault.Load();
        if (saved is null) return false;
        if (client?.BaseAddress is not { } configured || !Uri.TryCreate(saved.Origin, UriKind.Absolute, out var savedOrigin) || configured != savedOrigin)
        { vault.Clear(); return false; }
        foreach (var cookie in saved.CookieHeader.Split("; ", StringSplitOptions.RemoveEmptyEntries))
            cookies.SetCookies(client!.BaseAddress!, cookie + "; Path=/; Secure; HttpOnly");
        remember = true; SessionExpiresAt = saved.ExpiresAt;
        return true;
    }
    public event EventHandler? SessionEnded;
    public void ClearSession()
    {
        remember = false; Username = ""; UserId=""; SessionExpiresAt = null; Dispose();
        try { vault.Clear(); }
        finally { SessionEnded?.Invoke(this, EventArgs.Empty); }
    }
    public void Connect(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.AbsolutePath != "/"
            || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            throw new InvalidOperationException(ApiConnectionService.UnavailableMessage);
        if (client?.BaseAddress == uri) return;
        if (client is not null && (Username.Length > 0 || SessionExpiresAt is not null)) ClearSession();
        client?.Dispose();
        cookies = new CookieContainer();
        retryUntil = null;
        client = new HttpClient(transportFactory?.Invoke(cookies) ?? new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = false })
        { BaseAddress = uri, Timeout = TimeSpan.FromSeconds(15), MaxResponseContentBufferSize = 1024 * 1024 };
    }
    public async Task<(HttpStatusCode Status, ApiHealthDto? Health, TimeSpan? RetryAfter)> CheckHealthAsync(CancellationToken token)
    {
        if (RemainingCooldown is { } remaining) return (HttpStatusCode.TooManyRequests, null, remaining);
        using var response = await Current.GetAsync("/health", token);
        if (response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable))
        { var retry = TrackRetry(response); return (response.StatusCode, null, retry); }
        if (response.Content.Headers.ContentLength > 16384) throw new JsonException("Resposta de saúde inválida.");
        var report = await response.Content.ReadFromJsonAsync<ApiHealthDto>(token);
        return (response.StatusCode, report, null);
    }
    public async Task<T> GetAsync<T>(string path)
    {
        CheckCooldown();
        using var request=new HttpRequestMessage(HttpMethod.Get,path);
        if(UserId.Length>0&&RequiresActor(path))request.Headers.Add("X-PHsystem-Actor",UserId);
        using var response = await Current.SendAsync(request);
        EnsureSuccess(response);
        var result = await response.Content.ReadFromJsonAsync<T>() ?? throw new InvalidDataException("Resposta vazia da comunidade.");
        if (path == "/api/v1/account/me" && result is JsonElement identity && identity.TryGetProperty("userName", out var username))
        {
            Username = username.GetString() ?? "";
            UserId=identity.TryGetProperty("id",out var id)?id.GetString()??"":"";
            SessionExpiresAt = identity.TryGetProperty("expiresAt", out var expiry) && expiry.TryGetDateTimeOffset(out var serverExpiry) ? serverExpiry : DateTimeOffset.UtcNow.AddHours(4);
            SessionStarted?.Invoke(this, EventArgs.Empty);
        }
        return result;
    }
    public async Task SendAsync(HttpMethod method, string path, object? body = null)
    {
        using var response = await SendResponseAsync(method, path, body);
        if (path is "/api/v1/account/login" or "/api/v1/account/refresh-session") SessionExpiresAt = DateTimeOffset.UtcNow.AddHours(4);
        if (path == "/api/v1/account/refresh-session") await GetAsync<JsonElement>("/api/v1/account/me");
        SaveSession();
    }
    private async Task<HttpResponseMessage> SendResponseAsync(HttpMethod method,string path,object? body)
    {
        CheckCooldown();
        var csrf=await GetAsync<JsonElement>("/api/v1/security/csrf");
        using var request=new HttpRequestMessage(method,path);if(UserId.Length>0&&RequiresActor(path))request.Headers.Add("X-PHsystem-Actor",UserId);request.Headers.Add("X-CSRF-TOKEN",csrf.GetProperty("token").GetString());
        if(body is not null)request.Content=JsonContent.Create(body);
        var response=await Current.SendAsync(request);
        try {EnsureSuccess(response);return response;} catch {response.Dispose();throw;}
    }
    public async Task<T> SendResultAsync<T>(string path,object body)
    {
        using var response=await SendResponseAsync(HttpMethod.Post,path,body);
        return await response.Content.ReadFromJsonAsync<T>()??throw new InvalidDataException("Resposta vazia.");
    }
    private static bool RequiresActor(string path)=>path is not (
        "/api/v1/security/csrf" or "/api/v1/account/me" or "/api/v1/account/login" or "/api/v1/account/register" or
        "/api/v1/account/forgot-password" or "/api/v1/account/reset-password" or "/api/v1/account/confirm-email" or "/api/v1/account/resend-confirmation");
    private HttpClient Current => client ?? throw new InvalidOperationException(ApiConnectionService.UnavailableMessage);
    private void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized) ClearSession();
        var retry = TrackRetry(response);
        var correlation = response.Headers.TryGetValues("X-Correlation-ID", out var ids) ? ids.FirstOrDefault() : null;
        if (correlation?.Length != 32 || !correlation.All(Uri.IsHexDigit)) correlation = null;
        var message = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => "Revise os campos e os requisitos desta operação.",
            HttpStatusCode.ServiceUnavailable => ApiConnectionService.UnavailableMessage,
            HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout => ApiConnectionService.UnavailableMessage,
            HttpStatusCode.Unauthorized => "Não foi possível validar a sessão. Confira suas credenciais, confirmação de e-mail e código de autenticação.",
            HttpStatusCode.Forbidden => "Esta conta não tem permissão para essa ação.",
            HttpStatusCode.Conflict => "A alteração entrou em conflito. Atualize o conteúdo antes de tentar novamente.",
            HttpStatusCode.UnprocessableEntity => "Os dados não atendem às regras desta operação.",
            HttpStatusCode.TooManyRequests => retry is { TotalSeconds: > 0 } wait ? $"Limite de requisições. Tente novamente após {Math.Ceiling(wait.TotalSeconds):N0} segundos." : "Limite de requisições. Aguarde antes de tentar novamente.",
            HttpStatusCode.NotFound => "Registro indisponível ou acesso não autorizado.",
            _ => $"A API não confirmou a operação (HTTP {(int)response.StatusCode}). Revise os campos e tente novamente."
        };
        if (correlation is not null) message += $" Código de suporte: {correlation}.";
        throw new ApiResponseException(response.StatusCode, message, retry, correlation);
    }
    public void Dispose() { client?.Dispose(); client = null; }
    private TimeSpan? RemainingCooldown => retryUntil is { } until && until > DateTimeOffset.UtcNow ? until - DateTimeOffset.UtcNow : null;
    private TimeSpan? TrackRetry(HttpResponseMessage response)
    {
        var retry = response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : null);
        if (response.StatusCode == HttpStatusCode.TooManyRequests && retry is { TotalSeconds: > 0 }) retryUntil = DateTimeOffset.UtcNow + retry.Value;
        return retry;
    }
    private void CheckCooldown()
    {
        if (RemainingCooldown is { } wait) throw new ApiResponseException(HttpStatusCode.TooManyRequests, $"Limite de requisições. Tente novamente após {Math.Ceiling(wait.TotalSeconds):N0} segundos.", wait);
    }
}

public sealed class ApiResponseException(HttpStatusCode statusCode, string message, TimeSpan? retryAfter = null, string? correlationId = null) : InvalidOperationException(message)
{ public HttpStatusCode StatusCode => statusCode; public TimeSpan? RetryAfter => retryAfter; public string? CorrelationId => correlationId; }
