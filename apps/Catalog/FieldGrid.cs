using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using Catalog.Pages;

namespace Catalog;

/// <summary>The states shown for a field Block.</summary>
internal enum FieldSampleState { Rest, Hover, Focused, KeyboardFocus, Error, Disabled }

/// <summary>
/// A grid of a field Block (TextInput, ComboBox, NumericUpDown, AutoCompleteBox) in every kind and
/// state, forced through pseudo-classes, with a live interactive field at the end of each row.
/// </summary>
internal static class FieldGrid
{
    private static readonly FieldSampleState[] States = Enum.GetValues<FieldSampleState>();

    public static Control Build(IReadOnlyList<(string Label, Func<Control> Create)> kinds)
    {
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 4 };
        for (var c = 0; c <= States.Length + 1; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(c == 0 ? new GridLength(150) : GridLength.Auto));

        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var c = 0; c < States.Length; c++)
            Place(grid, Ui.Caption(Label(States[c])), 0, c + 1, HorizontalAlignment.Center);
        Place(grid, Ui.Caption("Live"), 0, States.Length + 1, HorizontalAlignment.Center);

        for (var r = 0; r < kinds.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var label = Ui.Body(kinds[r].Label);
            label.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
            Place(grid, label, r + 1, 0, HorizontalAlignment.Left);
            for (var c = 0; c < States.Length; c++)
                Place(grid, Sample(kinds[r].Create(), States[c]), r + 1, c + 1, HorizontalAlignment.Center);
            Place(grid, new Border { Padding = new Thickness(12), Child = kinds[r].Create() }, r + 1, States.Length + 1, HorizontalAlignment.Center);
        }
        // Only the grid scrolls sideways when it cannot fit; the page never does.
        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = grid,
        };
    }

    private static string Label(FieldSampleState state) => state switch
    {
        FieldSampleState.KeyboardFocus => "Keyboard focus",
        _ => state.ToString(),
    };

    private static void Place(Grid grid, Control control, int row, int column, HorizontalAlignment alignment)
    {
        control.HorizontalAlignment = alignment;
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        grid.Children.Add(control);
    }

    /// <summary>
    /// Wraps a field in a non-interactive host and forces it into <paramref name="state"/>. Focus
    /// is shown through the pseudo-classes alone, so the samples never take real focus.
    /// </summary>
    private static Control Sample(Control control, FieldSampleState state)
    {
        control.IsEnabled = state != FieldSampleState.Disabled;
        // Forced once attached: a control resets some of its pseudo-classes when it joins the tree.
        control.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            var pseudo = (IPseudoClasses)control.Classes;
            switch (state)
            {
                case FieldSampleState.Hover:
                    pseudo.Set(":pointerover", true);
                    break;
                case FieldSampleState.Error:
                    pseudo.Set(":error", true);
                    break;
                case FieldSampleState.Focused or FieldSampleState.KeyboardFocus:
                    pseudo.Set(":focus", true);
                    pseudo.Set(":focus-within", true);
                    if (state == FieldSampleState.KeyboardFocus)
                        pseudo.Set(":focus-visible", true);
                    break;
            }
        });
        return new Border { Padding = new Thickness(12), IsHitTestVisible = false, Child = control };
    }
}
