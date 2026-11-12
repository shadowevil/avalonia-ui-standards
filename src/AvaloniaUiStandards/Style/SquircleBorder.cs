using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// A decorator that draws its background and border with continuous (squircle) corners
/// and clips its child to the same shape.
/// </summary>
/// <remarks>
/// The stroke is inset by half the border thickness, so it lies fully inside the shape, and
/// is drawn above the child, so a child with a background does not cover it at the corners.
/// Continuous corners need one stroke width: the largest side of <see cref="BorderThickness"/>
/// is used for the stroke, while layout honours each side.
/// The decorator manages its own <see cref="Visual.Clip"/>.
/// </remarks>
public class SquircleBorder : Decorator
{
    /// <summary>The default smoothing, matching the <c>CornerSmoothing</c> resource.</summary>
    internal const double DefaultSmoothing = 0.6;

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<SquircleBorder>();

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<SquircleBorder>();

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        Border.BorderThicknessProperty.AddOwner<SquircleBorder>();

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<SquircleBorder>();

    // Theme-wide only: fed from the CornerSmoothing resource by StandardsTheme.
    internal static readonly StyledProperty<double> SmoothingProperty =
        AvaloniaProperty.Register<SquircleBorder, double>(nameof(Smoothing), DefaultSmoothing);

    private readonly BorderOverlay _borderOverlay;
    private ShapeCache _shape;

    static SquircleBorder()
    {
        AffectsRender<SquircleBorder>(
            BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty, SmoothingProperty);
        AffectsMeasure<SquircleBorder>(BorderThicknessProperty);
        AffectsArrange<SquircleBorder>(CornerRadiusProperty, SmoothingProperty);
        ChildProperty.Changed.AddClassHandler<SquircleBorder>((b, _) => b._borderOverlay.MoveToTop(b.VisualChildren));
    }

    public SquircleBorder()
    {
        _borderOverlay = new BorderOverlay(RenderBorder);
        VisualChildren.Add(_borderOverlay);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>Corner smoothing from 0 (circular corners) to 1 (fully continuous).</summary>
    internal double Smoothing => GetValue(SmoothingProperty);

    /// <summary>The fill outline for the current size.</summary>
    internal Geometry? FillGeometry => _shape.Fill;

    protected override Size MeasureOverride(Size availableSize)
    {
        _borderOverlay.Measure(availableSize);
        return LayoutHelper.MeasureChild(Child, availableSize, Padding, BorderThickness);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var shape = Shape(finalSize);
        SetCurrentValue(ClipProperty, shape.Fill);
        _borderOverlay.Arrange(new Rect(finalSize));
        return LayoutHelper.ArrangeChild(Child, finalSize, Padding, BorderThickness);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BorderBrushProperty || change.Property == BorderThicknessProperty
            || change.Property == CornerRadiusProperty || change.Property == SmoothingProperty)
            _borderOverlay.InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var shape = Shape(Bounds.Size);
        if (shape.Fill is not null && Background is { } background)
            context.DrawGeometry(background, null, shape.Fill);
    }

    // Drawn by the overlay, above the child.
    private void RenderBorder(DrawingContext context)
    {
        var shape = Shape(Bounds.Size);
        if (BorderBrush is { } borderBrush && shape.Stroke is not null)
            context.DrawGeometry(null, new Pen(borderBrush, shape.StrokeWidth), shape.Stroke);
    }

    private ShapeCache Shape(Size size)
    {
        var t = BorderThickness;
        var strokeWidth = Math.Max(Math.Max(t.Left, t.Right), Math.Max(t.Top, t.Bottom));
        var key = new ShapeKey(size, CornerRadius, Smoothing, strokeWidth);
        if (_shape.Key == key)
            return _shape;

        _shape = ShapeCache.Create(key);
        return _shape;
    }

    private readonly record struct ShapeKey(Size Size, CornerRadius Radius, double Smoothing, double StrokeWidth);

    private readonly record struct ShapeCache(ShapeKey? Key, Geometry? Fill, Geometry? Stroke, double StrokeWidth)
    {
        public static ShapeCache Create(ShapeKey key)
        {
            if (key.Size.Width <= 0 || key.Size.Height <= 0)
                return new ShapeCache(key, null, null, 0);

            var rect = new Rect(key.Size);
            var fill = SquircleGeometry.Create(rect, key.Radius, key.Smoothing);

            Geometry? stroke = null;
            if (key.StrokeWidth > 0)
            {
                var (strokeRect, strokeRadius) = SquircleGeometry.Inset(rect, key.Radius, key.StrokeWidth / 2);
                stroke = SquircleGeometry.Create(strokeRect, strokeRadius, key.Smoothing);
            }

            return new ShapeCache(key, fill, stroke, key.StrokeWidth);
        }
    }
}
