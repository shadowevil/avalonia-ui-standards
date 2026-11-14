using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaUiStandards.Blocks;

namespace Catalog.Pages;

/// <summary>ListBox: the item states forced, and live single and multiple selection lists that scroll.</summary>
internal sealed class ListBoxPage : UserControl
{
    private static readonly (string Label, Action<ListBoxItem>? Apply)[] States =
    [
        ("Rest", null),
        ("Hover", item => Set(item, ":pointerover")),
        ("Pressed", item => Set(item, ":pressed")),
        ("Selected", item => item.IsSelected = true),
        ("Selected + hover", item => { item.IsSelected = true; Set(item, ":pointerover"); }),
        ("Selected + focus", item => { item.IsSelected = true; Set(item, ":focus-visible"); }),
        ("Disabled", item => item.IsEnabled = false),
    ];

    private static readonly (string Name, Geometry Icon)[] Places =
    [
        ("Home", Icons.House), ("Calendar", Icons.Calendar), ("Recent", Icons.Clock), ("Notifications", Icons.Bell),
        ("Messages", Icons.ChatText), ("Tables", Icons.Table), ("Palette", Icons.Palette), ("Appearance", Icons.Eye),
        ("Layout", Icons.SquaresFour), ("Tabs", Icons.Tabs), ("Search", Icons.MagnifyingGlass), ("Settings", Icons.Gear),
    ];

    private static readonly string[] Names =
        ["Aurora", "Basalt", "Cedar", "Dune", "Ember", "Fjord", "Glacier", "Harbor", "Indigo", "Juniper", "Kelp", "Lagoon", "Meadow", "Nimbus", "Orchid", "Pebble"];

    public ListBoxPage() => Content = Ui.Page("ListBox",
        Ui.Section("States", StatesPanel()),
        Ui.Section("Live", Live()));

    private static Control Live()
    {
        var withIcons = new ListBox { Width = 240, Height = 200 };
        foreach (var (name, icon) in Places)
            withIcons.Items.Add(new ListBoxItem { Content = Row(name, icon) });
        withIcons.SelectedIndex = 1;

        var text = new ListBox { Width = 240, Height = 200, ItemsSource = Names, SelectedIndex = 2 };
        var multiple = new ListBox { Width = 240, Height = 200, SelectionMode = SelectionMode.Multiple, ItemsSource = Names };
        multiple.SelectedItems?.Add(Names[1]);
        multiple.SelectedItems?.Add(Names[3]);

        // The accent flag option, on all three lists.
        var flag = new ToggleSwitch { IsChecked = false };
        flag.IsCheckedChanged += (_, _) =>
        {
            foreach (var list in new[] { withIcons, text, multiple })
                SelectionOptions.SetAccentFlag(list, flag.IsChecked == true);
        };
        var options = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto"), ColumnSpacing = 12, RowDefinitions = new RowDefinitions("Auto") };
        Ui.Place(options, Ui.Body("Accent flag"), 0, 0);
        Ui.Place(options, flag, 0, 1);

        return new StackPanel
        {
            Spacing = 12,
            Children =
            {
                options,
                new WrapPanel
                {
                    ItemSpacing = 24,
                    LineSpacing = 16,
                    Children =
                    {
                        Captioned("Single, text and icon", withIcons),
                        Captioned("Single, text", text),
                        Captioned("Multiple", multiple),
                    },
                },
                Ui.Caption("Up and Down move between items; Space or Enter selects. In the multiple list a click toggles an item, and Shift and Ctrl extend the selection. " +
                    "The lists scroll: the scrollbar appears over the content, and a soft shadow marks each edge with items out of view."),
            },
        };
    }

    private static Control Row(string text, Geometry icon) => new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Spacing = 8,
        Children = { new Icon { Data = icon }, new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center } },
    };

    private static Control Captioned(string caption, Control control) =>
        new StackPanel { Spacing = 4, Children = { Ui.Caption(caption), control } };

    private static Control StatesPanel()
    {
        var panel = new WrapPanel();
        foreach (var (label, apply) in States)
        {
            var item = new ListBoxItem { Content = Row("Documents", Icons.House) };
            var list = new ListBox { Width = 160, IsHitTestVisible = false, Focusable = false, Items = { item, new ListBoxItem { Content = "Downloads" } } };
            if (apply is not null)
                list.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() => apply(item));
            panel.Children.Add(new StackPanel
            {
                Spacing = 4,
                Margin = new Thickness(0, 0, 12, 12),
                Children = { Ui.Caption(label), list },
            });
        }
        return panel;
    }

    private static void Set(ListBoxItem item, string pseudoClass) => ((IPseudoClasses)item.Classes).Set(pseudoClass, true);
}
