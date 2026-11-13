using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the Button sheets (2x, labelled) to the test output (screenshots/).</summary>
public class ButtonRenderTests
{
    internal static readonly (string Label, ThemeVariant Variant)[] Themes = [("Light", ThemeVariant.Light), ("Dark", ThemeVariant.Dark)];

    private static readonly (string Label, string? Class)[] Variants =
        [("Standard", null), ("Accent", "accent"), ("Subtle", "subtle"), ("Destructive", "destructive")];

    [AvaloniaFact]
    public void States_RendersSheet()
    {
        using var sheet = new Sheet("Button: variants × states (AA fills from accent #007CEA and danger #E1332A)");
        foreach (var (themeLabel, theme) in Themes)
            sheet.AddSection(themeLabel, StateCells.All.Select(s => s.ToString()).ToList(),
                Variants.Select(v => (v.Label, (IReadOnlyList<Control>)StateCells.All
                    .Select(s => StateCells.Render(sheet, theme, NewButton(v.Class), s)).ToList())));
        sheet.Save("m3-button-states.png");
    }

    [AvaloniaFact]
    public void BeforeAfter_RendersFluentNextToLibrary()
    {
        (string Label, string? Class)[] variants = [("Standard", null), ("Accent", "accent")];

        using var sheet = new Sheet("Button: Fluent default (before) and the library (after)");
        foreach (var (themeLabel, theme) in Themes)
        {
            var rows = new List<(string, IReadOnlyList<Control>)>();
            foreach (var (label, cls) in variants)
            {
                rows.Add(($"Fluent {label}", StateCells.All.Select(s => StateCells.Render(sheet, theme, NewButton(cls), s, w => w.Styles.Add(new FluentTheme()))).ToList()));
                rows.Add(($"Library {label}", StateCells.All.Select(s => StateCells.Render(sheet, theme, NewButton(cls), s)).ToList()));
            }
            sheet.AddSection(themeLabel, StateCells.All.Select(s => s.ToString()).ToList(), rows);
        }
        sheet.Save("m3-button-before-after.png");
    }

    private static Button NewButton(string? cls)
    {
        var button = new Button { Content = "Button" };
        if (cls is not null)
            button.Classes.Add(cls);
        return button;
    }
}
