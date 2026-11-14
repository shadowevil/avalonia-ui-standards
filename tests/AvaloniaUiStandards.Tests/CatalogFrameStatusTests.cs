using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaUiStandards.Style;
using Catalog;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// The catalog status bar's frame readout: the rendered frames per second, the high refresh rate state and why
/// animation is paused; that it counts frames without causing any; and that it fits the status bar at every width.
/// </summary>
public class CatalogFrameStatusTests
{
    [AvaloniaFact]
    public void StatusBar_ShowsFramesAndHighRefreshRate_FollowingTheProperty()
    {
        var window = NewWindow(1200, 800, reduceMotion: false);
        var theme = window.Styles.OfType<StandardsTheme>().Single();
        try
        {
            window.Show();
            window.Activate();
            Settle(window);
            Assert.Matches(@"^\d+ fps · High refresh rate: off$", window.StatusFrames.Text);
            Assert.Equal(AppInfo.LibraryText, window.StatusVersion.Text);

            // The window's own value.
            window.HighRefreshRate = true;
            Settle(window);
            Assert.EndsWith("High refresh rate: on", window.StatusFrames.Text);
            window.HighRefreshRate = false;
            Settle(window);
            Assert.EndsWith("High refresh rate: off", window.StatusFrames.Text);

            // The app-wide default, when the window sets none.
            window.ClearValue(AvaloniaUiStandards.Modules.StandardsWindow.HighRefreshRateProperty);
            theme.HighRefreshRate = true;
            Settle(window);
            Assert.EndsWith("High refresh rate: on", window.StatusFrames.Text);
            theme.HighRefreshRate = false;
            Settle(window);
            Assert.EndsWith("High refresh rate: off", window.StatusFrames.Text);

            // Paused ticks say why.
            theme.ReduceMotion = true;
            Settle(window);
            Assert.EndsWith("High refresh rate: off · Animation paused (reduce motion)", window.StatusFrames.Text);
            theme.ReduceMotion = false;
            Settle(window);
            Assert.EndsWith("High refresh rate: off", window.StatusFrames.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PauseReason_FollowsTheFrameTickRules()
    {
        Assert.Null(FrameStatus.PauseReason(reduceMotion: false, active: true, WindowState.Normal));
        Assert.Equal("reduce motion", FrameStatus.PauseReason(reduceMotion: true, active: true, WindowState.Normal));
        Assert.Equal("window inactive", FrameStatus.PauseReason(reduceMotion: false, active: false, WindowState.Normal));
        Assert.Equal("minimised", FrameStatus.PauseReason(reduceMotion: false, active: false, WindowState.Minimized));
    }

    [AvaloniaFact]
    public void Meter_CountsRenderedFramesWithoutCausingAny()
    {
        // Nothing animates under reduce motion, so the window renders only when something changes.
        var window = NewWindow(1200, 800, reduceMotion: true);
        try
        {
            window.Show();
            window.Activate();
            Settle(window);
            var meter = window.FrameRateMeter;
            Assert.True(meter.IsAttached, "the counting visual is not laid over the window");

            // A changed frame is counted.
            window.StatusPage.Text = "Changed";
            Frames(window, 2);
            meter.Read();
            Assert.True(meter.FramesPerSecond > 0, "a rendered frame was not counted");

            // Left alone, the readout settles at 0 (discounting its own repaint) and its timer stops.
            for (var i = 0; i < 4 && !meter.IsIdle; i++)
            {
                Frames(window, 3);
                meter.Read();
                Frames(window, 1);
            }
            Assert.Equal(0, meter.FramesPerSecond);
            Assert.True(meter.IsIdle, "the readout keeps reading while nothing renders");
            Assert.StartsWith("0 fps", window.StatusFrames.Text);

            // Idle, render ticks produce no frames: the meter itself causes none.
            Frames(window, 5);
            Assert.True(meter.IsIdle, "a frame was rendered while nothing changed");

            // The next real frame wakes it.
            window.StatusPage.Text = "Changed again";
            Frames(window, 1);
            Assert.False(meter.IsIdle, "a rendered frame did not wake the readout");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void StatusBar_FitsAtEveryWindowSize()
    {
        // The default, a medium and the minimum window (the 480 x 320 frame plus the shadow margin), with and
        // without the pause note, which is the longest text.
        var problems = new List<string>();
        foreach (var (width, height) in new[] { (1200.0, 800.0), (848.0, 600.0), (528.0, 368.0) })
        foreach (var reduceMotion in new[] { false, true })
        {
            var window = NewWindow(width, height, reduceMotion);
            try
            {
                window.Show();
                window.Activate();
                // The page with the longest name, beside the readout.
                var longest = CatalogPages.All.MaxBy(p => p.Title.Length)!;
                window.SelectPage(CatalogPages.All.ToList().IndexOf(longest));
                Settle(window);
                Settle(window);

                var label = $"{width}x{height}{(reduceMotion ? " reduced" : "")}";
                TextBlock[] row = [window.StatusPage, window.StatusFrames, window.StatusVersion];
                foreach (var block in row)
                    if (Natural(block) > block.Bounds.Width + 1)
                        problems.Add($"{label}: \"{block.Text}\" is clipped ({block.Bounds.Width:0} wide, needs {Natural(block):0})");
                for (var i = 1; i < row.Length; i++)
                    if (Right(row[i - 1], window) > Left(row[i], window) + 0.5)
                        problems.Add($"{label}: \"{row[i - 1].Text}\" runs into \"{row[i].Text}\"");
                if (Right(window.StatusVersion, window) > Right(window.StatusRow, window) + 0.5)
                    problems.Add($"{label}: the version runs past the status bar");
                if (width == 1200 && !window.StatusFrames.Text!.Contains("High refresh rate:"))
                    problems.Add($"{label}: the full readout does not show at the default width (\"{window.StatusFrames.Text}\")");
            }
            finally
            {
                window.Close();
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    private static double Natural(TextBlock block) =>
        string.IsNullOrEmpty(block.Text) ? 0 : new FormattedText(block.Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(block.FontFamily, block.FontStyle, block.FontWeight), block.FontSize, null).Width;

    private static double Left(Control control, Window window) => control.TranslatePoint(default, window)!.Value.X;

    private static double Right(Control control, Window window) => control.TranslatePoint(new Point(control.Bounds.Width, 0), window)!.Value.X;

    private static MainWindow NewWindow(double width, double height, bool reduceMotion)
    {
        var window = new MainWindow { Width = width, Height = height };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion });
        return window;
    }

    // Render passes, each after the jobs before it, as the render timer would run them.
    private static void Frames(Window window, int count)
    {
        for (var i = 0; i < count; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static void Settle(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }
}
