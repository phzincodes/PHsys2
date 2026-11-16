using System.Windows;
using System.Windows.Media;

namespace PHsystem.Desktop.Components;

/// <summary>Vector icon with a stable 24 DIP drawing area and centered ink bounds.</summary>
public sealed class IconControl : FrameworkElement
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(nameof(Data), typeof(Geometry), typeof(IconControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(IconControl),
        new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(IconControl),
        new FrameworkPropertyMetadata(1.6, FrameworkPropertyMetadataOptions.AffectsRender), value => value is double number && double.IsFinite(number) && number > 0 && number <= 8);

    public Geometry? Data { get => (Geometry?)GetValue(DataProperty); set => SetValue(DataProperty, value); }
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

    public IconControl()
    {
        Width = Height = 24;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        UseLayoutRounding = SnapsToDevicePixels = true;
        IsHitTestVisible = false;
        Focusable = false;
    }

    public static Matrix DrawingTransform(Rect ink, Size viewport)
    {
        if (ink.IsEmpty || ink.Width <= 0 || ink.Height <= 0 || viewport.Width <= 0 || viewport.Height <= 0) return Matrix.Identity;
        // Leave one logical pixel around the ink, including the rounded pen caps.
        var scale = Math.Min(viewport.Width / 24, viewport.Height / 24);
        scale *= Math.Min(1, Math.Min(22 / ink.Width, 22 / ink.Height));
        return new Matrix(scale, 0, 0, scale,
            viewport.Width / 2 - (ink.Left + ink.Width / 2) * scale,
            viewport.Height / 2 - (ink.Top + ink.Height / 2) * scale);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Data is null || ActualWidth <= 0 || ActualHeight <= 0) return;
        var pen = new Pen(Stroke, StrokeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        drawingContext.PushTransform(new MatrixTransform(DrawingTransform(Data.GetRenderBounds(pen), RenderSize)));
        drawingContext.DrawGeometry(null, pen, Data);
        drawingContext.Pop();
    }
}
