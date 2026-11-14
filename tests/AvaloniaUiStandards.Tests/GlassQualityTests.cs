using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// Glass quality: full glass at the first level, minimal glass (blur, tint and rim; no refraction or
/// dispersion) at the second level and on small surfaces, solid from the third; the app setting
/// makes every glass minimal or solid.
/// </summary>
public class GlassQualityTests
{
    private static (Window Window, GlassBorder[] Levels) Nest(int depth, StandardsTheme? theme = null)
    {
        var levels = new GlassBorder[depth];
        Control? child = null;
        for (var i = depth - 1; i >= 0; i--)
        {
            levels[i] = new GlassBorder { Padding = new Thickness(24), MinWidth = 100, MinHeight = 100, Child = child };
            child = levels[i];
        }
        var window = new Window { Width = 400, Height = 400, Content = new Panel { Children = { Backdrop.Colourful(), levels[0] } } };
        if (theme is not null)
            window.Styles.Add(theme);
        window.Show();
        return (window, levels);
    }

    [AvaloniaFact]
    public void Level1_IsFull_Level2_IsMinimal_Level3_IsSolid()
    {
        var (window, levels) = Nest(3);
        Assert.Equal([false, true, false], levels.Select(g => g.IsMinimal));
        Assert.Equal([false, false, true], levels.Select(g => g.IsSolid));
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(48, true)]
    [InlineData(63, true)]
    [InlineData(64, false)]
    [InlineData(200, false)]
    public void SmallSurfaces_AreMinimal(double shorterSide, bool minimal)
    {
        var glass = new GlassBorder { Width = 300, Height = shorterSide };
        var window = new Window { Width = 400, Height = 300, Content = glass };
        window.Show();
        Assert.Equal(minimal, glass.IsMinimal);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(GlassQuality.Full, false, false)]
    [InlineData(GlassQuality.Minimal, true, false)]
    [InlineData(GlassQuality.Solid, false, true)]
    public void AppSetting_AppliesToEveryLevel(GlassQuality quality, bool minimal, bool solid)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { GlassQuality = quality };
        var (window, levels) = Nest(1, theme);
        Assert.Equal(minimal, levels[0].IsMinimal);
        Assert.Equal(solid, levels[0].IsSolid);
        window.Close();
    }

    // With Minimal app-wide, every level that draws as glass is minimal (the second too, the third solid).
    [AvaloniaFact]
    public void MinimalSetting_KeepsTheNestingCap()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { GlassQuality = GlassQuality.Minimal };
        var (window, levels) = Nest(3, theme);
        Assert.Equal([true, true, false], levels.Select(g => g.IsMinimal));
        Assert.Equal([false, false, true], levels.Select(g => g.IsSolid));
        window.Close();
    }

    [AvaloniaFact]
    public void ReducedTransparency_IsSolid_WhateverTheQuality()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { GlassQuality = GlassQuality.Minimal, ReduceTransparency = true };
        var (window, levels) = Nest(1, theme);
        Assert.True(levels[0].IsSolid);
        Assert.False(levels[0].IsMinimal);
        window.Close();
    }

    // Minimal glass is the full glass without refraction and dispersion, pixel for pixel; full glass differs.
    [AvaloniaFact]
    public void MinimalGlass_IsFullGlassWithoutRefraction()
    {
        using var minimal = Render(g => g.SetValue(GlassBorder.QualityProperty, GlassQuality.Minimal));
        using var unrefracted = Render(g => { g.RefractionStrength = 0; g.ChromaticDispersion = 0; });
        using var full = Render(_ => { });
        var same = true;
        var differs = false;
        for (var y = 0; y < minimal.Height; y++)
            for (var x = 0; x < minimal.Width; x++)
            {
                same &= minimal[x, y] == unrefracted[x, y];
                differs |= minimal[x, y] != full[x, y];
            }
        Assert.True(same, "minimal glass is not the unrefracted glass");
        Assert.True(differs, "minimal glass looks like full glass");
    }

    [AvaloniaFact]
    public void FullAndMinimal_Render()
    {
        using var sheet = new Sheet("Glass quality: full (refraction, dispersion, blur, tint, rim) and minimal (blur, tint, rim)");
        var rows = new List<(string, IReadOnlyList<Control>)>();
        foreach (var (label, variant) in ButtonRenderTests.Themes)
        {
            Control Cell(GlassQuality q)
            {
                var glass = new GlassBorder { Width = 320, Height = 180, Child = new TextBlock { Text = q.ToString(), Margin = new Thickness(16) } };
                glass.SetValue(GlassBorder.QualityProperty, q);
                Canvas.SetLeft(glass, 20);
                Canvas.SetTop(glass, 20);
                return sheet.Cell(new Panel { Width = 360, Height = 220, Children = { Backdrop.Colourful(), new Canvas { Children = { glass } } } }, variant);
            }
            rows.Add((label, [Cell(GlassQuality.Full), Cell(GlassQuality.Minimal)]));
        }
        sheet.AddSection("Side by side", ["Full", "Minimal"], rows);
        sheet.Save("glass-quality.png");
    }

    private static Frame Render(Action<GlassBorder> setup)
    {
        var glass = new GlassBorder { Width = 300, Height = 200 };
        setup(glass);
        Canvas.SetLeft(glass, 40);
        Canvas.SetTop(glass, 40);
        var window = new Window { Width = 380, Height = 280, Content = new Panel { Children = { Backdrop.Colourful(), new Canvas { Children = { glass } } } } };
        var frame = Frame.Capture(window);
        window.Close();
        return frame;
    }
}
