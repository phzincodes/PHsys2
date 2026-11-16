using PHsystem.Community;
using Xunit;
namespace PHsystem.SecurityTests;
public sealed class HardwareSharingTests
{
    [Theory]
    [InlineData(@"C:\Users\Private\file.txt")]
    [InlineData("/home/private/file")]
    [InlineData("person@example.invalid")]
    [InlineData("address 192.0.2.1")]
    [InlineData("00:11:22:33:44:55")]
    [InlineData("2001:db8::1")]
    [InlineData("123e4567-e89b-12d3-a456-426614174000")]
    [InlineData("token=fixture-token")]
    [InlineData("https://example.invalid")]
    [InlineData("<script>")]
    public void SensitiveValuesAreRejectedBeforeSharing(string value) => Assert.False(HardwareSharingPolicy.SafeValue(value));
    [Theory]
    [InlineData("Intel Core i7-14700K")]
    [InlineData("NVIDIA GeForce RTX 4070")]
    [InlineData("17179869184")]
    [InlineData("Microsoft Windows 11")]
    public void TechnicalModelsRemainEligibleForExplicitOptIn(string value) => Assert.True(HardwareSharingPolicy.SafeValue(value));
}
