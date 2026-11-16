using System.Windows;
using System.Windows.Controls;
namespace PHsystem.Desktop.Views;
public partial class CommunityView : UserControl
{
 public CommunityView() { InitializeComponent(); IsVisibleChanged += (_, _) => { if (!IsVisible) AccountPassword.Clear(); }; }
 private async void LoginClick(object sender, RoutedEventArgs e) { if(DataContext is MainViewModel m) await m.Community.LoginAsync(AccountPassword.Password); AccountPassword.Clear(); }
 private async void RegisterClick(object sender, RoutedEventArgs e) { if(DataContext is MainViewModel m) await m.Community.RegisterAsync(AccountPassword.Password); AccountPassword.Clear(); }
 private async void ResetPasswordClick(object sender, RoutedEventArgs e) { if(DataContext is MainViewModel m) await m.Community.ResetPasswordAsync(AccountPassword.Password); AccountPassword.Clear(); }
 private async void DeleteAccountClick(object sender, RoutedEventArgs e) { if(DataContext is MainViewModel m) await m.Community.DeleteAccountAsync(AccountPassword.Password); AccountPassword.Clear(); }
 private void AccountPasswordChanged(object sender, RoutedEventArgs e) { if(DataContext is MainViewModel m) m.Community.ValidatePassword(AccountPassword.Password); }
 private void ViewUnloaded(object sender, RoutedEventArgs e) => AccountPassword.Clear();
}
