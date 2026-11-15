using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// Keyboard access to a DataGrid's column headers, which the package does not focus; the keys
/// are the web grid's (decision 0040, "Header keys"). Up from the first row moves to the current
/// column's header. On a header: Down, Ctrl+Down, Ctrl+Up, PageDown, PageUp and Ctrl+Home or End
/// go to a row, Left, Right, Ctrl+Left, Ctrl+Right, Home and End move between headers, Enter or
/// Space sorts and Shift+Left or Shift+Right resizes. Left and Right swap in a right-to-left
/// grid. A header takes focus only through these keys or a click, never as a Tab stop.
/// </summary>
internal static class DataGridHeaderKeys
{
    /// <summary>How far Shift+Left and Shift+Right resize a column, in pixels.</summary>
    public const double ResizeStep = 8;

    /// <summary>The "jump to the edge" modifier: Cmd on macOS, Ctrl elsewhere.</summary>
    public static KeyModifiers Edge => OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

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
        var key = e.Key;
        if (grid.FlowDirection == FlowDirection.RightToLeft)
            key = key switch { Key.Left => Key.Right, Key.Right => Key.Left, _ => key };
        var plain = e.KeyModifiers == KeyModifiers.None;
        var shift = e.KeyModifiers == KeyModifiers.Shift;
        var edge = e.KeyModifiers == Edge;
        var columns = grid.Columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayIndex).ToList();
        var last = (grid.ItemsSource?.Cast<object>().Count() ?? 0) - 1;
        var at = columns.IndexOf(column);

        switch (key)
        {
            case Key.Left or Key.Right when plain || edge:
                var target = edge ? (key == Key.Right ? columns[^1] : columns[0]) : columns.ElementAtOrDefault(at + (key == Key.Right ? 1 : -1));
                if (target is not null)
                    Focus(grid, target);
                return true;
            case Key.Left or Key.Right when shift:
                Resize(grid, column, columns, key == Key.Right ? ResizeStep : -ResizeStep);
                return true;
            case Key.Home or Key.End when plain:
                Focus(grid, key == Key.Home ? columns[0] : columns[^1]);
                return true;
            case Key.Down when plain:
                GoTo(grid, 0, column);
                return true;
            case Key.Down when edge:
                GoTo(grid, last, column);
                return true;
            case Key.Up when edge:
                GoTo(grid, 0, column);
                return true;
            case Key.Home when edge:
                GoTo(grid, 0, columns[0]);
                return true;
            case Key.End when edge:
                GoTo(grid, last, columns[^1]);
                return true;
            case Key.PageDown when plain:
                GoTo(grid, PageRows(grid, header), column);
                return true;
            case Key.PageUp when plain:
                GoTo(grid, 0, column);
                return true;
            case Key.Enter or Key.Space when plain:
                if (grid.CanUserSortColumns && column.CanUserSort)
                    column.Sort();
                return true;
            // Other navigation keys would move the grid's current cell while a header has focus.
            case Key.Up or Key.Down or Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown:
                return true;
            default:
                return false;
        }
    }

    // The rows that fit under the headers, as the web grid counts a page.
    private static int PageRows(DataGrid grid, DataGridColumnHeader header)
    {
        var row = grid.GetVisualDescendants().OfType<DataGridRow>().FirstOrDefault(r => r.IsVisible && r.Bounds.Height > 0);
        return row is null ? 1 : Math.Max(1, (int)Math.Floor((grid.Bounds.Height - header.Bounds.Height) / row.Bounds.Height));
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
        if (header is null)
            return false;
        if (visible is null)
            header.BringIntoView();
        return header.Focus(NavigationMethod.Directional);
    }

    // The current cell moves to a row (clamped to the rows there are) in a column, selecting the row, as an arrow key would.
    private static void GoTo(DataGrid grid, int row, DataGridColumn column)
    {
        var count = grid.ItemsSource?.Cast<object>().Count() ?? 0;
        if (count == 0)
            return;
        grid.SelectedIndex = Math.Clamp(row, 0, count - 1);
        grid.CurrentColumn = column;
        // The selected item, not the source's row: the grid's rows follow its sort.
        if (grid.SelectedItem is { } item)
            grid.ScrollIntoView(item, column);
        grid.Focus(NavigationMethod.Directional);
    }

    private static void Resize(DataGrid grid, DataGridColumn column, List<DataGridColumn> columns, double delta)
    {
        // As a drag: the last visible column does not resize while it is star-sized.
        if (!grid.CanUserResizeColumns || !column.CanUserResize || (column.Width.IsStar && columns[^1] == column))
            return;
        var width = Math.Min(column.MaxWidth, Math.Max(column.MinWidth, column.ActualWidth + delta));
        column.Width = new DataGridLength(width);
    }
}
