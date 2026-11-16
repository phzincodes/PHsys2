using System.Collections.Immutable;
using PHsystem.Core;
using PHsystem.LocalData;
using Xunit;

namespace PHsystem.IntegrationTests;

public sealed class SnapshotRepositoryTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "PHsystem-repository-tests", Guid.NewGuid().ToString("N"));
    private string Database => Path.Combine(directory, "snapshots.db");

    [Fact]
    public async Task SnapshotRoundTripsAcrossRepositoryInstancesAndCanBeDeleted()
    {
        var snapshot = CreateSnapshot(0);
        await new SnapshotRepository(Database).SaveAsync(snapshot);
        var repository = new SnapshotRepository(Database);
        var loaded = await repository.LoadLatestAsync();
        Assert.NotNull(loaded);
        Assert.Equal(snapshot.ScanId, loaded.ScanId);
        Assert.Equal("42", Assert.Single(loaded.Categories).Facts!["value"]);
        Assert.Null(loaded.HealthScore);
        await repository.DeleteAllAsync();
        Assert.Null(await new SnapshotRepository(Database).LoadLatestAsync());
    }

    [Fact]
    public async Task RetentionKeepsNewestOneHundredSnapshots()
    {
        var repository = new SnapshotRepository(Database);
        for (var i = 0; i < 102; i++) await repository.SaveAsync(CreateSnapshot(i));
        var history = await repository.LoadHistoryAsync(100);
        Assert.Equal(100, history.Count);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddMinutes(101), history[0].CapturedAt);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddMinutes(2), history[^1].CapturedAt);
    }

    [Fact]
    public async Task DuplicateScanIdDoesNotReplaceExistingSnapshot()
    {
        var repository = new SnapshotRepository(Database);
        var snapshot = CreateSnapshot(1);
        await repository.SaveAsync(snapshot);
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() => repository.SaveAsync(snapshot));
        Assert.Single(await repository.LoadHistoryAsync());
    }

    private static DiagnosticSnapshot CreateSnapshot(int minute) => new(Guid.NewGuid(), DateTimeOffset.UnixEpoch.AddMinutes(minute), "test",
        [new("Test", CollectionState.Partial, DateTimeOffset.UtcNow, "local", .5, [], new Dictionary<string, string> { ["value"] = "42" }.ToImmutableDictionary())]);
    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
