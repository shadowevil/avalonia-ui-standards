using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// Frame ticks for continuous animation: 30 per second by default, the display rate when opted in,
/// stopped under reduce motion and paused while the window is minimised (or inactive).
/// </summary>
public class FrameTickTests
{
    // --- The ticker's pacing, on a simulated display ------------------------------------

    [Theory]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(144)]
    public void StandardRate_IsAbout30TicksPerSecond_OnAnyDisplay(int hz)
    {
        var display = new Display(hz);
        var ticks = new List<TimeSpan>();
        var ticker = new FrameTicker(display, (_, elapsed) => ticks.Add(elapsed));
        ticker.Start();
        display.Run(TimeSpan.FromSeconds(2));

        var perSecond = ticks.Count / 2.0;
        Assert.InRange(perSecond, 27, 31);
        Assert.All(ticks.Skip(1), e => Assert.InRange(e.TotalMilliseconds, 29, 42));
        // It waits between ticks instead of taking every display frame.
        Assert.True(display.FramesRequested <= ticks.Count * 2 + 1, $"{display.FramesRequested} frames requested for {ticks.Count} ticks");
    }

    [Theory]
    [InlineData(60)]
    [InlineData(120)]
    public void HighRate_IsTheDisplayRate(int hz)
    {
        var display = new Display(hz);
        var count = 0;
        var ticker = new FrameTicker(display, (_, _) => count++) { HighRate = true };
        ticker.Start();
        display.Run(TimeSpan.FromSeconds(1));
        Assert.InRange(count, hz - 1, hz + 1);
    }

    [Fact]
    public void Stopped_NoTicks_AndRestarting_StartsWithZeroElapsed()
    {
        var display = new Display(60);
        var ticks = new List<TimeSpan>();
        var ticker = new FrameTicker(display, (_, elapsed) => ticks.Add(elapsed));
        ticker.Start();
        display.Run(TimeSpan.FromSeconds(0.5));
        ticker.Stop();
        var stopped = ticks.Count;
        display.Run(TimeSpan.FromSeconds(0.5));
        Assert.Equal(stopped, ticks.Count);

        ticker.Start();
        display.Run(TimeSpan.FromSeconds(0.2));
        Assert.Equal(TimeSpan.Zero, ticks[stopped]);
        Assert.True(ticks.Count > stopped + 3);
    }

    // --- The window ----------------------------------------------------------------------
    // (Headless rendering delivers only the first of a chain of animation frames, so these check
    // when the window runs its ticks; the pacing is checked on the simulated display above.)

    [AvaloniaFact]
    public void FrameTick_Ticks_WhileSomeoneListens()
    {
        var (window, _) = Show(highRate: true);
        try
        {
            Assert.False(window.IsFrameTickRunning);
            var times = new List<FrameTickEventArgs>();
            void OnTick(object? sender, FrameTickEventArgs e) => times.Add(e);
            window.FrameTick += OnTick;
            Assert.True(window.IsFrameTickRunning);
            Frames(window, 3);
            Assert.NotEmpty(times);
            Assert.Equal(TimeSpan.Zero, times[0].Elapsed);

            window.FrameTick -= OnTick;
            Assert.False(window.IsFrameTickRunning);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FrameTick_StopsUnderReduceMotion()
    {
        var (window, theme) = Show(highRate: false);
        try
        {
            window.FrameTick += (_, _) => { };
            Assert.True(window.IsFrameTickRunning);
            theme.ReduceMotion = true;
            Assert.False(window.IsFrameTickRunning);
            theme.ReduceMotion = false;
            Assert.True(window.IsFrameTickRunning);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FrameTick_PausesWhileMinimised_AndWhenClosed()
    {
        var (window, _) = Show(highRate: false);
        window.FrameTick += (_, _) => { };
        Assert.True(window.IsFrameTickRunning);
        window.WindowState = WindowState.Minimized;
        Assert.False(window.IsFrameTickRunning);
        window.WindowState = WindowState.Normal;
        Assert.True(window.IsFrameTickRunning);
        window.Close();
        Assert.False(window.IsFrameTickRunning);
    }

    [AvaloniaFact]
    public void HighRefreshRate_DefaultsFromTheTheme_AndTheWindowWins()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var window = new StandardsWindow { Width = 400, Height = 300 };
        window.Styles.Add(theme);
        window.Show();
        try
        {
            Assert.False(window.HighRefreshRate);
            theme.HighRefreshRate = true;
            Assert.True(window.HighRefreshRate);
            window.HighRefreshRate = false;
            Assert.False(window.HighRefreshRate);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Helpers ---------------------------------------------------------------------

    private static (StandardsWindow Window, StandardsTheme Theme) Show(bool highRate)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { HighRefreshRate = highRate };
        var window = new StandardsWindow { Width = 400, Height = 300, WindowButtonStyle = WindowButtonStyle.CaptionButtons };
        window.Styles.Add(theme);
        window.Show();
        window.Activate();
        Dispatcher.UIThread.RunJobs();
        return (window, theme);
    }

    private static void Frames(Window window, int count)
    {
        for (var i = 0; i < count; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    // A display refreshing at a fixed rate on a virtual clock: a requested frame comes at the
    // next refresh, a delay ends after its time.
    private sealed class Display(int hz) : IFrameScheduler
    {
        private readonly TimeSpan _period = TimeSpan.FromSeconds(1.0 / hz);
        private readonly PriorityQueue<Action, TimeSpan> _events = new();
        private TimeSpan _now;

        public int FramesRequested { get; private set; }

        public void RequestFrame(Action<TimeSpan> callback)
        {
            FramesRequested++;
            var next = TimeSpan.FromTicks((_now.Ticks / _period.Ticks + 1) * _period.Ticks);
            _events.Enqueue(() => callback(next), next);
        }

        public void Delay(TimeSpan delay, Action callback) => _events.Enqueue(callback, _now + delay);

        public void Run(TimeSpan duration)
        {
            var end = _now + duration;
            while (_events.TryPeek(out _, out var at) && at <= end)
            {
                var action = _events.Dequeue();
                _now = at;
                action();
            }
            _now = end;
        }
    }
}
