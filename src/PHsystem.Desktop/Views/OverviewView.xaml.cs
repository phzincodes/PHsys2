using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
namespace PHsystem.Desktop.Views;
public partial class OverviewView : UserControl
{
 private MainViewModel? model;
 private Window? host;
 public OverviewView() { InitializeComponent(); Loaded += Load; Unloaded += Unload; IsVisibleChanged += (_, _) => UpdateMotion(); }
 private void Load(object sender, RoutedEventArgs e)
 {
  model = DataContext as MainViewModel; host = Window.GetWindow(this);
  if (model is not null) { model.PropertyChanged += ModelChanged; model.Preferences.Updated += PreferenceChanged; }
  if (host is not null) { host.Activated += PreferenceChanged; host.Deactivated += PreferenceChanged; }
  SystemParameters.StaticPropertyChanged += ModelChanged;
  UpdateMotion();
 }
 private void Unload(object sender, RoutedEventArgs e)
 {
  if (model is not null) { model.PropertyChanged -= ModelChanged; model.Preferences.Updated -= PreferenceChanged; }
  if (host is not null) { host.Activated -= PreferenceChanged; host.Deactivated -= PreferenceChanged; }
  SystemParameters.StaticPropertyChanged -= ModelChanged;
  model = null; host = null; ScanRotation.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);
 }
 private void ModelChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName is nameof(MainViewModel.IsScanning) or nameof(SystemParameters.ClientAreaAnimation)) UpdateMotion(); }
 private void PreferenceChanged(object? sender, EventArgs e) => UpdateMotion();
 private void UpdateMotion()
 {
  if (ScanRotation is null) return;
  ScanRotation.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);
  ScanRotation.Angle = 0;
  if (model?.IsScanning == true && model.Preferences.AnimationsEnabled && Components.Motion.Allowed(this))
   ScanRotation.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, new DoubleAnimation(0,360,TimeSpan.FromSeconds(2)) { RepeatBehavior = RepeatBehavior.Forever });
 }
}
