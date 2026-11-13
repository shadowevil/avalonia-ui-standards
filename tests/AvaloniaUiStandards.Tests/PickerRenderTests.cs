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

/// <summary>Renders the DatePicker and TimePicker sheets (2x, labelled), closed and open.</summary>
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
            sheet.AddSection($"{themeLabel}: flyout open", ["Normal, row hovered", "Compact"],
            [
                ("Over the window", [
                    OpenCell(sheet, theme, new DatePicker { SelectedDate = Date }, OpenFlyout, new Size(340, 340), hover: true),
                    OpenCell(sheet, theme, new DatePicker { SelectedDate = Date }, OpenFlyout, new Size(340, 280), compact: true),
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
            sheet.AddSection($"{themeLabel}: flyout open", ["Normal, row hovered", "24-hour, compact"],
            [
                ("Over the window", [
                    OpenCell(sheet, theme, new TimePicker { SelectedTime = Time }, OpenFlyout, new Size(290, 340), hover: true),
                    OpenCell(sheet, theme, new TimePicker { SelectedTime = Time, ClockIdentifier = "24HourClock" }, OpenFlyout, new Size(290, 280), compact: true),
                ]),
            ]);
        }
        sheet.Save("m3-timepicker.png");
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
    internal static InputElement FocusTarget(Control picker) =>
        picker.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_FlyoutButton");

    internal static void OpenFlyout(Control picker) =>
        picker.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_FlyoutButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static Control OpenCell(Sheet sheet, ThemeVariant theme, Control picker, Action<Control> open, Size size, bool compact = false, bool hover = false)
    {
        var window = new Window { Width = size.Width, Height = size.Height, RequestedThemeVariant = theme };
        if (compact)
            window.Styles.Add(Compact());
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
