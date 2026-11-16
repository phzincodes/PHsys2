using System.Collections.Immutable;
using PHsystem.Core;
using PHsystem.Optimizer;
using Xunit;

namespace PHsystem.UnitTests;

public sealed class RecommendationTests
{
    [Fact]
    public void HighMemoryAndLowStorageProduceSpecificRecommendations()
    {
        var snapshot = Snapshot(
            new("Memória", CollectionState.Partial, DateTimeOffset.UtcNow.AddMinutes(5), "test", 1, [],
                new Dictionary<string, string> { ["UsoPercentual"] = "90" }.ToImmutableDictionary()),
            new("Armazenamento", CollectionState.Partial, DateTimeOffset.UtcNow.AddMinutes(5), "test", 1, [],
                new Dictionary<string, string> { ["Volume 1 TotalBytes"] = "100", ["Volume 1 FreeBytes"] = "5" }.ToImmutableDictionary()));
        var items = new RecommendationService().Evaluate(snapshot);
        Assert.Equal(2, items.Count);
        Assert.Contains(items, r => r.Title.Contains("memória"));
        Assert.Contains(items, r => r.Title.Contains("espaço"));
    }

    [Fact]
    public void ExpiredEvidenceDoesNotProduceCurrentHealthAdvice()
    {
        var snapshot = Snapshot(new CategoryResult("Memória", CollectionState.Partial, DateTimeOffset.UtcNow.AddMinutes(-5), "test", 1, [],
            new Dictionary<string, string> { ["UsoPercentual"] = "95" }.ToImmutableDictionary()));
        var item = Assert.Single(new RecommendationService().Evaluate(snapshot));
        Assert.Contains("Atualize", item.Title);
        Assert.True(item.RequiresRefresh);
    }

    [Fact]
    public void MissingSnapshotRequiresEvidence() => Assert.Contains("diagnóstico", Assert.Single(new RecommendationService().Evaluate(null)).Title);
    private static DiagnosticSnapshot Snapshot(params CategoryResult[] categories) => new(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", categories.ToImmutableArray());
}
