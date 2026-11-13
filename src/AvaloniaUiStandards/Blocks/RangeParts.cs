using Avalonia;
using Avalonia.Controls;
using Avalonia.Reactive;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Blocks;

/// <summary>The values shared by the range themes (Slider, ProgressBar, ProgressRing).</summary>
internal static class RangeParts
{
    /// <summary>The thickness of the slider and progress bar tracks.</summary>
    public const double TrackThickness = 4;

    /// <summary>A corner larger than any side: the shapes clamp it to half the shorter side, a pill (or circle).</summary>
    public static readonly CornerRadius Pill = new(1000);

    /// <summary>The density sizes of the ranges.</summary>
    internal sealed record Sizes(double Row, double Thumb, double Ring);

    /// <summary>Normal: slider row 32, thumb 20; ring 20.</summary>
    public static readonly Sizes Normal = new(32, 20, 20);

    /// <summary>Compact: slider row 24, thumb 16; ring 16.</summary>
    public static readonly Sizes Compact = new(24, 16, 16);

    // Density values, set by ApplyDensity under these non-public keys.
    internal static readonly object RowKey = new();
    internal static readonly object ThumbKey = new();
    internal static readonly object RingKey = new();

    public static void ApplyDensity(IResourceDictionary resources, Sizes sizes)
    {
        resources[RowKey] = sizes.Row;
        resources[ThumbKey] = sizes.Thumb;
        resources[RingKey] = sizes.Ring;
    }
}

/// <summary>
/// The running time of a Block's continuous animation (decision 0016), from the frame ticks of the
/// window it is in (<see cref="IFrameTickSource"/>): 30 per second by default, none while motion is
/// reduced, paused while the window is inactive. Outside such a window, and while motion is reduced,
/// the time stays at zero, the animation's still frame.
/// </summary>
internal sealed class ContinuousClock(Control owner, Action changed)
{
    private IDisposable? _ticks;
    private IDisposable? _motion;
    private bool _reduced;

    /// <summary>The animation's running time; zero at its start and while it does not run.</summary>
    public TimeSpan Time { get; private set; }

    /// <summary>Starts or stops following the window's frame ticks.</summary>
    public void Run(bool run)
    {
        if (run && _ticks is null)
        {
            _ticks = FrameTicks.For(owner)?.SubscribeFrameTicks(OnTick) ?? FrameTicks.Subscription(() => { });
            // Reduce motion makes every motion duration zero; the animation then holds its still frame.
            _motion = owner.GetResourceObservable("MotionNormal").Subscribe(new AnonymousObserver<object?>(v =>
            {
                _reduced = v is TimeSpan t && t == TimeSpan.Zero;
                if (_reduced)
                    Set(TimeSpan.Zero);
            }));
        }
        else if (!run && _ticks is not null)
        {
            _ticks.Dispose();
            _motion?.Dispose();
            _ticks = _motion = null;
            Set(TimeSpan.Zero);
        }
    }

    /// <summary>Sets the running time (frame ticks, and tests).</summary>
    internal void Set(TimeSpan time)
    {
        Time = time;
        changed();
    }

    private void OnTick(TimeSpan elapsed)
    {
        if (!_reduced)
            Set(Time + elapsed);
    }
}
