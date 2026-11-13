using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards;

/// <summary>
/// The library theme, used in place of <c>FluentTheme</c>, and the entry point that assembles
/// every tier: Fluent with the neutral palettes and the accent, the Style tier resources (type,
/// spacing, shape, motion, glass, danger) and the Blocks tier control themes, including those of
/// the DataGrid and ColorPicker packages' controls over those packages' Fluent styles. Light or dark
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

    public static readonly DirectProperty<StandardsTheme, bool> HighRefreshRateProperty =
        AvaloniaProperty.RegisterDirect<StandardsTheme, bool>(
            nameof(HighRefreshRate), o => o.HighRefreshRate, (o, v) => o.HighRefreshRate = v);

    // The effective reduce-motion setting (app override, else OS), bound into StandardsWindow.
    internal static readonly DirectProperty<StandardsTheme, bool> IsMotionReducedProperty =
        AvaloniaProperty.RegisterDirect<StandardsTheme, bool>(
            nameof(IsMotionReduced), o => o.IsMotionReduced);

    // The effective setting (app override, else OS), bound into GlassBorder by the theme style.
    internal static readonly DirectProperty<StandardsTheme, bool> IsTransparencyReducedProperty =
        AvaloniaProperty.RegisterDirect<StandardsTheme, bool>(
            nameof(IsTransparencyReduced), o => o.IsTransparencyReduced);

    /// <summary>The danger colour, for destructive actions, with white text on it.</summary>
    private static readonly Color Danger = Color.FromRgb(0xE1, 0x33, 0x2A);

    /// <summary>The success colour, between the Windows success green (#0F7B0F) and the macOS system green (#34C759).</summary>
    private static readonly Color Success = Color.FromRgb(0x22, 0xA1, 0x34);

    /// <summary>The warning colour, between the Windows caution (#9D5D00) and the macOS system orange (#FF9500).</summary>
    private static readonly Color Warning = Color.FromRgb(0xCE, 0x79, 0x00);

    private static readonly TimeSpan MotionFast = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MotionNormal = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan MotionSlow = TimeSpan.FromMilliseconds(300);
    private static readonly Easing MotionEasing = new CubicEaseOut();

    /// <summary>The DataGrid package's Fluent theme (<c>Avalonia.Controls.DataGrid</c>).</summary>
    internal static readonly Uri DataGridFluentSource = new("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml");

    private readonly FluentTheme _fluent;
    private readonly StyleInclude _dataGrid;
    private readonly ColorPaletteResources _light;
    private readonly ColorPaletteResources _dark;
    private readonly OsAccessibility.Settings _os;
    private Color _accent = DefaultAccent;
    private bool? _reduceMotion;
    private bool? _reduceTransparency;
    private bool _isTransparencyReduced;
    private bool _highRefreshRate;
    private bool _isMotionReduced;

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
        // The DataGrid and ColorPicker packages' Fluent styles, after Fluent's, as Fluent's own are
        // included; the Blocks themes below restyle their controls.
        _dataGrid = new StyleInclude(new Uri("avares://AvaloniaUiStandards/")) { Source = DataGridFluentSource };
        Add(_dataGrid);
        Add(new StyleInclude(new Uri("avares://AvaloniaUiStandards/"))
        {
            Source = new Uri("avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml"),
        });

        AddResources(Resources);
        AddDanger(Resources, Danger);
        Resources["SuccessColor"] = Success;
        Resources["WarningColor"] = Warning;
        AddFills(Resources, "AccentFillColor", _accent);
        AddAccentText(_accent);
        UpdateMotion();

        // The Blocks tier control themes (Fluent overrides, 0004).
        Blocks.BlocksThemes.AddTo(Resources, type =>
            _fluent.TryGetResource(type, null, out var fluentTheme) || _dataGrid.TryGetResource(type, null, out fluentTheme)
                ? fluentTheme as ControlTheme
                : null);
        Blocks.BlocksThemes.ApplyDensity(Resources, _fluent.DensityStyle);
        foreach (var style in Blocks.BlocksThemes.Styles())
            Add(style);

        // The Modules tier control themes; StandardsWindow builds on Fluent's Window theme.
        _fluent.TryGetResource(typeof(Window), null, out var windowTheme);
        Modules.ModulesThemes.AddTo(Resources, windowTheme as ControlTheme);
        Modules.ModulesThemes.ApplyDensity(Resources, _fluent.DensityStyle);
        Add(Modules.StandardsWindowTheme.StatusBarTextStyle());
        // Frame ticks for continuous animation: the app-wide rate default and reduce motion.
        Add(new Avalonia.Styling.Style(x => x.Is<Modules.StandardsWindow>())
        {
            Setters =
            {
                new Setter(Modules.StandardsWindow.HighRefreshRateProperty, this.GetObservable(HighRefreshRateProperty).ToBinding()),
                new Setter(Modules.StandardsWindow.ReduceMotionProperty, this.GetObservable(IsMotionReducedProperty).ToBinding()),
            },
        });

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
                new Setter(GlassBorder.RimAbsorptionProperty, new DynamicResourceExtension(GlassBorder.RimAbsorptionKey)),
                new Setter(GlassBorder.RimLightenProperty, new DynamicResourceExtension(GlassBorder.RimLightenKey)),
                new Setter(GlassBorder.RimSpreadWhiteProperty, new DynamicResourceExtension(GlassBorder.RimSpreadWhiteKey)),
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
            Modules.ModulesThemes.ApplyDensity(Resources, value);
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

    /// <summary>
    /// The app-wide default for <see cref="Modules.StandardsWindow.HighRefreshRate"/>: whether
    /// windows' continuous animation (<see cref="Modules.StandardsWindow.FrameTick"/>) runs at the
    /// display rate (true) or at 30 ticks per second (false, the default). A value set on a window wins.
    /// </summary>
    public bool HighRefreshRate
    {
        get => _highRefreshRate;
        set => SetAndRaise(HighRefreshRateProperty, ref _highRefreshRate, value);
    }

    internal bool IsMotionReduced => _isMotionReduced;

    private void UpdateMotion()
    {
        var reduced = _reduceMotion ?? _os.ReduceMotion;
        SetAndRaise(IsMotionReducedProperty, ref _isMotionReduced, reduced);
        Resources["MotionFast"] = reduced ? TimeSpan.Zero : MotionFast;
        Resources["MotionNormal"] = reduced ? TimeSpan.Zero : MotionNormal;
        Resources["MotionSlow"] = reduced ? TimeSpan.Zero : MotionSlow;

        // Control transitions are fixed objects (as Fluent declares them), replaced here when motion changes.
        Blocks.BlocksThemes.ApplyMotion(Resources, reduced ? TimeSpan.Zero : MotionFast, reduced ? TimeSpan.Zero : MotionNormal,
            reduced ? TimeSpan.Zero : MotionSlow, MotionEasing);
        Modules.ModulesThemes.ApplyMotion(Resources, reduced ? TimeSpan.Zero : MotionNormal, MotionEasing);
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
        r[GlassBorder.RimAbsorptionKey] = GlassBorder.DefaultRimAbsorption;
        r.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary
        {
            ["GlassTintColor"] = GlassBorder.DefaultTintLight,
            ["GlassTintOpacity"] = GlassBorder.DefaultTintOpacity,
            ["GlassBorderBrush"] = new SolidColorBrush(GlassBorder.DefaultBorderLight),
            [GlassBorder.RimLightenKey] = GlassBorder.DefaultRimLightenLight,
            [GlassBorder.RimSpreadWhiteKey] = GlassBorder.DefaultRimSpreadWhiteLight,
        };
        r.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
        {
            ["GlassTintColor"] = GlassBorder.DefaultTintDark,
            ["GlassTintOpacity"] = GlassBorder.DefaultTintOpacity,
            ["GlassBorderBrush"] = new SolidColorBrush(GlassBorder.DefaultBorderDark),
            [GlassBorder.RimLightenKey] = GlassBorder.DefaultRimLightenDark,
            [GlassBorder.RimSpreadWhiteKey] = GlassBorder.DefaultRimSpreadWhiteDark,
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

    /// <summary>How much accent the backdrop wash's start colour mixes into the page colour, per theme.</summary>
    internal const double WashAccentLight = 0.10;
    internal const double WashAccentDark = 0.14;

    /// <summary>
    /// Per theme: the backdrop wash (<c>BackdropWash</c>, a gradient from <c>BackdropWashStartColor</c>,
    /// the page colour with a little accent mixed in, at the top left, to <c>BackdropWashEndColor</c>,
    /// the page colour, at the bottom right), and <c>AccentTextColor</c> / <c>AccentTextColorPressed</c>,
    /// accent-hued text that meets WCAG AA on the page, the card, the wash's two extremes, and the
    /// glass panel's tint over those extremes (see <see cref="AccessibleColors"/>). The palette's
    /// secondary text (<c>BaseMedium</c>) is stepped to AA on the same backgrounds.
    /// </summary>
    private void AddAccentText(Color brand)
    {
        SetWashAndText(ThemeVariant.Light, _light, brand, WashAccentLight, GlassBorder.DefaultTintLight, SecondaryTextLight, onDark: false);
        SetWashAndText(ThemeVariant.Dark, _dark, brand, WashAccentDark, GlassBorder.DefaultTintDark, SecondaryTextDark, onDark: true);
    }

    // The palette's secondary-text grey (Fluent's BaseMedium) before the AA step.
    private static readonly Color SecondaryTextLight = Color.FromRgb(0x73, 0x73, 0x73);
    private static readonly Color SecondaryTextDark = Color.FromRgb(0x9E, 0x9E, 0x9E);

    private void SetWashAndText(ThemeVariant variant, ColorPaletteResources palette, Color brand, double washAccent, Color glassTint,
        Color secondaryText, bool onDark)
    {
        var start = Mix(palette.RegionColor, brand, washAccent);
        var end = palette.RegionColor;
        Color UnderGlass(Color backdrop) => Mix(backdrop, glassTint, GlassBorder.DefaultTintOpacity);
        Color[] backgrounds = [palette.RegionColor, palette.ChromeMediumLow, start, end, UnderGlass(start), UnderGlass(end)];
        var text = AccessibleColors.ForText(brand, backgrounds, onDark);

        // Secondary text (BaseMedium) gets the same AA step, from the palette grey; disabled text
        // uses BaseMediumLow and is left as it is.
        palette.BaseMedium = AccessibleColors.ForText(secondaryText, backgrounds, onDark).Normal;

        var dictionary = (IResourceDictionary)Resources.ThemeDictionaries[variant];
        dictionary["BackdropWashStartColor"] = start;
        dictionary["BackdropWashEndColor"] = end;
        dictionary["BackdropWash"] = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(start, 0), new GradientStop(end, 1) },
        };
        dictionary["AccentTextColor"] = text.Normal;
        dictionary["AccentTextColorPressed"] = text.Pressed;

        // Status text and icon colours (notification cards): the same AA step on the same backgrounds.
        dictionary["SuccessTextColor"] = AccessibleColors.ForText(Success, backgrounds, onDark).Normal;
        dictionary["WarningTextColor"] = AccessibleColors.ForText(Warning, backgrounds, onDark).Normal;
        dictionary["DangerTextColor"] = AccessibleColors.ForText(Danger, backgrounds, onDark).Normal;
    }

    /// <summary>Colour <paramref name="a"/> with <paramref name="amount"/> of <paramref name="b"/> mixed in.</summary>
    internal static Color Mix(Color a, Color b, double amount)
    {
        byte Channel(byte x, byte y) => (byte)Math.Round(x + (y - x) * amount);
        return Color.FromRgb(Channel(a.R, b.R), Channel(a.G, b.G), Channel(a.B, b.B));
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
        BaseMedium = SecondaryTextLight,
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
        BaseMedium = SecondaryTextDark,
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
