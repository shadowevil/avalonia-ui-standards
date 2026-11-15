using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// The opaque regions of one top level and who follows them: the glass that skips the opaque regions of its own
/// content, the shared backdrop samplers and the windows that clip their backdrop. Recomputed after every layout pass
/// (moves, resizes, scrolling, visibility) and, through a queued layout pass, after a change of opacity, transform,
/// clip or effect on an opaque control or its ancestors, so the regions always match the frame they are drawn in.
/// </summary>
internal sealed class BackdropRegions
{
    private static readonly ConditionalWeakTable<TopLevel, BackdropRegions> All = new();

    private readonly TopLevel _root;
    private readonly HashSet<Visual> _sources = [];
    private readonly HashSet<GlassBorder> _glass = [];
    private readonly HashSet<GlassBackdropSampler> _samplers = [];
    private readonly List<Action> _listeners = [];
    private bool _subscribed;

    private BackdropRegions(TopLevel root) => _root = root;

    static BackdropRegions()
    {
        foreach (var property in new AvaloniaProperty[]
        {
            Visual.OpacityProperty, Visual.OpacityMaskProperty, Visual.RenderTransformProperty, Visual.ClipProperty,
            Visual.ClipToBoundsProperty, Visual.EffectProperty,
        })
            property.Changed.Subscribe(new Observer(e => OnVisualChanged(e.Sender as Visual)));
    }

    /// <summary>The opaque regions in the top level's coordinates, each with the control that paints it.</summary>
    public IReadOnlyList<(Visual Source, Rect[] Rects)> Opaque { get; private set; } = [];

    /// <summary>
    /// How far, in px, glass in the top level reads the backdrop beyond a visible pixel: three blur sigmas plus the
    /// furthest refraction displacement. Opaque areas are shrunk by this much before the backdrop is cut out under
    /// them. 0 when no glass draws as glass.
    /// </summary>
    public double Reach { get; private set; }

    internal static BackdropRegions? For(Visual visual, bool create = true)
    {
        if (TopLevel.GetTopLevel(visual) is not { } root)
            return null;
        return create ? All.GetValue(root, r => new BackdropRegions(r)) : All.TryGetValue(root, out var regions) ? regions : null;
    }

    public static void AddSource(Visual source)
    {
        if (For(source) is { } regions && regions._sources.Add(source))
            regions.Changed();
    }

    public static void RemoveSource(Visual source, Visual? root = null)
    {
        var regions = root is TopLevel top && All.TryGetValue(top, out var r) ? r : For(source, create: false);
        if (regions is not null && regions._sources.Remove(source))
            regions.Changed();
    }

    public void AddGlass(GlassBorder glass)
    {
        if (_glass.Add(glass))
            Changed();
    }

    public void RemoveGlass(GlassBorder glass)
    {
        if (_glass.Remove(glass))
            Changed();
    }

    public void AddSampler(GlassBackdropSampler sampler)
    {
        if (_samplers.Add(sampler))
            Changed();
    }

    public void RemoveSampler(GlassBackdropSampler sampler) => _samplers.Remove(sampler);

    /// <summary>Calls <paramref name="listener"/> after every update; the returned object stops it.</summary>
    public IDisposable Listen(Action listener)
    {
        _listeners.Add(listener);
        Changed();
        return new Subscription(() =>
        {
            _listeners.Remove(listener);
            Subscribe();
        });
    }

    /// <summary>The glass in the top level.</summary>
    public IEnumerable<GlassBorder> Glass => _glass;

    /// <summary>The opaque rectangles of the sources that <paramref name="include"/> accepts, in <paramref name="target"/>'s coordinates.</summary>
    public List<Rect> OpaqueIn(Visual target, Func<Visual, bool> include)
    {
        var result = new List<Rect>();
        if (Opaque.Count == 0 || _root.TransformToVisual(target) is not { } toTarget)
            return result;
        foreach (var (source, rects) in Opaque)
            if (include(source))
                foreach (var r in rects)
                    result.Add(r.TransformToAABB(toTarget));
        return result;
    }

    /// <summary>Recomputes after a change in how glass draws: now when layout is settled, else after the coming layout pass.</summary>
    public void Refresh() => Changed();

    /// <summary>Draws every glass again (each chooses its fill again) and recomputes.</summary>
    public void Redraw()
    {
        foreach (var glass in _glass)
            glass.InvalidateVisual();
        foreach (var sampler in _samplers)
            sampler.InvalidateVisual();
        Changed();
    }

    // A change that may move a region: recomputed now when layout is settled, else after the coming layout pass.
    private void Changed()
    {
        Subscribe();
        if (_root.IsArrangeValid)
            Update();
    }

    private void Subscribe()
    {
        var needed = _sources.Count > 0 || _samplers.Count > 0 || _listeners.Count > 0;
        if (needed == _subscribed)
            return;
        _subscribed = needed;
        if (needed)
            _root.LayoutUpdated += OnLayoutUpdated;
        else
            _root.LayoutUpdated -= OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e) => Update();

    private static void OnVisualChanged(Visual? visual)
    {
        if (visual is null || For(visual, create: false) is not { _sources.Count: > 0 } regions)
            return;
        if (regions._sources.Any(s => s == visual || visual.IsVisualAncestorOf(s)))
            regions._root.InvalidateArrange();
    }

    internal void Update()
    {
        var opaque = new List<(Visual, Rect[])>();
        foreach (var source in _sources)
            if (OpaqueRects(source) is { Length: > 0 } rects)
                opaque.Add((source, rects));
        Opaque = opaque;

        var reach = 0.0;
        foreach (var glass in _glass)
            if (!glass.IsSolid)
                reach = Math.Max(reach, glass.ReadReach);
        Reach = reach;

        foreach (var glass in _glass.ToArray())
            glass.SetSkip(SkipFor(glass));
        foreach (var sampler in _samplers.ToArray())
            sampler.UpdateRegion(this);
        foreach (var listener in _listeners.ToArray())
            listener();
    }

    // The opaque regions of the glass's own content, in its coordinates.
    private Rect[] SkipFor(GlassBorder glass)
    {
        if (glass.IsSolid || Opaque.Count == 0)
            return [];
        var size = new Rect(glass.Bounds.Size);
        return OpaqueIn(glass, s => s != glass && glass.IsVisualAncestorOf(s))
            .Select(r => r.Intersect(size)).Where(RectSet.NotEmpty).ToArray();
    }

    /// <summary>The surely opaque rectangles of <paramref name="source"/> in the top level's coordinates, or null.</summary>
    private Rect[]? OpaqueRects(Visual source)
    {
        if (!source.IsEffectivelyVisible || source.Bounds.Width <= 0 || source.Bounds.Height <= 0)
            return null;
        if (source is GlassBorder glass && !glass.IsOpaqueFill)
            return null;
        if (source.TransformToVisual(_root) is not { } toRoot || toRoot.M12 != 0 || toRoot.M21 != 0)
            return null;

        List<Rect> rects = [.. RectSet.Cross(new Rect(source.Bounds.Size), CornerInset(source)).Select(r => r.TransformToAABB(toRoot))];
        for (Visual? v = source; v is not null && v != _root; v = v.GetVisualParent())
        {
            if (v.Opacity < 1 || v.OpacityMask is not null || v.Effect is not null)
                return null;
            if (v != source && (v.ClipToBounds || v.Clip is not null))
            {
                if (ClipRects(v) is not { } clip || v.TransformToVisual(_root) is not { } clipToRoot)
                    return null;
                rects = RectSet.Intersect(rects, clip.Select(c => c.TransformToAABB(clipToRoot)).ToArray());
            }
            else if (v == source && v.Clip is not null && ClipRects(v) is { } own)
            {
                rects = RectSet.Intersect(rects, own.Select(c => c.TransformToAABB(toRoot)).ToArray());
            }
        }
        return rects.ToArray();
    }

    // How far in from the edges a control's rounded corners reach: a squircle's corner curve starts further out than
    // a circular one of the same radius.
    private static double CornerInset(Visual v) => v switch
    {
        GlassBorder g => 2 * Max(g.CornerRadius),
        SquircleBorder s => 2 * Max(s.CornerRadius),
        Border b => Max(b.CornerRadius),
        TemplatedControl t => Max(t.CornerRadius),
        _ => 0,
    };

    private static double Max(CornerRadius r) => Math.Max(Math.Max(r.TopLeft, r.TopRight), Math.Max(r.BottomLeft, r.BottomRight));

    // The surely visible part of what a visual clips its children to, in its coordinates; null when it cannot be told.
    private static Rect[]? ClipRects(Visual v)
    {
        var bounds = new Rect(v.Bounds.Size);
        if (v.Clip is { } clip)
        {
            // GlassBorder and SquircleBorder clip to their own squircle.
            if (v is GlassBorder or SquircleBorder)
                return RectSet.Cross(bounds, CornerInset(v));
            if (clip is RectangleGeometry { Transform: null } rect)
                return v.ClipToBounds ? RectSet.Intersect([rect.Rect], [bounds]).ToArray() : [rect.Rect];
            return null;
        }
        return RectSet.Cross(bounds, v is Border or TemplatedControl ? CornerInset(v) : 0);
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }

    private sealed class Observer(Action<AvaloniaPropertyChangedEventArgs> next) : IObserver<AvaloniaPropertyChangedEventArgs>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(AvaloniaPropertyChangedEventArgs value) => next(value);
    }
}
