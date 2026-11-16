using System.Collections.Immutable;
using PHsystem.Core;
using Xunit;
namespace PHsystem.UnitTests;
public sealed class SnapshotTests
{
    [Fact] public void MissingEvidenceNeverProducesPerfectScore()
    {
        var snapshot = new DiagnosticSnapshot(Guid.NewGuid(), DateTimeOffset.UtcNow, "0.1.0", ImmutableArray<CategoryResult>.Empty);
        Assert.Equal(0, snapshot.Coverage);
        Assert.Null(snapshot.HealthScore);
    }
    [Fact] public void PartialEvidenceIsNotCompleteCoverage()
    {
        var category = new CategoryResult("CPU", CollectionState.Partial, DateTimeOffset.UtcNow, "test", .5, []);
        Assert.Equal(0, new DiagnosticSnapshot(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", [category]).Coverage);
    }
}
