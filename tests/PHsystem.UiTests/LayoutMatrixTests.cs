using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PHsystem.Cleanup;
using PHsystem.Core;
using PHsystem.Desktop;
using PHsystem.Desktop.Components;
using PHsystem.Diagnostics;
using PHsystem.Drivers;
using PHsystem.Optimizer;
using Xunit;

namespace PHsystem.UiTests;

public sealed class LayoutMatrixTests
{
    [Fact]
    public void RealXamlKeepsNavigationInkAtTheSameHorizontalPositionAcrossLayoutMatrix()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            MainWindow? window = null;
            try
            {
                var store = new SnapshotStore(); var repository = new EmptyRepository();
                using var game = new GameModeSession();
                using var client = new CommunityClient(new SessionVault(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "session.bin")));
                var calls = 0;
                var model = new MainViewModel(store, new ScanCoordinator([new UnusedCollector(() => calls++)], store, repository), repository,
                    new TempCleanupService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))), new RecommendationService(), new DriverInventory(), game);
                window = new MainWindow(model, new AuthenticationViewModel(client), client) { ShowActivated = false, ShowInTaskbar = false, Left = -32000, Top = -32000 };
                window.Show(); Pump();
                // This test inspects the shell without making a production authentication bypass.
                // The separate onboarding tests verify that production starts with this shell hidden.
                var shell = (Grid)window.FindName("AppShell"); shell.Visibility = Visibility.Visible;
                ((FrameworkElement)window.FindName("Authentication")).Visibility = Visibility.Collapsed;
                var nav = (ListBox)window.FindName("MainNavigation");
                foreach (var (width, height) in new[] { (1366, 768), (1600, 900), (1920, 1080), (2560, 1440) })
                foreach (var scale in new[] { 1d, 1.25, 1.5, 2d })
                {
                    window.Width = width / scale; window.Height = height / scale;
                    Pump(); window.UpdateLayout();
                    if (!model.SidebarExpanded) model.ToggleSidebarCommand.Execute(null);
                    Pump(); window.UpdateLayout();
                    var expanded = Descendants<IconControl>(nav).Where(i => i.ActualWidth > 0).ToArray();
                    Assert.NotEmpty(expanded);
                    Assert.All(Descendants<System.Windows.Controls.Primitives.ScrollBar>(nav).Where(b => b.IsVisible && b.Orientation == Orientation.Vertical), bar => Assert.True(bar.ActualWidth <= 8.5, $"Navigation scrollbar must stay thin: {bar.ActualWidth} DIP"));
                    var x = expanded[0].TranslatePoint(new Point(expanded[0].ActualWidth / 2, 0), shell).X;
                    model.ToggleSidebarCommand.Execute(null); Pump(); window.UpdateLayout();
                    var compact = Descendants<IconControl>(nav).Where(i => i.ActualWidth > 0).ToArray();
                    Assert.NotEmpty(compact);
                    var compactX = compact[0].TranslatePoint(new Point(compact[0].ActualWidth / 2, 0), shell).X;
                    Assert.True(Math.Abs(x - compactX) <= 0.6, $"{width}×{height} / {scale}: expanded={x}, compact={compactX}");
                    Assert.All(compact, icon => Assert.Equal(compactX, icon.TranslatePoint(new Point(icon.ActualWidth / 2, 0), shell).X, 1));
                    Assert.True(shell.ActualWidth > 0 && shell.ActualHeight > 0);
                }
                Assert.Equal(0, calls);
                var directory = Environment.GetEnvironmentVariable("PHSYSTEM_UI_CAPTURE_DIRECTORY");
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                    window.Width = 1366; window.Height = 768; model.AdaptToWidth(1366);
                    if (!model.SidebarExpanded) model.ToggleSidebarCommand.Execute(null);
                    Pump(); window.UpdateLayout();
                    var root = (FrameworkElement)window.Content;
                    foreach (var element in Descendants<UIElement>(root)) element.InvalidateVisual();
                    root.InvalidateVisual(); Pump(); window.UpdateLayout();
                    var bitmap = new RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(root);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var file = File.Create(Path.Combine(directory, "test-shell-empty-home.png")); encoder.Save(file);
                }
                model.Community.Dispose();
            }
            catch (Exception ex) { failure = ex; }
            finally { window?.Close(); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(45))); Assert.Null(failure);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        { var child = VisualTreeHelper.GetChild(root, index); if (child is T match) yield return match; foreach (var item in Descendants<T>(child)) yield return item; }
    }
    private static void Pump()
    {
        var frame = new DispatcherFrame(); Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false)); Dispatcher.PushFrame(frame);
    }
    private sealed class UnusedCollector(Action called) : IDiagnosticCollector
    { public string Category => "fixture"; public Task<CategoryResult> CollectAsync(CancellationToken cancellationToken) { called(); throw new InvalidOperationException("Navigation cannot scan."); } }
    private sealed class EmptyRepository : ISnapshotRepository
    {
        public Task SaveAsync(DiagnosticSnapshot snapshot, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task<DiagnosticSnapshot?> LoadLatestAsync(CancellationToken cancellationToken = default) => Task.FromResult<DiagnosticSnapshot?>(null);
        public Task<IReadOnlyList<DiagnosticSnapshot>> LoadHistoryAsync(int limit = 20, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DiagnosticSnapshot>>([]);
        public Task DeleteAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
