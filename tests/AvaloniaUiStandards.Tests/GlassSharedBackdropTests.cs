using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// First-level glass in a StandardsWindow reads one shared backdrop sample: it looks like the glass that blurs the
/// backdrop itself (the order is blur, then refract, so they differ only in the refraction band), and overlays,
/// the full screen sheet and nested glass keep their own backdrop blur.
/// </summary>
public class GlassSharedBackdropTests
{
    private const double Width = 640, Height = 420;

    [AvaloniaFact]
    public void ContentPanel_ReadsTheSharedSample()
    {
        var window = Show(NewWindow());
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            Assert.True(panel.ReadsSharedSample);
            Assert.Same(Sampler(window), panel.Sampler);
        }
        finally
        {
            window.Close();
        }
    }

    // Against the panel's own backdrop blur, over the colourful backdrop with refraction and dispersion on: the flat
    // centre is within a few levels; the refraction band differs more where a sharp feature crosses the edge.
    [AvaloniaTheory]
    [InlineData("light", 1.0)]
    [InlineData("light", 2.0)]
    [InlineData("dark", 2.0)]
    public void SharedSample_LooksLikeTheOwnBlur(string theme, double scale)
    {
        var variant = theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var (before, after, panel) = BeforeAfter(() => NewWindow(variant), scale);
        using (before)
        using (after)
        {
            var (centre, band) = Compare(before, after, panel, scale, GlassBorder.DefaultRefractionEdge);
            SideBySide(before, after, $"glass-perf-final-panel-{theme}-{scale:0}x.png");
            Assert.True(centre.Mean < 1 && centre.Max <= 4, $"centre: {centre}");
            Assert.True(band.Mean < 4 && band.Max <= 24 && band.Over4 < 0.25, $"band: {band}");
        }
    }

    // Minimal glass has no refraction, so the order makes no difference: the two match within rounding.
    [AvaloniaFact]
    public void Minimal_MatchesTheOwnBlur()
    {
        var theme = new StandardsTheme(AccessibilitySettings.NotReduced) { GlassQuality = GlassQuality.Minimal };
        var (before, after, panel) = BeforeAfter(() =>
        {
            var w = NewWindow();
            w.Styles.Add(theme);
            return w;
        }, 2);
        using (before)
        using (after)
        {
            var (centre, band) = Compare(before, after, panel, 2, 0);
            SideBySide(before, after, "glass-perf-final-minimal-light-2x.png");
            Assert.True(Math.Max(centre.Max, band.Max) <= 4 && centre.Mean < 0.5, $"{centre} {band}");
        }
    }

    // The filter rim (inside an effect layer, or forced) draws over the shared fill as over the own one.
    [AvaloniaFact]
    public void FilterRim_OverTheSharedFill_LooksLikeOverTheOwnBlur()
    {
        var (before, after, panel) = BeforeAfter(() =>
        {
            var w = NewWindow(ThemeVariant.Dark);
            w.Opened += (_, _) => Part<GlassBorder>(w, StandardsWindowTheme.ContentPanelPart).SetValue(GlassBorder.RimMethodProperty, RimMethod.Filter);
            return w;
        }, 2);
        using (before)
        using (after)
        {
            var (centre, band) = Compare(before, after, panel, 2, GlassBorder.DefaultRefractionEdge);
            Assert.True(centre.Mean < 1 && band.Max <= 24, $"{centre} {band}");
        }
    }

    // An opaque document over most of the panel: the panel skips it and the backdrop is clipped around it, with the
    // same look where the glass shows.
    [AvaloniaFact]
    public void OpaqueDocument_SkippedAndClipped_LooksTheSame()
    {
        Border? doc = null;
        StandardsWindow Make(bool opaque)
        {
            doc = new Border { Background = Brushes.White, Width = 470, Height = 250, Child = new TextBlock { Text = "Opaque document", Margin = new Thickness(16) } };
            OpaqueRegion.SetIsOpaque(doc, opaque);
            return NewWindow(content: doc, scrollable: false);
        }
        var window = Make(false);
        window.SetRenderScaling(2);
        Show(window);
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            GlassBackdropSampler.Enabled = false;
            Pump(window);
            using var before = window.CaptureRenderedFrame()!;
            GlassBackdropSampler.Enabled = true;
            OpaqueRegion.SetIsOpaque(doc!, true);
            Pump(window);
            Assert.True(panel.ReadsSharedSample);
            Assert.Single(panel.Skip);
            Assert.NotNull(Part<Control>(window, StandardsWindowTheme.BackdropPart).Clip);
            using var after = window.CaptureRenderedFrame()!;
            var (centre, band) = Compare(before, after, PanelRect(window, 2), 2, GlassBorder.DefaultRefractionEdge);
            SideBySide(before, after, "glass-perf-final-document-light-2x.png");
            Assert.True(centre.Mean < 1 && band.Max <= 24, $"{centre} {band}");
        }
        finally
        {
            GlassBackdropSampler.Enabled = true;
            window.Close();
        }
    }

    // A redraw of a small region (a square on the backdrop under the panel changes colour) reads the kept sample:
    // inside the redrawn region the glass matches the same window drawn from scratch. (The fill's own backdrop blur
    // reads the last frame just outside the region and is off by up to about 20 levels there. Glass pixels outside
    // the redrawn region keep the last frame with either fill: Avalonia redraws only what changed.)
    [AvaloniaFact]
    public void PartialRedraw_ReadsTheKeptSample()
    {
        Border square = null!;
        StandardsWindow Make(IBrush fill)
        {
            square = new Border { Background = fill, Width = 40, Height = 40 };
            Canvas.SetLeft(square, 320);
            Canvas.SetTop(square, 200);
            var w = NewWindow();
            w.Backdrop = new Panel { Children = { Backdrop.Colourful(), new Canvas { Children = { square } } } };
            return w;
        }
        var window = Show(Make(Brushes.Black));
        var changed = square;
        var fresh = Show(Make(Brushes.Yellow));
        try
        {
            window.CaptureRenderedFrame()!.Dispose();
            changed.Background = Brushes.Yellow;
            Pump(window);
            using var redrawn = window.CaptureRenderedFrame()!;
            using var full = fresh.CaptureRenderedFrame()!;
            var at = changed.TranslatePoint(default, window)!.Value;
            var (inSquare, _) = Compare(full, redrawn, new PixelRect((int)at.X, (int)at.Y, 40, 40), 1, 0);
            Assert.True(inSquare.Max <= 1, $"inside the redrawn region: {inSquare}");
        }
        finally
        {
            window.Close();
            fresh.Close();
        }
    }

    [AvaloniaFact]
    public void NestedGlass_KeepsItsOwnBlur_AndTheNestingCap()
    {
        var third = new GlassBorder { Width = 80, Height = 60 };
        var second = new GlassBorder { Width = 200, Height = 140, Child = third };
        var window = Show(NewWindow(content: second));
        try
        {
            Assert.Same(Sampler(window), second.Sampler);
            Assert.False(second.ReadsSharedSample);
            Assert.True(second.Nested);
            Assert.True(third.IsSolid);
            Assert.False(third.ReadsSharedSample);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Overlays_AndTheFullScreenSheet_KeepTheirOwnBlur()
    {
        var window = Show(NewWindow());
        try
        {
            var overlay = new GlassBorder { Width = 200, Height = 120 };
            OverlayLayer.GetOverlayLayer(window)!.Children.Add(overlay);
            Pump(window);
            Assert.Null(overlay.Sampler);
            Assert.False(overlay.ReadsSharedSample);

            var sheet = Part<GlassBorder>(window, StandardsWindowTheme.RevealSheetPart);
            Assert.Null(sheet.Sampler);
            Assert.False(sheet.ReadsSharedSample);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(GlassQuality.Solid, false)]
    [InlineData(GlassQuality.Minimal, true)]
    [InlineData(GlassQuality.Full, true)]
    public void QualityTiers_DecideWhetherThePanelReadsTheSample(GlassQuality quality, bool reads)
    {
        var window = NewWindow();
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { GlassQuality = quality });
        Show(window);
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            Assert.Equal(reads, panel.ReadsSharedSample);
            Assert.Equal(reads ? 1 : 0, Sampler(window).Consumers.Count);
        }
        finally
        {
            window.Close();
        }
    }

    // Moving an opaque card changes what the panel skips, so a new sample operation is made each time: the window's
    // kept textures are reused, and the panel keeps reading the sample.
    [AvaloniaFact]
    public void KeptTextures_SurviveNewSampleOperations()
    {
        var doc = new Border { Background = Brushes.White, Width = 300, Height = 200 };
        OpaqueRegion.SetIsOpaque(doc, true);
        Canvas.SetTop(doc, 20);
        var window = Show(NewWindow(content: new Canvas { Children = { doc } }, scrollable: false));
        try
        {
            window.CaptureRenderedFrame()!.Dispose();
            var sample = Sampler(window).Sample;
            var generation = sample.Generation;
            Assert.NotNull(sample.Down);
            for (var i = 1; i <= 5; i++)
            {
                Canvas.SetLeft(doc, 30 * i);
                Pump(window);
                window.CaptureRenderedFrame()!.Dispose();
                Assert.Equal(generation, sample.Generation);
                Assert.NotEmpty(sample.Covered);
            }

            // A new size allocates them again.
            window.Width = 700;
            Pump(window);
            window.CaptureRenderedFrame()!.Dispose();
            Assert.Equal(generation + 1, sample.Generation);
        }
        finally
        {
            window.Close();
        }
    }

    // With no glass reading the sample (Solid), the kept textures are freed.
    [AvaloniaFact]
    public void KeptTextures_AreFreed_WhenNoGlassReadsTheSample()
    {
        var theme = new StandardsTheme(AccessibilitySettings.NotReduced);
        var window = NewWindow();
        window.Styles.Add(theme);
        Show(window);
        try
        {
            window.CaptureRenderedFrame()!.Dispose();
            var sample = Sampler(window).Sample;
            Assert.NotNull(sample.Down);
            theme.GlassQuality = GlassQuality.Solid;
            Pump(window);
            window.CaptureRenderedFrame()!.Dispose();
            Assert.Null(sample.Down);
            Assert.Null(sample.Image);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Switch_Off_DrawsEveryFillWithItsOwnBlur()
    {
        GlassBackdropSampler.Enabled = false;
        var window = Show(NewWindow());
        try
        {
            Assert.False(Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart).ReadsSharedSample);
            Assert.Empty(Sampler(window).Consumers);
        }
        finally
        {
            GlassBackdropSampler.Enabled = true;
            window.Close();
        }
    }

    // --- Helpers -------------------------------------------------------------------------

    internal readonly record struct Diff(double Mean, int Max, double Over4)
    {
        public override string ToString() => $"mean {Mean:F2}, max {Max}, {100 * Over4:F1} % over 4";
    }

    private static (WriteableBitmap Before, WriteableBitmap After, PixelRect Panel) BeforeAfter(Func<StandardsWindow> make, double scale)
    {
        var window = make();
        window.SetRenderScaling(scale);
        Show(window);
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            GlassBackdropSampler.Enabled = false;
            Pump(window);
            Assert.False(panel.ReadsSharedSample);
            var before = window.CaptureRenderedFrame()!;
            GlassBackdropSampler.Enabled = true;
            Pump(window);
            Assert.True(panel.ReadsSharedSample);
            var after = window.CaptureRenderedFrame()!;
            return (before, after, PanelRect(window, scale));
        }
        finally
        {
            GlassBackdropSampler.Enabled = true;
            window.Close();
        }
    }

    private static PixelRect PanelRect(StandardsWindow window, double scale)
    {
        var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
        var origin = panel.TranslatePoint(default, window)!.Value;
        return new PixelRect((int)Math.Round(origin.X * scale), (int)Math.Round(origin.Y * scale),
            (int)Math.Round(panel.Bounds.Width * scale), (int)Math.Round(panel.Bounds.Height * scale));
    }

    // Over the panel's pixels, inset 2 px from its edge (the rim): the flat centre and the refraction band apart.
    internal static (Diff Centre, Diff Band) Compare(WriteableBitmap a, WriteableBitmap b, PixelRect panel, double scale, double band)
    {
        using var fa = a.Lock();
        using var fb = b.Lock();
        var bytes = fa.RowBytes * fa.Size.Height;
        var x = new byte[bytes];
        var y = new byte[bytes];
        Marshal.Copy(fa.Address, x, 0, bytes);
        Marshal.Copy(fb.Address, y, 0, bytes);
        var inner = Inset(panel, (int)Math.Ceiling(2 * scale));
        var centreRect = Inset(panel, (int)Math.Ceiling((band + 2) * scale));
        long[] n = new long[2], over4 = new long[2];
        double[] sum = new double[2];
        int[] max = new int[2];
        for (var py = inner.Y; py < inner.Bottom; py++)
            for (var px = inner.X; px < inner.Right; px++)
            {
                var i = py * fa.RowBytes + px * 4;
                var d = Math.Max(Math.Abs(x[i] - y[i]), Math.Max(Math.Abs(x[i + 1] - y[i + 1]), Math.Abs(x[i + 2] - y[i + 2])));
                var k = centreRect.Contains(new PixelPoint(px, py)) ? 0 : 1;
                n[k]++;
                sum[k] += d;
                max[k] = Math.Max(max[k], d);
                if (d > 4)
                    over4[k]++;
            }
        Diff Make(int k) => n[k] == 0 ? new Diff(0, 0, 0) : new Diff(sum[k] / n[k], max[k], (double)over4[k] / n[k]);
        return (Make(0), Make(1));
    }

    private static PixelRect Inset(PixelRect r, int by) => new(r.X + by, r.Y + by, Math.Max(0, r.Width - 2 * by), Math.Max(0, r.Height - 2 * by));

    // Before | after | difference times 8 (white is none), with an 8 px gap, in the test run's screenshots folder.
    internal static void SideBySide(WriteableBitmap a, WriteableBitmap b, string name)
    {
        using var fa = a.Lock();
        using var fb = b.Lock();
        var w = fa.Size.Width;
        var h = fa.Size.Height;
        const int gap = 8;
        var outW = w * 3 + gap * 2;
        using var o = new WriteableBitmap(new PixelSize(outW, h), new Vector(96, 96), fa.Format, AlphaFormat.Premul);
        using (var fo = o.Lock())
        {
            var ra = new byte[w * 4];
            var rb = new byte[w * 4];
            var row = new byte[outW * 4];
            for (var py = 0; py < h; py++)
            {
                Marshal.Copy(fa.Address + py * fa.RowBytes, ra, 0, ra.Length);
                Marshal.Copy(fb.Address + py * fb.RowBytes, rb, 0, rb.Length);
                Array.Fill(row, (byte)255);
                Array.Copy(ra, 0, row, 0, ra.Length);
                Array.Copy(rb, 0, row, (w + gap) * 4, rb.Length);
                for (var i = 0; i < ra.Length; i++)
                    row[(2 * (w + gap)) * 4 + i] = i % 4 == 3 ? (byte)255 : (byte)(255 - Math.Min(255, Math.Abs(ra[i] - rb[i]) * 8));
                Marshal.Copy(row, 0, fo.Address + py * fo.RowBytes, row.Length);
            }
        }
        var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(dir);
        using var stream = File.Create(Path.Combine(dir, name));
        o.Save(stream, new PngBitmapEncoderOptions());
    }

    internal static StandardsWindow NewWindow(ThemeVariant? variant = null, object? content = null, bool scrollable = true) => new()
    {
        Title = "Glass",
        Width = Width,
        Height = Height,
        RequestedThemeVariant = variant ?? ThemeVariant.Light,
        WindowButtonStyle = WindowButtonStyle.CaptionButtons,
        Backdrop = Backdrop.Colourful(),
        IsContentScrollable = scrollable,
        Content = content ?? new TextBlock { Text = "Content panel text over the glass", FontSize = 16 },
    };

    internal static GlassBackdropSampler Sampler(Visual root) =>
        root.GetVisualDescendants().OfType<GlassBackdropSampler>().Single(s => s.Name == StandardsWindowTheme.BackdropSamplerPart);

    internal static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    internal static StandardsWindow Show(StandardsWindow window)
    {
        window.Show();
        Pump(window);
        return window;
    }

    internal static void Pump(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
