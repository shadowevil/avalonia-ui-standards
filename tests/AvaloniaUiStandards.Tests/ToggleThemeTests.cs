using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
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
using Path = Avalonia.Controls.Shapes.Path;

namespace AvaloniaUiStandards.Tests;

/// <summary>CheckBox, RadioButton and ToggleSwitch (decision 0030).</summary>
public class ToggleThemeTests
{
    [AvaloniaTheory]
    [InlineData("Normal", 20, 5, 32)]
    [InlineData("Compact", 16, 4, 24)]
    public void CheckBox_BoxAndRow_FollowDensity(string density, double box, double radius, double row)
    {
        var (_, checkBox) = Show(new CheckBox { Content = "Label" }, theme: Theme(density));
        var indicator = Part<Panel>(checkBox, ToggleParts.IndicatorPart);

        Assert.Equal(new Size(box, box), indicator.Bounds.Size);
        Assert.Equal(new CornerRadius(radius), Part<SquircleBorder>(checkBox, ToggleParts.OutlinePart).CornerRadius);
        Assert.Equal(row, checkBox.Bounds.Height);
        Assert.Equal(new Thickness(1), Part<SquircleBorder>(checkBox, ToggleParts.OutlinePart).BorderThickness);
        // The label starts SpacingS after the box.
        Assert.Equal(box + 8, Label(checkBox).Bounds.X);
    }

    [AvaloniaTheory]
    [InlineData("Normal", 20, 8, 32)]
    [InlineData("Compact", 16, 6, 24)]
    public void RadioButton_CircleAndDot_FollowDensity(string density, double circle, double dot, double row)
    {
        var (_, radio) = Show(new RadioButton { Content = "Label", IsChecked = true }, theme: Theme(density));

        Assert.Equal(new Size(circle, circle), Part<Panel>(radio, ToggleParts.IndicatorPart).Bounds.Size);
        Assert.Equal(new CornerRadius(circle / 2), Part<SquircleBorder>(radio, ToggleParts.OutlinePart).CornerRadius);
        Assert.Equal(new Size(dot, dot), Part<SquircleBorder>(radio, RadioButtonTheme.DotPart).Bounds.Size);
        Assert.Equal(row, radio.Bounds.Height);
    }

    [AvaloniaFact]
    public void CheckBox_WithoutLabel_IsJustTheBox()
    {
        var (_, checkBox) = Show(new CheckBox());
        Assert.Equal(20, checkBox.Bounds.Width);
    }

    [AvaloniaTheory]
    [InlineData("Normal", 40, 20, 12, 14)]
    [InlineData("Compact", 32, 16, 10, 12)]
    public void ToggleSwitch_TrackAndKnob_FollowDensity(string density, double width, double height, double off, double on)
    {
        // Under reduce motion, so sizes and the knob position take their end values at once.
        var (window, toggle) = Show(new ToggleSwitch(), theme: new StandardsTheme(OsAccessibility.Settings.NotReduced)
        {
            Density = density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal,
            ReduceMotion = true,
        });
        var knob = Part<SquircleBorder>(toggle, ToggleSwitchTheme.KnobPart);
        var moving = Part<Panel>(toggle, ToggleSwitchTheme.MovingKnobsPart);

        Assert.Equal(new Size(width, height), Part<Panel>(toggle, ToggleParts.IndicatorPart).Bounds.Size);
        Assert.Equal(new CornerRadius(height / 2), Part<SquircleBorder>(toggle, ToggleParts.OutlinePart).CornerRadius);
        Assert.Equal(new Size(off, off), knob.Bounds.Size);
        Assert.Equal(0, Canvas.GetLeft(moving));

        ((IPseudoClasses)toggle.Classes).Set(":pressed", true);
        Settle(window);
        Assert.Equal(new Size(off + 4, off), knob.Bounds.Size);
        ((IPseudoClasses)toggle.Classes).Set(":pressed", false);

        toggle.IsChecked = true;
        Settle(window);
        Assert.Equal(new Size(on, on), knob.Bounds.Size);
        // The knob cell (a square of the track's height) ends at the track's end.
        Assert.Equal(width - height, Canvas.GetLeft(moving));
    }

    [AvaloniaFact]
    public void ToggleSwitch_ShowsOnOrOffContent()
    {
        var (window, toggle) = Show(new ToggleSwitch());
        var on = Part<ContentPresenter>(toggle, ToggleSwitchTheme.OnContentPart);
        var off = Part<ContentPresenter>(toggle, ToggleSwitchTheme.OffContentPart);
        Assert.Equal((0d, 1d), (on.Opacity, off.Opacity));

        toggle.IsChecked = true;
        Settle(window);
        Assert.Equal((1d, 0d), (on.Opacity, off.Opacity));
    }

    [AvaloniaFact]
    public void Click_TogglesCheckBox_AlsoOnTheLabelAndGap()
    {
        var (window, checkBox) = Show(new CheckBox { Content = "A longer label" });
        // In the gap between the box and the label: the row's background takes the click.
        var gap = checkBox.TranslatePoint(new Point(24, checkBox.Bounds.Height / 2), window)!.Value;
        window.MouseDown(gap, MouseButton.Left);
        window.MouseUp(gap, MouseButton.Left);
        Assert.True(checkBox.IsChecked);

        var label = checkBox.TranslatePoint(new Point(60, checkBox.Bounds.Height / 2), window)!.Value;
        window.MouseDown(label, MouseButton.Left);
        window.MouseUp(label, MouseButton.Left);
        Assert.False(checkBox.IsChecked);
    }

    [AvaloniaFact]
    public void Space_TogglesToggleSwitch()
    {
        var (window, toggle) = Show(new ToggleSwitch());
        toggle.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Assert.True(toggle.IsChecked);
    }

    // Expected outline border, fill and mark per control, check state and state (null mark = not shown).
    public static TheoryData<string, string, string, string, string, string?> States()
    {
        var data = new TheoryData<string, string, string, string, string, string?>();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            data.Add("CheckBox", theme, "unchecked", "rest", "CheckBoxCheckBackgroundStrokeUnchecked", null);
            data.Add("CheckBox", theme, "unchecked", ":pointerover", "CheckBoxCheckBackgroundStrokeUncheckedPointerOver", null);
            data.Add("CheckBox", theme, "unchecked", ":pressed", "CheckBoxCheckBackgroundStrokeUncheckedPressed", null);
            data.Add("CheckBox", theme, "unchecked", ":disabled", "CheckBoxCheckBackgroundStrokeUncheckedDisabled", null);
            data.Add("CheckBox", theme, "checked", "rest", "AccentFillColor", "CheckBoxCheckGlyphForegroundChecked");
            data.Add("CheckBox", theme, "checked", ":pointerover", "AccentFillColorPointerOver", "CheckBoxCheckGlyphForegroundCheckedPointerOver");
            data.Add("CheckBox", theme, "checked", ":pressed", "AccentFillColorPressed", "CheckBoxCheckGlyphForegroundCheckedPressed");
            data.Add("CheckBox", theme, "checked", ":disabled", "AccentButtonBackgroundDisabled", "AccentButtonForegroundDisabled");
            data.Add("CheckBox", theme, "indeterminate", "rest", "AccentFillColor", "CheckBoxCheckGlyphForegroundIndeterminate");
            data.Add("CheckBox", theme, "indeterminate", ":disabled", "AccentButtonBackgroundDisabled", "AccentButtonForegroundDisabled");
            data.Add("RadioButton", theme, "unchecked", "rest", "RadioButtonOuterEllipseStroke", null);
            data.Add("RadioButton", theme, "unchecked", ":pressed", "RadioButtonOuterEllipseStrokePressed", null);
            data.Add("RadioButton", theme, "checked", "rest", "AccentFillColor", "RadioButtonCheckGlyphFill");
            data.Add("RadioButton", theme, "checked", ":pointerover", "AccentFillColorPointerOver", "RadioButtonCheckGlyphFillPointerOver");
            data.Add("RadioButton", theme, "checked", ":pressed", "AccentFillColorPressed", "RadioButtonCheckGlyphFillPressed");
            data.Add("RadioButton", theme, "checked", ":disabled", "AccentButtonBackgroundDisabled", "AccentButtonForegroundDisabled");
            data.Add("ToggleSwitch", theme, "unchecked", "rest", "ToggleSwitchStrokeOff", "ToggleSwitchStrokeOff");
            data.Add("ToggleSwitch", theme, "unchecked", ":pointerover", "ToggleSwitchStrokeOffPointerOver", "ToggleSwitchStrokeOffPointerOver");
            data.Add("ToggleSwitch", theme, "unchecked", ":disabled", "ToggleSwitchStrokeOffDisabled", "ToggleSwitchStrokeOffDisabled");
            data.Add("ToggleSwitch", theme, "checked", "rest", "AccentFillColor", "ToggleSwitchKnobFillOn");
            data.Add("ToggleSwitch", theme, "checked", ":pressed", "AccentFillColorPressed", "ToggleSwitchKnobFillOnPressed");
            data.Add("ToggleSwitch", theme, "checked", ":disabled", "AccentButtonBackgroundDisabled", "AccentButtonForegroundDisabled");
        }
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(States))]
    public void StateColours_ResolvePerControlAndState(string control, string theme, string check, string state, string borderKey, string? markKey)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, toggle) = Show(Create(control, check), variant);
        SetState(window, toggle, state);

        Assert.Equal(Effective(Resolve(window, borderKey, variant)), BaseColour(Part<SquircleBorder>(toggle, ToggleParts.OutlinePart), Border.BorderBrushProperty));
        var fill = Part<SquircleBorder>(toggle, ToggleParts.FillPart);
        Assert.Equal(check == "unchecked" ? 0 : 1, fill.Opacity);
        if (check != "unchecked")
            Assert.Equal(Effective(Resolve(window, borderKey, variant)), BaseColour(fill, Border.BackgroundProperty));
        if (markKey is not null)
            Assert.Equal(Effective(Resolve(window, markKey, variant)), Mark(toggle, check));
    }

    // The white mark on every enabled accent fill meets AA (4.5:1); the unchecked outline in every state and the
    // checked fill at rest meet the 3:1 non-text contrast on the page and on cards. (In dark, the darker hover and
    // pressed fills, shared with the accent buttons, fall below 3:1 on cards; the mark carries the state.)
    [AvaloniaTheory]
    [InlineData("CheckBox", "Light")]
    [InlineData("CheckBox", "Dark")]
    [InlineData("RadioButton", "Light")]
    [InlineData("RadioButton", "Dark")]
    [InlineData("ToggleSwitch", "Light")]
    [InlineData("ToggleSwitch", "Dark")]
    public void MarksAndOutlines_MeetContrast(string control, string theme)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        foreach (var state in new[] { "rest", ":pointerover", ":pressed" })
        {
            var (window, toggle) = Show(Create(control, "checked"), variant);
            SetState(window, toggle, state);
            var fill = BaseColour(Part<SquircleBorder>(toggle, ToggleParts.FillPart), Border.BackgroundProperty);
            var mark = Mark(toggle, "checked");
            Assert.True(AccessibleColors.ContrastRatio(mark, fill) >= 4.5, $"{state} mark {mark} on {fill}");
            foreach (var surface in state == "rest" ? Surfaces(window, variant) : [])
                Assert.True(AccessibleColors.ContrastRatio(fill, surface) >= 3, $"{state} fill {fill} on {surface}");

            var (uncheckedWindow, uncheckedToggle) = Show(Create(control, "unchecked"), variant);
            SetState(uncheckedWindow, uncheckedToggle, state);
            var outline = BaseColour(Part<SquircleBorder>(uncheckedToggle, ToggleParts.OutlinePart), Border.BorderBrushProperty);
            foreach (var surface in Surfaces(window, variant))
                Assert.True(AccessibleColors.ContrastRatio(outline, surface) >= 3, $"{state} outline {outline} on {surface}");
        }
    }

    [AvaloniaTheory]
    [InlineData("CheckBox")]
    [InlineData("RadioButton")]
    [InlineData("ToggleSwitch")]
    public void Focus_ShowsRingAroundTheIndicator(string control)
    {
        var (_, toggle) = Show(Create(control, "checked"));
        var ring = Part<SquircleBorder>(toggle, FocusRing.Part);
        Assert.False(ring.IsVisible);

        toggle.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();

        var indicator = Part<Panel>(toggle, ToggleParts.IndicatorPart);
        var outline = Part<SquircleBorder>(toggle, ToggleParts.OutlinePart);
        Assert.True(ring.IsVisible);
        Assert.Null(toggle.FocusAdorner);
        Assert.Equal(new Rect(-4, -4, indicator.Bounds.Width + 8, indicator.Bounds.Height + 8), ring.Bounds);
        Assert.Equal(new CornerRadius(outline.CornerRadius.TopLeft + 4), ring.CornerRadius);
        Assert.Equal(new Thickness(2), ring.BorderThickness);
    }

    [AvaloniaFact]
    public void Transitions_FadeChecksOverMotionNormal_ColoursOverMotionFast_AndStopUnderReduceMotion()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var (window, toggle) = Show(new ToggleSwitch(), theme: theme);
        var fill = Part<SquircleBorder>(toggle, ToggleParts.FillPart);
        var transitions = fill.Transitions!.Cast<TransitionBase>().ToList();

        Assert.Equal(TimeSpan.FromMilliseconds(200), transitions.Single(t => t.Property == Visual.OpacityProperty).Duration);
        Assert.All(transitions.Where(t => t.Property != Visual.OpacityProperty), t => Assert.Equal(TimeSpan.FromMilliseconds(100), t.Duration));
        Assert.All(transitions, t => Assert.IsType<CubicEaseOut>(t.Easing));
        Assert.All(transitions.Where(t => t.Property!.PropertyType == typeof(IBrush)), t => Assert.IsType<CrossFadeBrushTransition>(t));

        var knob = Assert.IsType<DoubleTransition>(Assert.Single(toggle.KnobTransitions!));
        Assert.Equal(Canvas.LeftProperty, knob.Property);
        Assert.Equal(TimeSpan.FromMilliseconds(200), knob.Duration);
        Assert.IsType<CubicEaseOut>(knob.Easing);

        theme.ReduceMotion = true;
        Settle(window);
        Assert.Empty(fill.Transitions!);
        Assert.Empty(toggle.KnobTransitions!);
        var (_, checkBox) = Show(new CheckBox(), theme: new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        Assert.Empty(Part<Path>(checkBox, CheckBoxTheme.CheckPart).Transitions!);
    }

    [AvaloniaFact]
    public void Density_FollowsRuntimeChanges()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var (window, toggle) = Show(new ToggleSwitch { IsChecked = true }, theme: theme);
        theme.Density = DensityStyle.Compact;
        Settle(window);
        Assert.Equal(new Size(32, 16), Part<Panel>(toggle, ToggleParts.IndicatorPart).Bounds.Size);
        // The end value: the knob slides there.
        Assert.Equal(16d, Part<Panel>(toggle, ToggleSwitchTheme.MovingKnobsPart).GetBaseValue(Canvas.LeftProperty).Value);
    }

    [AvaloniaFact]
    public void LibraryTemplate_ReplacesFluents()
    {
        var (_, checkBox) = Show(new CheckBox { Content = "Label" });
        Assert.DoesNotContain(checkBox.GetVisualDescendants().OfType<Border>(), b => b.Name == "NormalRectangle");
    }

    private static Control Create(string control, string check) => (control, check) switch
    {
        ("CheckBox", "indeterminate") => new CheckBox { Content = "Label", IsThreeState = true, IsChecked = null },
        ("CheckBox", _) => new CheckBox { Content = "Label", IsChecked = check == "checked" },
        ("RadioButton", _) => new RadioButton { Content = "Label", IsChecked = check == "checked" },
        _ => new ToggleSwitch { IsChecked = check == "checked" },
    };

    private static void SetState(Window window, Control control, string state)
    {
        if (state == ":disabled")
            control.IsEnabled = false;
        else if (state != "rest")
            ((IPseudoClasses)control.Classes).Set(state, true);
        Settle(window);
    }

    private static Color Mark(Control toggle, string check) => toggle switch
    {
        CheckBox => BaseColour(Part<Path>(toggle, check == "indeterminate" ? CheckBoxTheme.DashPart : CheckBoxTheme.CheckPart), Shape.StrokeProperty),
        RadioButton => BaseColour(Part<SquircleBorder>(toggle, RadioButtonTheme.DotPart), Border.BackgroundProperty),
        _ => BaseColour(Part<SquircleBorder>(toggle, ToggleSwitchTheme.KnobPart), Border.BackgroundProperty),
    };

    private static IEnumerable<Color> Surfaces(Window window, ThemeVariant variant) =>
        new[] { "SystemRegionColor", "SystemChromeMediumLowColor" }.Select(k => Effective(Resolve(window, k, variant)));

    // The end value of a (possibly transitioning) brush property.
    private static Color BaseColour(AvaloniaObject element, AvaloniaProperty<IBrush?> property) =>
        Effective(element.GetBaseValue(property).Value as IBrush);

    private static ContentPresenter Label(Control control) => Part<ContentPresenter>(control, ButtonParts.ContentPart);

    internal static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    internal static StandardsTheme? Theme(string density) => density == "Compact"
        ? new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = DensityStyle.Compact }
        : null;

    internal static (Window Window, T Control) Show<T>(T control, ThemeVariant? variant = null, StandardsTheme? theme = null)
    {
        var window = new Window { Width = 300, Height = 120, RequestedThemeVariant = variant ?? ThemeVariant.Light };
        if (theme is not null)
            window.Styles.Add(theme);
        window.Content = control;
        window.Show();
        Settle(window);
        return (window, control);
    }

    internal static void Settle(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    internal static object? Resolve(Window window, string key, ThemeVariant variant) =>
        window.TryFindResource(key, variant, out var value) ? value : throw new InvalidOperationException(key);
}
