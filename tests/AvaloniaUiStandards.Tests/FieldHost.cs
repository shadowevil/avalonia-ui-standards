using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>Shows a field Block in a window and reads its parts and colours, for the field theme tests.</summary>
internal static class FieldHost
{
    public static Window Show(Control control, ThemeVariant? variant = null, StandardsTheme? theme = null)
    {
        var window = new Window { Width = 400, Height = 300, RequestedThemeVariant = variant ?? ThemeVariant.Light, Content = control };
        if (theme is not null)
            window.Styles.Add(theme);
        window.Show();
        Pump(window);
        return window;
    }

    public static ThemeVariant Variant(string theme) => theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

    public static void Pump(TopLevel top)
    {
        Dispatcher.UIThread.RunJobs();
        top.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    public static T Part<T>(Visual control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    public static SquircleBorder Chrome(Visual control) => Part<SquircleBorder>(control, FieldParts.ChromePart);

    public static SquircleBorder Ring(Visual control) =>
        control.GetVisualDescendants().OfType<SquircleBorder>().First(c => c.Name == FocusRing.Part && c.TemplatedParent == control);

    public static object Resolve(Control control, string key)
    {
        Assert.True(control.TryFindResource(key, control.ActualThemeVariant, out var value), key);
        return value!;
    }

    public static Color Colour(Control control, string key) => Effective(Resolve(control, key));

    /// <summary>
    /// The surfaces a field sits on, as 0011 measures text: the page, the card, the backdrop wash's
    /// two ends, and the glass panel's tint (at GlassTintOpacity) over each end, as StandardsWindowTests does.
    /// </summary>
    public static IEnumerable<(string Name, Color Colour)> Surfaces(Control control)
    {
        var tint = Effective(Resolve(control, "GlassTintColor"));
        var opacity = (double)Resolve(control, "GlassTintOpacity");
        var start = Colour(control, "BackdropWashStartColor");
        var end = Colour(control, "BackdropWashEndColor");
        yield return ("page", Colour(control, "SystemRegionColor"));
        yield return ("card", Colour(control, "SystemChromeMediumLowColor"));
        yield return ("wash start", start);
        yield return ("wash end", end);
        yield return ("glass over wash start", StandardsTheme.Mix(start, tint, opacity));
        yield return ("glass over wash end", StandardsTheme.Mix(end, tint, opacity));
    }

    /// <summary>The non-text contrast floor for a field's boundary (WCAG 1.4.11).</summary>
    public const double NonTextContrast = 3;
}
