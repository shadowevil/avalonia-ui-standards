using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Draws a glass fill from the shared backdrop sample (<see cref="GlassBackdropSampler"/>): the blurred sample, read
/// through the refraction worked out per pixel (the same slab as <see cref="GlassRefraction"/>, with dispersion), then
/// the tint, clipped to the squircle less the opaque regions of the glass's content. The order is blur, then refract.
/// Where the sample does not cover this glass as it is now (the first frame after it changed, or without a surface),
/// it draws with its own backdrop blur (<see cref="GlassDrawOperation"/>).
/// </summary>
internal sealed class GlassSharedFillOperation : ICustomDrawOperation
{
    private readonly SharedBackdropSample _sample;
    private readonly SampleConsumer _consumer;
    private readonly GlassDrawOperation.Parameters _p;
    private readonly Rect[] _skip;

    public GlassSharedFillOperation(SharedBackdropSample sample, SampleConsumer consumer, GlassDrawOperation.Parameters p, Rect[] skip)
    {
        _sample = sample;
        _consumer = consumer;
        _p = p;
        _skip = skip;
    }

    private const string Source = """
        uniform shader backdrop;
        uniform float2 size;
        uniform float4 radii;
        uniform float edge;
        uniform float strength;
        uniform float dispersion;
        uniform float3 rowX;
        uniform float3 rowY;
        uniform float4 tint;

        float2 at(float2 p) {
            float3 q = float3(p, 1.0);
            return float2(dot(rowX, q), dot(rowY, q));
        }

        half4 main(float2 coord) {
            // The displacement of GlassRefraction: zero in the flat centre, rising steeply to the rim along the
            // inward normal of a rounded rectangle whose radius is at least the band width.
            float2 halfSize = size * 0.5;
            float2 p = coord - halfSize;
            float r = p.x < 0.0 ? (p.y < 0.0 ? radii.x : radii.w) : (p.y < 0.0 ? radii.y : radii.z);
            r = min(max(r, edge), min(halfSize.x, halfSize.y));
            float2 q = abs(p) - halfSize + r;
            float distanceInside = -(min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r);
            float2 n;
            if (q.x > 0.0 && q.y > 0.0) {
                n = normalize(q);
            } else {
                n = q.x > q.y ? float2(1.0, 0.0) : float2(0.0, 1.0);
            }
            n *= float2(p.x < 0.0 ? -1.0 : 1.0, p.y < 0.0 ? -1.0 : 1.0);
            float u = clamp(1.0 - distanceInside / edge, 0.0, 1.0);
            float2 d = -n * (1.0 - sqrt(1.0 - u * u)) * strength;

            half4 c = backdrop.eval(at(coord + d));
            if (dispersion > 0.0) {
                c.r = backdrop.eval(at(coord + d * (1.0 - dispersion))).r;
                c.b = backdrop.eval(at(coord + d * (1.0 + dispersion))).b;
            }
            half4 t = half4(tint);
            return t + (1.0 - t.a) * c;
        }
        """;

    private static readonly Lazy<SKRuntimeEffect?> LazyEffect = new(() => SKRuntimeEffect.CreateShader(Source, out _));
    private static readonly SKSamplingOptions Linear = new(SKFilterMode.Linear, SKMipmapMode.None);

    private SKPath? _path;
    private GlassDrawOperation? _fallback;

    public Rect Bounds => _p.Rect;

    public bool HitTest(Point point) => _p.Rect.Contains(point);

    public bool Equals(ICustomDrawOperation? other) => other is GlassSharedFillOperation o && o._p == _p
        && o._consumer.Equals(_consumer) && ReferenceEquals(o._sample, _sample) && o._skip.AsSpan().SequenceEqual(_skip);


    public override bool Equals(object? obj) => obj is ICustomDrawOperation o && Equals(o);

    public override int GetHashCode() => _p.GetHashCode();

    public void Render(ImmediateDrawingContext context)
    {
        var refract = _p.RefractionStrength > 0 && _p.RefractionEdge > 0;
        if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } feature || !_sample.Covers(_consumer)
            || (refract && LazyEffect.Value is null))
        {
            (_fallback ??= new GlassDrawOperation(_p, _skip)).Render(context);
            return;
        }

        using var lease = feature.Lease();
        var canvas = lease.SkCanvas;
        var image = _sample.Image!;
        var m = canvas.TotalMatrix;
        var k = _sample.Scale;
        // Local (relative to the rectangle) to sample texels: (device - origin) * k.
        var toDevice = m.PreConcat(SKMatrix.CreateTranslation((float)_p.Rect.X, (float)_p.Rect.Y));
        var toTexels = SKMatrix.CreateScale(k, k).PreConcat(SKMatrix.CreateTranslation(-_sample.Origin.X, -_sample.Origin.Y)).PreConcat(toDevice);
        var a = _p.Tint.A / 255.0 * _p.TintOpacity;
        _path ??= SquircleGeometry.CreateSkPath(_p.Rect, _p.CornerRadius, _p.Smoothing);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.White.WithAlpha((byte)Math.Round(Math.Clamp(lease.CurrentOpacity, 0, 1) * 255)),
        };
        using var backdrop = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, Linear);
        if (refract)
        {
            var effect = LazyEffect.Value!;
            using var uniforms = new SKRuntimeEffectUniforms(effect)
            {
                ["size"] = new[] { (float)_p.Rect.Width, (float)_p.Rect.Height },
                ["radii"] = new[] { (float)_p.CornerRadius.TopLeft, (float)_p.CornerRadius.TopRight, (float)_p.CornerRadius.BottomRight, (float)_p.CornerRadius.BottomLeft },
                ["edge"] = (float)_p.RefractionEdge,
                ["strength"] = (float)_p.RefractionStrength,
                ["dispersion"] = (float)_p.ChromaticDispersion,
                ["rowX"] = new[] { toTexels.ScaleX, toTexels.SkewX, toTexels.TransX },
                ["rowY"] = new[] { toTexels.SkewY, toTexels.ScaleY, toTexels.TransY },
                ["tint"] = new[] { (float)(_p.Tint.R / 255.0 * a), (float)(_p.Tint.G / 255.0 * a), (float)(_p.Tint.B / 255.0 * a), (float)a },
            };
            using var children = new SKRuntimeEffectChildren(effect) { ["backdrop"] = backdrop };
            paint.Shader = effect.ToShader(uniforms, children, SKMatrix.CreateTranslation((float)_p.Rect.X, (float)_p.Rect.Y));
        }
        else
        {
            // Minimal glass: the sample itself under the tint, with no runtime effect.
            var texelsToLocal = SKMatrix.CreateScale(k, k).PreConcat(SKMatrix.CreateTranslation(-_sample.Origin.X, -_sample.Origin.Y)).PreConcat(m);
            if (!texelsToLocal.TryInvert(out var inverse))
                return;
            paint.Shader = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, Linear, inverse);
            paint.ColorFilter = SKColorFilter.CreateBlendMode(
                new SKColor(_p.Tint.R, _p.Tint.G, _p.Tint.B, (byte)Math.Round(Math.Clamp(a, 0, 1) * 255)), SKBlendMode.SrcOver);
        }

        canvas.Save();
        foreach (var r in _skip)
            canvas.ClipRect(new SKRect((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom), SKClipOperation.Difference);
        canvas.DrawPath(_path, paint);
        canvas.Restore();
        paint.Shader?.Dispose();
        paint.ColorFilter?.Dispose();
    }

    public void Dispose()
    {
        _path?.Dispose();
        _fallback?.Dispose();
    }
}
