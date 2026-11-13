using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Glass nests two levels deep; a third level and deeper draws the solid fallback.</summary>
public class GlassNestingTests
{
    // Glass surfaces, each inside the previous one, over the colourful backdrop.
    private static (Window Window, GlassBorder[] Levels) Nest(int depth, StandardsTheme? theme = null)
    {
        var levels = new GlassBorder[depth];
        Control? child = null;
        for (var i = depth - 1; i >= 0; i--)
        {
            levels[i] = new GlassBorder { Padding = new Thickness(24), Child = child };
            child = levels[i];
        }
        var window = new Window { Width = 360, Height = 300, Content = new Panel { Children = { Backdrop.Colourful(), levels[0] } } };
        if (theme is not null)
            window.Styles.Add(theme);
        window.Show();
        return (window, levels);
    }

    [AvaloniaFact]
    public void Levels1And2_AreGlass_Levels3And4_AreSolid()
    {
        var (window, levels) = Nest(4);
        Assert.Equal([false, false, true, true], levels.Select(g => g.IsSolid));
        window.Close();
    }

    [AvaloniaFact]
    public void Level3_DrawsTheSolidFill()
    {
        var (window, levels) = Nest(3);
        using var frame = Frame.Capture(window);
        var inner = levels[2].TranslatePoint(new Point(levels[2].Bounds.Width / 2, levels[2].Bounds.Height / 2), window)!.Value;
        Assert.Equal(((byte)255, (byte)255, (byte)255), frame[(int)inner.X, (int)inner.Y]);
        window.Close();
    }

    [AvaloniaFact]
    public void Reparenting_UpdatesTheLevel()
    {
        var (window, levels) = Nest(3);
        Assert.True(levels[2].IsSolid);

        // Moved out to level 1's child slot, the former third level is the second.
        levels[1].Child = null;
        levels[0].Child = levels[2];
        Assert.False(levels[2].IsSolid);

        // Moved back under the second level, it is the third again.
        levels[0].Child = levels[1];
        levels[1].Child = levels[2];
        Assert.True(levels[2].IsSolid);
        window.Close();
    }

    [AvaloniaFact]
    public void SolidAncestors_DoNotCount()
    {
        var (window, levels) = Nest(4);

        // The outer level on its popup-window fallback is solid, so the next two levels are glass.
        levels[0].InPopupWindow = true;
        Assert.Equal([true, false, false, true], levels.Select(g => g.IsSolid));

        levels[0].InPopupWindow = false;
        Assert.Equal([false, false, true, true], levels.Select(g => g.IsSolid));
        window.Close();
    }

    [AvaloniaFact]
    public void ReducedTransparency_KeepsEveryLevelSolid()
    {
        var theme = new StandardsTheme(new OsAccessibility.Settings(false, false)) { ReduceTransparency = true };
        var (window, levels) = Nest(4, theme);
        Assert.All(levels, g => Assert.True(g.IsSolid));

        theme.ReduceTransparency = false;
        Assert.Equal([false, false, true, true], levels.Select(g => g.IsSolid));
        window.Close();
    }
}
