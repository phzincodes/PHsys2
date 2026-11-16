using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Data;
namespace PHsystem.Desktop;
public partial class MainWindow : Window
{
 private bool initialized;
 private readonly System.Windows.Threading.DispatcherTimer sessionTimer = new() { Interval = TimeSpan.FromSeconds(30) };
 public MainWindow(MainViewModel model, AuthenticationViewModel? authentication = null, CommunityClient? communityClient = null, InterfaceAudio? audio = null)
 {
  InitializeComponent();
  DataContext = model;
  AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler((_, _) => audio?.Play(InterfaceSound.Click)));
  model.PropertyChanged += (_, change) =>
  {
   if (change.PropertyName == nameof(MainViewModel.SelectedPageIndex)) { Components.Motion.FadeIn(PageHost); audio?.Play(InterfaceSound.Navigation); }
   if (change.PropertyName == nameof(MainViewModel.IsScanning) && model.IsScanning) audio?.Play(InterfaceSound.ScanStart);
   if (change.PropertyName == nameof(MainViewModel.ScanState) && model.ScanState == "Análise concluída") audio?.Play(InterfaceSound.ScanComplete);
   if (change.PropertyName == nameof(MainViewModel.ScanState) && model.ScanState == "Erro na análise") audio?.Play(InterfaceSound.Error);
  };
  var client = communityClient ?? new CommunityClient();
  var account = authentication ?? new AuthenticationViewModel(client);
  sessionTimer.Tick += (_, _) => { if (client.SessionExpiresAt is { } expires && expires <= DateTimeOffset.UtcNow) client.ClearSession(); };
  sessionTimer.Start(); Closed += (_, _) => sessionTimer.Stop();
  Authentication.DataContext = account;
  model.OnlineAccessRequested += async (_, _) => { account.Page = AuthenticationPage.Welcome; Authentication.Visibility = Visibility.Visible; await account.RestoreAsync(); };
  account.CloseRequested += (_, _) => Authentication.Visibility = Visibility.Collapsed;
  account.Authenticated += async (_, _) =>
  {
   Authentication.Visibility = Visibility.Collapsed;
   AppShell.Visibility = Visibility.Visible;
   model.SetOnlineIdentity(client.Username);
   Components.Motion.FadeIn(AppShell); audio?.Play(InterfaceSound.Confirm);
   if (!initialized) { initialized = true; await model.InitializeAsync(); }
  };
  client.SessionEnded += (_, _) => Dispatcher.Invoke(() =>
  {
   model.SetOnlineIdentity(null);
   Authentication.Visibility = Visibility.Collapsed;
   account.Page = AuthenticationPage.Login;
  });
  ((CollectionViewSource)Resources["NavigationSource"]).Source = model.Navigation;
  MainNavigation.SelectedItem = model.SelectedNavigationItem;
 }
 private async void WindowLoaded(object sender, RoutedEventArgs e)
 {
  if (!initialized && DataContext is MainViewModel model) { initialized = true; await model.InitializeAsync(); }
  if (Authentication.DataContext is AuthenticationViewModel account) await account.RestoreRememberedOnlyAsync();
 }
 private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
 {
  if (DataContext is MainViewModel model) model.AdaptToWidth(e.NewSize.Width);
 }
 private void WindowSourceInitialized(object? sender, EventArgs e)
 {
  if (SystemParameters.HighContrast) return;
  var enabled = 1;
  _ = DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref enabled, sizeof(int));
  var caption = 0x002B1A0D;
  var text = 0x00F4EDE8;
  _ = DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 35, ref caption, sizeof(int));
  _ = DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 36, ref text, sizeof(int));
 }
 [DllImport("dwmapi.dll")]
 private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
