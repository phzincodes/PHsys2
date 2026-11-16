using System.Collections.Immutable;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PHsystem.Cleanup;
using PHsystem.Desktop;
using PHsystem.Diagnostics;
using PHsystem.Drivers;
using PHsystem.LocalData;
using PHsystem.Optimizer;
using Xunit;

namespace PHsystem.UiTests;

public sealed class DesktopTests
{
    [Fact]
    public void OfflineFirstOpeningCanScanOnceAndSessionLossDoesNotHideLocalTools()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(System.Windows.Threading.Dispatcher.CurrentDispatcher));
            var directory = Path.Combine(Path.GetTempPath(), "PHsystem-offline-tests", Guid.NewGuid().ToString("N"));
            MainWindow? window = null;
            try
            {
                var store = new SnapshotStore(); var calls = 0;
                using var client = new CommunityClient(new SessionVault(Path.Combine(directory, "session.bin")));
                var connection = new ApiConnectionService(client, new FixedEndpoint(null));
                var account = new AuthenticationViewModel(client, connection);
                var repository = new SnapshotRepository(Path.Combine(directory, "snapshots.db"));
                using var game = new GameModeSession();
                using var community = new CommunityViewModel(store, client, connection);
                var model = new MainViewModel(store, new ScanCoordinator([new Collector(() => calls++)], store, repository), repository,
                    new TempCleanupService(directory), new RecommendationService(), new DriverInventory(), game, community: community);
                window = new MainWindow(model, account, client) { ShowActivated = false, ShowInTaskbar = false, Left = -32000 };
                window.Show(); PumpBindings();
                void WaitReady()
                {
                    var end = DateTime.UtcNow.AddSeconds(8);
                    while (model.IsBusy && DateTime.UtcNow < end) { PumpBindings(); Thread.Sleep(10); }
                    PumpBindings(); Assert.False(model.IsBusy);
                }
                WaitReady(); Assert.False(model.IsAuthenticated);
                Assert.Equal(ApiConnectionState.NotChecked, connection.Current.State);
                Assert.True(model.ScanCommand.CanExecute(null));
                model.ScanCommand.Execute(null); model.ScanCommand.Execute(null); WaitReady();
                Assert.Equal(1, calls); Assert.NotNull(store.Current); Assert.Single(model.History);
                var snapshot = store.Current;
                client.ClearSession(); PumpBindings();
                Assert.Same(snapshot, store.Current);
                Assert.Equal(Visibility.Visible, ((Grid)window.FindName("AppShell")).Visibility);
                Assert.Equal(Visibility.Collapsed, ((FrameworkElement)window.FindName("Authentication")).Visibility);
                model.OpenCommunityCommand.Execute(null); PumpBindings(); Assert.True(model.IsOfflineUser);
                Assert.Equal(ApiConnectionState.NotChecked, connection.Current.State);
            }
            catch (Exception ex) { failure = ex; }
            finally { window?.Close(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(30))); Assert.Null(failure);
    }
    [Fact]
    public async Task SharingStartsPrivateAndUnconfiguredLoginDoesNotAlterSnapshot()
    {
        var store = new SnapshotStore();
        var snapshot = new PHsystem.Core.DiagnosticSnapshot(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", [new("Processador", PHsystem.Core.CollectionState.Partial,
            DateTimeOffset.UtcNow.AddMinutes(1), "test", 1, [], new Dictionary<string, string> { ["Modelo"] = "Simulator CPU", ["serial"] = "private-fixture" }.ToImmutableDictionary())]);
        store.Set(snapshot);
        using var model = new CommunityViewModel(store);
        model.PrepareSharingCommand.Execute(null);
        Assert.Single(model.ShareFields);
        Assert.All(model.ShareFields, item => Assert.False(item.Selected));
        Assert.DoesNotContain(model.ShareFields, item => item.Key == "serial");
        await model.LoginAsync("");
        Assert.Equal(ApiConnectionService.UnavailableMessage, model.Status);
        Assert.Same(snapshot, store.Current);
    }
    [Fact]
    public void WindowLoadsRealXamlAndNavigationDoesNotTriggerCollection()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var directory = Path.Combine(Path.GetTempPath(), "PHsystem-ui-tests", Guid.NewGuid().ToString("N"));
            MainWindow? window = null;
            try
            {
                var calls = 0;
                var store = new SnapshotStore();
                var repository = new SnapshotRepository(Path.Combine(directory, "snapshots.db"));
                var model = new MainViewModel(store, new ScanCoordinator([new Collector(() => calls++)], store, repository),
                    repository, new TempCleanupService(directory), new RecommendationService(), new DriverInventory(), new GameModeSession(), new CorruptPngProvider());
                Assert.Null(model.Logo);
                window = new MainWindow(model);
                Assert.Equal(Color.FromRgb(7, 8, 11), Assert.IsType<SolidColorBrush>(window.Background).Color);
                Assert.Equal(Color.FromRgb(243, 244, 246), Assert.IsType<SolidColorBrush>(window.Foreground).Color);
                window.Measure(new Size(1280, 800));
                window.Arrange(new Rect(0, 0, 1280, 800));
                window.UpdateLayout();
                PumpBindings();
                var tabs = Assert.IsAssignableFrom<TabControl>(window.FindName("PageHost"));
                var navigation = Assert.IsType<ListBox>(window.FindName("MainNavigation"));
                Assert.Equal(Visibility.Visible, Assert.IsType<Grid>(window.FindName("AppShell")).Visibility);
                Assert.Equal(Visibility.Collapsed, Assert.IsType<PHsystem.Desktop.Views.AuthenticationView>(window.FindName("Authentication")).Visibility);
                Assert.Equal(14, tabs.Items.Count);
                Assert.Equal(14, navigation.Items.Count);
                for (var i = 0; i < tabs.Items.Count; i++)
                {
                    model.SelectedPageIndex = i;
                    PumpBindings();
                    window.UpdateLayout();
                    Assert.Equal(i, tabs.SelectedIndex);
                    Assert.Same(model.Navigation[i], navigation.SelectedItem);
                    Assert.Equal(model.Navigation[i].Title, model.PageTitle);
                }
                model.OpenDiagnosticsCommand.Execute(null);
                Assert.Equal(1, tabs.SelectedIndex);
                model.ToggleSidebarCommand.Execute(null);
                Assert.False(model.SidebarExpanded);
                Assert.Equal(68, model.SidebarWidth.Value);
                model.ToggleSidebarCommand.Execute(null);
                model.AdaptToWidth(960);
                Assert.False(model.SidebarExpanded);
                Assert.Equal(0, calls);
                Assert.False(model.CleanupCommand.CanExecute(null));
                Assert.False(model.ExportCommand.CanExecute(null));
                Assert.True(model.ScanCommand.CanExecute(null));
            }
            catch (Exception ex) { failure = ex; }
            finally
            {
                window?.Close();
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A thread WPF não concluiu.");
        Assert.Null(failure);
    }

    private static T? Find<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T match) return match;
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            if (Find<T>(child) is { } found) return found;
        return null;
    }
    private static void PumpBindings()
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => frame.Continue = false));
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }

    private sealed class Collector(Action called) : PHsystem.Core.IDiagnosticCollector
    {
        public string Category => "test";
        public Task<PHsystem.Core.CategoryResult> CollectAsync(CancellationToken cancellationToken)
        {
            called();
            return Task.FromResult(new PHsystem.Core.CategoryResult("test", PHsystem.Core.CollectionState.Partial, DateTimeOffset.UtcNow, "test", 1, []));
        }
    }
    private sealed class CorruptPngProvider : PHsystem.Core.IAssetProvider
    {
        public PHsystem.Core.AssetResult Get(string path)
        {
            byte[] bytes = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0, 0, 0, 0, 0];
            return new(PHsystem.Core.AssetState.Available, "neutro", bytes.ToImmutableArray());
        }
        public void ClearCache() { }
    }

    [Fact]
    public async Task DashboardAndCategoryFilterUseTheExistingSnapshotWithoutRescanning()
    {
        var snapshot = new PHsystem.Core.DiagnosticSnapshot(Guid.NewGuid(), DateTimeOffset.UtcNow, "test",
        [
            new("Processador", PHsystem.Core.CollectionState.Partial, DateTimeOffset.UtcNow.AddMinutes(5), "test", 1, [], new Dictionary<string,string>{["Modelo"]="Fixture CPU",["Processadores lógicos"]="8"}.ToImmutableDictionary()),
            new("Memória", PHsystem.Core.CollectionState.Partial, DateTimeOffset.UtcNow.AddMinutes(5), "test", 1, [], new Dictionary<string,string>{["TotalBytes"]="17179869184",["AvailableBytes"]="4294967296",["UsoPercentual"]="75"}.ToImmutableDictionary()),
            new("Armazenamento", PHsystem.Core.CollectionState.Partial, DateTimeOffset.UtcNow.AddMinutes(5), "test", 1, [], new Dictionary<string,string>{["Volume 1 TotalBytes"]="1000",["Volume 1 FreeBytes"]="250"}.ToImmutableDictionary())
        ]);
        var store = new SnapshotStore(); var repository = new FixtureRepository(snapshot); var calls = 0;
        using var game = new GameModeSession();
        var model = new MainViewModel(store, new ScanCoordinator([new Collector(()=>calls++)],store,repository), repository,
            new TempCleanupService(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"))),new RecommendationService(),new DriverInventory(),game);
        await model.InitializeAsync();
        Assert.Equal("Fixture CPU", model.CpuModel);
        Assert.Equal(75, model.MemoryUsedPercent);
        Assert.Equal(3, model.AvailableCategoryCount);
        Assert.Equal(75, Assert.Single(model.Volumes).UsedPercent);
        model.CategoryFilter = "memória";
        Assert.Equal("Memória", Assert.Single(model.FilteredCategories).Name);
        Assert.Equal("Memória", model.SelectedCategory!.Name);
        model.CategoryFilter = "sem correspondência";
        Assert.Empty(model.FilteredCategories);
        Assert.Null(model.SelectedCategory);
        model.CategoryFilter = "";
        Assert.Equal(3, model.FilteredCategories.Count);
        Assert.Same(snapshot, store.Current);
        Assert.Equal(0, calls);
        Assert.Null(snapshot.HealthScore);
        model.OpenSharingCommand.Execute(null);
        Assert.Equal(8, model.SelectedPageIndex);
        Assert.Equal(3, model.CommunityTabIndex);
        Assert.Equal(0, calls);
        model.Community.Dispose();
    }
    private sealed class FixtureRepository(PHsystem.Core.DiagnosticSnapshot snapshot) : PHsystem.Core.ISnapshotRepository
    {
        public Task SaveAsync(PHsystem.Core.DiagnosticSnapshot value, CancellationToken cancellationToken=default) => throw new InvalidOperationException("A navegação não deve criar uma coleta.");
        public Task<PHsystem.Core.DiagnosticSnapshot?> LoadLatestAsync(CancellationToken cancellationToken=default) => Task.FromResult<PHsystem.Core.DiagnosticSnapshot?>(snapshot);
        public Task<IReadOnlyList<PHsystem.Core.DiagnosticSnapshot>> LoadHistoryAsync(int limit=20,CancellationToken cancellationToken=default) => Task.FromResult<IReadOnlyList<PHsystem.Core.DiagnosticSnapshot>>([snapshot]);
        public Task DeleteAllAsync(CancellationToken cancellationToken=default) => Task.CompletedTask;
    }
}
