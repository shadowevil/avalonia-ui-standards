using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Catalog.Pages;

/// <summary>Button's siblings: ToggleButton, RepeatButton, SplitButton, ToggleSplitButton, DropDownButton and HyperlinkButton.</summary>
internal sealed class ButtonFamilyPage : UserControl
{
    public ButtonFamilyPage() => Content = Ui.Page("Button family",
        Ui.Section("ToggleButton", StateGrid.Build(
        [
            ("Unchecked", () => new ToggleButton { Content = "Toggle" }),
            ("Checked", () => new ToggleButton { Content = "Toggle", IsChecked = true }),
            ("Indeterminate", () => new ToggleButton { Content = "Toggle", IsThreeState = true, IsChecked = null }),
        ])),
        Ui.Section("RepeatButton", StateGrid.Build([("RepeatButton", () => new RepeatButton { Content = "Repeat" })])),
        Ui.Section("SplitButton", StateGrid.Build(
        [
            ("SplitButton", () => new SplitButton { Content = "Split" }),
            ("ToggleSplitButton, checked", () => new ToggleSplitButton { Content = "Split", IsChecked = true }),
        ])),
        Ui.Section("DropDownButton", StateGrid.Build([("DropDownButton", () => new DropDownButton { Content = "Drop down" })])),
        Ui.Section("HyperlinkButton", StateGrid.Build([("HyperlinkButton", () => new HyperlinkButton { Content = "Link" })])));
}
