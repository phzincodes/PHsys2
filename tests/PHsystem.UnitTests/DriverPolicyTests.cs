using PHsystem.Drivers;
using Xunit;

namespace PHsystem.UnitTests;

public sealed class DriverPolicyTests
{
    [Theory]
    [InlineData("https://official.example.invalid", true, true, "HARDWARE", true)]
    [InlineData("https://official.example.invalid.attacker.invalid", true, true, "HARDWARE", false)]
    [InlineData("http://official.example.invalid", true, true, "HARDWARE", false)]
    [InlineData("https://official.example.invalid", false, true, "HARDWARE", false)]
    [InlineData("https://official.example.invalid", true, false, "HARDWARE", false)]
    [InlineData("https://official.example.invalid", true, true, "OTHER", false)]
    public void UnverifiedPackagesCannotProceed(string url, bool signature, bool complete, string hardware, bool expected)
    {
        var hash = new string('A', 64);
        var decision = new DriverPackagePolicy(["official.example.invalid"]).Evaluate(new("HARDWARE", "Windows", "x64"),
            new(new Uri(url), hardware, "Windows", "x64", signature, "Test publisher", hash, hash, complete));
        Assert.Equal(expected, decision.CanProceed);
        if (!expected) Assert.NotEmpty(decision.Reasons);
    }
    [Fact]
    public void TamperedDownloadsAreRejected()
    {
        var decision = new DriverPackagePolicy(["official.example.invalid"]).Evaluate(new("HW", "Windows", "x64"),
            new(new Uri("https://official.example.invalid"), "HW", "Windows", "x64", true, "Test", new string('A', 64), new string('B', 64), true));
        Assert.False(decision.CanProceed);
    }
}
