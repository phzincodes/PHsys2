using PHsystem.Core;
using PHsystem.Windows;
using Xunit;

namespace PHsystem.WindowsTests;

public sealed class CollectorTests
{
    [Fact]
    public async Task NativeCollectorsReturnScopedEvidenceWithoutMachineIdentifiers()
    {
        var collectors = WindowsCollectors.Create().ToArray();
        Assert.Equal(18, collectors.Length);
        foreach (var collector in collectors.Take(6))
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var result = await collector.CollectAsync(timeout.Token);
            Assert.Equal(collector.Category, result.Category);
            Assert.Equal(CollectionState.Partial, result.State);
            Assert.NotEmpty(result.Facts!);
            Assert.Empty(result.Errors);
            Assert.DoesNotContain(result.Facts!.Keys, key => key.Contains("Serial", StringComparison.OrdinalIgnoreCase)
                || key.Contains("MAC", StringComparison.OrdinalIgnoreCase) || key.Contains("MachineName", StringComparison.OrdinalIgnoreCase));
            if (result.Category == "Memória") Assert.True(ulong.Parse(result.Facts["TotalBytes"]) > 0);
        }
    }
}
