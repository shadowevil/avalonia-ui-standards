using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;

namespace Catalog.Pages;

/// <summary>TreeView: the row states forced, and a live tree with its options.</summary>
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

    private static readonly (string Label, TreeViewClickAction Action)[] Clicks =
    [
        ("Single", TreeViewClickAction.SingleClick),
        ("Double", TreeViewClickAction.DoubleClick),
        ("Both", TreeViewClickAction.SingleClick | TreeViewClickAction.DoubleClick),
    ];

    public TreeViewPage() => Content = Ui.Page("TreeView",
        Ui.Section("States", StatesPanel()),
        Ui.Section("Live", Live()));

    private static Control Live()
    {
        var tree = new TreeView { Width = 280, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, ItemsSource = Sample() };
        var options = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto"), ColumnSpacing = 12, RowSpacing = 8 };
        AddOption(options, 0, "Expand on", ClickChoice(tree, TreeViewOptions.ExpandOnProperty));
        AddOption(options, 1, "Collapse on", ClickChoice(tree, TreeViewOptions.CollapseOnProperty));
        var lines = new ToggleSwitch { IsChecked = TreeViewOptions.GetShowLines(tree) };
        lines.IsCheckedChanged += (_, _) => TreeViewOptions.SetShowLines(tree, lines.IsChecked == true);
        AddOption(options, 2, "Lines", lines);
        return new StackPanel
        {
            Spacing = 12,
            Children =
            {
                options,
                tree,
                Ui.Caption("Up and Down move between rows; Right expands, then moves to the first child; Left collapses, then moves to the parent; Enter toggles. " +
                    "The caret toggles with one click whatever the click options; a double click toggles a row once even when single clicks toggle it too. The caret turns over MotionNormal."),
            },
        };
    }

    private static void AddOption(Grid grid, int row, string label, Control control)
    {
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        Ui.Place(grid, Ui.Body(label), row, 0);
        Ui.Place(grid, control, row, 1);
    }

    // Single, Double or Both for one of the click options, applied to the tree.
    private static SegmentedControl ClickChoice(TreeView tree, AttachedProperty<TreeViewClickAction> property)
    {
        var control = new SegmentedControl();
        foreach (var (label, _) in Clicks)
            control.Items.Add(new SegmentedControlItem { Content = label });
        control.SelectedIndex = Array.FindIndex(Clicks, c => c.Action == tree.GetValue(property));
        control.SelectionChanged += (_, _) =>
        {
            if (control.SelectedIndex >= 0)
                tree.SetValue(property, Clicks[control.SelectedIndex].Action);
        };
        return control;
    }

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
