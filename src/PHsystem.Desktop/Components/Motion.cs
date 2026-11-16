using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PHsystem.Desktop.Components;

public static class Motion
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(Motion), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, (_, _) => { }));
    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);
    public static readonly DependencyProperty InteractiveProperty = DependencyProperty.RegisterAttached("Interactive", typeof(bool), typeof(Motion), new PropertyMetadata(false, InteractiveChanged));
    public static bool GetInteractive(DependencyObject element) => (bool)element.GetValue(InteractiveProperty);
    public static void SetInteractive(DependencyObject element, bool value) => element.SetValue(InteractiveProperty, value);
    public static bool Allowed(FrameworkElement element) => GetEnabled(element) && SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast && element.IsVisible && Window.GetWindow(element)?.IsActive != false;

    private static void InteractiveChanged(DependencyObject target, DependencyPropertyChangedEventArgs change)
    {
        if (target is not FrameworkElement element) return;
        if ((bool)change.NewValue)
        { element.MouseEnter += Enter; element.MouseLeave += Leave; element.PreviewMouseLeftButtonDown += Press; element.PreviewMouseLeftButtonUp += Release; element.Unloaded += Unload; }
        else
        { element.MouseEnter -= Enter; element.MouseLeave -= Leave; element.PreviewMouseLeftButtonDown -= Press; element.PreviewMouseLeftButtonUp -= Release; element.Unloaded -= Unload; }
    }
    private static void Animate(FrameworkElement element, double opacity, int milliseconds)
    {
        element.BeginAnimation(UIElement.OpacityProperty, null);
        if (!Allowed(element)) { element.Opacity = 1; return; }
        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(element.Opacity, opacity, TimeSpan.FromMilliseconds(milliseconds)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
    }
    private static void Enter(object sender, MouseEventArgs e) => Animate((FrameworkElement)sender, 0.9, 120);
    private static void Leave(object sender, MouseEventArgs e) => Animate((FrameworkElement)sender, 1, 120);
    private static void Press(object sender, MouseButtonEventArgs e) => Animate((FrameworkElement)sender, 0.72, 80);
    private static void Release(object sender, MouseButtonEventArgs e) => Animate((FrameworkElement)sender, 0.9, 90);
    private static void Unload(object sender, RoutedEventArgs e) { var element = (FrameworkElement)sender; element.BeginAnimation(UIElement.OpacityProperty, null); element.Opacity = 1; }
    public static void FadeIn(FrameworkElement element)
    {
        element.BeginAnimation(UIElement.OpacityProperty, null); element.Opacity = 1;
        if (Allowed(element)) element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(200)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
    }
}
