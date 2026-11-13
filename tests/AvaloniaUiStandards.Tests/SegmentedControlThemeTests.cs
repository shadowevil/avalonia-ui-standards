using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>SegmentedControl: shape, sizes, keyboard, the sliding pill, focus ring, colours and motion.</summary>
public class SegmentedControlThemeTests
{
    [AvaloniaFact]
    public void Theme_DrawsTrackPillAndEqualSegments()
    {
        var (window, control, _, _) = Show(Items("Day", "Week", "Month", "Year"));
        try
        {
            Assert.Equal(new CornerRadius(8), Part(control, SegmentedControlTheme.TrackPart).CornerRadius);
            Assert.Equal(new CornerRadius(6), Part(control, SegmentedControlTheme.PillPart).CornerRadius);
            Assert.Equal(32, control.Bounds.Height);
            Assert.Null(control.FocusAdorner);

            // Equal-width segments inset 2 px from the track, side by side.
            var widths = Enumerable.Range(0, 4).Select(i => Segment(control, i).Bounds.Width).ToList();
            Assert.All(widths, w => Assert.Equal(widths[0], w, 3));
            Assert.Equal(new Point(2, 2), Segment(control, 0).TranslatePoint(default, control));
            Assert.Equal(control.Bounds.Width - 4, widths.Sum(), 3);

            // One pill element, under the selected segment, the same size as it.
            Assert.Single(control.GetVisualDescendants().OfType<SquircleBorder>(), b => b.Name == SegmentedControlTheme.PillPart);
            AssertPillOn(control, 0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Normal", 32, 12)]
    [InlineData("Compact", 24, 8)]
    public void Size_FollowsDensity(string density, double height, double side)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal };
        var (window, control, _, _) = Show(Items("Day", "Week"), theme: theme);
        try
        {
            Assert.Equal(height, control.Bounds.Height);
            Assert.Equal(height - 4, Segment(control, 0).Bounds.Height);
            Assert.Equal(new Thickness(side, 0), Segment(control, 0).Padding);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Keyboard_OneTabStop()
    {
        var (window, control, before, after) = Show(Items("Day", "Week", "Month"), selected: 1);
        try
        {
            before.Focus(NavigationMethod.Tab);
            Press(window, Key.Tab);
            Assert.Same(Segment(control, 1), Focused(window));
            Press(window, Key.Tab);
            Assert.Same(after, Focused(window));
            Press(window, Key.Tab, RawInputModifiers.Shift);
            Assert.Same(Segment(control, 1), Focused(window));
            Press(window, Key.Tab, RawInputModifiers.Shift);
            Assert.Same(before, Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Keyboard_ArrowsMoveAndSelect_StoppingAtTheEnds_HomeAndEnd()
    {
        var (window, control, _, _) = Show(Items("Day", "Week", "Month", "Year"), selected: 1);
        try
        {
            Segment(control, 1).Focus(NavigationMethod.Tab);
            foreach (var (key, expected) in new[]
                     {
                         (Key.Right, 2), (Key.Right, 3), (Key.Right, 3), (Key.Left, 2), (Key.Home, 0), (Key.Left, 0),
                         (Key.End, 3), (Key.Up, 2), (Key.Down, 3),
                     })
            {
                Press(window, key);
                Assert.Equal(expected, control.SelectedIndex);
                Assert.Same(Segment(control, expected), Focused(window));
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Keyboard_SkipsDisabledSegments_AndFollowsRightToLeft()
    {
        var (window, control, _, _) = Show(Items("Day", "Week", "Month", "Year"), selected: 0);
        try
        {
            Segment(control, 1).IsEnabled = false;
            Segment(control, 3).IsEnabled = false;
            Segment(control, 0).Focus(NavigationMethod.Tab);
            Press(window, Key.Right);
            Assert.Equal(2, control.SelectedIndex);
            Press(window, Key.End);
            Assert.Equal(2, control.SelectedIndex);

            control.FlowDirection = FlowDirection.RightToLeft;
            Press(window, Key.Right);
            Assert.Equal(0, control.SelectedIndex);
            Press(window, Key.Left);
            Assert.Equal(2, control.SelectedIndex);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Selection_IsAlwaysKept()
    {
        var (window, control, _, _) = Show(Items("Day", "Week"), selected: -1);
        try
        {
            Assert.Equal(0, control.SelectedIndex);
            control.SelectedIndex = -1;
            Pump(window);
            Assert.NotEqual(-1, control.SelectedIndex);
        }
        finally
        {
            window.Close();
        }
    }

    // Ctrl+click (Cmd on macOS) on the selected segment keeps it selected; a plain click selects.
    [AvaloniaTheory]
    [InlineData(RawInputModifiers.Control)]
    [InlineData(RawInputModifiers.Meta)]
    [InlineData(RawInputModifiers.Shift)]
    public void Selection_ToggleClickKeepsTheSelectedSegment(RawInputModifiers modifiers)
    {
        var (window, control, _, _) = Show(Items("Day", "Week", "Month", "Year"), selected: 2);
        try
        {
            Click(window, control, 2, modifiers);
            Assert.Equal(2, control.SelectedIndex);
            Click(window, control, 1, modifiers);
            Assert.Equal(1, control.SelectedIndex);
            Assert.Single(control.SelectedItems!);
        }
        finally
        {
            window.Close();
        }
    }

    // Removing the selected segment selects the nearest remaining one: the one that took its
    // place, or the new last one when it was last. Removing another keeps the selection.
    [AvaloniaFact]
    public void Selection_MovesToTheNearestSegment_WhenTheSelectedOneIsRemoved()
    {
        var items = new System.Collections.ObjectModel.ObservableCollection<string>(["Day", "Week", "Month", "Year"]);
        var (window, control, _, _) = Show(new SegmentedControl { ItemsSource = items }, selected: 2);
        try
        {
            items.RemoveAt(2);
            Pump(window);
            Assert.Equal("Year", control.SelectedItem);
            AssertPillOn(control, control.SelectedIndex);

            items.RemoveAt(2);
            Pump(window);
            Assert.Equal("Week", control.SelectedItem);
            AssertPillOn(control, 1);

            items.RemoveAt(0);
            Pump(window);
            Assert.Equal("Week", control.SelectedItem);
            AssertPillOn(control, 0);
        }
        finally
        {
            window.Close();
        }
    }

    // Each removal of the selected segment raises exactly one selection change, straight to the
    // nearest segment (never passing through the first), with no dispatcher turn in between.
    [AvaloniaFact]
    public void Selection_OneChangePerRemoval_AndBackToBackRemovals()
    {
        var items = new System.Collections.ObjectModel.ObservableCollection<string>(["A", "B", "C", "D", "E"]);
        var (window, control, _, _) = Show(new SegmentedControl { ItemsSource = items }, selected: 2);
        try
        {
            var changes = new List<object?>();
            control.SelectionChanged += (_, e) => changes.Add(e.AddedItems.Count > 0 ? e.AddedItems[0] : null);

            items.RemoveAt(2); // C, selected: D takes its place
            Assert.Equal("D", control.SelectedItem);
            Assert.Equal(["D"], changes);

            items.RemoveAt(0); // A, not selected: D stays, no change
            Assert.Equal("D", control.SelectedItem);
            Assert.Equal(["D"], changes);

            items.RemoveAt(items.Count - 1); // E, not selected
            items.RemoveAt(items.Count - 1); // D, selected and last: B, the new last
            Assert.Equal("B", control.SelectedItem);
            Assert.Equal(["D", "B"], changes);
            Pump(window);
            AssertPillOn(control, 0);
        }
        finally
        {
            window.Close();
        }
    }

    // Replacing the selected item keeps its index, with one selection change to the new item.
    [AvaloniaFact]
    public void Selection_ReplacingTheSelectedItemKeepsItsIndex()
    {
        var items = new System.Collections.ObjectModel.ObservableCollection<string>(["Day", "Week", "Month"]);
        var (window, control, _, _) = Show(new SegmentedControl { ItemsSource = items }, selected: 1);
        try
        {
            var changes = 0;
            control.SelectionChanged += (_, _) => changes++;
            items[1] = "Fortnight";
            Assert.Equal(1, control.SelectedIndex);
            Assert.Equal("Fortnight", control.SelectedItem);
            Assert.Equal(1, changes);
            items[0] = "Hour";
            Assert.Equal(1, control.SelectedIndex);
            Assert.Equal(1, changes);
        }
        finally
        {
            window.Close();
        }
    }

    // The pill stays on its segment as segments are added or removed around it.
    [AvaloniaFact]
    public void Pill_FollowsItsSegment_WhenItemsChange()
    {
        var items = new System.Collections.ObjectModel.ObservableCollection<string>(["Day", "Week", "Month"]);
        var (window, control, _, _) = Show(new SegmentedControl { ItemsSource = items }, selected: 1);
        try
        {
            items.Insert(0, "Hour");
            Pump(window);
            Assert.Equal("Week", control.SelectedItem);
            AssertPillOn(control, 2);
            items.Add("Year");
            Pump(window);
            AssertPillOn(control, 2);
            items.RemoveAt(0);
            Pump(window);
            AssertPillOn(control, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Selection_DoesNotScrollThePage()
    {
        var control = Items("Day", "Week", "Month", "Year");
        var scroller = new ScrollViewer
        {
            Width = 200,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { new Border { Width = 400 }, control } },
        };
        var window = new Window { Width = 300, Height = 200, Content = scroller };
        window.Show();
        try
        {
            Pump(window);
            control.SelectedIndex = 3;
            Pump(window);
            Assert.Equal(0, scroller.Offset.X);
        }
        finally
        {
            window.Close();
        }
    }

    // The pill slides to the new selection: its transitions run over MotionNormal, and once they
    // end (instant under reduce motion) it rests on the selected segment.
    [AvaloniaFact]
    public void Pill_SlidesToTheSelection()
    {
        var (window, control, _, _) = Show(Items("Day", "Week", "Month", "Year"));
        try
        {
            var indicator = Indicator(control);
            var slides = indicator.Transitions!.OfType<TransitionBase>().ToList();
            Assert.Equal(2, slides.Count);
            Assert.All(slides, t => Assert.Equal(TimeSpan.FromMilliseconds(200), t.Duration));
            Assert.Contains(slides, t => t.Property == SelectionIndicator.OffsetProperty);
            Assert.Contains(slides, t => t.Property == SelectionIndicator.SizeProperty);

            control.SelectedIndex = 2;
            Pump(window);
            // The slide has started from the old segment towards the new one.
            Assert.Equal(Rect(control, 2), indicator.Target);
            Assert.NotEqual(Rect(control, 2).Position, indicator.Offset);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Pill_LandsOnTheSelection_UnderReduceMotion()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true };
        var (window, control, _, _) = Show(Items("Day", "Week", "Month", "Year"), theme: theme);
        try
        {
            Assert.Empty(Indicator(control).Transitions!);
            foreach (var index in new[] { 3, 1, 2, 0 })
            {
                control.SelectedIndex = index;
                Pump(window);
                AssertPillOn(control, index);
            }
        }
        finally
        {
            window.Close();
        }
    }

    // A resize moves the pill with its segment at once, with motion on: it never lags the layout.
    [AvaloniaFact]
    public void Pill_FollowsResizesAtOnce()
    {
        var (window, control, _, _) = Show(Items("Day", "Week", "Month", "Year"), selected: 2);
        try
        {
            AssertPillOn(control, 2);
            control.Width = 400;
            Pump(window);
            AssertPillOn(control, 2);
            control.Width = double.NaN;
            control.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            window.Width = 520;
            Pump(window);
            AssertPillOn(control, 2);
            // A slide still follows afterwards.
            Assert.NotEmpty(Indicator(control).Transitions!);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FocusRing_SurroundsTheControl_OnKeyboardFocusOnly()
    {
        var (window, control, before, _) = Show(Items("Day", "Week"));
        try
        {
            var ring = Part(control, FocusRing.Part);
            Assert.False(ring.IsVisible);

            before.Focus(NavigationMethod.Tab);
            Press(window, Key.Tab);
            Assert.True(ring.IsVisible);
            Assert.Equal(new CornerRadius(12), ring.CornerRadius);
            Assert.Equal(new Thickness(-4), ring.Margin);
            Assert.Equal(1, control.ZIndex);

            Press(window, Key.Right);
            Assert.True(ring.IsVisible);

            before.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.False(ring.IsVisible);

            Segment(control, 1).Focus(NavigationMethod.Pointer);
            Pump(window);
            Assert.False(ring.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    // The pill and selected text come from the palette; every enabled text meets 4.5:1 over what is under it.
    [AvaloniaTheory]
    [InlineData("Light", "SystemAltHighColor", "SystemAltHighColor")]
    [InlineData("Dark", "SystemBaseMediumLowColor", "SystemChromeHighColor")]
    public void Colours_FromThePalette_AndTextMeetsAa(string theme, string pillKey, string disabledPillKey)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, control, _, _) = Show(Items("Day", "Week"), variant);
        try
        {
            var pill = Part(control, SegmentedControlTheme.PillPart);
            Assert.Equal(Effective(Resolve(window, pillKey)), Effective(pill.Background));
            Assert.Equal(Effective(Resolve(window, "SystemBaseHighColor")), Text(Segment(control, 0)));

            var track = Effective(Part(control, SegmentedControlTheme.TrackPart).Background);
            foreach (var (text, under, name) in new[]
                     {
                         (Text(Segment(control, 0)), Effective(pill.Background), "selected"),
                         (Text(Segment(control, 1)), track, "unselected"),
                     })
            {
                var ratio = AccessibleColors.ContrastRatio(text, under);
                Assert.True(ratio >= 4.5, $"{name}: {ratio:F2}");
            }

            control.IsEnabled = false;
            Pump(window);
            Assert.Equal(Effective(Resolve(window, disabledPillKey)), Effective(pill.Background));
        }
        finally
        {
            window.Close();
        }
    }

    // A hovered or pressed segment stays clearly apart from the pill (at least 1.3:1, the
    // luminance contrast ratio) and still shows on the track, in both themes.
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void HoverAndPressed_StayApartFromThePill(string theme)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        foreach (var state in new[] { ":pointerover", ":pressed" })
        {
            var (window, control, _, _) = Show(Items("Day", "Week"), variant);
            try
            {
                var segment = Segment(control, 1);
                ((IPseudoClasses)segment.Classes).Set(state, true);
                Pump(window);
                var chrome = Part<SquircleBorder>(segment, ButtonParts.ChromePart);
                var fill = Effective(chrome.GetBaseValue(SquircleBorder.BackgroundProperty).Value);
                var track = Effective(Part(control, SegmentedControlTheme.TrackPart).Background);
                var pill = Effective(Part(control, SegmentedControlTheme.PillPart).Background);
                var onTrack = Over(fill, track);
                var fromPill = AccessibleColors.ContrastRatio(onTrack, pill);
                Assert.True(fromPill >= 1.3, $"{state} vs pill: {fromPill:F2}");
                Assert.NotEqual(track, onTrack);
            }
            finally
            {
                window.Close();
            }
        }
    }

    // Selected text cross-fades with the slide; no frame overshoots its ends.
    [AvaloniaFact]
    public void Text_CrossFades()
    {
        var (window, control, _, _) = Show(Items("Day", "Week"));
        try
        {
            var presenter = (ContentPresenter)Part<SquircleBorder>(Segment(control, 0), ButtonParts.ChromePart).Child!;
            var fade = Assert.Single(presenter.Transitions!.OfType<TransitionBase>());
            Assert.IsType<CrossFadeBrushTransition>(fade);
            Assert.Equal(ContentPresenter.ForegroundProperty, fade.Property);
            Assert.Equal(TimeSpan.FromMilliseconds(200), fade.Duration);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Helpers -------------------------------------------------------------------------

    private static SegmentedControl Items(params string[] items) => new() { ItemsSource = items };

    private static (Window Window, SegmentedControl Control, Button Before, Button After) Show(SegmentedControl control,
        ThemeVariant? variant = null, StandardsTheme? theme = null, int selected = 0)
    {
        if (selected >= 0)
            control.SelectedIndex = selected;
        var before = new Button { Content = "Before" };
        var after = new Button { Content = "After" };
        var window = new Window
        {
            Width = 600, Height = 300, RequestedThemeVariant = variant ?? ThemeVariant.Light,
            Content = new StackPanel { Spacing = 8, Margin = new Thickness(8), Children = { before, control, after } },
        };
        if (theme is not null)
            window.Styles.Add(theme);
        window.Show();
        Pump(window);
        return (window, control, before, after);
    }

    private static SegmentedControlItem Segment(SegmentedControl control, int index) => (SegmentedControlItem)control.ContainerFromIndex(index)!;

    private static SelectionIndicator Indicator(SegmentedControl control) => control.GetVisualDescendants().OfType<SelectionIndicator>().Single();

    private static SquircleBorder Part(Control control, string name) => Part<SquircleBorder>(control, name);

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static Rect Rect(SegmentedControl control, int index)
    {
        var indicator = Indicator(control);
        var segment = Segment(control, index);
        return new Rect(segment.TranslatePoint(default, indicator)!.Value, segment.Bounds.Size);
    }

    private static void AssertPillOn(SegmentedControl control, int index)
    {
        var expected = Rect(control, index);
        var pill = Part(control, SegmentedControlTheme.PillPart);
        Assert.Equal(expected, Indicator(control).Target);
        Assert.Equal(expected, pill.Bounds);
    }

    private static Color Text(SegmentedControlItem segment) =>
        Effective(((ContentPresenter)Part<SquircleBorder>(segment, ButtonParts.ChromePart).Child!).Foreground);

    private static void Click(Window window, SegmentedControl control, int index, RawInputModifiers modifiers)
    {
        var segment = Segment(control, index);
        var point = segment.TranslatePoint(new Point(segment.Bounds.Width / 2, segment.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left, modifiers);
        window.MouseUp(point, MouseButton.Left, modifiers);
        Pump(window);
    }

    private static IInputElement? Focused(Window window) => window.FocusManager!.GetFocusedElement();

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, null);
        window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Pump(window);
    }

    private static object Resolve(Control control, string key)
    {
        Assert.True(control.TryFindResource(key, control.ActualThemeVariant, out var value), key);
        return value!;
    }

    private static void Pump(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
