using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards;

/// <summary>
/// The library theme, used in place of <c>FluentTheme</c>, and the entry point that assembles
/// every tier: Fluent with the neutral palettes and the accent, the Style tier resources (type,
/// spacing, shape, motion, glass, danger) and the Blocks tier control themes. Light or dark
/// follows the application's <c>RequestedThemeVariant</c>, which follows the OS by default.
/// </summary>
public class StandardsTheme : Styles
{
    /// <summary>The default accent colour, #007CEA.</summary>
    public static readonly Color DefaultAccent = Color.FromRgb(0x00, 0x7C, 0xEA);

    public static readonly DirectProperty<StandardsTheme, Color> AccentProperty =
        AvaloniaProperty.RegisterDirect<StandardsTheme, Color>(
            nameof(Accent), o => o.Accent, (o, v) => o.Accent = v, DefaultAccent);

    public static readonly DirectProperty<StandardsTheme, DensityStyle> DensityProperty =
        AvaloniaProperty.RegisterDirect<StandardsTheme, DensityStyle>(
            nameof(Density), o => o.Density, (o, v) => o.Density = v);

    public static readonly DirectProperty<StandardsTheme, bool?> ReduceMotionProperty =
        AvaloniaProperty.RegisterDirect<StandardsTheme, bool?>(
            nameof(ReduceMotion), o => o.ReduceMotion, (o, v) => o.ReduceMotion = v);

    public static readonly DirectProperty<StandardsTheme, bool?> ReduceTransparencyProperty =
        AvaloniaProperty.RegisterDirect<StandardsTheme, bool?>(
            nameof(ReduceTransparency), o => o.ReduceTransparency, (o, v) => o.ReduceTransparency = v);

    // The effective setting (app override, else OS), bound into GlassBorder by the theme style.
    internal static readonly DirectProperty<StandardsTheme, bool> IsTransparencyReducedProperty =
        AvaloniaProperty.RegisterDirect<StandardsTheme, bool>(
            nameof(IsTransparencyReduced), o => o.IsTransparencyReduced);

    /// <summary>The danger colour, for destructive actions, with white text on it.</summary>
    private static readonly Color Danger = Color.FromRgb(0xE1, 0x33, 0x2A);

    private static readonly TimeSpan MotionFast = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MotionNormal = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan MotionSlow = TimeSpan.FromMilliseconds(300);
    private static readonly Easing MotionEasing = new CubicEaseOut();

    private readonly FluentTheme _fluent;
    private readonly ColorPaletteResources _light;
    private readonly ColorPaletteResources _dark;
    private readonly OsAccessibility.Settings _os;
    private Color _accent = DefaultAccent;
    private bool? _reduceMotion;
    private bool? _reduceTransparency;
    private bool _isTransparencyReduced;

    /// <summary>Creates the theme, reading the OS reduce-motion and reduce-transparency settings.</summary>
    public StandardsTheme() : this(OsAccessibility.Read())
    {
    }

    internal StandardsTheme(OsAccessibility.Settings os)
    {
        _os = os;
        _isTransparencyReduced = os.ReduceTransparency;

        _light = LightPalette();
        _dark = DarkPalette();
        _light.Accent = _dark.Accent = _accent;

        _fluent = new FluentTheme { DensityStyle = DensityStyle.Normal };
        _fluent.Palettes[ThemeVariant.Light] = _light;
        _fluent.Palettes[ThemeVariant.Dark] = _dark;
        Add(_fluent);

        AddResources(Resources);
        AddDanger(Resources, Danger);
        AddFills(Resources, "AccentFillColor", _accent);
        AddAccentText(_accent);
        UpdateMotion();

        // The Blocks tier control themes (Fluent overrides, 0004).
        Blocks.BlocksThemes.AddTo(Resources);
        Blocks.BlocksThemes.ApplyDensity(Resources, _fluent.DensityStyle);

        Add(new Avalonia.Styling.Style(x => x.OfType<SquircleBorder>())
        {
            Setters = { new Setter(SquircleBorder.SmoothingProperty, new DynamicResourceExtension("CornerSmoothing")) },
        });
        Add(new Avalonia.Styling.Style(x => x.OfType<GlassBorder>())
        {
            Setters =
            {
                new Setter(GlassBorder.TintColorProperty, new DynamicResourceExtension("GlassTintColor")),
                new Setter(GlassBorder.TintOpacityProperty, new DynamicResourceExtension("GlassTintOpacity")),
                new Setter(GlassBorder.BlurSigmaProperty, new DynamicResourceExtension("GlassBlurSigma")),
                new Setter(GlassBorder.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusOverlay")),
                new Setter(GlassBorder.BorderBrushProperty, new DynamicResourceExtension("GlassBorderBrush")),
                new Setter(GlassBorder.RefractionStrengthProperty, new DynamicResourceExtension("GlassRefractionStrength")),
                new Setter(GlassBorder.RefractionEdgeProperty, new DynamicResourceExtension("GlassRefractionEdge")),
                new Setter(GlassBorder.ChromaticDispersionProperty, new DynamicResourceExtension("GlassChromaticDispersion")),
                new Setter(GlassBorder.SmoothingProperty, new DynamicResourceExtension("CornerSmoothing")),
                new Setter(GlassBorder.ReduceTransparencyProperty, this.GetObservable(IsTransparencyReducedProperty).ToBinding()),
            },
        });
    }

    /// <summary>The accent colour. Defaults to <see cref="DefaultAccent"/>.</summary>
    public Color Accent
    {
        get => _accent;
        set
        {
            if (SetAndRaise(AccentProperty, ref _accent, value))
            {
                _light.Accent = _dark.Accent = value;
                AddFills(Resources, "AccentFillColor", value);
                AddAccentText(value);
            }
        }
    }

    /// <summary>Fluent control density. Defaults to <see cref="DensityStyle.Normal"/>.</summary>
    public DensityStyle Density
    {
        get => _fluent.DensityStyle;
        set
        {
            var old = _fluent.DensityStyle;
            if (old == value)
                return;
            _fluent.DensityStyle = value;
            Blocks.BlocksThemes.ApplyDensity(Resources, value);
            RaisePropertyChanged(DensityProperty, old, value);
        }
    }

    /// <summary>
    /// Makes motion instant when true: the <c>MotionFast</c>, <c>MotionNormal</c> and
    /// <c>MotionSlow</c> durations become zero. Null (the default) follows the OS setting,
    /// read when the theme is created.
    /// </summary>
    public bool? ReduceMotion
    {
        get => _reduceMotion;
        set
        {
            if (SetAndRaise(ReduceMotionProperty, ref _reduceMotion, value))
                UpdateMotion();
        }
    }

    /// <summary>
    /// Draws glass as a solid fill when true. Null (the default) follows the OS setting,
    /// read when the theme is created.
    /// </summary>
    public bool? ReduceTransparency
    {
        get => _reduceTransparency;
        set
        {
            if (SetAndRaise(ReduceTransparencyProperty, ref _reduceTransparency, value))
                SetAndRaise(IsTransparencyReducedProperty, ref _isTransparencyReduced, value ?? _os.ReduceTransparency);
        }
    }

    internal bool IsTransparencyReduced => _isTransparencyReduced;

    private void UpdateMotion()
    {
        var reduced = _reduceMotion ?? _os.ReduceMotion;
        Resources["MotionFast"] = reduced ? TimeSpan.Zero : MotionFast;
        Resources["MotionNormal"] = reduced ? TimeSpan.Zero : MotionNormal;
        Resources["MotionSlow"] = reduced ? TimeSpan.Zero : MotionSlow;

        // Control transitions are fixed objects (as Fluent declares them), replaced here when motion changes.
        Blocks.BlocksThemes.ApplyMotion(Resources, reduced ? TimeSpan.Zero : MotionFast, MotionEasing);
    }

    private static void AddResources(IResourceDictionary r)
    {
        // Type
        var inter = new FontFamily("fonts:Inter#Inter");
        r["ContentControlThemeFontFamily"] = inter;
        r["FontSizeCaption"] = 12d;
        r["FontSizeBody"] = 14d;
        r["FontSizeBodyStrong"] = 14d;
        r["FontWeightBodyStrong"] = FontWeight.SemiBold;
        r["FontSizeSubtitle"] = 20d;
        r["FontWeightSubtitle"] = FontWeight.SemiBold;
        r["FontSizeTitle"] = 28d;
        r["FontWeightTitle"] = FontWeight.SemiBold;
        r["FontSizeLargeTitle"] = 40d;
        r["FontWeightLargeTitle"] = FontWeight.SemiBold;
        r["FontSizeDisplay"] = 68d;
        r["FontWeightDisplay"] = FontWeight.SemiBold;

        // Spacing, on a 4 px grid
        r["SpacingXS"] = Spacing.XS;
        r["SpacingS"] = Spacing.S;
        r["SpacingM"] = Spacing.M;
        r["SpacingL"] = Spacing.L;
        r["SpacingXL"] = Spacing.XL;
        r["SpacingXXL"] = Spacing.XXL;
        r["SpacingXXXL"] = Spacing.XXXL;

        // Shape
        r["CornerRadiusControl"] = new CornerRadius(8);
        r["CornerRadiusOverlay"] = new CornerRadius(16);
        r["ControlCornerRadius"] = new CornerRadius(8);
        r["OverlayCornerRadius"] = new CornerRadius(16);
        r["CornerSmoothing"] = SquircleBorder.DefaultSmoothing;

        // Motion (durations are set by UpdateMotion)
        r["MotionEasing"] = MotionEasing;

        // Glass
        r["GlassBlurSigma"] = GlassBorder.DefaultBlurSigma;
        r["GlassRefractionStrength"] = GlassBorder.DefaultRefractionStrength;
        r["GlassRefractionEdge"] = GlassBorder.DefaultRefractionEdge;
        r["GlassChromaticDispersion"] = GlassBorder.DefaultChromaticDispersion;
        r.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary
        {
            ["GlassTintColor"] = GlassBorder.DefaultTintLight,
            ["GlassTintOpacity"] = GlassBorder.DefaultTintOpacity,
            ["GlassBorderBrush"] = new SolidColorBrush(GlassBorder.DefaultBorderLight),
        };
        r.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
        {
            ["GlassTintColor"] = GlassBorder.DefaultTintDark,
            ["GlassTintOpacity"] = GlassBorder.DefaultTintOpacity,
            ["GlassBorderBrush"] = new SolidColorBrush(GlassBorder.DefaultBorderDark),
        };
    }

    /// <summary>
    /// Sets <c>DangerColor</c>, <c>DangerForeground</c> (the text on it) and the AA danger fills.
    /// </summary>
    private static void AddDanger(IResourceDictionary r, Color danger)
    {
        r["DangerColor"] = danger;
        r["DangerForeground"] = Colors.White;
        AddFills(r, "DangerFillColor", danger);
    }

    /// <summary>
    /// Sets <c>AccentTextColor</c> and <c>AccentTextColorPressed</c> per theme: accent-hued text
    /// that meets WCAG AA on that theme's page (<c>RegionColor</c>) and card (<c>ChromeMediumLow</c>)
    /// colours (see <see cref="AccessibleColors"/>).
    /// </summary>
    private void AddAccentText(Color brand)
    {
        SetAccentText(ThemeVariant.Light, AccessibleColors.ForText(brand, [_light.RegionColor, _light.ChromeMediumLow], onDark: false));
        SetAccentText(ThemeVariant.Dark, AccessibleColors.ForText(brand, [_dark.RegionColor, _dark.ChromeMediumLow], onDark: true));
    }

    private void SetAccentText(ThemeVariant variant, (Color Normal, Color Pressed) text)
    {
        var dictionary = (IResourceDictionary)Resources.ThemeDictionaries[variant];
        dictionary["AccentTextColor"] = text.Normal;
        dictionary["AccentTextColorPressed"] = text.Pressed;
    }

    /// <summary>
    /// Sets <c>{key}</c>, <c>{key}PointerOver</c> and <c>{key}Pressed</c>: the WCAG AA fills for
    /// white text on <paramref name="brand"/> (see <see cref="AccessibleColors"/>).
    /// </summary>
    private static void AddFills(IResourceDictionary r, string key, Color brand)
    {
        var (normal, pointerOver, pressed) = AccessibleColors.ForWhiteText(brand);
        r[key] = normal;
        r[key + "PointerOver"] = pointerOver;
        r[key + "Pressed"] = pressed;
    }

    private static ColorPaletteResources LightPalette() => new()
    {
        RegionColor = Hex(0xF5F5F5),
        AltHigh = Hex(0xFFFFFF),
        BaseHigh = Hex(0x1A1A1A),
        BaseMediumHigh = Hex(0x404040),
        BaseMedium = Hex(0x737373),
        BaseMediumLow = Hex(0xA6A6A6),
        BaseLow = Hex(0xD4D4D4),
        ChromeLow = Hex(0xE8E8E8),
        ChromeMediumLow = Hex(0xFFFFFF),
        ChromeMedium = Hex(0xE8E8E8),
        ChromeHigh = Hex(0xD4D4D4),
        ListLow = Hex(0xE8E8E8),
        ListMedium = Hex(0xD4D4D4),
    };

    private static ColorPaletteResources DarkPalette() => new()
    {
        RegionColor = Hex(0x1E1E1E),
        AltHigh = Hex(0x1E1E1E),
        BaseHigh = Hex(0xF2F2F2),
        BaseMediumHigh = Hex(0xD9D9D9),
        BaseMedium = Hex(0x9E9E9E),
        BaseMediumLow = Hex(0x6B6B6B),
        BaseLow = Hex(0x454545),
        ChromeLow = Hex(0x2A2A2A),
        ChromeMediumLow = Hex(0x2A2A2A),
        ChromeMedium = Hex(0x333333),
        ChromeHigh = Hex(0x525252),
        ListLow = Hex(0x2A2A2A),
        ListMedium = Hex(0x404040),
    };

    private static Color Hex(uint rgb) => Color.FromUInt32(0xFF000000 | rgb);
}
