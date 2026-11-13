using Avalonia.Media;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Derives colours from a brand colour that meet WCAG AA (4.5:1), by the smallest HSL lightness
/// step that reaches it. The same rule applies to any brand colour, so an app's accent override
/// keeps AA contrast. Fills for white text: the brand colour darkened until white text reaches
/// 4.5:1, with hover and pressed further steps darker. Text on given backgrounds: the brand colour
/// darkened (light backgrounds) or lightened (dark backgrounds) until it reaches 4.5:1 on all of
/// them, with pressed a further step.
/// </summary>
internal static class AccessibleColors
{
    /// <summary>The WCAG AA minimum for normal text.</summary>
    public const double MinContrast = 4.5;

    /// <summary>The HSL lightness drop from rest to hover, and again from hover to pressed.</summary>
    public const double StateStep = 0.04;

    private const double SearchStep = 0.005;

    public static (Color Normal, Color PointerOver, Color Pressed) ForWhiteText(Color brand)
    {
        var hsl = brand.ToHsl();
        var lightness = hsl.L;
        while (ContrastRatio(Colors.White, WithLightness(hsl, lightness)) < MinContrast && lightness > 0)
            lightness = Math.Max(0, lightness - SearchStep);

        return (WithLightness(hsl, lightness),
                WithLightness(hsl, lightness - StateStep),
                WithLightness(hsl, lightness - 2 * StateStep));
    }

    /// <summary>
    /// Text in <paramref name="brand"/>'s hue that reaches 4.5:1 on every one of
    /// <paramref name="backgrounds"/>, stepping lightness away from them (lighter when
    /// <paramref name="onDark"/>, darker otherwise); pressed is one more state step that way.
    /// </summary>
    public static (Color Normal, Color Pressed) ForText(Color brand, IReadOnlyList<Color> backgrounds, bool onDark)
    {
        var hsl = brand.ToHsl();
        var direction = onDark ? 1 : -1;
        var lightness = hsl.L;
        while (backgrounds.Min(b => ContrastRatio(WithLightness(hsl, lightness), b)) < MinContrast && lightness is > 0 and < 1)
            lightness = Math.Clamp(lightness + direction * SearchStep, 0, 1);

        return (WithLightness(hsl, lightness), WithLightness(hsl, lightness + direction * StateStep));
    }

    /// <summary>The WCAG contrast ratio between two opaque colours.</summary>
    public static double ContrastRatio(Color a, Color b)
    {
        double la = RelativeLuminance(a), lb = RelativeLuminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>The WCAG relative luminance of an opaque colour.</summary>
    public static double RelativeLuminance(Color c) =>
        0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);

    private static double Linear(byte channel)
    {
        var s = channel / 255.0;
        return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }

    private static Color WithLightness(HslColor hsl, double lightness) =>
        HslColor.ToRgb(hsl.H, hsl.S, Math.Clamp(lightness, 0, 1), hsl.A);
}
