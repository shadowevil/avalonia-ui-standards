using Avalonia.Controls;

namespace Catalog.Pages;

/// <summary>Button: every variant in every state, and live buttons.</summary>
internal sealed class ButtonPage : UserControl
{
    public ButtonPage() => Content = Ui.Page("Button", Ui.Section("Variants and states", StateGrid.Build(
    [
        ("Standard", () => New(null)),
        ("Accent", () => New("accent")),
        ("Subtle", () => New("subtle")),
        ("Destructive", () => New("destructive")),
    ])));

    private static Button New(string? variantClass)
    {
        var button = new Button { Content = "Button" };
        if (variantClass is not null)
            button.Classes.Add(variantClass);
        return button;
    }
}
