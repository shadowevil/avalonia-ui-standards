using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>The glass rim: takes in the backdrop's brightness (never its hue), inside the shape only.</summary>
public class GlassRimTests
{
    private const int Left = 40, Top = 30, Width = 160, Height = 100;

    // The rim is off by default (absorption 0); these tests and renders turn it on at this amount.
    private const double Absorption = 0.5;

    // The rim sits just inside the edge; the border is off so the rim alone is measured.
    private const int RimX = Left + 1, MidY = Top + Height / 2;

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Rim_TracksTheBackdropBrightness(string theme)
    {
        // Mid grey, not white: over white the light glass is already white and nothing can brighten it.
        var bright = RimOpacity(Color.Parse("#B0B0B0"), theme);
        var dark = RimOpacity(Color.Parse("#202020"), theme);
        Assert.True(bright > 2 * dark && bright > 0.1, $"Rim opacity over light grey {bright:F2} vs over dark {dark:F2}.");
        Assert.True(dark >= 0, $"The rim darkens the edge over a dark backdrop ({dark:F2}).");
    }

    // How strongly the rim colour shows at the rim: the luma change over the room up to the rim colour.
    private static double RimOpacity(Color backdrop, string theme)
    {
        var rim = theme == "Dark" ? GlassBorder.DefaultRimDark : GlassBorder.DefaultRimLight;
        using var without = Render(backdrop, theme, absorption: 0);
        using var with = Render(backdrop, theme, absorption: Absorption);
        var before = without.Luma(RimX, MidY);
        return (with.Luma(RimX, MidY) - before) / (0.2126 * rim.R + 0.7152 * rim.G + 0.0722 * rim.B - before);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Rim_TakesNoHueFromTheBackdrop(string theme)
    {
        var change = RimChange(Color.Parse("#E02020"), theme);
        Assert.True(change.Luma > 1, $"No rim over the red backdrop ({change.Luma:F1}).");
        // The neutral rim adds grey to the edge: the channel changes move together, not towards red.
        Assert.True(change.R <= change.G + 2 && change.R <= change.B + 2, $"The rim took the backdrop's hue: {change}.");
    }

    [AvaloniaFact]
    public void AccentRim_IsTheAccentAtTheBackdropBrightness()
    {
        var change = RimChange(Color.Parse("#B0B0B0"), "Light", rim: Color.Parse("#0050C0"));
        Assert.True(change.B > change.R + 5, $"The accent rim is not blue: {change}.");
    }

    [AvaloniaFact]
    public void Interior_IsUnchanged()
    {
        using var without = Render(Colors.White, "Light", absorption: 0);
        using var with = Render(Colors.White, "Light", absorption: 1);
        Assert.Equal(without[Left + Width / 2, MidY], with[Left + Width / 2, MidY]);
    }

    // Reduce transparency draws the solid fill as before: no sampling, no rim.
    [AvaloniaFact]
    public void Fallback_IsUnchanged()
    {
        using var without = Render(Colors.White, "Light", absorption: 0, reduce: true);
        using var with = Render(Colors.White, "Light", absorption: 1, reduce: true);
        AssertSame(without, with, (_, _) => true);
    }

    [AvaloniaFact]
    public void NothingDrawsOutsideTheShape()
    {
        using var without = Render(Colors.White, "Light", absorption: 0);
        using var with = Render(Colors.White, "Light", absorption: 1);
        // Outside the bounds, and in the corner tips outside the curve.
        AssertSame(without, with, (x, y) => x < Left || y < Top || x >= Left + Width || y >= Top + Height
            || (x - Left) + (y - Top) < 3 || (Left + Width - 1 - x) + (Top + Height - 1 - y) < 3);
    }

    [AvaloniaFact]
    public void Edge_RendersBeforeAndAfter()
    {
        string[] columns = ["Before (no rim)", "(a) Accent-tinted brightness", "(b) Neutral brightness (default)"];
        using var sheet = new Sheet($"Glass edge: the rim takes in the backdrop's brightness (absorption {Absorption}, rim {GlassDrawOperation.RimWidth} px, sampled over σ {GlassDrawOperation.RimSampleSigma} px)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            var rows = new List<(string, IReadOnlyList<Control>)>();
            foreach (var colourful in new[] { true, false })
            {
                var name = colourful ? "Colourful backdrop" : "Plain wash";
                rows.Add((name, [.. Candidates().Select(c => sheet.Cell(Scene(colourful, c, zoom: false), theme))]));
                rows.Add(($"{name}, bottom-left corner ×4", [.. Candidates().Select(c => sheet.Cell(Scene(colourful, c, zoom: true), theme))]));
            }
            sheet.AddSection(themeLabel, columns, rows);
        }
        sheet.Save("m3-glass-edge-before-after.png");
    }

    // --- Helpers -------------------------------------------------------------------------

    private enum Candidate { Before, Accent, Neutral }

    private static Candidate[] Candidates() => [Candidate.Before, Candidate.Accent, Candidate.Neutral];

    // A glass panel with a label over the backdrop; zoomed shows its bottom-left corner at 4×.
    private static Control Scene(bool colourful, Candidate candidate, bool zoom)
    {
        var glass = new GlassBorder
        {
            Width = 220, Height = 120,
            Padding = new Thickness(16),
            Child = new TextBlock { Text = "Glass panel", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center },
        };
        glass.RimAbsorption = candidate == Candidate.Before ? 0 : Absorption;
        if (candidate == Candidate.Accent)
            glass.Bind(GlassBorder.RimColorProperty, new DynamicResourceExtension("AccentTextColor"));
        Canvas.SetLeft(glass, 30);
        Canvas.SetTop(glass, 25);

        var backdrop = colourful ? Backdrop.Colourful() : new Border();
        if (!colourful)
            ((Border)backdrop).Bind(Border.BackgroundProperty, new DynamicResourceExtension("BackdropWash"));
        var scene = new Panel { Width = 280, Height = 170, Children = { backdrop, new Canvas { Children = { glass } } } };
        if (!zoom)
            return scene;

        scene.RenderTransformOrigin = RelativePoint.TopLeft;
        scene.RenderTransform = new TransformGroup { Children = { new ScaleTransform(4, 4), new TranslateTransform(-26 * 4, -107 * 4) } };
        return new Border { Width = 280, Height = 170, ClipToBounds = true, Child = new Canvas { Children = { scene } } };
    }

    // The colour change at the rim (with the rim minus without), per channel and in luma.
    private static (double R, double G, double B, double Luma) RimChange(Color backdrop, string theme, Color? rim = null)
    {
        using var without = Render(backdrop, theme, absorption: 0, rim: rim);
        using var with = Render(backdrop, theme, absorption: Absorption, rim: rim);
        var (r0, g0, b0) = without[RimX, MidY];
        var (r1, g1, b1) = with[RimX, MidY];
        return (r1 - r0, g1 - g0, b1 - b0, with.Luma(RimX, MidY) - without.Luma(RimX, MidY));
    }

    private static Frame Render(Color backdrop, string theme, double absorption, Color? rim = null, bool reduce = false)
    {
        var glass = new GlassBorder { Width = Width, Height = Height, BorderBrush = null, RimAbsorption = absorption };
        if (rim is { } colour)
            glass.SetValue(GlassBorder.RimColorProperty, colour);
        if (reduce)
            glass.SetValue(GlassBorder.ReduceTransparencyProperty, true);
        Canvas.SetLeft(glass, Left);
        Canvas.SetTop(glass, Top);
        var window = new Window
        {
            Width = 240, Height = 160,
            RequestedThemeVariant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light,
            Background = new SolidColorBrush(backdrop),
            Content = new Canvas { Children = { glass } },
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
