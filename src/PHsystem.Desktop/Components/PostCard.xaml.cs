using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace PHsystem.Desktop.Components;
public partial class PostCard : UserControl
{
 public static readonly DependencyProperty UpvoteCommandProperty = DependencyProperty.Register(nameof(UpvoteCommand),typeof(ICommand),typeof(PostCard));
 public static readonly DependencyProperty DownvoteCommandProperty = DependencyProperty.Register(nameof(DownvoteCommand),typeof(ICommand),typeof(PostCard));
 public static readonly DependencyProperty SaveCommandProperty = DependencyProperty.Register(nameof(SaveCommand),typeof(ICommand),typeof(PostCard));
 public static readonly DependencyProperty OpenCommandProperty = DependencyProperty.Register(nameof(OpenCommand),typeof(ICommand),typeof(PostCard));
 public ICommand? UpvoteCommand { get => (ICommand?)GetValue(UpvoteCommandProperty); set => SetValue(UpvoteCommandProperty,value); }
 public ICommand? DownvoteCommand { get => (ICommand?)GetValue(DownvoteCommandProperty); set => SetValue(DownvoteCommandProperty,value); }
 public ICommand? SaveCommand { get => (ICommand?)GetValue(SaveCommandProperty); set => SetValue(SaveCommandProperty,value); }
 public ICommand? OpenCommand { get => (ICommand?)GetValue(OpenCommandProperty); set => SetValue(OpenCommandProperty,value); }
 public PostCard() => InitializeComponent();
}
