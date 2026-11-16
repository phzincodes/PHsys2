using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using PHsystem.Desktop;
using Xunit;

namespace PHsystem.UiTests;

public sealed class ApiErrorTests
{
    [Theory]
    [InlineData(400, "Revise")]
    [InlineData(401, "sessão")]
    [InlineData(403, "permissão")]
    [InlineData(404, "indisponível")]
    [InlineData(409, "conflito")]
    [InlineData(422, "regras")]
    [InlineData(500, "Não foi possível conectar")]
    public async Task ErrorsAreTypedCorrelatedAndDoNotExposeServerDetails(int status, string expected)
    {
        const string id = "abcdef0123456789abcdef0123456789";
        using var client = new CommunityClient(new SessionVault(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "session.bin")), _ => new Handler(_ =>
        {
            var response = new HttpResponseMessage((HttpStatusCode)status) { Content = JsonContent.Create(new { detail = "do-not-expose-private-server-fixture" }) };
            response.Headers.Add("X-Correlation-ID", id); return response;
        }));
        client.Connect("https://localhost:40001/");
        var error = await Assert.ThrowsAsync<ApiResponseException>(() => client.GetAsync<object>("/api/v1/test"));
        Assert.Equal((HttpStatusCode)status, error.StatusCode); Assert.Equal(id, error.CorrelationId);
        Assert.Contains(expected, error.Message, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("do-not-expose", error.Message);
    }
    [Fact]
    public async Task RetryAfterPreventsAdditionalRequestsWithoutPretendingTheServerIsDown()
    {
        var calls = 0;
        using var client = new CommunityClient(new SessionVault(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "session.bin")), _ => new Handler(_ =>
        {
            calls++; var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new(TimeSpan.FromSeconds(60)); return response;
        }));
        client.Connect("https://localhost:40001/");
        var error = await Assert.ThrowsAsync<ApiResponseException>(() => client.GetAsync<object>("/api/v1/test")); Assert.True(error.RetryAfter > TimeSpan.Zero);
        await Assert.ThrowsAsync<ApiResponseException>(() => client.SendAsync(HttpMethod.Post, "/api/v1/test"));
        var service = new ApiConnectionService(client, new FixedEndpoint(new("https://localhost:40001/")));
        Assert.Equal(ApiConnectionState.RateLimited, (await service.CheckAsync()).State); Assert.Equal(1, calls);
    }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(response(request)); }
}
