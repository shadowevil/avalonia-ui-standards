using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

namespace Catalog.Pages;

/// <summary>Menus and flyouts: menu items and menu bar items in every state, and live menus, context menu and flyouts.</summary>
internal sealed class MenuPage : UserControl
{
    private static readonly (string Label, string[] PseudoClasses, bool Enabled)[] States =
    [
        ("Rest", [], true),
        ("Highlight", [":selected"], true),
        ("Pressed", [":selected", ":pressed"], true),
        ("Disabled", [], false),
    ];

    public MenuPage() => Content = Ui.Page("Menus and flyouts",
        Ui.Section("Menu items", ItemStates()),
        Ui.Section("Menu bar items", BarStates()),
        Ui.Section("Live", Live()),
        Ui.Caption("Menus and flyouts are glass overlays (radius 16, 4 px inset). Items highlight in the list item's neutral fill; the menu bar's items are subtle buttons. Overlays fade in over MotionFast."));

    private static Control ItemStates()
    {
        (string Label, Func<MenuItem> Create)[] variants =
        [
            ("Icon and shortcut", () => new MenuItem { Header = "Open…", Icon = new Icon { Data = Icons.MagnifyingGlass }, InputGesture = new KeyGesture(Key.O, KeyModifiers.Control) }),
            ("Submenu", () => new MenuItem { Header = "Recent", ItemsSource = new[] { new MenuItem { Header = "Report.txt" } } }),
            ("Checked", () => new MenuItem { Header = "Show sidebar", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true }),
            ("Radio, checked", () => new MenuItem { Header = "Small icons", ToggleType = MenuItemToggleType.Radio, IsChecked = true }),
        ];
        var grid = StateTable(variants.Length);
        for (var r = 0; r < variants.Length; r++)
        {
            Place(grid, Ui.Body(variants[r].Label), r + 1, 0);
            for (var c = 0; c < States.Length; c++)
            {
                var item = variants[r].Create();
                Grid.SetIsSharedSizeScope(item, true);
                Place(grid, InMenu(Forced(item, States[c])), r + 1, c + 1);
            }
        }
        return Scroll(grid);
    }

    private static Control BarStates()
    {
        var grid = StateTable(1);
        Place(grid, Ui.Body("File"), 1, 0);
        for (var c = 0; c < States.Length; c++)
        {
            var item = new MenuItem { Header = "File" };
            var state = States[c];
            var menu = new Menu { ItemsSource = new[] { Forced(item, state) }, IsHitTestVisible = false };
            Place(grid, new Border { Padding = new Thickness(8), Child = menu }, 1, c + 1);
        }
        return Scroll(grid);
    }

    private static Control Live()
    {
        var menu = new Menu
        {
            ItemsSource = new[]
            {
                new MenuItem
                {
                    Header = "File",
                    ItemsSource = new object[]
                    {
                        new MenuItem { Header = "New", Icon = new Icon { Data = Icons.Plus }, InputGesture = new KeyGesture(Key.N, KeyModifiers.Control) },
                        new MenuItem { Header = "Open…", Icon = new Icon { Data = Icons.MagnifyingGlass }, InputGesture = new KeyGesture(Key.O, KeyModifiers.Control) },
                        new MenuItem { Header = "Recent", ItemsSource = new[] { new MenuItem { Header = "Report.txt" }, new MenuItem { Header = "Notes.md" } } },
                        new Separator(),
                        new MenuItem { Header = "Print…", IsEnabled = false, InputGesture = new KeyGesture(Key.P, KeyModifiers.Control) },
                    },
                },
                new MenuItem
                {
                    Header = "View",
                    ItemsSource = new object[]
                    {
                        new MenuItem { Header = "Show sidebar", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true },
                        new MenuItem { Header = "Show status bar", ToggleType = MenuItemToggleType.CheckBox },
                        new Separator(),
                        new MenuItem { Header = "Large icons", ToggleType = MenuItemToggleType.Radio, GroupName = "catalog-size" },
                        new MenuItem { Header = "Small icons", ToggleType = MenuItemToggleType.Radio, GroupName = "catalog-size", IsChecked = true },
                    },
                },
            },
        };

        var contextArea = new SquircleBorder { Width = 200, Height = 64, Child = Ui.Caption("Right-click here") };
        ((TextBlock)contextArea.Child).HorizontalAlignment = HorizontalAlignment.Center;
        contextArea.Bind(SquircleBorder.CornerRadiusProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CornerRadiusControl"));
        contextArea.Bind(SquircleBorder.BackgroundProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("SystemControlBackgroundChromeMediumLowBrush"));
        contextArea.ContextMenu = new ContextMenu
        {
            ItemsSource = new object[]
            {
                new MenuItem { Header = "Cut", InputGesture = new KeyGesture(Key.X, KeyModifiers.Control) },
                new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) },
                new MenuItem { Header = "Paste", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) },
            },
        };

        var menuFlyout = new Button
        {
            Content = "Menu flyout",
            Flyout = new MenuFlyout { ItemsSource = new[] { new MenuItem { Header = "Rename" }, new MenuItem { Header = "Duplicate" } } },
        };
        var confirm = new Button { Content = "Empty bin" };
        confirm.Classes.Add("destructive");
        var flyout = new Button
        {
            Content = "Flyout",
            Flyout = new Flyout
            {
                Content = new StackPanel
                {
                    Spacing = 12,
                    Children = { new TextBlock { Text = "All items will be deleted.", TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 200 }, confirm },
                },
            },
        };
        return new StackPanel
        {
            Spacing = 12,
            Children =
            {
                menu,
                new WrapPanel { Children = { Spaced(contextArea), Spaced(menuFlyout), Spaced(flyout) } },
            },
        };
    }

    // A menu item on a glass menu surface, as it appears in an open menu.
    private static Control InMenu(MenuItem item) => new Border
    {
        Padding = new Thickness(4),
        IsHitTestVisible = false,
        Child = new GlassBorder { Width = 220, Padding = new Thickness(4), Child = item },
    };

    private static MenuItem Forced(MenuItem item, (string Label, string[] PseudoClasses, bool Enabled) state)
    {
        item.IsEnabled = state.Enabled;
        item.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            foreach (var pseudoClass in state.PseudoClasses)
                ((IPseudoClasses)item.Classes).Set(pseudoClass, true);
        });
        return item;
    }

    private static Grid StateTable(int rows)
    {
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 4 };
        for (var c = 0; c <= States.Length; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(c == 0 ? new GridLength(150) : GridLength.Auto));
        for (var r = 0; r <= rows; r++)
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var c = 0; c < States.Length; c++)
        {
            var label = Ui.Caption(States[c].Label);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            Place(grid, label, 0, c + 1);
        }
        return grid;
    }

    private static Control Scroll(Control content) => new ScrollViewer
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        Content = content,
    };

    private static Control Spaced(Control control)
    {
        control.VerticalAlignment = VerticalAlignment.Center;
        return new Border { Margin = new Thickness(0, 0, 16, 12), Child = control };
    }

    private static void Place(Grid grid, Control control, int row, int column)
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        grid.Children.Add(control);
    }
}
