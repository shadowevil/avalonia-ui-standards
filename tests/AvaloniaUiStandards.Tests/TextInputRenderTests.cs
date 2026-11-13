using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the TextInput sheets (2x, labelled): states × kinds, light and dark, Normal and Compact, and Fluent before/after.</summary>
public class TextInputRenderTests
{
    private static readonly FieldState[] States =
        [FieldState.Rest, FieldState.Hover, FieldState.Focused, FieldState.KeyboardFocused, FieldState.Error, FieldState.Disabled];

    internal static readonly (string Label, Func<TextBox> Create)[] Kinds =
    [
        ("Placeholder", () => new TextBox { Width = 200, PlaceholderText = "Search files" }),
        ("Text", () => new TextBox { Width = 200, Text = "Quarterly report" }),
        ("Clear button", () => Classed(new TextBox { Width = 200, Text = "Quarterly report" }, "clearButton")),
        ("Password", () => Classed(new TextBox { Width = 200, Text = "hunter22", PasswordChar = '•' }, "revealPasswordButton")),
        ("Password, revealed", () => Classed(new TextBox { Width = 200, Text = "hunter22", PasswordChar = '•', RevealPassword = true }, "revealPasswordButton")),
        ("Selected text", () => Selected(new TextBox { Width = 200, Text = "Quarterly report" })),
        ("Multi-line", () => new TextBox
        {
            Width = 200, Height = 76, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
            Text = "Multi-line text wraps in the same chrome, from the top padding.",
        }),
    ];

    [AvaloniaFact]
    public void States_RenderSheet()
    {
        using var sheet = new Sheet("TextInput: kinds × states (TextControl colours, 1 px border, accent on focus, ring on keyboard focus, danger on error)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            foreach (var (densityLabel, density) in FieldCells.Densities)
                sheet.AddSection($"{themeLabel}, {densityLabel}", States.Select(FieldCells.Label).ToList(),
                    Kinds.Select(k => (k.Label, (IReadOnlyList<Control>)States
                        .Select(s => FieldCells.Render(sheet, theme, density, k.Create(), s)).ToList())));
        sheet.Save("m3-textinput.png");
    }

    [AvaloniaFact]
    public void BeforeAfter_RendersFluentNextToLibrary()
    {
        FieldState[] states = [FieldState.Rest, FieldState.Hover, FieldState.Focused, FieldState.Disabled];
        using var sheet = new Sheet("TextInput: Fluent default (before) and the library (after)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, states.Select(FieldCells.Label).ToList(),
            [
                ("Fluent, placeholder", states.Select(s => FieldCells.RenderFluent(sheet, theme, Kinds[0].Create(), s)).ToList()),
                ("Library, placeholder", states.Select(s => FieldCells.Render(sheet, theme, Avalonia.Themes.Fluent.DensityStyle.Normal, Kinds[0].Create(), s)).ToList()),
                ("Fluent, text", states.Select(s => FieldCells.RenderFluent(sheet, theme, Kinds[1].Create(), s)).ToList()),
                ("Library, text", states.Select(s => FieldCells.Render(sheet, theme, Avalonia.Themes.Fluent.DensityStyle.Normal, Kinds[1].Create(), s)).ToList()),
            ]);
        sheet.Save("m3-textinput-before-after.png");
    }

    private static TextBox Classed(TextBox box, string cls)
    {
        box.Classes.Add(cls);
        return box;
    }

    // Selected once focused: a TextBox shows its selection while it has focus.
    private static TextBox Selected(TextBox box)
    {
        box.GotFocus += (_, _) => box.SelectAll();
        return box;
    }
}
