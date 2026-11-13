using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Draws the glass fill in one Skia backdrop layer clipped to the squircle path. Its filter
/// refracts and blurs the backdrop, lays the tint over it, then the rim: a wider blur of the
/// backdrop reduced to its luminance, which sets the opacity of the rim colour in a band
/// that fades inward from the edge. Without runtime effects refraction is skipped; without
/// Skia it falls back to a solid fill.
/// </summary>
internal sealed class GlassDrawOperation : ICustomDrawOperation
{
    /// <summary>The rim band: full at the edge, half at this depth in px, gone at twice it.</summary>
    internal const double RimWidth = 2;

    /// <summary>The blur, as a Gaussian sigma in px, over which the rim samples the backdrop's brightness.</summary>
    internal const double RimSampleSigma = 12;

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
        Color RimColor,
        double RimAbsorption,
        double BackdropScale)
    {
        public bool UsesRefraction => RefractionStrength > 0 && RefractionEdge > 0 && GlassRefraction.Effect is not null;

        public bool UsesRim => RimAbsorption > 0 && RimColor.A > 0;
    }

    private readonly Parameters _p;
    private SKPath? _path;
    private SKImageFilter? _backdrop;
    private SKPicture? _rimMask;
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
        _rimMask?.Dispose();
    }

    // Without Skia there is no path drawing here; a circular rounded rectangle is the closest shape.
    private void RenderFallback(ImmediateDrawingContext context)
    {
        var r = _p.CornerRadius.TopLeft;
        context.DrawRectangle(new ImmutableSolidColorBrush(_p.Tint), null, _p.Rect, r, r, default);
    }

    // Refraction and blur (null input = the backdrop), the tint over them, then the rim over that.
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
        if (!_p.UsesRim)
            return tinted;

        using var rim = BuildRimFilter();
        var glass = SKImageFilter.CreateBlendMode(SKBlendMode.SrcOver, tinted, rim);
        tinted?.Dispose();
        return glass;
    }

    // The rim colour at an opacity of absorption × the luminance of the widely blurred backdrop,
    // kept only in the band along the edge.
    private SKImageFilter BuildRimFilter()
    {
        var c = _p.RimColor;
        var k = (float)(_p.RimAbsorption * c.A / 255.0);
        var luminance = new float[]
        {
            0, 0, 0, 0, c.R / 255f,
            0, 0, 0, 0, c.G / 255f,
            0, 0, 0, 0, c.B / 255f,
            0.2126f * k, 0.7152f * k, 0.0722f * k, 0, 0,
        };
        var sigma = (float)RimSampleSigma;
        using var wide = SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Clamp, null);
        using var toRim = SKColorFilter.CreateColorMatrix(luminance);
        using var coloured = SKImageFilter.CreateColorFilter(toRim, wide);

        _rimMask ??= BuildRimMask();
        using var band = SKImageFilter.CreatePicture(_rimMask, ToSkRect(_p.Rect));
        return SKImageFilter.CreateBlendMode(SKBlendMode.DstIn, coloured, band);
    }

    // The band: the edge stroked 2 × RimWidth wide (half of it inside the clip), softened so it
    // fades inward.
    private SKPicture BuildRimMask()
    {
        using var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(ToSkRect(_p.Rect));
        using var paint = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)(2 * RimWidth),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, (float)(RimWidth / 2)),
        };
        canvas.DrawPath(_path, paint);
        return recorder.EndRecording();
    }

    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.None);

    private static Rect ScaleRect(Rect r, double s) => new(r.X * s, r.Y * s, r.Width * s, r.Height * s);

    private static CornerRadius ScaleRadius(CornerRadius r, double s) =>
        new(r.TopLeft * s, r.TopRight * s, r.BottomRight * s, r.BottomLeft * s);

    private static SKRect ToSkRect(Rect r) => new((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom);

    private static SKColor ToSkColor(Color c, double alpha) => new(c.R, c.G, c.B, ToByte(alpha));

    private static byte ToByte(double unit) => (byte)Math.Round(Math.Clamp(unit, 0, 1) * 255);
}
