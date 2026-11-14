using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
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
    // and 2x: over the rim's pixels a mean difference under 1.5 levels. More than 20 px from every
    // corner no pixel is more than FarBound levels off. Within 20 px of a corner a few catches
    // differ by up to 40 levels: the strip carries light along the edge itself, the filters across
    // a square that reaches over the corner to the next edge.
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
        var (mean, far, near) = Compare(filter, strip, bare, scale);
        Assert.True(mean < 1.5, $"mean difference over the rim {mean:F2}");
        Assert.True(far <= FarBound, $"max difference away from the corners {far}");
        Assert.True(near <= 40, $"max difference near the corners {near}");
    }

    // The largest difference measured away from the corners (20, dark at 2x) plus a margin.
    private const int FarBound = 24;

    // Inside a translucent panel the strip rim takes the opacity as the filter rim does.
    [AvaloniaFact]
    public void StripRim_TakesTheOpacity()
    {
        using var filter = Render(ThemeVariant.Dark, 1, g => g.SetValue(GlassBorder.RimMethodProperty, RimMethod.Filter), opacity: 0.5);
        using var strip = Render(ThemeVariant.Dark, 1, g => g.SetValue(GlassBorder.RimMethodProperty, RimMethod.Strip), opacity: 0.5);
        using var bare = Render(ThemeVariant.Dark, 1, g => g.SetValue(GlassBorder.RimAbsorptionProperty, 0d), opacity: 0.5);
        var (mean, far, near) = Compare(filter, strip, bare, 1);
        Assert.True(mean < 1.5 && far <= FarBound && near <= 40, $"mean {mean:F2}, max away from the corners {far}, near them {near}");
    }

    // With SkiaOptions.UseOpacitySaveLayer, an opacity below 1 draws into a layer too.
    [AvaloniaFact]
    public void WithOpacityLayers_ATranslucentAncestor_SwitchesToTheFilterRim()
    {
        var glass = new GlassBorder { Width = 200, Height = 120 };
        var host = new Border { Child = glass, Opacity = 0.5 };
        var window = new Window { Width = 300, Height = 200, Content = new Panel { Children = { Backdrop.Colourful(), host } } };
        window.Show();
        try
        {
            Assert.True(glass.RimReadsSurface);
            GlassBorder.OpacityLayers = true;
            Assert.False(glass.RimReadsSurface);
            host.Opacity = 1;
            Assert.True(glass.RimReadsSurface);
            glass.Opacity = 0.9;
            Assert.False(glass.RimReadsSurface);
        }
        finally
        {
            GlassBorder.OpacityLayers = false;
            window.Close();
        }
    }

    // With opacity layers, a panel turning translucent redraws its rims as the filter rim.
    [AvaloniaFact]
    public void WithOpacityLayers_TurningTranslucent_RedrawsTheFilterRim()
    {
        // Each frame: drawn at full opacity, then redrawn after the panel turns translucent.
        static Frame Turn(Action<GlassBorder> setup)
        {
            var window = Window(ThemeVariant.Dark, 1, setup, 1, false, out var panel);
            using (Frame.Capture(window)) { }
            panel.Opacity = 0.5;
            var frame = Frame.Capture(window);
            window.Close();
            return frame;
        }
        using var filter = Turn(g => g.SetValue(GlassBorder.RimMethodProperty, RimMethod.Filter));
        using var strip = Turn(_ => { });
        GlassBorder.OpacityLayers = true;
        try
        {
            using var turned = Turn(_ => { });
            var (mean, far, near) = Compare(filter, turned, filter, 1);
            Assert.Equal(0, Math.Max(far, near));
            Assert.Equal(0, mean);
            // Without opacity layers the strip stays, so the comparison above shows the switch.
            Assert.True(Compare(filter, strip, filter, 1).Mean > 0);
        }
        finally
        {
            GlassBorder.OpacityLayers = false;
        }
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
        var (mean, far, near) = Compare(filter, auto, filter, 1);
        Assert.Equal(0, Math.Max(far, near));
        Assert.Equal(0, mean);
    }

    // A white bar drawn above the glass covers the points just outside its top edge (not the
    // rim). A later redraw of a small region next to it reads only the points inside that region:
    // outside it the surface holds the last frame, bar included, which the rim must not take on.
    [AvaloniaFact]
    public void PartialRedraw_KeepsTheSamplesOutsideTheClip()
    {
        var poke = new Border { Width = 16, Height = 12, Background = Brushes.Transparent };
        var glass = new GlassBorder { Width = 200, Height = 100, CornerRadius = new CornerRadius(12) };
        var bar = new Border { Width = 40, Height = 7, Background = Brushes.White };
        Canvas.SetLeft(poke, 140); Canvas.SetTop(poke, 54);
        Canvas.SetLeft(glass, 40); Canvas.SetTop(glass, 60);
        Canvas.SetLeft(bar, 96); Canvas.SetTop(bar, 52);
        var window = new Window
        {
            Width = 300, Height = 200, RequestedThemeVariant = ThemeVariant.Dark,
            Content = new Canvas { Background = new SolidColorBrush(Color.Parse("#202020")), Children = { poke, glass, bar } },
        };
        using var before = Frame.Capture(window);
        poke.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        using var after = Frame.Capture(window);
        window.Close();
        var max = 0;
        for (var y = 60; y < 66; y++)
            for (var x = 140; x < 156; x++)
                max = Math.Max(max, Diff(before[x, y], after[x, y]));
        Assert.True(max <= 2, $"the rim next to the bar changed by {max}");
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
        var (_, far, near) = Compare(one, rows, one, scale);
        Assert.True(Math.Max(far, near) <= 1, $"max difference {Math.Max(far, near)}");
    }

    // A full-window panel at 2x: preparing either method for a new size costs about what it costs
    // for a small card, with no rasterised masks (the filter rim's geometry is a shader). Masks
    // rasterised per size took 0.5 to 1.7 s at this size.
    [AvaloniaFact]
    public void NewSize_PreparesWithoutRasterising()
    {
        static GlassRimDrawOperation.Parameters P(double w, double h) =>
            new(new Rect(0, 0, w, h), new CornerRadius(16), 0.6, 1, 0.6, 1, 12, 0.5, 4, Colors.White);
        static double Prepare(double w, double h)
        {
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < 6; i++)
            {
                GlassRimFilter.Create(P(w - 20 * i, h - 10 * i), 2)!.Dispose();
                GlassRimStrip.Create(P(w - 20 * i, h - 10 * i), 2)!.Dispose();
            }
            return watch.Elapsed.TotalMilliseconds;
        }
        Prepare(300, 200);
        var small = Prepare(300, 200);
        var large = Prepare(1392, 803);
        Assert.True(large < 3 * small + 50, $"6 full-window sizes took {large:F0} ms, 6 card sizes {small:F0} ms");
    }

    // A glass panel holding four glass cards over the colourful backdrop.
    private static Frame Render(ThemeVariant variant, double scale, Action<GlassBorder> setup, double opacity = 1, bool effect = false)
    {
        var window = Window(variant, scale, setup, opacity, effect, out _);
        var frame = Frame.Capture(window);
        window.Close();
        return frame;
    }

    private static Window Window(ThemeVariant variant, double scale, Action<GlassBorder> setup, double opacity, bool effect, out GlassBorder panel)
    {
        var cards = new WrapPanel { Margin = new Thickness(12) };
        for (var i = 0; i < 4; i++)
            cards.Children.Add(new GlassBorder { Width = CardWidth, Height = CardHeight, CornerRadius = new CornerRadius(12), Margin = new Thickness(0, 0, CardGap, CardGap) });
        panel = new GlassBorder { CornerRadius = new CornerRadius(16), Margin = new Thickness(PanelMargin), Child = cards, Opacity = opacity };
        if (effect)
            panel.Effect = new DropShadowEffect { BlurRadius = 8, Opacity = 0.5 };
        foreach (var g in new[] { panel }.Concat(cards.Children.OfType<GlassBorder>()))
            setup(g);
        var window = new Window { Width = WindowWidth, Height = WindowHeight, RequestedThemeVariant = variant, Content = new Panel { Children = { Backdrop.Colourful(), panel } } };
        window.SetRenderScaling(scale);
        return window;
    }

    private const double WindowWidth = 380, WindowHeight = 300, PanelMargin = 20, CardWidth = 150, CardHeight = 100, CardGap = 12;

    // The glass corners in the window, in px: the panel's and the cards', from a laid-out window
    // (the layout is the same in every theme and at every scale).
    private static Point[]? _corners;

    private static Point[] Corners => _corners ??= CornersOf();

    private static Point[] CornersOf()
    {
        var window = Window(ThemeVariant.Dark, 1, _ => { }, 1, false, out var panel);
        window.Show();
        var corners = panel.GetSelfAndVisualDescendants().OfType<GlassBorder>()
            .Select(g => new Rect(g.TranslatePoint(default, window)!.Value, g.Bounds.Size))
            .SelectMany(r => new[] { r.TopLeft, r.TopRight, r.BottomRight, r.BottomLeft }).ToArray();
        window.Close();
        return corners;
    }

    // The mean difference over the rim's pixels (where the frame without a rim differs), and the
    // max more than 20 px from every glass corner (far) and within 20 px of one (near).
    private static (double Mean, int Far, int Near) Compare(Frame a, Frame b, Frame bare, double scale)
    {
        double sum = 0;
        int far = 0, near = 0, rim = 0;
        for (var y = 0; y < a.Height; y++)
            for (var x = 0; x < a.Width; x++)
            {
                var d = Diff(a[x, y], b[x, y]);
                var p = new Point((x + 0.5) / scale, (y + 0.5) / scale);
                if (Corners.Any(c => Point.Distance(c, p) <= 20))
                    near = Math.Max(near, d);
                else
                    far = Math.Max(far, d);
                if (Diff(a[x, y], bare[x, y]) > 0 || ReferenceEquals(a, bare))
                {
                    sum += d;
                    rim++;
                }
            }
        return (sum / Math.Max(rim, 1), far, near);
    }

    private static int Diff((byte R, byte G, byte B) p, (byte R, byte G, byte B) q) =>
        Math.Max(Math.Abs(p.R - q.R), Math.Max(Math.Abs(p.G - q.G), Math.Abs(p.B - q.B)));
}
