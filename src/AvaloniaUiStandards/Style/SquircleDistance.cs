using Avalonia;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// The squircle's signed distance in SkSL. Each corner of the figma-squircle construction is
/// symmetric about its diagonal, so half of it, from the straight edge to the diagonal, is passed
/// as a polyline of <see cref="PointsPerCorner"/> points (the cubic sampled in 9 steps, then the
/// arc), close to the exact curve within about 0.05 px. A shader folds a point onto that half and
/// measures to the nearest segment. Points are in corner-local coordinates: u along the edge from
/// the corner of the bounding rectangle, v into the shape.
/// </summary>
internal static class SquircleDistance
{
    internal const int PointsPerCorner = 16;

    /// <summary>The four half corners (top-left, top-right, bottom-right, bottom-left) and their extents P.</summary>
    public static (float[] Points, float[] Extents) Corners(Rect rect, CornerRadius radius, double smoothing)
    {
        var points = new float[8 * PointsPerCorner];
        var extents = new float[4];
        double[] radii = [radius.TopLeft, radius.TopRight, radius.BottomRight, radius.BottomLeft];
        for (var k = 0; k < 4; k++)
        {
            if (radii[k] <= 0 || rect.Width <= 0 || rect.Height <= 0)
                continue;
            var (half, extent) = HalfCorner(rect, radii[k], smoothing);
            Array.Copy(half, 0, points, 2 * PointsPerCorner * k, half.Length);
            extents[k] = (float)extent;
        }
        return (points, extents);
    }

    // The top-right corner of the shape with this radius on every corner, in corner-local coordinates.
    private static (float[] Points, double Extent) HalfCorner(Rect rect, double radius, double smoothing)
    {
        var sink = new Recorder();
        SquircleGeometry.Build(rect, new CornerRadius(radius), smoothing, sink);
        // The outline starts with the top edge, then the top-right corner: a cubic, an arc, a cubic.
        var cubic = sink.Items[1];
        var arc = sink.Items[2];
        Point Local(Point p) => new(rect.Right - p.X, p.Y - rect.Top);
        var b = cubic.Points.Select(Local).ToArray();
        // From past the corner's start along the straight edge, so the edge beyond it counts too.
        var pts = new List<Point> { new(b[0].X + 40, 0) };
        const int steps = 9;
        for (var i = 0; i <= steps; i++)
        {
            var t = i / (double)steps;
            var mt = 1 - t;
            pts.Add(new Point(
                mt * mt * mt * b[0].X + 3 * mt * mt * t * b[1].X + 3 * mt * t * t * b[2].X + t * t * t * b[3].X,
                mt * mt * mt * b[0].Y + 3 * mt * mt * t * b[1].Y + 3 * mt * t * t * b[2].Y + t * t * t * b[3].Y));
        }
        // The arc's centre lies on the diagonal (c, c), at the arc's radius from its start.
        var e1 = Local(arc.Points[0]);
        var r = arc.Radius;
        double sum = e1.X + e1.Y, q = e1.X * e1.X + e1.Y * e1.Y - r * r;
        var c = (2 * sum + Math.Sqrt(Math.Max(0, 4 * sum * sum - 8 * q))) / 4;
        var a0 = Math.Atan2(e1.Y - c, e1.X - c);
        var a1 = Math.Atan2(-1, -1);
        var arcSteps = PointsPerCorner - pts.Count;
        for (var i = 1; i <= arcSteps; i++)
        {
            var a = a0 + (a1 - a0) * i / arcSteps;
            pts.Add(new Point(c + r * Math.Cos(a), c + r * Math.Sin(a)));
        }
        return (pts.SelectMany(p => new[] { (float)p.X, (float)p.Y }).ToArray(), b[0].X);
    }

    /// <summary>
    /// How much shallower, on average, the rim reads its depth than the exact distance: the rim's
    /// look was tuned with depth taken from concentric fills 0.25 px apart, blended by their
    /// antialiasing, which read about a quarter of a device pixel shallow. Kept so the rim keeps that look.
    /// </summary>
    public static double DepthBias(double pixel)
    {
        double sum = 0;
        const int samples = 600;
        for (var i = 0; i < samples; i++)
        {
            var d = 0.3 + i * 0.01;
            double v = 0;
            for (var k = 0; k < 27; k++)
            {
                var t = k * 0.25;
                var cover = Math.Clamp((d - t) / pixel + 0.5, 0, 1);
                v = v * (1 - cover) + t * cover;
            }
            sum += d - v;
        }
        return sum / samples;
    }

    private sealed class Recorder : SquircleGeometry.ISink
    {
        public readonly List<(Point[] Points, double Radius)> Items = [];
        private Point _last;
        public void MoveTo(Point p) => _last = p;
        public void LineTo(Point p) { Items.Add(([_last, p], 0)); _last = p; }
        public void CubicTo(Point c1, Point c2, Point end) { Items.Add(([_last, c1, c2, end], 0)); _last = end; }
        public void ArcTo(Point end, double radius) { Items.Add(([_last, end], radius)); _last = end; }
        public void Close() { }
    }

    /// <summary>
    /// SkSL: <c>float3 sdf(float2 coord)</c> returns the signed distance (positive inside) and the
    /// outward unit normal, in the shape's local coordinates (0, 0 at its top-left).
    /// Uniforms: <c>size</c>, <c>cornerP</c> (the extents, top-left first, clockwise), <c>pts</c>.
    /// </summary>
    public const string Code = """
        uniform float2 size;
        uniform float4 cornerP;
        uniform float2 pts[64];

        float3 sdf(float2 coord) {
            bool left = coord.x < size.x * 0.5;
            bool top = coord.y < size.y * 0.5;
            float u = left ? coord.x : size.x - coord.x;
            float v = top ? coord.y : size.y - coord.y;
            int k = left ? (top ? 0 : 3) : (top ? 1 : 2);
            float p = k == 0 ? cornerP.x : k == 1 ? cornerP.y : k == 2 ? cornerP.z : cornerP.w;
            float d;
            float2 nl;
            if (u < p && v < p) {
                bool flip = v > u;
                float2 q = flip ? float2(v, u) : float2(u, v);
                float best = 1e9;
                float sgn = 1.0;
                float2 nearest = q;
                float2 seg = float2(-1.0, 0.0);
                for (int c = 0; c < 4; c++) {
                    if (c == k) {
                        for (int i = 0; i < 15; i++) {
                            float2 a = pts[c * 16 + i];
                            float2 ab = pts[c * 16 + i + 1] - a;
                            float2 aq = q - a;
                            float h = clamp(dot(aq, ab) / dot(ab, ab), 0.0, 1.0);
                            float2 near = a + ab * h;
                            float dist = length(q - near);
                            if (dist < best) {
                                best = dist;
                                nearest = near;
                                sgn = (ab.x * aq.y - ab.y * aq.x) < 0.0 ? 1.0 : -1.0;
                                seg = ab;
                            }
                        }
                    }
                }
                d = best * sgn;
                float2 n = best > 0.2 ? (nearest - q) / best * sgn : -normalize(float2(seg.y, -seg.x));
                nl = flip ? n.yx : n;
            } else if (u < v) {
                d = u; nl = float2(-1.0, 0.0);
            } else {
                d = v; nl = float2(0.0, -1.0);
            }
            return float3(d, left ? nl.x : -nl.x, top ? nl.y : -nl.y);
        }
        """;
}
