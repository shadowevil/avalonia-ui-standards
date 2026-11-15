using Avalonia;
using Avalonia.Collections;
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
/// column's header. On a header: Down, Edge+Down, Edge+Up, PageDown, PageUp and Edge+Home or End
/// go to a row (with Shift they extend the selection), Left, Right, Edge+Left, Edge+Right, Home
/// and End move between headers, Enter or Space sorts, Shift+Left or Shift+Right resizes and
/// Edge+A selects all rows in extended selection mode. Left and Right swap in a right-to-left
/// grid. "Edge" is Cmd on macOS and Ctrl elsewhere. A header takes focus only through these keys
/// or a click, never as a Tab stop.
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
    // The first row is the first item, which with grouping is not the row at index 0 (a group header is).
    private static bool EnterHeaders(DataGrid grid)
    {
        var cell = grid.GetVisualDescendants().OfType<DataGridCell>().FirstOrDefault(c => c.IsVisible && c.Classes.Contains(":current"));
        if (cell is null || grid.HeadersVisibility is not (DataGridHeadersVisibility.Column or DataGridHeadersVisibility.All)
            || cell.FindAncestorOfType<DataGridRow>() is not { DataContext: { } item }
            || !ReferenceEquals(item, Items(grid).FirstOrDefault())
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
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var bare = e.KeyModifiers & ~KeyModifiers.Shift;
        var plain = bare == KeyModifiers.None;
        var edge = bare == Edge;
        var columns = grid.Columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayIndex).ToList();
        var at = columns.IndexOf(column);

        switch (key)
        {
            case Key.Left or Key.Right when !shift && (plain || edge):
                var target = edge ? (key == Key.Right ? columns[^1] : columns[0]) : columns.ElementAtOrDefault(at + (key == Key.Right ? 1 : -1));
                if (target is not null)
                    Focus(grid, target);
                return true;
            case Key.Left or Key.Right when shift && plain:
                Resize(grid, column, columns, key == Key.Right ? ResizeStep : -ResizeStep);
                return true;
            case Key.Home or Key.End when plain:
                Focus(grid, key == Key.Home ? columns[0] : columns[^1]);
                return true;
            case Key.Down when plain:
            case Key.Up when edge:
            case Key.PageUp when plain:
                GoTo(grid, _ => 0, column, shift);
                return true;
            case Key.Down when edge:
                GoTo(grid, count => count - 1, column, shift);
                return true;
            case Key.Home when edge:
                GoTo(grid, _ => 0, columns[0], shift);
                return true;
            case Key.End when edge:
                GoTo(grid, count => count - 1, columns[^1], shift);
                return true;
            case Key.PageDown when plain:
                var page = PageRows(grid, header);
                GoTo(grid, _ => page, column, shift);
                return true;
            case Key.A when edge && !shift:
                if (grid.SelectionMode == DataGridSelectionMode.Extended)
                    grid.SelectAll();
                return true;
            case Key.Enter or Key.Space when plain && !shift:
                if (grid.CanUserSortColumns && column.CanUserSort)
                    column.Sort();
                return true;
            // Other navigation keys would move the grid's current cell while a header has focus.
            // Enter and Space with a modifier would act on the cell behind the header too.
            case Key.Up or Key.Down or Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown or Key.Enter or Key.Space:
                return true;
            default:
                return false;
        }
    }

    // The grid's rows in its own order (sorted, filtered, grouped), without group headers.
    private static IEnumerable<object> Items(DataGrid grid) => grid.CollectionView?.Cast<object>() ?? [];

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
        // With no rows there is nothing to scroll a column through (the package scrolls the headers
        // only with the rows), so focus stays on the headers that start inside the grid.
        if (visible is null && header.TranslatePoint(default, grid) is { } origin && (origin.X >= grid.Bounds.Width || origin.X + header.Bounds.Width <= 0))
            return false;
        return header.Focus(NavigationMethod.Directional);
    }

    // The row where a Shift-extended selection started: kept across presses while it stays selected,
    // so the range grows from it rather than from the row the last press landed on.
    private static readonly AttachedProperty<object?> AnchorProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, DataGrid, object?>("SelectionAnchor");

    // The current cell moves to a row (clamped to the rows there are) in a column, selecting the row as an arrow
    // key would, or with <paramref name="extend"/> in extended selection mode the rows from the selection's anchor.
    private static void GoTo(DataGrid grid, Func<int, int> row, DataGridColumn column, bool extend)
    {
        var count = grid.CollectionView switch { DataGridCollectionView view => view.Count, { } view => view.Cast<object>().Count(), _ => 0 };
        if (count == 0)
            return;
        var index = Math.Clamp(row(count), 0, count - 1);
        var anchor = grid.GetValue(AnchorProperty);
        if (anchor is null || !grid.SelectedItems.Contains(anchor))
            anchor = grid.SelectedItem;
        var items = Items(grid).ToList();
        var anchorIndex = anchor is null ? -1 : items.IndexOf(anchor);
        grid.SelectedIndex = index;
        if (extend && grid.SelectionMode == DataGridSelectionMode.Extended && anchorIndex >= 0)
        {
            var (from, to) = (Math.Min(anchorIndex, index), Math.Max(anchorIndex, index));
            grid.SelectedItems.Clear();
            foreach (var item in items.Skip(from).Take(to - from + 1))
                grid.SelectedItems.Add(item);
            grid.SetValue(AnchorProperty, anchor);
        }
        else
            grid.SetValue(AnchorProperty, null);
        grid.CurrentColumn = column;
        // The selected item, not the source's row: the grid's rows follow its sort.
        if (grid.SelectedItem is { } selected)
            grid.ScrollIntoView(selected, column);
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
