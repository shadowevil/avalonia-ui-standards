using Avalonia.Controls;

namespace Catalog.Pages;

/// <summary>The toggles: CheckBox, RadioButton and ToggleSwitch, in every check state.</summary>
internal sealed class TogglesPage : UserControl
{
    public TogglesPage() => Content = Ui.Page("Toggles",
        Ui.Section("CheckBox", StateGrid.Build(
        [
            ("Unchecked", () => new CheckBox { Content = "Label" }),
            ("Checked", () => new CheckBox { Content = "Label", IsChecked = true }),
            ("Indeterminate", () => new CheckBox { Content = "Label", IsThreeState = true, IsChecked = null }),
        ])),
        Ui.Section("RadioButton", StateGrid.Build(
        [
            ("Unchecked", () => new RadioButton { Content = "Label" }),
            ("Checked", () => new RadioButton { Content = "Label", IsChecked = true }),
        ])),
        Ui.Section("ToggleSwitch", StateGrid.Build(
        [
            ("Off", () => new ToggleSwitch()),
            ("On", () => new ToggleSwitch { IsChecked = true }),
            ("With header", () => new ToggleSwitch { Content = "Header" }),
        ])));
}
