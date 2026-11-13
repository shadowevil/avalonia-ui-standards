using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
using static AvaloniaUiStandards.Tests.PickerHost;

namespace AvaloniaUiStandards.Tests;

/// <summary>Calendar: the panel, day cells, states, header and the keyboard ring.</summary>
public class CalendarThemeTests
{
    [AvaloniaTheory]
    [InlineData("Normal", 32)]
    [InlineData("Compact", 24)]
    public void DayCells_AreSquirclePillsOfTheDensityHeight(string density, double size)
    {
        var (window, calendar) = Show(new Calendar { DisplayDate = PickerRenderTests.Date }, theme: density == "Compact" ? PickerRenderTests.Compact() : null, height: 500);
        try
        {
            var day = calendar.GetVisualDescendants().OfType<CalendarDayButton>().First();
            Assert.Equal(new Size(size, size), day.Bounds.Size);
            Assert.Equal(new CornerRadius(8), day.CornerRadius);
            Assert.Equal(ButtonParts.ChromePart, day.GetVisualDescendants().OfType<SquircleBorder>().First().Name);
            var chrome = calendar.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == CalendarThemes.ChromePart);
            Assert.Equal(new CornerRadius(16), chrome.CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    // Expected fill and text per day state (null fill = transparent).
    public static TheoryData<string, string, string?, string> DayStates()
    {
        var data = new TheoryData<string, string, string?, string>();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            data.Add(theme, "rest", null, "SystemControlForegroundBaseHighBrush");
            data.Add(theme, ":inactive", null, "SystemControlForegroundBaseMediumBrush");
            data.Add(theme, ":today", null, "AccentTextColor");
            data.Add(theme, ":pointerover", "SystemControlHighlightListMediumBrush", "SystemControlForegroundBaseHighBrush");
            data.Add(theme, ":pressed", "SystemControlHighlightBaseMediumLowBrush", "SystemControlForegroundBaseHighBrush");
            data.Add(theme, ":selected", "AccentFillColor", "AccentButtonForeground");
            data.Add(theme, ":selected:pointerover", "AccentFillColorPointerOver", "AccentButtonForeground");
            data.Add(theme, ":today:selected", "AccentFillColor", "AccentButtonForeground");
            data.Add(theme, ":blackout", null, "SystemControlDisabledBaseMediumLowBrush");
        }
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(DayStates))]
    public void DayStates_Resolve(string theme, string state, string? fillKey, string textKey)
    {
        var (window, calendar) = Show(new Calendar { DisplayDate = PickerRenderTests.Date },
            variant: theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, height: 500);
        try
        {
            // A day with no state of its own, so only the states set here apply.
            var day = calendar.GetVisualDescendants().OfType<CalendarDayButton>().First(d => d.Classes.All(c => !c.StartsWith(':')));
            foreach (var pseudo in state.Split(':', StringSplitOptions.RemoveEmptyEntries).Where(_ => state != "rest"))
                ((IPseudoClasses)day.Classes).Set(":" + pseudo, true);
            Pump(window);

            var chrome = day.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart);
            var fill = chrome.GetBaseValue(SquircleBorder.BackgroundProperty) is { HasValue: true } set ? Effective(set.Value) : Effective(chrome.Background);
            var text = (Avalonia.Controls.Presenters.ContentPresenter)chrome.Child!;
            Assert.Equal(fillKey is null ? Colors.Transparent : Effective(Resolve(window, fillKey)), fill);
            Assert.Equal(Effective(Resolve(window, textKey)), Effective(text.Foreground));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Header_HasSubtleCaretButtonsAndATitleButton()
    {
        var (window, calendar) = Show(new Calendar { DisplayDate = PickerRenderTests.Date }, height: 500);
        try
        {
            Button Part(string name) => calendar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == name);
            Assert.Contains("subtle", Part("PART_HeaderButton").Classes);
            Assert.Equal(FontWeight.SemiBold, Part("PART_HeaderButton").FontWeight);
            foreach (var (name, icon) in new[] { ("PART_PreviousButton", Icons.CaretLeft), ("PART_NextButton", Icons.CaretRight) })
            {
                Assert.Contains("subtle", Part(name).Classes);
                Assert.Same(icon, Assert.IsType<Icon>(Part(name).Content).Data);
                Assert.Equal(new Size(32, 32), Part(name).Bounds.Size);
            }

            // The arrows still page the month, as in Fluent.
            Part("PART_NextButton").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Pump(window);
            Assert.Equal(PickerRenderTests.Date.Month + 1, calendar.DisplayDate.Month);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Keyboard_MovesTheSelectionAndTheRingFollowsIt()
    {
        var calendar = new Calendar { SelectedDate = PickerRenderTests.Date, DisplayDate = PickerRenderTests.Date };
        var (window, _) = Show(calendar, height: 500);
        try
        {
            CalendarDayButton Day(int day) => calendar.GetVisualDescendants().OfType<CalendarDayButton>()
                .First(d => d.Content?.ToString() == day.ToString() && !d.Classes.Contains(":inactive"));
            SquircleBorder Ring(Control day) => day.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == FocusRing.Part);

            Day(14).Focus(NavigationMethod.Tab);
            window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            Pump(window);

            Assert.Equal(PickerRenderTests.Date.AddDays(8), calendar.SelectedDate);
            Assert.True(Ring(Day(22)).IsVisible);
            Assert.False(Ring(Day(14)).IsVisible);

            // Focus by pointer shows no ring.
            window.Focus();
            Day(10).Focus(NavigationMethod.Pointer);
            Pump(window);
            Assert.All(calendar.GetVisualDescendants().OfType<CalendarDayButton>(), d => Assert.False(Ring(d).IsVisible));
        }
        finally
        {
            window.Close();
        }
    }
}
