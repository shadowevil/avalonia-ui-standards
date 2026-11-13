using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Draws the glass rim: a hairline on the squircle edge that takes a light tint of the backdrop
/// directly outside each point of the edge. Each rim pixel samples the backdrop a little outward
/// along the edge's normal, lightens it towards white with its hue kept,
/// and shows it at an opacity that follows its luminance, so the rim is faint over dark areas and
/// never uniform. Its width follows the same brightness: a hairline over dim areas, swelling
/// smoothly inward where the backdrop is bright, and bright stretches are amplified a little more
/// so they catch the light. A bright feature touching the edge, even a thin line, carries its light
/// a short way along the rim in both directions, and a soft glow falls inward from the rim. The top
/// edge and the corners catch a little more.
/// It is drawn above the glass's child; outside the shape the backdrop is still unchanged. Only
/// thin strips along the edges are filtered. Without Skia it draws the fixed border.
/// </summary>
internal sealed class GlassRimDrawOperation : ICustomDrawOperation
{
    /// <summary>The rim's width in px over the darkest backdrop, inward from the edge.</summary>
    internal const double MinWidth = 1;

    /// <summary>The rim's width in px over the brightest backdrop.</summary>
    internal const double MaxWidth = 1.75;

    /// <summary>
    /// Bright stretches of the rim are amplified by up to this much (1: up to double), rising smoothly with
    /// the backdrop's luminance from <see cref="BoostFrom"/> to <see cref="BoostTo"/>; dim stretches
    /// are unchanged. The boost compresses towards full opacity and white, so it never clips, and
    /// widens the rim by up to <see cref="BoostWidth"/> (within <see cref="MaxWidth"/>), so it shows
    /// where the rim is already near white.
    /// </summary>
    internal const double Boost = 1;
    internal const double BoostFrom = 0.4;
    internal const double BoostTo = 0.9;
    internal const double BoostWidth = 0.25;

    /// <summary>
    /// Light carried along the edge: a bright feature touching the edge (even a 1 px line) lights
    /// the rim at full strength for this many px either side along the edge, then fades over about
    /// two <see cref="SpreadSigma"/>. It is a maximum over the rim's band only, so the light stays on
    /// the rim, rounds corners, never takes in the glass inside, and leaves broad areas unchanged.
    /// </summary>
    internal const double Spread = 12;
    internal const double SpreadSigma = 4;

    // The carried light's excess over the local sample is passed to the blender in alpha as
    // 1 - ExcessScale · excess (kept well above zero so the colour keeps its precision).
    private const double ExcessScale = 0.75;

    /// <summary>
    /// How strongly carried light catches: its excess over the local brightness counts this many
    /// times (above 1: brighter than the backdrop alone), in brightness, width and the white-mix.
    /// </summary>
    internal const double ExcessGain = 1.7;

    /// <summary>
    /// A soft glow inward from the rim, in px beyond its width: half the rim's strength at the rim,
    /// fading to nothing over this distance.
    /// </summary>
    internal const double GlowWidth = 4;

    /// <summary>How far outward, in px, the rim samples the backdrop.</summary>
    internal const double SampleOffset = 1.5;

    // How far past its strip each strip's filter sees the backdrop, in px: the outward sample, the
    // spread and the fade all reach that far.
    private const double Margin = 24;

    // The depth range the depth picture encodes (red 0..1 = 0..DepthRange px from the edge: the
    // widest rim, its glow and a soft edge), and its steps: concentric fills of the exact
    // squircle, blended smoothly by their antialiasing.
    private double DepthRange => MaxWidth + _p.Glow + 0.75;
    private const double DepthStep = 0.25;

    // The farthest a rim pixel samples, in px: from the band's inner side out past the edge.
    private double MaxOffset => DepthRange + SampleOffset;

    // The outward displacement (mode 0: encoded 0.5 + n · offset / maxOffset / 2, for Skia's
    // displacement filter, so each rim pixel samples sampleOffset px outside the edge whatever its
    // depth), or the depth (red, a uniform per fill) and the light bias (green; mode 1), from a
    // rounded-rectangle distance field close to the squircle.
    private const string ShaderSource = """
        uniform float2 size;
        uniform float4 radii;
        uniform float mode;
        uniform float depth;
        uniform float sampleOffset;
        uniform float maxOffset;

        half4 main(float2 coord) {
            float2 halfSize = size * 0.5;
            float2 p = coord - halfSize;
            float r = p.x < 0.0 ? (p.y < 0.0 ? radii.x : radii.w) : (p.y < 0.0 ? radii.y : radii.z);
            r = min(max(r, 1.0), min(halfSize.x, halfSize.y));
            float2 q = abs(p) - halfSize + r;
            float2 n = (q.x > 0.0 && q.y > 0.0) ? normalize(q) : (q.x > q.y ? float2(1.0, 0.0) : float2(0.0, 1.0));
            n *= float2(p.x < 0.0 ? -1.0 : 1.0, p.y < 0.0 ? -1.0 : 1.0);
            if (mode < 0.5)
            {
                float inside = max(-(min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r), 0.0);
                float k = clamp((inside + sampleOffset) / maxOffset, 0.0, 1.0);
                return half4(half2(0.5 + n * k * 0.5), 0.5, 1.0);
            }
            // Light from above: the top edge full, the sides and bottom less, the corners a little more.
            float light = clamp(0.7 + 0.3 * max(-n.y, 0.0) + 0.6 * n.x * n.x * n.y * n.y, 0.0, 1.0);
            return half4(half(depth), half(light), 0.0, 1.0);
        }
        """;

    // src: the depth picture (red: depth, green: light bias); dst: the sampled backdrop. The width
    // follows the backdrop's luminance; coverage falls off over one device pixel at that width.
    private const string BlenderSource = """
        uniform float absorption;
        uniform float lighten;
        uniform float minWidth;
        uniform float maxWidth;
        uniform float depthRange;
        uniform float pixel;
        uniform float boost;
        uniform float boostFrom;
        uniform float boostTo;
        uniform float boostWidth;
        uniform float spreadWhite;
        uniform float excessScale;
        uniform float excessGain;
        uniform float glow;

        half4 main(half4 src, half4 dst) {
            // dst: the spread colour (premultiplied), its alpha encoding how much brighter it is
            // than the local sample (the light carried in along the edge).
            float3 c = dst.a > 0.0 ? float3(dst.rgb) / float(dst.a) : float3(0.0);
            // The carried light's excess, amplified by excessGain; a clear catch takes the full white-mix.
            float excess = clamp((1.0 - float(dst.a)) / excessScale, 0.0, 1.0) * excessGain;
            float x = smoothstep(0.0, 0.35, excess);
            // Its brightness: the spread colour's, plus the amplified part of the excess.
            float l = min(dot(c, float3(0.2126, 0.7152, 0.0722)) + excess * (1.0 - 1.0 / excessGain), 1.0);
            float bright = boost > 0.0 ? smoothstep(boostFrom, boostTo, l) : 0.0;
            // Bright stretches also widen a little, within the maximum width.
            float width = min(mix(minWidth, maxWidth, l) + boostWidth * bright, maxWidth);
            float d = float(src.r) * depthRange;
            float coverage = clamp((width - d) / pixel + 0.5, 0.0, 1.0);
            // The optional glow: a soft halo inward beyond the hairline, at half strength, fading out.
            if (glow > 0.0)
                coverage = max(coverage, 0.5 * pow(clamp(1.0 - (d - width) / glow, 0.0, 1.0), 2.0));
            float a = clamp(absorption * l * float(src.g) * coverage, 0.0, 1.0);
            // Bright stretches pop: a gain of up to 1 + boost, applied so that it adds about boost
            // where the rim is faint and less towards full opacity, never past it.
            float gain = 1.0 + boost * bright;
            a = 1.0 - pow(1.0 - a, gain);
            // Carried light: towards white by spreadWhite (dark), and lightened less as
            // spreadWhite falls (light), so the feature's own colour shows on light glass.
            float3 rgb = mix(c, float3(1.0), lighten * (1.0 - x * (1.0 - spreadWhite)));
            rgb = mix(rgb, float3(1.0), spreadWhite * x);
            // The colour brightens by the same gain, compressed towards white.
            rgb = 1.0 - (1.0 - rgb) / gain;
            return half4(half3(rgb * a), half(a));
        }
        """;

    private static readonly Lazy<SKRuntimeEffect?> LazyShader = new(() => SKRuntimeEffect.CreateShader(ShaderSource, out _));
    private static readonly Lazy<SKRuntimeEffect?> LazyBlender = new(() => SKRuntimeEffect.CreateBlender(BlenderSource, out _));

    /// <summary>The compiled effects, or null when Skia runtime effects are not available.</summary>
    internal static SKRuntimeEffect? Effect => LazyShader.Value is not null && LazyBlender.Value is not null ? LazyShader.Value : null;

    internal readonly record struct Parameters(Rect Rect, CornerRadius CornerRadius, double Smoothing, double Absorption, double Lighten, double Boost, double Spread, double SpreadWhite, double Glow, Color FallbackBorder);

    private readonly Parameters _p;
    private SKPath? _ring;
    private SKPicture? _depth;
    private SKPicture? _band;
    private SKImageFilter[]? _stripFilters;
    private double _deviceScale;

    public GlassRimDrawOperation(Parameters parameters) => _p = parameters;

    public Rect Bounds => _p.Rect;

    public bool HitTest(Point p) => false;

    public bool Equals(ICustomDrawOperation? other) => other is GlassRimDrawOperation o && o._p == _p;

    public override bool Equals(object? obj) => obj is ICustomDrawOperation o && Equals(o);

    public override int GetHashCode() => _p.GetHashCode();

    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } leaseFeature)
        {
            var r = _p.CornerRadius.TopLeft;
            context.DrawRectangle(null, new ImmutablePen(new ImmutableSolidColorBrush(_p.FallbackBorder), MinWidth), _p.Rect.Deflate(MinWidth / 2), r, r, default);
            return;
        }

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;
        var m = canvas.TotalMatrix;
        var deviceScale = Math.Max(Math.Sqrt(m.ScaleX * m.ScaleX + m.SkewY * m.SkewY), 0.01);
        _ring ??= BuildRing();
        if (_stripFilters is null || deviceScale != _deviceScale)
        {
            DisposeFilters();
            _deviceScale = deviceScale;
            _stripFilters = BuildStripFilters(1 / deviceScale);
        }
        if (_stripFilters is null)
            return;

        canvas.Save();
        canvas.ClipPath(_ring, SKClipOperation.Intersect, true);
        using var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)Math.Round(Math.Clamp(lease.CurrentOpacity, 0, 1) * 255)) };
        // Each strip's layer is clipped to the strip plus a margin, so its filter sees the backdrop
        // around the strip (the spread and blurs reach past it); the filter keeps only the strip's
        // own rectangle, so neighbouring strips never draw the rim twice.
        var strips = Strips().ToArray();
        for (var i = 0; i < strips.Length; i++)
        {
            var reach = SKRect.Inflate(strips[i], (float)Margin, (float)Margin);
            canvas.Save();
            canvas.ClipRect(reach);
            var layer = new SKCanvasSaveLayerRec { Bounds = reach, Backdrop = _stripFilters[i], Paint = lease.CurrentOpacity < 1 ? paint : null };
            canvas.SaveLayer(in layer);
            canvas.Restore();
            canvas.Restore();
        }
        canvas.Restore();
    }

    public void Dispose()
    {
        _ring?.Dispose();
        _depth?.Dispose();
        _band?.Dispose();
        DisposeFilters();
    }

    private void DisposeFilters()
    {
        foreach (var filter in _stripFilters ?? [])
            filter.Dispose();
        _stripFilters = null;
    }

    // The rim's filter per strip: the shared filter, kept to that strip's rectangle.
    private SKImageFilter[]? BuildStripFilters(double pixel)
    {
        using var shared = BuildFilter(pixel);
        if (shared is null)
            return null;
        return Strips().Select(strip =>
        {
            using var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(strip);
            using (var paint = new SKPaint { Color = SKColors.White })
                canvas.DrawRect(strip, paint);
            using var picture = recorder.EndRecording();
            using var keep = SKImageFilter.CreatePicture(picture, strip);
            return SKImageFilter.CreateBlendMode(SKBlendMode.DstIn, shared, keep);
        }).ToArray();
    }

    // The band the rim can cover: the shape minus the shape inset by the widest rim and its soft edge.
    private SKPath BuildRing()
    {
        using var outer = SquircleGeometry.CreateSkPath(_p.Rect, _p.CornerRadius, _p.Smoothing);
        var (innerRect, innerRadius) = SquircleGeometry.Inset(_p.Rect, _p.CornerRadius, MaxWidth + _p.Glow + 1);
        using var inner = SquircleGeometry.CreateSkPath(innerRect, innerRadius, _p.Smoothing);
        return outer.Op(inner, SKPathOp.Difference) ?? new SKPath();
    }

    // The backdrop sampled outward along the normal; kept only in the rim's
    // band, so light then carried along it (a maximum, then a fade) stays on the rim and never takes
    // in the glass inside; blended with the depth picture into the rim (width, opacity and
    // lightening from its luminance, times the light bias). The blender's colour is the band's
    // premultiplied colour over its alpha, so the fade averages along the rim: broad areas keep
    // their colour, a thin bright feature lights a short stretch that fades at its ends.
    private SKImageFilter? BuildFilter(double pixel)
    {
        if (LazyShader.Value is not { } shaderEffect || LazyBlender.Value is not { } blenderEffect)
            return null;

        using var normal = SKImageFilter.CreateShader(Shader(shaderEffect, 0, 0), false);
        // The backdrop exactly SampleOffset px outside the edge (unblurred: a blur there would
        // reach into the glass and mute thin features).
        var local = SKImageFilter.CreateDisplacementMapEffect(SKColorChannel.R, SKColorChannel.G, (float)(2 * MaxOffset), normal, null);
        using var sampled = _p.Spread > 0 ? Carry(local) : local;
        if (!ReferenceEquals(sampled, local))
            local.Dispose();

        _depth ??= BuildDepth(shaderEffect);
        using var depth = SKImageFilter.CreatePicture(_depth, ToSkRect(_p.Rect));

        using var uniforms = new SKRuntimeEffectUniforms(blenderEffect)
        {
            ["absorption"] = (float)_p.Absorption,
            ["lighten"] = (float)_p.Lighten,
            ["minWidth"] = (float)MinWidth,
            ["maxWidth"] = (float)MaxWidth,
            ["depthRange"] = (float)DepthRange,
            ["pixel"] = (float)pixel,
            ["boost"] = (float)_p.Boost,
            ["boostFrom"] = (float)BoostFrom,
            ["boostTo"] = (float)BoostTo,
            ["boostWidth"] = (float)BoostWidth,
            ["spreadWhite"] = (float)_p.SpreadWhite,
            ["excessScale"] = (float)ExcessScale,
            ["excessGain"] = (float)ExcessGain,
            ["glow"] = (float)_p.Glow,
        };
        using var children = new SKRuntimeEffectChildren(blenderEffect);
        using var blender = blenderEffect.ToBlender(uniforms, children);
        return SKImageFilter.CreateBlendMode(blender, sampled, depth);
    }

    // The local sample kept in the rim's band, its bright features spread along the band (a
    // maximum) and faded (a blur, normalised by the band), packed with how much brighter that
    // spread colour is than its surroundings: (spread colour · e, e), e = 1 - ExcessScale · excess.
    private SKImageFilter Carry(SKImageFilter local)
    {
        _band ??= BuildBand();
        using var band = SKImageFilter.CreatePicture(_band, ToSkRect(_p.Rect));
        using var masked = SKImageFilter.CreateBlendMode(SKBlendMode.DstIn, local, band);
        var spread = (float)_p.Spread;
        var fade = (float)SpreadSigma;
        using var carried = SKImageFilter.CreateDilate(spread, spread, masked);
        using var faded = SKImageFilter.CreateBlur(fade, fade, SKShaderTileMode.Decal, carried);

        // The faded colour over its band coverage, made opaque: the spread colour.
        using var opaqueMatrix = SKColorFilter.CreateColorMatrix([1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1]);
        using var spreadColour = SKImageFilter.CreateColorFilter(opaqueMatrix, faded);

        // Luminance in the colour channels, opaque, for the spread colour and the local sample.
        float[] luma = [0.2126f, 0.7152f, 0.0722f, 0, 0, 0.2126f, 0.7152f, 0.0722f, 0, 0, 0.2126f, 0.7152f, 0.0722f, 0, 0, 0, 0, 0, 0, 1];
        using var lumaMatrix = SKColorFilter.CreateColorMatrix(luma);
        using var spreadLuma = SKImageFilter.CreateColorFilter(lumaMatrix, spreadColour);
        using var localLuma = SKImageFilter.CreateColorFilter(lumaMatrix, local);

        // The baseline: the darkest local brightness along the band within the spread (outside the
        // band counts as white, so it is ignored). A thin bright line drops out of it, so the
        // crossing itself catches as strongly as its neighbours; uniform areas keep their own.
        using var outsideWhite = SKImageFilter.CreateArithmetic(1, 0, -1, 1, false, band, localLuma);
        using var baseline = SKImageFilter.CreateErode(spread, spread, outsideWhite);

        // e = 1 - k (spread - baseline), clamped to at most 1, in the colour channels, opaque.
        var k = (float)ExcessScale;
        using var e = SKImageFilter.CreateArithmetic(0, -k, k, 1, false, baseline, spreadLuma);
        // e as (1, 1, 1) at alpha e, premultiplied (e, e, e, e).
        using var alphaMatrix = SKColorFilter.CreateColorMatrix([0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0]);
        using var eAlpha = SKImageFilter.CreateColorFilter(alphaMatrix, e);
        return SKImageFilter.CreateArithmetic(1, 0, 0, 0, true, eAlpha, spreadColour);
    }

    // The rim's band, opaque.
    private SKPicture BuildBand()
    {
        using var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(ToSkRect(_p.Rect));
        _ring ??= BuildRing();
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        canvas.DrawPath(_ring, paint);
        return recorder.EndRecording();
    }

    // Concentric fills of the shape inset in DepthStep steps, each carrying its depth; the
    // antialiased edges blend neighbouring depths smoothly.
    private SKPicture BuildDepth(SKRuntimeEffect effect)
    {
        using var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(ToSkRect(_p.Rect));
        for (var inset = 0.0; inset <= DepthRange + 1e-6; inset += DepthStep)
        {
            var (rect, radius) = SquircleGeometry.Inset(_p.Rect, _p.CornerRadius, inset);
            if (rect.Width <= 0 || rect.Height <= 0)
                break;
            using var path = SquircleGeometry.CreateSkPath(rect, radius, _p.Smoothing);
            using var shader = Shader(effect, 1, inset / DepthRange);
            using var paint = new SKPaint { Shader = shader, IsAntialias = true };
            canvas.DrawPath(path, paint);
        }
        return recorder.EndRecording();
    }

    private SKShader Shader(SKRuntimeEffect effect, float mode, double depth)
    {
        using var uniforms = new SKRuntimeEffectUniforms(effect)
        {
            ["size"] = new[] { (float)_p.Rect.Width, (float)_p.Rect.Height },
            ["radii"] = new[] { (float)_p.CornerRadius.TopLeft, (float)_p.CornerRadius.TopRight, (float)_p.CornerRadius.BottomRight, (float)_p.CornerRadius.BottomLeft },
            ["mode"] = mode,
            ["depth"] = (float)depth,
            ["sampleOffset"] = (float)SampleOffset,
            ["maxOffset"] = (float)MaxOffset,
        };
        using var children = new SKRuntimeEffectChildren(effect);
        return effect.ToShader(uniforms, children, SKMatrix.CreateTranslation((float)_p.Rect.X, (float)_p.Rect.Y));
    }

    // The top and bottom strips hold the corners; the side strips are just wider than the rim.
    private IEnumerable<SKRect> Strips()
    {
        var r = _p.Rect;
        var c = _p.CornerRadius;
        var top = Math.Max(c.TopLeft, c.TopRight) + 3 + _p.Glow;
        var bottom = Math.Max(c.BottomLeft, c.BottomRight) + 3 + _p.Glow;
        var side = (float)(MaxWidth + 2 + _p.Glow);
        if (top + bottom >= r.Height)
        {
            yield return ToSkRect(r);
            yield break;
        }
        yield return new SKRect((float)r.X, (float)r.Y, (float)r.Right, (float)(r.Y + top));
        yield return new SKRect((float)r.X, (float)(r.Bottom - bottom), (float)r.Right, (float)r.Bottom);
        yield return new SKRect((float)r.X, (float)(r.Y + top), (float)r.X + side, (float)(r.Bottom - bottom));
        yield return new SKRect((float)r.Right - side, (float)(r.Y + top), (float)r.Right, (float)(r.Bottom - bottom));
    }

    private static SKRect ToSkRect(Rect r) => new((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom);
}
