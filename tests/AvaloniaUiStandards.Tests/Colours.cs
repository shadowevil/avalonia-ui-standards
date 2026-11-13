using Avalonia.Media;

namespace AvaloniaUiStandards.Tests;

/// <summary>Colour helpers for state and contrast assertions.</summary>
internal static class Colours
{
    /// <summary>A colour or solid brush as one colour, with brush opacity folded into alpha.</summary>
    public static Color Effective(object? value) => value switch
    {
        Color c => c,
        ISolidColorBrush b => Color.FromArgb((byte)Math.Round(b.Color.A * b.Opacity), b.Color.R, b.Color.G, b.Color.B),
        _ => throw new InvalidOperationException($"Not a colour: {value}"),
    };

    /// <summary>A colour with alpha composited over an opaque background.</summary>
    public static Color Over(Color top, Color bottom)
    {
        var a = top.A / 255.0;
        byte Mix(byte t, byte b) => (byte)Math.Round(t * a + b * (1 - a));
        return Color.FromRgb(Mix(top.R, bottom.R), Mix(top.G, bottom.G), Mix(top.B, bottom.B));
    }
}
