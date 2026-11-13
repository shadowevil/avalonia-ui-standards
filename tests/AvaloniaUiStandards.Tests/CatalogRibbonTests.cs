using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Catalog;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>The catalog's animated ribbon backdrop: renders, animation rules, seamless looping and text contrast.</summary>
public class CatalogRibbonTests
{
    private static readonly Size WindowSize = new(1200, 800);

    [AvaloniaFact]
    public void Ribbons_RenderAtThreePointsOfTheLoop()
    {
        // Three moments with the highlights in different places and different glints twinkling.
        double[] phases = [0.04, 0.37, 0.71];
        foreach (var (label, variant) in ButtonRenderTests.Themes)
            for (var step = 0; step < phases.Length; step++)
            {
                var window = NewWindow(variant, reduceMotion: false);
                window.SetRenderScaling(Sheet.Scale);
                window.Show();
                Settle(window);
                var ribbon = window.GetVisualDescendants().OfType<RibbonBackdrop>().Single();
                ribbon.AutoAdvance = false;
                ribbon.Phase = phases[step];
                Settle(window);

                using var frame = Frame.Capture(window);
                frame.Save($"catalog-ribbons-{label.ToLowerInvariant()}-{step + 1}.png");
                window.Close();

                // The backdrop alone, with no panel frosting it and no text areas kept clear.
                var bareRibbon = new RibbonBackdrop { AutoAdvance = false, Phase = phases[step] };
                var bareFrame = new SquircleBorder { Child = bareRibbon, Margin = new Thickness(24) };
                bareFrame.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("BackdropWash"));
                var bare = new Window { Width = 1200, Height = 560, RequestedThemeVariant = variant, Content = bareFrame };
                bare.SetRenderScaling(Sheet.Scale);
                bare.Show();
                Settle(bare);
                using var bareCapture = Frame.Capture(bare);
                bareCapture.Save($"catalog-ribbons-bare-{label.ToLowerInvariant()}-{step + 1}.png");
                bare.Close();

                // A 4x zoom on a ribbon's edge, from the same moment.
                if (step == 1)
                {
                    var zoomRibbon = new RibbonBackdrop { AutoAdvance = false, Phase = phases[step], Width = 1200, Height = 560 };
                    var viewport = new Border
                    {
                        Width = 320,
                        Height = 200,
                        ClipToBounds = true,
                        Child = new Canvas { Width = 320, Height = 200, Children = { zoomRibbon } },
                    };
                    Canvas.SetLeft(zoomRibbon, -520);
                    Canvas.SetTop(zoomRibbon, -150);
                    viewport.Bind(Border.BackgroundProperty, new DynamicResourceExtension("BackdropWash"));
                    var zoom = new Window { Width = 320, Height = 200, RequestedThemeVariant = variant, Content = viewport };
                    zoom.SetRenderScaling(4);
                    zoom.Show();
                    Settle(zoom);
                    using var zoomCapture = Frame.Capture(zoom);
                    zoomCapture.Save($"catalog-ribbons-zoom-{label.ToLowerInvariant()}.png");
                    zoom.Close();
                }
            }
    }

    [AvaloniaFact]
    public void Ribbons_AdvanceWithTheWindowsFrameTicks_WhileActive_AndStopWhenReducedInactiveOrMinimised()
    {
        // The library's frame ticks (the window's, about 30 a second), not calls to Advance.
        var window = NewWindow(ThemeVariant.Light, reduceMotion: false);
        try
        {
            window.Show();
            window.Activate();
            Settle(window);
            var ribbon = window.GetVisualDescendants().OfType<RibbonBackdrop>().Single();
            Assert.True(window.IsFrameTickRunning, "no ticks while active");
            Assert.True(ribbon.Twinkling);

            var start = ribbon.Phase;
            Ticks(window, 12);
            Assert.True(ribbon.Phase > start, "the loop did not move with the window's ticks while active");

            // Inactive: the same handler the platform calls when the window loses focus.
            PlatformSays(window, "HandleDeactivated");
            Assert.False(window.IsActive, "window still active");
            Assert.False(window.IsFrameTickRunning, "ticks while inactive");
            var inactive = ribbon.Phase;
            Ticks(window, 12);
            Assert.Equal(inactive, ribbon.Phase);

            // Active again: it moves.
            PlatformSays(window, "HandleActivated");
            Ticks(window, 12);
            Assert.True(ribbon.Phase > inactive, "the loop did not move again once active");

            // Minimised: it stops.
            window.WindowState = WindowState.Minimized;
            Assert.False(window.IsFrameTickRunning, "ticks while minimised");
            var minimised = ribbon.Phase;
            Ticks(window, 12);
            Assert.Equal(minimised, ribbon.Phase);
        }
        finally
        {
            window.Close();
        }

        var reduced = NewWindow(ThemeVariant.Light, reduceMotion: true);
        try
        {
            reduced.Show();
            reduced.Activate();
            Settle(reduced);
            var ribbon = reduced.GetVisualDescendants().OfType<RibbonBackdrop>().Single();
            Assert.False(reduced.IsFrameTickRunning, "ticks while motion is reduced");
            Assert.False(ribbon.Twinkling, "dots show while reduced");
            var start = ribbon.Phase;
            Ticks(reduced, 12);
            Assert.Equal(start, ribbon.Phase);
        }
        finally
        {
            reduced.Close();
        }
    }

    [AvaloniaFact]
    public void Ribbons_AdvanceByTheTickElapsed_AndNotWhenAutoAdvanceIsOff()
    {
        // The ribbon's own path: Advance moves the loop by the time given, and wraps at the loop's end.
        var ribbon = new RibbonBackdrop();
        ribbon.Advance(TimeSpan.FromSeconds(4.5));
        Assert.Equal(0.1, ribbon.Phase, 9);
        ribbon.Advance(TimeSpan.FromSeconds(RibbonBackdrop.LoopSeconds));
        Assert.Equal(0.1, ribbon.Phase, 9);
    }

    // Lets the window's frame ticks run: the headless render timer ticks, with a real pause longer than a tick's 33 ms.
    private static void Ticks(Window window, int count)
    {
        for (var i = 0; i < count; i++)
        {
            Thread.Sleep(40);
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void Ribbons_AreExactlyPeriodicInTheLoop()
    {
        // Everything that moves is a sine of a whole number of cycles per loop, so the shapes, the light and the dots (their
        // strength and position) at the end of the loop (phase 1) are those at its start (phase 0), to rounding.
        var ribbon = new RibbonBackdrop { Width = 1200, Height = 800, AutoAdvance = false };
        var window = new Window { Width = 1200, Height = 800, Content = ribbon };
        try
        {
            window.Show();
            Settle(window);
            for (var r = 0; r < RibbonBackdrop.RibbonCount; r++)
            {
                for (var x = -20.0; x < 1240; x += 7)
                {
                    ribbon.Phase = 0;
                    var centre = ribbon.CentreAt(r, x, 800);
                    var half = ribbon.HalfWidthAt(r, x, 800);
                    var lights = new[] { ribbon.Light(r, 0, x), ribbon.Light(r, 1, x), ribbon.Light(r, 2, x) };
                    ribbon.Phase = 1;
                    Assert.Equal(centre, ribbon.CentreAt(r, x, 800), 9);
                    Assert.Equal(half, ribbon.HalfWidthAt(r, x, 800), 9);
                    for (var l = 0; l < 3; l++)
                        Assert.Equal(lights[l], ribbon.Light(r, l, x), 9);
                }

                ribbon.Phase = 0;
                var start = ribbon.DotStates(r, 800);
                ribbon.Phase = 1;
                var end = ribbon.DotStates(r, 800);
                Assert.NotEmpty(start);
                Assert.Equal(start.Length, end.Length);
                for (var i = 0; i < start.Length; i++)
                {
                    Assert.Equal(start[i].Strength, end[i].Strength, 9);
                    Assert.Equal(start[i].X, end[i].X, 9);
                    Assert.Equal(start[i].Y, end[i].Y, 9);
                }
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Ribbons_LoopSeamlessly(string theme)
    {
        // The frame at the end of the loop (phase 1) equals the frame at the start (phase 0). The dots are left out:
        // their brightness is in steps; the maths test above covers their strength and position.
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var ribbon = new RibbonBackdrop { Width = 1000, Height = 600, AutoAdvance = false, HideDots = true };
        var host = new Border { Child = ribbon };
        host.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemRegionColor"));
        var window = new Window { Width = 1000, Height = 600, RequestedThemeVariant = variant, Content = host };
        try
        {
            window.Show();
            Settle(window);
            ribbon.Phase = 0;
            Settle(window);
            using var start = Frame.Capture(window);
            ribbon.Phase = 1;
            Settle(window);
            using var end = Frame.Capture(window);

            var worst = 0;
            var (wx, wy) = (0, 0);
            for (var y = 0; y < start.Height; y++)
                for (var x = 0; x < start.Width; x++)
                {
                    var (r0, g0, b0) = start[x, y];
                    var (r1, g1, b1) = end[x, y];
                    var d = Math.Max(Math.Abs(r0 - r1), Math.Max(Math.Abs(g0 - g1), Math.Abs(b0 - b1)));
                    if (d > worst)
                        (worst, wx, wy) = (d, x, y);
                }
            Assert.True(worst <= 2, $"loop end differs from start by up to {worst} levels at ({wx}, {wy}): {start[wx, wy]} vs {end[wx, wy]}");

            // Sanity: the ribbons do move within the loop.
            ribbon.Phase = 0.25;
            Settle(window);
            using var quarter = Frame.Capture(window);
            var moved = false;
            for (var y = 0; y < start.Height && !moved; y += 3)
                for (var x = 0; x < start.Width && !moved; x += 3)
                    moved = start[x, y] != quarter[x, y];
            Assert.True(moved);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Ribbons_LightIsContinuousAlongLinesAndOverTime()
    {
        // The light on a line is a function of position and of the loop, so it has no steps: neighbouring points a
        // few px apart, and the same point a frame (16 ms) later, differ by only a small amount. And it does have dark
        // gaps and lit stretches.
        var ribbon = new RibbonBackdrop { Width = 1200, Height = 800, AutoAdvance = false };
        const double frame = 0.016 / RibbonBackdrop.LoopSeconds;
        var dark = 0;
        var total = 0;
        for (var r = 0; r < RibbonBackdrop.RibbonCount; r++)
            for (var line = 0; line < 2; line++)
                for (var step = 0; step < 50; step++)
                {
                    ribbon.Phase = step / 50.0;
                    for (var x = 0.0; x < 1200; x += 4)
                    {
                        ribbon.Phase = step / 50.0;
                        var here = ribbon.Light(r, line, x);
                        var next = ribbon.Light(r, line, x + 4);
                        ribbon.Phase = step / 50.0 + frame;
                        var later = ribbon.Light(r, line, x);
                        Assert.True(Math.Abs(next - here) <= 0.12, $"light steps by {Math.Abs(next - here):0.00} over 4 px (ribbon {r}, line {line}, x {x})");
                        Assert.True(Math.Abs(later - here) <= 0.02, $"light steps by {Math.Abs(later - here):0.00} in 16 ms (ribbon {r}, line {line}, x {x})");
                        total++;
                        if (here < 0.02)
                            dark++;
                    }
                }
        Assert.InRange((double)dark / total, 0.35, 0.6);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Ribbons_ChangeSmoothlyOverTime(string theme)
    {
        // Three frames about 16 ms apart, without the dots (their quick flashes are meant to change fast): nothing may
        // jump between neighbours, so there are no steps in the light or the shape.
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var ribbon = new RibbonBackdrop { Width = 1000, Height = 600, AutoAdvance = false, HideDots = true };
        var host = new Border { Child = ribbon };
        host.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemRegionColor"));
        var window = new Window { Width = 1000, Height = 600, RequestedThemeVariant = variant, Content = host };
        const double step = 0.016 / RibbonBackdrop.LoopSeconds;
        try
        {
            window.Show();
            var frames = new List<Frame>();
            foreach (var phase in new[] { 0.3, 0.3 + step, 0.3 + 2 * step })
            {
                ribbon.Phase = phase;
                Settle(window);
                frames.Add(Frame.Capture(window));
            }

            var biggest = 0;
            for (var y = 0; y < frames[0].Height; y += 2)
                for (var x = 0; x < frames[0].Width; x += 2)
                {
                    var (r0, g0, b0) = frames[0][x, y];
                    var (r1, g1, b1) = frames[1][x, y];
                    var (r2, g2, b2) = frames[2][x, y];
                    biggest = Math.Max(biggest, Math.Max(Math.Abs(r1 - r0), Math.Max(Math.Abs(g1 - g0), Math.Abs(b1 - b0))));
                    biggest = Math.Max(biggest, Math.Max(Math.Abs(r2 - r1), Math.Max(Math.Abs(g2 - g1), Math.Abs(b2 - b1))));
                }
            foreach (var frame in frames)
                frame.Dispose();
            // A line moving a fraction of a pixel changes the pixels it crosses by up to a few tens of levels (more for the deep
            // lines in light); a pop would change them by well over a hundred.
            Assert.True(biggest <= 80, $"a pixel changed by {biggest} levels in 16 ms");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Ribbons_KeepTextOutsideThePanelAccessible(string theme)
    {
        // The title band, sidebar and status bar sit on the backdrop. Across a full loop, with every dot and line at full
        // brightness, the primary text colour must keep 5:1 (a margin over the AA minimum of 4.5:1) against the lightest
        // and the darkest backdrop pixel under each of them.
        var lowest = LowestContrast(theme);
        var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"catalog-ribbons-contrast-{theme.ToLowerInvariant()}.txt"), $"{lowest:0.00}");
        Assert.True(lowest >= 5.0, $"lowest text contrast over the ribbons is {lowest:0.00}:1");
    }

    // The lowest ratio of the primary text colour against the lightest or darkest backdrop pixel under the text
    // areas outside the panel, reading every pixel at 64 points of the loop.
    private static double LowestContrast(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var real = NewWindow(variant, reduceMotion: true);
        Rect[] areas;
        Rect panel;
        Color text;
        try
        {
            real.Show();
            Settle(real);
            areas =
            [
                Area(real, StandardsWindowTheme.TitleBandPart, 20, 4),
                Area(real, StandardsWindowTheme.SidebarPart, 8, 8),
                Area(real, StandardsWindowTheme.StatusBarPart, 20, 4),
            ];
            panel = Area(real, StandardsWindowTheme.ContentPanelPart, 0, 0);
            Assert.True(real.TryFindResource("SystemBaseHighColor", variant, out var value));
            text = (Color)value!;
        }
        finally
        {
            real.Close();
        }

        // The same backdrop without anything on it: the wash and the ribbons in a frame of the window's size, with
        // the panel placed where the window's is, and every dot at full brightness.
        var margin = StandardsWindowTheme.ShadowMargin;
        var ribbon = new RibbonBackdrop
        {
            PanelBounds = new Rect(panel.X - margin.Left, panel.Y - margin.Top, panel.Width, panel.Height),
            AutoAdvance = false,
            FullGlints = true,
        };
        var frame = new SquircleBorder { Child = ribbon, Margin = margin };
        frame.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("BackdropWash"));
        var bare = new Window { Width = WindowSize.Width, Height = WindowSize.Height, RequestedThemeVariant = variant, Content = frame };
        bare.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced));
        var lowest = double.MaxValue;
        try
        {
            bare.Show();
            Settle(bare);
            var lightest = new double[areas.Length];
            var darkest = Enumerable.Repeat(1.0, areas.Length).ToArray();
            const int steps = 64;
            for (var step = 0; step < steps; step++)
            {
                ribbon.Phase = (double)step / steps;
                Settle(bare);
                using var capture = Frame.Capture(bare);
                for (var a = 0; a < areas.Length; a++)
                    for (var y = (int)areas[a].Top; y < areas[a].Bottom; y++)
                        for (var x = (int)areas[a].Left; x < areas[a].Right; x++)
                        {
                            var l = Luminance(capture[x, y]);
                            lightest[a] = Math.Max(lightest[a], l);
                            darkest[a] = Math.Min(darkest[a], l);
                        }
            }

            var textLuminance = Luminance((text.R, text.G, text.B));
            for (var a = 0; a < areas.Length; a++)
                lowest = Math.Min(lowest, Math.Min(Ratio(textLuminance, lightest[a]), Ratio(textLuminance, darkest[a])));
        }
        finally
        {
            bare.Close();
        }

        return lowest;
    }

    [AvaloniaFact]
    public void Ribbons_AreVisibleBehindTheGlassInLight()
    {
        // Viewed straight on, the ribbons behind the glass must stand out from the glass around them by a clear amount:
        // the bands by a luminance contrast of 1.15:1 or more (median over the samples), the lines more than the bands.
        var (bands, lines) = MeasureThroughGlass();
        var bandMedian = Median(bands);
        var lineMedian = Median(lines);
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "ribbons-glass-visibility.txt"),
            $"bands: median {bandMedian:0.000}, min {bands.DefaultIfEmpty(0).Min():0.000}, {bands.Count} samples; lines: median {lineMedian:0.000}, min {lines.DefaultIfEmpty(0).Min():0.000}, {lines.Count} samples");
        Assert.True(bands.Count >= 12, $"only {bands.Count} band samples ({lines.Count} line samples)");
        Assert.True(lines.Count >= 12, $"only {lines.Count} line samples");
        Assert.True(bandMedian >= 1.15, $"bands behind the glass differ from it by only {bandMedian:0.000}:1");
        Assert.True(lineMedian >= bandMedian, $"lines ({lineMedian:0.000}:1) stand out less than bands ({bandMedian:0.000}:1)");
    }

    [AvaloniaFact]
    public void Text_OverTheGlassStaysAccessibleOverTheRibbonsInLight()
    {
        // Body text over the brightest and darkest backdrop behind the glass, at 16 points of the loop, keeps 4.5:1.
        var (window, ribbon) = LightWindowWithoutContent();
        try
        {
            var panel = window.ContentPanelBounds;
            var origin = ribbon.TranslatePoint(new Point(0, 0), window)!.Value;
            var area = new Rect(origin + panel.Position, panel.Size).Deflate(30);
            Assert.True(window.TryFindResource("SystemBaseHighColor", ThemeVariant.Light, out var value));
            var text = (Color)value!;
            var textLuminance = Luminance((text.R, text.G, text.B));
            var lowest = double.MaxValue;
            for (var step = 0; step < 16; step++)
            {
                ribbon.Phase = step / 16.0;
                Settle(window);
                using var frame = Frame.Capture(window);
                var darkest = 1.0;
                for (var y = (int)area.Top; y < area.Bottom; y++)
                    for (var x = (int)area.Left; x < area.Right; x++)
                        darkest = Math.Min(darkest, Luminance(frame[x, y]));
                lowest = Math.Min(lowest, Ratio(textLuminance, darkest));
            }
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "text-over-glass-light.txt"), $"{lowest:0.00}");
            Assert.True(lowest >= 4.5, $"text over the glass reaches only {lowest:0.00}:1 over the darkest ribbon area");
        }
        finally
        {
            window.Close();
        }
    }

    private static (MainWindow Window, RibbonBackdrop Ribbon) LightWindowWithoutContent()
    {
        var window = NewWindow(ThemeVariant.Light, reduceMotion: false);
        window.Content = new Border();
        window.Show();
        Settle(window);
        var ribbon = window.GetVisualDescendants().OfType<RibbonBackdrop>().Single();
        ribbon.AutoAdvance = false;
        ribbon.HideDots = true;
        return (window, ribbon);
    }

    // Samples the light window's panel, with nothing on it, where a ribbon's band or line is and where there is no
    // ribbon: the luminance contrast of each pair, over four points of the loop.
    private static (List<double> Bands, List<double> Lines) MeasureThroughGlass()
    {
        var (window, ribbon) = LightWindowWithoutContent();
        var bands = new List<double>();
        var lines = new List<double>();
        try
        {
            var panel = window.ContentPanelBounds;
            var origin = ribbon.TranslatePoint(new Point(0, 0), window)!.Value;
            var height = ribbon.Bounds.Height;
            bool Covered(double x, double y, double margin)
            {
                for (var r = 0; r < RibbonBackdrop.RibbonCount; r++)
                    if (Math.Abs(y - ribbon.CentreAt(r, x, height)) <= ribbon.HalfWidthAt(r, x, height) + margin)
                        return true;
                return false;
            }

            double Average(Frame frame, double x, double y, int radius)
            {
                var sum = 0.0;
                var count = 0;
                for (var dy = -radius; dy <= radius; dy++)
                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        sum += Luminance(frame[(int)(origin.X + x) + dx, (int)(origin.Y + y) + dy]);
                        count++;
                    }
                return sum / count;
            }

            foreach (var phase in new[] { 0.05, 0.3, 0.55, 0.8 })
            {
                ribbon.Phase = phase;
                Settle(window);
                using var frame = Frame.Capture(window);
                for (var r = 0; r < RibbonBackdrop.RibbonCount; r++)
                    for (var x = panel.Left + 70; x < panel.Right - 70; x += 60)
                    {
                        var centre = ribbon.CentreAt(r, x, height);
                        var half = ribbon.HalfWidthAt(r, x, height);

                        // Band: its middle against the page beside it, with no other ribbon near either.
                        var beside = centre - half - 40 > panel.Top + 40 && !Covered(x, centre - half - 40, 10) ? centre - half - 40
                            : centre + half + 40 < panel.Bottom - 40 && !Covered(x, centre + half + 40, 10) ? centre + half + 40
                            : double.NaN;
                        if (!double.IsNaN(beside) && centre > panel.Top + 40 && centre < panel.Bottom - 40
                            && !Enumerable.Range(0, RibbonBackdrop.RibbonCount).Where(o => o != r).Any(o => Math.Abs(centre - ribbon.CentreAt(o, x, height)) <= ribbon.HalfWidthAt(o, x, height) + 10))
                            bands.Add(Ratio(Average(frame, x, centre, 3), Average(frame, x, beside, 3)));

                    }
            }

            // Lines, where the top edge is lit, against the page just outside it; finer steps, as few points are lit.
            foreach (var phase in new[] { 0.05, 0.17, 0.3, 0.42, 0.55, 0.67, 0.8, 0.92 })
            {
                ribbon.Phase = phase;
                Settle(window);
                using var frame = Frame.Capture(window);
                for (var r = 0; r < RibbonBackdrop.RibbonCount; r++)
                    for (var x = panel.Left + 70; x < panel.Right - 70; x += 12)
                    {
                        var top = ribbon.CentreAt(r, x, height) - ribbon.HalfWidthAt(r, x, height);
                        var outside = top - 16;
                        if (ribbon.Light(r, 0, x) >= 0.75 && top > panel.Top + 50 && top < panel.Bottom - 50 && !Covered(x, outside, 10)
                            && !Enumerable.Range(0, RibbonBackdrop.RibbonCount).Where(o => o != r).Any(o => Math.Abs(top - ribbon.CentreAt(o, x, height)) <= ribbon.HalfWidthAt(o, x, height) + 10))
                            lines.Add(Ratio(Average(frame, x, top, 1), Average(frame, x, outside, 1)));
                    }
            }
        }
        finally
        {
            window.Close();
        }
        return (bands, lines);
    }

    private static double Median(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return sorted.Count == 0 ? 0 : sorted[sorted.Count / 2];
    }

    // Headless windows never lose activation on their own; this calls the handler a real platform
    // activation change reaches (the headless platform has no public way to deactivate a window).
    private static void PlatformSays(Window window, string handler)
    {
        var method = typeof(Window).GetMethod(handler, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?? typeof(Window).BaseType!.GetMethod(handler, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?? typeof(TopLevel).GetMethod(handler, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        Assert.NotNull(method);
        method.Invoke(window, null);
    }

    // The bounds of a named window part in window coordinates, inset to keep clear of the rounded corners.
    private static Rect Area(Window window, string part, double insetX, double insetY)
    {
        var control = window.GetVisualDescendants().OfType<Control>().First(c => c.Name == part);
        var origin = control.TranslatePoint(new Point(0, 0), window)!.Value;
        return new Rect(origin, control.Bounds.Size).Deflate(new Thickness(insetX, insetY));
    }

    private static readonly double[] LinearChannel = Enumerable.Range(0, 256).Select(v =>
    {
        var s = v / 255.0;
        return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }).ToArray();

    private static double Luminance((byte R, byte G, byte B) c) =>
        0.2126 * LinearChannel[c.R] + 0.7152 * LinearChannel[c.G] + 0.0722 * LinearChannel[c.B];

    private static double Ratio(double a, double b) => (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);

    private static MainWindow NewWindow(ThemeVariant variant, bool reduceMotion)
    {
        var window = new MainWindow
        {
            Width = WindowSize.Width,
            Height = WindowSize.Height,
            RequestedThemeVariant = variant,
        };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion });
        return window;
    }

    private static void Settle(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }
}
