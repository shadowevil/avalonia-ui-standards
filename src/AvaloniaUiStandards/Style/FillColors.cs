using Avalonia.Media;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Derives the fills of a filled control (white text on a brand colour) that meet WCAG AA:
/// rest is the brand colour, darkened by the smallest lightness step that gives white text
/// at least 4.5:1; hover and pressed are further lightness steps darker. The same rule applies
/// to any brand colour, so an app's accent override keeps AA contrast.
/// </summary>
internal static class FillColors
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
