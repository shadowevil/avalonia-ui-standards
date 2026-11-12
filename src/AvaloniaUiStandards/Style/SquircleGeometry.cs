using Avalonia;
using Avalonia.Media;
using SkiaSharp;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Builds continuous-corner (squircle) outlines with the figma-squircle construction:
/// each corner is a circular arc flanked by two cubic Béziers, and the smoothing value
/// (0 to 1) moves length from the arc into the Béziers. Smoothing 0 is a plain circular corner.
/// </summary>
internal static class SquircleGeometry
{
    internal interface ISink
    {
        void MoveTo(Point p);
        void LineTo(Point p);
        void CubicTo(Point c1, Point c2, Point end);
        void ArcTo(Point end, double radius);
        void Close();
    }

    private readonly record struct Corner(double A, double B, double C, double D, double P, double Arc, double R);

    /// <summary>Creates the outline of <paramref name="rect"/> as an Avalonia geometry.</summary>
    public static StreamGeometry Create(Rect rect, CornerRadius radius, double smoothing)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.SetFillRule(FillRule.NonZero);
            Build(rect, radius, smoothing, new GeometrySink(context));
        }
        return geometry;
    }

    /// <summary>Creates the outline of <paramref name="rect"/> as a Skia path.</summary>
    public static SKPath CreateSkPath(Rect rect, CornerRadius radius, double smoothing)
    {
        var path = new SKPath();
        Build(rect, radius, smoothing, new SkPathSink(path));
        return path;
    }

    /// <summary>Shrinks a rectangle and its corner radii by <paramref name="inset"/> on every side.</summary>
    public static (Rect Rect, CornerRadius Radius) Inset(Rect rect, CornerRadius radius, double inset) =>
        (rect.Deflate(inset),
         new CornerRadius(
             Math.Max(0, radius.TopLeft - inset),
             Math.Max(0, radius.TopRight - inset),
             Math.Max(0, radius.BottomRight - inset),
             Math.Max(0, radius.BottomLeft - inset)));

    internal static void Build(Rect rect, CornerRadius radius, double smoothing, ISink sink)
    {
        double x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height;
        if (w <= 0 || h <= 0)
            return;

        var budget = Math.Min(w, h) / 2;
        var tl = CornerParams(radius.TopLeft, smoothing, budget);
        var tr = CornerParams(radius.TopRight, smoothing, budget);
        var br = CornerParams(radius.BottomRight, smoothing, budget);
        var bl = CornerParams(radius.BottomLeft, smoothing, budget);

        sink.MoveTo(new Point(x + tl.P, y));

        // Top-right corner, travelling right then down.
        sink.LineTo(new Point(x + w - tr.P, y));
        DrawCorner(sink, tr, new Point(x + w - tr.P, y), new Vector(1, 0), new Vector(0, 1));

        // Bottom-right corner, travelling down then left.
        sink.LineTo(new Point(x + w, y + h - br.P));
        DrawCorner(sink, br, new Point(x + w, y + h - br.P), new Vector(0, 1), new Vector(-1, 0));

        // Bottom-left corner, travelling left then up.
        sink.LineTo(new Point(x + bl.P, y + h));
        DrawCorner(sink, bl, new Point(x + bl.P, y + h), new Vector(-1, 0), new Vector(0, -1));

        // Top-left corner, travelling up then right.
        sink.LineTo(new Point(x, y + tl.P));
        DrawCorner(sink, tl, new Point(x, y + tl.P), new Vector(0, -1), new Vector(1, 0));

        sink.Close();
    }

    // Draws one corner starting at `start`, where `along` is the direction of the edge
    // being left and `turn` the direction of the next edge.
    private static void DrawCorner(ISink sink, Corner k, Point start, Vector along, Vector turn)
    {
        if (k.R <= 0)
            return;

        var e1 = start + along * (k.A + k.B + k.C) + turn * k.D;
        sink.CubicTo(start + along * k.A, start + along * (k.A + k.B), e1);

        var e2 = e1 + along * k.Arc + turn * k.Arc;
        sink.ArcTo(e2, k.R);

        var e3 = e2 + along * k.D + turn * (k.A + k.B + k.C);
        sink.CubicTo(e2 + along * k.D + turn * k.C, e2 + along * k.D + turn * (k.B + k.C), e3);
    }

    // figma-squircle getPathParamsForCorner, without preserveSmoothing: when the corner
    // does not fit the budget (half the shorter side), smoothing is reduced first.
    private static Corner CornerParams(double radius, double smoothing, double budget)
    {
        if (radius <= 0)
            return default;

        var r = Math.Min(radius, budget);
        var s = Math.Clamp(smoothing, 0, 1);
        s = Math.Max(0, Math.Min(s, budget / r - 1));
        var p = Math.Min((1 + s) * r, budget);

        var arcMeasure = 90 * (1 - s);
        var arc = Math.Sin(ToRadians(arcMeasure / 2)) * r * Math.Sqrt(2);
        var alpha = (90 - arcMeasure) / 2;
        var p3ToP4 = r * Math.Tan(ToRadians(alpha / 2));
        var beta = 45 * s;
        var c = p3ToP4 * Math.Cos(ToRadians(beta));
        var d = c * Math.Tan(ToRadians(beta));
        var b = Math.Max(0, (p - arc - c - d) / 3);
        var a = 2 * b;
        return new Corner(a, b, c, d, p, arc, r);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private sealed class GeometrySink(StreamGeometryContext context) : ISink
    {
        public void MoveTo(Point p) => context.BeginFigure(p, true);
        public void LineTo(Point p) => context.LineTo(p);
        public void CubicTo(Point c1, Point c2, Point end) => context.CubicBezierTo(c1, c2, end);
        public void ArcTo(Point end, double radius) =>
            context.ArcTo(end, new Size(radius, radius), 0, false, SweepDirection.Clockwise);
        public void Close() => context.EndFigure(true);
    }

    private sealed class SkPathSink(SKPath path) : ISink
    {
        public void MoveTo(Point p) => path.MoveTo((float)p.X, (float)p.Y);
        public void LineTo(Point p) => path.LineTo((float)p.X, (float)p.Y);
        public void CubicTo(Point c1, Point c2, Point end) =>
            path.CubicTo((float)c1.X, (float)c1.Y, (float)c2.X, (float)c2.Y, (float)end.X, (float)end.Y);
        public void ArcTo(Point end, double radius) =>
            path.ArcTo((float)radius, (float)radius, 0, SKPathArcSize.Small, SKPathDirection.Clockwise, (float)end.X, (float)end.Y);
        public void Close() => path.Close();
    }
}
