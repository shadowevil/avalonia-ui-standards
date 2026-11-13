using Avalonia.Controls;
using Catalog.Pages;

namespace Catalog;

/// <summary>A page in the sidebar: its title and how to create it.</summary>
internal sealed record PageInfo(string Title, Func<Control> Create);

/// <summary>The sidebar pages, in order. Adding a page is one line here and one class in Pages/.</summary>
internal static class CatalogPages
{
    public static readonly IReadOnlyList<PageInfo> All =
    [
        new("Colours", () => new ColoursPage()),
        new("Type", () => new TypePage()),
        new("Spacing", () => new SpacingPage()),
        new("Shape", () => new ShapePage()),
        new("Glass", () => new GlassPage()),
        new("Motion", () => new MotionPage()),
        new("Button", () => new ButtonPage()),
        new("Button family", () => new ButtonFamilyPage()),
        new("Icon", () => new IconPage()),
        new("TextInput", () => new TextInputPage()),
        new("ComboBox", () => new ComboBoxPage()),
        new("NumericUpDown", () => new NumericUpDownPage()),
    ];
}
