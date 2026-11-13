using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;
using static AvaloniaUiStandards.Tests.FieldHost;

namespace AvaloniaUiStandards.Tests;

/// <summary>NumericUpDown and ButtonSpinner: TextInput chrome and sizes, spin buttons, states, focus, behaviour, motion.</summary>
public class NumericThemeTests
{
    private static TextBox Text(NumericUpDown box) => Part<TextBox>(box, SpinnerThemes.TextBoxPart);

    [AvaloniaTheory]
    [InlineData("Normal", 32, 24, 4)]
    [InlineData("Compact", 24, 20, 2)]
    public void Field_MatchesTextInput_WithInsetSpinButtons(string density, double height, double button, double inset)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal };
        var box = new NumericUpDown { Width = 200, Value = 3 };
        var spinner = new ButtonSpinner { Width = 200, Content = "Medium" };
        var text = new TextBox { Width = 200 };
        var window = Show(new StackPanel { Children = { box, spinner, text } }, theme: theme);
        try
        {
            foreach (var field in new Control[] { box, spinner })
            {
                Assert.Equal(height, field.Bounds.Height);
                Assert.Equal(new CornerRadius(8), Chrome(field).CornerRadius);
                Assert.Equal(new Thickness(1), Chrome(field).BorderThickness);
                var increase = Part<RepeatButton>(field, SpinnerThemes.IncreaseButtonPart);
                var decrease = Part<RepeatButton>(field, SpinnerThemes.DecreaseButtonPart);
                Assert.Equal(Icons.CaretUp, Assert.IsType<Icon>(increase.Content).Data);
                Assert.Equal(Icons.CaretDown, Assert.IsType<Icon>(decrease.Content).Data);
                Assert.Equal(new Size(button, button), decrease.Bounds.Size);
                // Side by side, the last one the inset from the field's edge, top and bottom.
                var origin = decrease.TranslatePoint(default, field)!.Value;
                Assert.Equal(inset, origin.Y);
                Assert.Equal(field.Bounds.Width - inset, origin.X + button);
                Assert.Equal(origin.X - button, increase.TranslatePoint(default, field)!.Value.X);
                Assert.False(increase.IsTabStop);
            }
            // The number sits where TextInput's text does.
            var numberText = Part<Avalonia.Controls.Presenters.TextPresenter>(Text(box), "PART_TextPresenter");
            var plainText = Part<Avalonia.Controls.Presenters.TextPresenter>(text, "PART_TextPresenter");
            Assert.Equal(plainText.TranslatePoint(default, text), numberText.TranslatePoint(default, box));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ButtonsLeft_SitAtTheLeadingEnd()
    {
        var box = new NumericUpDown { Width = 200, Value = 3, ButtonSpinnerLocation = Location.Left };
        var window = Show(new StackPanel { Children = { box } });
        try
        {
            var increase = Part<RepeatButton>(box, SpinnerThemes.IncreaseButtonPart);
            Assert.Equal(new Point(4, 4), increase.TranslatePoint(default, box));
        }
        finally
        {
            window.Close();
        }
    }

    public static TheoryData<string, string, string, string> States()
    {
        var data = new TheoryData<string, string, string, string>();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            data.Add(theme, "rest", "TextControlBackground", "TextControlBorderBrush");
            data.Add(theme, ":pointerover", "TextControlBackgroundPointerOver", "TextControlBorderBrushPointerOver");
            data.Add(theme, ":focus-within", "TextControlBackgroundFocused", "TextControlBorderBrushFocused");
            data.Add(theme, ":error", "TextControlBackground", "DangerColor");
            data.Add(theme, ":disabled", "TextControlBackgroundDisabled", "TextControlBorderBrushDisabled");
        }
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(States))]
    public void States_Resolve_OnBothBlocks(string theme, string state, string backgroundKey, string borderKey)
    {
        var box = new NumericUpDown { Width = 200, Value = 3 };
        var spinner = new ButtonSpinner { Width = 200, Content = "Medium" };
        var window = Show(new StackPanel { Children = { box, spinner } }, Variant(theme));
        try
        {
            foreach (var field in new TemplatedControl[] { box, spinner })
            {
                if (state != "rest")
                    ((IPseudoClasses)field.Classes).Set(state, true);
                Pump(window);
                Assert.Equal(Colour(field, backgroundKey), Effective(field.Background));
                Assert.Equal(Colour(field, borderKey), Effective(field.BorderBrush));
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InnerTextBox_HasNoChrome_AndKeyboardFocusShowsTheRingAroundTheField()
    {
        var box = new NumericUpDown { Width = 200, Value = 3 };
        var other = new Button { Content = "Other" };
        var window = Show(new StackPanel { Children = { box, other } });
        try
        {
            var text = Text(box);
            var spinner = Part<ButtonSpinner>(box, SpinnerThemes.SpinnerPart);
            Assert.Equal(0, Effective(text.Background).A);
            Assert.Equal(new Thickness(0), text.BorderThickness);

            text.Focus(NavigationMethod.Pointer);
            Pump(window);
            Assert.False(Ring(spinner).IsVisible);
            Assert.Equal(Colour(box, "TextControlBorderBrushFocused"), Effective(box.BorderBrush));

            other.Focus();
            text.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.True(Ring(spinner).IsVisible);
            Assert.False(Ring(text).IsVisible);
            Assert.Equal(new Rect(-4, -4, box.Bounds.Width + 8, box.Bounds.Height + 8),
                Ring(spinner).Bounds.Translate(spinner.TranslatePoint(default, box)!.Value));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SpinButtons_ChangeTheValue_AndDisableAtTheLimits()
    {
        var box = new NumericUpDown { Width = 200, Value = 9, Maximum = 10, Minimum = 0 };
        var window = Show(box);
        try
        {
            var increase = Part<RepeatButton>(box, SpinnerThemes.IncreaseButtonPart);
            increase.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump(window);
            Assert.Equal(10, box.Value);
            Assert.False(increase.IsEffectivelyEnabled);
            var chrome = increase.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart);
            Assert.Equal(0, Effective(chrome.Background).A);
            Assert.Equal(Colour(box, "ButtonForegroundDisabled"), Effective(Assert.IsType<Icon>(increase.Content).Foreground));
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
        var box = new NumericUpDown { Width = 200, Value = 3 };
        var window = Show(box, theme: theme);
        try
        {
            var fades = Chrome(box).Transitions!.Cast<TransitionBase>().ToList();
            Assert.Contains(fades, t => t.Property == SquircleBorder.BorderBrushProperty);
            Assert.All(fades, t => Assert.Equal(TimeSpan.FromMilliseconds(100), t.Duration));
            theme.ReduceMotion = true;
            Pump(window);
            Assert.Empty(Chrome(box).Transitions!);
        }
        finally
        {
            window.Close();
        }
    }
}
