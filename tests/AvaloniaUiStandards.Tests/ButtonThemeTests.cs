using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
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

public class ButtonThemeTests
{
    [AvaloniaFact]
    public void Theme_AppliesToButton()
    {
        var (_, button) = Show(variant: null);

        // Template root: the chrome and, beside it, the (hidden) focus ring.
        var root = Assert.IsType<Panel>(Assert.Single(button.GetVisualChildren()));
        var chrome = Assert.IsType<SquircleBorder>(root.Children[0]);
        Assert.Equal(ButtonParts.ChromePart, chrome.Name);
        Assert.Equal(FocusRing.Part, Assert.IsType<SquircleBorder>(root.Children[1]).Name);
        var presenter = Assert.IsType<ContentPresenter>(chrome.Child);
        Assert.Equal(ButtonParts.ContentPart, presenter.Name);
        Assert.Equal(new CornerRadius(8), button.CornerRadius);
        Assert.Equal(0.6, chrome.Smoothing);
    }

    [AvaloniaFact]
    public void PaddingAndHeight_SitOnTheGrid()
    {
        var (_, button) = Show(variant: null);

        Assert.Equal(new Thickness(12, 4, 12, 4), button.Padding);
        Assert.Equal(32, button.MinHeight);
        Assert.Equal(32, button.Bounds.Height);
    }

    // Expected resource per variant and state, as wired by the theme (null key = transparent).
    public static TheoryData<string?, string, string, string?> Backgrounds()
    {
        var data = new TheoryData<string?, string, string, string?>();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            data.Add(null, theme, "rest", "ButtonBackground");
            data.Add(null, theme, ":pointerover", "ButtonBackgroundPointerOver");
            data.Add(null, theme, ":pressed", "ButtonBackgroundPressed");
            data.Add("accent", theme, "rest", "AccentFillColor");
            data.Add("accent", theme, ":pointerover", "AccentFillColorPointerOver");
            data.Add("accent", theme, ":pressed", "AccentFillColorPressed");
            data.Add("subtle", theme, "rest", null);
            data.Add("subtle", theme, ":pointerover", "ButtonBackgroundPointerOver");
            data.Add("subtle", theme, ":pressed", "ButtonBackgroundPressed");
            data.Add("destructive", theme, "rest", "DangerFillColor");
            data.Add("destructive", theme, ":pointerover", "DangerFillColorPointerOver");
            data.Add("destructive", theme, ":pressed", "DangerFillColorPressed");
        }
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Backgrounds))]
    public void Background_ResolvesPerVariantAndState(string? variant, string theme, string state, string? expectedKey)
    {
        var themeVariant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, button) = Show(variant, themeVariant);
        if (state != "rest")
            ((IPseudoClasses)button.Classes).Set(state, true);
        window.UpdateLayout();

        var actual = Effective(Chrome(button).GetBaseValue(SquircleBorder.BackgroundProperty).Value as IBrush);
        var expected = expectedKey is null ? Colors.Transparent : Effective(Resolve(window, expectedKey, themeVariant));
        Assert.Equal(expected, actual);
    }

    // Filled variants: text over each state's fill, in both themes, meets WCAG AA; hover is
    // darker than rest and pressed darker again.
    [AvaloniaTheory]
    [InlineData("accent", "Light")]
    [InlineData("accent", "Dark")]
    [InlineData("destructive", "Light")]
    [InlineData("destructive", "Dark")]
    public void FilledVariants_MeetAaContrast_AndDarkenOnHoverAndPress(string variant, string theme)
    {
        var themeVariant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (rest, restText) = StateColours(variant, themeVariant, null);
        var (hover, hoverText) = StateColours(variant, themeVariant, ":pointerover");
        var (pressed, pressedText) = StateColours(variant, themeVariant, ":pressed");

        Assert.True(AccessibleColors.ContrastRatio(restText, rest) >= 4.5, $"rest {rest}");
        Assert.True(AccessibleColors.ContrastRatio(hoverText, hover) >= 4.5, $"hover {hover}");
        Assert.True(AccessibleColors.ContrastRatio(pressedText, pressed) >= 4.5, $"pressed {pressed}");
        Assert.True(AccessibleColors.RelativeLuminance(hover) < AccessibleColors.RelativeLuminance(rest));
        Assert.True(AccessibleColors.RelativeLuminance(pressed) < AccessibleColors.RelativeLuminance(hover));
    }

    [AvaloniaFact]
    public void BrandColours_StayAsSet_AndFillsAreDerivedFromThem()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        Assert.True(theme.TryGetResource("SystemAccentColor", ThemeVariant.Light, out var accent));
        Assert.True(theme.TryGetResource("DangerColor", ThemeVariant.Light, out var danger));
        Assert.Equal(Color.Parse("#007CEA"), accent);
        Assert.Equal(Color.Parse("#E1332A"), danger);
        Assert.Equal(AccessibleColors.ForWhiteText((Color)accent!).Normal, Resource<Color>(theme, "AccentFillColor"));
        Assert.Equal(AccessibleColors.ForWhiteText((Color)danger!).Normal, Resource<Color>(theme, "DangerFillColor"));
        Assert.Equal(Colors.White, Resource<Color>(theme, "DangerForeground"));
    }

    [AvaloniaTheory]
    [InlineData("#FF8C00")]
    [InlineData("#00B7C3")]
    [InlineData("#0078D4")]
    [InlineData("#7A00CC")]
    public void AccentOverride_KeepsAaContrast(string accent)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Accent = Color.Parse(accent) };
        foreach (var key in new[] { "AccentFillColor", "AccentFillColorPointerOver", "AccentFillColorPressed" })
            Assert.True(AccessibleColors.ContrastRatio(Colors.White, Resource<Color>(theme, key)) >= 4.5, key);
    }

    // Standard and Subtle text over their (possibly translucent) fills, on the region and on a card.
    [AvaloniaTheory]
    [InlineData(null, "Light")]
    [InlineData(null, "Dark")]
    [InlineData("subtle", "Light")]
    [InlineData("subtle", "Dark")]
    public void StandardAndSubtle_MeetAaContrast(string? variant, string theme)
    {
        var themeVariant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        foreach (var surfaceKey in new[] { "SystemRegionColor", "SystemChromeMediumLowColor" })
            foreach (var state in new string?[] { null, ":pointerover", ":pressed" })
            {
                var (window, button) = Show(variant, themeVariant);
                var surface = Effective(Resolve(window, surfaceKey, themeVariant));
                if (state is not null)
                    ((IPseudoClasses)button.Classes).Set(state, true);
                window.UpdateLayout();

                var fill = Over(Effective(Chrome(button).GetBaseValue(SquircleBorder.BackgroundProperty).Value as IBrush), surface);
                // Text has no transition, so its effective value is its end value (at rest it is inherited).
                var text = Over(Effective(Content(button).Foreground), fill);
                Assert.True(AccessibleColors.ContrastRatio(text, fill) >= 4.5, $"{state ?? "rest"} on {surfaceKey}: {text} over {fill}");
            }
    }

    [AvaloniaFact]
    public void Destructive_UsesDangerForeground()
    {
        var (window, button) = Show("destructive");
        window.UpdateLayout();
        Assert.Equal(Colors.White, Effective(Content(button).GetBaseValue(ContentPresenter.ForegroundProperty).Value as IBrush));
    }

    [AvaloniaFact]
    public void CompactDensity_Is24High_WithSpacingSSides()
    {
        var (_, button) = Show(variant: null, theme: new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = DensityStyle.Compact });

        Assert.Equal(new Thickness(8, 2, 8, 2), button.Padding);
        Assert.Equal(24, button.MinHeight);
        Assert.Equal(24, button.Bounds.Height);
    }

    [AvaloniaFact]
    public void Density_FollowsRuntimeChanges()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var (window, button) = Show(variant: null, theme: theme);
        Assert.Equal(32, button.Bounds.Height);

        theme.Density = DensityStyle.Compact;
        window.UpdateLayout();
        Assert.Equal(new Thickness(8, 2, 8, 2), button.Padding);
        Assert.Equal(24, button.Bounds.Height);

        theme.Density = DensityStyle.Normal;
        window.UpdateLayout();
        Assert.Equal(new Thickness(12, 4, 12, 4), button.Padding);
        Assert.Equal(32, button.Bounds.Height);
    }

    [AvaloniaFact]
    public void Disabled_FollowsFluentDisabledColours()
    {
        var (window, button) = Show("accent");
        button.IsEnabled = false;
        window.UpdateLayout();

        Assert.Equal(Effective(Resolve(window, "AccentButtonBackgroundDisabled", ThemeVariant.Light)),
            Effective(Chrome(button).GetBaseValue(SquircleBorder.BackgroundProperty).Value as IBrush));
        Assert.Equal(Effective(Resolve(window, "AccentButtonForegroundDisabled", ThemeVariant.Light)),
            Effective(Content(button).GetBaseValue(ContentPresenter.ForegroundProperty).Value as IBrush));
    }

    [AvaloniaFact]
    public void Transitions_UseMotionFast_AndBecomeInstantUnderReduceMotion()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var (window, button) = Show(variant: null, theme: theme);
        var transitions = Chrome(button).Transitions!.Cast<TransitionBase>().ToList();

        Assert.Equal(3, transitions.Count);
        Assert.All(transitions, t => Assert.Equal(TimeSpan.FromMilliseconds(100), t.Duration));
        Assert.All(transitions, t => Assert.IsType<Avalonia.Animation.Easings.CubicEaseOut>(t.Easing));

        theme.ReduceMotion = true;
        window.UpdateLayout();
        Assert.Empty(Chrome(button).Transitions!);

        theme.ReduceMotion = false;
        window.UpdateLayout();
        Assert.Equal(3, Chrome(button).Transitions!.Count);
    }

    [AvaloniaFact]
    public void Transitions_KeepTheirEasing_WhenTheButtonLeavesTheTree()
    {
        var host = new ContentControl();
        var (window, button) = Show(variant: null);
        window.Content = host;
        host.Content = button;
        window.UpdateLayout();
        var transitions = Chrome(button).Transitions!.Cast<TransitionBase>().ToList();

        host.Content = null;
        Assert.All(transitions, t => Assert.NotNull(t.Easing));
        Assert.All(transitions, t => Assert.Equal(TimeSpan.FromMilliseconds(100), t.Duration));
    }

    [AvaloniaFact]
    public void Pressed_ScalesTheChrome()
    {
        var (window, button) = Show(variant: null);
        ((IPseudoClasses)button.Classes).Set(":pressed", true);
        window.UpdateLayout();

        var transform = Chrome(button).GetBaseValue(Visual.RenderTransformProperty).Value as ITransform;
        Assert.Equal(0.98, transform!.Value.M11, 3);
    }

    [AvaloniaFact]
    public void Focus_ShowsAccentRingWithGapAroundTheButton()
    {
        var (window, button) = Show(variant: null);
        var ring = Ring(button);
        Assert.False(ring.IsVisible);

        button.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();

        // Drawn in the button's own template (so its ancestors clip it), not in the adorner layer.
        Assert.True(ring.IsVisible);
        Assert.Empty(AdornerLayer.GetAdornerLayer(button)!.Children);
        Assert.Equal(new CornerRadius(12), ring.CornerRadius);
        Assert.Equal(new Thickness(-4), ring.Margin);
        Assert.Equal(new Thickness(2), ring.BorderThickness);
        Assert.Equal(Color.Parse("#007CEA"), ((ISolidColorBrush)ring.BorderBrush!).Color);
        Assert.Equal(new Rect(-4, -4, button.Bounds.Width + 8, button.Bounds.Height + 8), ring.Bounds);
        Assert.Equal(1, button.ZIndex);
    }

    private static (Window Window, Button Button) Show(string? variant, ThemeVariant? themeVariant = null, StandardsTheme? theme = null)
    {
        var button = new Button { Content = "Button" };
        if (variant is not null)
            button.Classes.Add(variant);
        var window = new Window
        {
            Width = 200, Height = 100,
            RequestedThemeVariant = themeVariant ?? ThemeVariant.Light,
            Content = button,
        };
        if (theme is not null)
            window.Styles.Add(theme);
        window.Show();
        return (window, button);
    }

    [AvaloniaFact]
    public void FocusRing_FollowsCornerRadiusChanges()
    {
        var (window, button) = Show(variant: null);
        button.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        var ring = Ring(button);
        Assert.Equal(new CornerRadius(12), ring.CornerRadius);

        button.CornerRadius = new CornerRadius(4);
        Assert.Equal(new CornerRadius(8), ring.CornerRadius);
    }

    // Fluent controls whose templates hold button-family controls keep Fluent's look for them
    // (0007): their plain Buttons set an explicit theme, and the subclasses they use either set
    // one too or have theme keys of their own. The ListBox, ListBoxItem, ComboBoxItem and
    // ScrollViewer restyles apply to every instance, inside Fluent templates as well; the picker
    // presenters' item containers have theme keys of their own and keep Fluent's look. Expander
    // has the library's own theme (0033), so it is not one of them.
    public static TheoryData<string> FluentControlsWithButtons => new()
    {
        nameof(CalendarDatePicker), nameof(Calendar), nameof(DatePicker), nameof(TimePicker),
        nameof(DatePickerPresenter), nameof(TimePickerPresenter), nameof(NumericUpDown), nameof(ButtonSpinner),
        nameof(ComboBox), nameof(ScrollBar), nameof(Slider), nameof(TreeView),
    };

    [AvaloniaTheory]
    [MemberData(nameof(FluentControlsWithButtons))]
    public void FluentTemplates_DoNotPickUpLibraryButtonThemes(string name)
    {
        Control control = name switch
        {
            nameof(CalendarDatePicker) => new CalendarDatePicker(),
            nameof(Calendar) => new Calendar(),
            nameof(DatePicker) => new DatePicker(),
            nameof(TimePicker) => new TimePicker(),
            nameof(DatePickerPresenter) => new DatePickerPresenter(),
            nameof(TimePickerPresenter) => new TimePickerPresenter(),
            nameof(NumericUpDown) => new NumericUpDown(),
            nameof(ButtonSpinner) => new ButtonSpinner { Content = "Spin" },
            nameof(ComboBox) => new ComboBox { ItemsSource = new[] { "Item" } },
            nameof(ScrollBar) => new ScrollBar { Orientation = Avalonia.Layout.Orientation.Vertical, Height = 200, AllowAutoHide = false, ViewportSize = 10, Maximum = 100 },
            nameof(Slider) => new Slider { Width = 200 },
            _ => new TreeView { ItemsSource = new[] { new TreeViewItem { Header = "Parent", IsExpanded = true, ItemsSource = new[] { "Child" } } } },
        };
        var window = new Window { Width = 400, Height = 400, Content = control };
        window.Show();

        var libraryThemed = control.GetVisualDescendants().OfType<TemplatedControl>()
            .Where(c => c.GetVisualChildren().SelectMany(v => v.GetVisualChildren().Prepend(v)).Any(v => v is SquircleBorder { Name: ButtonParts.ChromePart }))
            .Select(c => $"{c.GetType().Name}#{c.Name}");
        Assert.Empty(libraryThemed);
    }

    // The fill and text of one state of a filled variant.
    private static (Color Fill, Color Text) StateColours(string variant, ThemeVariant themeVariant, string? state)
    {
        var (window, button) = Show(variant, themeVariant);
        if (state is not null)
            ((IPseudoClasses)button.Classes).Set(state, true);
        window.UpdateLayout();
        return (Effective(Chrome(button).GetBaseValue(SquircleBorder.BackgroundProperty).Value as IBrush),
                Effective(Content(button).GetBaseValue(ContentPresenter.ForegroundProperty).Value as IBrush));
    }

    // Every frame of a background or border fade, composited over the surface, stays between the
    // two states' composited colours in every channel: no frame is lighter or darker than both ends.
    public static TheoryData<string, string, string, string, string> ChromeFades()
    {
        var data = new TheoryData<string, string, string, string, string>();
        foreach (var theme in new[] { "Light", "Dark" })
            foreach (var surfaceKey in new[] { "SystemRegionColor", "SystemChromeMediumLowColor" })
            {
                foreach (var variant in new[] { "standard", "accent", "subtle", "destructive" })
                    foreach (var (from, to) in new[] { ("rest", ":pointerover"), (":pointerover", "rest"), (":pointerover", ":pressed") })
                        data.Add(theme, surfaceKey, variant, from, to);
                data.Add(theme, surfaceKey, "toggle", "rest", ":checked");
                data.Add(theme, surfaceKey, "toggle", ":checked", "rest");
            }
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(ChromeFades))]
    public void ChromeFades_NeverOvershootTheirEnds(string theme, string surfaceKey, string variant, string from, string to)
    {
        Button button = variant == "toggle" ? new ToggleButton { Content = "Toggle" } : new Button { Content = "Button" };
        if (variant is not ("standard" or "toggle"))
            button.Classes.Add(variant);
        var themeVariant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var window = new Window { Width = 200, Height = 100, RequestedThemeVariant = themeVariant, Content = button };
        window.Show();
        try
        {
            var surface = Effective(Resolve(window, surfaceKey, themeVariant));
            var chrome = Chrome(button);
            var fades = chrome.Transitions!.OfType<TransitionBase>()
                .Where(t => t.Property == SquircleBorder.BackgroundProperty || t.Property == SquircleBorder.BorderBrushProperty).ToList();
            Assert.Equal(2, fades.Count);
            Assert.All(fades, t => Assert.IsType<CrossFadeBrushTransition>(t));

            void Set(string state, bool on)
            {
                if (state != "rest")
                    ((IPseudoClasses)button.Classes).Set(state, on);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
            }

            Set(from, true);
            var fromBrushes = (Target(chrome, SquircleBorder.BackgroundProperty), Target(chrome, SquircleBorder.BorderBrushProperty));
            Set(from, false);
            Set(to, true);
            var toBrushes = (Target(chrome, SquircleBorder.BackgroundProperty), Target(chrome, SquircleBorder.BorderBrushProperty));

            // Backgrounds are solid; a gradient border (none today) would switch at the midpoint instead.
            Assert.True(fromBrushes.Item1 is null or ISolidColorBrush && toBrushes.Item1 is null or ISolidColorBrush);
            foreach (var (part, a, b) in new[] { ("background", fromBrushes.Item1, toBrushes.Item1), ("border", fromBrushes.Item2, toBrushes.Item2) })
            {
                if (a is not (null or ISolidColorBrush) || b is not (null or ISolidColorBrush))
                    continue;
                var start = Over(Fill(a), surface);
                var end = Over(Fill(b), surface);
                for (var step = 0; step <= 50; step++)
                {
                    var frame = Over(Fill(CrossFadeBrushTransition.At(step / 50.0, a, b)), surface);
                    foreach (var (f, s, e, channel) in new[] { (frame.R, start.R, end.R, "R"), (frame.G, start.G, end.G, "G"), (frame.B, start.B, end.B, "B") })
                        Assert.True(f >= Math.Min(s, e) - 1 && f <= Math.Max(s, e) + 1,
                            $"{variant} {part} {from} -> {to}: frame {step}/50 is {frame}, outside {start}..{end} in {channel}");
                }
            }
        }
        finally
        {
            window.Close();
        }
    }

    // The brush the chrome's state sets, which its transition runs towards.
    private static IBrush? Target(SquircleBorder chrome, AvaloniaProperty<IBrush?> property) => (IBrush?)chrome.GetBaseValue(property).Value;

    private static Color Fill(IBrush? brush) => brush is null ? Colors.Transparent : Effective(brush);

    private static T Resource<T>(StandardsTheme theme, string key)
    {
        Assert.True(theme.TryGetResource(key, ThemeVariant.Light, out var value), key);
        return Assert.IsType<T>(value);
    }

    private static SquircleBorder Chrome(Button button) =>
        button.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart);

    private static SquircleBorder Ring(Button button) =>
        button.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == FocusRing.Part);

    private static ContentPresenter Content(Button button) => (ContentPresenter)Chrome(button).Child!;

    private static object Resolve(Window window, string key, ThemeVariant variant)
    {
        Assert.True(window.TryFindResource(key, variant, out var value), key);
        return value!;
    }
}
