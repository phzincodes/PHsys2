using PHsystem.Community;
using PHsystem.Diagnostics;
using PHsystem.Drivers;
using PHsystem.LocalData;
using PHsystem.Optimizer;
using PHsystem.Windows;
using Xunit;

namespace PHsystem.E2ETests;

public sealed class LocalWorkflowTests
{
    [Fact]
    public async Task ScanPersistsAndFeedsOtherModulesWithoutRescanning()
    {
        var directory = Path.Combine(Path.GetTempPath(), "PHsystem-workflow-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var repository = new SnapshotRepository(Path.Combine(directory, "snapshots.db"));
            var store = new SnapshotStore();
            var snapshot = await new ScanCoordinator(WindowsCollectors.Create(), store, repository).ScanAsync();
            Assert.Equal(18, snapshot.Categories.Length);
            Assert.DoesNotContain(snapshot.Categories.Take(6), c => c.State == PHsystem.Core.CollectionState.Error);
            Assert.All(snapshot.Categories, c => Assert.True(c.Facts?.Count > 0 || c.Errors.Length > 0));
            var loaded = await repository.LoadLatestAsync();
            Assert.Equal(snapshot.ScanId, loaded!.ScanId);
            Assert.NotEmpty(new RecommendationService().Evaluate(loaded));
            _ = new DriverInventory().Read(loaded);
            Assert.Contains("Resumo", CommunityDraftBuilder.Create(loaded).Body);
            Assert.Same(snapshot, store.Current);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
