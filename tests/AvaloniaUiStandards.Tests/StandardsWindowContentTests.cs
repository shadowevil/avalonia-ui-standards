using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
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

/// <summary>StandardsWindow's backdrop slot and its self-scrolling glass content panel.</summary>
public class StandardsWindowContentTests
{
    private const double Padding = 12;

    // --- Backdrop ------------------------------------------------------------------------

    [AvaloniaFact]
    public void Backdrop_IsEmptyByDefault()
    {
        var window = Show(NewWindow(new TextBlock { Text = "Content" }));
        try
        {
            Assert.Null(window.Backdrop);
            Assert.False(Part<ContentPresenter>(window, StandardsWindowTheme.BackdropPart).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Backdrop_DrawsBehindTheShell_InsideTheFrame()
    {
        var window = NewWindow(new TextBlock { Text = "Content" });
        window.Backdrop = new Border { Background = Brushes.Red };
        Show(window);
        try
        {
            // Behind the title band, sidebar, status bar and panel: first in the frame.
            var frame = Part<SquircleBorder>(window, StandardsWindowTheme.FramePart);
            var layers = Assert.IsType<Panel>(frame.Child);
            Assert.Equal(StandardsWindowTheme.BackdropPart, Assert.IsType<ContentPresenter>(layers.Children[0]).Name);

            var frameBounds = new Rect(frame.TranslatePoint(default, window)!.Value, frame.Bounds.Size);
            var title = Part<Control>(window, StandardsWindowTheme.TitleBandPart);
            var titleCentre = title.TranslatePoint(new Point(title.Bounds.Width / 2, title.Bounds.Height / 2), window)!.Value;
            using var capture = Frame.Capture(window);
            Assert.True(IsRed(capture[(int)titleCentre.X, (int)titleCentre.Y]), "The backdrop does not show behind the title band.");
            // Inside the frame's shape only: outside the frame and in its rounded corner tip, no backdrop.
            Assert.False(IsRed(capture[(int)frameBounds.Left - 4, (int)frameBounds.Center.Y]));
            Assert.False(IsRed(capture[(int)frameBounds.Left + 1, (int)frameBounds.Top + 1]));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Backdrop_IsBlurredByTheGlassPanel()
    {
        var window = NewWindow(new Border());
        window.Backdrop = Backdrop.Stripes(period: 8);
        Show(window);
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            var title = Part<Control>(window, StandardsWindowTheme.TitleBandPart);
            var panelRow = panel.TranslatePoint(new Point(40, panel.Bounds.Height / 2), window)!.Value;
            var titleRow = title.TranslatePoint(new Point(panel.Bounds.X + 40, 4), window)!.Value;
            using var capture = Frame.Capture(window);
            // Sharp stripes behind the title band; blurred smooth under the glass.
            Assert.True(Spread(capture, titleRow, 32) > 100, "The stripes are not behind the title band.");
            Assert.True(Spread(capture, panelRow, 32) < 30, "The glass does not blur the backdrop.");
        }
        finally
        {
            window.Close();
        }
    }

    // --- Content panel bounds ----------------------------------------------------------

    [AvaloniaFact]
    public void ContentPanelBounds_IsThePanelInTheBackdropsCoordinates()
    {
        var backdrop = new Border();
        var window = NewWindow(new TextBlock { Text = "Content" });
        window.Backdrop = backdrop;
        Show(window);
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            Assert.Equal(new Rect(panel.TranslatePoint(default, backdrop)!.Value, panel.Bounds.Size), window.ContentPanelBounds);
            Assert.True(window.ContentPanelBounds.Width > 0 && window.ContentPanelBounds.Height > 0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ContentPanelBounds_FollowsResizeMaximiseAndDensity()
    {
        var backdrop = new Border();
        var window = NewWindow(new TextBlock { Text = "Content" });
        window.Backdrop = backdrop;
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        window.Styles.Add(theme);
        var changes = new List<Rect>();
        window.PropertyChanged += (_, e) =>
        {
            if (e.Property == StandardsWindow.ContentPanelBoundsProperty)
                changes.Add((Rect)e.NewValue!);
        };
        Show(window);
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            void Check(string step, Action change)
            {
                var before = window.ContentPanelBounds;
                var count = changes.Count;
                change();
                Pump(window);
                Assert.NotEqual(before, window.ContentPanelBounds);
                Assert.True(changes.Count > count, $"{step}: no property change");
                Assert.Equal(window.ContentPanelBounds, changes[^1]);
                Assert.Equal(new Rect(panel.TranslatePoint(default, backdrop)!.Value, panel.Bounds.Size), window.ContentPanelBounds);
            }

            Check("resize", () => window.Width = 800);
            Check("density", () => theme.Density = DensityStyle.Compact);
            Check("maximise", () => window.WindowState = WindowState.Maximized);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Self-scrolling content panel -------------------------------------------------

    [AvaloniaFact]
    public void Content_ReachesThePanelEdge_WhenScrolled()
    {
        var page = Tall();
        var window = Show(NewWindow(page));
        try
        {
            var (panel, scroller) = Panel(window);
            var inner = Inner(window, panel);
            var x = (int)inner.Center.X;

            using (var top = Frame.Capture(window))
                Assert.False(IsRed(top[x, (int)inner.Top + 4]), "At the top, the padding should show above the content.");

            scroller.Offset = new Vector(0, 400);
            Pump(window);
            using var scrolled = Frame.Capture(window);
            Assert.True(IsRed(scrolled[x, (int)inner.Top]), "Scrolled, the content does not reach the panel's top edge.");
            Assert.True(IsRed(scrolled[x, (int)inner.Bottom - 1]), "The content does not reach the panel's bottom edge.");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Padding_IsInsideTheScrolledContent()
    {
        var page = Tall();
        var window = Show(NewWindow(page));
        try
        {
            var (panel, scroller) = Panel(window);
            Assert.Equal(new Thickness(0), panel.Padding);
            var inner = Inner(window, panel);

            Assert.Equal(inner.Top + Padding, page.TranslatePoint(default, window)!.Value.Y, 1);
            Assert.Equal(inner.Left + Padding, page.TranslatePoint(default, window)!.Value.X, 1);

            scroller.Offset = new Vector(0, double.MaxValue);
            Pump(window);
            Assert.Equal(inner.Bottom - Padding, page.TranslatePoint(new Point(0, page.Bounds.Height), window)!.Value.Y, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ScrollBarAndShadows_SitAtThePanelEdge_Clipped()
    {
        var window = NewWindow(Tall());
        Show(window);
        try
        {
            var (panel, scroller) = Panel(window);
            var inner = Inner(window, panel);
            Assert.Equal(inner, Bounds(window, scroller));

            var bar = scroller.GetVisualDescendants().OfType<ScrollBar>().Single(b => b.Orientation == Orientation.Vertical);
            Assert.Equal(inner.Right, Bounds(window, bar).Right, 1);

            var shadows = scroller.GetVisualDescendants().OfType<ScrollShadowOverlay>().Single();
            Assert.Equal(inner, Bounds(window, shadows));

            // Scrolled, the top shadow shows at the panel's edge and nothing changes above it.
            var x = (int)inner.Center.X;
            using var before = Frame.Capture(window);
            foreach (var edge in shadows.Children)
                edge.Transitions = null;
            scroller.Offset = new Vector(0, 400);
            Pump(window);
            using var after = Frame.Capture(window);
            Assert.True(shadows.Children.Any(e => e.Opacity > 0), "No scroll shadow while scrolled.");
            for (var y = (int)inner.Top - 6; y < (int)inner.Top - 1; y++)
                Assert.Equal(before[x, y], after[x, y]);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OffSwitch_GivesTheContentThePanelsSize_InsideThePadding()
    {
        var list = new ListBox { ItemsSource = Enumerable.Range(1, 100).Select(i => $"Item {i}").ToList() };
        var window = NewWindow(list);
        window.IsContentScrollable = false;
        Show(window);
        try
        {
            var (panel, scroller) = Panel(window);
            Assert.Equal(ScrollBarVisibility.Disabled, scroller.VerticalScrollBarVisibility);
            var inner = Inner(window, panel);
            Assert.Equal(inner.Deflate(Padding), Bounds(window, list));

            // The list scrolls itself: its own scroller has more content than room.
            var own = list.GetVisualDescendants().OfType<ScrollViewer>().First();
            Assert.True(own.Extent.Height > own.Viewport.Height);

            window.IsContentScrollable = true;
            Pump(window);
            Assert.Equal(ScrollBarVisibility.Auto, scroller.VerticalScrollBarVisibility);
        }
        finally
        {
            window.Close();
        }
    }

    // The padding is inside the scrolled content, so a focused control at the top of the page
    // shows its whole ring; scrolled partly out of view, the panel cuts the ring at its edge.
    [AvaloniaFact]
    public void FocusRing_ShowsWhole_AtTheTopOfTheContent_AndIsCutAtThePanelEdge()
    {
        var button = new Button { Content = "Focus me" };
        var page = new StackPanel { Children = { button, new Border { Height = 2000 } } };
        var window = Show(NewWindow(page));
        try
        {
            var (panel, scroller) = Panel(window);
            var inner = Inner(window, panel);
            button.Focus(NavigationMethod.Tab);
            Pump(window);

            var top = button.TranslatePoint(default, window)!.Value;
            using (var whole = Frame.Capture(window))
                Assert.True(RingPixels(whole, (int)top.X + 10, (int)top.X + 40, (int)top.Y - 4, (int)top.Y - 1) > 0, "The ring above the button is missing.");

            scroller.Offset = new Vector(0, Padding + 10);
            Pump(window);
            using var cut = Frame.Capture(window);
            Assert.Equal(0, RingPixels(cut, (int)inner.Left, (int)inner.Right, (int)inner.Top - 8, (int)inner.Top - 1));
        }
        finally
        {
            window.Close();
        }
    }

    // --- Helpers ---------------------------------------------------------------------

    private static StandardsWindow NewWindow(object content) => new()
    {
        Title = "Content",
        Width = 640,
        Height = 420,
        RequestedThemeVariant = ThemeVariant.Light,
        WindowButtonStyle = WindowButtonStyle.CaptionButtons,
        Content = content,
    };

    // A red page taller than the panel.
    private static Border Tall() => new() { Height = 2000, Background = Brushes.Red };

    private static (GlassBorder Panel, ScrollViewer Scroller) Panel(StandardsWindow window) =>
        (Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart), Part<ScrollViewer>(window, StandardsWindowTheme.ContentScrollViewerPart));

    // The panel inside its 1 px border.
    private static Rect Inner(Window window, GlassBorder panel) => Bounds(window, panel).Deflate(1);

    private static Rect Bounds(Window window, Visual visual) => new(visual.TranslatePoint(default, window)!.Value, visual.Bounds.Size);

    private static bool IsRed((byte R, byte G, byte B) p) => p.R > 150 && p.G < 110 && p.B < 110;

    // The largest luma difference along a run of pixels.
    private static double Spread(Frame frame, Point start, int length)
    {
        var values = Enumerable.Range(0, length).Select(i => frame.Luma((int)start.X + i, (int)start.Y)).ToList();
        return values.Max() - values.Min();
    }

    // The focus ring is the accent, #007CEA.
    private static int RingPixels(Frame frame, int x0, int x1, int y0, int y1)
    {
        var count = 0;
        for (var y = Math.Max(0, y0); y <= y1; y++)
            for (var x = Math.Max(0, x0); x <= x1; x++)
            {
                var (r, g, b) = frame[x, y];
                if (r < 40 && Math.Abs(g - 0x7C) < 30 && b > 200)
                    count++;
            }
        return count;
    }

    private static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static StandardsWindow Show(StandardsWindow window)
    {
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
}
