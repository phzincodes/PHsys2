using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using PHsystem.Desktop;
using PHsystem.SharedContracts.V1;
using Xunit;

namespace PHsystem.UiTests;

internal sealed class FixedEndpoint(Uri? address) : IApiEndpointSource
{ public ApiEndpoint Resolve() => new(address, "Testing"); }

public sealed class ApiConnectionTests
{
    private static readonly Uri Endpoint = new("https://localhost:41937/"); // Isolated handler; no socket or fixed development port.
    private static CommunityClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) =>
        new(new SessionVault(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "session.bin")), _ => new Handler(send));
    private static HttpResponseMessage Healthy(bool registration = true) => new(HttpStatusCode.OK)
    { Content = JsonContent.Create(new ApiHealthDto("PHsystem.Next", 1, "ready", true, registration, registration)) };

    [Fact]
    public async Task AvailableApiIsCheckedAndSameOriginDoesNotDiscardCookies()
    {
        var calls = 0;
        using var client = Client((_, _) => { calls++; return Task.FromResult(Healthy()); });
        var connection = new ApiConnectionService(client, new FixedEndpoint(Endpoint));
        Assert.True(await connection.EnsureAsync());
        Assert.True(await connection.EnsureAsync()); Assert.Equal(1, calls);
        Assert.True(connection.Current.Health!.RegistrationAvailable);
    }
    [Fact]
    public async Task TemporaryFailureRetriesHealthOnlyThenReconnects()
    {
        var calls = 0;
        using var client = Client((_, _) => Task.FromResult(++calls < 3 ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            { Content = JsonContent.Create(new ApiHealthDto("PHsystem.Next", 1, "unavailable", false, false, false)) } : Healthy()));
        var service = new ApiConnectionService(client, new FixedEndpoint(Endpoint));
        Assert.Equal(ApiConnectionState.Available, (await service.CheckAsync()).State); Assert.Equal(3, calls);
    }
    [Fact]
    public async Task UnavailableApiStopsAfterThreeAttemptsAndManualRetryCanRecover()
    {
        var available = false; var calls = 0;
        using var client = Client((_, _) => { calls++; return available ? Task.FromResult(Healthy()) : throw new HttpRequestException("fixture network failure"); });
        var service = new ApiConnectionService(client, new FixedEndpoint(Endpoint));
        Assert.Equal(ApiConnectionState.Unavailable, (await service.CheckAsync()).State); Assert.Equal(3, calls);
        available = true; Assert.Equal(ApiConnectionState.Available, (await service.CheckAsync()).State); Assert.Equal(4, calls);
    }
    [Fact]
    public async Task NoEndpointMakesNoNetworkRequestAndNeverAuthenticates()
    {
        var calls = 0;
        using var client = Client((_, _) => { calls++; return Task.FromResult(Healthy()); });
        var service = new ApiConnectionService(client, new FixedEndpoint(null));
        var model = new AuthenticationViewModel(client, service);
        await model.RestoreAsync();
        Assert.Equal(ApiConnectionService.UnavailableMessage, model.ConnectionMessage);
        Assert.False(model.CanLogin); Assert.False(model.CanCreateAccount); Assert.True(model.IsReady); Assert.False(model.IsChecking); Assert.Equal(0, calls);
    }
    [Fact]
    public async Task CertificateErrorsStopImmediatelyWithoutCredentialsOrRetries()
    {
        var calls = 0;
        using var client = Client((_, _) => { calls++; throw new HttpRequestException(HttpRequestError.SecureConnectionError, "certificate fixture"); });
        var model = new AuthenticationViewModel(client, new ApiConnectionService(client, new FixedEndpoint(Endpoint))) { Page = AuthenticationPage.Login, Username = "FixtureUser" };
        await model.SubmitAsync("ValidPass!123", "");
        Assert.Equal(1, calls); Assert.Contains("conexão segura", model.ConnectionDetail);
        Assert.Equal(ApiConnectionService.UnavailableMessage, model.Status); Assert.True(model.IsReady); Assert.False(model.CanLogin);
    }
    [Fact]
    public async Task UnrelatedOrRedirectedServiceCannotReceiveCredentials()
    {
        using var client = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { status = "healthy" }) }));
        var service = new ApiConnectionService(client, new FixedEndpoint(Endpoint));
        Assert.Equal(ApiConnectionState.Incompatible, (await service.CheckAsync()).State);
        Assert.False(service.Available);
    }
    [Fact]
    public async Task DegradedEmailDoesNotBlockExistingLoginButDisablesRegistration()
    {
        using var client = Client((_, _) => Task.FromResult(Healthy(false)));
        var model = new AuthenticationViewModel(client, new ApiConnectionService(client, new FixedEndpoint(Endpoint)));
        await model.RestoreAsync(); Assert.True(model.CanLogin); Assert.False(model.CanCreateAccount); Assert.False(model.CanRecover);
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token); }

    [Fact]
    public async Task AuthenticationMutationIsNeverAutomaticallyRetried()
    {
        var posts = 0;
        using var client = Client((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/health") return Task.FromResult(Healthy());
            if (request.Method == HttpMethod.Get) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { token = "csrf-fixture" }) });
            posts++; throw new HttpRequestException("interrupted fixture request");
        });
        var model = new AuthenticationViewModel(client, new ApiConnectionService(client, new FixedEndpoint(Endpoint))) { Page = AuthenticationPage.Login, Username = "FixtureUser" };
        await model.SubmitAsync("ValidPass!123", "");
        Assert.Equal(1, posts); Assert.True(model.IsReady); Assert.False(model.IsChecking);
        Assert.Equal(ApiConnectionService.UnavailableMessage, model.Status);
        await model.RestoreAsync(); Assert.Equal(1, posts); // Retry performs discovery/health only.
    }

    [Fact]
    public async Task CancellationDoesNotLeaveAnInfiniteCheckingState()
    {
        using var client = Client(async (_, token) => { await Task.Delay(Timeout.Infinite, token); return Healthy(); });
        var service = new ApiConnectionService(client, new FixedEndpoint(Endpoint));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CheckAsync(cancellation.Token));
        Assert.Equal(ApiConnectionState.Unavailable, service.Current.State);
    }

    [Fact]
    public async Task UnresponsiveServiceReachesItsOwnDeadlineWithoutUserCancellation()
    {
        var calls = 0;
        using var client = Client(async (_, token) => { calls++; await Task.Delay(Timeout.Infinite, token); return Healthy(); });
        var service = new ApiConnectionService(client, new FixedEndpoint(Endpoint));
        var result = await service.CheckAsync().WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Equal(ApiConnectionState.Unavailable, result.State);
        Assert.InRange(calls, 1, 3);
    }

    [Fact]
    public void StoredSessionCannotSelectAnOldOrDifferentServer()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var vault = new SessionVault(Path.Combine(directory, "session.bin"));
        try
        {
            vault.Save(new("https://another.example.com/", "__Host-PHsystem=synthetic-cookie", DateTimeOffset.UtcNow.AddMinutes(1)));
            var calls = 0;
            using var client = new CommunityClient(vault, _ => new Handler((_, _) => { calls++; return Task.FromResult(Healthy()); }));
            client.Connect(Endpoint.AbsoluteUri);
            Assert.False(client.RestoreSession()); Assert.Equal(0, calls); Assert.Equal(Endpoint.AbsoluteUri, client.ApiAddress); Assert.Null(vault.Load());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task ChangingEndpointDoesNotInventLogoutForAnAnonymousRegistrationFlow()
    {
        using var client = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        { Content = JsonContent.Create(new { id = "fixture", userName = "FixtureUser", expiresAt = DateTimeOffset.UtcNow.AddMinutes(1) }) }));
        var ended = 0; client.SessionEnded += (_, _) => ended++;
        client.Connect(Endpoint.AbsoluteUri);
        client.Connect("https://localhost:41938/");
        Assert.Equal(0, ended); // Retry/discovery must not change the current anonymous form to Login.
        await client.GetAsync<System.Text.Json.JsonElement>("/api/v1/account/me");
        client.Connect("https://localhost:41939/");
        Assert.Equal(1, ended); Assert.Equal("", client.Username); // A real session is invalidated across origins.
    }
}
