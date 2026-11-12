using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

public class GlassTests
{
    // The glass sits at (20, 20)–(220, 140) in a 240 × 160 window over sharp stripes.
    private const int Left = 20, Top = 20, Width = 200, Height = 120;

    [AvaloniaFact]
    public void Glass_BlursTheBackdrop()
    {
        using var sharp = Render(g => { g.BlurSigma = 0; g.RefractionStrength = 0; });
        using var blurred = Render(g => { g.BlurSigma = 6; g.RefractionStrength = 0; });

        var y = Top + Height / 2;
        double difference = 0, sharpVariation = 0, blurredVariation = 0;
        for (var x = Left + 40; x < Left + Width - 40; x++)
        {
            difference += Math.Abs(sharp.Luma(x, y) - blurred.Luma(x, y));
            sharpVariation += Math.Abs(sharp.Luma(x + 1, y) - sharp.Luma(x, y));
            blurredVariation += Math.Abs(blurred.Luma(x + 1, y) - blurred.Luma(x, y));
        }

        Assert.True(difference / (Width - 80) > 50, $"Mean difference {difference / (Width - 80):F1} too small.");
        Assert.True(blurredVariation < sharpVariation * 0.2, $"Blurred {blurredVariation:F0} vs sharp {sharpVariation:F0}.");
    }

    [AvaloniaTheory]
    [InlineData(false, true)]
    [InlineData(true, null)]
    public void Glass_FallsBackToSolid_WhenTransparencyIsReduced(bool os, bool? setting)
    {
        var theme = new StandardsTheme(new OsAccessibility.Settings(false, os)) { ReduceTransparency = setting };
        using var frame = Render(_ => { }, theme, tintOpacity: null);

        for (var y = Top + 10; y < Top + Height - 10; y += 3)
            for (var x = Left + 10; x < Left + Width - 10; x += 3)
                Assert.Equal(((byte)255, (byte)255, (byte)255), frame[x, y]);
    }

    [AvaloniaFact]
    public void Refraction_DisplacesNearTheEdge_NotInTheCentre()
    {
        using var flat = Render(g => { g.BlurSigma = 0; g.RefractionStrength = 0; g.ChromaticDispersion = 0; });
        using var refracted = Render(g => { g.BlurSigma = 0; g.ChromaticDispersion = 0; });

        var y = Top + Height / 2;
        var edgeDifferences = 0;
        for (var x = Left + 1; x < Left + 24; x++)
            if (flat[x, y] != refracted[x, y])
                edgeDifferences++;

        for (var cy = Top + 30; cy < Top + Height - 30; cy++)
            for (var cx = Left + 30; cx < Left + Width - 30; cx++)
                Assert.Equal(flat[cx, cy], refracted[cx, cy]);

        Assert.True(edgeDifferences >= 4, $"Only {edgeDifferences} pixels in the rim band changed.");
    }

    [AvaloniaTheory]
    [InlineData(false, null, false)]
    [InlineData(true, null, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    public void ReduceTransparency_OverridesOrFollowsOs(bool os, bool? setting, bool expected)
    {
        var theme = new StandardsTheme(new OsAccessibility.Settings(false, os)) { ReduceTransparency = setting };
        var glass = new GlassBorder();
        var window = new Window { Content = glass };
        window.Styles.Add(theme);
        window.Show();
        Assert.Equal(expected, glass.GetValue(GlassBorder.ReduceTransparencyProperty));

        theme.ReduceTransparency = !expected;
        Assert.Equal(!expected, glass.GetValue(GlassBorder.ReduceTransparencyProperty));
    }

    [AvaloniaFact]
    public void Glass_ChildWithBackground_LeavesTheBorderVisible()
    {
        var glass = new GlassBorder
        {
            Width = Width, Height = Height,
            BorderBrush = Avalonia.Media.Brushes.Blue,
            Child = new Border { Background = Avalonia.Media.Brushes.Red },
        };
        var window = new Window
        {
            Width = 240, Height = 160,
            Content = new Canvas { Children = { Position(glass) } },
        };
        using var frame = Frame.Capture(window);

        Assert.Equal(new Rect(1, 1, Width - 2, Height - 2), glass.Child!.Bounds);
        var rim = frame[Left, Top + Height / 2];
        Assert.True(rim.B > 200 && rim.R < 60, $"Rim pixel {rim} is not the border colour.");
        Assert.Equal(((byte)255, (byte)0, (byte)0), frame[Left + Width / 2, Top + Height / 2]);
    }

    [AvaloniaFact]
    public void Glass_BorderStaysAboveChildOnTheCornerCurve()
    {
        var glass = new GlassBorder
        {
            Width = Width, Height = Height,
            BorderBrush = Avalonia.Media.Brushes.Blue,
            Child = new Border { Background = Avalonia.Media.Brushes.Red },
        };
        var window = new Window
        {
            Width = 240, Height = 160,
            Content = new Canvas { Children = { Position(glass) } },
        };
        using var frame = Frame.Capture(window);

        Assert.True(SquircleTests.CornerCurveShowsBorder(frame, Left, Top, 16),
            "The child covers the border on the corner curve.");
    }

    private static Frame Render(Action<GlassBorder> configure, StandardsTheme? theme = null, double? tintOpacity = 0)
    {
        var glass = new GlassBorder { Width = Width, Height = Height, BorderBrush = null };
        if (tintOpacity is { } opacity)
            glass.TintOpacity = opacity;
        configure(glass);

        var window = new Window
        {
            Width = 240,
            Height = 160,
            RequestedThemeVariant = ThemeVariant.Light,
            Content = new Panel
            {
                Children =
                {
                    Backdrop.Stripes(),
                    new Canvas { Children = { Position(glass) } },
                },
            },
        };
        if (theme is not null)
            window.Styles.Add(theme);
        return Frame.Capture(window);
    }

    private static Control Position(Control c)
    {
        Canvas.SetLeft(c, Left);
        Canvas.SetTop(c, Top);
        return c;
    }
}
