using Avalonia.Controls;

namespace Catalog.Pages;

/// <summary>NumericUpDown and ButtonSpinner: every kind in every state, and live fields.</summary>
internal sealed class NumericUpDownPage : UserControl
{
    public NumericUpDownPage() => Content = Ui.Page("NumericUpDown",
        Ui.Section("Kinds and states", FieldGrid.Build(
        [
            ("NumericUpDown", () => new NumericUpDown { Width = 200, Value = 42 }),
            ("Placeholder", () => new NumericUpDown { Width = 200, PlaceholderText = "Quantity" }),
            ("Buttons left", () => new NumericUpDown { Width = 200, Value = 42, ButtonSpinnerLocation = Location.Left }),
            ("At its maximum", () => new NumericUpDown { Width = 200, Value = 10, Maximum = 10 }),
            ("ButtonSpinner", () => new ButtonSpinner { Width = 200, Content = "Medium" }),
        ])),
        Ui.Caption("The spin buttons are field buttons with caret-up and caret-down icons; the Up and Down keys also spin."));
}
