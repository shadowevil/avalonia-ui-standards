using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Catalog.Pages;

namespace Catalog;

/// <summary>The interaction states shown for a control.</summary>
internal enum SampleState { Rest, Hover, Pressed, Disabled, Focused }

/// <summary>
/// A grid of one control in every variant and state, forced through pseudo-classes, with a live
/// interactive control at the end of each row.
/// </summary>
internal static class StateGrid
{
    private static readonly SampleState[] States = Enum.GetValues<SampleState>();

    public static Control Build(IReadOnlyList<(string Label, Func<Control> Create)> variants)
    {
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 4 };
        for (var c = 0; c <= States.Length + 1; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(c == 0 ? new GridLength(150) : GridLength.Auto));

        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var c = 0; c < States.Length; c++)
            Place(grid, Ui.Caption(States[c].ToString()), 0, c + 1, HorizontalAlignment.Center);
        Place(grid, Ui.Caption("Live"), 0, States.Length + 1, HorizontalAlignment.Center);

        for (var r = 0; r < variants.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var label = Ui.Body(variants[r].Label);
            label.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
            Place(grid, label, r + 1, 0, HorizontalAlignment.Left);
            for (var c = 0; c < States.Length; c++)
                Place(grid, Sample(variants[r].Create(), States[c]), r + 1, c + 1, HorizontalAlignment.Center);
            var live = new Border { Padding = new Thickness(12), Child = variants[r].Create() };
            Place(grid, live, r + 1, States.Length + 1, HorizontalAlignment.Center);
        }
        // Only the grid scrolls sideways when it cannot fit; the page never does.
        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = grid,
        };
    }

    private static void Place(Grid grid, Control control, int row, int column, HorizontalAlignment alignment)
    {
        control.HorizontalAlignment = alignment;
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        grid.Children.Add(control);
    }

    /// <summary>Wraps a control in a non-interactive host and forces it into <paramref name="state"/>.</summary>
    private static Control Sample(Control control, SampleState state)
    {
        control.IsEnabled = state != SampleState.Disabled;
        AdornerLayer? layer = null;
        Control? ring = null;

        // Forced once attached: a control resets some of its pseudo-classes when it joins the tree.
        control.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            // A split button's hover and press happen on its primary part.
            var target = control.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == "PART_PrimaryButton") ?? control;
            var pseudo = (IPseudoClasses)target.Classes;
            if (state == SampleState.Hover)
                pseudo.Set(":pointerover", true);
            if (state == SampleState.Pressed)
                pseudo.Set(":pressed", true);
            if (state == SampleState.Focused && ring is null && control.FocusAdorner?.Build() is { } adorner)
            {
                layer = AdornerLayer.GetAdornerLayer(control);
                if (layer is null)
                    return;
                AdornerLayer.SetAdornedElement(adorner, control);
                layer.Children.Add(adorner);
                ring = adorner;
            }
        });
        control.DetachedFromVisualTree += (_, _) =>
        {
            if (ring is not null)
                layer?.Children.Remove(ring);
            ring = null;
        };
        return new Border { Padding = new Thickness(12), IsHitTestVisible = false, Child = control };
    }
}
