using Avalonia.Controls;

namespace Catalog.Pages;

/// <summary>ComboBox: the closed field in every kind and state, live ComboBoxes, and fields lined up in a row.</summary>
internal sealed class ComboBoxPage : UserControl
{
    private static readonly string[] Folders = ["Documents", "Downloads", "Pictures", "Music"];

    public ComboBoxPage() => Content = Ui.Page("ComboBox",
        Ui.Section("Kinds and states", FieldGrid.Build(
        [
            ("Placeholder", () => new ComboBox { Width = 200, ItemsSource = Folders, PlaceholderText = "Choose a folder" }),
            ("Selected", () => new ComboBox { Width = 200, ItemsSource = Folders, SelectedIndex = 1 }),
            ("Editable", () => new ComboBox { Width = 200, ItemsSource = Folders, IsEditable = true, Text = "Pictures" }),
            ("Editable, placeholder", () => new ComboBox { Width = 200, ItemsSource = Folders, IsEditable = true, PlaceholderText = "Type or choose" }),
        ])),
        Ui.Section("In a row", new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new WrapPanel
                {
                    ItemSpacing = 8,
                    LineSpacing = 8,
                    Children =
                    {
                        new TextBox { Width = 160, PlaceholderText = "Name" },
                        new ComboBox { Width = 160, ItemsSource = Folders, SelectedIndex = 0 },
                        new Button { Content = "Save" },
                    },
                },
                Ui.Caption("A ComboBox has the TextInput chrome, so fields and buttons line up."),
            },
        }));
}
