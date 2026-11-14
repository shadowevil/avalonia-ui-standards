using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Catalog;

/// <summary>
/// Measures the frames per second a window actually renders, without causing any. A composition custom visual that
/// draws nothing is laid over the whole window; the compositor calls its <c>OnRender</c> once for every frame in
/// which any part of the window is redrawn (its bounds cover every dirty region) and never otherwise. It never
/// invalidates itself or requests frames, so the window renders exactly as often as without it.
/// </summary>
/// <remarks>
/// The count is read twice a second. A changed reading repaints its own text once, which the next reading discounts
/// when that frame is the only one, so an idle window settles at 0 instead of measuring its own readout. While the
/// window stays idle the reading timer stops; the next rendered frame restarts it.
/// </remarks>
internal sealed class FrameRateMeter : IDisposable
{
    /// <summary>How often the count is read.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(500);

    private readonly Visual _host;
    private readonly Action<int> _changed;
    private readonly Counter _counter;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _clock = new();
    private CompositionCustomVisual? _visual;
    private bool _changedLastTime;

    /// <param name="host">The window whose frames are counted.</param>
    /// <param name="changed">Called with the frames per second whenever the reading changes.</param>
    public FrameRateMeter(Visual host, Action<int> changed)
    {
        _host = host;
        _changed = changed;
        _counter = new Counter(() => Dispatcher.UIThread.Post(Wake));
        // Started by the first frame (Wake), not here.
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = Interval };
        _timer.Tick += (_, _) => Read();
    }

    /// <summary>The last reading, in frames per second.</summary>
    public int FramesPerSecond { get; private set; }

    /// <summary>Whether the reading timer is stopped because the window renders nothing.</summary>
    public bool IsIdle => !_timer.IsEnabled;

    /// <summary>Lays the counting visual over the host, once the host has its composition visual (after it is shown).</summary>
    public void Attach()
    {
        if (_visual is not null || ElementComposition.GetElementVisual(_host) is not { } hostVisual)
            return;
        _visual = hostVisual.Compositor.CreateCustomVisual(_counter);
        Resize();
        ElementComposition.SetElementChildVisual(_host, _visual);
        Wake();
    }

    /// <summary>
    /// Keeps the counting visual the host's size, so it meets every dirty region; lays it over the host first if the
    /// host had no composition visual yet when <see cref="Attach"/> was called.
    /// </summary>
    public void Resize()
    {
        if (_visual is null)
            Attach();
        else
            _visual.Size = new Vector(_host.Bounds.Width, _host.Bounds.Height);
    }

    /// <summary>Whether the counting visual is laid over the host.</summary>
    internal bool IsAttached => _visual is not null;

    /// <summary>Reads the count now (the timer does this twice a second).</summary>
    internal void Read()
    {
        var seconds = _clock.Elapsed.TotalSeconds;
        _clock.Restart();
        var frames = _counter.Take();

        // The frame that showed the previous reading, when it is the only one, is the readout's own.
        if (_changedLastTime && frames <= 1)
            frames = 0;
        var fps = seconds > 0 ? (int)Math.Round(frames / seconds) : 0;

        _changedLastTime = fps != FramesPerSecond;
        if (_changedLastTime)
        {
            FramesPerSecond = fps;
            _changed(fps);
        }
        else if (fps == 0)
        {
            // Idle: stop reading until the next frame. The wake is armed before the count is checked again, so a
            // frame drawn just before it was armed still wakes the reading (and stays counted for it).
            _timer.Stop();
            _counter.WakeOnNextFrame = true;
            if (_counter.HasFrames)
            {
                _counter.WakeOnNextFrame = false;
                Wake();
            }
        }
    }

    private void Wake()
    {
        if (_timer.IsEnabled)
            return;
        _clock.Restart();
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        if (_visual is not null)
            ElementComposition.SetElementChildVisual(_host, null);
        _visual = null;
    }

    // Runs on the render thread: counts the frames it is drawn in, and draws nothing.
    private sealed class Counter(Action wake) : CompositionCustomVisualHandler
    {
        private int _frames;
        private int _wake;

        public bool WakeOnNextFrame
        {
            set => Volatile.Write(ref _wake, value ? 1 : 0);
        }

        public int Take() => Interlocked.Exchange(ref _frames, 0);

        public bool HasFrames => Volatile.Read(ref _frames) > 0;

        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            Interlocked.Increment(ref _frames);
            if (Interlocked.Exchange(ref _wake, 0) == 1)
                wake();
        }
    }
}
