using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using SkiaSharp;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Where first-level glass reads the shared backdrop sample: set on a container (the plane), it names the sampler that
/// draws just before it; <see cref="Excluded"/> on a container inside a plane makes its glass take its own backdrop.
/// The nearest container with a plane decides.
/// </summary>
internal sealed class BackdropPlane(GlassBackdropSampler? sampler)
{
    public static readonly BackdropPlane Excluded = new(null);

    public GlassBackdropSampler? Sampler { get; } = sampler;

    public static readonly AttachedProperty<BackdropPlane?> PlaneProperty =
        AvaloniaProperty.RegisterAttached<Visual, BackdropPlane?>("Plane", typeof(BackdropPlane));

    public static void SetPlane(Visual visual, BackdropPlane? plane) => visual.SetValue(PlaneProperty, plane);

    /// <summary>The sampler <paramref name="visual"/> reads, from the nearest ancestor with a plane; null when none.</summary>
    public static GlassBackdropSampler? SamplerFor(Visual visual)
    {
        for (var v = visual.GetVisualParent(); v is not null; v = v.GetVisualParent())
            if (v.GetValue(PlaneProperty) is { } plane)
                return plane.Sampler;
        return null;
    }
}

/// <summary>
/// Takes the shared backdrop sample: placed right after a window's backdrop, it copies what is drawn so far at
/// <see cref="GlassBorder.DefaultBackdropScale"/> of the device resolution into a texture it keeps, and blurs it once,
/// only where first-level glass on its plane reads it (the glass less the opaque regions of its content, widened by the
/// refraction's reach). Each such glass fill then draws that one blurred sample through its refraction and tint,
/// instead of blurring the backdrop itself. Only the redrawn region of a frame is copied and blurred again.
/// </summary>
internal sealed class GlassBackdropSampler : Avalonia.Controls.Control
{
    /// <summary>Whether first-level glass on a plane reads the shared sample; false draws every fill with its own backdrop blur, as a measurement baseline.</summary>
    internal static bool Enabled
    {
        get => _enabled;
        set
        {
            if (value == _enabled)
                return;
            _enabled = value;
            // Every glass on a plane chooses its fill again, and every sampler its consumers.
            foreach (var weak in Live.ToArray())
                if (weak.TryGetTarget(out var sampler))
                    sampler._regions?.Redraw();
                else
                    Live.Remove(weak);
        }
    }

    private static bool _enabled = true;
    private static readonly List<WeakReference<GlassBackdropSampler>> Live = [];
    private readonly WeakReference<GlassBackdropSampler> _self;

    public static readonly StyledProperty<double> BlurSigmaProperty =
        AvaloniaProperty.Register<GlassBackdropSampler, double>(nameof(BlurSigma), GlassBorder.DefaultBlurSigma);

    internal static readonly StyledProperty<double> BackdropScaleProperty =
        AvaloniaProperty.Register<GlassBackdropSampler, double>("BackdropScale", GlassBorder.DefaultBackdropScale);

    private BackdropRegions? _regions;

    static GlassBackdropSampler()
    {
        AffectsRender<GlassBackdropSampler>(BlurSigmaProperty, BackdropScaleProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<GlassBackdropSampler>(false);
    }

    public GlassBackdropSampler()
    {
        Plane = new BackdropPlane(this);
        _self = new WeakReference<GlassBackdropSampler>(this);
    }

    /// <summary>The plane to set on the container whose glass reads this sampler.</summary>
    public BackdropPlane Plane { get; }

    public double BlurSigma
    {
        get => GetValue(BlurSigmaProperty);
        set => SetValue(BlurSigmaProperty, value);
    }

    internal double BackdropScale => GetValue(BackdropScaleProperty);

    /// <summary>The render-thread state the sample operation writes and the fills read.</summary>
    internal SharedBackdropSample Sample { get; } = new();

    /// <summary>The glass that reads the sample, each with what it covers.</summary>
    internal IReadOnlyList<SampleConsumer> Consumers { get; private set; } = [];

    /// <summary>The rectangles blurred, in this control's coordinates.</summary>
    internal IReadOnlyList<Rect> Targets { get; private set; } = [];

    // Draws nothing a user sees: hidden from assistive technology.
    protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new Avalonia.Automation.Peers.NoneAutomationPeer(this);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _regions = BackdropRegions.For(this);
        _regions?.AddSampler(this);
        Live.Add(_self);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _regions?.RemoveSampler(this);
        _regions = null;
        Live.Remove(_self);
        Consumers = [];
        Targets = [];
    }

    /// <summary>Whether <paramref name="glass"/> may read this sampler: first-level glass drawn as glass with the sampler's blur.</summary>
    internal bool Accepts(GlassBorder glass) =>
        Enabled && !glass.IsSolid && !glass.Nested && glass.BlurSigma == BlurSigma && glass.BackdropScale == BackdropScale;

    /// <summary>Recomputes the consumers and the blurred rectangles; draws again when they change.</summary>
    internal void UpdateRegion(BackdropRegions regions)
    {
        var consumers = new List<SampleConsumer>();
        var targets = new List<Rect>();
        var size = new Rect(Bounds.Size);
        foreach (var glass in regions.Glass)
        {
            if (glass.Sampler != this || !Accepts(glass) || glass.TransformToVisual(this) is not { } toSampler)
                continue;
            var rect = new Rect(glass.Bounds.Size).TransformToAABB(toSampler);
            var skip = glass.Skip;
            consumers.Add(new SampleConsumer(glass, skip));
            var reach = glass.RefractionReach;
            foreach (var visible in RectSet.Subtract([rect], skip.Select(s => s.TransformToAABB(toSampler))))
            {
                var target = visible.Inflate(reach).Intersect(rect).Intersect(size);
                if (RectSet.NotEmpty(target))
                    targets.Add(target);
            }
        }
        if (consumers.SequenceEqual(Consumers) && targets.SequenceEqual(Targets))
            return;
        Consumers = consumers;
        Targets = targets;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (Consumers.Count > 0 && Enabled)
            context.Custom(new GlassSampleOperation(Sample, new Rect(Bounds.Size), Targets.ToArray(), Consumers.ToArray(), BlurSigma, BackdropScale));
    }
}

/// <summary>A glass that reads the shared sample, with the opaque regions it skips (the sample leaves them out). Where it is does not matter: the fill maps itself to the sample when it draws, and the targets follow it.</summary>
internal readonly record struct SampleConsumer(GlassBorder Glass, Rect[] Skip)
{
    public bool Equals(SampleConsumer other) => Glass == other.Glass && ReferenceEquals(Skip, other.Skip);

    public override int GetHashCode() => Glass.GetHashCode();
}

/// <summary>
/// The shared sample as the render thread sees it: the blurred image of this frame's sample operation, how it maps to
/// the device, and the consumers it covers.
/// </summary>
internal sealed class SharedBackdropSample
{
    internal SKImage? Image;
    internal SKPointI Origin;
    internal float Scale;
    internal SampleConsumer[] Covered = [];
    internal object? Owner;

    /// <summary>Whether the sample covers <paramref name="consumer"/> as it is now.</summary>
    public bool Covers(SampleConsumer consumer) => Image is not null && Array.IndexOf(Covered, consumer) >= 0;

    internal void Clear(object owner)
    {
        if (Owner != owner)
            return;
        Image?.Dispose();
        Image = null;
        Covered = [];
        Owner = null;
    }
}

/// <summary>
/// Copies the redrawn part of the backdrop into the kept, downscaled texture and blurs it where glass reads it. It owns
/// its textures: a new operation (a change of consumers or targets) starts with fresh ones, and Avalonia redraws its
/// whole bounds then, so they are filled in full.
/// </summary>
internal sealed class GlassSampleOperation : ICustomDrawOperation
{
    private readonly SharedBackdropSample _sample;
    private readonly Rect _bounds;
    private readonly Rect[] _targets;
    private readonly SampleConsumer[] _consumers;
    private readonly double _sigma;
    private readonly double _backdropScale;

    public GlassSampleOperation(SharedBackdropSample sample, Rect bounds, Rect[] targets, SampleConsumer[] consumers, double sigma, double backdropScale)
    {
        _sample = sample;
        _bounds = bounds;
        _targets = targets;
        _consumers = consumers;
        _sigma = sigma;
        _backdropScale = backdropScale;
    }

    private static readonly SKSamplingOptions Linear = new(SKFilterMode.Linear, SKMipmapMode.None);

    private SKSurface? _down, _blurred;
    private SKSizeI _size;
    private float _scale;

    public Rect Bounds => _bounds;

    public bool HitTest(Point p) => false;

    public bool Equals(ICustomDrawOperation? other) => other is GlassSampleOperation o && o._bounds == _bounds
        && ReferenceEquals(o._sample, _sample) && o._targets.AsSpan().SequenceEqual(_targets) && o._consumers.AsSpan().SequenceEqual(_consumers)
        && o._sigma == _sigma && o._backdropScale == _backdropScale;


    public override bool Equals(object? obj) => obj is ICustomDrawOperation o && Equals(o);

    public override int GetHashCode() => HashCode.Combine(_bounds, _targets.Length, _consumers.Length);

    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } feature)
            return;
        using var lease = feature.Lease();
        if (lease.SkSurface is not { } surface)
        {
            _sample.Clear(this);
            return;
        }
        var canvas = lease.SkCanvas;
        var m = canvas.TotalMatrix;
        var deviceScale = Math.Sqrt(m.ScaleX * m.ScaleX + m.SkewY * m.SkewY);
        if (deviceScale <= 0)
            return;
        var k = (float)Math.Min(1, _backdropScale / deviceScale);
        var device = SKRectI.Ceiling(m.MapRect(new SKRect(0, 0, (float)_bounds.Width, (float)_bounds.Height)));
        device = new SKRectI(Math.Max(device.Left, 0), Math.Max(device.Top, 0), device.Right, device.Bottom);
        if (device.Width <= 0 || device.Height <= 0)
            return;

        var size = new SKSizeI((int)Math.Ceiling(device.Width * k), (int)Math.Ceiling(device.Height * k));
        if (_down is null || _size != size || _scale != k)
        {
            _down?.Dispose();
            _blurred?.Dispose();
            var info = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            _down = NewSurface(surface, info);
            _blurred = NewSurface(surface, info);
            _size = size;
            _scale = k;
            if (_down is null || _blurred is null)
            {
                _sample.Clear(this);
                return;
            }
        }

        // Texels: (device - origin) * k. The redrawn region, in whole texels inside it.
        var clip = canvas.DeviceClipBounds;
        clip.Intersect(device);
        if (clip.Width <= 0 || clip.Height <= 0)
            return;
        SKRect ToTexels(SKRect d) => new((d.Left - device.Left) * k, (d.Top - device.Top) * k, (d.Right - device.Left) * k, (d.Bottom - device.Top) * k);
        var redrawn = ToTexels(new SKRect(clip.Left, clip.Top, clip.Right, clip.Bottom));
        var fresh = new SKRect(MathF.Ceiling(redrawn.Left), MathF.Ceiling(redrawn.Top), MathF.Floor(redrawn.Right), MathF.Floor(redrawn.Bottom));
        // At the window's edges the redrawn region reaches the texture's edge.
        if (clip.Right >= device.Right) fresh.Right = size.Width;
        if (clip.Bottom >= device.Bottom) fresh.Bottom = size.Height;
        if (clip.Left <= device.Left) fresh.Left = 0;
        if (clip.Top <= device.Top) fresh.Top = 0;

        var sigmaTexels = (float)(_sigma * deviceScale * k);
        var blurReach = MathF.Ceiling(3 * sigmaTexels) + 1;
        var texelTargets = _targets.Select(t => ToTexels(m.MapRect(new SKRect((float)t.X, (float)t.Y, (float)t.Right, (float)t.Bottom)))).ToArray();

        // The copy: what the blur reads, inside the fresh texels.
        using (var copyArea = new SKPath())
        {
            foreach (var t in texelTargets)
            {
                var r = SKRect.Inflate(t, blurReach, blurReach);
                if (r.IntersectsWith(fresh))
                    copyArea.AddRect(SKRect.Intersect(r, fresh));
            }
            if (!copyArea.IsEmpty)
            {
                var b = copyArea.Bounds;
                var from = SKRectI.Ceiling(new SKRect(device.Left + b.Left / k - 1, device.Top + b.Top / k - 1, device.Left + b.Right / k + 1, device.Top + b.Bottom / k + 1));
                from.Intersect(clip);
                using var copy = from.Width > 0 && from.Height > 0 ? surface.Snapshot(from) : null;
                if (copy is not null)
                {
                    var down = _down.Canvas;
                    down.Save();
                    down.ClipPath(copyArea);
                    down.Scale(k);
                    down.DrawImage(copy, from.Left - device.Left, from.Top - device.Top, Linear);
                    down.Restore();
                }
            }
        }

        // The blur: each target near the fresh texels, again (blurred texels within the blur's reach of a changed
        // texel change, and refraction reads up to the target's edge).
        var stale = SKRect.Inflate(fresh, blurReach, blurReach);
        _sample.Image?.Dispose();
        _sample.Image = null;
        using (var downImage = _down!.Snapshot())
        using (var blur = SKImageFilter.CreateBlur(sigmaTexels, sigmaTexels, SKShaderTileMode.Clamp))
        using (var paint = new SKPaint { ImageFilter = blur, BlendMode = SKBlendMode.Src })
        {
            var target = _blurred!.Canvas;
            foreach (var t in texelTargets)
            {
                var r = SKRect.Intersect(SKRect.Inflate(t, 1, 1), stale);
                if (r.Width <= 0 || r.Height <= 0)
                    continue;
                target.Save();
                target.ClipRect(r);
                target.DrawImage(downImage, 0, 0, paint);
                target.Restore();
            }
        }

        _sample.Image = _blurred.Snapshot();
        _sample.Origin = new SKPointI(device.Left, device.Top);
        _sample.Scale = k;
        _sample.Covered = _consumers;
        _sample.Owner = this;
    }

    private static SKSurface? NewSurface(SKSurface like, SKImageInfo info) =>
        like.Context is GRRecordingContext context ? SKSurface.Create(context, false, info) : SKSurface.Create(info);

    public void Dispose()
    {
        _sample.Clear(this);
        _down?.Dispose();
        _blurred?.Dispose();
    }
}
