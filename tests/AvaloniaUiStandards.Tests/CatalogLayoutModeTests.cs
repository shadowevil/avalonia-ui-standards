using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Catalog;
using Catalog.Pages;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// The catalog's layout mode (Settings page, saved as LayoutMode) and its window menu: the setting reaches the main
/// window at once and round-trips through the settings file; the menu shows in the menu row in the Windows layout, with
/// About at the end of Help, and its entries work.
/// </summary>
public class CatalogLayoutModeTests
{
    private static string NewPath() =>
        Path.Combine(AppContext.BaseDirectory, "settings-tests", Guid.NewGuid().ToString("N"), "settings.json");

    [AvaloniaFact]
    public void LayoutMode_RoundTripsThroughTheSettingsFile_AndUnknownValuesGiveAutomatic()
    {
        var app = Application.Current!;
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var path = NewPath();
        try
        {
            Assert.Equal("Automatic", new CatalogSettings().LayoutMode);
            new CatalogSettings { LayoutMode = "Windows" }.Save(path);
            Assert.Equal("Windows", CatalogSettings.Load(path).LayoutMode);

            CatalogSettings.Load(path).ApplyTo(app, theme);
            Assert.Equal(WindowLayoutMode.Windows, CatalogLayout.Get(app));
            CatalogLayout.Set(app, WindowLayoutMode.MacOS);
            Assert.Equal("MacOS", CatalogSettings.From(app, theme).LayoutMode);

            new CatalogSettings { LayoutMode = "Sideways" }.ApplyTo(app, theme);
            Assert.Equal(WindowLayoutMode.Automatic, CatalogLayout.Get(app));
            new CatalogSettings { LayoutMode = "7" }.ApplyTo(app, theme);
            Assert.Equal(WindowLayoutMode.Automatic, CatalogLayout.Get(app));
        }
        finally
        {
            CatalogLayout.Set(app, WindowLayoutMode.Automatic);
            app.RequestedThemeVariant = ThemeVariant.Default;
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [AvaloniaFact]
    public void Store_SavesALayoutModeChange()
    {
        var app = Application.Current!;
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var path = NewPath();
        var store = CatalogSettingsStore.Start(app, theme, path);
        try
        {
            CatalogLayout.Set(app, WindowLayoutMode.Windows);
            Assert.True(store.IsPending);
            store.Flush();
            Assert.Equal("Windows", CatalogSettings.Load(path).LayoutMode);
        }
        finally
        {
            store.Dispose();
            CatalogLayout.Set(app, WindowLayoutMode.Automatic);
            app.RequestedThemeVariant = ThemeVariant.Default;
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [AvaloniaFact]
    public void SettingsPage_LayoutMode_SetsTheMainWindowsLayoutAtOnce()
    {
        var app = Application.Current!;
        var window = new MainWindow { Width = 1200, Height = 800 };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        try
        {
            window.Show();
            MenuRowTests.Pump(window);
            var page = new SettingsPage();
            Assert.Equal(SettingsPage.LayoutModeLabels, page.LayoutMode.Items.Select(i => ((AvaloniaUiStandards.Blocks.SegmentedControlItem)i!).Content));
            Assert.Equal(0, page.LayoutMode.SelectedIndex);

            page.LayoutMode.SelectedIndex = 2;
            MenuRowTests.Pump(window);
            Assert.Equal(WindowLayoutMode.Windows, CatalogLayout.Get(app));
            Assert.Equal(WindowLayoutMode.Windows, window.LayoutMode);
            Assert.True(window.IsMenuRowShown);

            page.LayoutMode.SelectedIndex = 1;
            MenuRowTests.Pump(window);
            Assert.Equal(WindowLayoutMode.MacOS, window.LayoutMode);
            Assert.False(window.IsMenuRowShown);

            // A page opened later shows the current choice.
            Assert.Equal(1, new SettingsPage().LayoutMode.SelectedIndex);
        }
        finally
        {
            window.Close();
            CatalogLayout.Set(app, WindowLayoutMode.Automatic);
        }
    }

    [AvaloniaFact]
    public void MainWindow_Menu_ShowsInTheMenuRow_WithAboutAtTheEndOfHelp_AndWorks()
    {
        var app = Application.Current!;
        App.ApplyIdentity(app);
        var window = new MainWindow { Width = 1200, Height = 800, LayoutMode = WindowLayoutMode.Windows };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        try
        {
            window.Show();
            MenuRowTests.Pump(window);
            Assert.Equal(new object?[] { "_File", "_Edit", "_View", "_Help" }, window.MenuRow.Titles.Select(t => t.Header));
            var help = window.MenuRow.Titles[^1];
            Assert.Equal(new object?[] { "Show the settings _file location", "-", $"About {AppInfo.Name}" },
                help.Items.Select(i => i is MenuItem m ? m.Header : "-"));

            // View: the theme and density radios follow the toolbar band and set it.
            var menus = window.Menus;
            Assert.True(menus.ThemeItems[0].IsChecked);
            window.ThemeControls.Theme.SelectedIndex = 2;
            Assert.Equal(new[] { false, false, true }, menus.ThemeItems.Select(i => i.IsChecked));
            ((INativeMenuItemExporterEventsImplBridge)menus.DensityItems[1]).RaiseClicked();
            Assert.Equal(1, window.ThemeControls.Density.SelectedIndex);
            Assert.True(menus.DensityItems[1].IsChecked);

            // Next and previous page move through the pages, wrapping.
            window.SelectPage(0);
            ((INativeMenuItemExporterEventsImplBridge)menus.NextPage).RaiseClicked();
            Assert.Equal(1, window.SelectedPage);
            ((INativeMenuItemExporterEventsImplBridge)menus.PreviousPage).RaiseClicked();
            ((INativeMenuItemExporterEventsImplBridge)menus.PreviousPage).RaiseClicked();
            Assert.Equal(CatalogPages.All.Count - 1, window.SelectedPage);

            // Close window closes it.
            ((INativeMenuItemExporterEventsImplBridge)menus.CloseWindow).RaiseClicked();
            Assert.False(window.IsVisible);
        }
        finally
        {
            window.Close();
            window.ThemeControls.Theme.SelectedIndex = 0;
            window.ThemeControls.Density.SelectedIndex = 0;
            app.Name = null;
            NativeMenu.SetMenu(app, null!);
            app.RequestedThemeVariant = ThemeVariant.Default;
        }
    }

    [AvaloniaFact]
    public void ControlA_InAFocusedListBox_SelectsAll_NotTheCatalogsSelectAll()
    {
        // The catalog's Edit, Select all has the platform's select-all gesture (Ctrl+A, Cmd+A on macOS).
        var app = Application.Current!;
        var window = new Catalog.MainWindow { Width = 1000, Height = 700 };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        var list = new ListBox { SelectionMode = SelectionMode.Multiple, ItemsSource = new[] { "One", "Two", "Three" } };
        var box = new TextBox { Text = "Some text" };
        window.Content = new StackPanel { Children = { list, box } };
        var selectAll = window.Menus.TextActions[^1];
        var gesture = selectAll.Gesture!;
        Assert.Equal(app.PlatformSettings!.HotkeyConfiguration.SelectAll[0], gesture);
        try
        {
            window.Show();
            MenuRowTests.Pump(window);
            var clicks = 0;
            selectAll.Click += (_, _) => clicks++;

            // A list focused: the list selects all; the menu entry is disabled and claims nothing.
            list.SelectedIndex = 0;
            list.ContainerFromIndex(0)!.Focus();
            MenuRowTests.Pump(window);
            Assert.False(selectAll.IsEnabled);
            MenuRowTests.Press(window, gesture.Key, (RawInputModifiers)gesture.KeyModifiers, "a");
            Assert.Equal(0, clicks);
            Assert.Equal(3, list.SelectedItems!.Count);

            // A text box focused: the entry is enabled and acts on it.
            box.Focus();
            MenuRowTests.Pump(window);
            Assert.True(selectAll.IsEnabled);
            MenuRowTests.Press(window, gesture.Key, (RawInputModifiers)gesture.KeyModifiers, "a");
            Assert.Equal(box.Text!.Length, box.SelectionEnd - box.SelectionStart);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Renders ------------------------------------------------------------------------------

    /// <summary>
    /// The catalog window's top in the Windows layout with its menu row (screenshots/catalog-menu-row-*.png), light and
    /// dark, Normal and Compact; Help open with About merged; at the minimum width; and the Settings page's layout mode.
    /// </summary>
    [AvaloniaFact]
    public void MenuRow_Renders()
    {
        var app = Application.Current!;
        App.ApplyIdentity(app);
        // The Windows layout, set as the Settings page sets it.
        CatalogLayout.Set(app, WindowLayoutMode.Windows);
        try
        {
            foreach (var (label, variant) in ButtonRenderTests.Themes)
            foreach (var compact in new[] { false, true })
                Render($"catalog-menu-row-{label.ToLowerInvariant()}-{(compact ? "compact" : "normal")}.png", variant, compact, 1200, 220, null);
            Render("catalog-menu-row-open-help-light.png", ThemeVariant.Light, false, 1200, 320, w =>
            {
                var help = w.MenuRow.Titles[^1];
                help.Focus();
                help.IsSubMenuOpen = true;
            });
            Render("catalog-menu-row-narrow-light.png", ThemeVariant.Light, false, 528, 220, null);
            Render("catalog-menu-row-settings-light.png", ThemeVariant.Light, false, 1200, 520, w =>
                w.SelectPage(CatalogPages.All.ToList().FindIndex(p => p.Title == "Settings")));
        }
        finally
        {
            CatalogLayout.Set(app, WindowLayoutMode.Automatic);
            app.Name = null;
            NativeMenu.SetMenu(app, null!);
        }
    }

    private static void Render(string name, ThemeVariant variant, bool compact, double width, double cropHeight, Action<MainWindow>? act)
    {
        var window = new MainWindow
        {
            Width = width,
            Height = 800,
            RequestedThemeVariant = variant,
            Background = new SolidColorBrush(Color.Parse(variant == ThemeVariant.Dark ? "#3A3D42" : "#C9CDD3")),
        };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced)
        {
            ReduceMotion = true,
            Density = compact ? Avalonia.Themes.Fluent.DensityStyle.Compact : Avalonia.Themes.Fluent.DensityStyle.Normal,
        });
        window.SetRenderScaling(Sheet.Scale);
        try
        {
            window.Show();
            window.Activate();
            MenuRowTests.Pump(window);
            if (act is not null)
            {
                act(window);
                MenuRowTests.Pump(window);
            }
            using var frame = Frame.Capture(window);
            using var crop = Frame.Crop(frame.Bitmap, new PixelRect(0, 0, frame.Width, (int)Math.Min(cropHeight * Sheet.Scale, frame.Height)));
            var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(dir);
            using var stream = File.Create(Path.Combine(dir, name));
            crop.Save(stream, new PngBitmapEncoderOptions());
        }
        finally
        {
            window.Close();
        }
    }
}
