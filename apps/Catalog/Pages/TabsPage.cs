using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using AvaloniaUiStandards.Blocks;

namespace Catalog.Pages;

/// <summary>TabControl and TabStrip: headers in every state (hover and press on "Activity"), with icons, and live tabs.</summary>
internal sealed class TabsPage : UserControl
{
    public TabsPage() => Content = Ui.Page("Tabs",
        Ui.Section("TabControl", StateGrid.Build(
        [
            ("Text", () => Tabs(icons: false)),
            ("With icons", () => Tabs(icons: true)),
        ], Activity)),
        Ui.Section("TabStrip", StateGrid.Build([("TabStrip", Strip)], Activity)),
        Ui.Section("Use", Ui.Caption(
            "Tabs switch between content areas; a segmented control chooses a value for one setting. The arrow keys, Home and End move between tabs.")));

    private static Control Activity(Control control) => ((SelectingItemsControl)control).ContainerFromIndex(1) ?? control;

    private static TabControl Tabs(bool icons)
    {
        var tabs = new TabControl { Width = 300 };
        foreach (var (header, icon) in new[] { ("General", Icons.Gear), ("Activity", Icons.Bell), ("Sharing", Icons.ChatText) })
            tabs.Items.Add(new TabItem
            {
                Header = header,
                Icon = icons ? new Icon { Data = icon } : null,
                Content = new TextBlock { Text = $"{header} settings.", TextWrapping = TextWrapping.Wrap },
            });
        tabs.SelectedIndex = 0;
        return tabs;
    }

    private static TabStrip Strip()
    {
        var strip = new TabStrip();
        foreach (var header in new[] { "General", "Activity", "Sharing" })
            strip.Items.Add(new TabStripItem { Content = header });
        strip.SelectedIndex = 0;
        return strip;
    }
}
