using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the NumericUpDown and ButtonSpinner sheets (2x, labelled): kinds × states, light and dark, Normal and Compact, and Fluent before/after.</summary>
public class NumericRenderTests
{
    private static readonly FieldState[] States =
        [FieldState.Rest, FieldState.Hover, FieldState.Focused, FieldState.KeyboardFocused, FieldState.Error, FieldState.Disabled];

    internal static readonly (string Label, Func<Control> Create)[] Kinds =
    [
        ("NumericUpDown", () => new NumericUpDown { Width = 200, Value = 42 }),
        ("NumericUpDown, placeholder", () => new NumericUpDown { Width = 200, PlaceholderText = "Quantity" }),
        ("NumericUpDown, buttons left", () => new NumericUpDown { Width = 200, Value = 42, ButtonSpinnerLocation = Location.Left }),
        ("NumericUpDown, at maximum", () => new NumericUpDown { Width = 200, Value = 10, Maximum = 10 }),
        ("ButtonSpinner", () => new ButtonSpinner { Width = 200, Content = "Medium" }),
    ];

    // Focus goes to a NumericUpDown's inner TextBox, as when the user tabs or clicks into it.
    internal static Avalonia.Input.InputElement FocusTarget(Control control) =>
        control is NumericUpDown
            ? control.GetVisualDescendants().OfType<TextBox>().First(t => t.Name == SpinnerThemes.TextBoxPart)
            : control;

    [AvaloniaFact]
    public void States_RenderSheet()
    {
        using var sheet = new Sheet("NumericUpDown and ButtonSpinner: kinds × states (TextInput chrome, caret-up and caret-down field buttons)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            foreach (var (densityLabel, density) in FieldCells.Densities)
                sheet.AddSection($"{themeLabel}, {densityLabel}", States.Select(FieldCells.Label).ToList(),
                    Kinds.Select(k => (k.Label, (IReadOnlyList<Control>)States
                        .Select(s => FieldCells.Render(sheet, theme, density, k.Create(), s, FocusTarget)).ToList())));
        sheet.Save("m3-numericupdown.png");
    }

    [AvaloniaFact]
    public void BeforeAfter_RendersFluentNextToLibrary()
    {
        FieldState[] states = [FieldState.Rest, FieldState.Hover, FieldState.KeyboardFocused, FieldState.Disabled];
        using var sheet = new Sheet("NumericUpDown: Fluent default (before) and the library (after)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, states.Select(FieldCells.Label).ToList(),
            [
                ("Fluent", states.Select(s => FieldCells.RenderFluent(sheet, theme, Kinds[0].Create(), s, FocusTarget)).ToList()),
                ("Library", states.Select(s => FieldCells.Render(sheet, theme, DensityStyle.Normal, Kinds[0].Create(), s, FocusTarget)).ToList()),
            ]);
        sheet.Save("m3-numericupdown-before-after.png");
    }
}
