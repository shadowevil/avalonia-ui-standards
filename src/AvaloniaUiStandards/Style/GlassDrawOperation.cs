using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Draws the glass fill: a Skia backdrop layer (refraction, then blur) clipped to the
/// squircle path, then the tint. Without runtime effects refraction is skipped; without
/// Skia it falls back to a solid fill.
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
        double ChromaticDispersion)
    {
        public bool UsesRefraction => RefractionStrength > 0 && RefractionEdge > 0 && GlassRefraction.Effect is not null;
    }

    private readonly Parameters _p;
    private SKPath? _path;
    private SKImageFilter? _backdrop;

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
        _backdrop ??= BuildBackdropFilter();

        canvas.Save();
        canvas.ClipPath(_path, SKClipOperation.Intersect, true);

        if (_backdrop is not null)
        {
            using var layerPaint = new SKPaint { Color = SKColors.White.WithAlpha(ToByte(opacity)) };
            var layer = new SKCanvasSaveLayerRec
            {
                Bounds = ToSkRect(_p.Rect),
                Backdrop = _backdrop,
                Paint = opacity < 1 ? layerPaint : null,
            };
            canvas.SaveLayer(in layer);
            canvas.Restore();
        }

        using var tint = new SKPaint
        {
            Color = ToSkColor(_p.Tint, _p.Tint.A / 255.0 * _p.TintOpacity * opacity),
            IsAntialias = true,
        };
        canvas.DrawPath(_path, tint);
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

    private SKImageFilter? BuildBackdropFilter()
    {
        var refraction = _p.UsesRefraction
            ? GlassRefraction.CreateFilter(_p.Rect, _p.CornerRadius, _p.RefractionStrength, _p.RefractionEdge, _p.ChromaticDispersion)
            : null;

        if (_p.BlurSigma <= 0)
            return refraction;

        var sigma = (float)_p.BlurSigma;
        var blur = SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Clamp, refraction);
        refraction?.Dispose();
        return blur;
    }

    private static SKRect ToSkRect(Rect r) => new((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom);

    private static SKColor ToSkColor(Color c, double alpha) => new(c.R, c.G, c.B, ToByte(alpha));

    private static byte ToByte(double unit) => (byte)Math.Round(Math.Clamp(unit, 0, 1) * 255);
}
