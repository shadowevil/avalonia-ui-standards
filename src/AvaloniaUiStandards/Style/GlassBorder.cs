using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// A decorator drawn as frosted glass: what is already rendered behind it is refracted
/// near the edge, blurred and tinted, in a continuous-corner (squircle) shape, with a hairline
/// rim on the edge that takes a light tint of the backdrop just outside it (its hue kept,
/// fainter over dark areas). Its child is clipped to the same shape.
/// </summary>
/// <remarks>
/// Falls back to a solid fill in <see cref="TintColor"/> with a fixed 1 px border in
/// <see cref="BorderBrush"/> instead of the rim when transparency is reduced (<c>StandardsTheme.ReduceTransparency</c>) or Skia is not
/// available. Without Skia runtime effects it keeps the blur and drops refraction.
/// The 1 px border is counted in layout, so the child sits inside it, and is drawn above the
/// child, so a child with a background does not cover it at the corners.
/// The decorator manages its own <see cref="Visual.Clip"/>.
/// </remarks>
public class GlassBorder : Decorator
{
    private const double BorderWidth = 1;

    // Defaults, shared with the Glass* resources that StandardsTheme sets.
    internal static readonly Color DefaultTintLight = Colors.White;
    internal static readonly Color DefaultTintDark = Color.FromRgb(0x1E, 0x1E, 0x1E);
    internal const double DefaultTintOpacity = 0.68;
    internal static readonly Color DefaultBorderLight = Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF);
    internal static readonly Color DefaultBorderDark = Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF);
    internal const double DefaultBlurSigma = 6;
    internal const double DefaultRefractionStrength = 28;
    internal const double DefaultRefractionEdge = 28;
    internal const double DefaultChromaticDispersion = 0.35;
    internal const double DefaultRimAbsorption = 1;

    // How far the rim's sampled colour is lightened towards white (0 = as is, 1 = white), per theme.
    internal const double DefaultRimLightenLight = 0.3;
    internal const double DefaultRimLightenDark = 0.6;
    internal const double DefaultRimSpreadWhiteLight = 0.5;
    internal const double DefaultRimSpreadWhiteDark = 0.5;

    public static readonly StyledProperty<Color> TintColorProperty =
        AvaloniaProperty.Register<GlassBorder, Color>(nameof(TintColor), DefaultTintLight);

    public static readonly StyledProperty<double> TintOpacityProperty =
        AvaloniaProperty.Register<GlassBorder, double>(nameof(TintOpacity), DefaultTintOpacity);

    public static readonly StyledProperty<double> BlurSigmaProperty =
        AvaloniaProperty.Register<GlassBorder, double>(nameof(BlurSigma), DefaultBlurSigma);

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<GlassBorder>();

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<GlassBorder>();

    public static readonly StyledProperty<double> RefractionStrengthProperty =
        AvaloniaProperty.Register<GlassBorder, double>(nameof(RefractionStrength), DefaultRefractionStrength);

    public static readonly StyledProperty<double> RefractionEdgeProperty =
        AvaloniaProperty.Register<GlassBorder, double>(nameof(RefractionEdge), DefaultRefractionEdge);

    public static readonly StyledProperty<double> ChromaticDispersionProperty =
        AvaloniaProperty.Register<GlassBorder, double>(nameof(ChromaticDispersion), DefaultChromaticDispersion);

    // Keys of the rim's theme resources (its strength, and per theme its lightening and white
    // mix), which StandardsTheme feeds into the properties below.
    internal static readonly object RimAbsorptionKey = new();
    internal static readonly object RimLightenKey = new();
    internal static readonly object RimSpreadWhiteKey = new();

    internal static readonly StyledProperty<double> RimAbsorptionProperty =
        AvaloniaProperty.Register<GlassBorder, double>(nameof(RimAbsorption), DefaultRimAbsorption);

    internal static readonly StyledProperty<double> RimLightenProperty =
        AvaloniaProperty.Register<GlassBorder, double>("RimLighten", DefaultRimLightenLight);

    // How far light carried along the rim turns white (1) or keeps the feature's colour (0),
    // per theme from the resource under RimSpreadWhiteKey.
    internal static readonly StyledProperty<double> RimSpreadWhiteProperty =
        AvaloniaProperty.Register<GlassBorder, double>("RimSpreadWhite", DefaultRimSpreadWhiteLight);

    // The rim's boost of bright stretches; tests set 0 to measure the rim without it.
    internal static readonly StyledProperty<double> RimBoostProperty =
        AvaloniaProperty.Register<GlassBorder, double>("RimBoost", GlassRimDrawOperation.Boost);

    // How far the rim carries a bright feature along the edge; tests set 0 to measure without it.
    internal static readonly StyledProperty<double> RimSpreadProperty =
        AvaloniaProperty.Register<GlassBorder, double>("RimSpread", GlassRimDrawOperation.Spread);

    // The rim's inward glow; tests set 0 to measure without it.
    internal static readonly StyledProperty<double> RimGlowProperty =
        AvaloniaProperty.Register<GlassBorder, double>("RimGlow", GlassRimDrawOperation.GlowWidth);

    /// <summary>The fraction of the device resolution at which refraction and blur run (1 = full).</summary>
    internal const double DefaultBackdropScale = 0.5;

    // The scale refraction and blur run at; tests set 1 to render the full-resolution reference.
    internal static readonly StyledProperty<double> BackdropScaleProperty =
        AvaloniaProperty.Register<GlassBorder, double>("BackdropScale", DefaultBackdropScale);

    // Theme-wide only: StandardsTheme feeds Smoothing from the CornerSmoothing resource and
    // ReduceTransparency from its own effective setting.
    internal static readonly StyledProperty<double> SmoothingProperty =
        AvaloniaProperty.Register<GlassBorder, double>("Smoothing", SquircleBorder.DefaultSmoothing);

    internal static readonly StyledProperty<bool> ReduceTransparencyProperty =
        AvaloniaProperty.Register<GlassBorder, bool>("ReduceTransparency");

    private readonly BorderOverlay _borderOverlay;
    private ShapeKey? _shapeKey;
    private Geometry? _fill;
    private Geometry? _stroke;

    static GlassBorder()
    {
        AffectsRender<GlassBorder>(
            TintColorProperty, TintOpacityProperty, BlurSigmaProperty, CornerRadiusProperty, BorderBrushProperty,
            RefractionStrengthProperty, RefractionEdgeProperty, ChromaticDispersionProperty,
            RimAbsorptionProperty, RimLightenProperty, RimBoostProperty, RimSpreadProperty, RimSpreadWhiteProperty, RimGlowProperty, BackdropScaleProperty, SmoothingProperty, ReduceTransparencyProperty);
        AffectsArrange<GlassBorder>(CornerRadiusProperty, SmoothingProperty);
        ChildProperty.Changed.AddClassHandler<GlassBorder>((b, _) => b._borderOverlay.MoveToTop(b.VisualChildren));
    }

    public GlassBorder()
    {
        _borderOverlay = new BorderOverlay(RenderBorder);
        VisualChildren.Add(_borderOverlay);
    }

    /// <summary>The tint drawn over the blurred backdrop, and the solid fallback colour.</summary>
    public Color TintColor
    {
        get => GetValue(TintColorProperty);
        set => SetValue(TintColorProperty, value);
    }

    /// <summary>The opacity of the tint, from 0 to 1.</summary>
    public double TintOpacity
    {
        get => GetValue(TintOpacityProperty);
        set => SetValue(TintOpacityProperty, value);
    }

    /// <summary>The blur strength, as a Skia Gaussian sigma in px.</summary>
    public double BlurSigma
    {
        get => GetValue(BlurSigmaProperty);
        set => SetValue(BlurSigmaProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>The brush of the 1 px border.</summary>
    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    /// <summary>How far, in px, the backdrop is displaced at the rim. 0 turns refraction off.</summary>
    public double RefractionStrength
    {
        get => GetValue(RefractionStrengthProperty);
        set => SetValue(RefractionStrengthProperty, value);
    }

    /// <summary>The width, in px, of the curved rim band where refraction happens.</summary>
    public double RefractionEdge
    {
        get => GetValue(RefractionEdgeProperty);
        set => SetValue(RefractionEdgeProperty, value);
    }

    /// <summary>
    /// Splits refraction per colour channel, as real glass does. Red is displaced by (1 − value),
    /// green by 1 and blue by (1 + value) times <see cref="RefractionStrength"/>. 0 turns it off.
    /// </summary>
    public double ChromaticDispersion
    {
        get => GetValue(ChromaticDispersionProperty);
        set => SetValue(ChromaticDispersionProperty, value);
    }

    /// <summary>
    /// How strongly the rim shows, from 0 (no rim) to 1: its opacity where the backdrop just
    /// outside the edge is white. It scales with that backdrop's luminance.
    /// </summary>
    internal double RimAbsorption
    {
        get => GetValue(RimAbsorptionProperty);
        set => SetValue(RimAbsorptionProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _borderOverlay.Measure(availableSize);
        return LayoutHelper.MeasureChild(Child, availableSize, Padding, new Thickness(BorderWidth));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateShape(finalSize);
        SetCurrentValue(ClipProperty, _fill);
        _borderOverlay.Arrange(new Rect(finalSize));
        return LayoutHelper.ArrangeChild(Child, finalSize, Padding, new Thickness(BorderWidth));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BorderBrushProperty || change.Property == CornerRadiusProperty
            || change.Property == SmoothingProperty || change.Property == RimAbsorptionProperty
            || change.Property == RimLightenProperty || change.Property == RimBoostProperty || change.Property == RimSpreadProperty
            || change.Property == RimSpreadWhiteProperty || change.Property == RimGlowProperty
            || change.Property == ReduceTransparencyProperty)
            _borderOverlay.InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        UpdateShape(size);
        if (_fill is null)
            return;

        var solid = new ImmutableSolidColorBrush(TintColor);
        if (GetValue(ReduceTransparencyProperty))
        {
            context.DrawGeometry(solid, null, _fill);
        }
        else
        {
            context.Custom(new GlassDrawOperation(new GlassDrawOperation.Parameters(
                new Rect(size), CornerRadius, GetValue(SmoothingProperty), TintColor, TintOpacity, BlurSigma,
                RefractionStrength, RefractionEdge, ChromaticDispersion, GetValue(BackdropScaleProperty))));
        }
    }

    // Drawn by the overlay, above the child: the rim, or the fixed border for the solid
    // fallback and where Skia runtime effects are missing (the rim cannot sample there).
    private void RenderBorder(DrawingContext context)
    {
        var size = Bounds.Size;
        UpdateShape(size);
        if (!GetValue(ReduceTransparencyProperty) && GlassRimDrawOperation.Effect is not null)
        {
            if (RimAbsorption > 0)
                context.Custom(new GlassRimDrawOperation(new GlassRimDrawOperation.Parameters(
                    new Rect(size), CornerRadius, GetValue(SmoothingProperty), RimAbsorption, GetValue(RimLightenProperty),
                    GetValue(RimBoostProperty), GetValue(RimSpreadProperty), GetValue(RimSpreadWhiteProperty),
                    GetValue(RimGlowProperty),
                    (BorderBrush as ISolidColorBrush)?.Color ?? Colors.Transparent)));
        }
        else if (BorderBrush is { } borderBrush && _stroke is not null)
        {
            context.DrawGeometry(null, new Pen(borderBrush, BorderWidth), _stroke);
        }
    }

    private void UpdateShape(Size size)
    {
        var key = new ShapeKey(size, CornerRadius, GetValue(SmoothingProperty));
        if (_shapeKey == key)
            return;

        _shapeKey = key;
        if (size.Width <= 0 || size.Height <= 0)
        {
            _fill = _stroke = null;
            return;
        }

        var rect = new Rect(size);
        _fill = SquircleGeometry.Create(rect, key.Radius, key.Smoothing);
        var (strokeRect, strokeRadius) = SquircleGeometry.Inset(rect, key.Radius, BorderWidth / 2);
        _stroke = SquircleGeometry.Create(strokeRect, strokeRadius, key.Smoothing);
    }

    private readonly record struct ShapeKey(Size Size, CornerRadius Radius, double Smoothing);
}
