using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Catalog.Pages;

/// <summary>DataGrid: a live grid with text, number, check and combo columns, sorting, a frozen column and editing, and its states.</summary>
internal sealed class DataGridPage : UserControl
{
    public DataGridPage() => Content = Ui.Page("DataGrid",
        Ui.Section("Live", new StackPanel
        {
            Spacing = 12,
            Children =
            {
                Grid(Sample(), height: 300, configure: g =>
                {
                    g.FrozenColumnCount = 1;
                    g.CanUserResizeColumns = true;
                    g.CanUserReorderColumns = true;
                }),
                Ui.Caption("Click a header to sort; drag a header edge to resize (the grip shows near a resizable edge). Name stays frozen while the grid scrolls sideways. " +
                    "Double click or press F2 to edit a cell: text becomes a field, Category a combo box, In stock toggles. Tab in and use the arrow keys to see the focus ring on the current cell."),
            },
        }),
        Ui.Section("States", new WrapPanel
        {
            Children =
            {
                State("Read-only", g => g.IsReadOnly = true),
                State("Disabled", g => { g.IsEnabled = false; g.SelectedIndex = 1; }),
                State("All grid lines", g => g.GridLinesVisibility = DataGridGridLinesVisibility.All),
            },
        }),
        Ui.Caption("Rows are the list item height (32, or 24 at Compact) with the list's hover and accent selection; horizontal hairlines in the border colour. " +
            "Vertical lines and row headers are options."));

    private static Control State(string label, Action<DataGrid> configure) => Ui.Spaced(new StackPanel
    {
        Spacing = 4,
        Width = 320,
        Children = { Ui.Body(label), Grid([.. Sample().Take(4)], height: 176, configure, compact: true) },
    });

    private static DataGrid Grid(ObservableCollection<Part> items, double height, Action<DataGrid> configure, bool compact = false)
    {
        var grid = new DataGrid
        {
            Height = height,
            ItemsSource = items,
            AutoGenerateColumns = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(Part.Name)), Width = new DataGridLength(120) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Count", Binding = new Binding(nameof(Part.Count)), Width = new DataGridLength(90) });
        grid.Columns.Add(new DataGridCheckBoxColumn { Header = "In stock", Binding = new Binding(nameof(Part.InStock)), Width = new DataGridLength(100) });
        if (!compact)
        {
            grid.Columns.Add(CategoryColumn());
            grid.Columns.Add(new DataGridTextColumn { Header = "Notes", Binding = new Binding(nameof(Part.Notes)), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 160 });
        }
        configure(grid);
        return grid;
    }

    private static readonly string[] Categories = ["Style", "Blocks", "Modules", "Screens"];

    // A combo column: a template column that shows the text and edits with a ComboBox.
    private static DataGridTemplateColumn CategoryColumn() => new()
    {
        Header = "Category",
        Width = new DataGridLength(130),
        SortMemberPath = nameof(Part.Category),
        CellTemplate = new FuncDataTemplate<Part>((_, _) =>
        {
            var text = new TextBlock();
            text.Bind(TextBlock.TextProperty, new Binding(nameof(Part.Category)));
            text.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("DataGridCellTextBlockTheme"));
            return text;
        }),
        CellEditingTemplate = new FuncDataTemplate<Part>((_, _) =>
        {
            var combo = new ComboBox { ItemsSource = Categories };
            combo.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(Part.Category)) { Mode = BindingMode.TwoWay });
            return combo;
        }),
    };

    private static ObservableCollection<Part> Sample() =>
    [
        new("Accent", 12, true, "Style", "The brand colour, AA fills derived"),
        new("Swatch", 3, false, "Blocks", "A colour chip"),
        new("Button", 48, true, "Blocks", "Five variants, two densities"),
        new("Sidebar", 7, true, "Modules", "Collapses to an icon rail"),
        new("Radius", 2, false, "Style", "8 for controls, 16 for overlays"),
        new("App shell", 1, false, "Screens", "Frame, title band and panel"),
        new("TextInput", 21, true, "Blocks", "Clear and reveal buttons"),
        new("Glass", 5, true, "Style", "Blur, tint and refraction"),
        new("ComboBox", 9, true, "Blocks", "Editable or not"),
        new("Settings page", 0, false, "Screens", "Planned"),
        new("Spacing", 7, true, "Style", "A 4 px grid"),
        new("TreeView", 4, true, "Blocks", "Node lines and click modes"),
    ];

    /// <summary>A sample row; settable so the grid can edit it.</summary>
    internal sealed class Part(string name, int count, bool inStock, string category, string notes)
    {
        public string Name { get; set; } = name;
        public int Count { get; set; } = count;
        public bool InStock { get; set; } = inStock;
        public string Category { get; set; } = category;
        public string Notes { get; set; } = notes;
    }
}
