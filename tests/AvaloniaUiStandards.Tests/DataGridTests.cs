using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;

using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>The DataGrid restyle: rows, headers, sort caret, cell focus, editing, density, themes and renders.</summary>
public class DataGridTests
{
    // --- Theme and behaviour -------------------------------------------------------------

    [AvaloniaFact]
    public void StandardsTheme_StylesDataGrid_WithNoExtraStep()
    {
        // The app's theme is only StandardsTheme (TestApp); the grid gets the library themes.
        var (window, grid) = Show(useAppTheme: true);
        try
        {
            Assert.Same(Application.Current!.FindResource(typeof(DataGrid)), grid.FindResource(typeof(DataGrid)));
            Assert.Equal(DataGridGridLinesVisibility.Horizontal, grid.GridLinesVisibility);
            Assert.Equal(DataGridHeadersVisibility.Column, grid.HeadersVisibility);
            Assert.IsType<SquircleBorder>(grid.GetVisualChildren().Single());
            Assert.NotEmpty(Rows(grid));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Normal", 32)]
    [InlineData("Compact", 24)]
    public void Rows_AreFlatBandsOfTheListItemHeight_WithHairlinesOverTheirBottomEdge(string density, double height)
    {
        var (window, grid) = Show(density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal);
        try
        {
            foreach (var row in Rows(grid))
            {
                Assert.Equal(height, row.Bounds.Height);
                var chrome = RowChrome(row);
                Assert.Equal(default, chrome.CornerRadius);
                Assert.Equal(row.Bounds.Width, chrome.Bounds.Width);
                var line = row.GetVisualDescendants().OfType<Rectangle>().Single(r => r.Name == "PART_BottomGridLine");
                Assert.True(line.IsVisible);
                Assert.Equal(height - 1, line.TranslatePoint(default, row)!.Value.Y);
                Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, "SystemBaseLowColor")), Effective(line.Fill));
            }
            foreach (var header in Headers(grid))
                Assert.Equal(height, header.Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Selection_AndHover_UseTheListItemColours(string theme)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, grid) = Show(variant: variant);
        try
        {
            grid.SelectedIndex = 1;
            Pump(window);
            var rows = Rows(grid);
            Assert.Equal(Effective(window.FindResource(variant, "AccentFillColor")), Effective(RowChrome(rows[1]).Background));
            Assert.Equal(Effective(window.FindResource(variant, "AccentButtonForeground")), Effective(rows[1].Foreground));
            Assert.True(rows[1].GetValue(DataGridTheme.OnAccentProperty));

            ((IPseudoClasses)rows[3].Classes).Set(":pointerover", true);
            ((IPseudoClasses)rows[1].Classes).Set(":pointerover", true);
            Pump(window);
            Assert.Equal(Effective(window.FindResource(variant, "SystemListMediumColor")), Effective(RowChrome(rows[3]).Background));
            Assert.Equal(Effective(window.FindResource(variant, "AccentFillColorPointerOver")), Effective(RowChrome(rows[1]).Background));
            Assert.Equal(Colors.Transparent, Effective(RowChrome(rows[2]).Background));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SortCaret_IsUpWhenAscending_DownWhenDescending_AndHiddenUnsorted()
    {
        var (window, grid) = Show();
        try
        {
            var header = Headers(grid)[0];
            var caret = header.GetVisualDescendants().OfType<Icon>().Single(i => i.Name == DataGridTheme.SortIconPart);
            Assert.False(caret.IsVisible);

            grid.Columns[0].Sort(ListSortDirection.Ascending);
            Pump(window);
            Assert.True(caret.IsVisible);
            Assert.Same(Icons.CaretUp, caret.Data);
            Assert.Equal(new Size(16, 16), caret.Bounds.Size);
            // After the text, SpacingXS apart.
            var text = header.GetVisualDescendants().OfType<TextBlock>().Single();
            Assert.Equal(text.TranslatePoint(new Point(text.Bounds.Width, 0), header)!.Value.X + 4, caret.TranslatePoint(default, header)!.Value.X, 1);

            grid.Columns[0].Sort(ListSortDirection.Descending);
            Pump(window);
            Assert.Same(Icons.CaretDown, caret.Data);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Headers_AreBodyStrongInTheSecondaryColour_OverAHairline(string theme)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, grid) = Show(variant: variant);
        try
        {
            var header = Headers(grid)[1];
            Assert.Equal(14, header.FontSize);
            Assert.Equal(FontWeight.SemiBold, header.FontWeight);
            Assert.Equal(Effective(window.FindResource(variant, "SystemBaseMediumColor")), Effective(header.Foreground));
            Assert.Equal(Colors.Transparent, Effective(header.Background));
            var separator = grid.GetVisualDescendants().OfType<Rectangle>().Single(r => r.Name == "PART_ColumnHeadersAndRowsSeparator");
            Assert.Equal(Effective(window.FindResource(variant, "SystemBaseLowColor")), Effective(separator.Fill));
            // No separators between headers without vertical grid lines.
            Assert.Equal(0, HeaderSeparator(header).Opacity);
            grid.GridLinesVisibility = DataGridGridLinesVisibility.All;
            Pump(window);
            Assert.Equal(1, HeaderSeparator(header).Opacity);

            ((IPseudoClasses)header.Classes).Set(":pointerover", true);
            Pump(window);
            Assert.Equal(Effective(window.FindResource(variant, "SystemListMediumColor")),
                Effective(header.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart).Background));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ResizeGrip_ShowsNearAResizableEdge_AndTakesTheDragColourWhileResizing()
    {
        var (window, grid) = Show(reduceMotion: true);
        try
        {
            var header = Headers(grid)[1];
            grid.CanUserResizeColumns = true; // off by default in the package
            var right = Grip(header, DataGridTheme.GripRightPart);
            var left = Grip(header, DataGridTheme.GripLeftPart);
            Assert.Equal(0, right.Opacity);
            Assert.Equal(new Size(4, 24), right.Bounds.Size);
            var origin = header.TranslatePoint(default, window)!.Value;

            window.MouseMove(origin + new Point(header.Bounds.Width / 2, 16));
            Pump(window);
            Assert.Equal(0, right.Opacity);
            Assert.Equal(0, left.Opacity);

            window.MouseMove(origin + new Point(header.Bounds.Width - 2, 16));
            Pump(window);
            Assert.Equal(1, right.Opacity);
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, ScrollBarTheme.ThumbHoverKey)), Effective(right.Background));

            window.MouseDown(origin + new Point(header.Bounds.Width - 2, 16), MouseButton.Left);
            Pump(window);
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, ScrollBarTheme.ThumbDragKey)), Effective(right.Background));
            window.MouseUp(origin + new Point(header.Bounds.Width - 2, 16), MouseButton.Left);

            window.MouseMove(origin + new Point(2, 16));
            Pump(window);
            Assert.Equal(1, left.Opacity);
            Assert.Equal(0, right.Opacity);

            // A column that cannot resize shows no grip.
            grid.Columns[1].CanUserResize = false;
            window.MouseMove(origin + new Point(header.Bounds.Width - 2, 16));
            Pump(window);
            Assert.Equal(0, right.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CellFocusRing_ShowsOnTheCurrentCell_FromTheKeyboardOnly()
    {
        var (window, grid) = Show();
        try
        {
            grid.SelectedIndex = 1;
            grid.CurrentColumn = grid.Columns[1];
            Pump(window);
            var cell = CurrentCell(grid);
            var ring = Ring(cell);
            Assert.False(ring.IsVisible);

            grid.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.True(ring.IsVisible);
            // Inset 2 inside the cell, a 2 px ring, in the selected text colour on the accent band.
            Assert.Equal(new Thickness(2), ring.BorderThickness);
            Assert.Equal(new Rect(2, 2, cell.Bounds.Width - 4, cell.Bounds.Height - 4), ring.Bounds);
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, "AccentButtonForeground")), Effective(ring.BorderBrush));

            // A pointer press hides it; an arrow key brings it back on the new current cell.
            var point = Rows(grid)[3].TranslatePoint(new Point(150, 10), window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Pump(window);
            Assert.False(Ring(CurrentCell(grid)).IsVisible);
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Pump(window);
            Assert.True(Ring(CurrentCell(grid)).IsVisible);
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, "AccentButtonForeground")), Effective(Ring(CurrentCell(grid)).BorderBrush));

            // Focus leaving the grid hides it.
            var other = new Button();
            ((Panel)window.Content!).Children.Add(other);
            Pump(window);
            other.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.False(Ring(CurrentCell(grid)).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Normal", 32, 11, 4)]
    [InlineData("Compact", 24, 7, 2)]
    public void EditMode_IsATextInputFieldFillingTheCell(string density, double height, double side, double top)
    {
        var (window, grid) = Show(density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal);
        try
        {
            grid.SelectedIndex = 1;
            grid.CurrentColumn = grid.Columns[0];
            grid.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.True(grid.BeginEdit());
            Pump(window);
            var cell = CurrentCell(grid);
            var box = cell.GetVisualDescendants().OfType<TextBox>().Single();
            var theme = Assert.IsType<ControlTheme>(box.Theme);
            Assert.Same(grid.FindResource(DataGridTheme.CellTextBoxThemeKey), theme);
            Assert.Same(grid.FindResource(typeof(TextBox)), theme.BasedOn);
            Assert.Equal(height, box.Bounds.Height);
            Assert.Equal(cell.Bounds.Width, box.Bounds.Width);
            Assert.Equal(new Thickness(side, top), box.Padding);
            var chrome = box.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == FieldParts.ChromePart);
            Assert.Equal(new Thickness(1), chrome.BorderThickness);
            Assert.Equal(new CornerRadius(8), chrome.CornerRadius);
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, "AccentButtonForeground")), Effective(box.BorderBrush));
            // No ring in edit mode: neither the field's nor the cell's.
            Assert.False(box.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == FocusRing.Part).IsVisible);
            Assert.False(Ring(cell).IsVisible);
            // The text starts where the cell's text did: 1 px border + padding = the cell padding.
            Assert.Equal(side + 1, ((TextBlock)Rows(grid)[0].GetVisualDescendants().OfType<DataGridCell>().First().Content!).Margin.Left);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CheckColumn_UsesTheLibraryCheckBox_DisabledOnlyInADisabledGrid()
    {
        var (window, grid) = Show();
        try
        {
            var unchecked1 = CheckBoxIn(Rows(grid)[1]);
            // The cell check theme, not the plain CheckBox theme, on the column's read-only (disabled) check box.
            Assert.NotNull(unchecked1.Theme);
            Assert.NotSame(window.FindResource(typeof(CheckBox)), unchecked1.Theme);
            Assert.False(unchecked1.IsEnabled);
            var outline = Outline(unchecked1);
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, "CheckBoxCheckBackgroundStrokeUnchecked")), Effective(outline.BorderBrush));
            Assert.Equal(new Size(20, 20), outline.Bounds.Size);

            // On a selected row it inverts: a white outline.
            grid.SelectedIndex = 1;
            Pump(window);
            Assert.Equal(Colors.White, Effective(Outline(CheckBoxIn(Rows(grid)[1])).BorderBrush));

            grid.SelectedIndex = -1;
            grid.IsEnabled = false;
            Pump(window);
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, "CheckBoxCheckBackgroundStrokeUncheckedDisabled")),
                Effective(Outline(CheckBoxIn(Rows(grid)[1])).BorderBrush));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ScrollBars_AreTheOverlayBars_WithNoCornerSquare()
    {
        var (window, grid) = Show();
        try
        {
            var vertical = grid.GetVisualDescendants().OfType<ScrollBar>().Single(b => b.Name == "PART_VerticalScrollbar");
            var horizontal = grid.GetVisualDescendants().OfType<ScrollBar>().Single(b => b.Name == "PART_HorizontalScrollbar");
            Assert.Equal(ScrollBarTheme.Thickness, vertical.Width);
            Assert.Equal(ScrollBarTheme.Thickness, horizontal.Height);
            Assert.False(grid.GetVisualDescendants().OfType<Rectangle>().Single(r => r.Name == "PART_BottomRightCorner").IsVisible);
            Assert.DoesNotContain(grid.GetVisualDescendants(), v => v is Control { Name: "PART_DisabledVisualElement" });
            // The rows run under the vertical bar (it overlays them).
            var rows = grid.GetVisualDescendants().OfType<DataGridRowsPresenter>().Single();
            Assert.Equal(grid.Bounds.Width, rows.Bounds.Width);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Fades_UseTheFixedTransitions_UnlessMotionIsReduced(bool reduceMotion)
    {
        var (window, grid) = Show(reduceMotion: reduceMotion);
        try
        {
            var transitions = RowChrome(Rows(grid)[0]).Transitions!;
            var grip = Grip(Headers(grid)[0], DataGridTheme.GripRightPart).Transitions!;
            if (reduceMotion)
            {
                Assert.Empty(transitions);
                Assert.Empty(grip);
            }
            else
            {
                Assert.Contains(transitions, t => t is CrossFadeBrushTransition { Property.Name: "Background" } c && c.Duration == TimeSpan.FromMilliseconds(100));
                Assert.Contains(grip, t => t is DoubleTransition { Property.Name: "Opacity" } d && d.Duration == TimeSpan.FromMilliseconds(100));
            }
        }
        finally
        {
            window.Close();
        }
    }

    // --- Sheets ----------------------------------------------------------------------------

    private enum GridState { Rest, Hover, KeyboardFocus, EditingText, EditingCombo, ReadOnly, Disabled, AllGridLines }

    private static readonly string[] GridStateLabels =
    [
        "Rest: sorted by Name, Radius selected",
        "Hover: a row and a header edge (resize grip)",
        "Keyboard focus: the current cell",
        "Editing a text cell",
        "Editing a combo cell",
        "Read-only",
        "Disabled",
        "All grid lines (option)",
    ];

    [AvaloniaFact]
    public void DataGrid_RenderSheet()
    {
        using var sheet = new Sheet("DataGrid: flat row bands, horizontal hairlines, Body strong headers in the secondary colour, sort caret, inset cell focus ring");
        var states = Enum.GetValues<GridState>();
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, ["Normal", "Compact density"],
                states.Select((s, i) => (GridStateLabels[i], (IReadOnlyList<Control>)
                [
                    GridCell(sheet, theme, DensityStyle.Normal, s),
                    GridCell(sheet, theme, DensityStyle.Compact, s),
                ])));
        sheet.Save("m3-datagrid.png");
    }

    private static Control GridCell(Sheet sheet, ThemeVariant theme, DensityStyle density, GridState state)
    {
        using var capture = Capture(theme, density, grid =>
        {
            grid.IsReadOnly = state == GridState.ReadOnly;
            grid.IsEnabled = state != GridState.Disabled;
            if (state == GridState.AllGridLines)
                grid.GridLinesVisibility = DataGridGridLinesVisibility.All;
        }, (window, grid) =>
        {
            grid.Columns[0].Sort(ListSortDirection.Ascending);
            Pump(window);
            grid.SelectedIndex = 2;
            Pump(window);
            switch (state)
            {
                case GridState.Hover:
                    ((IPseudoClasses)Rows(grid)[3].Classes).Set(":pointerover", true);
                    var header = Headers(grid)[1];
                    ((IPseudoClasses)header.Classes).Set(":pointerover", true);
                    ((IPseudoClasses)header.Classes).Set(DataGridTheme.ResizeRight, true);
                    break;
                case GridState.KeyboardFocus:
                    grid.CurrentColumn = grid.Columns[1];
                    grid.Focus(NavigationMethod.Tab);
                    break;
                case GridState.EditingText:
                    grid.CurrentColumn = grid.Columns[0];
                    grid.Focus();
                    grid.BeginEdit();
                    break;
                case GridState.EditingCombo:
                    grid.CurrentColumn = grid.Columns[3];
                    grid.Focus();
                    grid.BeginEdit();
                    break;
            }
        });
        return sheet.Image(capture);
    }

    [AvaloniaFact]
    public void DataGrid_Rows_RenderSheet()
    {
        using var sheet = new Sheet("DataGrid rows: a flat full-width band (built) or the list's inset squircle pill");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, ["Band (built)", "Inset pill"],
            [
                ("Selected and hover", [RowsCell(sheet, theme, pill: false, RowsState.Selection), RowsCell(sheet, theme, pill: true, RowsState.Selection)]),
                ("Keyboard focus on a cell", [RowsCell(sheet, theme, pill: false, RowsState.Focus), RowsCell(sheet, theme, pill: true, RowsState.Focus)]),
                ("Scrolled right, Name frozen", [RowsCell(sheet, theme, pill: false, RowsState.Scrolled), RowsCell(sheet, theme, pill: true, RowsState.Scrolled)]),
                ("All grid lines", [RowsCell(sheet, theme, pill: false, RowsState.Lines), RowsCell(sheet, theme, pill: true, RowsState.Lines)]),
            ]);
        sheet.Save("m3-datagrid-rows.png");
    }

    private enum RowsState { Selection, Focus, Scrolled, Lines }

    private static Control RowsCell(Sheet sheet, ThemeVariant theme, bool pill, RowsState state)
    {
        using var capture = Capture(theme, DensityStyle.Normal, grid =>
        {
            if (pill)
                grid.RowTheme = PillRow((ControlTheme)Application.Current!.FindResource(typeof(DataGridRow))!);
            if (state == RowsState.Lines)
                grid.GridLinesVisibility = DataGridGridLinesVisibility.All;
            if (state == RowsState.Scrolled)
            {
                grid.FrozenColumnCount = 1;
                // Notes fixed at 150: scrolled to the end, Category and Notes (270) fill the viewport beside the frozen Name (120), so
                // the hidden Count and In stock (190) end exactly at the frozen edge.
                grid.Columns[^1].Width = new DataGridLength(150);
                grid.Width = 390;
            }
        }, (window, grid) =>
        {
            grid.SelectedIndex = 1;
            Pump(window);
            if (state == RowsState.Selection)
                ((IPseudoClasses)Rows(grid)[3].Classes).Set(":pointerover", true);
            if (state == RowsState.Focus)
            {
                grid.CurrentColumn = grid.Columns[1];
                grid.Focus(NavigationMethod.Tab);
            }
            if (state == RowsState.Scrolled)
            {
                grid.ScrollIntoView(grid.ItemsSource!.Cast<object>().ElementAt(1), grid.Columns[^1]);
                ((IPseudoClasses)Rows(grid)[3].Classes).Set(":pointerover", true);
            }
        });
        return sheet.Image(capture);
    }

    // The list item design for comparison: the band inset 4 from the grid's sides, a squircle of CornerRadiusControl.
    private static ControlTheme PillRow(ControlTheme band) => new(typeof(DataGridRow))
    {
        BasedOn = band,
        Children =
        {
            new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<SquircleBorder>().Name(ButtonParts.ChromePart))
            {
                Setters =
                {
                    new Setter(Layoutable.MarginProperty, new Thickness(4, 0)),
                    new Setter(SquircleBorder.CornerRadiusProperty, new CornerRadius(8)),
                },
            },
        },
    };

    // --- Helpers ---------------------------------------------------------------------------

    public sealed class Item : INotifyPropertyChanged
    {
        private string _name = "";

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public int Count { get; set; }
        public bool InStock { get; set; }
        public string Category { get; set; } = "";
        public string Notes { get; set; } = "";

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    internal static readonly string[] Categories = ["Style", "Blocks", "Modules"];

    private static ObservableCollection<Item> Items() =>
    [
        new() { Name = "Accent", Count = 12, InStock = true, Category = "Style", Notes = "Brand colour" },
        new() { Name = "Swatch", Count = 3, InStock = false, Category = "Blocks", Notes = "Colour chip" },
        new() { Name = "Button", Count = 48, InStock = true, Category = "Blocks", Notes = "Five variants" },
        new() { Name = "Sidebar", Count = 7, InStock = true, Category = "Modules", Notes = "Collapses" },
        new() { Name = "Radius", Count = 2, InStock = false, Category = "Style", Notes = "8 and 16" },
    ];

    internal static DataGrid NewGrid()
    {
        var category = new DataGridTemplateColumn
        {
            Header = "Category",
            Width = new DataGridLength(120),
            SortMemberPath = nameof(Item.Category),
            CellTemplate = new FuncDataTemplate<Item>((_, _) =>
            {
                var text = new TextBlock();
                text.Bind(TextBlock.TextProperty, new Binding(nameof(Item.Category)));
                text.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension(DataGridTheme.CellTextBlockThemeKey));
                return text;
            }),
            CellEditingTemplate = new FuncDataTemplate<Item>((_, _) =>
            {
                var combo = new ComboBox { ItemsSource = Categories };
                combo.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(Item.Category)) { Mode = BindingMode.TwoWay });
                return combo;
            }),
        };
        return new DataGrid
        {
            Width = 520,
            Height = 230,
            ItemsSource = Items(),
            AutoGenerateColumns = false,
            Columns =
            {
                new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(Item.Name)), Width = new DataGridLength(120) },
                new DataGridTextColumn { Header = "Count", Binding = new Binding(nameof(Item.Count)), Width = new DataGridLength(90) },
                new DataGridCheckBoxColumn { Header = "In stock", Binding = new Binding(nameof(Item.InStock)), Width = new DataGridLength(100) },
                category,
                new DataGridTextColumn { Header = "Notes", Binding = new Binding(nameof(Item.Notes)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) },
            },
        };
    }

    private static WriteableBitmap Capture(ThemeVariant theme, DensityStyle density, Action<DataGrid>? configure, Action<Window, DataGrid>? afterShow)
    {
        var grid = NewGrid();
        configure?.Invoke(grid);
        var surface = new Border { Padding = new Thickness(16, 12), Child = ListBoxRenderTests.OnSurface(grid, ListBoxRenderTests.Surface.GlassPanel) };
        surface.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemRegionColor"));
        var window = new Window { SizeToContent = SizeToContent.WidthAndHeight, RequestedThemeVariant = theme };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true, Density = density });
        window.Content = surface;
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        Pump(window);
        afterShow?.Invoke(window, grid);
        Pump(window);
        var capture = window.CaptureRenderedFrame()!;
        window.Close();
        return capture;
    }

    private static (Window Window, DataGrid Grid) Show(DensityStyle density = DensityStyle.Normal, ThemeVariant? variant = null,
        bool reduceMotion = true, bool useAppTheme = false)
    {
        var grid = NewGrid();
        var window = new Window { Width = 700, Height = 400, RequestedThemeVariant = variant ?? ThemeVariant.Light };
        if (!useAppTheme)
            window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion, Density = density });
        window.Content = new StackPanel { Children = { grid } };
        window.Show();
        Pump(window);
        return (window, grid);
    }

    private static SquircleBorder RowChrome(DataGridRow row) =>
        row.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart);

    private static Rectangle HeaderSeparator(DataGridColumnHeader header) =>
        header.GetVisualDescendants().OfType<Rectangle>().Single(r => r.Name == DataGridTheme.SeparatorPart);

    private static PillBorder Grip(DataGridColumnHeader header, string name) =>
        header.GetVisualDescendants().OfType<PillBorder>().Single(p => p.Name == name);

    private static DataGridCell CurrentCell(DataGrid grid) =>
        Rows(grid).SelectMany(r => r.GetVisualDescendants().OfType<DataGridCell>()).Single(c => c.IsVisible && c.Classes.Contains(":current"));

    private static SquircleBorder Ring(DataGridCell cell) =>
        cell.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == DataGridTheme.RingPart && b.TemplatedParent == cell);

    private static CheckBox CheckBoxIn(DataGridRow row) => row.GetVisualDescendants().OfType<CheckBox>().Single();

    private static SquircleBorder Outline(CheckBox box) =>
        box.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == ToggleParts.OutlinePart);

    internal static List<DataGridRow> Rows(DataGrid grid) =>
        grid.GetVisualDescendants().OfType<DataGridRow>().Where(r => r.IsVisible).OrderBy(r => r.Index).ToList();

    internal static List<DataGridColumnHeader> Headers(DataGrid grid) =>
        grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Where(h => h.Name != "PART_TopLeftCornerHeader" && h.IsVisible)
            .OrderBy(h => h.TranslatePoint(default, grid)!.Value.X).ToList();

    internal static void Pump(TopLevel top)
    {
        Dispatcher.UIThread.RunJobs();
        top.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        top.UpdateLayout();
    }
}
