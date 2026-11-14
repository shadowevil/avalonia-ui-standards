using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// The rim's two methods: the edge strip (the default) and the backdrop-filter rim (inside effect
/// and opacity-mask layers). They look the same, the switch follows the layers, a strip wrapped
/// onto several rows changes nothing, and nothing is rasterised per size.
/// </summary>
public class GlassRimMethodTests
{
    // Nested glass (a panel holding four cards) over the colourful backdrop, light and dark, at 1x
    // and 2x: over the rim's pixels a mean difference under 1.5 levels, and no pixel more than 40
    // levels off (the strip carries light along the edge itself, the filters across a square that
    // reaches over a corner to the next edge, so a few catches within 20 px of a corner differ).
    [AvaloniaTheory]
    [InlineData("Light", 1.0)]
    [InlineData("Light", 2.0)]
    [InlineData("Dark", 1.0)]
    [InlineData("Dark", 2.0)]
    public void StripRim_LooksLikeTheFilterRim(string theme, double scale)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        using var filter = Render(variant, scale, g => g.SetValue(GlassBorder.RimMethodProperty, RimMethod.Filter));
        using var strip = Render(variant, scale, g => g.SetValue(GlassBorder.RimMethodProperty, RimMethod.Strip));
        using var bare = Render(variant, scale, g => g.SetValue(GlassBorder.RimAbsorptionProperty, 0d));
        var (mean, max) = Compare(filter, strip, bare);
        Assert.True(mean < 1.5, $"mean difference over the rim {mean:F2}");
        Assert.True(max <= 40, $"max difference {max}");
    }

    // Inside a translucent panel the strip rim takes the opacity as the filter rim does.
    [AvaloniaFact]
    public void StripRim_TakesTheOpacity()
    {
        using var filter = Render(ThemeVariant.Dark, 1, g => g.SetValue(GlassBorder.RimMethodProperty, RimMethod.Filter), opacity: 0.5);
        using var strip = Render(ThemeVariant.Dark, 1, g => g.SetValue(GlassBorder.RimMethodProperty, RimMethod.Strip), opacity: 0.5);
        using var bare = Render(ThemeVariant.Dark, 1, g => g.SetValue(GlassBorder.RimAbsorptionProperty, 0d), opacity: 0.5);
        var (mean, max) = Compare(filter, strip, bare);
        Assert.True(mean < 1.5 && max <= 40, $"mean {mean:F2}, max {max}");
    }

    [AvaloniaFact]
    public void EffectOrOpacityMask_OnAnAncestor_SwitchesToTheFilterRim()
    {
        var glass = new GlassBorder { Width = 200, Height = 120 };
        var host = new Border { Child = glass };
        var window = new Window { Width = 300, Height = 200, Content = new Panel { Children = { Backdrop.Colourful(), host } } };
        window.Show();
        Assert.True(glass.RimReadsSurface);

        host.Effect = new DropShadowEffect();
        Assert.False(glass.RimReadsSurface);
        host.Effect = null;
        Assert.True(glass.RimReadsSurface);

        host.OpacityMask = Brushes.Black;
        Assert.False(glass.RimReadsSurface);
        host.OpacityMask = null;
        Assert.True(glass.RimReadsSurface);
        window.Close();
    }

    // Under a drop shadow the rim is the filter rim's exactly (the strip would read the surface
    // without the layer's content).
    [AvaloniaFact]
    public void UnderAnEffect_TheRimIsTheFilterRim()
    {
        using var auto = Render(ThemeVariant.Dark, 1, _ => { }, effect: true);
        using var filter = Render(ThemeVariant.Dark, 1, g => g.SetValue(GlassBorder.RimMethodProperty, RimMethod.Filter), effect: true);
        var (mean, max) = Compare(filter, auto, filter);
        Assert.Equal(0, max);
        Assert.Equal(0, mean);
    }

    // The strip wrapped onto rows of 64 texels draws the same rim as on one row.
    [AvaloniaTheory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void WrappedStrip_DrawsTheSameRim(double scale)
    {
        using var one = Render(ThemeVariant.Dark, scale, g => g.SetValue(GlassBorder.RimMethodProperty, RimMethod.Strip));
        using var rows = Render(ThemeVariant.Dark, scale, g =>
        {
            g.SetValue(GlassBorder.RimMethodProperty, RimMethod.Strip);
            g.SetValue(GlassBorder.RimStripRowProperty, 64);
        });
        var (_, max) = Compare(one, rows, one);
        Assert.True(max <= 1, $"max difference {max}");
    }

    // A full-window panel at 2x: preparing either method for a new size takes milliseconds, with no
    // rasterised masks (the filter rim's geometry is a shader).
    [AvaloniaFact]
    public void NewSize_PreparesWithoutRasterising()
    {
        static GlassRimDrawOperation.Parameters P(double w, double h) =>
            new(new Rect(0, 0, w, h), new CornerRadius(16), 0.6, 1, 0.6, 1, 12, 0.5, 4, Colors.White);
        GlassRimFilter.Create(P(300, 200), 2)?.Dispose();
        GlassRimStrip.Create(P(300, 200), 2)?.Dispose();
        var watch = Stopwatch.StartNew();
        for (var i = 0; i < 6; i++)
        {
            GlassRimFilter.Create(P(1392 - 20 * i, 803 - 10 * i), 2)!.Dispose();
            GlassRimStrip.Create(P(1392 - 20 * i, 803 - 10 * i), 2)!.Dispose();
        }
        Assert.True(watch.Elapsed.TotalMilliseconds < 6 * 30, $"6 sizes took {watch.Elapsed.TotalMilliseconds:F0} ms");
    }

    // A glass panel holding four glass cards over the colourful backdrop.
    private static Frame Render(ThemeVariant variant, double scale, Action<GlassBorder> setup, double opacity = 1, bool effect = false)
    {
        var cards = new WrapPanel { Margin = new Thickness(12) };
        for (var i = 0; i < 4; i++)
            cards.Children.Add(new GlassBorder { Width = 150, Height = 100, CornerRadius = new CornerRadius(12), Margin = new Thickness(0, 0, 12, 12) });
        var panel = new GlassBorder { CornerRadius = new CornerRadius(16), Margin = new Thickness(20), Child = cards, Opacity = opacity };
        if (effect)
            panel.Effect = new DropShadowEffect { BlurRadius = 8, Opacity = 0.5 };
        foreach (var g in new[] { panel }.Concat(cards.Children.OfType<GlassBorder>()))
            setup(g);
        var window = new Window { Width = 380, Height = 300, RequestedThemeVariant = variant, Content = new Panel { Children = { Backdrop.Colourful(), panel } } };
        window.SetRenderScaling(scale);
        var frame = Frame.Capture(window);
        window.Close();
        return frame;
    }

    // The mean difference over the rim's pixels (where the frame without a rim differs) and the max overall.
    private static (double Mean, int Max) Compare(Frame a, Frame b, Frame bare)
    {
        double sum = 0;
        int max = 0, rim = 0;
        for (var y = 0; y < a.Height; y++)
            for (var x = 0; x < a.Width; x++)
            {
                var d = Diff(a[x, y], b[x, y]);
                max = Math.Max(max, d);
                if (Diff(a[x, y], bare[x, y]) > 0 || ReferenceEquals(a, bare))
                {
                    sum += d;
                    rim++;
                }
            }
        return (sum / Math.Max(rim, 1), max);
    }

    private static int Diff((byte R, byte G, byte B) p, (byte R, byte G, byte B) q) =>
        Math.Max(Math.Abs(p.R - q.R), Math.Max(Math.Abs(p.G - q.G), Math.Abs(p.B - q.B)));
}
