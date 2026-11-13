using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Catalog.Pages;

/// <summary>Expander: closed and open in every state (on the header), and live expanders.</summary>
internal sealed class ExpanderPage : UserControl
{
    public ExpanderPage() => Content = Ui.Page("Expander",
        Ui.Section("States", StateGrid.Build(
        [
            ("Closed", () => New(expanded: false)),
            ("Open", () => New(expanded: true)),
        ], Header)),
        Ui.Section("Live", new StackPanel
        {
            Spacing = 8,
            Children = { New(expanded: false, width: double.NaN), New(expanded: true, width: double.NaN) },
        }));

    private static Control Header(Control expander) =>
        expander.GetVisualDescendants().OfType<ToggleButton>().FirstOrDefault(t => t.Name == "ExpanderHeader") ?? expander;

    private static Expander New(bool expanded, double width = 240) => new()
    {
        Width = width,
        Header = "Advanced options",
        IsExpanded = expanded,
        Content = new StackPanel
        {
            Spacing = 8,
            Children = { Ui.Body("Keep a history of changes"), Ui.Caption("The content reveals below the header.") },
        },
    };
}
