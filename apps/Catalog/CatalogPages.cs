using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaUiStandards.Blocks;
using Catalog.Pages;

namespace Catalog;

/// <summary>A page in the sidebar: its title, its sidebar icon (from <see cref="Icons"/>) and how to create it.</summary>
internal sealed record PageInfo(string Title, Geometry Icon, Func<Control> Create);

/// <summary>The sidebar pages, in order. Adding a page is one line here and one class in Pages/.</summary>
internal static class CatalogPages
{
    public static readonly IReadOnlyList<PageInfo> All =
    [
        new("Colours", Icons.Palette, () => new ColoursPage()),
        new("Type", Icons.TextAa, () => new TypePage()),
        new("Spacing", Icons.Ruler, () => new SpacingPage()),
        new("Shape", Icons.SquaresFour, () => new ShapePage()),
        new("Glass", Icons.Drop, () => new GlassPage()),
        new("Motion", Icons.Play, () => new MotionPage()),
        new("Button", Icons.CursorClick, () => new ButtonPage()),
        new("Button family", Icons.CheckSquare, () => new ButtonFamilyPage()),
        new("Icon", Icons.Bell, () => new IconPage()),
        new("TextInput", Icons.ChatText, () => new TextInputPage()),
        new("ComboBox", Icons.ListBullets, () => new ComboBoxPage()),
        new("Toggles", Icons.ToggleRight, () => new TogglesPage()),
        new("Segmented", Icons.RadioButton, () => new SegmentedControlPage()),
        new("TreeView", Icons.TreeStructure, () => new TreeViewPage()),
        new("Ranges", Icons.SlidersHorizontal, () => new RangesPage()),
        new("NumericUpDown", Icons.Plus, () => new NumericUpDownPage()),
        new("AutoCompleteBox", Icons.MagnifyingGlass, () => new AutoCompleteBoxPage()),
        new("Tabs", Icons.Tabs, () => new TabsPage()),
        new("Expander", Icons.CaretDown, () => new ExpanderPage()),
        new("Pickers", Icons.CalendarBlank, () => new PickersPage()),
    ];
}
