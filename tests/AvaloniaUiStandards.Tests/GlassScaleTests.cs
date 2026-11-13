using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Glass refracts and blurs a downscaled backdrop: it looks the same as at full resolution.</summary>
public class GlassScaleTests
{
    // Over the colourful backdrop with refraction and dispersion on, against the full-resolution
    // render: a mean difference under 1.5 levels, no pixel more than 10 levels off, and under 2 %
    // of pixels more than 4 levels off (the differences are low and spread evenly over the blur).
    [AvaloniaTheory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void DownscaledBackdrop_LooksLikeFullResolution(double renderScale)
    {
        using var full = Render(1, renderScale);
        using var scaled = Render(GlassBorder.DefaultBackdropScale, renderScale);
        double sum = 0;
        int max = 0, over4 = 0;
        for (var y = 0; y < full.Height; y++)
            for (var x = 0; x < full.Width; x++)
            {
                var (r0, g0, b0) = full[x, y];
                var (r1, g1, b1) = scaled[x, y];
                var d = Math.Max(Math.Abs(r0 - r1), Math.Max(Math.Abs(g0 - g1), Math.Abs(b0 - b1)));
                sum += d;
                max = Math.Max(max, d);
                if (d > 4)
                    over4++;
            }
        var pixels = full.Width * full.Height;
        Assert.True(sum / pixels < 1.5, $"mean difference {sum / pixels:F2}");
        Assert.True(max <= 10, $"max difference {max}");
        Assert.True(over4 < pixels * 0.02, $"{100.0 * over4 / pixels:F2} % of pixels more than 4 off");
    }

    private static Frame Render(double backdropScale, double renderScale)
    {
        var glass = new GlassBorder { Width = 300, Height = 200, RimAbsorption = 0 };
        glass.SetValue(GlassBorder.BackdropScaleProperty, backdropScale);
        Canvas.SetLeft(glass, 40);
        Canvas.SetTop(glass, 40);
        var window = new Window { Width = 380, Height = 280, Content = new Panel { Children = { Backdrop.Colourful(), new Canvas { Children = { glass } } } } };
        window.SetRenderScaling(renderScale);
        var frame = Frame.Capture(window);
        window.Close();
        return frame;
    }
}
