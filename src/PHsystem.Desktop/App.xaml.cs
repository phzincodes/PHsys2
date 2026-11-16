using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PHsystem.Core;
using PHsystem.Diagnostics;
using PHsystem.LocalData;
using PHsystem.Cleanup;
using PHsystem.Drivers;
using PHsystem.Optimizer;
using PHsystem.Windows;
namespace PHsystem.Desktop;
public partial class App : Application
{
    private ServiceProvider? services;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var collection = new ServiceCollection()
            .AddSingleton<IAssetProvider>(_ => new CatalogAssetProvider(System.IO.Path.Combine(AppContext.BaseDirectory, "assets"),
                Environment.GetEnvironmentVariable("PHSYSTEM_ASSETS_DIRECTORY")))
            .AddSingleton(_ => new UiPreferences(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PHsystemNext", "preferences.json")))
            .AddSingleton<SnapshotStore>()
            .AddSingleton<ISnapshotStore>(s => s.GetRequiredService<SnapshotStore>())
            .AddSingleton<ISnapshotRepository>(_ => new SnapshotRepository(System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PHsystemNext", "snapshots.db")))
            .AddSingleton<ScanCoordinator>()
            .AddSingleton(_ => new TempCleanupService(System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp")))
            .AddSingleton<RecommendationService>().AddSingleton<DriverInventory>().AddSingleton<GameModeSession>()
            .AddSingleton(s => new InterfaceAudio(s.GetRequiredService<IAssetProvider>(), s.GetRequiredService<UiPreferences>(), System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PHsystemNext", "audio-cache")))
            .AddSingleton<IApiEndpointSource>(_ => ApiEndpointConfiguration.Installed()).AddSingleton<CommunityClient>().AddSingleton<ApiConnectionService>().AddSingleton<AuthenticationViewModel>().AddSingleton<CommunityViewModel>().AddSingleton<MainViewModel>().AddSingleton<MainWindow>();
        foreach (var collector in WindowsCollectors.Create()) collection.AddSingleton(collector);
        services = collection.BuildServiceProvider();
        var preferences = services.GetRequiredService<UiPreferences>();
        ThemeManager.Apply(preferences.CurrentTheme);
        preferences.Updated += (_, _) => ThemeManager.Apply(preferences.CurrentTheme);
        services.GetRequiredService<MainWindow>().Show();
    }
    protected override void OnExit(ExitEventArgs e) { services?.Dispose(); base.OnExit(e); }
}
