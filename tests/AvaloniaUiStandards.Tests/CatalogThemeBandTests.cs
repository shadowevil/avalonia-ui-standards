using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Catalog;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// The catalog's theme controls in the window's toolbar band: each sets the application's theme, the choices persist
/// through the settings file, and as the band narrows theme and accent stay visible longest. Renders the window's top
/// area (screenshots/catalog-toolbar-band-*.png).
/// </summary>
public class CatalogThemeBandTests
{
    [AvaloniaFact]
    public void MainWindow_HasTheThemeBandInTheToolbarBandSlot_AndNothingInTheTitleBand()
    {
        var window = new MainWindow();
        try
        {
            window.Show();
            Pump(window);
            Assert.Null(window.Toolbar);
            Assert.Same(window.ThemeControls.Band, window.ToolbarBand);
            var slot = window.GetVisualDescendants().OfType<Control>().First(c => c.Name == StandardsWindowTheme.ToolbarBandPart);
            Assert.Contains(window.ThemeControls.Band, slot.GetVisualDescendants());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EachBandControl_ChangesTheTheme()
    {
        var (app, theme) = AppTheme();
        var window = new MainWindow();
        try
        {
            window.Show();
            Pump(window);
            var band = window.ThemeControls;

            foreach (var (index, variant) in new[] { (1, ThemeVariant.Light), (2, ThemeVariant.Dark), (0, ThemeVariant.Default) })
            {
                band.Theme.SelectedIndex = index;
                Assert.Equal(variant, app.RequestedThemeVariant);
            }

            band.Density.SelectedIndex = 1;
            Assert.Equal(DensityStyle.Compact, theme.Density);
            band.Density.SelectedIndex = 0;
            Assert.Equal(DensityStyle.Normal, theme.Density);

            foreach (var (control, read) in new (SegmentedControl, Func<bool?>)[]
                     {
                         (band.Motion, () => theme.ReduceMotion),
                         (band.Transparency, () => theme.ReduceTransparency),
                     })
                foreach (var (index, value) in new[] { (1, (bool?)true), (2, false), (0, null) })
                {
                    control.SelectedIndex = index;
                    Assert.Equal(value, read());
                }

            // Each preset, chosen in the accent menu, sets the accent and the hex box, checks its own item only, and
            // the button's chip shows it.
            var chip = Assert.IsType<ChipIcon>(band.Accent.Content);
            for (var i = 0; i < ThemeBand.Accents.Length; i++)
            {
                ClickCentre(window, band.Accent);
                Assert.True(band.AccentMenu.IsOpen);
                ClickCentre(window, band.AccentPresets[i]);
                Assert.False(band.AccentMenu.IsOpen);
                Assert.Equal(Color.Parse(ThemeBand.Accents[i].Hex), theme.Accent);
                Assert.Equal(ThemeBand.Accents[i].Hex, band.AccentHex.Text);
                Assert.Equal(i, CheckedPreset(band));
                Assert.Equal(theme.Accent, Assert.IsType<SolidColorBrush>(chip.Background).Color);
            }

            // Choosing the chosen preset again keeps it checked.
            var last = ThemeBand.Accents.Length - 1;
            ClickCentre(window, band.Accent);
            ClickCentre(window, band.AccentPresets[last]);
            Assert.Equal(last, CheckedPreset(band));

            // A hex typed in the menu's box sets the accent and clears the presets' check, and the menu stays open;
            // an invalid one changes nothing.
            ClickCentre(window, band.Accent);
            ClickCentre(window, band.AccentHex);
            Assert.True(band.AccentMenu.IsOpen);
            Assert.True(band.AccentHex.IsFocused);
            band.AccentHex.Text = "";
            window.KeyTextInput("#123456");
            Pump(window);
            Assert.True(band.AccentMenu.IsOpen);
            Assert.Equal(Color.Parse("#123456"), theme.Accent);
            Assert.Equal(-1, CheckedPreset(band));
            band.AccentHex.Text = "#12";
            Assert.Equal(Color.Parse("#123456"), theme.Accent);
            band.AccentHex.Text = ThemeBand.Accents[2].Hex;
            Assert.Equal(2, CheckedPreset(band));
            band.AccentMenu.Hide();
        }
        finally
        {
            window.Close();
            Reset(app, theme);
        }
    }

    [AvaloniaFact]
    public void AccentMenu_Keyboard_ReachesTheHexBoxAndBack()
    {
        var window = new MainWindow();
        try
        {
            window.Show();
            Pump(window);
            var band = window.ThemeControls;
            var pink = band.AccentPresets[^1];
            ClickCentre(window, band.Accent);
            Assert.True(band.AccentMenu.IsOpen);
            pink.Focus(Avalonia.Input.NavigationMethod.Directional);
            Pump(window);

            // Down from Pink puts the caret in the hex box; Up returns to Pink; Escape closes the menu.
            Press(window, Avalonia.Input.Key.Down);
            Assert.True(band.AccentHex.IsFocused);
            Press(window, Avalonia.Input.Key.Up);
            Assert.True(pink.IsFocused);
            Assert.True(band.AccentMenu.IsOpen);
            Press(window, Avalonia.Input.Key.Down);
            Assert.True(band.AccentHex.IsFocused);
            Press(window, Avalonia.Input.Key.Escape);
            Assert.False(band.AccentMenu.IsOpen);
        }
        finally
        {
            window.ThemeControls.AccentMenu.Hide();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BandChoices_PersistAndReload()
    {
        var (app, theme) = AppTheme();
        var path = Path.Combine(AppContext.BaseDirectory, "settings-tests", Guid.NewGuid().ToString("N"), "settings.json");
        var window = new MainWindow();
        try
        {
            using (var store = CatalogSettingsStore.Start(app, theme, path))
            {
                window.Show();
                Pump(window);
                var band = window.ThemeControls;
                band.Theme.SelectedIndex = 2;
                band.Density.SelectedIndex = 1;
                band.AccentPresets[4].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                band.Motion.SelectedIndex = 1;
                band.Transparency.SelectedIndex = 2;
                Assert.True(store.IsPending);
                store.Flush();
                window.Close();
            }

            // The same JSON fields as before the band.
            using (var json = JsonDocument.Parse(File.ReadAllText(path)))
            {
                var root = json.RootElement;
                Assert.Equal("Dark", root.GetProperty("Theme").GetString());
                Assert.Equal("Compact", root.GetProperty("Density").GetString());
                Assert.Equal(ThemeBand.Accents[4].Hex, root.GetProperty("Accent").GetString());
                Assert.True(root.GetProperty("ReduceMotion").GetBoolean());
                Assert.False(root.GetProperty("ReduceTransparency").GetBoolean());
            }

            // Back to the defaults, then a new start reads the file and a new window's band shows the choices.
            Reset(app, theme);
            using (CatalogSettingsStore.Start(app, theme, path))
            {
                window = new MainWindow();
                window.Show();
                Pump(window);
                var band = window.ThemeControls;
                Assert.Equal(2, band.Theme.SelectedIndex);
                Assert.Equal(1, band.Density.SelectedIndex);
                Assert.Equal(4, CheckedPreset(band));
                Assert.Equal(ThemeBand.Accents[4].Hex, band.AccentHex.Text);
                Assert.Equal(1, band.Motion.SelectedIndex);
                Assert.Equal(2, band.Transparency.SelectedIndex);
            }
        }
        finally
        {
            window.Close();
            Reset(app, theme);
            var folder = Path.GetDirectoryName(path)!;
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [AvaloniaFact]
    public void NarrowingBand_OverflowsTransparencyFirst_AndKeepsThemeAndAccentLongest()
    {
        var controls = new ThemeBand();
        var band = controls.Band;
        band.HorizontalAlignment = HorizontalAlignment.Left;
        var window = new Window { Width = 1400, Height = 300, Content = band };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true });
        // Overflow order: the first item in the list goes first, the last item last.
        Control[] order = [controls.Transparency, controls.Motion, controls.Density, controls.Accent, controls.Theme];
        try
        {
            window.Show();
            band.Width = 1300;
            Pump(window);
            Assert.All(order, item => Assert.False(band.IsOverflowed(item)));
            Assert.False(band.MoreButton.IsEffectivelyVisible);

            var seen = new HashSet<int>();
            for (var width = 1300; width >= 120; width -= 10)
            {
                band.Width = width;
                Pump(window);
                var overflowed = order.Count(band.IsOverflowed);
                // Whatever overflows is a prefix of the order: nothing goes before the items listed ahead of it.
                Assert.All(order.Take(overflowed), item => Assert.True(band.IsOverflowed(item), $"{width}: {Label(item)} should be in the menu"));
                Assert.All(order.Skip(overflowed), item => Assert.False(band.IsOverflowed(item), $"{width}: {Label(item)} should be in the band"));
                seen.Add(overflowed);
            }

            // At some width only theme and accent are left in the band, and at the narrowest everything is in the menu.
            Assert.Contains(3, seen);
            Assert.Contains(order.Length, seen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OverflowMenu_ShowsSegmentedGroups_AndTheAccentSubmenu()
    {
        var controls = new ThemeBand();
        var band = controls.Band;
        band.HorizontalAlignment = HorizontalAlignment.Left;
        band.VerticalAlignment = VerticalAlignment.Top;
        var window = new Window { Width = 1000, Height = 700, Content = band };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true });
        try
        {
            window.Show();
            band.Width = 140;
            Pump(window);
            band.OverflowMenu.ShowAt(band.MoreButton);
            Pump(window);
            var entries = band.MenuEntries.OfType<MenuItem>().ToList();
            var radios = entries.Where(e => e.ToggleType == MenuItemToggleType.Radio).Select(e => e.Header as string).ToList();
            Assert.Equal(["System", "Light", "Dark", "Normal", "Compact", "Follow OS", "Reduced", "Full", "Follow OS", "Reduced", "Full"], radios);

            // The accent drop-down is a submenu: the presets as check items, the chosen one checked, then "Custom colour…".
            var accent = entries.Single(e => e.Header as string == "Accent colour");
            var items = accent.Items.OfType<MenuItem>().ToList();
            Assert.Equal(ThemeBand.Accents.Select(a => a.Name).Append("Custom colour…"), items.Select(i => i.Header as string));
            Assert.All(items.Take(ThemeBand.Accents.Length), i => Assert.Equal(MenuItemToggleType.CheckBox, i.ToggleType));
            Assert.True(items[0].IsChecked);

            // Choosing a preset there sets the accent.
            var theme = AppTheme().Theme;
            items[3].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Pump(window);
            Assert.Equal(Color.Parse(ThemeBand.Accents[3].Hex), theme.Accent);
            Assert.Equal(3, CheckedPreset(controls));

            // "Custom colour…" opens the whole accent menu at the band's trailing end, right edges aligned.
            // Chosen with the pointer in the open submenu, as a user would; it opens once the "…" menu has closed.
            band.OverflowMenu.Hide();
            Pump(window);
            band.OverflowMenu.ShowAt(band.MoreButton);
            Pump(window);
            var submenu = band.MenuEntries.OfType<MenuItem>().Single(e => e.Header as string == "Accent colour");
            ClickCentre(window, submenu);
            Assert.True(submenu.IsSubMenuOpen);
            ClickCentre(window, submenu.Items.OfType<MenuItem>().Last());
            Assert.False(band.OverflowMenu.IsOpen);
            Assert.True(controls.AccentMenu.IsOpen);
            Assert.Same(band, controls.AccentMenu.Target);
            Assert.Equal(PlacementMode.BottomEdgeAlignedRight, controls.AccentMenu.Placement);
            controls.AccentMenu.Hide();
            Pump(window);

            // From the drop-down button it aligns left edges.
            band.Width = 1000;
            Pump(window);
            ClickCentre(window, controls.Accent);
            Assert.True(controls.AccentMenu.IsOpen);
            Assert.Same(controls.Accent, controls.AccentMenu.Target);
            Assert.Equal(PlacementMode.BottomEdgeAlignedLeft, controls.AccentMenu.Placement);
            controls.AccentMenu.Hide();
        }
        finally
        {
            band.OverflowMenu.Hide();
            Reset(Application.Current!, AppTheme().Theme);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AccentMenuOpenedFromTheOverflow_StaysInsideTheWindow()
    {
        var window = new MainWindow { Width = 600, Height = 700 };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true });
        try
        {
            window.Show();
            Pump(window);
            var controls = window.ThemeControls;
            Assert.True(controls.Band.IsOverflowed(controls.Motion));
            controls.AccentMenu.ShowAt(controls.Band);
            Pump(window);
            var presenter = window.GetVisualDescendants().OfType<MenuFlyoutPresenter>().Single(p => p.IsEffectivelyVisible);
            var band = controls.Band;
            var right = presenter.TranslatePoint(new Point(presenter.Bounds.Width, 0), window)!.Value.X;
            var left = presenter.TranslatePoint(default, window)!.Value.X;
            Assert.Equal(band.TranslatePoint(new Point(band.Bounds.Width, 0), window)!.Value.X, right, 0.5);
            Assert.True(left >= band.TranslatePoint(default, window)!.Value.X);
        }
        finally
        {
            window.ThemeControls.AccentMenu.Hide();
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(DensityStyle.Normal, 20)]
    [InlineData(DensityStyle.Compact, 16)]
    public void AccentButton_IsAnIconDropDown_WithTheChipAtTheIconSize(DensityStyle density, double size)
    {
        var controls = new ThemeBand();
        var band = controls.Band;
        band.HorizontalAlignment = HorizontalAlignment.Left;
        var window = new Window { Width = 1400, Height = 300, Content = band };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true, Density = density });
        try
        {
            window.Show();
            band.Width = 1300;
            Pump(window);
            var chip = Assert.IsType<ChipIcon>(controls.Accent.Content);
            Assert.Equal(new Size(size, size), chip.Bounds.Size);
            // The band's icon drop-down size: 48 x 32 (40 x 24 Compact).
            Assert.Equal(density == DensityStyle.Compact ? new Size(40, 24) : new Size(48, 32), controls.Accent.Bounds.Size);
            Assert.All(controls.AccentPresets, p => Assert.IsType<ChipIcon>(p.Icon));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MotionIcon_SwapsWithTheSelectedValue()
    {
        var (app, theme) = AppTheme();
        var controls = new ThemeBand();
        var icon = Assert.IsType<LeadingIconSegmentedControl>(controls.Motion).Icon;
        try
        {
            Assert.Same(Icons.MotionSystem, icon.Data);
            foreach (var (index, data) in new[] { (1, Icons.MotionReduced), (2, Icons.MotionFull), (0, Icons.MotionSystem) })
            {
                controls.Motion.SelectedIndex = index;
                Assert.Same(data, icon.Data);
            }

            // A band made while motion is reduced starts with the reduced icon.
            theme.ReduceMotion = true;
            Assert.Same(Icons.MotionReduced, Assert.IsType<LeadingIconSegmentedControl>(new ThemeBand().Motion).Icon.Data);
        }
        finally
        {
            Reset(app, theme);
        }
    }

    [AvaloniaTheory]
    [InlineData(DensityStyle.Normal, 20)]
    [InlineData(DensityStyle.Compact, 16)]
    public void MotionAndTransparency_HaveADecorativeLeadingIcon_ThatOverflowsWithTheGroup(DensityStyle density, double size)
    {
        var controls = new ThemeBand();
        var band = controls.Band;
        band.HorizontalAlignment = HorizontalAlignment.Left;
        var window = new Window { Width = 1400, Height = 300, Content = band };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true, Density = density });
        try
        {
            window.Show();
            band.Width = 1300;
            Pump(window);
            foreach (var (control, data) in new[] { (controls.Motion, Icons.MotionSystem), (controls.Transparency, Icons.Drop) })
            {
                var icon = Assert.IsType<LeadingIconSegmentedControl>(control).Icon;
                Assert.Same(data, icon.Data);
                Assert.Equal(new Size(size, size), icon.Bounds.Size);
                Assert.False(icon.Focusable);
                Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(icon));
                Assert.Equal(Label(control), AutomationProperties.GetName(control));
                // The icon leads, 8 px before the segments.
                var track = control.GetVisualChildren().OfType<Control>().Single(c => c != icon);
                Assert.Equal(0, icon.Bounds.X);
                Assert.Equal(size + 8, track.Bounds.X);
                Assert.True(icon.IsEffectivelyVisible);
            }

            // Narrowed until both groups are in the menu: their icons go with them.
            band.Width = 500;
            Pump(window);
            Assert.True(band.IsOverflowed(controls.Motion));
            Assert.True(band.IsOverflowed(controls.Transparency));
            Assert.False(((LeadingIconSegmentedControl)controls.Motion).Icon.IsEffectivelyVisible);
            Assert.False(((LeadingIconSegmentedControl)controls.Transparency).Icon.IsEffectivelyVisible);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Renders ----------------------------------------------------------------------------

    [AvaloniaFact]
    public void Renders_TheWindowTopArea()
    {
        foreach (var (label, variant) in ButtonRenderTests.Themes)
        foreach (var density in new[] { DensityStyle.Normal, DensityStyle.Compact })
            Render($"catalog-toolbar-band-{label.ToLowerInvariant()}-{density.ToString().ToLowerInvariant()}.png", variant, density, 1200, 160);

        // The accent menu, open, in light and dark, Normal and Compact.
        foreach (var (label, variant) in ButtonRenderTests.Themes)
        foreach (var density in new[] { DensityStyle.Normal, DensityStyle.Compact })
            Render($"catalog-toolbar-band-accent-{label.ToLowerInvariant()}-{density.ToString().ToLowerInvariant()}.png", variant, density, 1200, 420,
                w => w.ThemeControls.AccentMenu.ShowAt(w.ThemeControls.Accent));

        // A narrow window: what does not fit is in the "…" menu, shown open.
        Render("catalog-toolbar-band-narrow-light.png", ThemeVariant.Light, DensityStyle.Normal, 600, 520,
            w => w.ThemeControls.Band.OverflowMenu.ShowAt(w.ThemeControls.Band.MoreButton));

        // The accent in the "…" menu: the window's minimum width keeps it in the band, so a band alone, 200 wide, with
        // the accent submenu open.
        var controls = new ThemeBand();
        var band = controls.Band;
        band.Width = 200;
        band.HorizontalAlignment = HorizontalAlignment.Left;
        band.VerticalAlignment = VerticalAlignment.Top;
        var surface = new Border { Child = band };
        surface.Bind(Border.BackgroundProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("SystemRegionColor"));
        var window = new Window { Width = 560, Height = 420, Content = surface, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true });
        try
        {
            window.SetRenderScaling(Sheet.Scale);
            window.Show();
            Pump(window);
            band.OverflowMenu.ShowAt(band.MoreButton);
            Pump(window);
            band.MenuEntries.OfType<MenuItem>().Single(e => e.Header as string == "Accent colour").Open();
            Pump(window);
            using var frame = Frame.Capture(window);
            frame.Save("catalog-toolbar-band-accent-overflow-light.png");
        }
        finally
        {
            band.OverflowMenu.Hide();
            window.Close();
        }
    }

    // The catalog window's frame from its top, <paramref name="height"/> high, with the application set to the variant
    // and density as the band would set them.
    private static void Render(string name, ThemeVariant variant, DensityStyle density, double width, double height, Action<MainWindow>? open = null)
    {
        var (app, theme) = AppTheme();
        app.RequestedThemeVariant = variant;
        theme.Density = density;
        var window = new MainWindow
        {
            Width = width,
            Height = 800,
            RequestedThemeVariant = variant,
            Background = new SolidColorBrush(Color.Parse(variant == ThemeVariant.Dark ? "#3A3D42" : "#C9CDD3")),
        };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true, Density = density });
        try
        {
            window.SetRenderScaling(Sheet.Scale);
            window.Show();
            window.Activate();
            Pump(window);
            if (open is not null)
            {
                open(window);
                Pump(window);
            }
            var frame = window.GetVisualDescendants().OfType<Control>().First(c => c.Name == StandardsWindowTheme.FramePart);
            var origin = frame.TranslatePoint(default, window)!.Value;
            using var capture = window.CaptureRenderedFrame()!;
            using var crop = Frame.Of(Frame.Crop(capture, new PixelRect((int)(origin.X * Sheet.Scale), (int)(origin.Y * Sheet.Scale),
                (int)(frame.Bounds.Width * Sheet.Scale), (int)(height * Sheet.Scale))));
            crop.Save(name);
        }
        finally
        {
            window.ThemeControls.AccentMenu.Hide();
            window.ThemeControls.Band.OverflowMenu.Hide();
            window.Close();
            Reset(app, theme);
        }
    }

    // --- Helpers ----------------------------------------------------------------------------

    private static (Application App, StandardsTheme Theme) AppTheme()
    {
        var app = Application.Current!;
        return (app, app.Styles.OfType<StandardsTheme>().Single());
    }

    private static void Reset(Application app, StandardsTheme theme)
    {
        app.RequestedThemeVariant = ThemeVariant.Default;
        theme.Density = DensityStyle.Normal;
        theme.Accent = StandardsTheme.DefaultAccent;
        theme.ReduceMotion = null;
        theme.ReduceTransparency = null;
    }

    private static string Label(Control item) => ToolbarBand.LabelOf(item);

    private static int CheckedPreset(ThemeBand band) => band.AccentPresets.ToList().FindIndex(p => p.IsChecked);

    private static void Press(Window window, Avalonia.Input.Key key)
    {
        window.KeyPress(key, Avalonia.Input.RawInputModifiers.None, Avalonia.Input.PhysicalKey.None, null);
        window.KeyRelease(key, Avalonia.Input.RawInputModifiers.None, Avalonia.Input.PhysicalKey.None, null);
        Pump(window);
    }

    private static void ClickCentre(Window window, Control control)
    {
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseMove(point);
        window.MouseDown(point, Avalonia.Input.MouseButton.Left);
        window.MouseUp(point, Avalonia.Input.MouseButton.Left);
        Pump(window);
    }

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
