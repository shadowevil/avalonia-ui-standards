using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;

using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>Keyboard access to the DataGrid's column headers: focus in and out, sorting, resizing, the focus ring.</summary>
public class DataGridHeaderKeysTests
{
    private static void Press(Window window, Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        var raw = modifiers == KeyModifiers.Shift ? RawInputModifiers.Shift : RawInputModifiers.None;
        window.KeyPress(key, raw, PhysicalKey.None, null);
        DataGridTests.Pump(window);
    }

    private static (Window Window, DataGrid Grid) Show(ThemeVariant? variant = null, Action<DataGrid>? configure = null)
    {
        var grid = DataGridTests.NewGrid();
        grid.CanUserResizeColumns = true;
        configure?.Invoke(grid);
        var window = new Window { Width = 700, Height = 400, RequestedThemeVariant = variant ?? ThemeVariant.Light };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true });
        window.Content = new StackPanel { Children = { grid } };
        window.Show();
        DataGridTests.Pump(window);
        return (window, grid);
    }

    private static DataGridColumnHeader Header(DataGrid grid, int index) => DataGridTests.Headers(grid)[index];

    private static ListSortDirection? SortOf(DataGrid grid, int index) =>
        Header(grid, index).Classes.Contains(":sortascending") ? ListSortDirection.Ascending
        : Header(grid, index).Classes.Contains(":sortdescending") ? ListSortDirection.Descending : null;

    private static DataGridCell CurrentCell(DataGrid grid) =>
        grid.GetVisualDescendants().OfType<DataGridCell>().Single(c => c.IsVisible && c.Classes.Contains(":current"));

    private static object? Focused(Window window) => window.FocusManager!.GetFocusedElement();

    // Tab into the grid, then arrow to the first row's second cell.
    private static void FocusFirstRowCell(Window window, DataGrid grid, int column)
    {
        grid.SelectedIndex = 0;
        grid.CurrentColumn = grid.Columns[column];
        grid.Focus(NavigationMethod.Tab);
        DataGridTests.Pump(window);
    }

    [AvaloniaFact]
    public void Up_FromTheFirstRow_FocusesTheCurrentColumnsHeader_AndDownReturns()
    {
        var (window, grid) = Show();
        try
        {
            FocusFirstRowCell(window, grid, 1);
            Press(window, Key.Up);
            Assert.Same(Header(grid, 1), Focused(window));

            Press(window, Key.Down);
            Assert.Same(grid, Focused(window));
            Assert.Equal(0, grid.SelectedIndex);
            Assert.Same(grid.Columns[1], grid.CurrentColumn);
            Assert.True(CurrentCell(grid).IsVisible);
            Assert.Same(grid.Columns[1], DataGridColumn.GetColumnContainingElement(CurrentCell(grid)));
            Assert.Equal(0, CurrentCell(grid).FindAncestorOfType<DataGridRow>()!.Index);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Up_FromALaterRow_MovesUpTheRows_NotIntoTheHeaders()
    {
        var (window, grid) = Show();
        try
        {
            grid.SelectedIndex = 2;
            grid.CurrentColumn = grid.Columns[0];
            grid.Focus(NavigationMethod.Tab);
            DataGridTests.Pump(window);
            Press(window, Key.Up);
            Assert.Equal(1, grid.SelectedIndex);
            Assert.Same(grid, Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LeftAndRight_MoveBetweenHeaders_AndStopAtTheEnds()
    {
        var (window, grid) = Show();
        try
        {
            FocusFirstRowCell(window, grid, 0);
            Press(window, Key.Up);
            Assert.Same(Header(grid, 0), Focused(window));
            Press(window, Key.Left);
            Assert.Same(Header(grid, 0), Focused(window));
            Press(window, Key.Right);
            Assert.Same(Header(grid, 1), Focused(window));
            Press(window, Key.Right);
            Press(window, Key.Left);
            Assert.Same(Header(grid, 1), Focused(window));
            for (var i = 0; i < 6; i++)
                Press(window, Key.Right);
            Assert.Same(Header(grid, 4), Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(Key.Enter)]
    [InlineData(Key.Space)]
    public void EnterAndSpace_SortTheColumn_AsAClickDoes(Key key)
    {
        var (window, grid) = Show();
        try
        {
            var sorting = 0;
            grid.Sorting += (_, _) => sorting++;
            FocusFirstRowCell(window, grid, 0);
            Press(window, Key.Up);
            Press(window, key);
            Assert.Equal(ListSortDirection.Ascending, SortOf(grid, 0));
            Assert.Equal("Accent", ((DataGridTests.Item)DataGridTests.Rows(grid)[0].DataContext!).Name);
            Press(window, key);
            Assert.Equal(ListSortDirection.Descending, SortOf(grid, 0));
            Assert.Equal(2, sorting);
            Assert.Same(Header(grid, 0), Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Sorting_IsOff_WhenTheGridOrTheColumnForbidsIt(bool gridForbids, bool columnForbids)
    {
        var (window, grid) = Show(configure: g =>
        {
            g.CanUserSortColumns = !gridForbids;
            g.Columns[0].CanUserSort = !columnForbids;
        });
        try
        {
            FocusFirstRowCell(window, grid, 0);
            Press(window, Key.Up);
            Press(window, Key.Enter);
            Assert.Null(SortOf(grid, 0));
            // Another column, when the grid allows it, still sorts.
            if (!gridForbids)
            {
                Press(window, Key.Right);
                Press(window, Key.Enter);
                Assert.Equal(ListSortDirection.Ascending, SortOf(grid, 1));
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ShiftLeftAndRight_ResizeBy8Pixels()
    {
        var (window, grid) = Show();
        try
        {
            FocusFirstRowCell(window, grid, 1);
            Press(window, Key.Up);
            Assert.Equal(90, grid.Columns[1].ActualWidth);
            Press(window, Key.Right, KeyModifiers.Shift);
            Assert.Equal(98, grid.Columns[1].ActualWidth);
            Assert.Equal(98, Header(grid, 1).Bounds.Width);
            Press(window, Key.Left, KeyModifiers.Shift);
            Press(window, Key.Left, KeyModifiers.Shift);
            Assert.Equal(82, grid.Columns[1].ActualWidth);
            Assert.Same(Header(grid, 1), Focused(window));
            Assert.Null(SortOf(grid, 1));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Resizing_StopsAtTheColumnsMinAndMaxWidth()
    {
        var (window, grid) = Show(configure: g => g.Columns.Insert(1,
            new DataGridTextColumn { Header = "Limited", Width = new DataGridLength(90), MinWidth = 80, MaxWidth = 100 }));
        try
        {
            FocusFirstRowCell(window, grid, 1);
            Press(window, Key.Up);
            for (var i = 0; i < 4; i++)
                Press(window, Key.Right, KeyModifiers.Shift);
            Assert.Equal(100, grid.Columns[1].ActualWidth);
            for (var i = 0; i < 4; i++)
                Press(window, Key.Left, KeyModifiers.Shift);
            Assert.Equal(80, grid.Columns[1].ActualWidth);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Resizing_IsOff_WhenTheGridOrTheColumnForbidsIt(bool gridForbids, bool columnForbids)
    {
        var (window, grid) = Show(configure: g =>
        {
            g.CanUserResizeColumns = !gridForbids;
            g.Columns[1].CanUserResize = !columnForbids;
        });
        try
        {
            FocusFirstRowCell(window, grid, 1);
            Press(window, Key.Up);
            Press(window, Key.Right, KeyModifiers.Shift);
            Assert.Equal(90, grid.Columns[1].ActualWidth);
            Assert.Same(Header(grid, 1), Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Headers_AreNotTabStops_AndTheRingFollowsKeyboardFocus()
    {
        var (window, grid) = Show();
        try
        {
            var header = Header(grid, 0);
            Assert.True(header.Focusable);
            Assert.False(KeyboardNavigation.GetIsTabStop(header));
            var ring = Ring(header);
            Assert.False(ring.IsVisible);

            // Tab in lands on the grid's cells, not a header.
            var before = new Button();
            ((Panel)window.Content!).Children.Insert(0, before);
            DataGridTests.Pump(window);
            before.Focus();
            Press(window, Key.Tab);
            Assert.IsNotType<DataGridColumnHeader>(Focused(window));

            FocusFirstRowCell(window, grid, 0);
            Press(window, Key.Up);
            Assert.True(ring.IsVisible);
            Assert.Equal(new Thickness(2), ring.BorderThickness);
            Assert.Equal(new Rect(2, 2, header.Bounds.Width - 4, header.Bounds.Height - 4), ring.Bounds);
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, "SystemAccentColor")), Effective(ring.BorderBrush));

            // Tab from a header leaves it, as from any other part of the grid.
            Press(window, Key.Tab);
            Assert.False(ring.IsVisible);
            Assert.IsNotType<DataGridColumnHeader>(Focused(window));

            Press(window, Key.Up);
            Press(window, Key.Down);
            Assert.False(ring.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    private static SquircleBorder Ring(DataGridColumnHeader header) =>
        header.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == DataGridTheme.RingPart);

    // --- Render ----------------------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData("light")]
    [InlineData("dark")]
    public void RenderFocusedHeader(string theme)
    {
        var (window, grid) = Show(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light, g => g.Columns[0].Width = new DataGridLength(120));
        try
        {
            window.SetRenderScaling(2);
            FocusFirstRowCell(window, grid, 1);
            Press(window, Key.Up);
            grid.Columns[1].Sort(ListSortDirection.Ascending);
            DataGridTests.Pump(window);
            Assert.True(Ring(Header(grid, 1)).IsVisible);
            using var frame = Frame.Capture(window);
            using var crop = Frame.Of(Frame.Crop(frame.Bitmap, new PixelRect(0, 0, 530 * 2, 132 * 2)));
            crop.Save($"datagrid-header-focus-{theme}.png");
        }
        finally
        {
            window.Close();
        }
    }
}
