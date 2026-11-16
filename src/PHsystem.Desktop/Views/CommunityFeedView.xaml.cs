using System.Windows;
using System.Windows.Controls;
namespace PHsystem.Desktop.Views;
public partial class CommunityFeedView : UserControl
{
 public CommunityFeedView() { InitializeComponent(); IsVisibleChanged += (_, _) => TryLoad(); }
 private void FeedLoaded(object sender, RoutedEventArgs e) => TryLoad();
 private void TryLoad() { if (IsVisible && DataContext is CommunityViewModel { FeedLoaded: false, IsReady: true } model) model.LoadFeedCommand.Execute(null); }
}
