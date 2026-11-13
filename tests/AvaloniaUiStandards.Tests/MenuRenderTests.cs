using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards.Blocks;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the menu and flyout sheet (2x, labelled): menu bar, open menus, item states, context menu and flyout.</summary>
public class MenuRenderTests
{
    internal static MenuItem[] FileItems() =>
    [
        new MenuItem { Header = "New", Icon = new Icon { Data = Icons.Plus }, InputGesture = new KeyGesture(Key.N, KeyModifiers.Control) },
        new MenuItem { Header = "Open…", Icon = new Icon { Data = Icons.MagnifyingGlass }, InputGesture = new KeyGesture(Key.O, KeyModifiers.Control) },
        new MenuItem { Header = "Recent", ItemsSource = new[] { new MenuItem { Header = "Report.txt" }, new MenuItem { Header = "Notes.md" } } },
        new MenuItem { Header = "-" },
        new MenuItem { Header = "Show sidebar", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true },
        new MenuItem { Header = "Show status bar", ToggleType = MenuItemToggleType.CheckBox },
        new MenuItem { Header = "-" },
        new MenuItem { Header = "Large icons", ToggleType = MenuItemToggleType.Radio, GroupName = "size" },
        new MenuItem { Header = "Small icons", ToggleType = MenuItemToggleType.Radio, GroupName = "size", IsChecked = true },
        new MenuItem { Header = "-" },
        new MenuItem { Header = "Print…", IsEnabled = false, InputGesture = new KeyGesture(Key.P, KeyModifiers.Control) },
        new MenuItem { Header = "Settings", Icon = new Icon { Data = Icons.Gear } },
    ];

    [AvaloniaFact]
    public void Menus_RenderSheet()
    {
        using var sheet = new Sheet("Menus and flyouts: glass overlays (radius 16, padding 4), list-item pills (neutral highlight), Icon caret and check");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            sheet.AddSection($"{themeLabel}: open menus", ["Menu bar, File open (hover on Open…)", "Submenu open", "Context menu", "Flyout", "Fluent (before)"],
            [
                ("Normal", [
                    BarMenuCell(sheet, theme, hoverIndex: 1, openSubmenu: false),
                    BarMenuCell(sheet, theme, hoverIndex: 2, openSubmenu: true),
                    ContextMenuCell(sheet, theme),
                    FlyoutCell(sheet, theme),
                    BarMenuCell(sheet, theme, hoverIndex: 1, openSubmenu: false, fluent: true),
                ]),
                ("Compact", [
                    BarMenuCell(sheet, theme, hoverIndex: 1, openSubmenu: false, compact: true),
                    BarMenuCell(sheet, theme, hoverIndex: 2, openSubmenu: true, compact: true),
                    ContextMenuCell(sheet, theme, compact: true),
                    FlyoutCell(sheet, theme, compact: true),
                    BarMenuCell(sheet, theme, hoverIndex: 1, openSubmenu: false, fluent: true, compact: true),
                ]),
            ]);

            string[] states = ["Rest", "Highlight (hover or keyboard)", "Pressed", "Disabled"];
            sheet.AddSection($"{themeLabel}: menu bar items and menu items", states,
            [
                ("Menu bar item", states.Select((_, i) => BarItemCell(sheet, theme, i)).ToList()),
                ("Menu item", states.Select((_, i) => ItemCell(sheet, theme, i)).ToList()),
            ]);
        }
        sheet.Save("m3-menu.png");
    }

    // --- Cells ---------------------------------------------------------------------------

    private static Control BarMenuCell(Sheet sheet, ThemeVariant theme, int hoverIndex, bool openSubmenu, bool fluent = false, bool compact = false)
    {
        var file = new MenuItem { Header = "_File", ItemsSource = FileItems() };
        var menu = new Menu
        {
            ItemsSource = new[] { file, new MenuItem { Header = "_Edit", ItemsSource = new[] { new MenuItem { Header = "Undo" } } }, new MenuItem { Header = "_View" } },
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8, 8),
        };
        var window = Overlays.Host(theme, menu, openSubmenu ? 440 : 300, compact ? 360 : 470);
        try
        {
            Setup(window, fluent, compact);
            file.Open();
            Overlays.Pump(window);
            var item = (MenuItem)file.ItemsSource!.Cast<object>().ElementAt(hoverIndex);
            if (openSubmenu)
            {
                item.Open();
                Overlays.Pump(window);
            }
            ((IPseudoClasses)item.Classes).Set(":selected", true);
            return Overlays.Capture(sheet, window);
        }
        finally
        {
            file.Close();
            window.Close();
        }
    }

    private static Control ContextMenuCell(Sheet sheet, ThemeVariant theme, bool compact = false)
    {
        var target = new Border { Width = 1, Height = 1, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(16) };
        var cut = new MenuItem { Header = "Cut", Icon = new Icon { Data = Icons.X }, InputGesture = new KeyGesture(Key.X, KeyModifiers.Control) };
        var menu = new ContextMenu
        {
            ItemsSource = new object[]
            {
                cut,
                new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) },
                new MenuItem { Header = "Paste", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) },
                new Separator(),
                new MenuItem { Header = "Delete", IsEnabled = false },
            },
            Placement = PlacementMode.BottomEdgeAlignedLeft,
        };
        target.ContextMenu = menu;
        var window = Overlays.Host(theme, target, 240, compact ? 220 : 260);
        try
        {
            Setup(window, fluent: false, compact);
            menu.Open(target);
            Overlays.Pump(window);
            ((IPseudoClasses)cut.Classes).Set(":selected", true);
            return Overlays.Capture(sheet, window);
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    private static Control FlyoutCell(Sheet sheet, ThemeVariant theme, bool compact = false)
    {
        var accept = new Button { Content = "Empty bin" };
        accept.Classes.Add("destructive");
        var button = new Button
        {
            Content = "Empty…",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16),
            Flyout = new Flyout
            {
                Placement = PlacementMode.BottomEdgeAlignedLeft,
                Content = new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock { Text = "All items will be deleted.", TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 200 },
                        accept,
                    },
                },
            },
        };
        var window = Overlays.Host(theme, button, 270, 230);
        try
        {
            Setup(window, fluent: false, compact);
            button.Flyout!.ShowAt(button);
            Overlays.Pump(window);
            return Overlays.Capture(sheet, window);
        }
        finally
        {
            button.Flyout!.Hide();
            window.Close();
        }
    }

    private static Control BarItemCell(Sheet sheet, ThemeVariant theme, int state)
    {
        var item = new MenuItem { Header = "_File" };
        var menu = new Menu { ItemsSource = new[] { item }, Margin = new Thickness(8) };
        if (state == 3)
            item.IsEnabled = false;
        return sheet.Cell(menu, theme, afterShow: w =>
        {
            w.UpdateLayout();
            StripTransitions(item);
            if (state == 1)
                ((IPseudoClasses)item.Classes).Set(":selected", true);
            if (state == 2)
                ((IPseudoClasses)item.Classes).Set(":pressed", true);
        });
    }

    private static Control ItemCell(Sheet sheet, ThemeVariant theme, int state)
    {
        var item = new MenuItem { Header = "Open…", Icon = new Icon { Data = Icons.MagnifyingGlass }, InputGesture = new KeyGesture(Key.O, KeyModifiers.Control) };
        var menu = new ContextMenu { ItemsSource = new[] { item } };
        var target = new Border { Width = 1, Height = 1, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, ContextMenu = menu };
        var window = Overlays.Host(theme, target, 220, 70);
        try
        {
            if (state == 3)
                item.IsEnabled = false;
            menu.Placement = PlacementMode.BottomEdgeAlignedLeft;
            menu.Open(target);
            Overlays.Pump(window);
            StripTransitions(item);
            if (state == 1)
                ((IPseudoClasses)item.Classes).Set(":selected", true);
            if (state == 2)
                ((IPseudoClasses)item.Classes).Set(":pressed", true);
            return Overlays.Capture(sheet, window);
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    private static void Setup(Window window, bool fluent, bool compact)
    {
        if (fluent)
        {
            window.Styles.Clear();
            window.Styles.Add(new FluentTheme { DensityStyle = compact ? DensityStyle.Compact : DensityStyle.Normal });
        }
        else if (compact)
        {
            window.Styles.OfType<StandardsTheme>().Single().Density = DensityStyle.Compact;
        }
        Overlays.Pump(window);
    }

    private static void StripTransitions(Control root)
    {
        root.Transitions = null;
        foreach (var part in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(root).OfType<Avalonia.Animation.Animatable>())
            part.Transitions = null;
    }
}
