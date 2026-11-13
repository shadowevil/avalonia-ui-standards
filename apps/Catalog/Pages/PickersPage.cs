using Avalonia;
using Avalonia.Controls;

namespace Catalog.Pages;

/// <summary>DatePicker, TimePicker and their flyouts.</summary>
internal sealed class PickersPage : UserControl
{
    private static readonly DateTime Sample = DateTime.Today;

    public PickersPage() => Content = Ui.Page("Pickers",
        Ui.Section("DatePicker", StateGrid.Build(
        [
            ("Empty", () => new DatePicker()),
            ("With a date", () => new DatePicker { SelectedDate = Sample }),
        ])),
        Ui.Section("TimePicker", StateGrid.Build(
        [
            ("12-hour", () => new TimePicker { SelectedTime = new TimeSpan(9, 41, 0) }),
            ("24-hour", () => new TimePicker { SelectedTime = new TimeSpan(21, 41, 0), ClockIdentifier = "24HourClock" }),
        ])),
        Ui.Section("Flyouts (shown inline)", Row(
            Labelled("DatePicker", new DatePickerPresenter { Date = Sample }),
            Labelled("TimePicker", new TimePickerPresenter { Time = new TimeSpan(9, 41, 0) }))));

    private static Control Row(params Control[] items)
    {
        var row = new WrapPanel { ItemSpacing = 24, LineSpacing = 24 };
        foreach (var item in items)
            row.Children.Add(item);
        return row;
    }

    private static Control Labelled(string label, Control control)
    {
        control.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        var caption = Ui.Caption(label);
        caption.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
        caption.MaxWidth = 280;
        // Only the sample scrolls sideways when it is wider than the page (the 296 px flyout); the page never does.
        var scroller = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = control,
        };
        return new StackPanel { Spacing = 8, Children = { caption, scroller } };
    }
}
