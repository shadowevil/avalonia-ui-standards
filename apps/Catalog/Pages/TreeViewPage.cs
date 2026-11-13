using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Catalog.Pages;

/// <summary>TreeView: the row states forced, and a live tree.</summary>
internal sealed class TreeViewPage : UserControl
{
    private static readonly (string Label, Action<TreeViewItem>? Apply)[] States =
    [
        ("Rest", null),
        ("Hover", item => Row(item, ":pointerover")),
        ("Pressed", item => Row(item, ":pressed")),
        ("Selected", item => item.IsSelected = true),
        ("Selected + hover", item => { item.IsSelected = true; Row(item, ":pointerover"); }),
        ("Disabled", item => item.IsEnabled = false),
    ];

    public TreeViewPage() => Content = Ui.Page("TreeView",
        Ui.Section("States", StatesPanel()),
        Ui.Section("Live", new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TreeView { Width = 280, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, ItemsSource = Sample() },
                Ui.Caption("Up and Down move between rows; Right expands, then moves to the first child; Left collapses, then moves to the parent; Enter toggles. The caret turns over MotionNormal."),
            },
        }));

    private static Control StatesPanel()
    {
        var panel = new WrapPanel();
        foreach (var (label, apply) in States)
        {
            var item = new TreeViewItem { Header = "Documents", ItemsSource = new[] { "Notes" } };
            var tree = new TreeView { Width = 160, IsHitTestVisible = false, Focusable = false, ItemsSource = new[] { item } };
            if (apply is not null)
                tree.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() => apply(item));
            panel.Children.Add(new StackPanel
            {
                Spacing = 4,
                Margin = new Thickness(0, 0, 12, 12),
                Children = { Ui.Caption(label), tree },
            });
        }
        return panel;
    }

    // A row state is its chrome's own pseudo-class.
    private static void Row(TreeViewItem item, string pseudoClass)
    {
        if (item.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == "PART_Chrome") is { } chrome)
            ((IPseudoClasses)chrome.Classes).Set(pseudoClass, true);
    }

    private static TreeViewItem[] Sample() =>
    [
        new()
        {
            Header = "Style", IsExpanded = true,
            ItemsSource = new object[] { "Colour", "Type", new TreeViewItem { Header = "Shape", ItemsSource = new[] { "Squircle", "Radii" } }, "Motion" },
        },
        new()
        {
            Header = "Blocks", IsExpanded = true,
            ItemsSource = new object[]
            {
                new TreeViewItem { Header = "Button family", IsExpanded = true, ItemsSource = new[] { "Button", "ToggleButton", "SplitButton" } },
                "ListBox",
                "TreeView",
            },
        },
        new() { Header = "Modules", ItemsSource = new[] { "StandardsWindow", "Sidebar" } },
    ];
}
