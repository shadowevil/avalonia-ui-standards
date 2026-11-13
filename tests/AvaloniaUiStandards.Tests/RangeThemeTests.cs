using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
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
using static AvaloniaUiStandards.Tests.Colours;
using static AvaloniaUiStandards.Tests.ToggleThemeTests;

namespace AvaloniaUiStandards.Tests;

/// <summary>Slider, ProgressBar and ProgressRing (decision 0031).</summary>
public class RangeThemeTests
{
    [AvaloniaTheory]
    [InlineData("Normal", 20, 32)]
    [InlineData("Compact", 16, 24)]
    public void Slider_ThumbAndRow_FollowDensity(string density, double thumb, double row)
    {
        var (_, slider) = Show(new Slider { Width = 200, Value = 50 }, theme: Theme(density));
        var thumbControl = slider.GetVisualDescendants().OfType<Thumb>().Single();

        Assert.Equal(new Size(thumb, thumb), thumbControl.Bounds.Size);
        Assert.Equal(row, slider.Bounds.Height);
        // The pills are 4 px thick and run under the thumb.
        var decrease = Part<RepeatButton>(slider, SliderTheme.DecreasePart);
        var pill = decrease.GetVisualDescendants().OfType<SquircleBorder>().Single();
        Assert.Equal(RangeParts.TrackThickness, pill.Bounds.Height);
        Assert.Equal(decrease.Bounds.Width + SliderTheme.UnderThumb, pill.Bounds.Width);
    }

    [AvaloniaFact]
    public void Slider_Vertical_FillsFromTheBottom()
    {
        var (_, slider) = Show(new Slider { Height = 200, Value = 25, Orientation = Orientation.Vertical });
        var decrease = Part<RepeatButton>(slider, SliderTheme.DecreasePart);
        var increase = Part<RepeatButton>(slider, SliderTheme.IncreasePart);
        Assert.True(decrease.Bounds.Top > increase.Bounds.Top);
        Assert.Equal(32, slider.Bounds.Width);
    }

    public static TheoryData<string, string, string, string> SliderStates()
    {
        var data = new TheoryData<string, string, string, string>();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            data.Add(theme, "rest", "AccentFillColor", "SliderTrackFill");
            data.Add(theme, ":pointerover", "AccentFillColorPointerOver", "SliderTrackFillPointerOver");
            data.Add(theme, ":pressed", "AccentFillColorPressed", "SliderTrackFillPressed");
            data.Add(theme, ":disabled", "SystemControlForegroundBaseMediumLowBrush", "SliderTrackFillDisabled");
        }
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(SliderStates))]
    public void Slider_StateColours_Resolve(string theme, string state, string valueKey, string restKey)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, slider) = Show(new Slider { Width = 200, Value = 50 }, variant);
        if (state == ":disabled")
            slider.IsEnabled = false;
        else if (state != "rest")
            ((IPseudoClasses)slider.Classes).Set(state, true);
        Settle(window);

        Assert.Equal(Effective(Resolve(window, valueKey, variant)), Effective(Part<RepeatButton>(slider, SliderTheme.DecreasePart).Background));
        Assert.Equal(Effective(Resolve(window, restKey, variant)), Effective(Part<RepeatButton>(slider, SliderTheme.IncreasePart).Background));
        var thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();
        Assert.Equal(state == ":disabled" ? Effective(Resolve(window, "SystemControlBackgroundBaseLowBrush", variant)) : Colors.White, Effective(thumb.GetBaseValue(TemplatedControl.BackgroundProperty).Value));
    }

    [AvaloniaTheory]
    [InlineData(":pointerover", 1.1)]
    [InlineData(":pressed", 1.2)]
    public void Slider_ThumbGrows_OnHoverAndPress(string state, double scale)
    {
        var (window, slider) = Show(new Slider { Width = 200, Value = 50 });
        ((IPseudoClasses)slider.Classes).Set(state, true);
        Settle(window);
        var thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();
        Assert.Equal(scale, ((ITransform)thumb.GetBaseValue(Visual.RenderTransformProperty).Value!).Value.M11, 3);
    }

    [AvaloniaFact]
    public void Slider_ValueFill_MeetsNonTextContrast_AtRest()
    {
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            var (window, slider) = Show(new Slider { Width = 200, Value = 50 }, variant);
            var fill = Effective(Part<RepeatButton>(slider, SliderTheme.DecreasePart).Background);
            foreach (var surface in new[] { "SystemRegionColor", "SystemChromeMediumLowColor" })
                Assert.True(AccessibleColors.ContrastRatio(fill, Effective(Resolve(window, surface, variant))) >= 3, $"{variant} {surface}");
        }
    }

    [AvaloniaFact]
    public void Slider_Focus_ShowsRingAroundTheThumb()
    {
        var (_, slider) = Show(new Slider { Width = 200, Value = 50 });
        var ring = Part<SquircleBorder>(slider, FocusRing.Part);
        Assert.False(ring.IsVisible);

        slider.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        var thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();
        Assert.True(ring.IsVisible);
        Assert.Null(slider.FocusAdorner);
        Assert.Equal(new Rect(-4, -4, thumb.Bounds.Width + 8, thumb.Bounds.Height + 8), ring.Bounds);
    }

    [AvaloniaFact]
    public void Slider_Keyboard_ChangesValue()
    {
        var (window, slider) = Show(new Slider { Width = 200, Value = 50, SmallChange = 1 });
        slider.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Assert.Equal(51, slider.Value);
    }

    [AvaloniaFact]
    public void Slider_Ticks_ShowByPlacement()
    {
        var (_, slider) = Show(new Slider { Width = 200, TickFrequency = 10, TickPlacement = TickPlacement.BottomRight });
        var ticks = slider.GetVisualDescendants().OfType<TickBar>().ToList();
        Assert.Equal(["BottomTickBar"], ticks.Where(t => t.IsVisible).Select(t => t.Name));
    }

    // Ticks sit 4 px from the thumb's edge: one side fits the row, both sides grow it.
    [AvaloniaTheory]
    [InlineData("Normal", TickPlacement.TopLeft, 32)]
    [InlineData("Normal", TickPlacement.Outside, 36)]
    [InlineData("Compact", TickPlacement.BottomRight, 24)]
    [InlineData("Compact", TickPlacement.Outside, 32)]
    public void Slider_Ticks_KeepClearOfTheThumb(string density, TickPlacement placement, double height)
    {
        var (_, slider) = Show(new Slider { Width = 200, TickFrequency = 10, TickPlacement = placement }, theme: Theme(density));
        Assert.Equal(height, slider.Bounds.Height);
    }

    [AvaloniaFact]
    public void Slider_Transitions_AreFastCrossFades_AndStopUnderReduceMotion()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var (window, slider) = Show(new Slider { Width = 200 }, theme: theme);
        var pill = Part<RepeatButton>(slider, SliderTheme.DecreasePart).GetVisualDescendants().OfType<SquircleBorder>().Single();
        var transitions = pill.Transitions!.Cast<TransitionBase>().ToList();
        Assert.All(transitions, t => Assert.Equal(TimeSpan.FromMilliseconds(100), t.Duration));
        Assert.Contains(transitions, t => t is CrossFadeBrushTransition);

        theme.ReduceMotion = true;
        Settle(window);
        Assert.Empty(pill.Transitions!);
    }

    [AvaloniaFact]
    public void ProgressBar_IsAPillTrack_WithTheAccentFill()
    {
        var (window, bar) = Show(new ProgressBar { Width = 200, Value = 40 });
        var track = Part<SquircleBorder>(bar, ProgressBarTheme.TrackPart);
        var indicator = Part<Border>(bar, ProgressBarTheme.IndicatorPart);

        Assert.Equal(new Size(200, 4), track.Bounds.Size);
        Assert.Equal(80, indicator.Bounds.Width, 1);
        Assert.Equal(Effective(Resolve(window, "AccentFillColor", ThemeVariant.Light)), Effective(indicator.Background));
        Assert.Equal(Effective(Resolve(window, "SystemControlBackgroundBaseLowBrush", ThemeVariant.Light)), Effective(track.Background));
    }

    [AvaloniaFact]
    public void ProgressBar_Text_ShowsAfterTheBar()
    {
        var (_, bar) = Show(new ProgressBar { Width = 200, Value = 40, ShowProgressText = true });
        var text = Part<TextBlock>(bar, ProgressBarTheme.TextPart);
        Assert.Equal("40%", text.Text);
        Assert.True(text.Bounds.Left >= Part<SquircleBorder>(bar, ProgressBarTheme.TrackPart).Bounds.Right + 8);
    }

    [AvaloniaFact]
    public void Sweep_StillFrameIsCentred_AndCrossesTheTrack()
    {
        var (start, length) = ProgressSweep.Segment(200, TimeSpan.Zero);
        Assert.Equal(80, length);
        Assert.Equal(60, start, 3);
        // A quarter period later it is past the middle, and at half a period it has left the track.
        Assert.True(ProgressSweep.Segment(200, TimeSpan.FromSeconds(0.5)).Start > 60);
        Assert.Equal(200, ProgressSweep.Segment(200, TimeSpan.FromSeconds(0.99999)).Start, 1);
        Assert.Equal(-80, ProgressSweep.Segment(200, TimeSpan.FromSeconds(1.0001)).Start, 0);
    }

    [AvaloniaFact]
    public void Indeterminate_RunsOnTheWindowsFrameTicks_AndNotUnderReduceMotion()
    {
        var bar = new ProgressBar { Width = 200, IsIndeterminate = true };
        var window = Window(bar, reduceMotion: false);
        var reduced = Window(new ProgressBar { IsIndeterminate = true }, reduceMotion: true);
        try
        {
            Assert.True(window.IsFrameTickRunning);

            bar.IsIndeterminate = false;
            Settle(window);
            Assert.False(window.IsFrameTickRunning);

            window.Content = new ProgressRing { IsIndeterminate = true };
            Settle(window);
            Assert.True(window.IsFrameTickRunning);

            Assert.False(reduced.IsFrameTickRunning);
        }
        finally
        {
            window.Close();
            reduced.Close();
        }
    }

    [AvaloniaFact]
    public void Determinate_LeavesFrameTicksOff()
    {
        var window = Window(new StackPanel { Children = { new ProgressBar { Value = 40 }, new ProgressRing { Value = 40 } } }, reduceMotion: false);
        try
        {
            Assert.False(window.IsFrameTickRunning);
        }
        finally
        {
            window.Close();
        }
    }

    // A busy overlay hidden with IsVisible = false must not keep the window ticking.
    [AvaloniaFact]
    public void Indeterminate_InAHiddenPanel_LeavesFrameTicksOff()
    {
        var bar = new ProgressBar { Width = 200, IsIndeterminate = true };
        var ring = new ProgressRing { IsIndeterminate = true };
        var overlay = new Panel { IsVisible = false, Children = { new StackPanel { Children = { bar, ring } } } };
        var window = Window(overlay, reduceMotion: false);
        try
        {
            Assert.False(window.IsFrameTickRunning);

            overlay.IsVisible = true;
            Settle(window);
            Assert.True(window.IsFrameTickRunning);

            overlay.IsVisible = false;
            Settle(window);
            Assert.False(window.IsFrameTickRunning);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FrameTicks_AdvanceTheTime_ExceptUnderReduceMotion()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var (window, bar) = Show(new ProgressBar { Width = 200, IsIndeterminate = true }, theme: theme);
        var clock = Part<ProgressSweep>(bar, ProgressBarTheme.SweepPart).Clock;

        clock.OnTick(TimeSpan.FromSeconds(0.1));
        clock.OnTick(TimeSpan.FromSeconds(0.05));
        Assert.Equal(TimeSpan.FromSeconds(0.15), clock.Time);

        theme.ReduceMotion = true;
        Settle(window);
        clock.OnTick(TimeSpan.FromSeconds(0.1));
        Assert.Equal(TimeSpan.Zero, clock.Time);
    }

    [AvaloniaFact]
    public void Time_StaysZero_OutsideAStandardsWindow()
    {
        var (window, bar) = Show(new ProgressBar { Width = 200, IsIndeterminate = true });
        var clock = Part<ProgressSweep>(bar, ProgressBarTheme.SweepPart).Clock;
        Assert.True(clock.IsRunning);
        for (var i = 0; i < 5; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Settle(window);
        }
        Assert.Equal(TimeSpan.Zero, clock.Time);
    }

    // Headless popups live in the window's overlay layer, so the PopupRoot.ParentTopLevel path is not reached here.
    [AvaloniaFact]
    public void InAPopup_TheWindowsTicksAreUsed()
    {
        var bar = new ProgressBar { Width = 200, IsIndeterminate = true };
        var anchor = new Border { Width = 10, Height = 10 };
        var window = Window(anchor, reduceMotion: false);
        var popup = new Popup { PlacementTarget = anchor, Child = bar };
        try
        {
            ((ISetLogicalParent)popup).SetParent(anchor);
            popup.IsOpen = true;
            Settle(window);
            Assert.Same(window, FrameTicks.For(bar));
            Assert.True(window.IsFrameTickRunning);
        }
        finally
        {
            popup.IsOpen = false;
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ProgressBarText_FollowsTheValue_AfterReTemplating()
    {
        var (window, bar) = Show(new ProgressBar { Width = 200, Value = 40, ShowProgressText = true });
        bar.Orientation = Orientation.Vertical;
        Settle(window);
        bar.Orientation = Orientation.Horizontal;
        Settle(window);
        bar.Value = 70;
        Assert.Equal("70%", Part<TextBlock>(bar, ProgressBarTheme.TextPart).Text);
    }

    [AvaloniaFact]
    public void Indeterminate_ReturnsToItsStillFrame_UnderReduceMotion()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var (window, bar) = Show(new ProgressBar { Width = 200, IsIndeterminate = true }, theme: theme);
        var sweep = Part<ProgressSweep>(bar, ProgressBarTheme.SweepPart);
        Assert.True(sweep.IsVisible);
        Assert.False(Part<Border>(bar, ProgressBarTheme.IndicatorPart).IsVisible);
        sweep.Clock.Set(TimeSpan.FromSeconds(0.7));

        theme.ReduceMotion = true;
        Settle(window);
        Assert.Equal(TimeSpan.Zero, sweep.Clock.Time);
    }

    [AvaloniaTheory]
    [InlineData("Normal", 20)]
    [InlineData("Compact", 16)]
    public void ProgressRing_Size_FollowsDensity(string density, double size)
    {
        var (_, ring) = Show(new ProgressRing { Value = 50 }, theme: Theme(density));
        Assert.Equal(new Size(size, size), ring.Bounds.Size);
        Assert.Equal(2, RingArc.Stroke(size));
        Assert.Equal(5, RingArc.Stroke(48));
    }

    [AvaloniaFact]
    public void ProgressRing_Arc_FollowsValue_AndIndeterminateIsCentredOnTheTop()
    {
        var (window, ring) = Show(new ProgressRing { Value = 25 });
        var arc = Part<RingArc>(ring, ProgressRingTheme.ArcPart);
        Assert.Equal((0d, 90d), arc.Span());

        ring.Maximum = 50;
        Assert.Equal((0d, 180d), arc.Span());

        ring.IsIndeterminate = true;
        Settle(window);
        Assert.Contains(":indeterminate", ring.Classes);
        Assert.Equal((-45d, 90d), arc.Span());
        arc.Clock.Set(TimeSpan.FromSeconds(0.25));
        Assert.Equal(45, arc.Span().Start, 3);
    }

    [AvaloniaFact]
    public void ProgressRing_IsAReadOnlyProgressBar_ToAutomation()
    {
        var (_, ring) = Show(new ProgressRing { Value = 30 });
        var peer = ControlAutomationPeer.CreatePeerForElement(ring);
        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
        var range = Assert.IsAssignableFrom<IRangeValueProvider>(peer);
        Assert.True(range.IsReadOnly);
        Assert.Equal(30, range.Value);
        Assert.Equal(100, range.Maximum);
        Assert.Throws<InvalidOperationException>(() => range.SetValue(50));
    }

    [AvaloniaFact]
    public void ProgressRing_Disabled_DimsTheArc()
    {
        var (window, ring) = Show(new ProgressRing { Value = 25, IsEnabled = false });
        Assert.Equal(Effective(Resolve(window, "SystemControlForegroundBaseMediumLowBrush", ThemeVariant.Light)),
            Effective(Part<RingArc>(ring, ProgressRingTheme.ArcPart).Arc));
    }

    // A shown, active library window with its own theme.
    private static StandardsWindow Window(Control content, bool reduceMotion)
    {
        var window = new StandardsWindow { Width = 400, Height = 300, Content = content };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion });
        window.Show();
        window.Activate();
        Settle(window);
        return window;
    }
}
