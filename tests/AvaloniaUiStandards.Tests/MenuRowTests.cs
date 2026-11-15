using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// The menu row (0044): the window's NativeMenu as an in-window Menu bar in the Windows layout, kept in sync with the
/// menu, with the Application's items at the end of Help, folding into "…" when narrow, reachable with Alt, access
/// keys, F10 and the items' gestures, and dragging the window by its empty area. The layout mode decides whether it
/// shows, and also the window buttons and the toolbar band's drag.
/// </summary>
public class MenuRowTests
{
    private static NativeMenuItem Item(string header, string? gesture = null) =>
        new(header) { Gesture = gesture is null ? null : KeyGesture.Parse(gesture) };

    private static NativeMenu FourMenus() => new()
    {
        new NativeMenuItem("_File") { Menu = new NativeMenu { Item("_New", "Ctrl+N"), new NativeMenuItemSeparator(), Item("E_xit") } },
        new NativeMenuItem("_Edit") { Menu = new NativeMenu { Item("_Copy", "Ctrl+Shift+C") } },
        new NativeMenuItem("_View") { Menu = new NativeMenu { new NativeMenuItem("_Status bar") { ToggleType = MenuItemToggleType.CheckBox, IsChecked = true } } },
        new NativeMenuItem("_Help") { Menu = new NativeMenu { Item("_Documentation", "F1") } },
    };

    private static StandardsWindow Show(NativeMenu? menu, double width = 800, WindowLayoutMode layout = WindowLayoutMode.Windows,
        DensityStyle density = DensityStyle.Normal, ThemeVariant? theme = null)
    {
        var window = new StandardsWindow(nativeFrame: false)
        {
            Title = "Menus",
            Width = width,
            Height = 420,
            LayoutMode = layout,
            Content = new TextBox { Text = "Content" },
            RequestedThemeVariant = theme ?? ThemeVariant.Light,
        };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true, Density = density });
        if (menu is not null)
            NativeMenu.SetMenu(window, menu);
        window.Show();
        window.Activate();
        Pump(window);
        return window;
    }

    private static MenuItem Title(StandardsWindow window, string header) =>
        window.MenuRow.Titles.First(t => Equals(t.Header, header));

    private static List<object?> Headers(ItemsControl menu) =>
        menu.Items.Select(i => i is MenuItem m ? m.Header : i is Separator ? "-" : i).ToList();

    private static List<object?> ShownTitles(StandardsWindow window) =>
        window.MenuRow.Titles.Where(t => t.IsVisible).Select(t => t.Header).ToList();

    // --- Layout mode --------------------------------------------------------------------------

    [Theory]
    [InlineData(WindowLayoutMode.Automatic, true, WindowLayoutMode.MacOS)]
    [InlineData(WindowLayoutMode.Automatic, false, WindowLayoutMode.Windows)]
    [InlineData(WindowLayoutMode.MacOS, false, WindowLayoutMode.MacOS)]
    [InlineData(WindowLayoutMode.Windows, true, WindowLayoutMode.Windows)]
    public void LayoutMode_ResolvesAutomaticByOS_LinuxAsWindows(WindowLayoutMode mode, bool macOS, WindowLayoutMode expected) =>
        Assert.Equal(expected, WindowLayout.Resolve(mode, macOS));

    [AvaloniaFact]
    public void LayoutMode_DrivesButtonsMenuRowAndToolbarDrag_AndSwitchesLive()
    {
        var window = Show(FourMenus(), layout: WindowLayoutMode.MacOS);
        try
        {
            var row = Part<Panel>(window, StandardsWindowTheme.MenuRowPart);
            var buttons = Part<WindowButtons>(window, StandardsWindowTheme.WindowButtonsPart);
            var panelTop = window.ContentPanelBounds.Top;

            // macOS: traffic lights, no menu row (its menu bar out of the window), the toolbar band drags.
            Assert.False(row.IsVisible);
            Assert.Null(window.MenuRow.Parent);
            Assert.Equal(WindowButtonStyle.TrafficLights, buttons.ButtonStyle);
            Assert.True(window.ToolbarBandDragsWindow);

            // Windows, live: caption buttons, the row with the menu, the band no longer drags; the panel moves down by the row.
            window.LayoutMode = WindowLayoutMode.Windows;
            Pump(window);
            Assert.True(row.IsVisible);
            Assert.Same(row, window.MenuRow.Parent);
            Assert.Equal(WindowButtonStyle.CaptionButtons, buttons.ButtonStyle);
            Assert.False(window.ToolbarBandDragsWindow);
            Assert.Equal(new object?[] { "_File", "_Edit", "_View", "_Help" }, window.MenuRow.Titles.Select(t => t.Header));
            Assert.Equal(StandardsWindowTheme.MenuRowNormal, row.Bounds.Height);
            Assert.Equal(panelTop + StandardsWindowTheme.MenuRowNormal, window.ContentPanelBounds.Top);

            // An explicit button style wins over the layout's.
            window.WindowButtonStyle = WindowButtonStyle.TrafficLights;
            Pump(window);
            Assert.Equal(WindowButtonStyle.TrafficLights, buttons.ButtonStyle);
            window.WindowButtonStyle = WindowButtonStyle.Auto;

            // Back to macOS: the row is gone and nothing below it stays shifted.
            window.LayoutMode = WindowLayoutMode.MacOS;
            Pump(window);
            Assert.False(row.IsVisible);
            Assert.Null(window.MenuRow.Parent);
            Assert.Equal(panelTop, window.ContentPanelBounds.Top);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MenuRow_ShowsOnlyWithAMenu_AndFollowsTheMenuBeingSet()
    {
        var window = Show(null);
        try
        {
            var row = Part<Panel>(window, StandardsWindowTheme.MenuRowPart);
            var panelTop = window.ContentPanelBounds.Top;
            Assert.False(row.IsVisible);

            NativeMenu.SetMenu(window, FourMenus());
            Pump(window);
            Assert.True(row.IsVisible);
            Assert.Equal(4, window.MenuRow.Titles.Count);

            NativeMenu.SetMenu(window, null!);
            Pump(window);
            Assert.False(row.IsVisible);
            Assert.Equal(panelTop, window.ContentPanelBounds.Top);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MenuRow_IsCompactAtCompactDensity_AndInsetLikeTheTitleBand()
    {
        var window = Show(FourMenus(), density: DensityStyle.Compact);
        try
        {
            var row = Part<Panel>(window, StandardsWindowTheme.MenuRowPart);
            Assert.Equal(StandardsWindowTheme.MenuRowCompact, row.Bounds.Height);
            var file = Title(window, "_File");
            Assert.Equal(StandardsWindowTheme.MenuRowCompact, file.Bounds.Height);
            Assert.Equal(Spacing.L, file.TranslatePoint(default, row)!.Value.X, 1);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Sync ---------------------------------------------------------------------------------

    [AvaloniaFact]
    public void Presenter_MirrorsTheMenu_AndFollowsItsChanges()
    {
        var menu = FourMenus();
        var window = Show(menu);
        try
        {
            var file = Title(window, "_File");
            Assert.Equal(new object?[] { "_New", "-", "E_xit" }, Headers(file));
            var newItem = (MenuItem)file.Items[0]!;
            Assert.Equal(KeyGesture.Parse("Ctrl+N"), newItem.InputGesture);
            var status = (MenuItem)Title(window, "_View").Items[0]!;
            Assert.Equal(MenuItemToggleType.CheckBox, status.ToggleType);
            Assert.True(status.IsChecked);

            // Item properties follow.
            var nativeNew = (NativeMenuItem)menu.Items[0].As<NativeMenuItem>().Menu!.Items[0];
            nativeNew.Header = "_New window";
            nativeNew.IsEnabled = false;
            nativeNew.Gesture = KeyGesture.Parse("Ctrl+Shift+N");
            var nativeStatus = menu.Items[2].As<NativeMenuItem>().Menu!.Items[0].As<NativeMenuItem>();
            nativeStatus.IsChecked = false;
            nativeStatus.ToggleType = MenuItemToggleType.Radio;
            Pump(window);
            Assert.Equal("_New window", newItem.Header);
            Assert.False(newItem.IsEnabled);
            Assert.Equal(KeyGesture.Parse("Ctrl+Shift+N"), newItem.InputGesture);
            Assert.False(status.IsChecked);
            Assert.Equal(MenuItemToggleType.Radio, status.ToggleType);

            // Items added and removed in a submenu, while it is open.
            file.IsSubMenuOpen = true;
            Pump(window);
            var fileMenu = menu.Items[0].As<NativeMenuItem>().Menu!;
            fileMenu.Items.Insert(1, Item("_Open…", "Ctrl+O"));
            Pump(window);
            Assert.True(file.IsSubMenuOpen);
            Assert.Equal(new object?[] { "_New window", "_Open…", "-", "E_xit" }, Headers(file));
            fileMenu.Items.RemoveAt(0);
            Pump(window);
            Assert.Equal(new object?[] { "_Open…", "-", "E_xit" }, Headers(file));

            // A submenu given to an item, and an item hidden.
            var exit = fileMenu.Items.OfType<NativeMenuItem>().Last();
            exit.Menu = new NativeMenu { Item("_Now"), Item("_Later") };
            exit.IsVisible = false;
            Pump(window);
            var builtExit = file.Items.OfType<MenuItem>().Last();
            Assert.Equal(new object?[] { "_Now", "_Later" }, Headers(builtExit));
            Assert.False(builtExit.IsVisible);

            // Titles added, renamed, hidden and removed.
            file.IsSubMenuOpen = false;
            menu.Items.Insert(3, new NativeMenuItem("_Tools") { Menu = new NativeMenu { Item("_Options") } });
            Pump(window);
            Assert.Equal(new object?[] { "_File", "_Edit", "_View", "_Tools", "_Help" }, window.MenuRow.Titles.Select(t => t.Header));
            menu.Items[1].As<NativeMenuItem>().Header = "_Change";
            menu.Items[2].As<NativeMenuItem>().IsVisible = false;
            menu.Items.RemoveAt(3);
            Pump(window);
            Assert.Equal(new object?[] { "_File", "_Change", "_Help" }, window.MenuRow.Titles.Select(t => t.Header));
        }
        finally
        {
            window.Close();
        }
    }

    private static List<object?> Shown(ItemsControl menu) =>
        menu.Items.OfType<Control>().Where(c => c.IsVisible).Select(i => i is MenuItem m ? m.Header : "-").ToList();

    [AvaloniaFact]
    public void Separators_NeverShowFirstLastOrNextToAnother_WhateverIsHidden()
    {
        var hiddenFirst = Item("_Hidden first");
        var hiddenTail = Item("_Hidden tail");
        var hiddenMid = Item("_Hidden mid");
        hiddenFirst.IsVisible = hiddenTail.IsVisible = hiddenMid.IsVisible = false;
        var menu = new NativeMenu
        {
            new NativeMenuItem("_File")
            {
                Menu = new NativeMenu
                {
                    hiddenFirst, new NativeMenuItemSeparator(), Item("_A"), new NativeMenuItemSeparator(), hiddenMid,
                    new NativeMenuItemSeparator(), Item("_B"), new NativeMenuItemSeparator(), new NativeMenuItemSeparator(),
                    Item("_C"), new NativeMenuItemSeparator(), hiddenTail,
                },
            },
            new NativeMenuItem("_Edit")
            {
                Menu = new NativeMenu
                {
                    Item("_Open"),
                    new NativeMenuItem("_Recent")
                    {
                        Menu = new NativeMenu { new NativeMenuItemSeparator(), Item("_One"), new NativeMenuItemSeparator(), new NativeMenuItemSeparator(), Item("_Two"), new NativeMenuItemSeparator() },
                    },
                    new NativeMenuItemSeparator(),
                },
            },
        };
        var window = Show(menu);
        try
        {
            Assert.Equal(new object?[] { "_A", "-", "_B", "-", "_C" }, Shown(Title(window, "_File")));
            var edit = Title(window, "_Edit");
            Assert.Equal(new object?[] { "_Open", "_Recent" }, Shown(edit));
            Assert.Equal(new object?[] { "_One", "-", "_Two" }, Shown((MenuItem)edit.Items[1]!));

            // An item shown or hidden later moves the separators with it.
            var file = menu.Items[0].As<NativeMenuItem>().Menu!;
            hiddenTail.IsVisible = true;
            Pump(window);
            Assert.Equal(new object?[] { "_A", "-", "_B", "-", "_C", "-", "_Hidden tail" }, Shown(Title(window, "_File")));
            hiddenMid.IsVisible = true;
            hiddenFirst.IsVisible = true;
            Pump(window);
            Assert.Equal(new object?[] { "_Hidden first", "-", "_A", "-", "_Hidden mid", "-", "_B", "-", "_C", "-", "_Hidden tail" }, Shown(Title(window, "_File")));
            foreach (var item in file.Items.OfType<NativeMenuItem>().Where(i => i.Header is "_A" or "_B" or "_C" or "_Hidden mid"))
                item.IsVisible = false;
            Pump(window);
            Assert.Equal(new object?[] { "_Hidden first", "-", "_Hidden tail" }, Shown(Title(window, "_File")));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AppMenu_SeparatorBeforeItsItems_HidesWhenTheyAreAllHidden()
    {
        var app = Application.Current!;
        var about = Item("_About");
        NativeMenu.SetMenu(app, new NativeMenu { about });
        var window = Show(FourMenus());
        try
        {
            Assert.Equal(new object?[] { "_Documentation", "-", "_About" }, Shown(Title(window, "_Help")));
            about.IsVisible = false;
            Pump(window);
            Assert.Equal(new object?[] { "_Documentation" }, Shown(Title(window, "_Help")));
        }
        finally
        {
            window.Close();
            NativeMenu.SetMenu(app, null!);
        }
    }

    [AvaloniaFact]
    public void Clicks_GoToTheNativeItem_WhichKeepsTheCheckState()
    {
        var menu = FourMenus();
        var window = Show(menu);
        try
        {
            var nativeNew = menu.Items[0].As<NativeMenuItem>().Menu!.Items[0].As<NativeMenuItem>();
            var clicks = 0;
            nativeNew.Click += (_, _) => clicks++;
            var file = Title(window, "_File");
            file.IsSubMenuOpen = true;
            Pump(window);
            ((MenuItem)file.Items[0]!).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Pump(window);
            Assert.Equal(1, clicks);

            // The app checks and unchecks the item in its Click, as with a system menu; the row shows its state.
            var nativeStatus = menu.Items[2].As<NativeMenuItem>().Menu!.Items[0].As<NativeMenuItem>();
            nativeStatus.Click += (_, _) => nativeStatus.IsChecked = !nativeStatus.IsChecked;
            var view = Title(window, "_View");
            view.IsSubMenuOpen = true;
            Pump(window);
            var status = (MenuItem)view.Items[0]!;
            status.Focus();
            Press(window, Key.Enter);
            Assert.False(nativeStatus.IsChecked);
            Assert.False(status.IsChecked);

            // An item whose Click does not change it stays as the native item has it.
            var untouched = new NativeMenuItem("_Fixed") { ToggleType = MenuItemToggleType.CheckBox, IsChecked = true };
            menu.Items[2].As<NativeMenuItem>().Menu!.Items.Add(untouched);
            Pump(window);
            view.IsSubMenuOpen = true;
            Pump(window);
            var fixedItem = view.Items.OfType<MenuItem>().Last();
            fixedItem.Focus();
            Press(window, Key.Enter);
            Assert.True(untouched.IsChecked);
            Assert.True(fixedItem.IsChecked);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RadioItems_KeepTheNativeCheckState_WhenClickChangesNothing()
    {
        var radios = new[] { "_Small", "_Medium", "_Large" }
            .Select((h, i) => new NativeMenuItem(h) { ToggleType = MenuItemToggleType.Radio, IsChecked = i == 0 }).ToList();
        var size = new NativeMenu();
        foreach (var radio in radios)
            size.Add(radio);
        var menu = new NativeMenu { new NativeMenuItem("_View") { Menu = size } };
        var window = Show(menu);
        try
        {
            var view = Title(window, "_View");
            view.IsSubMenuOpen = true;
            Pump(window);
            var items = view.Items.OfType<MenuItem>().ToList();
            items[2].Focus();
            Press(window, Key.Enter);
            Pump(window);
            for (var i = 0; i < radios.Count; i++)
                Assert.Equal(radios[i].IsChecked, items[i].IsChecked);
            Assert.True(items[0].IsChecked);
            Assert.False(items[2].IsChecked);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ClosedWindow_LetsGoOfTheMenus()
    {
        var app = Application.Current!;
        var about = new NativeMenuItem("_About Menus") { Gesture = KeyGesture.Parse("Ctrl+Shift+A") };
        NativeMenu.SetMenu(app, new NativeMenu { about });
        var appItems = (Avalonia.Collections.AvaloniaList<NativeMenuItemBase>)NativeMenu.GetMenu(app)!.Items;
        var handlersBefore = Handlers(appItems);
        var appHandlersBefore = PropertyChangedHandlers(app);
        var menu = FourMenus();
        // NativeMenu watches its own list: that handler stays.
        var menuHandlersBefore = Handlers(menu.Items);
        var window = Show(menu);
        try
        {
            var builtAbout = Title(window, "_Help").Items.OfType<MenuItem>().Last();
            Assert.True(Handlers(appItems) > handlersBefore);
            window.Close();
            Pump(window);

            // No subscriptions are left on the Application's menu, its items or the Application.
            Assert.Equal(handlersBefore, Handlers(appItems));
            Assert.Equal(appHandlersBefore, PropertyChangedHandlers(app));
            Assert.Equal(0, PropertyChangedHandlers(about));
            // The closed window's item no longer follows the Application's item (its binding is gone).
            about.Header = "_About";
            Assert.NotEqual("_About", builtAbout.Header);
            Assert.Equal(menuHandlersBefore, Handlers(menu.Items));
            Assert.Null(window.MenuRow.Parent);
        }
        finally
        {
            window.Close();
            NativeMenu.SetMenu(app, null!);
        }
    }

    // The number of handlers on a list's CollectionChanged event.
    private static int Handlers(object list)
    {
        var count = 0;
        for (var type = list.GetType(); type is not null; type = type.BaseType)
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
                if (field.FieldType == typeof(System.Collections.Specialized.NotifyCollectionChangedEventHandler))
                    count += (field.GetValue(list) as Delegate)?.GetInvocationList().Length ?? 0;
        return count;
    }

    // The number of handlers on an object's PropertyChanged event, and of observers of its properties.
    private static int PropertyChangedHandlers(AvaloniaObject target)
    {
        var count = 0;
        for (var type = target.GetType(); type is not null; type = type.BaseType)
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
                if (field.FieldType == typeof(EventHandler<AvaloniaPropertyChangedEventArgs>))
                    count += (field.GetValue(target) as Delegate)?.GetInvocationList().Length ?? 0;
        return count;
    }

    // --- Application menu merge ---------------------------------------------------------------

    [AvaloniaFact]
    public void AppMenu_JoinsTheEndOfHelp_AfterASeparator_AndClicksReachIt()
    {
        var app = Application.Current!;
        var about = new NativeMenuItem("_About Menus");
        var abouts = 0;
        about.Click += (_, _) => abouts++;
        NativeMenu.SetMenu(app, new NativeMenu { about });
        var window = Show(FourMenus());
        try
        {
            var help = Title(window, "_Help");
            Assert.Equal(new object?[] { "_Documentation", "-", "_About Menus" }, Headers(help));
            help.IsSubMenuOpen = true;
            Pump(window);
            help.Items.OfType<MenuItem>().Last().RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(1, abouts);

            // Application items added later join too.
            NativeMenu.GetMenu(app)!.Items.Add(Item("_Licences"));
            Pump(window);
            Assert.Equal(new object?[] { "_Documentation", "-", "_About Menus", "_Licences" }, Headers(Title(window, "_Help")));
        }
        finally
        {
            window.Close();
            NativeMenu.SetMenu(app, null!);
        }
    }

    [AvaloniaFact]
    public void AppMenu_WithoutAHelpMenu_GetsOneAddedAtTheEnd()
    {
        var app = Application.Current!;
        NativeMenu.SetMenu(app, new NativeMenu { new NativeMenuItem("_About Menus") });
        var menu = new NativeMenu { new NativeMenuItem("_File") { Menu = new NativeMenu { Item("_New") } } };
        var window = Show(menu);
        try
        {
            Assert.Equal(new object?[] { "_File", MenuRow.HelpHeader }, window.MenuRow.Titles.Select(t => t.Header));
            Assert.Equal(new object?[] { "_About Menus" }, Headers(window.MenuRow.Titles[^1]));

            // A Help title matches without its access key and case.
            Assert.True(MenuRow.SameHeader("_help", "Help"));
            Assert.False(MenuRow.SameHeader("Helper", "_Help"));
        }
        finally
        {
            window.Close();
            NativeMenu.SetMenu(app, null!);
        }
    }

    // --- Folding ------------------------------------------------------------------------------

    [Fact]
    public void TitlesThatFit_FoldFromTheEndBesideTheMoreTitle()
    {
        double[] widths = [50, 50, 50, 50];
        Assert.Equal(4, MenuRow.TitlesThatFit(widths, 32, 200));
        Assert.Equal(3, MenuRow.TitlesThatFit(widths, 32, 199));
        Assert.Equal(2, MenuRow.TitlesThatFit(widths, 32, 150));
        Assert.Equal(0, MenuRow.TitlesThatFit(widths, 32, 60));
    }

    private static NativeMenu EightMenus()
    {
        var menu = FourMenus();
        foreach (var header in new[] { "_Selection", "_Go", "_Run", "_Terminal" })
            menu.Items.Insert(menu.Items.Count - 1, new NativeMenuItem(header) { Menu = new NativeMenu { Item(header + " item") } });
        return menu;
    }

    [AvaloniaFact]
    public void Narrow_FoldsTitlesFromTheEndIntoMore_AndUnfoldsWhenWider()
    {
        var app = Application.Current!;
        NativeMenu.SetMenu(app, new NativeMenu { new NativeMenuItem("_About Menus") });
        var window = Show(EightMenus(), width: 1000);
        try
        {
            var more = window.MenuRow.MoreItem;
            Assert.Equal(8, ShownTitles(window).Count);
            Assert.False(more.IsVisible);

            // Eight menus do not fit at the minimum width (480 frame plus the shadow margin): the last ones fold.
            window.Width = 528;
            Pump(window);
            var shown = ShownTitles(window);
            Assert.InRange(shown.Count, 1, 7);
            Assert.Equal(new object?[] { "_File", "_Edit", "_View", "_Selection", "_Go", "_Run", "_Terminal", "_Help" }.Take(shown.Count), shown);
            Assert.True(more.IsVisible);
            Assert.Equal(ToolbarBand.MoreLabel, ToolTip.GetTip(more));

            // "…" holds the folded titles as submenus, with their items (Help with the Application's).
            var folded = window.MenuRow.Titles.Skip(shown.Count).Select(t => t.Header).ToList();
            Assert.Equal(folded, more.Items.OfType<MenuItem>().Select(m => m.Header));
            Assert.Equal(new object?[] { "_Documentation", "-", "_About Menus" }, Headers(more.Items.OfType<MenuItem>().Last()));

            // Everything shown lies inside the row, the "…" after the last title.
            var row = Part<Panel>(window, StandardsWindowTheme.MenuRowPart);
            var moreRight = more.TranslatePoint(new Point(more.Bounds.Width, 0), row)!.Value.X;
            Assert.True(moreRight <= row.Bounds.Width - Spacing.L + 0.5, $"\"…\" ends at {moreRight} of {row.Bounds.Width}");
            var last = window.MenuRow.Titles[shown.Count - 1];
            Assert.Equal(last.TranslatePoint(new Point(last.Bounds.Width, 0), row)!.Value.X, more.TranslatePoint(default, row)!.Value.X, 1);

            window.Width = 1000;
            Pump(window);
            Assert.Equal(8, ShownTitles(window).Count);
            Assert.False(more.IsVisible);
        }
        finally
        {
            window.Close();
            NativeMenu.SetMenu(app, null!);
        }
    }

    [AvaloniaFact]
    public void FourMenus_FitAtTheMinimumWidth()
    {
        var window = Show(FourMenus(), width: 528);
        try
        {
            Assert.Equal(4, ShownTitles(window).Count);
            Assert.False(window.MenuRow.MoreItem.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Keyboard -----------------------------------------------------------------------------

    [AvaloniaFact]
    public void Keyboard_AltAccessKeysAndF10ReachTheMenu()
    {
        var window = Show(FourMenus());
        try
        {
            var box = (TextBox)window.Content!;
            box.Focus();
            Pump(window);

            // Alt alone selects the first title.
            Press(window, Key.LeftAlt, RawInputModifiers.Alt, physical: PhysicalKey.AltLeft);
            Assert.True(Title(window, "_File").IsSelected);
            // The arrow keys move along the titles.
            Press(window, Key.Right);
            Assert.True(Title(window, "_Edit").IsSelected);
            Press(window, Key.Left);
            Press(window, Key.Left);
            Assert.True(Title(window, "_Help").IsSelected);
            Press(window, Key.Escape);
            Press(window, Key.Escape);
            box.Focus();
            Pump(window);

            // Alt with an access key opens that menu.
            Press(window, Key.E, RawInputModifiers.Alt, "e", PhysicalKey.E);
            Assert.True(Title(window, "_Edit").IsSubMenuOpen);
            Press(window, Key.Escape);
            Press(window, Key.Escape);
            box.Focus();
            Pump(window);

            // F10 selects the first title, and closes the bar again.
            Press(window, Key.F10);
            Assert.True(window.MenuRow.IsOpen);
            Assert.True(Title(window, "_File").IsSelected);
            Press(window, Key.F10);
            Assert.False(window.MenuRow.IsOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Gestures_AreWindowShortcuts_InEitherLayout_WhileTheMenuIsNotExported()
    {
        var menu = FourMenus();
        var nativeNew = menu.Items[0].As<NativeMenuItem>().Menu!.Items[0].As<NativeMenuItem>();
        var clicks = 0;
        nativeNew.Click += (_, _) => clicks++;
        var window = Show(menu);
        try
        {
            var box = (TextBox)window.Content!;
            box.Focus();
            Pump(window);
            Press(window, Key.N, RawInputModifiers.Control, "n");
            Assert.Equal(1, clicks);

            // A disabled item's gesture does nothing.
            nativeNew.IsEnabled = false;
            Press(window, Key.N, RawInputModifiers.Control, "n");
            Assert.Equal(1, clicks);
            nativeNew.IsEnabled = true;

            // In the macOS layout with no system menu bar (headless, as on Windows or Linux) the row is gone, but the
            // shortcuts stay; Alt, access keys and F10 open nothing.
            window.LayoutMode = WindowLayoutMode.MacOS;
            Pump(window);
            Assert.False(NativeMenu.GetIsNativeMenuExported(window));
            Assert.Contains(nativeNew.Gesture, window.MenuShortcuts.Gestures);
            Press(window, Key.N, RawInputModifiers.Control, "n");
            Assert.Equal(2, clicks);
            Press(window, Key.LeftAlt, RawInputModifiers.Alt, physical: PhysicalKey.AltLeft);
            Assert.False(window.MenuRow.IsOpen);
            Press(window, Key.F, RawInputModifiers.Alt, "f", PhysicalKey.F);
            Assert.False(window.MenuRow.IsOpen);
            Assert.All(window.MenuRow.Titles, t => Assert.False(t.IsSubMenuOpen));
            Press(window, Key.F10);
            Assert.False(window.MenuRow.IsOpen);

            // Shortcuts follow gestures that change, and leave with the menu.
            nativeNew.Gesture = KeyGesture.Parse("Ctrl+M");
            Pump(window);
            Press(window, Key.M, RawInputModifiers.Control, "m");
            Assert.Equal(3, clicks);
            NativeMenu.SetMenu(window, null!);
            Pump(window);
            Assert.Empty(window.MenuShortcuts.Gestures);
            Press(window, Key.M, RawInputModifiers.Control, "m");
            Assert.Equal(3, clicks);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FocusedControls_HandleTheirKeysFirst_UnusedGesturesReachTheMenu()
    {
        var command = Application.Current!.PlatformSettings!.HotkeyConfiguration.CommandModifiers;
        var clicks = new Dictionary<string, int>();
        NativeMenuItem Counted(string header, Key key)
        {
            var item = new NativeMenuItem(header) { Gesture = new KeyGesture(key, command) };
            item.Click += (_, _) => clicks[header] = clicks.GetValueOrDefault(header) + 1;
            return item;
        }
        var menu = new NativeMenu
        {
            new NativeMenuItem("_File") { Menu = new NativeMenu { Counted("_New", Key.N) } },
            new NativeMenuItem("_Edit") { Menu = new NativeMenu { Counted("_Copy", Key.C), Counted("Select _all", Key.A) } },
        };
        var box = new TextBox { Text = "Some text" };
        var list = new ListBox { SelectionMode = SelectionMode.Multiple, ItemsSource = new[] { "One", "Two", "Three" } };
        var window = Show(menu);
        window.Content = new StackPanel { Children = { box, list } };
        var modifiers = (RawInputModifiers)command;
        try
        {
            Pump(window);

            // A text box keeps select all and copy, though the menu's entries are enabled.
            box.Focus();
            Pump(window);
            Press(window, Key.A, modifiers, "a");
            Assert.Equal(box.Text!.Length, box.SelectionEnd - box.SelectionStart);
            Press(window, Key.C, modifiers, "c");
            Assert.Equal(0, clicks.GetValueOrDefault("Select _all"));
            Assert.Equal(0, clicks.GetValueOrDefault("_Copy"));

            // A list keeps select all.
            list.SelectedIndex = 0;
            list.ContainerFromIndex(0)!.Focus();
            Pump(window);
            Press(window, Key.A, modifiers, "a");
            Assert.Equal(3, list.SelectedItems!.Count);
            Assert.Equal(0, clicks.GetValueOrDefault("Select _all"));

            // A gesture no control uses reaches the menu, once, from either control.
            Press(window, Key.N, modifiers, "n");
            Assert.Equal(1, clicks.GetValueOrDefault("_New"));
            box.Focus();
            Pump(window);
            Press(window, Key.N, modifiers, "n");
            Assert.Equal(2, clicks.GetValueOrDefault("_New"));
        }
        finally
        {
            window.Close();
        }
    }

    // --- Drag ---------------------------------------------------------------------------------

    [AvaloniaFact]
    public void Row_IsTitleBar_TitlesAreInteractive_EmptyAreaDragsAndDoubleClickMaximises()
    {
        var window = Show(FourMenus());
        try
        {
            var row = Part<Panel>(window, StandardsWindowTheme.MenuRowPart);
            Assert.Equal(WindowDecorationsElementRole.TitleBar, WindowDecorationProperties.GetElementRole(row));
            foreach (var title in window.MenuRow.Titles.Append(window.MenuRow.MoreItem))
                Assert.Equal(WindowDecorationsElementRole.DecorationsElement, WindowDecorationProperties.GetElementRole(title));

            // A title never acts on the window.
            var file = Title(window, "_File");
            DoubleClick(window, file.TranslatePoint(new Point(file.Bounds.Width / 2, file.Bounds.Height / 2), window)!.Value);
            Assert.Equal(WindowState.Normal, window.WindowState);
            Press(window, Key.Escape);
            Press(window, Key.Escape);
            Wait(window);

            // The empty area right of the menus acts as the title band: a double-click maximises.
            var empty = row.TranslatePoint(new Point(row.Bounds.Width - 30, row.Bounds.Height / 2), window)!.Value;
            DoubleClick(window, empty);
            Assert.Equal(WindowState.Maximized, window.WindowState);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Renders ------------------------------------------------------------------------------

    /// <summary>Eight menus at the minimum width: the titles that do not fit fold into "…", shown open (screenshots/menu-row-fold-light.png).</summary>
    [AvaloniaFact]
    public void Fold_Renders()
    {
        var window = Show(EightMenus(), width: 528);
        window.Background = new SolidColorBrush(Color.Parse("#C9CDD3"));
        window.SetRenderScaling(Sheet.Scale);
        try
        {
            Pump(window);
            var more = window.MenuRow.MoreItem;
            more.Focus();
            more.IsSubMenuOpen = true;
            Pump(window);
            using var frame = Frame.Capture(window);
            using var crop = Frame.Crop(frame.Bitmap, new PixelRect(0, 0, frame.Width, (int)(300 * Sheet.Scale)));
            var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(dir);
            using var stream = File.Create(Path.Combine(dir, "menu-row-fold-light.png"));
            crop.Save(stream, new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// The catalog's View menu off macOS, where its full screen item is hidden: the separator before it shows no orphan
    /// (screenshots/menu-separators-view-light.png).
    /// </summary>
    [AvaloniaFact]
    public void HiddenLastItem_LeavesNoOrphanSeparator_Renders()
    {
        var menu = new NativeMenu
        {
            new NativeMenuItem("_View")
            {
                Menu = new NativeMenu
                {
                    new NativeMenuItem("_Theme") { Menu = new NativeMenu { Item("_Light") } },
                    new NativeMenuItem("_Density") { Menu = new NativeMenu { Item("_Normal") } },
                    new NativeMenuItemSeparator(),
                    Item("_Next page", "Ctrl+PageDown"),
                    Item("_Previous page", "Ctrl+PageUp"),
                    new NativeMenuItemSeparator(),
                    new NativeMenuItem("Enter _Full Screen") { IsVisible = false },
                },
            },
        };
        var window = Show(menu, width: 528);
        window.Background = new SolidColorBrush(Color.Parse("#C9CDD3"));
        window.SetRenderScaling(Sheet.Scale);
        try
        {
            Pump(window);
            var view = Title(window, "_View");
            view.Focus();
            view.IsSubMenuOpen = true;
            Pump(window);
            using var frame = Frame.Capture(window);
            using var crop = Frame.Crop(frame.Bitmap, new PixelRect(0, 0, (int)(400 * Sheet.Scale), (int)(260 * Sheet.Scale)));
            var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(dir);
            using var stream = File.Create(Path.Combine(dir, "menu-separators-view-light.png"));
            crop.Save(stream, new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
            Assert.Equal(new object?[] { "_Theme", "_Density", "-", "_Next page", "_Previous page" }, Shown(view));
        }
        finally
        {
            window.Close();
        }
    }

    // --- Alt activates the row only for a clean press and release ------------------------------

    private static void AltDown(Window window) =>
        window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);

    private static void AltUp(Window window)
    {
        window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
        Pump(window);
    }

    // Runs the input between Alt down and Alt up and returns whether the menu row took focus.
    private static bool AltActivates(Action<StandardsWindow, Point> between, WindowLayoutMode layout = WindowLayoutMode.Windows)
    {
        var window = Show(FourMenus(), layout: layout);
        try
        {
            var box = (TextBox)window.Content!;
            box.Focus();
            Pump(window);
            var point = box.TranslatePoint(new Point(40, 20), window)!.Value;
            AltDown(window);
            between(window, point);
            AltUp(window);
            return window.MenuRow.IsOpen || window.MenuRow.Titles.Any(t => t.IsSelected || t.IsKeyboardFocusWithin);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Alt_PlainPressAndReleaseActivatesTheRow() =>
        Assert.True(AltActivates((_, _) => { }));

    [AvaloniaFact]
    public void Alt_WithPointerPressDragAndReleaseDoesNotActivate()
    {
        Assert.False(AltActivates((w, p) =>
        {
            w.MouseDown(p, MouseButton.Left, RawInputModifiers.Alt);
            w.MouseUp(p, MouseButton.Left, RawInputModifiers.Alt);
        }));
        Assert.False(AltActivates((w, p) =>
        {
            w.MouseDown(p, MouseButton.Left, RawInputModifiers.Alt | RawInputModifiers.LeftMouseButton);
            w.MouseMove(p + new Point(30, 10), RawInputModifiers.Alt | RawInputModifiers.LeftMouseButton);
            w.MouseUp(p + new Point(30, 10), MouseButton.Left, RawInputModifiers.Alt);
        }));
    }

    [AvaloniaFact]
    public void Alt_WithAKeyDoesNotActivate() =>
        Assert.False(AltActivates((w, _) => Press(w, Key.A, RawInputModifiers.Alt, "a", PhysicalKey.A)));

    [AvaloniaFact]
    public void Alt_WithAWheelTurnDoesNotActivate() =>
        Assert.False(AltActivates((w, p) => w.MouseWheel(p, new Vector(0, -1), RawInputModifiers.Alt)));

    [AvaloniaFact]
    public void Alt_WithAPlainPointerMoveStillActivates() =>
        Assert.True(AltActivates((w, p) => w.MouseMove(p + new Point(5, 5), RawInputModifiers.Alt)));

    [AvaloniaFact]
    public void Alt_InTheMacOSLayoutOpensNothing() =>
        Assert.False(AltActivates((_, _) => { }, WindowLayoutMode.MacOS));

    [AvaloniaFact]
    public void Alt_AfterADirtyReleaseTheNextCleanOneActivates()
    {
        var window = Show(FourMenus());
        try
        {
            var box = (TextBox)window.Content!;
            box.Focus();
            Pump(window);
            var point = box.TranslatePoint(new Point(40, 20), window)!.Value;
            foreach (var dirty in new Action[]
            {
                () => Press(window, Key.A, RawInputModifiers.Alt, "a", PhysicalKey.A),
                () => window.MouseWheel(point, new Vector(0, -1), RawInputModifiers.Alt),
                () => { window.MouseDown(point, MouseButton.Left, RawInputModifiers.Alt); window.MouseUp(point, MouseButton.Left, RawInputModifiers.Alt); },
            })
            {
                AltDown(window);
                dirty();
                AltUp(window);
                Assert.False(window.MenuRow.IsOpen);
                Assert.False(Title(window, "_File").IsSelected);
                AltDown(window);
                AltUp(window);
                Assert.True(Title(window, "_File").IsSelected);
                Press(window, Key.Escape);
                box.Focus();
                Pump(window);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Alt_CleanTapAfterTheWindowWasDeactivatedActivates()
    {
        var window = Show(FourMenus());
        try
        {
            var box = (TextBox)window.Content!;
            box.Focus();
            Pump(window);
            AltDown(window);
            window.MouseWheel(box.TranslatePoint(new Point(40, 20), window)!.Value, new Vector(0, -1), RawInputModifiers.Alt);
            // The platform reports the window deactivated, then active again.
            // (These callbacks are not public API, so they are reached by reflection.)
            var impl = window.PlatformImpl!;
            Action? Callback(string name) => (Action?)typeof(Avalonia.Platform.IWindowBaseImpl)
                .GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(impl);
            Callback("Deactivated")?.Invoke();
            Pump(window);
            Callback("Activated")?.Invoke();
            Pump(window);
            box.Focus();
            Pump(window);
            AltDown(window);
            AltUp(window);
            Assert.True(Title(window, "_File").IsSelected);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Helpers ------------------------------------------------------------------------------

    private static void DoubleClick(Window window, Point point)
    {
        for (var i = 0; i < 2; i++)
        {
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
        }
        Pump(window);
    }

    private static void Wait(Window window) =>
        Thread.Sleep(window.GetPlatformSettings()!.GetDoubleTapTime(PointerType.Mouse) + TimeSpan.FromMilliseconds(100));

    internal static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None, string? symbol = null,
        PhysicalKey physical = PhysicalKey.None)
    {
        window.KeyPress(key, modifiers, physical, symbol);
        window.KeyRelease(key, modifiers & ~RawInputModifiers.Alt, physical, symbol);
        Pump(window);
    }

    internal static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    internal static void Pump(Window window)
    {
        for (var i = 0; i < 4; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
        Dispatcher.UIThread.RunJobs();
    }
}

internal static class NativeMenuTestExtensions
{
    public static T As<T>(this NativeMenuItemBase item) where T : NativeMenuItemBase => (T)item;
}
