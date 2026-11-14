using System.Collections.Concurrent;
using SkiaSharp;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// The rim with the edge unwrapped into a strip, one texel per device px along the perimeter:
/// <list type="number">
/// <item>The backdrop SampleOffset px outside the edge, read from four thin copies of the surface (one per side).</item>
/// <item>Along the strip, the brightest colour and the darkest luminance within the spread.</item>
/// <item>The brightest colour faded along the strip, packed with how much brighter it is than the
/// darkest: (spread · e, e), e = 1 - ExcessScale · excess.</item>
/// <item>Over the band, per pixel: the squircle's exact distance and normal, the strip at the pixel's
/// place along the edge, and the rim colour.</item>
/// </list>
/// The strip follows the rounded-rectangle perimeter (each corner's radius) from the top-left
/// corner's arc, clockwise; strips longer than a row wrap onto further rows. It needs the surface
/// as drawn so far (<c>SkSurface</c>), so it is not used inside effect or opacity-mask layers.
/// When the clip (the region Avalonia redraws) holds no band pixel, nothing is drawn; when it holds
/// no point the strip samples, the strip from the last frame is reused.
/// </summary>
internal sealed class GlassRimStrip : IDisposable
{
    // The perimeter: radii (top-left, top-right, bottom-right, bottom-left, each clamped to 1 and
    // half the shorter side), the offsets where each part starts (offA: top edge, top-right arc,
    // right edge, bottom-right arc; offB: bottom edge, bottom-left arc, left edge, the whole
    // length), and the strip's layout (grid: row length, texels).
    private const string PerimeterCode = """
        uniform float4 radii;
        uniform float4 offA;
        uniform float4 offB;
        uniform float2 grid;

        // The point on the edge at arc length s (wrapped) and its outward normal: (x, y, nx, ny).
        float4 edgeAt(float s, float2 sz) {
            s = s - offB.w * floor(s / offB.w);
            float a;
            if (s < offA.x) { a = 3.1415927 + s / radii.x; return float4(radii.x + radii.x * cos(a), radii.x + radii.x * sin(a), cos(a), sin(a)); }
            if (s < offA.y) return float4(radii.x + s - offA.x, 0.0, 0.0, -1.0);
            if (s < offA.z) { a = -1.5707963 + (s - offA.y) / radii.y; return float4(sz.x - radii.y + radii.y * cos(a), radii.y + radii.y * sin(a), cos(a), sin(a)); }
            if (s < offA.w) return float4(sz.x, radii.y + s - offA.z, 1.0, 0.0);
            if (s < offB.x) { a = (s - offA.w) / radii.z; return float4(sz.x - radii.z + radii.z * cos(a), sz.y - radii.z + radii.z * sin(a), cos(a), sin(a)); }
            if (s < offB.y) return float4(sz.x - radii.z - (s - offB.x), sz.y, 0.0, 1.0);
            if (s < offB.z) { a = 1.5707963 + (s - offB.y) / radii.w; return float4(radii.w + radii.w * cos(a), sz.y - radii.w + radii.w * sin(a), cos(a), sin(a)); }
            return float4(0.0, sz.y - radii.w - (s - offB.z), -1.0, 0.0);
        }

        // The texel centre of strip index i (wrapped around the perimeter).
        float2 texel(float i) {
            i = i - grid.y * floor(i / grid.y);
            float row = floor(i / grid.x);
            return float2(i - row * grid.x + 0.5, row + 0.5);
        }

        float indexAt(float2 coord) {
            return floor(coord.y) * grid.x + floor(coord.x);
        }
        """;

    // 1: the backdrop outside the edge, from the side's copy: top (with its arcs), right, bottom
    // (with its arcs), left.
    private const string SampleMain = """
        uniform float2 shapeSize;
        uniform shader top;
        uniform shader right;
        uniform shader bottom;
        uniform shader left;
        uniform float texelLength;
        uniform float sampleOffset;

        half4 main(float2 coord) {
            float i = indexAt(coord);
            if (i >= grid.y) return half4(0.0);
            float s = (i + 0.5) * texelLength;
            float4 e = edgeAt(s, shapeSize);
            float2 p = e.xy + e.zw * sampleOffset;
            half3 c = s < offA.z ? top.eval(p).rgb : s < offA.w ? right.eval(p).rgb : s < offB.z ? bottom.eval(p).rgb : left.eval(p).rgb;
            return half4(c, 1.0);
        }
        """;

    // 2: rows [0, rows): the brightest colour over ±w texels; rows [rows, 2 rows): the darkest luminance.
    private static string ExtremesMain(int w) => $$"""
        uniform shader samples;
        uniform float rows;

        half4 main(float2 coord) {
            float row = floor(coord.y);
            bool darkest = row >= rows;
            float i = (darkest ? row - rows : row) * grid.x + floor(coord.x);
            if (i >= grid.y) return half4(0.0);
            half3 brightest = half3(0.0);
            half dark = 1.0;
            for (int k = -{{w}}; k <= {{w}}; k++) {
                half3 c = samples.eval(texel(i + float(k))).rgb;
                brightest = max(brightest, c);
                dark = min(dark, dot(c, half3(0.2126, 0.7152, 0.0722)));
            }
            return darkest ? half4(half3(dark), 1.0) : half4(brightest, 1.0);
        }
        """;

    // 3: the brightest colour faded (a Gaussian, sigma in texels, over ±w), packed with e.
    private static string PackMain(int w, double sigma) => $$"""
        uniform shader extremes;
        uniform float rows;
        uniform float excessScale;

        half4 main(float2 coord) {
            float i = indexAt(coord);
            if (i >= grid.y) return half4(0.0);
            float3 spread = float3(0.0);
            float total = 0.0;
            for (int k = -{{w}}; k <= {{w}}; k++) {
                float x = float(k) / {{Math.Max(sigma, 0.01).ToString("R", System.Globalization.CultureInfo.InvariantCulture)}};
                float weight = exp(-0.5 * x * x);
                spread += float3(extremes.eval(texel(i + float(k))).rgb) * weight;
                total += weight;
            }
            float3 c = spread / total;
            float darkest = float(extremes.eval(texel(i) + float2(0.0, rows)).r);
            float e = clamp(1.0 - excessScale * (dot(c, float3(0.2126, 0.7152, 0.0722)) - darkest), 0.0, 1.0);
            return half4(half3(c * e), half(e));
        }
        """;

    // 4: the band.
    private const string BandMain = """
        uniform shader strip;
        uniform float bandWidth;
        uniform float depthRange;
        uniform float depthBias;

        // The arc length of the rounded rectangle's point nearest to coord (radial in the corners).
        float arcAt(float2 coord) {
            float2 hs = size * 0.5;
            float2 p = coord - hs;
            bool left = p.x < 0.0;
            bool top = p.y < 0.0;
            float r = left ? (top ? radii.x : radii.w) : (top ? radii.y : radii.z);
            float2 q = abs(p) - hs + r;
            if (q.x > 0.0 && q.y > 0.0) {
                if (top && left) return (atan(coord.y - r, coord.x - r) + 3.1415927) * r;
                if (top) return offA.y + (atan(coord.y - r, coord.x - (size.x - r)) + 1.5707963) * r;
                if (!left) return offA.w + atan(coord.y - (size.y - r), coord.x - (size.x - r)) * r;
                return offB.y + (atan(coord.y - (size.y - r), coord.x - r) - 1.5707963) * r;
            }
            if (q.x > q.y)
                return left ? offB.z + clamp(size.y - radii.w - coord.y, 0.0, offB.w - offB.z)
                            : offA.z + clamp(coord.y - radii.y, 0.0, offA.w - offA.z);
            return top ? offA.x + clamp(coord.x - radii.x, 0.0, offA.y - offA.x)
                       : offB.x + clamp(size.x - radii.z - coord.x, 0.0, offB.y - offB.x);
        }

        half4 main(float2 coord) {
            float3 s = sdf(coord);
            float d = s.x;
            if (d < -pixel || d > bandWidth) return half4(0.0);
            float cover = clamp(d / pixel + 0.5, 0.0, 1.0);
            // The strip, linearly between its two nearest texels.
            float t = arcAt(coord) / offB.w * grid.y - 0.5;
            float t0 = floor(t);
            half4 v = mix(strip.eval(texel(t0)), strip.eval(texel(t0 + 1.0)), half(t - t0));
            float3 c = v.a > 0.0 ? float3(v.rgb) / float(v.a) : float3(0.0);
            // The edge's coverage counts twice, as the band's own antialiasing and as the edge.
            return rim(c, float(v.a), clamp(d - depthBias, 0.0, depthRange), lightBias(s.yz) * cover) * half(cover);
        }
        """;

    private static readonly Lazy<SKRuntimeEffect?> LazySample = new(() => SKRuntimeEffect.CreateShader(PerimeterCode + SampleMain, out _));
    private static readonly Lazy<SKRuntimeEffect?> LazyBand = new(() =>
        SKRuntimeEffect.CreateShader(SquircleDistance.Code + PerimeterCode + GlassRimDrawOperation.RimCode + BandMain, out _));
    private static readonly ConcurrentDictionary<(int W, double Sigma), (SKRuntimeEffect? Extremes, SKRuntimeEffect? Pack)> Passes = new();

    internal static bool Available => LazySample.Value is not null && LazyBand.Value is not null;

    private readonly GlassRimDrawOperation.Parameters _p;
    private readonly double _scale;
    private readonly float[] _radii;
    private readonly float[] _offsets;
    private readonly int _texels, _row, _rows;
    private readonly SKRect[] _band;
    private readonly SKRuntimeEffect _sample, _extremes, _pack, _bandEffect;
    private readonly SKRuntimeEffectUniforms _uSample, _uExtremes, _uPack, _uBand;
    private SKSurface? _samples, _extremesSurface, _strip;
    private bool _valid;

    private GlassRimStrip(GlassRimDrawOperation.Parameters p, double scale, SKRuntimeEffect sample, SKRuntimeEffect extremes, SKRuntimeEffect pack, SKRuntimeEffect band)
    {
        _p = p;
        _scale = scale;
        _sample = sample;
        _extremes = extremes;
        _pack = pack;
        _bandEffect = band;
        double w = p.Rect.Width, h = p.Rect.Height;
        var limit = Math.Min(w, h) / 2;
        double R(double r) => Math.Min(Math.Max(r, 1), limit);
        double[] r = [R(p.CornerRadius.TopLeft), R(p.CornerRadius.TopRight), R(p.CornerRadius.BottomRight), R(p.CornerRadius.BottomLeft)];
        _radii = r.Select(x => (float)x).ToArray();
        double Arc(double x) => Math.PI * x / 2;
        var o1 = Arc(r[0]);
        var o2 = o1 + Math.Max(0, w - r[0] - r[1]);
        var o3 = o2 + Arc(r[1]);
        var o4 = o3 + Math.Max(0, h - r[1] - r[2]);
        var o5 = o4 + Arc(r[2]);
        var o6 = o5 + Math.Max(0, w - r[2] - r[3]);
        var o7 = o6 + Arc(r[3]);
        var perimeter = o7 + Math.Max(0, h - r[3] - r[0]);
        _offsets = [(float)o1, (float)o2, (float)o3, (float)o4, (float)o5, (float)o6, (float)o7, (float)perimeter];
        _texels = Math.Max(1, (int)Math.Ceiling(perimeter * scale));
        _row = Math.Max(1, Math.Min(_texels, p.StripRow));
        _rows = (_texels + _row - 1) / _row;
        _band = GlassRimDrawOperation.Strips(p, 0);

        float[] grid = [_row, _texels];
        _uSample = new SKRuntimeEffectUniforms(sample);
        SetPerimeter(_uSample, grid);
        _uSample["shapeSize"] = new[] { (float)w, (float)h };
        _uSample["texelLength"] = (float)(perimeter / _texels);
        _uSample["sampleOffset"] = (float)GlassRimDrawOperation.SampleOffset;
        _uExtremes = new SKRuntimeEffectUniforms(extremes);
        SetPerimeter(_uExtremes, grid);
        _uExtremes["rows"] = (float)_rows;
        _uPack = new SKRuntimeEffectUniforms(pack);
        SetPerimeter(_uPack, grid);
        _uPack["rows"] = (float)_rows;
        _uPack["excessScale"] = (float)GlassRimDrawOperation.ExcessScale;
        _uBand = new SKRuntimeEffectUniforms(band);
        SetPerimeter(_uBand, grid);
        GlassRimDrawOperation.SetShapeUniforms(_uBand, p);
        GlassRimDrawOperation.SetRimUniforms(_uBand, p, 1 / scale);
        _uBand["bandWidth"] = (float)p.BandWidth;
        _uBand["depthRange"] = (float)p.DepthRange;
        _uBand["depthBias"] = (float)SquircleDistance.DepthBias(1 / scale);
    }

    private void SetPerimeter(SKRuntimeEffectUniforms u, float[] grid)
    {
        u["radii"] = _radii;
        u["offA"] = _offsets[..4];
        u["offB"] = _offsets[4..];
        u["grid"] = grid;
    }

    /// <summary>The strip rim at this device scale, or null without runtime effects.</summary>
    internal static GlassRimStrip? Create(GlassRimDrawOperation.Parameters p, double scale)
    {
        if (LazySample.Value is not { } sample || LazyBand.Value is not { } band || p.Rect.Width <= 0 || p.Rect.Height <= 0)
            return null;
        // Taps one texel (about a device px) apart: the spread reaches ±Spread px, the fade's sigma in texels.
        var w = (int)Math.Ceiling(Math.Max(p.Spread, 0) * scale);
        var sigma = Math.Round(GlassRimDrawOperation.SpreadSigma * scale, 3);
        var (extremes, pack) = Passes.GetOrAdd((w, sigma), key => (
            SKRuntimeEffect.CreateShader(PerimeterCode + ExtremesMain(key.W), out _),
            SKRuntimeEffect.CreateShader(PerimeterCode + PackMain(key.W, key.Sigma), out _)));
        return extremes is null || pack is null ? null : new GlassRimStrip(p, scale, sample, extremes, pack, band);
    }

    /// <summary>Draws the rim; false when the surface could not be read (the caller falls back).</summary>
    public bool Draw(SKCanvas canvas, SKSurface surface, GRContext? gr, double opacity)
    {
        var m = canvas.TotalMatrix;
        if (!m.TryInvert(out var inverse))
            return true;
        var rect = _p.Rect;
        var clip = canvas.LocalClipBounds;
        if (!_band.Any(b => b.IntersectsWith(clip)))
            return true;
        var maxRadius = _radii.Max();
        var outer = SKRect.Inflate(GlassRimDrawOperation.ToSkRect(rect), 3, 3);
        var inner = SKRect.Inflate(GlassRimDrawOperation.ToSkRect(rect), -(maxRadius + 3), -(maxRadius + 3));
        var samplesInClip = outer.IntersectsWith(clip) && !inner.Contains(clip);
        if (!_valid || samplesInClip)
        {
            if (!BuildStrip(surface, gr, m, inverse))
                return false;
            _valid = true;
        }

        using var stripImage = _strip!.Snapshot();
        using var stripShader = stripImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, Nearest);
        using var children = new SKRuntimeEffectChildren(_bandEffect) { ["strip"] = stripShader };
        using var shader = _bandEffect.ToShader(_uBand, children, SKMatrix.CreateTranslation((float)rect.X, (float)rect.Y));
        using var paint = new SKPaint { Shader = shader, Color = SKColors.White.WithAlpha((byte)Math.Round(opacity * 255)) };
        foreach (var band in _band)
            canvas.DrawRect(band, paint);
        return true;
    }

    private static readonly SKSamplingOptions Nearest = new(SKFilterMode.Nearest);

    private bool BuildStrip(SKSurface surface, GRContext? gr, SKMatrix m, SKMatrix inverse)
    {
        float w = (float)_p.Rect.Width, h = (float)_p.Rect.Height;
        float r0 = _radii[0], r1 = _radii[1], r2 = _radii[2], r3 = _radii[3];
        // Thin copies along the sides, covering the points 1.5 px outside the edge.
        SKRect[] sides =
        [
            new(-3, -3, w + 3, Math.Max(r0, r1) + 1),
            new(w - 1, r1 - 1, w + 3, h - r2 + 1),
            new(-3, h - Math.Max(r2, r3) - 1, w + 3, h + 3),
            new(-3, r0 - 1, 1, h - r3 + 1),
        ];
        string[] names = ["top", "right", "bottom", "left"];
        var owned = new List<IDisposable>();
        try
        {
            using var children = new SKRuntimeEffectChildren(_sample);
            for (var i = 0; i < 4; i++)
            {
                var side = sides[i];
                side.Offset((float)_p.Rect.X, (float)_p.Rect.Y);
                var device = SKRectI.Ceiling(m.MapRect(side), true);
                // Skia clips a copy to the surface; its origin moves only where the left or top is cut.
                device = new SKRectI(Math.Max(device.Left, 0), Math.Max(device.Top, 0), device.Right, device.Bottom);
                var copy = device.Width > 0 && device.Height > 0 ? surface.Snapshot(device) : null;
                SKShader shader;
                if (copy is null)
                    shader = SKShader.CreateColor(SKColors.Black);
                else
                {
                    owned.Add(copy);
                    var local = inverse.PreConcat(SKMatrix.CreateTranslation(device.Left, device.Top))
                        .PostConcat(SKMatrix.CreateTranslation(-(float)_p.Rect.X, -(float)_p.Rect.Y));
                    shader = copy.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, Nearest, local);
                }
                owned.Add(shader);
                children[names[i]] = shader;
            }

            _samples ??= NewSurface(surface, gr, _rows);
            _extremesSurface ??= NewSurface(surface, gr, 2 * _rows);
            _strip ??= NewSurface(surface, gr, _rows);
            if (_samples is null || _extremesSurface is null || _strip is null)
                return false;

            Pass(_samples, _sample, _uSample, children, _rows);
            using (var samples = _samples.Snapshot())
            using (var samplesShader = samples.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, Nearest))
            using (var c2 = new SKRuntimeEffectChildren(_extremes) { ["samples"] = samplesShader })
                Pass(_extremesSurface, _extremes, _uExtremes, c2, 2 * _rows);
            using (var extremes = _extremesSurface.Snapshot())
            using (var extremesShader = extremes.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, Nearest))
            using (var c3 = new SKRuntimeEffectChildren(_pack) { ["extremes"] = extremesShader })
                Pass(_strip, _pack, _uPack, c3, _rows);
            return true;
        }
        finally
        {
            foreach (var d in owned)
                d.Dispose();
        }
    }

    private void Pass(SKSurface target, SKRuntimeEffect effect, SKRuntimeEffectUniforms u, SKRuntimeEffectChildren children, int height)
    {
        using var shader = effect.ToShader(u, children);
        using var paint = new SKPaint { Shader = shader, BlendMode = SKBlendMode.Src };
        target.Canvas.DrawRect(0, 0, _row, height, paint);
    }

    private SKSurface? NewSurface(SKSurface like, GRContext? gr, int height)
    {
        var info = new SKImageInfo(_row, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        return like.Context is GRRecordingContext context ? SKSurface.Create(context, true, info) : SKSurface.Create(info);
    }

    public void Dispose()
    {
        _uSample.Dispose();
        _uExtremes.Dispose();
        _uPack.Dispose();
        _uBand.Dispose();
        _samples?.Dispose();
        _extremesSurface?.Dispose();
        _strip?.Dispose();
    }
}
