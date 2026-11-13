using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
    /// The part of a field that holds its states, and the part that draws its ring. An
    /// AutoCompleteBox's TextBox holds both; a NumericUpDown holds its states and its ButtonSpinner
    /// draws the ring (shown, as when focus is on the inner TextBox, through that part's
    /// <c>:focus-visible-within</c>); other fields hold both themselves.
    /// </summary>
    private static (Control States, Control Ring) Targets(Control control) => control switch
    {
        AutoCompleteBox => Part<TextBox>(control, "PART_TextBox") is { } text ? (text, text) : (control, control),
        NumericUpDown => (control, Part<ButtonSpinner>(control, "PART_Spinner") ?? (Control)control),
        _ => (control, control),
    };

    private static T? Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().FirstOrDefault(c => c.Name == name);

    /// <summary>
    /// Wraps a field in a non-interactive host and forces it into <paramref name="state"/>. Focus
    /// is shown through the pseudo-classes alone, so the samples never take real focus.
    /// </summary>
    private static Control Sample(Control control, FieldSampleState state)
    {
        control.IsEnabled = state != FieldSampleState.Disabled;
        // Forced once attached (its template is built by then): a control resets some of its
        // pseudo-classes when it joins the tree.
        control.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            if (control is TemplatedControl templated)
                templated.ApplyTemplate();
            var (states, ring) = Targets(control);
            var pseudo = (IPseudoClasses)states.Classes;
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
                    {
                        // :focus-visible lights a TextBox ring, :focus-visible-within a composite field's.
                        ((IPseudoClasses)ring.Classes).Set(":focus-visible", true);
                        ((IPseudoClasses)ring.Classes).Set(":focus-visible-within", true);
                    }
                    break;
            }
        });
        return new Border { Padding = new Thickness(12), IsHitTestVisible = false, Child = control };
    }
}
