using System.Windows;
using System.Windows.Media;
using PHsystem.Desktop.Components;
using Xunit;

namespace PHsystem.UiTests;

public sealed class IconTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2)]
    public void OffCenterInkRemainsCenteredAndInsideViewport(double scale)
    {
        // Exercise asymmetric geometry, negative coordinates and an oversized source.
        foreach (var ink in new[] { new Rect(-7, 2, 19, 17), new Rect(4, 3, 16, 19), new Rect(-100, 9, 240, 40) })
        {
            var viewport = new Size(24 * scale, 24 * scale);
            var matrix = IconControl.DrawingTransform(ink, viewport);
            var transformed = Rect.Transform(ink, matrix);
            Assert.Equal(viewport.Width / 2, transformed.Left + transformed.Width / 2, 8);
            Assert.Equal(viewport.Height / 2, transformed.Top + transformed.Height / 2, 8);
            Assert.True(transformed.Left >= 0 && transformed.Top >= 0);
            Assert.True(transformed.Right <= viewport.Width && transformed.Bottom <= viewport.Height);
        }
    }
}
