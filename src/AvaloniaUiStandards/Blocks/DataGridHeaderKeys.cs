using System.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// Keyboard access to a DataGrid's column headers, which the package does not focus: Up from the
/// first row moves to the current column's header, Down returns to the first row, Left and Right
/// move between headers, Enter or Space sorts the column and Shift+Left or Shift+Right resizes it.
/// A header takes focus only through these keys or a click, never as a Tab stop.
/// </summary>
internal static class DataGridHeaderKeys
{
    /// <summary>How far Shift+Left and Shift+Right resize a column, in pixels.</summary>
    public const double ResizeStep = 8;

    public static void Register() =>
        InputElement.KeyDownEvent.AddClassHandler<DataGrid>(OnKeyDown, RoutingStrategies.Tunnel);

    private static void OnKeyDown(DataGrid grid, KeyEventArgs e)
    {
        if (e.Handled)
            return;
        if (e.Source is DataGridColumnHeader header)
            e.Handled = OnHeaderKey(grid, header, e);
        else if (e.Key == Key.Up && e.KeyModifiers == KeyModifiers.None && e.Source is DataGrid or DataGridCell)
            e.Handled = EnterHeaders(grid);
    }

    // The package keeps focus on the grid while arrow keys move the current cell, so the cell is the one marked current.
    private static bool EnterHeaders(DataGrid grid)
    {
        var cell = grid.GetVisualDescendants().OfType<DataGridCell>().FirstOrDefault(c => c.IsVisible && c.Classes.Contains(":current"));
        if (cell is null || grid.HeadersVisibility is not (DataGridHeadersVisibility.Column or DataGridHeadersVisibility.All)
            || cell.FindAncestorOfType<DataGridRow>() is not { Index: 0 }
            || DataGridColumn.GetColumnContainingElement(cell) is not { } column)
            return false;
        return Focus(grid, column);
    }

    private static bool OnHeaderKey(DataGrid grid, DataGridColumnHeader header, KeyEventArgs e)
    {
        if (DataGridColumn.GetColumnContainingElement(header) is not { } column || !grid.Columns.Contains(column))
            return false;
        var shift = e.KeyModifiers == KeyModifiers.Shift;
        var plain = e.KeyModifiers == KeyModifiers.None;
        switch (e.Key)
        {
            case Key.Left or Key.Right when plain:
                var next = Neighbour(grid, column, e.Key == Key.Right ? 1 : -1);
                if (next is not null)
                    Focus(grid, next);
                return true;
            case Key.Left or Key.Right when shift:
                Resize(grid, column, e.Key == Key.Right ? ResizeStep : -ResizeStep);
                return true;
            case Key.Down when plain:
                return LeaveHeaders(grid, column);
            case Key.Enter or Key.Space when plain:
                if (grid.CanUserSortColumns && column.CanUserSort)
                    column.Sort();
                return true;
            case Key.Home or Key.End when plain:
                var ends = grid.Columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayIndex).ToList();
                if (ends.Count > 0)
                    Focus(grid, e.Key == Key.Home ? ends[0] : ends[^1]);
                return true;
            // Other navigation keys would move the grid's current cell while a header has focus.
            case Key.Up or Key.Down or Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown:
                return true;
            default:
                return false;
        }
    }

    // The visible column one step along the display order, or null at either end.
    private static DataGridColumn? Neighbour(DataGrid grid, DataGridColumn column, int step)
    {
        var columns = grid.Columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayIndex).ToList();
        var index = columns.IndexOf(column) + step;
        return index >= 0 && index < columns.Count ? columns[index] : null;
    }

    private static bool Focus(DataGrid grid, DataGridColumn column)
    {
        // Scroll to the column only: through a row already in view, so the rows stay where they are.
        var visible = grid.GetVisualDescendants().OfType<DataGridRow>().Where(r => r.IsVisible).MinBy(r => r.Index);
        if (visible?.DataContext is { } item)
            grid.ScrollIntoView(item, column);
        grid.UpdateLayout();
        var header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>()
            .FirstOrDefault(h => h.IsVisible && ReferenceEquals(DataGridColumn.GetColumnContainingElement(h), column));
        return header?.Focus(NavigationMethod.Directional) == true;
    }

    private static bool LeaveHeaders(DataGrid grid, DataGridColumn column)
    {
        if (grid.ItemsSource is not IEnumerable items || !items.Cast<object>().Any())
            return true;
        grid.SelectedIndex = 0;
        grid.CurrentColumn = column;
        grid.Focus(NavigationMethod.Directional);
        return true;
    }

    private static void Resize(DataGrid grid, DataGridColumn column, double delta)
    {
        // As a drag: the last visible column does not resize while it is star-sized.
        if (!grid.CanUserResizeColumns || !column.CanUserResize || (column.Width.IsStar && Neighbour(grid, column, 1) is null))
            return;
        var width = Math.Min(column.MaxWidth, Math.Max(column.MinWidth, column.ActualWidth + delta));
        column.Width = new DataGridLength(width);
    }
}
