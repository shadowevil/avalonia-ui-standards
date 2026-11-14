using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;

namespace Catalog.Pages;

/// <summary>ToolbarBand: a live band on the window background in a card, narrowed with a slider so its items overflow into the "…" menu.</summary>
internal sealed class ToolbarBandPage : UserControl
{
    private readonly TextBlock _last = Ui.Caption("Last command: none");

    public ToolbarBandPage()
    {
        var band = Demo();
        band.HorizontalAlignment = HorizontalAlignment.Left;
        var card = new SquircleBorder { Child = band, BorderThickness = new Thickness(1), ClipToBounds = true };
        card.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("BackdropWash"));
        card.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("SystemControlForegroundBaseLowBrush"));
        card.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusOverlay"));

        var width = new Slider { Minimum = 30, Maximum = 100, Value = 100, Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
        AutomationProperties.SetName(width, "Band width");
        void Resize() => band.Width = Math.Max(0, (card.Bounds.Width - 2) * width.Value / 100);
        card.SizeChanged += (_, _) => Resize();
        width.ValueChanged += (_, _) => Resize();

        Content = Ui.Page("Toolbar band",
            Ui.Section("Live", new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    card,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 12,
                        Children = { Ui.Body("Width"), width },
                    },
                    _last,
                },
            }),
            Ui.Section("Use", Ui.Caption(
                "Directly below the title band, on the window background, 48 high (40 Compact). Leading, centre and trailing zones; " +
                "icon items with tooltips. Items that do not fit move into the trailing \"…\" menu, highest priority first, then from the " +
                "trailing end; a segmented control becomes a group of radio items. Each item is one Tab stop; \"…\" opens with Enter, Space or Down.")));
    }

    private ToolbarBand Demo()
    {
        var sidebar = Command("Show sidebar", Icons.SidebarSimple);
        ToolbarBand.SetPriority(sidebar, -1);
        var add = Command("New", Icons.Plus);
        ToolbarBand.SetBeginsGroup(add, true);
        var search = Command("Search", Icons.MagnifyingGlass);

        var range = Segmented("Range", ["Day", "Week", "Month"]);
        ToolbarBand.SetZone(range, ToolbarZone.Center);
        ToolbarBand.SetPriority(range, 1);
        var layout = new SegmentedControl();
        foreach (var (name, data) in new[] { ("List", Icons.ListBullets), ("Grid", Icons.SquaresFour) })
        {
            var segment = new SegmentedControlItem { Content = new Icon { Data = data } };
            ToolTip.SetTip(segment, name);
            AutomationProperties.SetName(segment, name);
            layout.Items.Add(segment);
        }
        layout.SelectedIndex = 0;
        ToolTip.SetTip(layout, "Layout");
        AutomationProperties.SetName(layout, "Layout");
        ToolbarBand.SetZone(layout, ToolbarZone.Center);
        ToolbarBand.SetPriority(layout, 2);
        ToolbarBand.SetBeginsGroup(layout, true);

        var sortByName = new MenuItem { Header = "Sort by name" };
        sortByName.Click += (_, _) => _last.Text = "Last command: Sort by name";
        var sortByDate = new MenuItem { Header = "Sort by date" };
        sortByDate.Click += (_, _) => _last.Text = "Last command: Sort by date";
        var view = new DropDownButton
        {
            Content = new Icon { Data = Icons.SlidersHorizontal },
            Flyout = new MenuFlyout { Items = { sortByName, sortByDate } },
        };
        Subtle(view, "View");
        ToolbarBand.SetZone(view, ToolbarZone.Trailing);
        var preview = new ToggleButton { Content = new Icon { Data = Icons.Eye } };
        Subtle(preview, "Preview");
        ToolbarBand.SetZone(preview, ToolbarZone.Trailing);
        var separator = new Separator();
        ToolbarBand.SetZone(separator, ToolbarZone.Trailing);
        var colour = new ColorPicker { Color = Color.Parse("#E0720B") };
        ToolTip.SetTip(colour, "Highlight colour");
        AutomationProperties.SetName(colour, "Highlight colour");
        ToolbarBand.SetZone(colour, ToolbarZone.Trailing);
        ToolbarBand.SetCanOverflow(colour, false);

        return new ToolbarBand { Items = { sidebar, add, search, range, layout, view, preview, separator, colour } };
    }

    private Button Command(string name, Geometry icon)
    {
        var button = new Button { Content = new Icon { Data = icon } };
        Subtle(button, name);
        button.Click += (_, _) => _last.Text = $"Last command: {name}";
        return button;
    }

    private static SegmentedControl Segmented(string label, string[] values)
    {
        var control = new SegmentedControl();
        foreach (var value in values)
            control.Items.Add(new SegmentedControlItem { Content = value });
        control.SelectedIndex = 0;
        ToolbarBand.SetLabel(control, label);
        AutomationProperties.SetName(control, label);
        return control;
    }

    // A subtle item with a tooltip and automation name.
    private static void Subtle(Control item, string name)
    {
        item.Classes.Add("subtle");
        ToolTip.SetTip(item, name);
        AutomationProperties.SetName(item, name);
    }
}
