using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Headless;
using Avalonia.Input;
using AvaloniaUiStandards.Blocks;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Modules;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>The title band shows Window.Icon at 16 px in the Windows layout only, as a decorative element.</summary>
public class TitleIconTests
{
    // A small two-tone icon, generated here.
    private static WriteableBitmap MakeBitmap()
    {
        var bitmap = new WriteableBitmap(new PixelSize(32, 32), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var fb = bitmap.Lock())
        {
            for (var y = 0; y < 32; y++)
                for (var x = 0; x < 32; x++)
                {
                    var inner = x is >= 8 and < 24 && y is >= 8 and < 24;
                    var colour = inner ? 0xFFFFC107 : 0xFF2A7DE1;
                    Marshal.WriteInt32(fb.Address + y * fb.RowBytes + x * 4, unchecked((int)colour));
                }
        }
        return bitmap;
    }

    // Headless windows have no icon that can save itself as an image, so the window's decoder is
    // replaced by one that returns the generated bitmap.
    private static WindowIcon MakeIcon() => new(MakeBitmap());

    private static void UseTestDecoder(StandardsWindow window) => window.IconDecoder = _ => MakeBitmap();

    private static StandardsWindow Open(WindowLayoutMode mode, bool icon, ThemeVariant? variant = null, DensityStyle density = DensityStyle.Normal)
    {
        var window = StandardsWindowTests.SampleWindow(variant ?? ThemeVariant.Light, slots: false, WindowButtonStyle.Auto);
        UseTestDecoder(window);
        window.LayoutMode = mode;
        if (icon)
            window.Icon = MakeIcon();
        var theme = StandardsTheme.CreateForTests(AccessibilitySettings.NotReduced);
        theme.Density = density;
        window.Styles.Add(theme);
        window.Show();
        Pump(window);
        return window;
    }

    private static void Pump(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static Image IconPart(Window window) =>
        window.GetVisualDescendants().OfType<Image>().Single(c => c.Name == StandardsWindowTheme.TitleIconPart);

    private static TextBlock TitlePart(Window window) =>
        window.GetVisualDescendants().OfType<TextBlock>().Single(c => c.Name == StandardsWindowTheme.TitlePart);

    [AvaloniaFact]
    public void WindowsLayout_WithIcon_ShowsIt16Px_LeftOfTheTitle_Centred()
    {
        var window = Open(WindowLayoutMode.Windows, icon: true);
        try
        {
            var icon = IconPart(window);
            var title = TitlePart(window);
            Assert.True(icon.IsVisible);
            Assert.NotNull(icon.Source);
            Assert.Equal(16, icon.Bounds.Width);
            Assert.Equal(16, icon.Bounds.Height);
            var iconBox = icon.TranslatePoint(default, window)!.Value;
            var titleBox = title.TranslatePoint(default, window)!.Value;
            Assert.True(iconBox.X + 16 <= titleBox.X);
            var iconMid = iconBox.Y + 8;
            var titleMid = titleBox.Y + title.Bounds.Height / 2;
            Assert.True(Math.Abs(iconMid - titleMid) <= 0.5, $"icon {iconMid} title {titleMid}");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void WithSidebarModule_TheIconComesAfterTheSidebarButton()
    {
        var window = Open(WindowLayoutMode.Windows, icon: true);
        try
        {
            window.Sidebar = new Sidebar();
            Pump(window);
            var toggle = window.GetVisualDescendants().OfType<Button>().Single(b => b.Name == StandardsWindowTheme.SidebarTogglePart);
            Assert.True(toggle.IsVisible);
            Assert.True(IconPart(window).TranslatePoint(default, window)!.Value.X >= toggle.TranslatePoint(new Point(toggle.Bounds.Width, 0), window)!.Value.X);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MacOsLayout_ShowsNoIcon()
    {
        var window = Open(WindowLayoutMode.MacOS, icon: true);
        try
        {
            Assert.False(IconPart(window).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void WithoutIcon_ShowsNone()
    {
        var window = Open(WindowLayoutMode.Windows, icon: false);
        try
        {
            Assert.False(IconPart(window).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FollowsLiveLayoutModeAndIconChanges()
    {
        var window = Open(WindowLayoutMode.MacOS, icon: false);
        try
        {
            var icon = IconPart(window);
            window.Icon = MakeIcon();
            Pump(window);
            Assert.False(icon.IsVisible);

            window.LayoutMode = WindowLayoutMode.Windows;
            Pump(window);
            Assert.True(icon.IsVisible);
            Assert.NotNull(icon.Source);

            window.LayoutMode = WindowLayoutMode.MacOS;
            Pump(window);
            Assert.False(icon.IsVisible);

            window.LayoutMode = WindowLayoutMode.Windows;
            window.Icon = null;
            Pump(window);
            Assert.False(icon.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheIcon_IsDecorative()
    {
        var window = Open(WindowLayoutMode.Windows, icon: true);
        try
        {
            var icon = IconPart(window);
            Assert.False(icon.Focusable);
            Assert.False(icon.IsHitTestVisible);
            Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(icon));
            Assert.Equal(WindowDecorationsElementRole.None, WindowDecorationProperties.GetElementRole(icon));
            // A press on it lands on the title band, which is the title bar.
            var hit = window.GetVisualAt(icon.TranslatePoint(new Point(8, 8), window)!.Value);
            var band = hit?.GetSelfAndVisualAncestors().OfType<Control>().FirstOrDefault(c => c.Name == StandardsWindowTheme.TitleBandPart);
            Assert.NotNull(band);
            Assert.Equal(WindowDecorationsElementRole.TitleBar, WindowDecorationProperties.GetElementRole(band));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("light", "normal")]
    [InlineData("light", "compact")]
    [InlineData("dark", "normal")]
    [InlineData("dark", "compact")]
    public void Renders_TitleBand(string theme, string density)
    {
        var window = Open(WindowLayoutMode.Windows, icon: true,
            theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light,
            density == "compact" ? DensityStyle.Compact : DensityStyle.Normal);
        try
        {
            window.SetRenderScaling(Sheet.Scale);
            Pump(window);
            using var frame = Frame.Capture(window);
            using var crop = Frame.Crop(frame.Bitmap, new PixelRect(0, 0, Math.Min(frame.Width, (int)(360 * Sheet.Scale)), (int)(60 * Sheet.Scale)));
            var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(dir);
            using var stream = File.Create(Path.Combine(dir, $"title-icon-{theme}-{density}.png"));
            crop.Save(stream, new PngBitmapEncoderOptions());
        }
        finally
        {
            window.Close();
        }
    }
}
