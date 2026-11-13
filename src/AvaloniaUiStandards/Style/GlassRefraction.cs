using Avalonia;
using SkiaSharp;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Builds the refraction part of the glass backdrop filter. The glass is treated as a slab
/// that is flat in the centre and curves down over a rim band of <c>edge</c> px. A Skia
/// runtime shader computes, per pixel, how far and in which direction light is bent there
/// (zero in the flat centre, rising steeply to the rim along the inward surface normal).
/// That map drives Skia's displacement filter, which samples the backdrop at the bent position.
/// </summary>
internal static class GlassRefraction
{
    // Output encodes the displacement as 0.5 + d / 2, with d in [-1, 1] times the strength.
    // The rim profile is a circular sag, 1 - sqrt(1 - u²), where u runs from 0 at the inner
    // edge of the band to 1 at the rim; light is bent inward, magnifying the rim like a lens.
    // The normal comes from a rounded-rectangle distance field whose radius is at least the
    // band width, so the normal field has no crease inside the band.
    private const string Source = """
        uniform float2 size;
        uniform float4 radii;
        uniform float edge;

        half4 main(float2 coord) {
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
            float bend = 1.0 - sqrt(1.0 - u * u);
            float2 d = -n * bend;
            return half4(half2(0.5 + d * 0.5), 0.5, 1.0);
        }
        """;

    private static readonly Lazy<SKRuntimeEffect?> LazyEffect = new(() =>
        SKRuntimeEffect.CreateShader(Source, out _));

    /// <summary>The compiled effect, or null when Skia runtime effects are not available.</summary>
    public static SKRuntimeEffect? Effect => LazyEffect.Value;

    /// <summary>
    /// Creates a filter that refracts <paramref name="input"/> (null: the filter's source, the
    /// backdrop when used as the backdrop filter).
    /// With dispersion, red, green and blue are displaced by (1 − dispersion), 1 and
    /// (1 + dispersion) times the strength and added back together.
    /// </summary>
    public static SKImageFilter? CreateFilter(Rect rect, CornerRadius radius, double strength, double edge, double dispersion, SKImageFilter? input = null)
    {
        var effect = Effect;
        if (effect is null)
            return null;

        using var uniforms = new SKRuntimeEffectUniforms(effect)
        {
            ["size"] = new[] { (float)rect.Width, (float)rect.Height },
            ["radii"] = new[]
            {
                (float)radius.TopLeft, (float)radius.TopRight, (float)radius.BottomRight, (float)radius.BottomLeft,
            },
            ["edge"] = (float)edge,
        };

        using var children = new SKRuntimeEffectChildren(effect);
        using var shader = effect.ToShader(uniforms, children,
            SKMatrix.CreateTranslation((float)rect.X, (float)rect.Y));
        using var map = SKImageFilter.CreateShader(shader, false);

        var scale = (float)(2 * strength);
        if (dispersion <= 0)
            return Displace(map, scale, input);

        var k = (float)dispersion;
        using var red = Channel(Displace(map, scale * (1 - k), input), 0);
        using var green = Channel(Displace(map, scale, input), 1);
        using var blue = Channel(Displace(map, scale * (1 + k), input), 2);
        using var redGreen = SKImageFilter.CreateBlendMode(SKBlendMode.Plus, red, green);
        return SKImageFilter.CreateBlendMode(SKBlendMode.Plus, redGreen, blue);
    }

    // A null input is the filter's source (the backdrop).
    private static SKImageFilter Displace(SKImageFilter map, float scale, SKImageFilter? input) =>
        SKImageFilter.CreateDisplacementMapEffect(SKColorChannel.R, SKColorChannel.G, scale, map, input);

    // Keeps one colour channel and alpha. Summing the three channels is exact over an opaque backdrop.
    private static SKImageFilter Channel(SKImageFilter input, int channel)
    {
        var matrix = new float[20];
        matrix[channel * 5 + channel] = 1;
        matrix[18] = 1;
        using var colorFilter = SKColorFilter.CreateColorMatrix(matrix);
        var filter = SKImageFilter.CreateColorFilter(colorFilter, input);
        input.Dispose();
        return filter;
    }
}
