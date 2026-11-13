using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the CheckBox, RadioButton and ToggleSwitch sheets (states × light/dark × density, 2x) to screenshots/.</summary>
public class ToggleRenderTests
{
    /// <summary>The densities of a sheet: the app theme (Normal), and a Compact theme on the cell's window.</summary>
    internal static readonly (string Label, Action<Window>? BeforeShow)[] Densities =
    [
        ("Normal", null),
        ("Compact", w => w.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = DensityStyle.Compact })),
    ];

    [AvaloniaFact]
    public void CheckBox_RendersSheet() => Render("CheckBox: states × light/dark × density", "m3-checkbox.png",
    [
        ("Unchecked", () => new CheckBox { Content = "Label" }),
        ("Checked", () => new CheckBox { Content = "Label", IsChecked = true }),
        ("Indeterminate", () => new CheckBox { Content = "Label", IsThreeState = true, IsChecked = null }),
        ("Checked, no label", () => new CheckBox { IsChecked = true }),
    ]);

    [AvaloniaFact]
    public void RadioButton_RendersSheet() => Render("RadioButton: states × light/dark × density", "m3-radiobutton.png",
    [
        ("Unchecked", () => new RadioButton { Content = "Label" }),
        ("Checked", () => new RadioButton { Content = "Label", IsChecked = true }),
    ]);

    [AvaloniaFact]
    public void ToggleSwitch_RendersSheet() => Render("ToggleSwitch: states × light/dark × density", "m3-toggleswitch.png",
    [
        ("Off", () => new ToggleSwitch()),
        ("On", () => new ToggleSwitch { IsChecked = true }),
        ("Off, header", () => new ToggleSwitch { Content = "Header" }),
        ("On, no text", () => new ToggleSwitch { IsChecked = true, OnContent = null, OffContent = null }),
    ]);

    [AvaloniaFact]
    public void BeforeAfter_RendersFluentNextToLibrary()
    {
        (string Label, Func<Control> Create)[] controls =
        [
            ("CheckBox", () => new CheckBox { Content = "Label", IsChecked = true }),
            ("RadioButton", () => new RadioButton { Content = "Label", IsChecked = true }),
            ("ToggleSwitch", () => new ToggleSwitch { IsChecked = true }),
        ];
        using var sheet = new Sheet("Toggles: Fluent default (before) and the library (after)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            var rows = new List<(string, IReadOnlyList<Control>)>();
            foreach (var (label, create) in controls)
            {
                rows.Add(($"Fluent {label}", StateCells.All.Select(s => StateCells.Render(sheet, theme, create(), s, w => w.Styles.Add(new FluentTheme()))).ToList()));
                rows.Add(($"Library {label}", StateCells.All.Select(s => StateCells.Render(sheet, theme, create(), s)).ToList()));
            }
            sheet.AddSection(themeLabel, StateCells.All.Select(s => s.ToString()).ToList(), rows);
        }
        sheet.Save("m3-toggles-before-after.png");
    }

    internal static void Render(string title, string file, (string Label, Func<Control> Create)[] variants)
    {
        using var sheet = new Sheet(title);
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, StateCells.All.Select(s => s.ToString()).ToList(),
                Densities.SelectMany(d => variants.Select(v => ($"{v.Label}, {d.Label}", (IReadOnlyList<Control>)StateCells.All
                    .Select(s => StateCells.Render(sheet, theme, v.Create(), s, d.BeforeShow)).ToList()))));
        sheet.Save(file);
    }
}
