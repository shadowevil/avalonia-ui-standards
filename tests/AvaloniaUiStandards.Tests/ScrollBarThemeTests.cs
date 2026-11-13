using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>ScrollBar: overlay pill bar with no line buttons, proximity widening, idle hiding, dragging, paging, motion.</summary>
public class ScrollBarThemeTests
{
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Bar_HasNoLineButtons_OnlyTheTrackAndPageAreas(bool vertical)
    {
        var (window, _, bar) = Show(vertical);
        try
        {
            var buttons = bar.GetVisualDescendants().OfType<RepeatButton>().Select(b => b.Name).Order().ToList();
            Assert.Equal(["PART_PageDownButton", "PART_PageUpButton"], buttons);
            Assert.Single(bar.GetVisualDescendants().OfType<Track>());
            Assert.Empty(bar.GetVisualDescendants().OfType<PathIcon>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThumbAndTrack_AreFullyRounded_AtEverySize(bool vertical)
    {
        var (window, _, bar) = Show(vertical, reduceMotion: true);
        try
        {
            var chrome = Chrome(bar);
            foreach (var proximity in new[] { 0, 0.25, 0.5, 0.75, 1 })
            {
                chrome.SetProximity(proximity);
                Pump(window);
                foreach (var pill in new[] { chrome.ThumbPill, chrome.TrackPill })
                {
                    var half = Math.Min(pill.Bounds.Width, pill.Bounds.Height) / 2;
                    Assert.True(half > 0, $"{pill.Name} has no size");
                    Assert.Equal(new CornerRadius(half), pill.CornerRadius);
                    Assert.IsType<PillBorder>(pill);
                }
            }
        }
        finally
        {
            window.Close();
        }
    }

    // The pills sit SpacingS in from the scroller's edge, their ends clear of the panel corner
    // (half CornerRadiusOverlay); the grab area runs to the edge.
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Pills_SitInsetFromTheEdge_AndClearThePanelCorners(bool vertical)
    {
        var (window, viewer, bar) = Show(vertical, reduceMotion: true);
        try
        {
            var gap = Spacing.S;
            var chrome = Chrome(bar);
            chrome.SetProximity(1);
            Pump(window);

            Assert.Equal(ScrollBarTheme.Thickness, vertical ? bar.Bounds.Width : bar.Bounds.Height);
            var barEdge = bar.TranslatePoint(new Point(bar.Bounds.Width, bar.Bounds.Height), viewer)!.Value;
            Assert.Equal(new Point(viewer.Bounds.Width, viewer.Bounds.Height), vertical ? barEdge.WithY(viewer.Bounds.Height) : barEdge.WithX(viewer.Bounds.Width));
            foreach (var pill in new[] { chrome.ThumbPill, chrome.TrackPill })
            {
                var topLeft = pill.TranslatePoint(default, bar)!.Value;
                var outerGap = vertical ? bar.Bounds.Width - (topLeft.X + pill.Bounds.Width) : bar.Bounds.Height - (topLeft.Y + pill.Bounds.Height);
                Assert.Equal(gap, outerGap, 3);
            }

            var end = ScrollBarTheme.EndInset(new CornerRadius(16));
            Assert.Equal(Math.Max(gap, 8), end);
            var track = chrome.TrackPill;
            var start = track.TranslatePoint(default, bar)!.Value;
            var length = vertical ? bar.Bounds.Height : bar.Bounds.Width;
            Assert.Equal(end, vertical ? start.Y : start.X, 3);
            Assert.Equal(end, length - (vertical ? start.Y + track.Bounds.Height : start.X + track.Bounds.Width), 3);
        }
        finally
        {
            window.Close();
        }
    }

    // The pointer approaching the bar's edge from the content widens the thumb with closeness, up to
    // the full width on the bar, with the track showing.
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Width_GrowsWithPointerProximity_AndReachesTheTargetOnHover(bool vertical)
    {
        var (window, _, bar) = Show(vertical, reduceMotion: true);
        try
        {
            var chrome = Chrome(bar);
            double Across(Control c) => vertical ? c.Bounds.Width : c.Bounds.Height;
            Point At(double distance)
            {
                var size = bar.Bounds.Size;
                var local = vertical ? new Point(-distance, size.Height / 2) : new Point(size.Width / 2, -distance);
                return bar.TranslatePoint(local, window)!.Value;
            }

            var widths = new List<double>();
            var opacities = new List<double>();
            foreach (var distance in new[] { 40.0, 18, 6, -4 })
            {
                window.MouseMove(At(distance));
                Pump(window);
                widths.Add(Across(chrome.ThumbPill));
                opacities.Add(chrome.ThumbPill.Opacity);
            }
            Assert.True(opacities[0] == 0 && opacities[1] > 0 && opacities[3] == 1, string.Join(", ", opacities));

            Assert.Equal(ScrollBarTheme.ThinWidth, widths[0]);
            Assert.True(widths[1] > widths[0] && widths[2] > widths[1], string.Join(", ", widths));
            Assert.Equal(ScrollBarTheme.FullWidth, widths[3]);
            Assert.True(bar.IsPointerOver);
            Assert.Equal(1, chrome.TrackPill.Opacity);
            Assert.Equal(ScrollBarTheme.FullWidth, Across(chrome.TrackPill));

            window.MouseMove(new Point(1, 1));
            Pump(window);
            Assert.False(bar.IsPointerOver);
            Assert.Equal(0, chrome.Proximity);
            Assert.Equal(0, chrome.TrackPill.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Thumb_ShowsThinWhileScrolling_AndHidesAfterIdle()
    {
        var (window, viewer, bar) = Show(vertical: true, reduceMotion: true);
        try
        {
            var chrome = Chrome(bar);
            Assert.Equal(0, chrome.ThumbPill.Opacity);
            viewer.Offset = new Vector(0, 60);
            Pump(window);
            Assert.Equal(1, chrome.ThumbPill.Opacity);
            Assert.Equal(ScrollBarTheme.ThinWidth, chrome.ThumbPill.Bounds.Width);

            // Still shown just before the delay, hidden after it (the real loop runs the timer).
            RunLoop(ScrollBarTheme.HideDelay - TimeSpan.FromMilliseconds(300));
            Assert.Equal(1, chrome.ThumbPill.Opacity);
            RunLoop(TimeSpan.FromMilliseconds(600));
            Assert.Equal(0, chrome.ThumbPill.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Thumb_StaysShown_WhenAutoHideIsOff()
    {
        var (window, viewer, bar) = Show(vertical: true, reduceMotion: true, allowAutoHide: false);
        try
        {
            Assert.Equal(1, Chrome(bar).ThumbPill.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void DraggingTheThumb_Scrolls(bool vertical)
    {
        var (window, viewer, bar) = Show(vertical, reduceMotion: true);
        try
        {
            var thumb = bar.GetVisualDescendants().OfType<Thumb>().Single();
            var start = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), window)!.Value;
            var end = vertical ? start + new Point(0, 30) : start + new Point(30, 0);
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            Pump(window);
            Assert.Equal(ScrollBarTheme.FullWidth, vertical ? Chrome(bar).ThumbPill.Bounds.Width : Chrome(bar).ThumbPill.Bounds.Height);
            window.MouseMove(end);
            window.MouseUp(end, MouseButton.Left);
            Pump(window);
            var offset = vertical ? viewer.Offset.Y : viewer.Offset.X;
            Assert.True(offset > 0, $"offset {offset}");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ClickingTheTrack_Pages()
    {
        var (window, viewer, bar) = Show(vertical: true, reduceMotion: true);
        try
        {
            var point = bar.TranslatePoint(new Point(bar.Bounds.Width / 2, bar.Bounds.Height - 12), window)!.Value;
            window.MouseMove(point);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Pump(window);
            Assert.Equal(viewer.Viewport.Height, viewer.Offset.Y, 1);
        }
        finally
        {
            window.Close();
        }
    }

    // Under reduce motion the parts change at once (no transitions); otherwise opacity, width,
    // height and colour fade over MotionFast, the colour through the cross-fade transition.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Motion_FollowsReduceMotion(bool reduceMotion)
    {
        var (window, _, bar) = Show(vertical: true, reduceMotion: reduceMotion);
        try
        {
            foreach (var pill in new[] { Chrome(bar).ThumbPill, Chrome(bar).TrackPill })
            {
                var transitions = pill.Transitions!.OfType<TransitionBase>().ToList();
                if (reduceMotion)
                {
                    Assert.Empty(transitions);
                    continue;
                }
                Assert.Equal([Visual.OpacityProperty, Layoutable.WidthProperty, Layoutable.HeightProperty, Border.BackgroundProperty],
                    transitions.Select(t => t.Property!));
                Assert.All(transitions, t => Assert.Equal(TimeSpan.FromMilliseconds(100), t.Duration));
                Assert.IsType<CrossFadeBrushTransition>(transitions[3]);
            }

            if (reduceMotion)
            {
                Chrome(bar).SetProximity(1);
                Assert.Equal(1, Chrome(bar).ThumbPill.Opacity);
                Assert.Equal(ScrollBarTheme.FullWidth, Chrome(bar).ThumbPill.Width);
            }
        }
        finally
        {
            window.Close();
        }
    }

    // Every frame of a thumb colour change (rest, hover, drag) and of a track or thumb fade,
    // composited over the surface, stays between its two ends in every channel.
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Fades_NeverOvershootTheirEnds(string theme)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, _, bar) = Show(vertical: true, variant: variant);
        try
        {
            var chrome = Chrome(bar);
            var brushes = new IBrush?[] { chrome.ThumbBrush, chrome.ThumbHoverBrush, chrome.ThumbDragBrush };
            foreach (var surfaceKey in new[] { "SystemRegionColor", "SystemChromeMediumLowColor" })
            {
                Assert.True(window.TryFindResource(surfaceKey, variant, out var value));
                var surface = Effective(value);
                var pairs = new List<(IBrush? From, IBrush? To)>();
                foreach (var a in brushes)
                    foreach (var b in brushes)
                        if (a != b)
                            pairs.Add((a, b));
                pairs.Add((null, chrome.ThumbBrush));
                pairs.Add((null, chrome.TrackPill.Background));
                foreach (var (from, to) in pairs)
                {
                    var start = Over(from is null ? Colors.Transparent : Effective(from), surface);
                    var end = Over(Effective(to), surface);
                    for (var step = 0; step <= 50; step++)
                    {
                        var frame = Over(Effective(CrossFadeBrushTransition.At(step / 50.0, from, to) ?? Brushes.Transparent), surface);
                        foreach (var (f, s, e) in new[] { (frame.R, start.R, end.R), (frame.G, start.G, end.G), (frame.B, start.B, end.B) })
                            Assert.InRange(f, Math.Min(s, e) - 1, Math.Max(s, e) + 1);
                    }
                }
            }
        }
        finally
        {
            window.Close();
        }
    }

    // The bars overlay the content: the content presenter keeps the whole viewer, with auto-hide
    // on or off and with both bars showing.
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Bars_TakeNoLayoutSpace(bool allowAutoHide)
    {
        var (window, viewer, _) = Show(vertical: true, allowAutoHide: allowAutoHide, both: true);
        try
        {
            var presenter = viewer.GetVisualDescendants().OfType<ScrollContentPresenter>().Single();
            var bars = viewer.GetVisualDescendants().OfType<ScrollBar>().ToList();
            Assert.Equal(2, bars.Count(b => b.IsVisible));
            Assert.Equal(new Rect(viewer.Bounds.Size), presenter.Bounds);
            Assert.Equal(viewer.Bounds.Size, viewer.Viewport);
            var shadows = viewer.GetVisualDescendants().OfType<ScrollShadowOverlay>().Single();
            Assert.Equal(presenter.Bounds, shadows.Bounds);
            Assert.Equal(ScrollBarTheme.Thickness, bars.Single(b => b.Orientation == Orientation.Vertical).Bounds.Width);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Helpers -------------------------------------------------------------------------

    private static (Window Window, ScrollViewer Viewer, ScrollBar Bar) Show(bool vertical, bool reduceMotion = false,
        ThemeVariant? variant = null, bool allowAutoHide = true, bool both = false)
    {
        var lines = new StackPanel();
        foreach (var i in Enumerable.Range(1, 30))
            lines.Children.Add(new TextBlock { Text = $"Line {i}: list content that runs wide", Margin = new Thickness(8, 4) });
        var viewer = new ScrollViewer
        {
            Width = 200,
            Height = 120,
            Content = both ? new Border { Width = 600, Child = lines } : vertical ? lines : new Border { Width = 800, Height = 60 },
            AllowAutoHide = allowAutoHide,
            HorizontalScrollBarVisibility = vertical && !both ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = vertical || both ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
        };
        var window = new Window
        {
            Width = 300, Height = 220, RequestedThemeVariant = variant ?? ThemeVariant.Light,
            Content = new Border { Padding = new Thickness(40), Child = viewer },
        };
        window.Styles.Add(new StandardsTheme(new OsAccessibility.Settings(reduceMotion, false)));
        window.Show();
        Pump(window);
        var bar = viewer.GetVisualDescendants().OfType<ScrollBar>().Single(b => b.Name == (vertical ? "PART_VerticalScrollBar" : "PART_HorizontalScrollBar"));
        return (window, viewer, bar);
    }

    private static void RunLoop(TimeSpan duration)
    {
        using var stop = new CancellationTokenSource(duration);
        Dispatcher.UIThread.MainLoop(stop.Token);
    }

    private static ScrollBarChrome Chrome(ScrollBar bar) => bar.GetVisualDescendants().OfType<ScrollBarChrome>().Single();

    private static void Pump(TopLevel window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
