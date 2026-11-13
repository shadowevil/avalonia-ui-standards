using Avalonia.Controls;
using Avalonia.Media;

namespace Catalog.Pages;

/// <summary>TextInput: every kind in every state, live fields, and a field lined up with buttons.</summary>
internal sealed class TextInputPage : UserControl
{
    public TextInputPage() => Content = Ui.Page("TextInput",
        Ui.Section("Kinds and states", FieldGrid.Build(
        [
            ("Placeholder", () => new TextBox { Width = 200, PlaceholderText = "Search files" }),
            ("Text", () => new TextBox { Width = 200, Text = "Quarterly report" }),
            ("Clear button", () => Classed(new TextBox { Width = 200, Text = "Quarterly report" }, "clearButton")),
            ("Password", () => Classed(new TextBox { Width = 200, Text = "hunter22", PasswordChar = '•' }, "revealPasswordButton")),
            ("Multi-line", () => new TextBox
            {
                Width = 200, Height = 76, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                Text = "Multi-line text wraps in the same chrome, from the top padding.",
            }),
        ])),
        Ui.Section("Lined up with buttons", new StackPanel
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
                        Classed(new TextBox { Width = 240, PlaceholderText = "Name the file" }, "clearButton"),
                        new Button { Content = "Cancel" },
                        Classed(new Button { Content = "Save" }, "accent"),
                    },
                },
                Ui.Caption("Fields and buttons share the height (32 Normal, 24 Compact), the corner radius and the side padding."),
            },
        }));

    private static T Classed<T>(T control, string cls) where T : Control
    {
        control.Classes.Add(cls);
        return control;
    }
}
