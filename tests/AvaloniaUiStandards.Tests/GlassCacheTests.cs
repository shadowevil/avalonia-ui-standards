using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// The rim's geometry is rasterised once and reused across frames: nested glass looks the same as
/// the reference, which draws that geometry again on every frame.
/// </summary>
public class GlassCacheTests
{
    // A glass panel holding four nested glass cards, over the colourful backdrop, at 1x and 2x:
    // a mean difference under 0.01 levels and no pixel more than 4 levels off (a few antialiased rim pixels round differently).
    [AvaloniaTheory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void NestedGlass_WithCachedRimGeometry_LooksLikeTheReference(double renderScale)
    {
        using var reference = Render(cache: false, renderScale);
        using var cached = Render(cache: true, renderScale);
        double sum = 0;
        var max = 0;
        for (var y = 0; y < reference.Height; y++)
            for (var x = 0; x < reference.Width; x++)
            {
                var (r0, g0, b0) = reference[x, y];
                var (r1, g1, b1) = cached[x, y];
                var d = Math.Max(Math.Abs(r0 - r1), Math.Max(Math.Abs(g0 - g1), Math.Abs(b0 - b1)));
                sum += d;
                max = Math.Max(max, d);
            }
        var pixels = reference.Width * reference.Height;
        Assert.True(sum / pixels < 0.01, $"mean difference {sum / pixels:F4}");
        Assert.True(max <= 4, $"max difference {max}");
    }

    private static Frame Render(bool cache, double renderScale)
    {
        var cards = new WrapPanel { Margin = new Thickness(12) };
        for (var i = 0; i < 4; i++)
            cards.Children.Add(new GlassBorder { Width = 150, Height = 100, CornerRadius = new CornerRadius(12), Margin = new Thickness(0, 0, 12, 12) });
        var panel = new GlassBorder { CornerRadius = new CornerRadius(16), Margin = new Thickness(20), Child = cards };
        var window = new Window { Width = 380, Height = 300, Content = new Panel { Children = { Backdrop.Colourful(), panel } } };
        window.SetRenderScaling(renderScale);
        window.Show();
        foreach (var glass in new[] { panel }.Concat(cards.Children.OfType<GlassBorder>()))
            glass.SetValue(GlassBorder.RimCacheGeometryProperty, cache);
        var frame = Frame.Capture(window);
        window.Close();
        return frame;
    }
}
