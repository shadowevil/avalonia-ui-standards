using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;

using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>The menu and flyout themes: glass overlays, list-item pills, columns, states, density and motion.</summary>
public class MenuThemeTests
{
    [AvaloniaFact]
    public void ContextMenu_IsAGlassOverlay_WithTheDropDownInset()
    {
        var (window, menu, target) = ShowContextMenu(ThemeVariant.Light);
        try
        {
            var surface = Surface(menu);
            Assert.Equal(new CornerRadius(16), surface.CornerRadius);
            Assert.Equal(new Thickness(4), surface.Padding);

            // Items are inset 4 from the glass edge (plus its 1 px edge) and are list-item pills.
            var item = Items(menu).First();
            var origin = item.TranslatePoint(default, surface)!.Value;
            Assert.Equal(new Point(5, 5), origin);
            Assert.Equal(new CornerRadius(8), item.CornerRadius);
            Assert.Equal(32, item.Bounds.Height);
            Assert.Equal(new Thickness(12, 4), item.Padding);
            Assert.Null(item.FocusAdorner);
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Compact_ShrinksItemsAndIcons()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = Avalonia.Themes.Fluent.DensityStyle.Compact };
        var (window, menu, _) = ShowContextMenu(ThemeVariant.Light, theme);
        try
        {
            var item = Items(menu).First();
            Assert.Equal(24, item.Bounds.Height);
            Assert.Equal(new Thickness(8, 2), item.Padding);
            Assert.Equal(16, Part<Viewbox>(item, MenuTheme.IconPart).Bounds.Width);
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    // Highlight (pointer or keyboard) and press use the list item's neutral fills (0024), not the accent.
    public static TheoryData<string, string, string?> ItemFills()
    {
        var data = new TheoryData<string, string, string?>();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            data.Add(theme, "rest", null);
            data.Add(theme, ":selected", "SystemListMediumColor");
            data.Add(theme, ":pressed", "SystemBaseMediumLowColor");
        }
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(ItemFills))]
    public void ItemFill_FollowsTheListItemDesign(string theme, string state, string? expectedKey)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, menu, _) = ShowContextMenu(variant);
        try
        {
            var item = Items(menu).First();
            if (state != "rest")
                ((IPseudoClasses)item.Classes).Set(state, true);
            window.UpdateLayout();
            var fill = Effective(Chrome(item).GetBaseValue(SquircleBorder.BackgroundProperty).Value);
            var expected = expectedKey is null ? Avalonia.Media.Colors.Transparent : Effective(window.FindResource(variant, expectedKey));
            Assert.Equal(expected.A == 0 ? 0 : expected.ToUInt32(), fill.A == 0 ? 0 : fill.ToUInt32());
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Columns_ShowTheMarksIconGestureAndCaret()
    {
        var (window, menu, _) = ShowContextMenu(ThemeVariant.Light);
        try
        {
            var items = Items(menu).ToList();
            var open = items[0];
            Assert.True(Part<Viewbox>(open, MenuTheme.IconPart).IsVisible);
            var gesture = Part<TextBlock>(open, MenuTheme.GesturePart);
            Assert.Equal(new KeyGesture(Key.O, KeyModifiers.Control).ToString("p", null), gesture.Text);
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, "SystemBaseMediumColor")), Effective(gesture.Foreground));
            Assert.Equal(HorizontalAlignment.Right, gesture.HorizontalAlignment);
            Assert.False(Part<Icon>(open, MenuTheme.CaretPart).IsVisible);

            var sub = items[1];
            var caret = Part<Icon>(sub, MenuTheme.CaretPart);
            Assert.True(caret.IsVisible);
            Assert.Same(Icons.CaretRight, caret.Data);
            Assert.False(Part<Viewbox>(sub, MenuTheme.IconPart).IsVisible);

            var check = items[2];
            Assert.True(Part<Panel>(check, MenuTheme.ToggleMarkPart).IsVisible);
            Assert.True(Part<Icon>(check, MenuTheme.CheckMarkPart).IsVisible);
            Assert.Same(Icons.Check, Part<Icon>(check, MenuTheme.CheckMarkPart).Data);

            // An unchecked toggle keeps the mark column, so headers line up.
            var uncheck = items[3];
            Assert.True(Part<Panel>(uncheck, MenuTheme.ToggleMarkPart).IsVisible);
            Assert.False(Part<Icon>(uncheck, MenuTheme.CheckMarkPart).IsVisible);
            Assert.Equal(Header(check).TranslatePoint(default, menu)!.Value.X, Header(uncheck).TranslatePoint(default, menu)!.Value.X);

            var radio = items[4];
            Assert.True(Part<Ellipse>(radio, MenuTheme.RadioMarkPart).IsVisible);
            Assert.Equal(MenuTheme.RadioMarkSize, Part<Ellipse>(radio, MenuTheme.RadioMarkPart).Bounds.Width);

            // Every header starts at the same x: the shared columns.
            Assert.All(items.Where(i => i.Header is string { Length: > 1 }), i =>
                Assert.Equal(Header(open).TranslatePoint(default, menu)!.Value.X, Header(i).TranslatePoint(default, menu)!.Value.X));
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Disabled_DimsTheHeaderAndGesture()
    {
        var (window, menu, _) = ShowContextMenu(ThemeVariant.Light);
        try
        {
            var disabled = Items(menu).Single(i => !i.IsEnabled);
            var dim = Effective(window.FindResource(ThemeVariant.Light, "SystemBaseMediumLowColor"));
            Assert.Equal(dim, Effective(disabled.Foreground));
            Assert.Equal(dim, Effective(Part<TextBlock>(disabled, MenuTheme.GesturePart).Foreground));
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Submenu_OpensAsAGlassOverlay_LinedUpWithItsItem()
    {
        var (window, menu, _) = ShowContextMenu(ThemeVariant.Light);
        try
        {
            var sub = Items(menu).ElementAt(1);
            sub.Open();
            Overlays.Pump(window);
            var popup = Part<Popup>(sub, MenuTheme.PopupPart);
            Assert.True(popup.IsOpen);
            var surface = Assert.IsType<GlassBorder>(popup.Child);
            Assert.Equal(new CornerRadius(16), surface.CornerRadius);
            var inner = sub.GetLogicalChildren().OfType<MenuItem>().First();
            // The submenu's first item is level with its parent item.
            Assert.Equal(sub.TranslatePoint(default, window)!.Value.Y, inner.TranslatePoint(default, window)!.Value.Y, 0.5);
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SeparatorHeader_DrawsAnInsetSeparator()
    {
        var (window, menu, _) = ShowContextMenu(ThemeVariant.Light);
        try
        {
            var item = Items(menu).Single(i => i.Header is "-");
            var separator = Assert.IsType<Separator>(Assert.Single(item.GetVisualChildren()));
            Assert.Equal(SeparatorTheme.MenuMargin, separator.Margin);
            Assert.Equal(9, item.Bounds.Height);
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MenuBar_ItemsAreSubtleButtons_AndTheirMenusOpenBelow()
    {
        var file = new MenuItem { Header = "_File", ItemsSource = new[] { new MenuItem { Header = "New" } } };
        var menu = new Menu { ItemsSource = new[] { file, new MenuItem { Header = "Edit" } }, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(16) };
        var window = Overlays.Host(ThemeVariant.Light, menu, 300, 200);
        try
        {
            Assert.Equal(32, file.Bounds.Height);
            Assert.Equal(32, menu.Bounds.Height);
            Assert.Equal(new CornerRadius(8), file.CornerRadius);
            var chrome = Chrome(file);
            Assert.Equal(0, Effective(chrome.GetBaseValue(SquircleBorder.BackgroundProperty).Value).A);

            ((IPseudoClasses)file.Classes).Set(":selected", true);
            window.UpdateLayout();
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, "ButtonBackgroundPointerOver")),
                Effective(chrome.GetBaseValue(SquircleBorder.BackgroundProperty).Value));

            file.Open();
            Overlays.Pump(window);
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, "ButtonBackgroundPressed")),
                Effective(chrome.GetBaseValue(SquircleBorder.BackgroundProperty).Value));
            var item = file.GetLogicalChildren().OfType<MenuItem>().First();
            var itemPos = item.TranslatePoint(default, window)!.Value;
            var filePos = file.TranslatePoint(default, window)!.Value;
            // Below the bar with a 4 px gap (plus the overlay's padding and edge), the item text in line with the header.
            Assert.Equal(filePos.Y + 32 + 4 + 5, itemPos.Y, 0.5);
            Assert.Equal(filePos.X, itemPos.X, 0.5);
        }
        finally
        {
            file.Close();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Flyout_IsAGlassCard_PaddedBySpacingL()
    {
        var content = new TextBlock { Text = "Flyout" };
        var button = new Button { Content = "Open", Flyout = new Flyout { Content = content } };
        var window = Overlays.Host(ThemeVariant.Light, button, 300, 200);
        try
        {
            button.Flyout!.ShowAt(button);
            Overlays.Pump(window);
            var presenter = content.FindLogicalAncestorOfType<FlyoutPresenter>()!;
            var surface = Surface(presenter);
            Assert.Equal(new CornerRadius(16), surface.CornerRadius);
            Assert.Equal(new Point(17, 17), content.TranslatePoint(default, surface)!.Value);
        }
        finally
        {
            button.Flyout!.Hide();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MenuFlyout_IsAGlassOverlay()
    {
        var item = new MenuItem { Header = "One" };
        var button = new Button { Content = "Open", Flyout = new MenuFlyout { ItemsSource = new[] { item } } };
        var window = Overlays.Host(ThemeVariant.Light, button, 300, 200);
        try
        {
            button.Flyout!.ShowAt(button);
            Overlays.Pump(window);
            var presenter = item.FindLogicalAncestorOfType<MenuFlyoutPresenter>()!;
            var surface = Surface(presenter);
            Assert.Equal(new Thickness(4), surface.Padding);
            Assert.Equal(new CornerRadius(16), surface.CornerRadius);
        }
        finally
        {
            button.Flyout!.Hide();
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Overlays_FadeIn_UnlessMotionIsReduced(bool reduceMotion)
    {
        var (window, menu, _) = ShowContextMenu(ThemeVariant.Light, reduceMotion: reduceMotion);
        try
        {
            var surface = Surface(menu);
            var fade = surface.FindResource(OverlayParts.FadeInFastKey);
            if (reduceMotion)
            {
                Assert.Null(fade);
                Assert.Equal(1, surface.Opacity);
            }
            else
            {
                var animation = Assert.IsType<Animation>(fade);
                Assert.Equal(TimeSpan.FromMilliseconds(100), animation.Duration);
                Assert.Same(animation, surface.FindResource(OverlayParts.FadeInFastKey));
            }
            // Item fades are the fixed chrome transitions (none under reduce motion).
            var transitions = Chrome(Items(menu).First()).Transitions!;
            Assert.Equal(reduceMotion ? 0 : 3, transitions.Count);
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FadeIn_StartsTransparentAndEndsOpaque()
    {
        var animation = OverlayParts.FadeIn(TimeSpan.FromMilliseconds(100), new Avalonia.Animation.Easings.CubicEaseOut())!;
        Assert.Equal(2, animation.Children.Count);
        Assert.Equal(0d, ((Setter)animation.Children[0].Setters[0]).Value);
        Assert.Equal(1d, ((Setter)animation.Children[1].Setters[0]).Value);
        Assert.Equal(FillMode.None, animation.FillMode);
        Assert.Null(OverlayParts.FadeIn(TimeSpan.Zero, new Avalonia.Animation.Easings.CubicEaseOut()));
    }

    // Every menu and flyout popup is asked for the window's overlay layer, so its glass blurs the app.
    [AvaloniaFact]
    public void MenusAndFlyouts_OpenInTheOverlayLayer()
    {
        var (window, menu, _) = ShowContextMenu(ThemeVariant.Light);
        try
        {
            AssertOverlay(Assert.IsType<Popup>(menu.Parent));
            var sub = Items(menu).ElementAt(1);
            sub.Open();
            Overlays.Pump(window);
            AssertOverlay(Part<Popup>(sub, MenuTheme.PopupPart));
        }
        finally
        {
            menu.Close();
            window.Close();
        }

        var item = new MenuItem { Header = "One" };
        var content = new TextBlock { Text = "Flyout" };
        var menuButton = new Button { Content = "Menu", Flyout = new MenuFlyout { ItemsSource = new[] { item } } };
        var flyoutButton = new Button { Content = "Flyout", Flyout = new Flyout { Content = content } };
        var file = new MenuItem { Header = "File", ItemsSource = new[] { new MenuItem { Header = "New" } } };
        var bar = new Menu { ItemsSource = new[] { file } };
        var host = Overlays.Host(ThemeVariant.Light, new StackPanel { Children = { bar, menuButton, flyoutButton } }, 300, 300);
        try
        {
            menuButton.Flyout!.ShowAt(menuButton);
            Overlays.Pump(host);
            AssertOverlay(Assert.IsType<Popup>(item.FindLogicalAncestorOfType<MenuFlyoutPresenter>()!.Parent));
            menuButton.Flyout.Hide();

            flyoutButton.Flyout!.ShowAt(flyoutButton);
            Overlays.Pump(host);
            AssertOverlay(Assert.IsType<Popup>(content.FindLogicalAncestorOfType<FlyoutPresenter>()!.Parent));
            flyoutButton.Flyout.Hide();

            file.Open();
            Overlays.Pump(host);
            AssertOverlay(Part<Popup>(file, MenuTheme.PopupPart));
            file.Close();
        }
        finally
        {
            host.Close();
        }

        static void AssertOverlay(Popup popup)
        {
            Assert.True(popup.ShouldUseOverlayLayer);
            Assert.True(popup.IsUsingOverlayLayer);
        }
    }

    [AvaloniaFact]
    public void Glass_InAPopupWindow_DrawsTheSolidFallback()
    {
        var glass = new GlassBorder { Width = 100, Height = 40 };
        var target = new Border { Width = 50, Height = 20 };
        var popup = new Popup { Child = glass, PlacementTarget = target, ShouldUseOverlayLayer = false };
        var window = new Window { Width = 300, Height = 200, Content = new Panel { Children = { target, popup } } };
        window.Show();
        try
        {
            popup.IsOpen = true;
            Overlays.Pump(window);
            Assert.Equal(popup.IsUsingOverlayLayer, !glass.IsSolid);

            popup.IsOpen = false;
            popup.ShouldUseOverlayLayer = true;
            popup.IsOpen = true;
            Overlays.Pump(window);
            Assert.True(popup.IsUsingOverlayLayer);
            Assert.False(glass.IsSolid);
        }
        finally
        {
            popup.IsOpen = false;
            window.Close();
        }
    }

    // --- Helpers ---------------------------------------------------------------------------

    private static (Window Window, ContextMenu Menu, Control Target) ShowContextMenu(ThemeVariant variant, StandardsTheme? theme = null, bool reduceMotion = true)
    {
        var target = new Border { Width = 10, Height = 10, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var menu = new ContextMenu
        {
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            ItemsSource = new object[]
            {
                new MenuItem { Header = "Open…", Icon = new Icon { Data = Icons.MagnifyingGlass }, InputGesture = new KeyGesture(Key.O, KeyModifiers.Control) },
                new MenuItem { Header = "Recent", ItemsSource = new[] { new MenuItem { Header = "One" }, new MenuItem { Header = "Two" } } },
                new MenuItem { Header = "Checked", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true },
                new MenuItem { Header = "Unchecked", ToggleType = MenuItemToggleType.CheckBox },
                new MenuItem { Header = "Radio", ToggleType = MenuItemToggleType.Radio, IsChecked = true },
                new MenuItem { Header = "-" },
                new MenuItem { Header = "Print…", IsEnabled = false, InputGesture = new KeyGesture(Key.P, KeyModifiers.Control) },
            },
        };
        target.ContextMenu = menu;
        var window = Overlays.Host(variant, target, 400, 500, reduceMotion);
        if (theme is not null)
        {
            window.Styles.Clear();
            window.Styles.Add(theme);
        }
        menu.Open(target);
        Overlays.Pump(window);
        return (window, menu, target);
    }

    private static IEnumerable<MenuItem> Items(ItemsControl menu) => menu.GetLogicalChildren().OfType<MenuItem>();

    private static GlassBorder Surface(Control overlay) =>
        overlay.GetVisualDescendants().OfType<GlassBorder>().First(g => g.Name == OverlayParts.SurfacePart);

    private static SquircleBorder Chrome(MenuItem item) =>
        item.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart);

    private static Control Header(MenuItem item) => Part<Control>(item, MenuTheme.HeaderPart);

    private static T Part<T>(MenuItem item, string name) where T : Control =>
        item.GetVisualDescendants().OfType<T>().First(c => c.Name == name);
}
