using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the DatePicker, TimePicker, Calendar and CalendarDatePicker sheets (2x, labelled), closed and open.</summary>
public class PickerRenderTests
{
    private enum FieldState { Rest, Hover, Focused, Disabled }

    private static readonly FieldState[] FieldStates = Enum.GetValues<FieldState>();
    private static readonly string[] FieldColumns = ["Rest", "Hover", "Keyboard focus", "Disabled"];

    internal static readonly DateTime Date = new(2026, 3, 14);
    internal static readonly TimeSpan Time = new(9, 41, 0);

    [AvaloniaFact]
    public void DatePicker_Renders()
    {
        using var sheet = new Sheet("DatePicker: the field (32 / 24, radius 8, TextControl colours) and the glass flyout (radius 16, items clipped to it)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            sheet.AddSection($"{themeLabel}: field on the glass panel", FieldColumns,
            [
                ("Empty", FieldStates.Select(s => FieldCell(sheet, theme, new DatePicker(), s)).ToList()),
                ("With a date", FieldStates.Select(s => FieldCell(sheet, theme, new DatePicker { SelectedDate = Date }, s)).ToList()),
                ("Compact", FieldStates.Select(s => FieldCell(sheet, theme, new DatePicker { SelectedDate = Date }, s, compact: true)).ToList()),
            ]);
            sheet.AddSection($"{themeLabel}: flyout open", ["Normal, row hovered", "Compact", "Pointer over the month selector", "Keyboard focus on the month selector"],
            [
                ("Over the window", [
                    OpenCell(sheet, theme, new DatePicker { SelectedDate = Date }, OpenFlyout, new Size(340, 340), hover: true),
                    OpenCell(sheet, theme, new DatePicker { SelectedDate = Date }, OpenFlyout, new Size(340, 280), compact: true),
                    OpenCell(sheet, theme, new DatePicker { SelectedDate = Date }, OpenFlyout, new Size(340, 340), state: FlyoutState.PointerOverSelector),
                    OpenCell(sheet, theme, new DatePicker { SelectedDate = Date }, OpenFlyout, new Size(340, 340), state: FlyoutState.SelectorFocused),
                ]),
            ]);
        }
        sheet.Save("m3-datepicker.png");
    }

    [AvaloniaFact]
    public void TimePicker_Renders()
    {
        using var sheet = new Sheet("TimePicker: the field and the glass flyout");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            sheet.AddSection($"{themeLabel}: field on the glass panel", FieldColumns,
            [
                ("Empty", FieldStates.Select(s => FieldCell(sheet, theme, new TimePicker(), s)).ToList()),
                ("With a time", FieldStates.Select(s => FieldCell(sheet, theme, new TimePicker { SelectedTime = Time }, s)).ToList()),
                ("24-hour, compact", FieldStates.Select(s => FieldCell(sheet, theme, new TimePicker { SelectedTime = Time, ClockIdentifier = "24HourClock" }, s, compact: true)).ToList()),
            ]);
            sheet.AddSection($"{themeLabel}: flyout open", ["Normal, row hovered", "24-hour, compact", "Pointer over the hour selector", "Keyboard focus on the hour selector"],
            [
                ("Over the window", [
                    OpenCell(sheet, theme, new TimePicker { SelectedTime = Time }, OpenFlyout, new Size(290, 340), hover: true),
                    OpenCell(sheet, theme, new TimePicker { SelectedTime = Time, ClockIdentifier = "24HourClock" }, OpenFlyout, new Size(290, 280), compact: true),
                    OpenCell(sheet, theme, new TimePicker { SelectedTime = Time }, OpenFlyout, new Size(290, 340), state: FlyoutState.PointerOverSelector),
                    OpenCell(sheet, theme, new TimePicker { SelectedTime = Time }, OpenFlyout, new Size(290, 340), state: FlyoutState.SelectorFocused),
                ]),
            ]);
        }
        sheet.Save("m3-timepicker.png");
    }

    [AvaloniaFact]
    public void Calendar_Renders()
    {
        string[] columns = ["Month (today, selected, hover)", "Keyboard cursor", "Year view", "Compact", "Disabled"];
        using var sheet = new Sheet("Calendar: squircle day pills, accent selection, accent text for today, secondary text outside the month");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, columns,
            [
                ("On the glass panel", [
                    CalendarCell(sheet, theme, hoverDay: true),
                    CalendarCell(sheet, theme, keyboard: true),
                    CalendarCell(sheet, theme, mode: CalendarMode.Year),
                    CalendarCell(sheet, theme, compact: true),
                    CalendarCell(sheet, theme, disabled: true),
                ]),
            ]);
        sheet.Save("m3-calendar.png");
    }

    [AvaloniaFact]
    public void CalendarDatePicker_Renders()
    {
        using var sheet = new Sheet("CalendarDatePicker: the field with a calendar button, and the Calendar in a glass drop-down");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            sheet.AddSection($"{themeLabel}: field on the glass panel", FieldColumns,
            [
                ("Empty", FieldStates.Select(s => FieldCell(sheet, theme, new CalendarDatePicker { Width = 200 }, s)).ToList()),
                ("With a date", FieldStates.Select(s => FieldCell(sheet, theme, new CalendarDatePicker { Width = 200, SelectedDate = Today(14) }, s)).ToList()),
                ("Compact", FieldStates.Select(s => FieldCell(sheet, theme, new CalendarDatePicker { Width = 200, SelectedDate = Today(14) }, s, compact: true)).ToList()),
            ]);
            sheet.AddSection($"{themeLabel}: drop-down open", ["Normal", "Compact"],
            [
                ("Over the window", [
                    OpenCell(sheet, theme, new CalendarDatePicker { Width = 200, SelectedDate = Today(14), IsTodayHighlighted = true }, p => ((CalendarDatePicker)p).IsDropDownOpen = true, new Size(320, 380)),
                    OpenCell(sheet, theme, new CalendarDatePicker { Width = 200, SelectedDate = Today(14), IsTodayHighlighted = true }, p => ((CalendarDatePicker)p).IsDropDownOpen = true, new Size(320, 320), compact: true),
                ]),
            ]);
        }
        sheet.Save("m3-calendardatepicker.png");
    }

    /// <summary>
    /// A day of the current month (so today is shown), clamped to its length. The Calendar and
    /// CalendarDatePicker sheets therefore change with the date they are rendered on.
    /// </summary>
    internal static DateTime Today(int day)
    {
        var today = DateTime.Today;
        var pick = Math.Min(day, DateTime.DaysInMonth(today.Year, today.Month));
        // Keep the selected day apart from today, so both looks show.
        if (pick == today.Day)
            pick = pick > 1 ? pick - 1 : pick + 1;
        return new DateTime(today.Year, today.Month, pick);
    }

    // --- Cells ---------------------------------------------------------------------------

    internal static StandardsTheme Compact() => new(OsAccessibility.Settings.NotReduced) { Density = DensityStyle.Compact };

    private static Control FieldCell(Sheet sheet, ThemeVariant theme, Control picker, FieldState state, bool compact = false)
    {
        if (state == FieldState.Disabled)
            picker.IsEnabled = false;
        return sheet.Cell(ListBoxRenderTests.OnSurface(picker, ListBoxRenderTests.Surface.GlassPanel), theme,
            beforeShow: w =>
            {
                if (compact)
                    w.Styles.Add(Compact());
            },
            afterShow: w =>
            {
                w.UpdateLayout();
                StripTransitions(picker);
                if (state == FieldState.Hover)
                    ((IPseudoClasses)picker.Classes).Set(":pointerover", true);
                if (state == FieldState.Focused)
                    FocusTarget(picker).Focus(NavigationMethod.Tab);
            });
    }

    /// <summary>The part of a picker that takes keyboard focus.</summary>
    internal static InputElement FocusTarget(Control picker) => picker switch
    {
        CalendarDatePicker => picker.GetVisualDescendants().OfType<TextBox>().First(),
        _ => picker.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_FlyoutButton"),
    };

    internal static void OpenFlyout(Control picker) =>
        picker.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_FlyoutButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static Control CalendarCell(Sheet sheet, ThemeVariant theme, bool hoverDay = false, bool keyboard = false,
        CalendarMode mode = CalendarMode.Month, bool compact = false, bool disabled = false)
    {
        var calendar = new Calendar { SelectedDate = Today(14), DisplayDate = Today(14), DisplayMode = mode, IsEnabled = !disabled };
        return sheet.Cell(ListBoxRenderTests.OnSurface(calendar, ListBoxRenderTests.Surface.GlassPanel), theme,
            beforeShow: w =>
            {
                if (compact)
                    w.Styles.Add(Compact());
            },
            afterShow: w =>
            {
                w.UpdateLayout();
                StripTransitions(calendar);
                var days = calendar.GetVisualDescendants().OfType<CalendarDayButton>().ToList();
                if (hoverDay)
                    ((IPseudoClasses)days.First(d => d.Content?.ToString() == "20").Classes).Set(":pointerover", true);
                if (keyboard)
                {
                    days.First(d => d.Classes.Contains(":selected")).Focus(NavigationMethod.Tab);
                    w.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
                    w.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
                }
            });
    }

    /// <summary>What the open flyout shows besides its rest look.</summary>
    internal enum FlyoutState { Rest, RowHovered, PointerOverSelector, SelectorFocused }

    private static Control OpenCell(Sheet sheet, ThemeVariant theme, Control picker, Action<Control> open, Size size, bool compact = false, bool hover = false,
        FlyoutState state = FlyoutState.Rest)
    {
        var window = new Window { Width = size.Width, Height = size.Height, RequestedThemeVariant = theme };
        // The sheets show end states: under reduce motion the overlay appears at once instead of fading in.
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced)
        {
            ReduceMotion = true,
            Density = compact ? DensityStyle.Compact : DensityStyle.Normal,
        });
        var surface = ListBoxRenderTests.OnSurface(picker, ListBoxRenderTests.Surface.GlassPanel);
        surface.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        surface.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        var region = new Border { Padding = new Thickness(16, 12), Child = surface };
        region.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemRegionColor"));
        window.Content = region;
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        try
        {
            Pump(window);
            open(picker);
            Pump(window);
            var popup = picker.GetVisualDescendants().OfType<Popup>().First(p => p.IsOpen);
            var root = TopLevel.GetTopLevel(popup.Child)!;
            StripTransitions((Control)popup.Child!);
            if (hover)
            {
                // The row below the selected one, in the first selector.
                var first = popup.Child!.GetVisualDescendants().OfType<DateTimePickerPanel>().First();
                var selected = first.Children.OfType<ListBoxItem>().First(i => i.IsSelected);
                var item = first.Children.OfType<ListBoxItem>().First(i => Math.Abs(i.Bounds.Top - selected.Bounds.Bottom) < 1);
                ((IPseudoClasses)item.Classes).Set(":pointerover", true);
            }
            var firstSelector = popup.Child!.GetVisualDescendants().OfType<DateTimePickerPanel>().FirstOrDefault();
            if (state == FlyoutState.PointerOverSelector && firstSelector?.FindAncestorOfType<ScrollViewer>()?.Parent is Panel host)
                ((IPseudoClasses)host.Classes).Set(":pointerover", true);
            if (state == FlyoutState.SelectorFocused)
            {
                // The flyout focuses its first selector when it opens; focus moves off and back by Tab.
                popup.Child!.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_DismissButton").Focus(NavigationMethod.Pointer);
                firstSelector?.Focus(NavigationMethod.Tab);
            }
            StripTransitions((Control)popup.Child!);
            Pump(window);
            Pump(root);
            using var capture = root.CaptureRenderedFrame()!;
            return sheet.Image(capture);
        }
        finally
        {
            window.Close();
        }
    }

    internal static void StripTransitions(Control root)
    {
        root.Transitions = null;
        foreach (var part in root.GetVisualDescendants().OfType<Animatable>())
            part.Transitions = null;
    }

    internal static void Pump(TopLevel top)
    {
        Dispatcher.UIThread.RunJobs();
        top.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
