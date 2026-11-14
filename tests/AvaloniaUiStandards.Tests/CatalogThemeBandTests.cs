using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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

            // A preset sets the accent and the hex box; the swatch shows it.
            var swatch = ((Panel)band.Accent.Content!).Children.OfType<SquircleBorder>().Single();
            for (var i = 0; i < ThemeBand.Accents.Length; i++)
            {
                band.AccentList.SelectedIndex = i;
                Pump(window);
                Assert.Equal(Color.Parse(ThemeBand.Accents[i].Hex), theme.Accent);
                Assert.Equal(ThemeBand.Accents[i].Hex, band.AccentHex.Text);
                Assert.Equal(theme.Accent, Assert.IsType<SolidColorBrush>(swatch.Background).Color);
            }

            // A typed hex sets the accent and clears the preset; an invalid one changes nothing.
            band.AccentHex.Text = "#123456";
            Assert.Equal(Color.Parse("#123456"), theme.Accent);
            Assert.Equal(-1, band.AccentList.SelectedIndex);
            band.AccentHex.Text = "#12";
            Assert.Equal(Color.Parse("#123456"), theme.Accent);
            band.AccentHex.Text = ThemeBand.Accents[2].Hex;
            Assert.Equal(2, band.AccentList.SelectedIndex);

            // Pressing the swatch opens the accent flyout.
            ClickCentre(window, band.Accent);
            Assert.True(band.AccentFlyout.IsOpen);
            band.AccentFlyout.Hide();
        }
        finally
        {
            window.Close();
            Reset(app, theme);
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
                band.AccentList.SelectedIndex = 4;
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
                Assert.Equal(4, band.AccentList.SelectedIndex);
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
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
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
    public void OverflowMenu_ShowsSegmentedGroups_AndTheAccentEntryOpensTheFlyout()
    {
        var controls = new ThemeBand();
        var band = controls.Band;
        band.HorizontalAlignment = HorizontalAlignment.Left;
        band.VerticalAlignment = VerticalAlignment.Top;
        var window = new Window { Width = 1000, Height = 700, Content = band };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
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

            var accent = entries.Single(e => e.Header as string == "Accent colour");
            ClickCentre(window, accent);
            Assert.True(controls.AccentFlyout.IsOpen);
            Assert.Same(band.MoreButton, controls.AccentFlyout.Target);
            Assert.Equal(PlacementMode.BottomEdgeAlignedRight, controls.AccentFlyout.Placement);
            controls.AccentFlyout.Hide();
            Pump(window);

            // From the swatch it aligns left edges again.
            band.Width = 1000;
            Pump(window);
            ClickCentre(window, controls.Accent);
            Assert.True(controls.AccentFlyout.IsOpen);
            Assert.Equal(PlacementMode.BottomEdgeAlignedLeft, controls.AccentFlyout.Placement);
            controls.AccentFlyout.Hide();
        }
        finally
        {
            band.OverflowMenu.Hide();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AccentFlyoutFromTheMoreButton_StaysInsideTheWindow()
    {
        var window = new MainWindow { Width = 600, Height = 700 };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        try
        {
            window.Show();
            Pump(window);
            var controls = window.ThemeControls;
            Assert.True(controls.Band.IsOverflowed(controls.Motion));
            controls.AccentFlyout.ShowAt(controls.Band.MoreButton);
            Pump(window);
            var presenter = window.GetVisualDescendants().OfType<FlyoutPresenter>().Single(p => p.IsEffectivelyVisible);
            var frame = window.GetVisualDescendants().OfType<Control>().First(c => c.Name == StandardsWindowTheme.FramePart);
            var right = presenter.TranslatePoint(new Point(presenter.Bounds.Width, 0), window)!.Value.X;
            var moreRight = controls.Band.MoreButton.TranslatePoint(new Point(controls.Band.MoreButton.Bounds.Width, 0), window)!.Value.X;
            Assert.Equal(moreRight, right, 0.5);
            Assert.True(right <= frame.TranslatePoint(new Point(frame.Bounds.Width, 0), window)!.Value.X);
        }
        finally
        {
            window.ThemeControls.AccentFlyout.Hide();
            window.Close();
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
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true, Density = density });
        try
        {
            window.Show();
            band.Width = 1300;
            Pump(window);
            foreach (var (control, data) in new[] { (controls.Motion, Icons.Play), (controls.Transparency, Icons.Drop) })
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

        // A narrow window: what does not fit is in the "…" menu, shown open.
        Render("catalog-toolbar-band-narrow-light.png", ThemeVariant.Light, DensityStyle.Normal, 600, 520,
            w => w.ThemeControls.Band.OverflowMenu.ShowAt(w.ThemeControls.Band.MoreButton));
        // The accent flyout opened from "…" in the narrow window: right edges aligned, inside the window.
        Render("catalog-toolbar-band-accent-overflow-light.png", ThemeVariant.Light, DensityStyle.Normal, 600, 520,
            w => w.ThemeControls.AccentFlyout.ShowAt(w.ThemeControls.Band.MoreButton));
        // The accent swatch's flyout, open, with the selected preset's row without the accent flag (before) and with it (after).
        foreach (var (label, variant) in ButtonRenderTests.Themes)
        foreach (var flag in new[] { false, true })
            Render($"catalog-toolbar-band-accent-{label.ToLowerInvariant()}-{(flag ? "after" : "before")}.png", variant, DensityStyle.Normal, 1200, 420, w =>
            {
                SelectionOptions.SetAccentFlag(w.ThemeControls.AccentList, flag);
                w.ThemeControls.AccentFlyout.ShowAt(w.ThemeControls.Accent);
            });
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
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true, Density = density });
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
            window.ThemeControls.AccentFlyout.Hide();
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
