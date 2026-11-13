using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Draws the glass fill in one Skia backdrop layer clipped to the squircle path. Its filter
/// refracts and blurs the backdrop, then lays the tint over it. Without runtime effects
/// refraction is skipped; without Skia it falls back to a solid fill.
/// </summary>
internal sealed class GlassDrawOperation : ICustomDrawOperation
{
    internal readonly record struct Parameters(
        Rect Rect,
        CornerRadius CornerRadius,
        double Smoothing,
        Color Tint,
        double TintOpacity,
        double BlurSigma,
        double RefractionStrength,
        double RefractionEdge,
        double ChromaticDispersion,
        double BackdropScale)
    {
        public bool UsesRefraction => RefractionStrength > 0 && RefractionEdge > 0 && GlassRefraction.Effect is not null;
    }

    private readonly Parameters _p;
    private SKPath? _path;
    private SKImageFilter? _backdrop;
    private double _deviceScale;

    public GlassDrawOperation(Parameters parameters) => _p = parameters;

    public Rect Bounds => _p.Rect;

    public bool HitTest(Point p) => _p.Rect.Contains(p);

    public bool Equals(ICustomDrawOperation? other) => other is GlassDrawOperation o && o._p == _p;

    public override bool Equals(object? obj) => obj is ICustomDrawOperation o && Equals(o);

    public override int GetHashCode() => _p.GetHashCode();

    public void Render(ImmediateDrawingContext context)
    {
        var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature is null)
        {
            RenderFallback(context);
            return;
        }

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;
        var opacity = lease.CurrentOpacity;
        _path ??= SquircleGeometry.CreateSkPath(_p.Rect, _p.CornerRadius, _p.Smoothing);

        // The filter depends on the device scale (BackdropScale is in device px), so it is rebuilt
        // when that changes, for example on a move to a screen with another scaling.
        var m = canvas.TotalMatrix;
        var deviceScale = Math.Sqrt(m.ScaleX * m.ScaleX + m.SkewY * m.SkewY);
        if (_backdrop is null || deviceScale != _deviceScale)
        {
            _backdrop?.Dispose();
            _deviceScale = deviceScale;
            _backdrop = BuildBackdropFilter(deviceScale);
        }

        canvas.Save();
        canvas.ClipPath(_path, SKClipOperation.Intersect, true);

        using var layerPaint = new SKPaint { Color = SKColors.White.WithAlpha(ToByte(opacity)) };
        var layer = new SKCanvasSaveLayerRec
        {
            Bounds = ToSkRect(_p.Rect),
            Backdrop = _backdrop,
            Paint = opacity < 1 ? layerPaint : null,
        };
        canvas.SaveLayer(in layer);
        canvas.Restore();
        canvas.Restore();
    }

    public void Dispose()
    {
        _path?.Dispose();
        _backdrop?.Dispose();
    }

    // Without Skia there is no path drawing here; a circular rounded rectangle is the closest shape.
    private void RenderFallback(ImmediateDrawingContext context)
    {
        var r = _p.CornerRadius.TopLeft;
        context.DrawRectangle(new ImmutableSolidColorBrush(_p.Tint), null, _p.Rect, r, r, default);
    }

    // Refraction and blur (null input = the backdrop), then the tint over them.
    // Null when none applies.
    private SKImageFilter? BuildBackdropFilter(double deviceScale)
    {
        // With a blur, refraction and blur run on a copy of the backdrop at BackdropScale of the
        // device resolution, and the result is scaled back up: the blur hides the lower resolution.
        var s = _p.BlurSigma > 0 && deviceScale > 0 ? Math.Min(1, _p.BackdropScale / deviceScale) : 1;
        using var source = s < 1 ? SKImageFilter.CreateMatrix(SKMatrix.CreateScale((float)s, (float)s), Sampling, null) : null;

        var refraction = _p.UsesRefraction
            ? GlassRefraction.CreateFilter(ScaleRect(_p.Rect, s), ScaleRadius(_p.CornerRadius, s),
                _p.RefractionStrength * s, _p.RefractionEdge * s, _p.ChromaticDispersion, source)
            : null;

        var blurred = refraction;
        if (_p.BlurSigma > 0)
        {
            var sigma = (float)(_p.BlurSigma * s);
            blurred = SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Clamp, refraction ?? source);
            refraction?.Dispose();
        }
        if (s < 1)
        {
            var up = SKImageFilter.CreateMatrix(SKMatrix.CreateScale((float)(1 / s), (float)(1 / s)), Sampling, blurred);
            blurred?.Dispose();
            blurred = up;
        }

        // Skia returns no filter for a blend that changes nothing (a transparent tint).
        var tinted = blurred;
        using var tintColor = SKColorFilter.CreateBlendMode(ToSkColor(_p.Tint, _p.Tint.A / 255.0 * _p.TintOpacity), SKBlendMode.SrcOver);
        if (tintColor is not null)
        {
            tinted = SKImageFilter.CreateColorFilter(tintColor, blurred);
            blurred?.Dispose();
        }
        return tinted;
    }

    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.None);

    private static Rect ScaleRect(Rect r, double s) => new(r.X * s, r.Y * s, r.Width * s, r.Height * s);

    private static CornerRadius ScaleRadius(CornerRadius r, double s) =>
        new(r.TopLeft * s, r.TopRight * s, r.BottomRight * s, r.BottomLeft * s);

    private static SKRect ToSkRect(Rect r) => new((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom);

    private static SKColor ToSkColor(Color c, double alpha) => new(c.R, c.G, c.B, ToByte(alpha));

    private static byte ToByte(double unit) => (byte)Math.Round(Math.Clamp(unit, 0, 1) * 255);
}
