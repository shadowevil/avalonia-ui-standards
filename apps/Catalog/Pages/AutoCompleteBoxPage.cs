using Avalonia.Controls;

namespace Catalog.Pages;

/// <summary>AutoCompleteBox: the field in every state, and a live field whose suggestions open in the ComboBox drop-down.</summary>
internal sealed class AutoCompleteBoxPage : UserControl
{
    private static readonly string[] Cities = ["Amsterdam", "Athens", "Auckland", "Austin", "Berlin", "Boston", "Brussels", "Budapest"];

    public AutoCompleteBoxPage() => Content = Ui.Page("AutoCompleteBox",
        Ui.Section("States", FieldGrid.Build(
        [
            ("Placeholder", () => new AutoCompleteBox { Width = 220, ItemsSource = Cities, PlaceholderText = "City" }),
            ("Text", () => new AutoCompleteBox { Width = 220, ItemsSource = Cities, Text = "Austin" }),
        ])),
        Ui.Caption("Type in a live field to open its suggestions; they use the ComboBox drop-down and list items."));
}
