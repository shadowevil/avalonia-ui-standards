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

namespace AvaloniaUiStandards.Tests;

public class ButtonThemeTests
{
    [AvaloniaFact]
    public void Theme_AppliesToButton()
    {
        var (_, button) = Show(variant: null);

        var chrome = Assert.IsType<SquircleBorder>(Assert.Single(button.GetVisualChildren()));
        Assert.Equal(ButtonTheme.ChromePart, chrome.Name);
        var presenter = Assert.IsType<ContentPresenter>(chrome.Child);
        Assert.Equal(ButtonTheme.ContentPart, presenter.Name);
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

        Assert.True(FillColors.ContrastRatio(restText, rest) >= 4.5, $"rest {rest}");
        Assert.True(FillColors.ContrastRatio(hoverText, hover) >= 4.5, $"hover {hover}");
        Assert.True(FillColors.ContrastRatio(pressedText, pressed) >= 4.5, $"pressed {pressed}");
        Assert.True(FillColors.RelativeLuminance(hover) < FillColors.RelativeLuminance(rest));
        Assert.True(FillColors.RelativeLuminance(pressed) < FillColors.RelativeLuminance(hover));
    }

    [AvaloniaFact]
    public void BrandColours_StayAsSet_AndFillsAreDerivedFromThem()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        Assert.True(theme.TryGetResource("SystemAccentColor", ThemeVariant.Light, out var accent));
        Assert.True(theme.TryGetResource("DangerColor", ThemeVariant.Light, out var danger));
        Assert.Equal(Color.Parse("#007CEA"), accent);
        Assert.Equal(Color.Parse("#E1332A"), danger);
        Assert.Equal(FillColors.ForWhiteText((Color)accent!).Normal, Resource<Color>(theme, "AccentFillColor"));
        Assert.Equal(FillColors.ForWhiteText((Color)danger!).Normal, Resource<Color>(theme, "DangerFillColor"));
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
            Assert.True(FillColors.ContrastRatio(Colors.White, Resource<Color>(theme, key)) >= 4.5, key);
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
                Assert.True(FillColors.ContrastRatio(text, fill) >= 4.5, $"{state ?? "rest"} on {surfaceKey}: {text} over {fill}");
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
        Assert.All(transitions, t => Assert.Equal(TimeSpan.Zero, t.Duration));
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
        button.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();

        var layer = AdornerLayer.GetAdornerLayer(button)!;
        var ring = Assert.IsType<SquircleBorder>(Assert.Single(layer.Children));
        Assert.Equal(new CornerRadius(12), ring.CornerRadius);
        Assert.Equal(new Thickness(-4), ring.Margin);
        Assert.Equal(new Thickness(2), ring.BorderThickness);
        Assert.Equal(Color.Parse("#007CEA"), ((ISolidColorBrush)ring.BorderBrush!).Color);
        Assert.Null(ring.Child);
        Assert.False(AdornerLayer.GetIsClipEnabled(ring));
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
        var ring = (SquircleBorder)AdornerLayer.GetAdornerLayer(button)!.Children.Single();
        Assert.Equal(new CornerRadius(12), ring.CornerRadius);

        button.CornerRadius = new CornerRadius(4);
        Assert.Equal(new CornerRadius(8), ring.CornerRadius);
    }

    // Fluent controls whose templates hold buttons keep Fluent's look for them (0007): their plain
    // Buttons set an explicit theme, and the Button subclasses have theme keys of their own.
    public static TheoryData<string> FluentControlsWithButtons => new()
    {
        nameof(CalendarDatePicker), nameof(Calendar), nameof(DatePicker), nameof(TimePicker),
        nameof(DatePickerPresenter), nameof(TimePickerPresenter), nameof(NumericUpDown),
        nameof(Expander), nameof(ComboBox), nameof(SplitButton),
    };

    [AvaloniaTheory]
    [MemberData(nameof(FluentControlsWithButtons))]
    public void FluentTemplates_DoNotPickUpTheLibraryButtonTheme(string name)
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
            nameof(Expander) => new Expander { Header = "Header", Content = "Content" },
            nameof(ComboBox) => new ComboBox { ItemsSource = new[] { "Item" } },
            _ => new SplitButton { Content = "Split" },
        };
        var window = new Window { Width = 400, Height = 400, Content = control };
        window.Show();

        var libraryThemed = control.GetVisualDescendants().OfType<Button>()
            .Where(b => b.GetVisualChildren().FirstOrDefault() is SquircleBorder { Name: ButtonTheme.ChromePart })
            .Select(b => $"{b.GetType().Name}#{b.Name}");
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

    private static T Resource<T>(StandardsTheme theme, string key)
    {
        Assert.True(theme.TryGetResource(key, ThemeVariant.Light, out var value), key);
        return Assert.IsType<T>(value);
    }

    // A colour with alpha composited over an opaque background.
    private static Color Over(Color top, Color bottom)
    {
        var a = top.A / 255.0;
        byte Mix(byte t, byte b) => (byte)Math.Round(t * a + b * (1 - a));
        return Color.FromRgb(Mix(top.R, bottom.R), Mix(top.G, bottom.G), Mix(top.B, bottom.B));
    }

    private static SquircleBorder Chrome(Button button) => (SquircleBorder)button.GetVisualChildren().Single();

    private static ContentPresenter Content(Button button) => (ContentPresenter)Chrome(button).Child!;

    private static object Resolve(Window window, string key, ThemeVariant variant)
    {
        Assert.True(window.TryFindResource(key, variant, out var value), key);
        return value!;
    }

    // A colour or solid brush as one colour, with brush opacity folded into alpha.
    private static Color Effective(object? value) => value switch
    {
        Color c => c,
        ISolidColorBrush b => Color.FromArgb((byte)Math.Round(b.Color.A * b.Opacity), b.Color.R, b.Color.G, b.Color.B),
        _ => throw new InvalidOperationException($"Not a colour: {value}"),
    };
}
