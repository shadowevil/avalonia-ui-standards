using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using AvaloniaUiStandards.Style;
using SkiaSharp;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The window's soft shadow: its squircle outline, offset down and blurred, drawn with Skia.
/// Without Skia there is no shadow.
/// </summary>
internal sealed class WindowShadow : Control
{
    /// <summary>The shadow's blur, as a Gaussian sigma in px.</summary>
    public const double Sigma = 8;

    /// <summary>How far the shadow is offset down, in px.</summary>
    public const double OffsetY = 8;

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<WindowShadow, CornerRadius>(nameof(CornerRadius));

    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<WindowShadow, Color>(nameof(Color));

    public static readonly StyledProperty<double> SmoothingProperty =
        AvaloniaProperty.Register<WindowShadow, double>(nameof(Smoothing), SquircleBorder.DefaultSmoothing);

    static WindowShadow()
    {
        AffectsRender<WindowShadow>(CornerRadiusProperty, ColorProperty, SmoothingProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<WindowShadow>(false);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public double Smoothing
    {
        get => GetValue(SmoothingProperty);
        set => SetValue(SmoothingProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (Bounds.Width > 0 && Bounds.Height > 0 && Color.A > 0)
            context.Custom(new ShadowOperation(new(new Rect(Bounds.Size), CornerRadius, Smoothing, Color)));
    }

    private readonly record struct Parameters(Rect Shape, CornerRadius Radius, double Smoothing, Color Color);

    private sealed class ShadowOperation(Parameters p) : ICustomDrawOperation
    {
        private readonly Parameters _p = p;
        private SKPath? _path;

        // The blur reaches about three sigma beyond the offset shape.
        public Rect Bounds => _p.Shape.Translate(new Vector(0, OffsetY)).Inflate(3 * Sigma);

        public bool HitTest(Point point) => false;

        public bool Equals(ICustomDrawOperation? other) => other is ShadowOperation o && o._p == _p;

        public override bool Equals(object? obj) => obj is ICustomDrawOperation o && Equals(o);

        public override int GetHashCode() => _p.GetHashCode();

        public void Render(ImmediateDrawingContext context)
        {
            if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } leaseFeature)
                return;

            using var lease = leaseFeature.Lease();
            _path ??= SquircleGeometry.CreateSkPath(_p.Shape.Translate(new Vector(0, OffsetY)), _p.Radius, _p.Smoothing);
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, (float)Sigma);
            using var paint = new SKPaint
            {
                Color = new SKColor(_p.Color.R, _p.Color.G, _p.Color.B, (byte)Math.Round(_p.Color.A * lease.CurrentOpacity)),
                MaskFilter = blur,
                IsAntialias = true,
            };
            lease.SkCanvas.DrawPath(_path, paint);
        }

        public void Dispose() => _path?.Dispose();
    }
}
