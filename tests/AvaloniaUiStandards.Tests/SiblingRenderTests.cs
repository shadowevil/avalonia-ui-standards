using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Themes.Fluent;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the sheets of Button's siblings (2x, labelled) to the test output (screenshots/).</summary>
public class SiblingRenderTests
{
    private static readonly (string Label, Func<Control> Create)[] Siblings =
    [
        ("RepeatButton", () => new RepeatButton { Content = "Repeat" }),
        ("ToggleButton", () => new ToggleButton { Content = "Toggle" }),
        ("ToggleButton, checked", () => new ToggleButton { Content = "Toggle", IsChecked = true }),
        ("ToggleButton, indeterminate", () => new ToggleButton { Content = "Toggle", IsThreeState = true, IsChecked = null }),
        ("HyperlinkButton", () => new HyperlinkButton { Content = "Link" }),
        ("DropDownButton", () => new DropDownButton { Content = "Drop down" }),
        ("SplitButton", () => new SplitButton { Content = "Split" }),
        ("ToggleSplitButton, checked", () => new ToggleSplitButton { Content = "Split", IsChecked = true }),
    ];

    [AvaloniaFact]
    public void States_RendersSheet()
    {
        using var sheet = new Sheet("Button's siblings × states");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, StateCells.All.Select(s => s.ToString()).ToList(),
                Siblings.Select(x => (x.Label, (IReadOnlyList<Control>)StateCells.All
                    .Select(s => StateCells.Render(sheet, theme, x.Create(), s)).ToList())));
        sheet.Save("m3-siblings-states.png");
    }

    [AvaloniaFact]
    public void BeforeAfter_RendersFluentNextToLibrary()
    {
        using var sheet = new Sheet("Button's siblings: Fluent default (before) and the library (after)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            var rows = new List<(string, IReadOnlyList<Control>)>();
            foreach (var (label, create) in Siblings)
            {
                rows.Add(($"Fluent {label}", StateCells.All.Select(s => StateCells.Render(sheet, theme, create(), s, w => w.Styles.Add(new FluentTheme()))).ToList()));
                rows.Add(($"Library {label}", StateCells.All.Select(s => StateCells.Render(sheet, theme, create(), s)).ToList()));
            }
            sheet.AddSection(themeLabel, StateCells.All.Select(s => s.ToString()).ToList(), rows);
        }
        sheet.Save("m3-siblings-before-after.png");
    }
}
