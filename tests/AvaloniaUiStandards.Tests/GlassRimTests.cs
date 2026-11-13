using Avalonia;
using Avalonia.Controls;
using System.Runtime.InteropServices;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// The glass rim: a hairline on the edge that takes a light tint of the backdrop just outside it
/// (hue kept, fainter over dark areas), drawn only on the edge; the fixed border is the fallback.
/// </summary>
public class GlassRimTests
{
    private const int Left = 40, Top = 30, Width = 160, Height = 100;
    private const int MidX = Left + Width / 2;

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Rim_IsALightTintOfTheBackdropBelow_KeepingItsHue(string theme)
    {
        using var magenta = Render(Color.Parse("#C0208F"), theme);
        using var teal = Render(Color.Parse("#1A9C8C"), theme);
        var (mr, mg, mb) = magenta[MidX, Top];
        var (tr, tg, tb) = teal[MidX, Top];
        Assert.True(mr > mg + 20 && mb > mg, $"Over magenta the rim is not pinkish: {(mr, mg, mb)}.");
        Assert.True(tg > tr + 20 && tb > tr, $"Over teal the rim is not teal-ish: {(tr, tg, tb)}.");
        // Lightened: brighter than the backdrop it samples.
        Assert.True(magenta.Luma(MidX, Top) > Luma(Color.Parse("#C0208F")), "The rim is not lighter than the backdrop.");
    }

    // On dark glass, where the rim shows against the fill (on light glass over pale backdrops
    // it is white on near-white).
    [AvaloniaFact]
    public void Rim_IsFaintOverDarkAreas()
    {
        var bright = RimChange(Color.Parse("#E0E0E0"), "Dark");
        var dark = RimChange(Color.Parse("#101010"), "Dark");
        Assert.True(bright > 4 * Math.Max(dark, 0.5), $"Rim over light {bright:F1} vs over dark {dark:F1}.");
    }

    // The rim varies along the edge with what is directly outside each point of it.
    [AvaloniaFact]
    public void Rim_FollowsTheBackdropAlongTheEdge()
    {
        var split = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        split.Children.Add(new Border { Background = new SolidColorBrush(Color.Parse("#F0F0F0")) });
        var right = new Border { Background = new SolidColorBrush(Color.Parse("#101010")) };
        Grid.SetColumn(right, 1);
        split.Children.Add(right);
        using var frame = Render(split, "Dark");
        // The split is at x = 120 (half the 240 px window): left of it bright, right of it dark.
        Assert.True(frame.Luma(80, Top) > frame.Luma(170, Top) + 40, $"Rim over light {frame.Luma(80, Top):F0} vs over dark {frame.Luma(170, Top):F0}.");
    }

    // The rim itself is a hairline: without its glow, two pixels in nothing changes.
    [AvaloniaFact]
    public void Rim_IsAHairline_OnTheEdge()
    {
        using var without = Render(Colors.White, "Dark", absorption: 0);
        using var with = Render(Colors.White, "Dark", glow: 0);
        Assert.Equal(without[MidX, Top + 2], with[MidX, Top + 2]);
        Assert.Equal(without[Left + 2, Top + Height / 2], with[Left + 2, Top + Height / 2]);
        Assert.NotEqual(without[MidX, Top], with[MidX, Top]);
    }

    // A soft glow falls inward from the rim: weaker than the rim, fading with depth, and gone a
    // few pixels past the glow's width.
    [AvaloniaFact]
    public void Glow_FadesInwardFromTheRim()
    {
        using var without = Render(Colors.White, "Dark", absorption: 0);
        using var with = Render(Colors.White, "Dark");
        double Added(int depth) => with.Luma(MidX, Top + depth) - without.Luma(MidX, Top + depth);

        Assert.True(Added(2) > 5, $"no glow 2 px in: {Added(2):F0}");
        Assert.True(Added(2) < Added(0), $"the glow is not weaker than the rim: {Added(2):F0} vs {Added(0):F0}");
        Assert.True(Added(2) > Added(4) && Added(4) >= Added(5), $"the glow does not fade: {Added(2):F0}, {Added(4):F0}, {Added(5):F0}");
        Assert.True(Math.Abs(Added(8)) < 1, $"the glow reaches too far: 8 px in {Added(8):F0}");
    }

    // Drawn above the child: a child with a background reaching the edge does not cover it.
    [AvaloniaFact]
    public void Rim_IsDrawnAboveTheChild()
    {
        var glass = new GlassBorder { Width = Width, Height = Height, Child = new Border { Background = Brushes.Red } };
        Canvas.SetLeft(glass, Left);
        Canvas.SetTop(glass, Top);
        var window = new Window
        {
            Width = 240, Height = 160,
            Content = new Panel { Children = { new Border { Background = new SolidColorBrush(Color.Parse("#1A9C8C")) }, new Canvas { Children = { glass } } } },
        };
        using var frame = Frame.Capture(window);
        window.Close();
        var (r, g, b) = frame[MidX, Top];
        Assert.True(g > r && b > r, $"The edge {(r, g, b)} is the child's red, not the rim.");
        Assert.Equal(((byte)255, (byte)0, (byte)0), frame[MidX, Top + Height / 2]);
    }

    [AvaloniaFact]
    public void TopEdge_CatchesMoreLightThanTheBottom()
    {
        using var frame = Render(Color.Parse("#A0A0A0"), "Dark");
        Assert.True(frame.Luma(MidX, Top) > frame.Luma(MidX, Top + Height - 1) + 3, $"top {frame.Luma(MidX, Top):F0} vs bottom {frame.Luma(MidX, Top + Height - 1):F0}");
    }

    // Reduce transparency: the solid fill with the fixed border, no sampling.
    [AvaloniaFact]
    public void Fallback_DrawsTheFixedBorder()
    {
        using var rim = Render(Colors.White, "Light", reduce: true, border: Colors.Blue);
        using var noRim = Render(Colors.White, "Light", reduce: true, border: Colors.Blue, absorption: 0);
        var edge = rim[MidX, Top];
        Assert.True(edge.B > 200 && edge.R < 120, $"Edge {edge} is not the border colour.");
        AssertSame(rim, noRim, (_, _) => true);
    }

    [AvaloniaFact]
    public void NothingDrawsOutsideTheShape()
    {
        using var without = Render(Color.Parse("#C0208F"), "Light", absorption: 0);
        using var with = Render(Color.Parse("#C0208F"), "Light");
        AssertSame(without, with, (x, y) => x < Left || y < Top || x >= Left + Width || y >= Top + Height
            || (x - Left) + (y - Top) < 3 || (Left + Width - 1 - x) + (Top + Height - 1 - y) < 3);
    }

    // Thicker where the backdrop is bright: one pixel further in, the rim still shows over white
    // but has nearly faded over mid grey.
    [AvaloniaFact]
    public void Rim_SwellsSlightly_WhereTheBackdropIsBright()
    {
        double Reach(Color backdrop)
        {
            using var without = Render(backdrop, "Dark", absorption: 0);
            using var with = Render(backdrop, "Dark");
            return (with.Luma(MidX, Top + 1) - without.Luma(MidX, Top + 1)) / Math.Max(with.Luma(MidX, Top) - without.Luma(MidX, Top), 1);
        }
        var bright = Reach(Colors.White);
        var grey = Reach(Color.Parse("#606060"));
        Assert.True(bright > grey + 0.25, $"second-pixel share over white {bright:F2} vs over grey {grey:F2}");
    }

    // Bright stretches are amplified (up to 75 %, smoothly with brightness) and widen a little;
    // dim ones are not. The rim's added brightness, with the boost against without it, on the edge
    // row and one row in. Over white the edge row is already near white and nearly opaque, so the
    // capped boost shows there mostly as width (the row one pixel in).
    [AvaloniaFact]
    public void Rim_BrightStretches_AreAmplified_DimOnesAreNot()
    {
        double Gain(Color backdrop, int row)
        {
            using var without = Render(backdrop, "Dark", absorption: 0);
            using var boosted = Render(backdrop, "Dark");
            using var plain = Render(backdrop, "Dark", boost: 0);
            return (boosted.Luma(MidX, row) - without.Luma(MidX, row)) / Math.Max(plain.Luma(MidX, row) - without.Luma(MidX, row), 0.5);
        }
        var limit = 1 + GlassRimDrawOperation.Boost + 0.01;
        var white = Gain(Colors.White, Top);
        var whiteInner = Gain(Colors.White, Top + 1);
        var bright = Gain(Color.Parse("#B0B0B0"), Top);
        var dim = Gain(Color.Parse("#303030"), Top);
        Assert.True(bright > 1.1 && white >= 0.99, $"bright stretch gains: light grey {bright:F3}, white {white:F3}");
        Assert.True(whiteInner > 1.05, $"over white the rim does not widen: one pixel in {whiteInner:F3}");
        Assert.True(white <= limit && bright <= limit, $"edge-row gain over 75 %: {white:F3}, {bright:F3}");
        Assert.True(Math.Abs(dim - 1) < 0.03, $"dim stretch gain {dim:F3}");
    }

    // A thin bright line touching the edge lights the rim at the crossing and its light travels a
    // short way along the edge, fading with distance; without the line the rim stays faint.
    [AvaloniaFact]
    public void ThinLine_LightsTheRim_AndSpreadsAlongTheEdge()
    {
        Control Line() => new Panel
        {
            Children =
            {
                new Border { Background = new SolidColorBrush(Color.Parse("#101010")) },
                new Border { Width = 1, Background = Brushes.White, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(MidX, 0, 0, 0) },
            },
        };
        using var spread = Render(Line(), "Dark");
        using var local = Render(Line(), "Dark", spread: 0);
        using var dark = Render(Color.Parse("#101010"), "Dark");
        var baseline = dark.Luma(MidX + 60, Top);
        double At(Frame f, int dx) => f.Luma(MidX + dx, Top) - baseline;

        // Lit at the crossing, still near full strength a dozen px along the edge, then fading out.
        Assert.True(At(spread, 0) > 60, $"the crossing is not lit: {At(spread, 0):F0}");
        Assert.True(At(spread, 6) > 40 && At(local, 6) < 5, $"the light does not travel along the edge: 6 px away {At(spread, 6):F0} (without spreading {At(local, 6):F0})");
        Assert.True(At(spread, 11) > 0.6 * At(spread, 0), $"the light does not run far along the edge: 11 px away {At(spread, 11):F0} of {At(spread, 0):F0}");
        Assert.True(At(spread, 11) > At(spread, 18) && At(spread, 18) > At(spread, 24), "the light does not fade with distance");
        Assert.True(Math.Abs(At(spread, 32)) < 4, $"the light reaches too far: 32 px away {At(spread, 32):F0}");
        Assert.True(Math.Abs(At(spread, -11) - At(spread, 11)) < 6, "the light does not spread both ways");
        Assert.True(At(dark, 0) < 3, "without a line the rim is not faint");
    }

    // At the crossing itself the rim blends into the spread: no single pixel stands out from its
    // neighbours along the edge (the line's own sample does not show through).
    [AvaloniaTheory]
    [InlineData("Dark", "#FFF4D6")]
    [InlineData("Dark", "#3A6FD8")]
    [InlineData("Light", "#FF40C0")]
    public void Crossing_BlendsIntoTheSpread(string theme, string lineColour)
    {
        var line = new Panel
        {
            Children =
            {
                new Border { Background = new SolidColorBrush(Color.Parse("#101018")) },
                new Border { Width = 1, Background = new SolidColorBrush(Color.Parse(lineColour)), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(MidX, 0, 0, 0) },
            },
        };
        using var frame = Render(line, theme);
        for (var dx = -3; dx <= 3; dx++)
        {
            var (r, g, b) = frame[MidX + dx, Top];
            var (r0, g0, b0) = frame[MidX + dx - 1, Top];
            var (r1, g1, b1) = frame[MidX + dx + 1, Top];
            var worst = Math.Max(Math.Abs(r - (r0 + r1) / 2.0), Math.Max(Math.Abs(g - (g0 + g1) / 2.0), Math.Abs(b - (b0 + b1) / 2.0)));
            Assert.True(worst <= 10, $"rim pixel {dx} px from the crossing stands out by {worst:F0}: {(r, g, b)} between {(r0, g0, b0)} and {(r1, g1, b1)}");
        }
    }

    // The light carried along the edge is part colour and part white in both themes: a magenta
    // line over near-black, measured 6 px along the edge, keeps its hue but is lifted towards white.
    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void CarriedLight_IsPartColour_PartWhite(string theme)
    {
        Control Line() => new Panel
        {
            Children =
            {
                new Border { Background = new SolidColorBrush(Color.Parse("#101010")) },
                new Border { Width = 1, Background = new SolidColorBrush(Color.Parse("#FF40C0")), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(MidX, 0, 0, 0) },
            },
        };
        using var lit = Render(Line(), theme);
        using var plain = Render(Color.Parse("#101010"), theme);
        var (r, g, b) = lit[MidX + 6, Top];
        var (_, pg, _) = plain[MidX + 6, Top];
        // Colour: the magenta shows (red above green). White: green, which the line barely has,
        // covers at least a quarter of the way from the rim's own level to white.
        Assert.True(r > g + 15 && b > g, $"the carried light lost the line's hue: {(r, g, b)}");
        Assert.True(g - pg > 0.25 * (255 - pg), $"the carried light is not lifted towards white: green {g} vs {pg} without the line");
    }

    // Broad areas, uniform along the edge, look the same with the spread as without.
    [AvaloniaTheory]
    [InlineData("#808080")]
    [InlineData("#C0208F")]
    public void BroadAreas_AreUnchangedBySpreading(string colour)
    {
        using var spread = Render(Color.Parse(colour), "Dark");
        using var local = Render(Color.Parse(colour), "Dark", spread: 0);
        foreach (var (x, y) in new[] { (MidX, Top), (Left + 1, Top + Height / 2), (MidX, Top + Height - 1) })
            Assert.True(Math.Abs(spread.Luma(x, y) - local.Luma(x, y)) < 2, $"({x}, {y}): {spread.Luma(x, y):F0} vs {local.Luma(x, y):F0}");
    }

    [AvaloniaFact]
    public void Spread_Renders()
    {
        using var sheet = new Sheet($"Glass rim: a 1 px bright line crossing the edge lights the rim and spreads along it ({GlassRimDrawOperation.Spread} px at full strength, fading over about {2 * GlassRimDrawOperation.SpreadSigma} px); part colour, part white (white mix {GlassBorder.DefaultRimSpreadWhiteDark}); 4× close-up");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, ["Before (rim without spreading)", "After (rim with spreading)"],
            [
                ("Cream line across the top edge", [Zoomed(sheet, LineScene(spread: false, "#FFF4D6"), theme, 64, 10), Zoomed(sheet, LineScene(spread: true, "#FFF4D6"), theme, 64, 10)]),
                ("Magenta line across the top edge", [Zoomed(sheet, LineScene(spread: false, "#FF40C0"), theme, 64, 10), Zoomed(sheet, LineScene(spread: true, "#FF40C0"), theme, 64, 10)]),
            ]);
        sheet.Save("m3-glass-rim-spread.png");
    }

    [AvaloniaFact]
    public void Edge_Renders()
    {
        using var sheet = new Sheet($"Glass rim: hairline {GlassRimDrawOperation.MinWidth} px, swelling to {GlassRimDrawOperation.MaxWidth} px where the backdrop is bright; light tint of the backdrop just outside (sampled {GlassRimDrawOperation.SampleOffset} px out, strength {GlassBorder.DefaultRimAbsorption}, bright stretches +{GlassRimDrawOperation.Boost * 100:F1} % and up to {GlassRimDrawOperation.BoostWidth} px wider; tint {GlassBorder.DefaultTintOpacity})");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            var lighten = theme == ThemeVariant.Dark ? GlassBorder.DefaultRimLightenDark : GlassBorder.DefaultRimLightenLight;
            string[] columns = ["Before (fixed 1 px border)", $"After (rim, lightened {lighten})"];
            var rows = new List<(string, IReadOnlyList<Control>)>();
            foreach (var (bands, zoom, label) in new[]
                     {
                         (true, Zoom.None, "Colour bands"), (true, Zoom.TopLeft, "Colour bands, top-left ×4"),
                         (true, Zoom.TopRight, "Colour bands, top-right ×4 (bright bands)"), (true, Zoom.BottomRight, "Colour bands, bottom-right ×4 (bright bands)"),
                         (false, Zoom.None, "Plain wash"), (false, Zoom.TopLeft, "Plain wash, top-left ×4"),
                     })
                rows.Add((label, [Cell(sheet, Scene(bands, rim: false), theme, zoom), Cell(sheet, Scene(bands, rim: true), theme, zoom)]));
            sheet.AddSection(themeLabel, columns, rows);
        }
        sheet.Save("m3-glass-rim.png");
    }

    // --- Helpers -------------------------------------------------------------------------

    // A dark violet backdrop with a 1 px line running diagonally across the panel's top
    // edge (at x ≈ 99, y = 25), like a ribbon's edge.
    private static Control LineScene(bool spread, string colour)
    {
        var glass = new GlassBorder
        {
            Width = 220, Height = 120,
            Child = new TextBlock { Text = "Glass panel", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center },
        };
        if (!spread)
            glass.SetValue(GlassBorder.RimSpreadProperty, 0d);
        Canvas.SetLeft(glass, 30);
        Canvas.SetTop(glass, 25);
        var line = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Point(90, 0), EndPoint = new Point(150, 170),
            Stroke = new SolidColorBrush(Color.Parse(colour)), StrokeThickness = 1,
        };
        var scene = new Panel
        {
            Width = 280, Height = 170,
            Children = { new Border { Background = new SolidColorBrush(Color.Parse("#2A1748")) }, new Canvas { Children = { line } }, new Canvas { Children = { glass } } },
        };
        return scene;
    }

    private static Control Cell(Sheet sheet, Control scene, ThemeVariant theme, Zoom zoom) => zoom switch
    {
        Zoom.None => sheet.Cell(scene, theme),
        Zoom.TopLeft => Zoomed(sheet, scene, theme, 26, 21),
        Zoom.TopRight => Zoomed(sheet, scene, theme, 30 + 220 + 4 - 70, 21),
        _ => Zoomed(sheet, scene, theme, 30 + 220 + 4 - 70, 25 + 120 + 4 - 42),
    };

    // A 70 × 42 px region of the scene at 4×. The whole scene is rendered at 4× and the region
    // cropped from the frame, so the rim sees the real backdrop around the region, not a clip.
    private static Control Zoomed(Sheet sheet, Control scene, ThemeVariant theme, int x, int y)
    {
        var window = new Window
        {
            SizeToContent = SizeToContent.WidthAndHeight,
            RequestedThemeVariant = theme,
            Content = new LayoutTransformControl { LayoutTransform = new ScaleTransform(4, 4), Child = scene },
        };
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        using var capture = window.CaptureRenderedFrame()!;
        window.Close();

        var scale = 4 * Sheet.Scale;
        var (left, top, width, height) = ((int)(x * scale), (int)(y * scale), (int)(70 * scale), (int)(42 * scale));
        using var source = capture.Lock();
        var crop = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), source.Format, AlphaFormat.Premul);
        using (var target = crop.Lock())
        {
            var row = new byte[width * 4];
            for (var r = 0; r < height; r++)
            {
                Marshal.Copy(source.Address + (top + r) * source.RowBytes + left * 4, row, 0, row.Length);
                Marshal.Copy(row, 0, target.Address + r * target.RowBytes, row.Length);
            }
        }
        using (crop)
            return sheet.Image(crop);
    }

    private enum Zoom { None, TopLeft, TopRight, BottomRight }

    // Wide diagonal colour bands crossing the panel's edges, dark and bright, like a wallpaper.
    private static IBrush Bands() => new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0.6, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#2B1055"), 0.00),
            new GradientStop(Color.Parse("#7B2FBE"), 0.18),
            new GradientStop(Color.Parse("#E040A0"), 0.34),
            new GradientStop(Color.Parse("#FF8A65"), 0.48),
            new GradientStop(Color.Parse("#101018"), 0.62),
            new GradientStop(Color.Parse("#0A0A12"), 0.70),
            new GradientStop(Color.Parse("#2EC4B6"), 0.84),
            new GradientStop(Color.Parse("#F7F3E3"), 1.00),
        },
    };

    // A glass panel over the backdrop; "before" draws today's fixed border over glass without
    // the rim.
    private static Control Scene(bool bands, bool rim)
    {
        var glass = new GlassBorder
        {
            Width = 220, Height = 120,
            Padding = new Thickness(16),
            Child = new TextBlock { Text = "Glass panel", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center },
        };
        if (!rim)
            glass.RimAbsorption = 0;
        var panel = new Panel { Width = 220, Height = 120, Children = { glass } };
        if (!rim)
        {
            var border = new SquircleBorder { BorderThickness = new Thickness(1), Background = Brushes.Transparent, IsHitTestVisible = false };
            border.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("GlassBorderBrush"));
            border.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusOverlay"));
            panel.Children.Add(border);
        }
        Canvas.SetLeft(panel, 30);
        Canvas.SetTop(panel, 25);

        var backdrop = new Border();
        if (bands)
            backdrop.Background = Bands();
        else
            backdrop.Bind(Border.BackgroundProperty, new DynamicResourceExtension("BackdropWash"));
        return new Panel { Width = 280, Height = 170, Children = { backdrop, new Canvas { Children = { panel } } } };
    }

    // The luma change at the top edge (with the rim minus without).
    private static double RimChange(Color backdrop, string theme)
    {
        using var without = Render(backdrop, theme, absorption: 0);
        using var with = Render(backdrop, theme);
        return with.Luma(MidX, Top) - without.Luma(MidX, Top);
    }

    private static double Luma(Color c) => 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;

    private static Frame Render(Color backdrop, string theme, double? absorption = null, bool reduce = false, Color? border = null, double? boost = null, double? spread = null, double? glow = null) =>
        Render(new Border { Background = new SolidColorBrush(backdrop) }, theme, absorption, reduce, border, boost, spread, glow);

    private static Frame Render(Control backdrop, string theme, double? absorption = null, bool reduce = false, Color? border = null, double? boost = null, double? spread = null, double? glow = null)
    {
        var glass = new GlassBorder { Width = Width, Height = Height };
        if (spread is { } sp)
            glass.SetValue(GlassBorder.RimSpreadProperty, sp);
        if (glow is { } gw)
            glass.SetValue(GlassBorder.RimGlowProperty, gw);
        if (boost is { } g)
            glass.SetValue(GlassBorder.RimBoostProperty, g);
        if (absorption is { } a)
            glass.RimAbsorption = a;
        if (border is { } b)
            glass.BorderBrush = new SolidColorBrush(b);
        if (reduce)
            glass.SetValue(GlassBorder.ReduceTransparencyProperty, true);
        Canvas.SetLeft(glass, Left);
        Canvas.SetTop(glass, Top);
        var window = new Window
        {
            Width = 240, Height = 160,
            RequestedThemeVariant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light,
            Content = new Panel { Children = { backdrop, new Canvas { Children = { glass } } } },
        };
        var frame = Frame.Capture(window);
        window.Close();
        return frame;
    }

    private static void AssertSame(Frame a, Frame b, Func<int, int, bool> where)
    {
        for (var y = 0; y < a.Height; y++)
            for (var x = 0; x < a.Width; x++)
                if (where(x, y))
                    Assert.True(a[x, y] == b[x, y], $"Pixel ({x}, {y}) changed: {a[x, y]} → {b[x, y]}.");
    }
}

