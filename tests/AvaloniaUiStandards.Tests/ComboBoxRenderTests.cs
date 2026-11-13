using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the ComboBox field sheets (2x, labelled): kinds × states, light and dark, Normal and Compact, Fluent before/after, and fields in a row.</summary>
public class ComboBoxRenderTests
{
    private static readonly FieldState[] States =
        [FieldState.Rest, FieldState.Hover, FieldState.Pressed, FieldState.Focused, FieldState.KeyboardFocused, FieldState.Error, FieldState.Disabled];

    internal static readonly string[] Items = ["Documents", "Downloads", "Pictures", "Music"];

    internal static readonly (string Label, Func<ComboBox> Create)[] Kinds =
    [
        ("Placeholder", () => new ComboBox { Width = 200, ItemsSource = Items, PlaceholderText = "Choose a folder" }),
        ("Selected", () => new ComboBox { Width = 200, ItemsSource = Items, SelectedIndex = 1 }),
        ("Editable", () => new ComboBox { Width = 200, ItemsSource = Items, IsEditable = true, Text = "Pictures" }),
        ("Editable, placeholder", () => new ComboBox { Width = 200, ItemsSource = Items, IsEditable = true, PlaceholderText = "Type or choose" }),
    ];

    // Focus goes to the inner TextBox of an editable ComboBox, as it does when the user tabs or clicks into it.
    internal static InputElement FocusTarget(Control control) =>
        control is ComboBox { IsEditable: true } box
            ? box.GetVisualDescendants().OfType<TextBox>().First(t => t.Name == ComboBoxTheme.EditableTextBoxPart)
            : control;

    [AvaloniaFact]
    public void States_RenderSheet()
    {
        using var sheet = new Sheet("ComboBox: kinds × states (TextInput chrome, ComboBox colours, caret zone; editable focus is a text field's)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            foreach (var (densityLabel, density) in FieldCells.Densities)
                sheet.AddSection($"{themeLabel}, {densityLabel}", States.Select(FieldCells.Label).ToList(),
                    Kinds.Select(k => (k.Label, (IReadOnlyList<Control>)States
                        .Select(s => FieldCells.Render(sheet, theme, density, k.Create(), s, FocusTarget)).ToList())));
        sheet.Save("m3-combobox.png");
    }

    [AvaloniaFact]
    public void BeforeAfter_RendersFluentNextToLibrary()
    {
        FieldState[] states = [FieldState.Rest, FieldState.Hover, FieldState.KeyboardFocused, FieldState.Disabled];
        using var sheet = new Sheet("ComboBox field: Fluent default (before) and the library (after)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, states.Select(FieldCells.Label).ToList(),
            [
                ("Fluent, selected", states.Select(s => FieldCells.RenderFluent(sheet, theme, Kinds[1].Create(), s)).ToList()),
                ("Library, selected", states.Select(s => FieldCells.Render(sheet, theme, DensityStyle.Normal, Kinds[1].Create(), s)).ToList()),
                ("Fluent, editable", states.Select(s => FieldCells.RenderFluent(sheet, theme, Kinds[2].Create(), s, FocusTarget)).ToList()),
                ("Library, editable", states.Select(s => FieldCells.Render(sheet, theme, DensityStyle.Normal, Kinds[2].Create(), s, FocusTarget)).ToList()),
            ]);
        sheet.Save("m3-combobox-before-after.png");
    }

    [AvaloniaFact]
    public void Row_LinesUpFieldsAndButtons()
    {
        using var sheet = new Sheet("Fields in a row: TextInput, ComboBox and Button share height, radius and side padding");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, FieldCells.Densities.Select(d => d.Label).ToList(),
            [
                ("Row", FieldCells.Densities.Select(d => FieldCells.Render(sheet, theme, d.Density, Row(), FieldState.Rest)).ToList()),
            ]);
        sheet.Save("m3-fields-row.png");
    }

    internal static Control Row() => new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Spacing = 8,
        Children =
        {
            new TextBox { Width = 160, PlaceholderText = "Name" },
            new ComboBox { Width = 140, ItemsSource = Items, SelectedIndex = 0 },
            new Button { Content = "Save" },
        },
    };
}
