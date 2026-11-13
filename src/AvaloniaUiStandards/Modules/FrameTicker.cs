namespace AvaloniaUiStandards.Modules;

/// <summary>Arguments of <see cref="StandardsWindow.FrameTick"/>.</summary>
public sealed class FrameTickEventArgs(TimeSpan time, TimeSpan elapsed) : EventArgs
{
    /// <summary>The frame's time, on the window's animation clock.</summary>
    public TimeSpan Time { get; } = time;

    /// <summary>Time since the previous tick; zero on the first tick after the ticks start or resume.</summary>
    public TimeSpan Elapsed { get; } = elapsed;
}

/// <summary>Where a <see cref="FrameTicker"/> gets its frames and waits.</summary>
internal interface IFrameScheduler
{
    /// <summary>Calls <paramref name="callback"/> on the next rendered frame, with its time.</summary>
    void RequestFrame(Action<TimeSpan> callback);

    /// <summary>Calls <paramref name="callback"/> after <paramref name="delay"/>.</summary>
    void Delay(TimeSpan delay, Action callback);
}

/// <summary>
/// Delivers frame ticks for continuous animation: at the display rate when high, otherwise at
/// most <see cref="StandardRate"/> per second. At the standard rate it waits between ticks
/// instead of taking every display frame, then takes the next real frame, so no frames are
/// requested in between and ticks stay aligned to rendered frames.
/// </summary>
internal sealed class FrameTicker(IFrameScheduler scheduler, Action<TimeSpan, TimeSpan> tick)
{
    /// <summary>The standard rate for continuous animation, in ticks per second.</summary>
    public const int StandardRate = 30;

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1.0 / StandardRate);

    // A frame may arrive this much early and still count; the wait ends this much before the
    // interval, so the next display frame after it is the one taken.
    private static readonly TimeSpan Tolerance = TimeSpan.FromMilliseconds(4);
    private static readonly TimeSpan Lead = TimeSpan.FromMilliseconds(8);

    private int _generation;
    private TimeSpan? _last;

    public bool IsRunning { get; private set; }

    public bool HighRate { get; set; }

    public void Start()
    {
        if (IsRunning)
            return;
        IsRunning = true;
        _last = null;
        RequestNext(++_generation, wait: false);
    }

    public void Stop()
    {
        if (!IsRunning)
            return;
        IsRunning = false;
        _generation++;
    }

    private void RequestNext(int generation, bool wait)
    {
        if (wait && !HighRate)
            scheduler.Delay(Interval - Lead, () =>
            {
                if (generation == _generation)
                    scheduler.RequestFrame(time => OnFrame(generation, time));
            });
        else
            scheduler.RequestFrame(time => OnFrame(generation, time));
    }

    private void OnFrame(int generation, TimeSpan time)
    {
        if (generation != _generation)
            return;
        // At the standard rate a frame that comes too early is passed over for the next one.
        if (!HighRate && _last is { } previous && time - previous < Interval - Tolerance)
        {
            RequestNext(generation, wait: false);
            return;
        }
        var elapsed = _last is { } last ? time - last : TimeSpan.Zero;
        _last = time;
        tick(time, elapsed);
        if (generation == _generation)
            RequestNext(generation, wait: true);
    }
}
