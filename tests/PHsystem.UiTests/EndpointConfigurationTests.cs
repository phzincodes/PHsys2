using System.IO;
using System.Text.Json;
using PHsystem.Desktop;
using Xunit;
namespace PHsystem.UiTests;
public sealed class EndpointConfigurationTests
{
    [Theory]
    [InlineData("http://localhost:1/")]
    [InlineData("https://user:password@example.com/")]
    [InlineData("https://example.com/api/")]
    [InlineData("https://example.com/?token=fixture")]
    [InlineData("https://example.com/#fragment")]
    public void UnsafeAddressesAreRejected(string address) => Assert.Null(ApiEndpointConfiguration.Validate(address, true));
    [Fact]
    public void ReleaseUsesDistributionDomainAndIgnoresDevelopmentEnvironmentAndDiscovery()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var file = Path.Combine(directory, "services.json"); var discovery = Path.Combine(directory, "discovery.json");
            File.WriteAllText(file, JsonSerializer.Serialize(new { schemaVersion = 1, environment = "Auto", apiBaseUrl = "https://services.example.com/" }));
            File.WriteAllText(discovery, JsonSerializer.Serialize(new { schemaVersion = 1, apiBaseUrl = "https://localhost:52789/", expiresAt = DateTimeOffset.UtcNow.AddHours(1) }));
            var production = new ApiEndpointConfiguration(file, discovery, false, _ => "https://localhost:1/").Resolve();
            Assert.Equal("Production", production.Environment); Assert.Equal("services.example.com", production.Address!.Host);
            var development = new ApiEndpointConfiguration(file, discovery, true, _ => null).Resolve();
            Assert.Equal(52789, development.Address!.Port);
            Assert.Null(ApiEndpointConfiguration.Validate("https://localhost:52789/", false));
            File.WriteAllText(discovery, JsonSerializer.Serialize(new { schemaVersion = 1, apiBaseUrl = "https://localhost:52789/", expiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) }));
            Assert.Equal("services.example.com", new ApiEndpointConfiguration(file, discovery, true, _ => null).Resolve().Address!.Host);
            File.WriteAllText(file, "broken JSON"); Assert.Null(new ApiEndpointConfiguration(file, discovery, false).Resolve().Address);
        }
        finally { Directory.Delete(directory, true); }
    }
}
