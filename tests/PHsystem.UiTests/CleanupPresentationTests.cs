using System.IO;
using System.Windows.Threading;
using PHsystem.Cleanup;
using PHsystem.Desktop;
using PHsystem.Diagnostics;
using PHsystem.Drivers;
using PHsystem.LocalData;
using PHsystem.Optimizer;
using Xunit;

namespace PHsystem.UiTests;

public sealed class CleanupPresentationTests
{
    [Fact]
    public void ZeroAndCompletedAreDifferentStatesAndSelectionRequiresConsent()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var directory = Path.Combine(Path.GetTempPath(), "PHsystem-cleanup-ui", Guid.NewGuid().ToString("N"));
            MainWindow? window = null;
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var root = Directory.CreateDirectory(Path.Combine(directory, "sandbox")).FullName;
                File.WriteAllText(Path.Combine(root, "recent.tmp"), "preserve");
                var store = new SnapshotStore(); var repository = new SnapshotRepository(Path.Combine(directory, "data", "snapshots.db"));
                using var game = new GameModeSession();
                using var client = new CommunityClient(new SessionVault(Path.Combine(directory, "session.bin")));
                var connection = new ApiConnectionService(client, new FixedEndpoint(null));
                using var community = new CommunityViewModel(store, client, connection);
                var model = new MainViewModel(store, new ScanCoordinator([new UnusedCollector()], store, repository), repository,
                    new TempCleanupService(root), new RecommendationService(), new DriverInventory(), game, community: community);
                window = new MainWindow(model, new AuthenticationViewModel(client, connection), client) { ShowActivated = false, ShowInTaskbar = false, Left = -32000 };
                void Pump()
                { var frame = new DispatcherFrame(); Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false)); Dispatcher.PushFrame(frame); }
                void Wait()
                { var end = DateTime.UtcNow.AddSeconds(8); do { Pump(); Thread.Sleep(10); } while (model.IsBusy && DateTime.UtcNow < end); Assert.False(model.IsBusy); }
                window.Show(); Wait(); model.OpenCleanupCommand.Execute(null); Pump();
                Assert.Equal(CleanupScreenState.NotAnalyzed, model.CleanupState);
                model.PreviewCleanupCommand.Execute(null); Wait();
                Assert.Equal(CleanupScreenState.NoEligibleItems, model.CleanupState); Assert.Contains("terminou", model.CleanupEmptyMessage);
                Assert.False(model.CanReviewCleanup); Assert.False(model.CleanupCommand.CanExecute(null));
                var old = Path.Combine(root, "eligible.tmp"); File.WriteAllText(old, "fixture"); File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-8));
                model.PreviewCleanupCommand.Execute(null); Wait();
                Assert.Equal(CleanupScreenState.ReadyForReview, model.CleanupState); Assert.False(model.CleanupConsent);
                var item = Assert.Single(model.CleanupFiles); item.Selected = false; model.CleanupConsent = true;
                Assert.False(model.CleanupCommand.CanExecute(null)); item.Selected = true; Assert.True(model.CleanupCommand.CanExecute(null));
                model.CleanupCommand.Execute(null); Wait();
                Assert.Equal(CleanupScreenState.Completed, model.CleanupState); Assert.True(Assert.Single(model.CleanupResults).Removed);
                Assert.False(model.CanReviewCleanup); Assert.False(model.CleanupCommand.CanExecute(null)); Assert.False(File.Exists(old));
                Assert.True(File.Exists(Path.Combine(root, "recent.tmp"))); Assert.Contains("nominal", model.CleanupStatus);
                Assert.Contains("variação", model.CleanupStatus, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) { failure = ex; }
            finally { window?.Close(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(30))); Assert.Null(failure);
    }
    private sealed class UnusedCollector : PHsystem.Core.IDiagnosticCollector
    { public string Category => "fixture"; public Task<PHsystem.Core.CategoryResult> CollectAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Cleanup must not start a hardware scan."); }
}
