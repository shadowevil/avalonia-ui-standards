using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>Button's siblings: RepeatButton, ToggleButton, HyperlinkButton, DropDownButton, SplitButton.</summary>
public class SiblingThemeTests
{
    public static TheoryData<string> Siblings => new()
    {
        nameof(RepeatButton), nameof(ToggleButton), nameof(HyperlinkButton), nameof(DropDownButton), nameof(SplitButton),
    };

    [AvaloniaTheory]
    [MemberData(nameof(Siblings))]
    public void Theme_AppliesToSibling(string sibling)
    {
        var (_, control) = Show(Create(sibling));

        Assert.Equal(new CornerRadius(8), ((TemplatedControl)control).CornerRadius);
        Assert.All(Chromes(control), chrome => Assert.Equal(0.6, chrome.Smoothing));
        Assert.NotEmpty(Chromes(control));
    }

    [AvaloniaFact]
    public void SplitButton_PartsKeepOnlyTheirOuterCorners()
    {
        var (_, split) = Show(Create(nameof(SplitButton)));
        var (primary, secondary) = Parts(split);

        Assert.Equal(new CornerRadius(8, 0, 0, 8), Chrome(primary).CornerRadius);
        Assert.Equal(new CornerRadius(0, 8, 8, 0), Chrome(secondary).CornerRadius);
        Assert.Equal(new Thickness(1, 1, 0, 1), primary.BorderThickness);
        Assert.Equal(new Thickness(0, 1, 1, 1), secondary.BorderThickness);
        Assert.False(primary.Focusable);
        Assert.False(secondary.Focusable);
    }

    [AvaloniaTheory]
    [InlineData(nameof(RepeatButton), "Normal")]
    [InlineData(nameof(RepeatButton), "Compact")]
    [InlineData(nameof(ToggleButton), "Normal")]
    [InlineData(nameof(ToggleButton), "Compact")]
    [InlineData(nameof(HyperlinkButton), "Normal")]
    [InlineData(nameof(HyperlinkButton), "Compact")]
    [InlineData(nameof(DropDownButton), "Normal")]
    [InlineData(nameof(DropDownButton), "Compact")]
    [InlineData(nameof(SplitButton), "Normal")]
    [InlineData(nameof(SplitButton), "Compact")]
    public void Size_FollowsDensity(string sibling, string density)
    {
        var compact = density == "Compact";
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = compact ? DensityStyle.Compact : DensityStyle.Normal };
        var (_, control) = Show(Create(sibling), theme: theme);
        double height = compact ? 24 : 32;
        var padding = compact ? new Thickness(8, 2, 8, 2) : new Thickness(12, 4, 12, 4);

        Assert.Equal(height, control.Bounds.Height);
        Assert.Equal(padding, ((TemplatedControl)control).Padding);
        if (control is DropDownButton)
            Assert.Equal(new Thickness(0, 0, padding.Right, 0), Glyph(control).Margin);
        if (control is SplitButton)
            Assert.Equal(height, Parts(control).Secondary.Bounds.Width);
    }

    [AvaloniaFact]
    public void Size_FollowsRuntimeDensityChanges()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var (window, split) = Show(Create(nameof(SplitButton)), theme: theme);
        Assert.Equal(32, split.Bounds.Height);

        theme.Density = DensityStyle.Compact;
        window.UpdateLayout();
        Assert.Equal(24, split.Bounds.Height);
        Assert.Equal(24, Parts(split).Secondary.Bounds.Width);
    }

    // Expected fill and text resource per sibling, setup and state (null fill = transparent).
    public static TheoryData<string, string, string, string?, string> States()
    {
        var data = new TheoryData<string, string, string, string?, string>();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            data.Add(nameof(RepeatButton), theme, "rest", "RepeatButtonBackground", "RepeatButtonForeground");
            data.Add(nameof(RepeatButton), theme, ":pointerover", "RepeatButtonBackgroundPointerOver", "RepeatButtonForegroundPointerOver");
            data.Add(nameof(RepeatButton), theme, ":pressed", "RepeatButtonBackgroundPressed", "RepeatButtonForegroundPressed");
            data.Add(nameof(ToggleButton), theme, "rest", "ToggleButtonBackground", "ToggleButtonForeground");
            data.Add(nameof(ToggleButton), theme, ":pointerover", "ToggleButtonBackgroundPointerOver", "ToggleButtonForegroundPointerOver");
            data.Add(nameof(ToggleButton), theme, ":pressed", "ToggleButtonBackgroundPressed", "ToggleButtonForegroundPressed");
            data.Add("ToggleButton:checked", theme, "rest", "AccentFillColor", "ToggleButtonForegroundChecked");
            data.Add("ToggleButton:checked", theme, ":pointerover", "AccentFillColorPointerOver", "ToggleButtonForegroundCheckedPointerOver");
            data.Add("ToggleButton:checked", theme, ":pressed", "AccentFillColorPressed", "ToggleButtonForegroundCheckedPressed");
            data.Add("ToggleButton:checked", theme, ":disabled", "ToggleButtonBackgroundCheckedDisabled", "ToggleButtonForegroundCheckedDisabled");
            data.Add("ToggleButton:indeterminate", theme, "rest", "ToggleButtonBackgroundIndeterminate", "ToggleButtonForegroundIndeterminate");
            data.Add(nameof(HyperlinkButton), theme, "rest", null, "AccentTextColor");
            data.Add(nameof(HyperlinkButton), theme, ":pointerover", null, "AccentTextColor");
            data.Add(nameof(HyperlinkButton), theme, ":pressed", null, "AccentTextColorPressed");
            data.Add("HyperlinkButton:visited", theme, "rest", null, "HyperlinkButtonForegroundVisited");
            data.Add("HyperlinkButton:visited", theme, ":pointerover", null, "HyperlinkButtonForegroundVisitedPointerOver");
            data.Add("HyperlinkButton:visited", theme, ":pressed", null, "HyperlinkButtonForegroundVisitedPressed");
            data.Add(nameof(DropDownButton), theme, "rest", "ButtonBackground", "ButtonForeground");
            data.Add(nameof(DropDownButton), theme, ":pointerover", "ButtonBackgroundPointerOver", "ButtonForegroundPointerOver");
            data.Add(nameof(DropDownButton), theme, ":pressed", "ButtonBackgroundPressed", "ButtonForegroundPressed");
            data.Add(nameof(SplitButton), theme, "rest", "SplitButtonBackground", "SplitButtonForeground");
            data.Add(nameof(SplitButton), theme, ":pointerover", "SplitButtonBackgroundPointerOver", "SplitButtonForegroundPointerOver");
            data.Add(nameof(SplitButton), theme, ":pressed", "SplitButtonBackgroundPressed", "SplitButtonForegroundPressed");
            data.Add(nameof(SplitButton), theme, ":flyout-open", "SplitButtonBackgroundPressed", "SplitButtonForegroundPressed");
            data.Add("ToggleSplitButton:checked", theme, "rest", "AccentFillColor", "SplitButtonForegroundChecked");
            data.Add("ToggleSplitButton:checked", theme, ":pointerover", "AccentFillColorPointerOver", "SplitButtonForegroundCheckedPointerOver");
            data.Add("ToggleSplitButton:checked", theme, ":pressed", "AccentFillColorPressed", "SplitButtonForegroundCheckedPressed");
        }
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(States))]
    public void StateColours_ResolvePerSiblingAndState(string setup, string theme, string state, string? fillKey, string textKey)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, control) = Show(Create(setup), variant);
        var (fill, text) = StateColours(window, control, state);

        Assert.Equal(fillKey is null ? Colors.Transparent : Effective(Resolve(window, fillKey, variant)), fill);
        Assert.Equal(Effective(Resolve(window, textKey, variant)), text);
    }

    // Every sibling's text meets 4.5:1 over its fill, on the page and on cards, in every enabled state.
    public static TheoryData<string, string> ContrastCases()
    {
        var data = new TheoryData<string, string>();
        foreach (var setup in new[] { nameof(RepeatButton), nameof(ToggleButton), "ToggleButton:checked", "ToggleButton:indeterminate",
                     nameof(HyperlinkButton), "HyperlinkButton:visited", nameof(DropDownButton), nameof(SplitButton), "ToggleSplitButton:checked" })
            foreach (var theme in new[] { "Light", "Dark" })
                data.Add(setup, theme);
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(ContrastCases))]
    public void Text_MeetsAaContrast(string setup, string theme)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        foreach (var surfaceKey in new[] { "SystemRegionColor", "SystemChromeMediumLowColor" })
            foreach (var state in new[] { "rest", ":pointerover", ":pressed" })
            {
                var (window, control) = Show(Create(setup), variant);
                var surface = Effective(Resolve(window, surfaceKey, variant));
                var (fill, text) = StateColours(window, control, state);
                var under = Over(fill, surface);
                var ratio = AccessibleColors.ContrastRatio(Over(text, under), under);
                Assert.True(ratio >= 4.5, $"{setup} {state} on {surfaceKey}: {ratio:F2}:1");
            }
    }

    [AvaloniaTheory]
    [InlineData("#007CEA")]
    [InlineData("#FF8C00")]
    [InlineData("#7A00CC")]
    [InlineData("#00B7C3")]
    public void AccentText_MeetsAaOnPageAndCards_ForAnyAccent(string accent)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Accent = Color.Parse(accent) };
        foreach (var (variant, surfaces) in new[]
                 {
                     (ThemeVariant.Light, new[] { "#F5F5F5", "#FFFFFF" }),
                     (ThemeVariant.Dark, new[] { "#1E1E1E", "#2A2A2A" }),
                 })
            foreach (var key in new[] { "AccentTextColor", "AccentTextColorPressed" })
            {
                Assert.True(theme.TryGetResource(key, variant, out var value), key);
                foreach (var surface in surfaces)
                    Assert.True(AccessibleColors.ContrastRatio((Color)value!, Color.Parse(surface)) >= 4.5, $"{accent} {key} {variant} on {surface}");
            }
    }

    [AvaloniaFact]
    public void HyperlinkButton_IsUnderlined_WithHandCursor()
    {
        var (_, link) = Show(Create(nameof(HyperlinkButton)));
        Assert.Equal(TextDecorations.Underline, link.GetValue(Inline.TextDecorationsProperty));
        Assert.Equal(StandardCursorType.Hand.ToString(), link.Cursor?.ToString());
    }

    [AvaloniaFact]
    public void DropDownGlyph_FollowsFluentColours()
    {
        var (window, dropDown) = Show(Create(nameof(DropDownButton)));
        Assert.Equal(Effective(Resolve(window, "SystemControlForegroundBaseMediumHighBrush", ThemeVariant.Light)), Effective(Glyph(dropDown).Foreground));

        ((IPseudoClasses)dropDown.Classes).Set(":pressed", true);
        window.UpdateLayout();
        Assert.Equal(Effective(Resolve(window, "ButtonForegroundPressed", ThemeVariant.Light)), Effective(Glyph(dropDown).Foreground));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Siblings))]
    public void Focus_ShowsAccentRing(string sibling)
    {
        var (_, control) = Show(Create(sibling));
        control.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();

        var ring = Assert.IsType<SquircleBorder>(Assert.Single(AdornerLayer.GetAdornerLayer(control)!.Children));
        Assert.Equal(new CornerRadius(12), ring.CornerRadius);
        Assert.Equal(new Thickness(-4), ring.Margin);
    }

    // Creates a sibling; "Type:state" also sets that persistent state (checked, indeterminate, visited).
    private static Control Create(string setup) => setup switch
    {
        nameof(RepeatButton) => new RepeatButton { Content = "Repeat" },
        nameof(ToggleButton) => new ToggleButton { Content = "Toggle" },
        "ToggleButton:checked" => new ToggleButton { Content = "Toggle", IsChecked = true },
        "ToggleButton:indeterminate" => new ToggleButton { Content = "Toggle", IsThreeState = true, IsChecked = null },
        nameof(HyperlinkButton) => new HyperlinkButton { Content = "Link" },
        "HyperlinkButton:visited" => new HyperlinkButton { Content = "Link", IsVisited = true },
        nameof(DropDownButton) => new DropDownButton { Content = "Drop" },
        nameof(SplitButton) => new SplitButton { Content = "Split" },
        "ToggleSplitButton:checked" => new ToggleSplitButton { Content = "Split", IsChecked = true },
        _ => throw new ArgumentException(setup),
    };

    // The base fill of the control's (or SplitButton primary part's) chrome and the effective text colour, in a state.
    private static (Color Fill, Color Text) StateColours(Window window, Control control, string state)
    {
        // SplitButton's :flyout-open is on the control; its other states are on the primary part.
        var target = control is SplitButton split && state != ":flyout-open" ? Parts(split).Primary : (TemplatedControl)control;
        if (state == ":disabled")
            control.IsEnabled = false;
        else if (state != "rest")
            ((IPseudoClasses)target.Classes).Set(state, true);
        window.UpdateLayout();

        var chrome = Chrome(control is SplitButton parts ? Parts(parts).Primary : (TemplatedControl)control);
        var presenter = (ContentPresenter)chrome.GetVisualDescendants().First(v => v is ContentPresenter { Name: ButtonParts.ContentPart });
        var fill = chrome.GetBaseValue(SquircleBorder.BackgroundProperty) is { HasValue: true } set ? Effective(set.Value) : Effective(chrome.Background);
        return (fill, Effective(presenter.Foreground));
    }

    private static (Window Window, Control Control) Show(Control control, ThemeVariant? variant = null, StandardsTheme? theme = null)
    {
        var window = new Window { Width = 300, Height = 120, RequestedThemeVariant = variant ?? ThemeVariant.Light, Content = control };
        if (theme is not null)
            window.Styles.Add(theme);
        window.Show();
        return (window, control);
    }

    private static IEnumerable<SquircleBorder> Chromes(Control control) =>
        control.GetVisualDescendants().OfType<SquircleBorder>().Where(b => b.Name == ButtonParts.ChromePart);

    private static SquircleBorder Chrome(TemplatedControl control) =>
        (SquircleBorder)control.GetVisualChildren().Single(v => v is SquircleBorder { Name: ButtonParts.ChromePart });

    private static (Button Primary, Button Secondary) Parts(Control split)
    {
        var parts = split.GetVisualDescendants().OfType<Button>().ToList();
        return (parts.Single(b => b.Name == SplitButtonTheme.PrimaryPart), parts.Single(b => b.Name == SplitButtonTheme.SecondaryPart));
    }

    private static PathIcon Glyph(Control dropDown) =>
        dropDown.GetVisualDescendants().OfType<PathIcon>().Single(p => p.Name == DropDownButtonTheme.GlyphPart);

    private static object Resolve(Window window, string key, ThemeVariant variant)
    {
        Assert.True(window.TryFindResource(key, variant, out var value), key);
        return value!;
    }
}
