using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

public class SquircleTests
{
    [AvaloniaTheory]
    [InlineData(0.0)]
    [InlineData(0.6)]
    [InlineData(1.0)]
    public void Geometry_BoundsMatchRect(double smoothing)
    {
        var rect = new Rect(10, 20, 200, 100);
        var bounds = SquircleGeometry.Create(rect, new CornerRadius(16), smoothing).Bounds;
        AssertClose(rect, bounds);
    }

    [AvaloniaFact]
    public void Geometry_RadiusLargerThanHalfSide_StaysInsideRect()
    {
        var rect = new Rect(0, 0, 120, 40);
        AssertClose(rect, SquircleGeometry.Create(rect, new CornerRadius(100), 0.6).Bounds);
    }

    [AvaloniaFact]
    public void Geometry_SmoothingZero_MatchesCircularRadius()
    {
        var rect = new Rect(0, 0, 100, 60);
        var squircle = SquircleGeometry.Create(rect, new CornerRadius(16), 0);
        var circular = new RectangleGeometry(rect, 16, 16);
        Assert.Equal(0, CountDifferences(squircle, circular, rect));
    }

    [AvaloniaFact]
    public void Geometry_Smoothing_DiffersFromCircularRadius()
    {
        var rect = new Rect(0, 0, 100, 60);
        var squircle = SquircleGeometry.Create(rect, new CornerRadius(16), 0.6);
        var circular = new RectangleGeometry(rect, 16, 16);
        Assert.True(CountDifferences(squircle, circular, rect) > 20);
    }

    [AvaloniaFact]
    public void SquircleBorder_ClipsToShapeAndUsesThemeSmoothing()
    {
        var border = new SquircleBorder
        {
            Width = 100,
            Height = 60,
            CornerRadius = new CornerRadius(16),
            Background = Brushes.Red,
            Child = new Border { Background = Brushes.Blue },
        };
        var window = new Window { Width = 200, Height = 100, Content = border };
        using var frame = Frame.Capture(window);

        Assert.Equal(0.6, border.Smoothing);
        Assert.NotNull(border.FillGeometry);
        AssertClose(new Rect(0, 0, 100, 60), border.FillGeometry!.Bounds);
        Assert.Same(border.FillGeometry, border.Clip);

        // The child fills the decorator but is clipped at the corner.
        var origin = border.TranslatePoint(default, window)!.Value;
        var corner = frame[(int)origin.X, (int)origin.Y];
        var centre = frame[(int)origin.X + 50, (int)origin.Y + 30];
        Assert.NotEqual(corner, centre);
        Assert.Equal((byte)255, centre.B);
    }

    [AvaloniaFact]
    public void SquircleBorder_BorderStaysAboveChildOnTheCornerCurve()
    {
        var border = new SquircleBorder
        {
            Width = 100,
            Height = 60,
            CornerRadius = new CornerRadius(16),
            BorderBrush = Brushes.Blue,
            BorderThickness = new Thickness(3),
            Child = new Border { Background = Brushes.Red },
        };
        var window = new Window { Width = 200, Height = 100, Content = border };
        using var frame = Frame.Capture(window);

        var origin = border.TranslatePoint(default, window)!.Value;
        Assert.True(CornerCurveShowsBorder(frame, (int)origin.X, (int)origin.Y, 16),
            "The child covers the border on the corner curve.");
    }

    /// <summary>
    /// Walks the 45° diagonal in from a top-left corner and reports whether the blue border shows
    /// on it, i.e. on the corner curve, where the child's rectangle would otherwise cover the border.
    /// The backdrop, the red child and their anti-aliased mixes keep blue equal to green; only the
    /// border, even a 1 px anti-aliased one, lifts blue above green.
    /// </summary>
    internal static bool CornerCurveShowsBorder(Frame frame, int left, int top, int radius)
    {
        for (var i = 0; i < radius; i++)
        {
            var (_, g, b) = frame[left + i, top + i];
            if (b > g + 40)
                return true;
        }
        return false;
    }

    // Compares fill membership on a grid offset from whole and half pixels, so no point lies on an edge.
    private static int CountDifferences(Geometry a, Geometry b, Rect rect)
    {
        var differences = 0;
        for (var y = rect.Y + 0.13; y < rect.Bottom; y += 0.5)
            for (var x = rect.X + 0.17; x < rect.Right; x += 0.5)
                if (a.FillContains(new Point(x, y)) != b.FillContains(new Point(x, y)))
                    differences++;
        return differences;
    }

    private static void AssertClose(Rect expected, Rect actual)
    {
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
        Assert.Equal(expected.Width, actual.Width, 3);
        Assert.Equal(expected.Height, actual.Height, 3);
    }
}
