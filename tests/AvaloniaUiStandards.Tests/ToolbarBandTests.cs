using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
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

/// <summary>The toolbar band: zones, spacing, overflow and its menu, keyboard, automation, density and the window slot.</summary>
public class ToolbarBandTests
{
    // --- Layout and overflow ----------------------------------------------------------------

    [AvaloniaFact]
    public void Zones_PlaceItems_WithTheSpacingAndPadding()
    {
        var (a, b, c, d, e) = (Icon("A"), Icon("B"), Icon("C"), Icon("D"), Icon("E"));
        ToolbarBand.SetBeginsGroup(b, true);
        ToolbarBand.SetZone(c, ToolbarZone.Center);
        ToolbarBand.SetZone(d, ToolbarZone.Trailing);
        ToolbarBand.SetZone(e, ToolbarZone.Trailing);
        var (window, band) = Show(600, a, b, c, d, e);
        try
        {
            Assert.Equal(48, band.Bounds.Height);
            Assert.Equal(16, X(band, a));
            Assert.Equal(16 + 32 + 16, X(band, b));
            Assert.Equal((600 - 32) / 2.0, X(band, c));
            Assert.Equal(600 - 16 - 32, X(band, e));
            Assert.Equal(600 - 16 - 32 - 8 - 32, X(band, d));
            // Icon buttons are square and vertically centred: 8 px above and below.
            Assert.Equal(new Size(32, 32), a.Bounds.Size);
            Assert.Equal(8, a.TranslatePoint(default, band)!.Value.Y);
            Assert.False(band.MoreButton.IsEffectivelyVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Spacers_TakeTheFreeRoomOnTheirSide_AndSeparatorsSitBetweenItems()
    {
        var (a, b, c, d) = (Icon("A"), Icon("B"), Icon("C"), Icon("D"));
        var spacer = new ToolbarSpacer();
        var separator = new Separator();
        ToolbarBand.SetZone(d, ToolbarZone.Trailing);
        var (window, band) = Show(600, a, separator, b, spacer, c, d);
        try
        {
            Assert.Equal(16 + 32 + 8, X(band, separator));
            Assert.Equal(new Size(1, 20), separator.Bounds.Size);
            Assert.Equal(16 + 32 + 8 + 1 + 8, X(band, b));
            // The spacer pushes C to the free room's end, a zone gap before the trailing item.
            Assert.Equal(600 - 16 - 32 - 16 - 32, X(band, c));
            Assert.Equal(600 - 16 - 32, X(band, d));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Overflow_GoesByPriority_ThenFromTheTrailingEnd()
    {
        // Leading A B, centre C D, trailing E F; D and A have higher priorities, F never overflows.
        var items = "ABCDEF".Select(n => Icon(n.ToString())).ToArray();
        ToolbarBand.SetZone(items[2], ToolbarZone.Center);
        ToolbarBand.SetZone(items[3], ToolbarZone.Center);
        ToolbarBand.SetZone(items[4], ToolbarZone.Trailing);
        ToolbarBand.SetZone(items[5], ToolbarZone.Trailing);
        ToolbarBand.SetPriority(items[3], 2);
        ToolbarBand.SetPriority(items[0], 1);
        ToolbarBand.SetCanOverflow(items[5], false);
        var (window, band) = Show(400, items);
        try
        {
            // Natural width: padding 16 + 16, three zones of two 32 px items 8 apart, 16 between zones.
            Assert.Equal(280, Natural(band));
            // At every width the items in the menu are the first ones of this order.
            string[] order = ["D", "A", "E", "C", "B"];
            var counts = new HashSet<int>();
            for (var width = 300.0; width >= 100; width -= 2)
            {
                band.Width = width;
                Pump(window);
                var overflowed = items.Where(band.IsOverflowed).Select(Name).ToHashSet();
                Assert.True(overflowed.SetEquals(order.Take(overflowed.Count)), $"{width}: {string.Join(",", overflowed)}");
                counts.Add(overflowed.Count);
                // What shows always fits.
                Assert.True(items.Where(i => !band.IsOverflowed(i)).All(i => i.Bounds.Right <= width), $"{width}");
            }
            Assert.Superset(new HashSet<int> { 0, 2, 3, 4, 5 }, counts);
            Assert.False(band.IsOverflowed(items[5]));

            // Exactly at the natural width nothing overflows; 1 px less moves D and then A (the
            // "…" button needs D's room and more).
            band.Width = 280;
            Pump(window);
            Assert.DoesNotContain(items, band.IsOverflowed);
            Assert.False(band.MoreButton.IsEffectivelyVisible);
            band.Width = 279;
            Pump(window);
            Assert.Equal(["A", "D"], items.Where(band.IsOverflowed).Select(Name));
            Assert.True(band.MoreButton.IsEffectivelyVisible);
            Assert.Equal(279 - 16 - 32, band.MoreButton.TranslatePoint(default, band)!.Value.X);

            // Widening brings them back.
            band.Width = 400;
            Pump(window);
            Assert.DoesNotContain(items, band.IsOverflowed);
            Assert.All(items, i => Assert.True(i.IsEffectivelyVisible));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Separators_HideWhenTheirNeighboursOverflow_AndHiddenItemsAreLeftOut()
    {
        var (a, b, c) = (Icon("A"), Icon("B"), Icon("C"));
        var separator = new Separator();
        ToolbarBand.SetPriority(c, 1);
        var (window, band) = Show(600, a, separator, b, c);
        try
        {
            Assert.True(separator.IsEffectivelyVisible);
            b.IsVisible = false;
            Pump(window);
            // C is still after the separator.
            Assert.True(separator.IsEffectivelyVisible);
            Assert.Equal(16 + 32 + 8 + 1 + 8, X(band, c));
            c.IsVisible = false;
            Pump(window);
            Assert.False(separator.IsEffectivelyVisible);
            Assert.Equal(16 + 32 + 16, Natural(band));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Layout_SettlesWithoutLoops()
    {
        var items = Enumerable.Range(0, 8).Select(i => Icon($"I{i}")).ToArray();
        var band = new CountingBand();
        foreach (var item in items)
            band.Items.Add(item);
        var window = Host(band, 400);
        try
        {
            foreach (var width in new[] { 380.0, 200, 120, 330, 90, 400 })
            {
                band.Width = width;
                band.Measures = 0;
                for (var i = 0; i < 5; i++)
                    Pump(window);
                // One pass for the new width, one after the shown items change; then nothing.
                Assert.InRange(band.Measures, 1, 3);
                band.Measures = 0;
                Pump(window);
                Assert.Equal(0, band.Measures);
            }
        }
        finally
        {
            window.Close();
        }
    }

    // --- Overflow menu ----------------------------------------------------------------------

    [AvaloniaFact]
    public void Menu_MapsButtonsTogglesSeparatorsAndSpacers()
    {
        var clicks = 0;
        var button = Icon("Search", Icons.MagnifyingGlass);
        button.Click += (_, _) => clicks++;
        var labelled = Icon("Add", Icons.Plus);
        ToolbarBand.SetLabel(labelled, "New item");
        var toggle = new ToggleButton { Content = new Icon { Data = Icons.Eye }, IsChecked = true };
        toggle.Classes.Add("subtle");
        ToolTip.SetTip(toggle, "Preview");
        var (window, band) = Show(600, button, new Separator(), labelled, new ToolbarSpacer(), toggle);
        try
        {
            band.Width = 60;
            Pump(window);
            var entries = OpenMenu(window, band);
            Assert.Collection(entries,
                e => AssertItem(e, "Search", Icons.MagnifyingGlass),
                e => Assert.IsType<Separator>(e),
                e => AssertItem(e, "New item", Icons.Plus),
                e =>
                {
                    AssertItem(e, "Preview", Icons.Eye);
                    Assert.Equal(MenuItemToggleType.CheckBox, ((MenuItem)e).ToggleType);
                    Assert.True(((MenuItem)e).IsChecked);
                });

            // Choosing a button clicks it; choosing the toggle toggles it, and the check follows.
            Click(window, (MenuItem)entries[0]);
            Assert.Equal(1, clicks);
            entries = OpenMenu(window, band);
            Click(window, (MenuItem)entries[3]);
            Assert.False(toggle.IsChecked);
            entries = OpenMenu(window, band);
            Assert.False(((MenuItem)entries[3]).IsChecked);
            toggle.IsChecked = true;
            Assert.True(((MenuItem)entries[3]).IsChecked);
            band.OverflowMenu.Hide();

            // A disabled item is disabled in the menu.
            button.IsEnabled = false;
            entries = OpenMenu(window, band);
            Assert.False(((MenuItem)entries[0]).IsEnabled);
            band.OverflowMenu.Hide();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Menu_ShowsASegmentedControlAsARadioGroup_SelectionBothWays()
    {
        var segmented = Segmented("Theme", "System", "Light", "Dark");
        segmented.SelectedIndex = 1;
        var before = Icon("Search", Icons.MagnifyingGlass);
        var after = Icon("Add", Icons.Plus);
        var (window, band) = Show(600, before, segmented, after);
        try
        {
            band.Width = 60;
            Pump(window);
            var entries = OpenMenu(window, band);
            Assert.Equal(8, entries.Count);
            AssertItem(entries[0], "Search", Icons.MagnifyingGlass);
            Assert.IsType<Separator>(entries[1]);
            var header = Assert.IsType<MenuItem>(entries[2]);
            Assert.False(header.IsEnabled);
            Assert.Equal("Theme", Assert.IsType<TextBlock>(header.Header).Text);
            var radios = entries.Skip(3).Take(3).Cast<MenuItem>().ToList();
            Assert.Equal(["System", "Light", "Dark"], radios.Select(r => r.Header));
            Assert.All(radios, r => Assert.Equal(MenuItemToggleType.Radio, r.ToggleType));
            Assert.Equal([false, true, false], radios.Select(r => r.IsChecked));
            Assert.IsType<Separator>(entries[6]);
            AssertItem(entries[7], "Add", Icons.Plus);
            // Keyboard focus skips the header.
            Assert.False(header.Focusable && header.IsEnabled);

            // The control's selection shows in the menu while it is open ...
            segmented.SelectedIndex = 2;
            Assert.Equal([false, false, true], radios.Select(r => r.IsChecked));
            // ... and choosing a radio item selects its segment.
            Click(window, radios[0]);
            Assert.Equal(0, segmented.SelectedIndex);
            entries = OpenMenu(window, band);
            Assert.Equal([true, false, false], entries.Skip(3).Take(3).Cast<MenuItem>().Select(r => r.IsChecked));
            band.OverflowMenu.Hide();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Menu_ShowsADropDownsMenuAsASubmenu()
    {
        var chosen = new List<string>();
        var sort = new MenuItem { Header = "Sort" };
        sort.Click += (_, _) => chosen.Add("Sort");
        var grid = new MenuItem { Header = "Grid", ToggleType = MenuItemToggleType.CheckBox };
        var dropDown = new DropDownButton
        {
            Content = new Icon { Data = Icons.SlidersHorizontal },
            Flyout = new MenuFlyout { Items = { sort, new Separator(), grid } },
        };
        dropDown.Classes.Add("subtle");
        ToolTip.SetTip(dropDown, "View");
        var (window, band) = Show(600, dropDown);
        try
        {
            // An icon drop-down: icon, SpacingXS, chevron, at the button height.
            Assert.Equal(new Size(48, 32), dropDown.Bounds.Size);
            band.Width = 40;
            Pump(window);
            var entries = OpenMenu(window, band);
            var view = Assert.IsType<MenuItem>(Assert.Single(entries));
            AssertItem(view, "View", Icons.SlidersHorizontal);
            Assert.Collection(view.Items,
                i => Assert.Equal("Sort", ((MenuItem)i!).Header),
                i => Assert.IsType<Separator>(i),
                i => Assert.Equal(MenuItemToggleType.CheckBox, ((MenuItem)i!).ToggleType));
            ((MenuItem)view.Items[0]!).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(["Sort"], chosen);
            band.OverflowMenu.Hide();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OtherControls_StayInTheBand()
    {
        var box = new TextBox { Width = 120 };
        var button = Icon("A");
        var (window, band) = Show(600, box, button);
        try
        {
            band.Width = 100;
            Pump(window);
            Assert.False(band.IsOverflowed(box));
            Assert.True(band.IsOverflowed(button));
            Assert.True(box.IsEffectivelyVisible);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Keyboard ---------------------------------------------------------------------------

    [AvaloniaFact]
    public void Tab_VisitsItemsInVisualOrder_SkippingOverflowedOnes()
    {
        // Declared out of zone order: the trailing item first.
        var trailing = Icon("T");
        ToolbarBand.SetZone(trailing, ToolbarZone.Trailing);
        var segmented = Segmented("Theme", "System", "Light", "Dark");
        ToolbarBand.SetZone(segmented, ToolbarZone.Center);
        // B is a text button, wider than the "…" button that replaces it.
        var (a, b) = (Icon("A"), new Button { Content = "Bold" });
        b.Classes.Add("subtle");
        ToolbarBand.SetPriority(b, 1);
        var (window, band) = Show(700, trailing, segmented, a, b);
        try
        {
            a.Focus(NavigationMethod.Tab);
            var visited = new List<object?> { Focused(window) };
            for (var i = 0; i < 3; i++)
            {
                Press(window, Key.Tab);
                visited.Add(Focused(window));
            }
            // Arrows stay inside the segmented control (one Tab stop for it).
            Assert.Equal([a, b, segmented.ContainerFromIndex(0), trailing], visited);

            // B overflows: it takes no focus, and Tab goes from A straight to the segmented control.
            band.Width = Natural(band) - 1;
            Pump(window);
            Assert.True(band.IsOverflowed(b));
            Assert.False(b.Focus());
            a.Focus(NavigationMethod.Tab);
            Press(window, Key.Tab);
            Assert.Same(segmented.ContainerFromIndex(0), Focused(window));
            Press(window, Key.Tab);
            Press(window, Key.Tab);
            Assert.Same(band.MoreButton, Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FocusOnAnItemThatOverflows_MovesToTheMoreButton()
    {
        var (a, b) = (Icon("A"), Icon("B"));
        var (window, band) = Show(400, a, b);
        try
        {
            b.Focus(NavigationMethod.Tab);
            band.Width = Natural(band) - 1;
            Pump(window);
            Assert.True(band.IsOverflowed(b));
            Assert.Same(band.MoreButton, Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(Key.Enter)]
    [InlineData(Key.Space)]
    [InlineData(Key.Down)]
    public void MoreButton_OpensWithEnterSpaceOrDown(Key key)
    {
        var (window, band) = Show(400, Icon("A"), Icon("B"));
        try
        {
            band.Width = 60;
            Pump(window);
            band.MoreButton.Focus(NavigationMethod.Tab);
            Press(window, key);
            Assert.True(band.OverflowMenu.IsOpen);
            band.OverflowMenu.Hide();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MoreButton_IsASquareSubtleIconButton_WithATooltip()
    {
        var (window, band) = Show(400, Icon("A"), Icon("B"));
        try
        {
            band.Width = 60;
            Pump(window);
            var more = band.MoreButton;
            Assert.Contains("subtle", more.Classes);
            Assert.Equal(new Size(32, 32), more.Bounds.Size);
            Assert.Same(Icons.DotsThree, Assert.IsType<Icon>(more.Content).Data);
            Assert.Equal(ToolbarBand.MoreLabel, ToolTip.GetTip(more));
            Assert.Equal(ToolbarBand.MoreLabel, AutomationProperties.GetName(more));
            Assert.Equal(PlacementMode.BottomEdgeAlignedRight, band.OverflowMenu.Placement);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Density and automation -------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData(DensityStyle.Normal, 48, 32, 20)]
    [InlineData(DensityStyle.Compact, 40, 24, 16)]
    public void Density_SetsTheBandItemAndSeparatorSizes(DensityStyle density, double height, double button, double icon)
    {
        var a = Icon("A");
        var separator = new Separator();
        var (window, band) = Show(400, [a, separator, Icon("B")], density);
        try
        {
            Assert.Equal(height, band.Bounds.Height);
            Assert.Equal(new Size(button, button), a.Bounds.Size);
            Assert.Equal(new Size(icon, icon), ((Control)a.Content!).Bounds.Size);
            Assert.Equal(icon, separator.Bounds.Height);
            Assert.Equal((height - button) / 2, a.TranslatePoint(default, band)!.Value.Y);
            Assert.Equal(16, X(band, a));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Automation_ReportsAToolbar_WithAName()
    {
        var (window, band) = Show(400, Icon("A"));
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(band);
            Assert.Equal(AutomationControlType.ToolBar, peer.GetAutomationControlType());
            Assert.Equal("Toolbar", peer.GetName());
            Assert.True(peer.IsControlElement());
            AutomationProperties.SetName(band, "Formatting");
            Assert.Equal("Formatting", peer.GetName());
        }
        finally
        {
            window.Close();
        }
    }

    // --- Window slot ------------------------------------------------------------------------

    [AvaloniaFact]
    public void WindowSlot_ShowsOnlyWhenSet_AndMovesTheContentPanelDown()
    {
        var window = new StandardsWindow { Width = 700, Height = 400, Content = new TextBlock { Text = "Content" } };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced));
        try
        {
            window.Show();
            Pump(window);
            var slot = Part<ContentPresenter>(window, StandardsWindowTheme.ToolbarBandPart);
            var panel = Part<Control>(window, StandardsWindowTheme.ContentPanelPart);
            Assert.False(slot.IsVisible);
            var top = panel.TranslatePoint(default, window)!.Value.Y;

            var band = new ToolbarBand { Items = { Icon("A") } };
            window.ToolbarBand = band;
            Pump(window);
            Assert.True(slot.IsVisible);
            Assert.Equal(48, band.Bounds.Height);
            var title = Part<Control>(window, StandardsWindowTheme.TitleBandPart);
            Assert.Equal(title.TranslatePoint(new Point(0, title.Bounds.Height), window)!.Value.Y, band.TranslatePoint(default, window)!.Value.Y);
            Assert.Equal(top + 48, panel.TranslatePoint(default, window)!.Value.Y);
            Assert.Equal(Part<Control>(window, StandardsWindowTheme.FramePart).Bounds.Width - 2, band.Bounds.Width, 1);

            window.ToolbarBand = null;
            Pump(window);
            Assert.False(slot.IsVisible);
            Assert.Equal(top, panel.TranslatePoint(default, window)!.Value.Y);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void WindowSlot_EmptyAreaDragsOnlyOnMacOS_AndItemsNever(bool macOS)
    {
        var button = Icon("A");
        var separator = new Separator();
        var band = new ToolbarBand { Items = { button, separator, Icon("B") } };
        var window = new StandardsWindow(nativeFrame: false)
        {
            Width = 700, Height = 400, ToolbarBand = band, Content = new TextBlock { Text = "Content" },
            WindowButtonStyle = WindowButtonStyle.CaptionButtons,
        };
        window.ToolbarBandDragsWindow = macOS;
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced));
        try
        {
            window.Show();
            window.Activate();
            Pump(window);
            var empty = band.TranslatePoint(new Point(band.Bounds.Width / 2, band.Bounds.Height / 2), window)!.Value;
            DoubleClick(window, empty);
            Assert.Equal(macOS ? WindowState.Maximized : WindowState.Normal, window.WindowState);
            window.WindowState = WindowState.Normal;
            Pump(window);
            Wait(window);

            // A double-click on an item or on a separator acts on neither the window nor the band.
            foreach (var target in new Control[] { button, separator })
            {
                var point = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window)!.Value;
                DoubleClick(window, point);
                Assert.Equal(WindowState.Normal, window.WindowState);
                Wait(window);
            }
        }
        finally
        {
            window.Close();
        }
    }

    // --- Renders ----------------------------------------------------------------------------

    [AvaloniaFact]
    public void Band_Renders()
    {
        string[] columns = ["Wide (960)", "Medium (720)", "Narrow (480)", "Narrow, menu open"];
        using var sheet = new Sheet("Toolbar band: leading, centre and trailing zones; items overflow into the trailing \"…\" menu as the window narrows");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, columns,
            [
                ("Normal", [
                    BandCell(sheet, theme, 960, DensityStyle.Normal),
                    BandCell(sheet, theme, 720, DensityStyle.Normal),
                    BandCell(sheet, theme, 480, DensityStyle.Normal),
                    BandCell(sheet, theme, 480, DensityStyle.Normal, openMenu: true),
                ]),
                ("Compact density", [
                    BandCell(sheet, theme, 960, DensityStyle.Compact),
                    BandCell(sheet, theme, 720, DensityStyle.Compact),
                    BandCell(sheet, theme, 480, DensityStyle.Compact),
                    BandCell(sheet, theme, 480, DensityStyle.Compact, openMenu: true),
                ]),
            ]);
        sheet.Save("m3-toolbar-band.png");
    }

    private static Control BandCell(Sheet sheet, ThemeVariant theme, double width, DensityStyle density, bool openMenu = false)
    {
        var band = DemoBand();
        var content = new StackPanel { Spacing = 8 };
        content.Children.Add(new TextBlock { Text = "Document", FontSize = 28, FontWeight = FontWeight.SemiBold });
        content.Children.Add(new TextBlock { Text = "The glass content panel sits below the toolbar band.", TextWrapping = TextWrapping.Wrap });
        var window = new StandardsWindow(nativeFrame: false)
        {
            Title = "Toolbar band",
            Width = width + StandardsWindowTheme.ShadowMargin.Left + StandardsWindowTheme.ShadowMargin.Right,
            Height = (openMenu ? 400 : 200) + StandardsWindowTheme.ShadowMargin.Top + StandardsWindowTheme.ShadowMargin.Bottom,
            ToolbarBand = band,
            Content = content,
            WindowButtonStyle = WindowButtonStyle.TrafficLights,
            RequestedThemeVariant = theme,
            Background = new SolidColorBrush(Color.Parse(theme == ThemeVariant.Dark ? "#3A3D42" : "#C9CDD3")),
        };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true, Density = density });
        try
        {
            window.SetRenderScaling(Sheet.Scale);
            window.Show();
            window.Activate();
            Pump(window);
            if (openMenu)
            {
                band.MoreButton.Flyout!.ShowAt(band.MoreButton);
                Pump(window);
            }
            var frame = Part<Control>(window, StandardsWindowTheme.FramePart);
            var origin = frame.TranslatePoint(default, window)!.Value;
            using var capture = window.CaptureRenderedFrame()!;
            var crop = new PixelRect((int)(origin.X * Sheet.Scale), (int)(origin.Y * Sheet.Scale),
                (int)(frame.Bounds.Width * Sheet.Scale), (int)(frame.Bounds.Height * Sheet.Scale));
            return sheet.Image(capture, crop);
        }
        finally
        {
            band.OverflowMenu.Hide();
            window.Close();
        }
    }

    /// <summary>
    /// The demo band: a leading sidebar-style button and icon buttons, the theme and density
    /// segmented controls in the centre (they overflow first), and a trailing drop-down, toggle
    /// and a colour swatch that never overflows.
    /// </summary>
    internal static ToolbarBand DemoBand()
    {
        var sidebar = Icon("Show sidebar", Icons.SidebarSimple);
        ToolbarBand.SetPriority(sidebar, -1);
        var add = Icon("New", Icons.Plus);
        ToolbarBand.SetBeginsGroup(add, true);
        var search = Icon("Search", Icons.MagnifyingGlass);
        var theme = Segmented("Theme", "System", "Light", "Dark");
        ToolbarBand.SetZone(theme, ToolbarZone.Center);
        ToolbarBand.SetPriority(theme, 1);
        var density = Segmented("Density", "Normal", "Compact");
        ToolbarBand.SetZone(density, ToolbarZone.Center);
        ToolbarBand.SetPriority(density, 2);
        ToolbarBand.SetBeginsGroup(density, true);
        var view = new DropDownButton
        {
            Content = new Icon { Data = Icons.SlidersHorizontal },
            Flyout = new MenuFlyout { Items = { new MenuItem { Header = "Sort by name" }, new MenuItem { Header = "Sort by date" } } },
        };
        view.Classes.Add("subtle");
        ToolTip.SetTip(view, "View");
        ToolbarBand.SetZone(view, ToolbarZone.Trailing);
        var preview = new ToggleButton { Content = new Icon { Data = Icons.Eye } };
        preview.Classes.Add("subtle");
        ToolTip.SetTip(preview, "Preview");
        AutomationProperties.SetName(preview, "Preview");
        ToolbarBand.SetZone(preview, ToolbarZone.Trailing);
        var separator = new Separator();
        ToolbarBand.SetZone(separator, ToolbarZone.Trailing);
        var accent = new ColorPicker { Color = StandardsTheme.DefaultAccent };
        ToolTip.SetTip(accent, "Accent colour");
        AutomationProperties.SetName(accent, "Accent colour");
        ToolbarBand.SetZone(accent, ToolbarZone.Trailing);
        ToolbarBand.SetCanOverflow(accent, false);
        return new ToolbarBand { Items = { sidebar, add, search, theme, density, view, preview, separator, accent } };
    }

    // --- Helpers ----------------------------------------------------------------------------

    private sealed class CountingBand : ToolbarBand
    {
        public int Measures;

        protected override Size MeasureOverride(Size availableSize)
        {
            Measures++;
            return base.MeasureOverride(availableSize);
        }
    }

    internal static Button Icon(string name, Geometry? icon = null)
    {
        var button = new Button { Content = new Icon { Data = icon ?? Icons.Plus } };
        button.Classes.Add("subtle");
        ToolTip.SetTip(button, name);
        AutomationProperties.SetName(button, name);
        return button;
    }

    internal static SegmentedControl Segmented(string label, params string[] values)
    {
        var control = new SegmentedControl();
        foreach (var value in values)
            control.Items.Add(new SegmentedControlItem { Content = value });
        control.SelectedIndex = 0;
        ToolbarBand.SetLabel(control, label);
        AutomationProperties.SetName(control, label);
        return control;
    }

    private static string Name(Control item) => AutomationProperties.GetName(item) ?? "";

    private static double X(ToolbarBand band, Control item) => item.TranslatePoint(default, band)!.Value.X;

    private static double Natural(ToolbarBand band) => band.NaturalWidth;

    private static (Window Window, ToolbarBand Band) Show(double width, params Control[] items) => Show(width, items, DensityStyle.Normal);

    private static (Window Window, ToolbarBand Band) Show(double width, Control[] items, DensityStyle density)
    {
        var band = new ToolbarBand();
        foreach (var item in items)
            band.Items.Add(item);
        return (Host(band, width, density), band);
    }

    private static Window Host(ToolbarBand band, double width, DensityStyle density = DensityStyle.Normal)
    {
        band.Width = width;
        band.HorizontalAlignment = HorizontalAlignment.Left;
        band.VerticalAlignment = VerticalAlignment.Top;
        var window = new Window { Width = 1000, Height = 400, Content = band };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true, Density = density });
        window.Show();
        Pump(window);
        return window;
    }

    private static List<Control> OpenMenu(Window window, ToolbarBand band)
    {
        band.OverflowMenu.ShowAt(band.MoreButton);
        Pump(window);
        Assert.True(band.OverflowMenu.IsOpen);
        return band.MenuEntries.ToList();
    }

    private static void AssertItem(Control entry, string header, Geometry icon)
    {
        var item = Assert.IsType<MenuItem>(entry);
        Assert.Equal(header, item.Header);
        Assert.Same(icon, Assert.IsType<Icon>(item.Icon).Data);
    }

    // A click on a menu item with the pointer, as a user would.
    private static void Click(Window window, MenuItem item)
    {
        var point = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)!.Value;
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Pump(window);
    }

    private static void DoubleClick(Window window, Point point)
    {
        for (var i = 0; i < 2; i++)
        {
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
        }
        Pump(window);
    }

    // Clicks soon after count on (a third, fourth click), so the double-click time is waited out.
    private static void Wait(Window window) =>
        Thread.Sleep(window.GetPlatformSettings()!.GetDoubleTapTime(PointerType.Mouse) + TimeSpan.FromMilliseconds(100));

    private static IInputElement? Focused(Window window) => window.FocusManager!.GetFocusedElement();

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, null);
        window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Pump(window);
    }

    private static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static void Pump(Window window)
    {
        for (var i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
        Dispatcher.UIThread.RunJobs();
    }
}
