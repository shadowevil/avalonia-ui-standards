using SkiaSharp;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// The rim drawn with Skia backdrop filters, for where the surface cannot be read as drawn so far
/// (<see cref="GlassRimStrip"/> needs that). Per strip along the edge, a layer's backdrop filter
/// samples the backdrop outward along the normal (a displacement), keeps it to the rim's band,
/// carries bright features along the band (a dilate, then a blur), takes the darkest brightness
/// along it (an erode), and blends that with the depth into the rim. The geometry (normal, depth,
/// band) comes from the squircle's signed distance in a shader, so nothing is rasterised per size.
/// </summary>
internal sealed class GlassRimFilter : IDisposable
{
    // How far past its strip each strip's filter sees the backdrop, in px: the outward sample, the
    // spread and the fade all reach that far.
    private const double Margin = 24;

    // mode 0: the outward displacement to SampleOffset px outside the edge, encoded for Skia's
    // displacement filter as 0.5 + n · offset / maxOffset / 2; 1: depth (red, 0..1 over the depth
    // range) and the light bias (green), premultiplied by the edge's coverage; 2: the band.
    private const string GeometryMain = """
        uniform float mode;
        uniform float sampleOffset;
        uniform float maxOffset;
        uniform float depthRange;
        uniform float bandWidth;
        uniform float pixel;
        uniform float depthBias;

        float lightBias(float2 n) {
            return clamp(0.7 + 0.3 * max(-n.y, 0.0) + 0.6 * n.x * n.x * n.y * n.y, 0.0, 1.0);
        }

        half4 main(float2 coord) {
            float3 s = sdf(coord);
            float d = s.x;
            float2 n = s.yz;
            if (mode < 0.5) {
                float k = clamp((max(d, 0.0) + sampleOffset) / maxOffset, 0.0, 1.0);
                return half4(half2(0.5 + n * k * 0.5), 0.5, 1.0);
            }
            float cover = clamp(d / pixel + 0.5, 0.0, 1.0);
            if (mode < 1.5) {
                float depth = clamp((d - depthBias) / depthRange, 0.0, 1.0);
                return half4(half(depth * cover), half(lightBias(n) * cover), 0.0, half(cover));
            }
            return half4(half(cover * clamp((bandWidth - d) / pixel + 0.5, 0.0, 1.0)));
        }
        """;

    // src: depth (red) and light bias (green); dst: the sampled backdrop, its alpha carrying e.
    private const string BlenderMain = """
        uniform float depthRange;

        half4 main(half4 src, half4 dst) {
            float3 c = dst.a > 0.0 ? float3(dst.rgb) / float(dst.a) : float3(0.0);
            return rim(c, float(dst.a), float(src.r) * depthRange, float(src.g));
        }
        """;

    private static readonly Lazy<SKRuntimeEffect?> LazyGeometry = new(() => SKRuntimeEffect.CreateShader(SquircleDistance.Code + GeometryMain, out _));
    private static readonly Lazy<SKRuntimeEffect?> LazyBlender = new(() => SKRuntimeEffect.CreateBlender(GlassRimDrawOperation.RimCode + BlenderMain, out _));

    internal static bool Available => LazyGeometry.Value is not null && LazyBlender.Value is not null;

    internal static SKRuntimeEffect? Blender => LazyBlender.Value;

    private readonly GlassRimDrawOperation.Parameters _p;
    private readonly double _scale;
    private readonly SKPath _ring;
    private readonly SKRect[] _strips;
    private readonly SKImageFilter[] _filters;

    private GlassRimFilter(GlassRimDrawOperation.Parameters p, double scale, SKRuntimeEffect geometry, SKRuntimeEffect blender)
    {
        _p = p;
        _scale = scale;
        using var outer = SquircleGeometry.CreateSkPath(p.Rect, p.CornerRadius, p.Smoothing);
        var (innerRect, innerRadius) = SquircleGeometry.Inset(p.Rect, p.CornerRadius, p.BandWidth);
        using var inner = SquircleGeometry.CreateSkPath(innerRect, innerRadius, p.Smoothing);
        _ring = outer.Op(inner, SKPathOp.Difference) ?? new SKPath();
        _strips = GlassRimDrawOperation.Strips(p, Margin);
        var shape = GlassRimDrawOperation.ToSkRect(p.Rect);
        _filters = _strips.Select(strip =>
        {
            var region = SKRect.Intersect(SKRect.Inflate(strip, (float)Margin, (float)Margin), shape);
            using var filter = BuildFilter(geometry, blender, region);
            return SKImageFilter.CreateOffset(0, 0, filter, strip);
        }).ToArray();
    }

    /// <summary>The filter rim at this device scale, or null without runtime effects.</summary>
    internal static GlassRimFilter? Create(GlassRimDrawOperation.Parameters p, double scale) =>
        LazyGeometry.Value is { } geometry && LazyBlender.Value is { } blender ? new GlassRimFilter(p, scale, geometry, blender) : null;

    public void Draw(SKCanvas canvas, double opacity)
    {
        canvas.Save();
        canvas.ClipPath(_ring, SKClipOperation.Intersect, true);
        using var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)Math.Round(opacity * 255)) };
        // Each strip's layer is clipped to the strip plus a margin, so its filter sees the backdrop
        // around the strip; the filter keeps only the strip's own rectangle, so neighbouring strips
        // never draw the rim twice.
        foreach (var (strip, filter) in _strips.Zip(_filters))
        {
            var reach = SKRect.Inflate(strip, (float)Margin, (float)Margin);
            canvas.Save();
            canvas.ClipRect(reach);
            var layer = new SKCanvasSaveLayerRec { Bounds = reach, Backdrop = filter, Paint = opacity < 1 ? paint : null };
            canvas.SaveLayer(in layer);
            canvas.Restore();
            canvas.Restore();
        }
        canvas.Restore();
    }

    public void Dispose()
    {
        _ring.Dispose();
        foreach (var filter in _filters)
            filter.Dispose();
    }

    private double MaxOffset => _p.DepthRange + GlassRimDrawOperation.SampleOffset;

    private SKImageFilter Geometry(SKRuntimeEffect effect, int mode, SKRect region)
    {
        using var u = new SKRuntimeEffectUniforms(effect);
        GlassRimDrawOperation.SetShapeUniforms(u, _p);
        u["mode"] = (float)mode;
        u["sampleOffset"] = (float)GlassRimDrawOperation.SampleOffset;
        u["maxOffset"] = (float)MaxOffset;
        u["depthRange"] = (float)_p.DepthRange;
        u["bandWidth"] = (float)_p.BandWidth;
        u["pixel"] = (float)(1 / _scale);
        u["depthBias"] = (float)SquircleDistance.DepthBias(1 / _scale);
        using var children = new SKRuntimeEffectChildren(effect);
        using var shader = effect.ToShader(u, children, SKMatrix.CreateTranslation((float)_p.Rect.X, (float)_p.Rect.Y));
        return SKImageFilter.CreateShader(shader, false, region);
    }

    // The backdrop sampled outward along the normal, the light carried along the band, blended with
    // the depth into the rim.
    private SKImageFilter BuildFilter(SKRuntimeEffect geometry, SKRuntimeEffect blenderEffect, SKRect region)
    {
        using var normal = Geometry(geometry, 0, region);
        // The backdrop exactly SampleOffset px outside the edge (unblurred: a blur there would
        // reach into the glass and mute thin features).
        using var local = SKImageFilter.CreateDisplacementMapEffect(SKColorChannel.R, SKColorChannel.G, (float)(2 * MaxOffset), normal, null);
        using var sampled = Carry(geometry, local, region);
        using var depth = Geometry(geometry, 1, region);
        using var u = new SKRuntimeEffectUniforms(blenderEffect);
        GlassRimDrawOperation.SetRimUniforms(u, _p, 1 / _scale);
        u["depthRange"] = (float)_p.DepthRange;
        using var children = new SKRuntimeEffectChildren(blenderEffect);
        using var blender = blenderEffect.ToBlender(u, children);
        return SKImageFilter.CreateBlendMode(blender, sampled, depth);
    }

    // The local sample kept in the band, its bright features spread along the band (a maximum) and
    // faded (a blur), packed with how much brighter that spread colour is than the darkest local
    // brightness along the band within the spread: (spread colour · e, e), e = 1 - ExcessScale · excess.
    private SKImageFilter Carry(SKRuntimeEffect geometry, SKImageFilter local, SKRect region)
    {
        float[] opaque = [1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1];
        if (_p.Spread <= 0)
        {
            using var opaqueMatrix = SKColorFilter.CreateColorMatrix(opaque);
            return SKImageFilter.CreateColorFilter(opaqueMatrix, local);
        }
        using var band = Geometry(geometry, 2, region);
        using var masked = SKImageFilter.CreateBlendMode(SKBlendMode.DstIn, local, band);
        var spread = (float)_p.Spread;
        var fade = (float)GlassRimDrawOperation.SpreadSigma;
        using var carried = SKImageFilter.CreateDilate(spread, spread, masked);
        using var faded = SKImageFilter.CreateBlur(fade, fade, SKShaderTileMode.Decal, carried);

        // The faded colour over its band coverage, made opaque: the spread colour.
        using var opaqueFilter = SKColorFilter.CreateColorMatrix(opaque);
        using var spreadColour = SKImageFilter.CreateColorFilter(opaqueFilter, faded);

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

        // e = 1 - k (spread - baseline), in the colour channels, opaque; then (1, 1, 1) at alpha e.
        var k = (float)GlassRimDrawOperation.ExcessScale;
        using var e = SKImageFilter.CreateArithmetic(0, -k, k, 1, false, baseline, spreadLuma);
        using var alphaMatrix = SKColorFilter.CreateColorMatrix([0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0]);
        using var eAlpha = SKImageFilter.CreateColorFilter(alphaMatrix, e);
        return SKImageFilter.CreateArithmetic(1, 0, 0, 0, true, eAlpha, spreadColour);
    }
}
