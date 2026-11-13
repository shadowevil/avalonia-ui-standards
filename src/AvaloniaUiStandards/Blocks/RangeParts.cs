using Avalonia;
using Avalonia.Controls;
using Avalonia.Reactive;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Blocks;

/// <summary>The values shared by the range themes (Slider, ProgressBar, ProgressRing).</summary>
internal static class RangeParts
{
    /// <summary>The thickness of the slider and progress bar tracks.</summary>
    public const double TrackThickness = 4;

    /// <summary>The density sizes of the ranges.</summary>
    internal sealed record Sizes(double Thumb, double Ring);

    /// <summary>Normal: slider thumb 20; ring 20.</summary>
    public static readonly Sizes Normal = new(20, 20);

    /// <summary>Compact: slider thumb 16; ring 16.</summary>
    public static readonly Sizes Compact = new(16, 16);

    // Density values, set by ApplyDensity under these non-public keys.
    internal static readonly object ThumbKey = new();
    internal static readonly object RingKey = new();

    public static void ApplyDensity(IResourceDictionary resources, Sizes sizes)
    {
        resources[ThumbKey] = sizes.Thumb;
        resources[RingKey] = sizes.Ring;
    }
}

/// <summary>
/// The running time of a Block's continuous animation (decision 0016), from the frame ticks of the
/// window it is in (<see cref="IFrameTickSource"/>): 30 per second by default, none while motion is
/// reduced, paused while the window is inactive. It follows the ticks only while its owner wants it
/// to run (<see cref="Run"/>) and it and every visual ancestor are visible, so a hidden ancestor (a
/// collapsed busy overlay) stops the ticks. Outside such a window, and while motion is reduced, the
/// time stays at zero, the animation's still frame.
/// </summary>
internal sealed class ContinuousClock
{
    private readonly Control _owner;
    private readonly Action _changed;
    private IDisposable? _ticks;
    private IDisposable? _motion;
    // The owner and its visual ancestors while attached, watched for IsVisible changes
    // (Avalonia 12 exposes no public effective-visibility change event).
    private readonly List<Visual> _chain = [];
    private bool _wanted;
    private bool _attached;
    private bool _reduced;

    public ContinuousClock(Control owner, Action changed)
    {
        _owner = owner;
        _changed = changed;
        // The clock lives as long as its owner, so these handlers need no removal.
        owner.AttachedToVisualTree += (_, _) => Update(attached: true);
        owner.DetachedFromVisualTree += (_, _) => Update(attached: false);
        if (owner.IsAttachedToVisualTree())
            Update(attached: true);
    }

    /// <summary>The animation's running time; zero at its start and while it does not run.</summary>
    public TimeSpan Time { get; private set; }

    /// <summary>Whether the clock follows frame ticks now.</summary>
    internal bool IsRunning => _ticks is not null;

    /// <summary>Whether the owner wants the animation to run (for example while indeterminate).</summary>
    public void Run(bool run)
    {
        _wanted = run;
        Update(_attached);
    }

    /// <summary>Sets the running time (frame ticks, and tests).</summary>
    internal void Set(TimeSpan time)
    {
        Time = time;
        _changed();
    }

    private void OnChainPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.IsVisibleProperty)
            Update(_attached);
    }

    /// <summary>One frame tick: the time advances by <paramref name="elapsed"/> unless motion is reduced.</summary>
    internal void OnTick(TimeSpan elapsed)
    {
        if (!_reduced)
            Set(Time + elapsed);
    }

    private void Update(bool attached)
    {
        if (attached != _attached)
        {
            foreach (var visual in _chain)
                visual.PropertyChanged -= OnChainPropertyChanged;
            _chain.Clear();
            if (attached)
                for (Visual? visual = _owner; visual is not null; visual = visual.GetVisualParent())
                {
                    visual.PropertyChanged += OnChainPropertyChanged;
                    _chain.Add(visual);
                }
            _attached = attached;
        }
        var run = _wanted && attached && _chain.TrueForAll(v => v.IsVisible);
        if (run && _ticks is null)
        {
            _ticks = FrameTicks.For(_owner)?.SubscribeFrameTicks(OnTick) ?? FrameTicks.Subscription(() => { });
            // Reduce motion makes every motion duration zero; the animation then holds its still frame.
            _motion = _owner.GetResourceObservable("MotionNormal").Subscribe(new AnonymousObserver<object?>(v =>
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
}
