using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Scroll shadows: shown along edges with content out of view, the off switch, the clip; ComboBox popup shape.</summary>
public class ScrollShadowTests
{
    [AvaloniaTheory]
    [InlineData(0, false, true)]
    [InlineData(150, true, true)]
    [InlineData(10_000, true, false)]
    public void Vertical_ShowsShadowsTowardsHiddenContent(double offset, bool top, bool bottom)
    {
        var (window, viewer) = Show(Tall());
        try
        {
            viewer.Offset = new Vector(0, offset);
            Pump(window);
            var shadows = Overlay(viewer);
            Assert.Equal(top, Shown(shadows.Top));
            Assert.Equal(bottom, Shown(shadows.Bottom));
            Assert.False(Shown(shadows.Left));
            Assert.False(Shown(shadows.Right));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(0, false, true)]
    [InlineData(150, true, true)]
    [InlineData(10_000, true, false)]
    public void Horizontal_ShowsShadowsTowardsHiddenContent(double offset, bool left, bool right)
    {
        var (window, viewer) = Show(Wide(), horizontal: true);
        try
        {
            viewer.Offset = new Vector(offset, 0);
            Pump(window);
            var shadows = Overlay(viewer);
            Assert.Equal(left, Shown(shadows.Left));
            Assert.Equal(right, Shown(shadows.Right));
            Assert.False(Shown(shadows.Top));
            Assert.False(Shown(shadows.Bottom));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Shadows_FollowScrolling()
    {
        var (window, viewer) = Show(Tall());
        try
        {
            var shadows = Overlay(viewer);
            Assert.False(Shown(shadows.Top));
            viewer.Offset = new Vector(0, 40);
            Pump(window);
            Assert.True(Shown(shadows.Top));
            viewer.Offset = default;
            Pump(window);
            Assert.False(Shown(shadows.Top));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Shadows_FitContent_ShowNone()
    {
        var (window, viewer) = Show(new StackPanel { Children = { new TextBlock { Text = "Short" } } });
        try
        {
            var shadows = Overlay(viewer);
            Assert.All(new[] { shadows.Top, shadows.Bottom, shadows.Left, shadows.Right }, e => Assert.False(Shown(e)));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OffSwitch_HidesTheShadows_OnAScrollViewerAndThroughAListBox()
    {
        var (window, viewer) = Show(Tall());
        var list = new ListBox { Height = 100, ItemsSource = Enumerable.Range(1, 30).Select(i => $"Item {i}").ToList() };
        ScrollShadows.SetIsEnabled(list, false);
        var other = new Window { Width = 200, Height = 200, Content = list };
        other.Show();
        try
        {
            Assert.True(Overlay(viewer).IsVisible);
            ScrollShadows.SetIsEnabled(viewer, false);
            Pump(window);
            Assert.False(Overlay(viewer).IsVisible);

            Pump(other);
            var listViewer = list.GetVisualDescendants().OfType<ScrollViewer>().First();
            Assert.False(Overlay(listViewer).IsVisible);
        }
        finally
        {
            other.Close();
            window.Close();
        }
    }

    // The shadows lie in the viewport: the overlay covers exactly the content cell, and pixels just
    // outside the viewport are untouched while the edge inside it is darkened.
    [AvaloniaFact]
    public void Shadows_AreClippedToTheViewport()
    {
        var (window, viewer) = Show(Tall());
        try
        {
            // Capture the shown shadow, not a frame of its fade.
            foreach (var side in Overlay(viewer).Children)
                side.Transitions = null;
            viewer.Offset = new Vector(0, 150);
            Pump(window);
            var presenter = viewer.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ScrollContentPresenter>().First();
            Assert.Equal(presenter.Bounds, Overlay(viewer).Bounds);

            var viewport = new Rect(viewer.TranslatePoint(default, window)!.Value, viewer.Bounds.Size);
            using var frame = Frame.Capture(window);
            int x = (int)viewport.Center.X;
            var above = frame.Luma(x, (int)viewport.Top - 2);
            var edge = frame.Luma(x, (int)viewport.Top + 1);
            var inside = frame.Luma(x, (int)viewport.Top + 40);
            Assert.True(Math.Abs(above - frame.Luma(x, 2)) < 1, "The page above the viewport changed.");
            Assert.True(edge < inside - 5, $"No shadow at the viewport's top edge ({edge:F0} vs {inside:F0}).");
        }
        finally
        {
            window.Close();
        }
    }

    // The restyles extend Fluent's templates: if a Fluent change stops a part being found, the
    // added part goes missing and these fail.
    [AvaloniaFact]
    public void ExtendedTemplates_KeepFluentsPartsAndAddTheirOwn()
    {
        INameScope? viewerScope = null;
        var viewer = new ScrollViewer { Width = 200, Height = 120, Content = Tall() };
        viewer.TemplateApplied += (_, e) => viewerScope = e.NameScope;
        var window = new Window { Width = 300, Height = 220, Content = viewer };
        window.Show();
        try
        {
            Pump(window);
            Assert.Single(viewer.GetVisualDescendants().OfType<ScrollShadowOverlay>(), o => o.Name == ScrollViewerTheme.ShadowsPart);
            Assert.IsType<Avalonia.Controls.Presenters.ScrollContentPresenter>(viewerScope!.Find("PART_ContentPresenter"));
        }
        finally
        {
            window.Close();
        }

        INameScope? boxScope = null;
        var box = new ComboBox { ItemsSource = new[] { "One" } };
        box.TemplateApplied += (_, e) => boxScope = e.NameScope;
        var boxWindow = new Window { Width = 300, Height = 300, Content = box };
        boxWindow.Show();
        try
        {
            box.IsDropDownOpen = true;
            Pump(boxWindow);
            var popup = box.GetVisualDescendants().OfType<Popup>().Single();
            Assert.Equal(ComboBoxTheme.PopupChromePart, Assert.IsType<SquircleBorder>(popup.Child).Name);
            Assert.Same(popup, boxScope!.Find("PART_Popup"));
        }
        finally
        {
            box.IsDropDownOpen = false;
            boxWindow.Close();
        }
    }

    // The ComboBox drop-down is a squircle: hover and selected fills stay inside its curved corners.
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void ComboBoxDropDown_KeepsItemFillsInsideItsShape(string theme)
    {
        var box = new ComboBox { ItemsSource = new[] { "One", "Two", "Three" }, SelectedIndex = 2, Width = 220 };
        var window = new Window { Width = 320, Height = 320, RequestedThemeVariant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, Content = box };
        window.Show();
        try
        {
            box.IsDropDownOpen = true;
            Pump(window);
            var items = box.GetLogicalDescendants().OfType<ComboBoxItem>().ToList();
            foreach (var item in items)
                foreach (var part in item.GetVisualDescendants().OfType<Avalonia.Animation.Animatable>())
                    part.Transitions = null;
            ((IPseudoClasses)items[0].Classes).Set(":pointerover", true);
            Pump(window);

            var chrome = Assert.IsType<SquircleBorder>(box.GetVisualDescendants().OfType<Popup>().Single().Child);
            Assert.Equal(ComboBoxTheme.PopupChromePart, chrome.Name);
            var root = TopLevel.GetTopLevel(chrome)!;
            Pump(window);
            using var frame = new FrameOf(root);

            var popup = new Rect(chrome.TranslatePoint(default, root)!.Value, chrome.Bounds.Size);
            var hover = Colours.Effective(items[0].GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart).Background);
            var selected = Colours.Effective(items[2].GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart).Background);

            // Inside the items: their fills. In the popup's outer corners: no item fill.
            var firstItem = new Rect(items[0].TranslatePoint(default, root)!.Value, items[0].Bounds.Size);
            Assert.Equal(Rgb(hover), frame[(int)firstItem.Center.X, (int)firstItem.Center.Y]);
            Assert.True(firstItem.Left >= popup.Left + 4 && firstItem.Top >= popup.Top + 4, "Items are not inset in the popup.");
            foreach (var (cx, cy) in new[] { (popup.Left + 1, popup.Top + 1), (popup.Right - 2, popup.Top + 1), (popup.Left + 1, popup.Bottom - 2), (popup.Right - 2, popup.Bottom - 2) })
            {
                var p = frame[(int)cx, (int)cy];
                Assert.NotEqual(Rgb(hover), p);
                Assert.NotEqual(Rgb(selected), p);
            }
        }
        finally
        {
            box.IsDropDownOpen = false;
            window.Close();
        }
    }

    // --- Helpers -------------------------------------------------------------------------

    private static Control Tall() => new StackPanel { Children = { new Border { Height = 600, Background = Brushes.Transparent } } };

    private static Control Wide() => new StackPanel { Orientation = Orientation.Horizontal, Children = { new Border { Width = 600, Height = 60 } } };

    private static (Window Window, ScrollViewer Viewer) Show(Control content, bool horizontal = false)
    {
        var viewer = new ScrollViewer
        {
            Width = 200,
            Height = 120,
            Content = content,
            HorizontalScrollBarVisibility = horizontal ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = horizontal ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Hidden,
        };
        var window = new Window
        {
            Width = 300, Height = 220, RequestedThemeVariant = ThemeVariant.Light,
            Content = new Border { Padding = new Thickness(40), Child = viewer },
        };
        window.Show();
        Pump(window);
        return (window, viewer);
    }

    // The shadow is the accent's hue and saturation at lightness 0.35 and 8 % (light) or 0.12 and
    // 18 % (dark), fading to the same colour fully transparent, and it follows accent changes.
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Shadow_IsADarkenedAccent_AndFollowsAccentChanges(string theme)
    {
        var dark = theme == "Dark";
        var standards = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var (window, viewer) = Show(Tall());
        window.Styles.Add(standards);
        window.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        try
        {
            var shadows = Overlay(viewer);
            foreach (var accent in new[] { standards.Accent, Color.Parse("#C42B1C") })
            {
                standards.Accent = accent;
                Pump(window);
                var edge = shadows.EdgeColor;
                var hsl = edge.ToHsl();
                Assert.InRange(Math.Abs(accent.ToHsl().H - hsl.H), 0, 2);
                Assert.InRange(Math.Abs((dark ? 0.12 : 0.35) - hsl.L), 0, 0.01);
                Assert.Equal(dark ? 46 : 20, edge.A);
                var stops = ((LinearGradientBrush)shadows.Bottom.Background!).GradientStops;
                Assert.Equal(edge, stops[0].Color);
                Assert.Equal(Color.FromArgb(0, edge.R, edge.G, edge.B), stops[1].Color);
            }
        }
        finally
        {
            window.Close();
        }
    }

    // The shadows fade by opacity only, over a gradient that keeps its colour, so every fade frame
    // composited over the surface lies between the surface and the full shadow: no overshoot.
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void ShadowFades_NeverOvershootTheirEnds(string theme)
    {
        var (window, viewer) = Show(Tall());
        window.RequestedThemeVariant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        try
        {
            Pump(window);
            var shadows = Overlay(viewer);
            foreach (var edge in new[] { shadows.Top, shadows.Bottom, shadows.Left, shadows.Right })
            {
                var fade = Assert.Single(edge.Transitions!.OfType<Avalonia.Animation.TransitionBase>());
                Assert.Equal(Visual.OpacityProperty, fade.Property);
            }

            Assert.True(window.TryFindResource("SystemRegionColor", window.ActualThemeVariant, out var region));
            var surface = (Color)region!;
            var colour = shadows.EdgeColor;
            var full = Colours.Over(colour, surface);
            for (var step = 0; step <= 50; step++)
            {
                var frame = Colours.Over(Color.FromArgb((byte)Math.Round(colour.A * step / 50.0), colour.R, colour.G, colour.B), surface);
                foreach (var (f, a, b) in new[] { (frame.R, surface.R, full.R), (frame.G, surface.G, full.G), (frame.B, surface.B, full.B) })
                    Assert.InRange(f, Math.Min(a, b) - 1, Math.Max(a, b) + 1);
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static ScrollShadowOverlay Overlay(ScrollViewer viewer) =>
        viewer.GetVisualDescendants().OfType<ScrollShadowOverlay>().Single();

    // The edge's target opacity (its base value; a fade may still be running).
    private static bool Shown(Border edge) => edge.GetBaseValue(Visual.OpacityProperty) is { HasValue: true } v && (double)v.Value! > 0;

    private static (byte, byte, byte) Rgb(Color c) => (c.R, c.G, c.B);

    private static void Pump(TopLevel window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    // A captured frame of any top level (the popup's own root).
    private sealed class FrameOf : IDisposable
    {
        private readonly Frame _frame;

        public FrameOf(TopLevel root) => _frame = Frame.Of(root.CaptureRenderedFrame()!);

        public (byte R, byte G, byte B) this[int x, int y] => _frame[x, y];

        public void Dispose() => _frame.Dispose();
    }
}
