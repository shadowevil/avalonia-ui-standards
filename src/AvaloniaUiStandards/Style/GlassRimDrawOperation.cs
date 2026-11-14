using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace AvaloniaUiStandards.Style;

/// <summary>How the rim is drawn.</summary>
internal enum RimMethod
{
    /// <summary>The edge strip where the surface can be read, else the filter rim.</summary>
    Auto,

    /// <summary>The edge strip (<see cref="GlassRimStrip"/>) only.</summary>
    Strip,

    /// <summary>The backdrop-filter rim (<see cref="GlassRimFilter"/>) only.</summary>
    Filter,
}

/// <summary>
/// Draws the glass rim: a hairline on the squircle edge that takes a light tint of the backdrop
/// directly outside each point of the edge. Each rim pixel takes the backdrop a little outward
/// along the edge's normal, lightens it towards white with its hue kept, and shows it at an
/// opacity that follows its luminance, so the rim is faint over dark areas and never uniform. Its
/// width follows the same brightness: a hairline over dim areas, swelling smoothly inward where
/// the backdrop is bright, and bright stretches are amplified a little more so they catch the
/// light. A bright feature touching the edge, even a thin line, carries its light a short way
/// along the rim in both directions, and a soft glow falls inward from the rim. The top edge and
/// the corners catch a little more.
/// It is drawn above the glass's child; outside the shape the backdrop is unchanged. By default it
/// reads the backdrop along the edge into a strip and works along it (<see cref="GlassRimStrip"/>);
/// where the surface cannot be read as drawn so far (inside an effect or opacity-mask layer, or
/// without a surface) it uses Skia backdrop filters (<see cref="GlassRimFilter"/>). Without Skia
/// it draws the fixed border.
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
    /// two <see cref="SpreadSigma"/>. It stays on the rim, rounds corners and leaves broad areas unchanged.
    /// </summary>
    internal const double Spread = 12;
    internal const double SpreadSigma = 4;

    /// <summary>
    /// The carried light's excess over the darkest backdrop along the edge within the spread is
    /// passed on as 1 - ExcessScale · excess (kept well above zero so the colour keeps its precision).
    /// </summary>
    internal const double ExcessScale = 0.75;

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

    /// <summary>The widest strip row, in texels; longer edges wrap onto further rows.</summary>
    internal const int MaxStripRow = 16384;

    // The rim's colour from the spread colour c, e = 1 - ExcessScale · excess, the depth d in px
    // and the light bias (times the edge's coverage): premultiplied. Shared by both methods.
    internal const string RimCode = """
        uniform float absorption;
        uniform float lighten;
        uniform float minWidth;
        uniform float maxWidth;
        uniform float pixel;
        uniform float boost;
        uniform float boostFrom;
        uniform float boostTo;
        uniform float boostWidth;
        uniform float spreadWhite;
        uniform float excessScale;
        uniform float excessGain;
        uniform float glow;

        half4 rim(float3 c, float e, float d, float light) {
            // The carried light's excess, amplified by excessGain; a clear catch takes the full white-mix.
            float excess = clamp((1.0 - e) / excessScale, 0.0, 1.0) * excessGain;
            float x = smoothstep(0.0, 0.35, excess);
            // Its brightness: the spread colour's, plus the amplified part of the excess.
            float l = min(dot(c, float3(0.2126, 0.7152, 0.0722)) + excess * (1.0 - 1.0 / excessGain), 1.0);
            float bright = boost > 0.0 ? smoothstep(boostFrom, boostTo, l) : 0.0;
            // Bright stretches also widen a little, within the maximum width.
            float width = min(mix(minWidth, maxWidth, l) + boostWidth * bright, maxWidth);
            float coverage = clamp((width - d) / pixel + 0.5, 0.0, 1.0);
            // The glow: a soft halo inward beyond the hairline, at half strength, fading out.
            if (glow > 0.0)
                coverage = max(coverage, 0.5 * pow(clamp(1.0 - (d - width) / glow, 0.0, 1.0), 2.0));
            float a = clamp(absorption * l * light * coverage, 0.0, 1.0);
            // Bright stretches pop: a gain of up to 1 + boost, adding about boost where the rim is
            // faint and less towards full opacity, never past it.
            float gain = 1.0 + boost * bright;
            a = 1.0 - pow(1.0 - a, gain);
            // Carried light: towards white by spreadWhite, and lightened less as spreadWhite falls,
            // so the feature's own colour shows on light glass.
            float3 rgb = mix(c, float3(1.0), lighten * (1.0 - x * (1.0 - spreadWhite)));
            rgb = mix(rgb, float3(1.0), spreadWhite * x);
            // The colour brightens by the same gain, compressed towards white.
            rgb = 1.0 - (1.0 - rgb) / gain;
            return half4(half3(rgb * a), half(a));
        }

        // Light from above: the top edge full, the sides and bottom less, the corners a little more.
        float lightBias(float2 n) {
            return clamp(0.7 + 0.3 * max(-n.y, 0.0) + 0.6 * n.x * n.x * n.y * n.y, 0.0, 1.0);
        }
        """;

    /// <summary>The compiled effects, or null when Skia runtime effects are not available.</summary>
    internal static SKRuntimeEffect? Effect => GlassRimFilter.Available && GlassRimStrip.Available ? GlassRimFilter.Blender : null;

    internal readonly record struct Parameters(
        Rect Rect, CornerRadius CornerRadius, double Smoothing, double Absorption, double Lighten, double Boost,
        double Spread, double SpreadWhite, double Glow, Color FallbackBorder, RimMethod Method = RimMethod.Auto,
        int StripRow = MaxStripRow)
    {
        /// <summary>The widest rim with its glow and a soft edge, in px: how deep the band reaches.</summary>
        public double DepthRange => MaxWidth + Glow + 0.75;

        /// <summary>The band the rim can cover, in px from the edge.</summary>
        public double BandWidth => MaxWidth + Glow + 1;
    }

    private readonly Parameters _p;
    private GlassRimStrip? _strip;
    private GlassRimFilter? _filter;
    private double _deviceScale;

    public GlassRimDrawOperation(Parameters parameters) => _p = parameters;

    public Rect Bounds => _p.Rect;

    public bool HitTest(Point p) => false;

    public bool Equals(ICustomDrawOperation? other) => other is GlassRimDrawOperation o && o._p == _p;

    public override bool Equals(object? obj) => obj is ICustomDrawOperation o && Equals(o);

    public override int GetHashCode() => _p.GetHashCode();

    /// <summary>Which method drew the last frame (for tests).</summary>
    internal RimMethod LastMethod { get; private set; }

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
        if (deviceScale != _deviceScale)
        {
            DisposeMethods();
            _deviceScale = deviceScale;
        }
        var opacity = Math.Clamp(lease.CurrentOpacity, 0, 1);

        if (_p.Method != RimMethod.Filter && lease.SkSurface is { } surface)
        {
            _strip ??= GlassRimStrip.Create(_p, deviceScale);
            if (_strip is not null && _strip.Draw(canvas, surface, lease.GrContext, opacity))
            {
                LastMethod = RimMethod.Strip;
                return;
            }
        }
        if (_p.Method == RimMethod.Strip)
            return;
        _filter ??= GlassRimFilter.Create(_p, deviceScale);
        _filter?.Draw(canvas, opacity);
        LastMethod = RimMethod.Filter;
    }

    public void Dispose() => DisposeMethods();

    private void DisposeMethods()
    {
        _strip?.Dispose();
        _strip = null;
        _filter?.Dispose();
        _filter = null;
    }

    /// <summary>Sets the uniforms of <see cref="RimCode"/>.</summary>
    internal static void SetRimUniforms(SKRuntimeEffectUniforms u, in Parameters p, double pixel)
    {
        u["absorption"] = (float)p.Absorption;
        u["lighten"] = (float)p.Lighten;
        u["minWidth"] = (float)MinWidth;
        u["maxWidth"] = (float)MaxWidth;
        u["pixel"] = (float)pixel;
        u["boost"] = (float)p.Boost;
        u["boostFrom"] = (float)BoostFrom;
        u["boostTo"] = (float)BoostTo;
        u["boostWidth"] = (float)BoostWidth;
        u["spreadWhite"] = (float)p.SpreadWhite;
        u["excessScale"] = (float)ExcessScale;
        u["excessGain"] = (float)ExcessGain;
        u["glow"] = (float)p.Glow;
    }

    /// <summary>Sets the uniforms of <see cref="SquircleDistance.Code"/>.</summary>
    internal static void SetShapeUniforms(SKRuntimeEffectUniforms u, in Parameters p)
    {
        var (points, extents) = SquircleDistance.Corners(new Rect(p.Rect.Size), p.CornerRadius, p.Smoothing);
        u["size"] = new[] { (float)p.Rect.Width, (float)p.Rect.Height };
        u["cornerP"] = extents;
        u["pts"] = points;
    }

    /// <summary>
    /// Where the rim's band lies: the top and bottom strips hold the corners; the side strips are
    /// just wider than the band. Small glass, whose whole area (with <paramref name="margin"/>) is
    /// at most 1.5 times the strips' area, takes one rectangle instead.
    /// </summary>
    internal static SKRect[] Strips(in Parameters p, double margin)
    {
        var r = p.Rect;
        var c = p.CornerRadius;
        var top = Math.Max(c.TopLeft, c.TopRight) + 3 + p.Glow;
        var bottom = Math.Max(c.BottomLeft, c.BottomRight) + 3 + p.Glow;
        var side = (float)(MaxWidth + 2 + p.Glow);
        double Reach(double w, double h) => (w + 2 * margin) * (h + 2 * margin);
        if (top + bottom >= r.Height || Reach(r.Width, r.Height) <= 1.5 * (2 * Reach(r.Width, top) + 2 * Reach(side, r.Height - top - bottom)))
            return [ToSkRect(r)];
        return
        [
            new SKRect((float)r.X, (float)r.Y, (float)r.Right, (float)(r.Y + top)),
            new SKRect((float)r.X, (float)(r.Bottom - bottom), (float)r.Right, (float)r.Bottom),
            new SKRect((float)r.X, (float)(r.Y + top), (float)r.X + side, (float)(r.Bottom - bottom)),
            new SKRect((float)r.Right - side, (float)(r.Y + top), (float)r.Right, (float)(r.Bottom - bottom)),
        ];
    }

    internal static SKRect ToSkRect(Rect r) => new((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom);
}
