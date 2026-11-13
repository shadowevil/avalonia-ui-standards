using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;
using static AvaloniaUiStandards.Tests.FieldHost;

namespace AvaloniaUiStandards.Tests;

/// <summary>TextInput (TextBox): squircle chrome, Fluent parts, sizes, state colours, contrast, inner buttons, focus, motion.</summary>
public class TextInputThemeTests
{
    [AvaloniaFact]
    public void Template_KeepsFluentParts_InASquircleChrome()
    {
        var box = new TextBox { Text = "Text" };
        var window = Show(box);
        try
        {
            var chrome = Chrome(box);
            Assert.Equal(new CornerRadius(8), box.CornerRadius);
            Assert.Equal(new CornerRadius(8), chrome.CornerRadius);
            Assert.Equal(new Thickness(1), chrome.BorderThickness);
            foreach (var name in new[] { "PART_InnerDockPanel", "PART_FloatingPlaceholder", "PART_ScrollViewer", "PART_Placeholder", "PART_TextPresenter" })
                Assert.Contains(box.GetVisualDescendants().OfType<Control>(), c => c.Name == name);
            Assert.IsType<DataValidationErrors>(box.GetVisualChildren().Single());
            Assert.Null(box.FocusAdorner);
            Assert.NotNull(box.ContextFlyout);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Normal", 32, 12, 4)]
    [InlineData("Compact", 24, 8, 2)]
    public void SizeAndPadding_FollowDensity_LikeButton(string density, double height, double side, double vertical)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal };
        var box = new TextBox { Text = "Text", Width = 200 };
        var button = new Button { Content = "Button" };
        var window = Show(new StackPanel { Children = { box, button } }, theme: theme);
        try
        {
            Assert.Equal(height, box.Bounds.Height);
            Assert.Equal(button.Bounds.Height, box.Bounds.Height);
            Assert.Equal(new Thickness(side, vertical), box.Padding);
            // The text is vertically centred in the field.
            var presenter = Part<TextPresenter>(box, "PART_TextPresenter");
            var top = presenter.TranslatePoint(default, box)!.Value.Y;
            Assert.True(Math.Abs(box.Bounds.Height - top - presenter.Bounds.Height - top) <= 1);
        }
        finally
        {
            window.Close();
        }
    }

    // Expected background, border and text keys per state.
    public static TheoryData<string, string, string, string, string> States()
    {
        var data = new TheoryData<string, string, string, string, string>();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            data.Add(theme, "rest", "TextControlBackground", "TextControlBorderBrush", "TextControlForeground");
            data.Add(theme, ":pointerover", "TextControlBackgroundPointerOver", "TextControlBorderBrushPointerOver", "TextControlForegroundPointerOver");
            data.Add(theme, ":focus", "TextControlBackgroundFocused", "TextControlBorderBrushFocused", "TextControlForegroundFocused");
            data.Add(theme, ":focus:pointerover", "TextControlBackgroundFocused", "TextControlBorderBrushFocused", "TextControlForegroundFocused");
            data.Add(theme, ":error", "TextControlBackground", "DangerColor", "TextControlForeground");
            data.Add(theme, ":focus:error", "TextControlBackgroundFocused", "DangerColor", "TextControlForegroundFocused");
            data.Add(theme, ":disabled", "TextControlBackgroundDisabled", "TextControlBorderBrushDisabled", "TextControlForegroundDisabled");
        }
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(States))]
    public void States_Resolve(string theme, string state, string backgroundKey, string borderKey, string foregroundKey)
    {
        var box = new TextBox { Text = "Text" };
        var window = Show(box, Variant(theme));
        try
        {
            SetState(box, state);
            Pump(window);
            Assert.Equal(Colour(box, backgroundKey), Effective(box.Background));
            Assert.Equal(Colour(box, borderKey), Effective(box.BorderBrush));
            Assert.Equal(Colour(box, foregroundKey), Effective(box.Foreground));
            // The border stays 1 px in every state, so nothing shifts on focus.
            Assert.Equal(new Thickness(1), Chrome(box).BorderThickness);
        }
        finally
        {
            window.Close();
        }
    }

    // The placeholder is real text: 4.5:1 over the field fill on every surface, in every enabled state, at full opacity.
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Placeholder_MeetsAaContrast(string theme)
    {
        foreach (var state in new[] { "rest", ":pointerover", ":focus" })
        {
            var box = new TextBox { PlaceholderText = "Search" };
            var window = Show(box, Variant(theme));
            try
            {
                SetState(box, state);
                Pump(window);
                var placeholder = Part<TextBlock>(box, "PART_Placeholder");
                Assert.True(placeholder.IsVisible);
                Assert.Equal(1, placeholder.Opacity);
                foreach (var (surface, colour) in Surfaces(box))
                {
                    var under = Over(Effective(box.Background), colour);
                    var ratio = AccessibleColors.ContrastRatio(Over(Effective(placeholder.Foreground), under), under);
                    Assert.True(ratio >= 4.5, $"{state} on {surface}: {ratio:F2}");
                }
            }
            finally
            {
                window.Close();
            }
        }
    }

    // The field's boundary meets 3:1 against what is around it, at rest, focused and in error.
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Border_MeetsNonTextContrast(string theme)
    {
        foreach (var state in new[] { "rest", ":pointerover", ":focus", ":error" })
        {
            var box = new TextBox();
            var window = Show(box, Variant(theme));
            try
            {
                SetState(box, state);
                Pump(window);
                foreach (var (surface, colour) in Surfaces(box))
                {
                    var border = Over(Effective(box.BorderBrush), colour);
                    var ratio = AccessibleColors.ContrastRatio(border, colour);
                    Assert.True(ratio >= NonTextContrast, $"{state} border on {surface}: {ratio:F2}");
                }
            }
            finally
            {
                window.Close();
            }
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Selection_UsesTheAaAccentFill_WithWhiteText(string theme)
    {
        var box = new TextBox { Text = "Text" };
        var window = Show(box, Variant(theme));
        try
        {
            Assert.Equal(Colour(box, "AccentFillColor"), Effective(box.SelectionBrush));
            Assert.Equal(Colors.White, Effective(box.SelectionForegroundBrush));
            Assert.True(AccessibleColors.ContrastRatio(Colors.White, Effective(box.SelectionBrush)) >= 4.5);
            var presenter = Part<TextPresenter>(box, "PART_TextPresenter");
            Assert.Equal(Effective(box.SelectionBrush), Effective(presenter.SelectionBrush));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Placeholder_HidesOnceThereIsText()
    {
        var box = new TextBox { PlaceholderText = "Search" };
        var window = Show(box);
        try
        {
            var placeholder = Part<TextBlock>(box, "PART_Placeholder");
            Assert.True(placeholder.IsVisible);
            box.Text = "a";
            Pump(window);
            Assert.False(placeholder.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ClearButton_ReservesItsPlace_AndShowsOnlyWhenFocusedAndNotEmpty()
    {
        var plain = new TextBox { Text = "Text" };
        var box = new TextBox { Text = "Text", Classes = { "clearButton" } };
        var window = Show(new StackPanel { Width = 240, Children = { plain, box } });
        try
        {
            var clear = Part<Button>(box, TextInputTheme.ClearButtonPart);
            Assert.False(Part<Button>(plain, TextInputTheme.ClearButtonPart).IsEffectivelyVisible);
            Assert.True(clear.IsVisible);
            Assert.Equal(0, clear.Opacity);
            Assert.False(clear.IsHitTestVisible);
            Assert.False(clear.Focusable);
            Assert.Equal(new Size(24, 24), clear.Bounds.Size);
            Assert.Equal(Icons.XCircle, Assert.IsType<Icon>(clear.Content).Data);

            var textWidth = Part<TextPresenter>(box, "PART_TextPresenter").Bounds.Width;
            box.Focus(NavigationMethod.Pointer);
            Pump(window);
            Assert.Equal(1d, clear.GetBaseValue(Visual.OpacityProperty).Value);
            Assert.True(clear.IsHitTestVisible);
            // The text does not reflow when the button appears.
            Assert.Equal(textWidth, Part<TextPresenter>(box, "PART_TextPresenter").Bounds.Width);
            // It sits the same inset (4) from the top, bottom and right of the field.
            var origin = clear.TranslatePoint(default, box)!.Value;
            Assert.Equal(4, origin.Y);
            Assert.Equal(box.Bounds.Width - 4, origin.X + clear.Bounds.Width);

            clear.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Pump(window);
            Assert.True(string.IsNullOrEmpty(box.Text));
            Assert.Equal(0d, clear.GetBaseValue(Visual.OpacityProperty).Value);
            Assert.True(box.IsFocused);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InnerButtons_DoNotApplyToMultiLineOrReadOnlyText()
    {
        var multi = new TextBox { Text = "Text", AcceptsReturn = true, Classes = { "clearButton", "revealPasswordButton" } };
        var readOnly = new TextBox { Text = "Text", IsReadOnly = true, Classes = { "clearButton", "revealPasswordButton" } };
        var window = Show(new StackPanel { Children = { multi, readOnly } });
        try
        {
            foreach (var box in new[] { multi, readOnly })
            {
                Assert.False(Part<Button>(box, TextInputTheme.ClearButtonPart).IsVisible);
                Assert.False(Part<ToggleButton>(box, TextInputTheme.RevealButtonPart).IsVisible);
            }
            Assert.Equal(VerticalAlignment.Top, multi.VerticalContentAlignment);
            Assert.Equal(VerticalAlignment.Center, readOnly.VerticalContentAlignment);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RevealButton_TogglesThePassword_AndSwapsItsIcon()
    {
        var box = new TextBox { Text = "secret", PasswordChar = '•', Classes = { "revealPasswordButton" } };
        var window = Show(box);
        try
        {
            var reveal = Part<ToggleButton>(box, TextInputTheme.RevealButtonPart);
            var icon = Part<Icon>(box, TextInputTheme.RevealIconPart);
            Assert.Equal(1d, reveal.GetBaseValue(Visual.OpacityProperty).Value);
            Assert.Equal(Icons.Eye, icon.Data);

            // As a click does: the current value, keeping the binding.
            reveal.SetCurrentValue(ToggleButton.IsCheckedProperty, true);
            Pump(window);
            Assert.True(box.RevealPassword);
            Assert.Equal(Icons.EyeSlash, icon.Data);

            box.RevealPassword = false;
            Pump(window);
            Assert.False(reveal.IsChecked);
            Assert.Equal(Icons.Eye, icon.Data);

            box.Text = "";
            Pump(window);
            Assert.Equal(0d, reveal.GetBaseValue(Visual.OpacityProperty).Value);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InnerButtons_UseTheSubtleButtonColours()
    {
        var box = new TextBox { Text = "Text", Classes = { "clearButton" } };
        var window = Show(box);
        try
        {
            box.Focus();
            Pump(window);
            var clear = Part<Button>(box, TextInputTheme.ClearButtonPart);
            var chrome = clear.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart);
            Assert.Equal(Colors.Transparent, Effective(chrome.Background));
            ((IPseudoClasses)clear.Classes).Set(":pointerover", true);
            Pump(window);
            Assert.Equal(Colour(box, "ButtonBackgroundPointerOver"), Effective(chrome.GetBaseValue(SquircleBorder.BackgroundProperty).Value));
            ((IPseudoClasses)clear.Classes).Set(":pressed", true);
            Pump(window);
            Assert.Equal(Colour(box, "ButtonBackgroundPressed"), Effective(chrome.GetBaseValue(SquircleBorder.BackgroundProperty).Value));
            Assert.Equal(new CornerRadius(4), clear.CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InnerButtons_ShrinkWithCompactDensity()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = DensityStyle.Compact };
        var box = new TextBox { Text = "Text", Width = 200, Classes = { "clearButton" } };
        var window = Show(new StackPanel { Children = { box } }, theme: theme);
        try
        {
            box.Focus();
            Pump(window);
            var clear = Part<Button>(box, TextInputTheme.ClearButtonPart);
            Assert.Equal(new Size(20, 20), clear.Bounds.Size);
            Assert.Equal(new CornerRadius(6), clear.CornerRadius);
            var origin = clear.TranslatePoint(default, box)!.Value;
            Assert.Equal(2, origin.Y);
            Assert.Equal(box.Bounds.Width - 2, origin.X + clear.Bounds.Width);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void KeyboardFocus_ShowsTheRing_PointerFocusOnlyTheAccentBorder()
    {
        var box = new TextBox { Text = "Text", Width = 200 };
        var other = new Button { Content = "Other" };
        var window = Show(new StackPanel { Margin = new Thickness(8), Children = { box, other } });
        try
        {
            var ring = Ring(box);
            box.Focus(NavigationMethod.Pointer);
            Pump(window);
            Assert.False(ring.IsVisible);
            Assert.Equal(Colour(box, "TextControlBorderBrushFocused"), Effective(box.BorderBrush));

            other.Focus();
            box.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.True(ring.IsVisible);
            Assert.Equal(new CornerRadius(12), ring.CornerRadius);
            Assert.Equal(new Rect(-4, -4, box.Bounds.Width + 8, box.Bounds.Height + 8), ring.Bounds);
            Assert.False(box.ClipToBounds);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Transitions_UseMotionFast_AndBecomeInstantUnderReduceMotion()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var box = new TextBox { Text = "Text", Classes = { "clearButton" } };
        var window = Show(box, theme: theme);
        try
        {
            var chrome = Chrome(box);
            var clear = Part<Button>(box, TextInputTheme.ClearButtonPart);
            var fades = chrome.Transitions!.Cast<TransitionBase>().ToList();
            Assert.Contains(fades, t => t.Property == SquircleBorder.BackgroundProperty);
            Assert.Contains(fades, t => t.Property == SquircleBorder.BorderBrushProperty);
            Assert.All(fades, t => Assert.Equal(TimeSpan.FromMilliseconds(100), t.Duration));
            Assert.Equal(TimeSpan.FromMilliseconds(100), Assert.Single(clear.Transitions!.Cast<TransitionBase>()).Duration);

            theme.ReduceMotion = true;
            Pump(window);
            Assert.Empty(chrome.Transitions!);
            Assert.Empty(clear.Transitions!);
        }
        finally
        {
            window.Close();
        }
    }

    // CalendarDatePicker's TextBox takes the inner theme: no fill, border, state or ring of its own;
    // the picker draws the field (0038).
    [AvaloniaFact]
    public void CalendarDatePickerTextBox_ShowsNoChromeStateOrRing()
    {
        var picker = new CalendarDatePicker { Width = 200 };
        var window = Show(picker);
        try
        {
            var inner = picker.GetVisualDescendants().OfType<TextBox>().Single();
            foreach (var state in new[] { ":pointerover", ":focus", ":disabled" })
            {
                SetState(inner, state);
                Pump(window);
                Assert.Equal(0, Effective(Chrome(inner).Background).A);
                Assert.Equal(0, Effective(Chrome(inner).BorderBrush).A);
                SetState(inner, state, on: false);
            }
            inner.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.True(inner.IsFocused);
            Assert.False(Ring(inner).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    private static void SetState(Control control, string state, bool on = true)
    {
        foreach (var pseudo in state.Split(':', StringSplitOptions.RemoveEmptyEntries))
            if (pseudo != "rest")
                ((IPseudoClasses)control.Classes).Set(":" + pseudo, on);
    }
}
