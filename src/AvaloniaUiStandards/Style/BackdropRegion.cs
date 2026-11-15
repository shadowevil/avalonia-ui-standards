using Avalonia;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// An immutable region made of rectangles that do not overlap: the part of a window's backdrop that can be seen or is
/// read by glass (<c>StandardsWindow.BackdropVisibleRegion</c>).
/// </summary>
public sealed class BackdropRegion : IEquatable<BackdropRegion>
{
    /// <summary>The empty region.</summary>
    public static BackdropRegion Empty { get; } = new([]);

    private readonly Rect[] _rects;

    private BackdropRegion(Rect[] rects)
    {
        _rects = rects;
        var bounds = default(Rect);
        foreach (var r in rects)
            bounds = bounds.Width <= 0 || bounds.Height <= 0 ? r : bounds.Union(r);
        Bounds = bounds;
    }

    /// <summary>The region's rectangles; they do not overlap.</summary>
    public IReadOnlyList<Rect> Rects => _rects;

    /// <summary>The smallest rectangle that holds the region (empty when the region is).</summary>
    public Rect Bounds { get; }

    /// <summary>Whether the region holds no area.</summary>
    public bool IsEmpty => _rects.Length == 0;

    /// <summary>Whether <paramref name="rect"/> overlaps the region.</summary>
    public bool Intersects(Rect rect)
    {
        foreach (var r in _rects)
        {
            var i = r.Intersect(rect);
            if (i.Width > 0 && i.Height > 0)
                return true;
        }
        return false;
    }

    /// <summary>Whether <paramref name="point"/> lies in the region.</summary>
    public bool Contains(Point point)
    {
        foreach (var r in _rects)
            if (r.Contains(point))
                return true;
        return false;
    }

    /// <summary><paramref name="area"/> less every rectangle in <paramref name="holes"/>.</summary>
    internal static BackdropRegion Create(Rect area, IEnumerable<Rect> holes)
    {
        if (area.Width <= 0 || area.Height <= 0)
            return Empty;
        return new BackdropRegion(RectSet.Subtract([area], holes).ToArray());
    }

    public bool Equals(BackdropRegion? other) =>
        other is not null && (ReferenceEquals(this, other) || _rects.AsSpan().SequenceEqual(other._rects));

    public override bool Equals(object? obj) => Equals(obj as BackdropRegion);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var r in _rects)
            hash.Add(r);
        return hash.ToHashCode();
    }

    public override string ToString() => IsEmpty ? "Empty" : string.Join("; ", _rects);
}

/// <summary>Rectangle-list arithmetic for regions.</summary>
internal static class RectSet
{
    /// <summary>Each rectangle of <paramref name="area"/> less every hole, as rectangles that do not overlap (when the area's do not).</summary>
    public static List<Rect> Subtract(IEnumerable<Rect> area, IEnumerable<Rect> holes)
    {
        var result = area.Where(NotEmpty).ToList();
        foreach (var hole in holes)
        {
            if (!NotEmpty(hole))
                continue;
            var next = new List<Rect>(result.Count + 4);
            foreach (var r in result)
                Subtract(r, hole, next);
            result = next;
        }
        return result;
    }

    // a less b, as up to four rectangles: the bands above and below b, then the parts left and right of it.
    private static void Subtract(Rect a, Rect b, List<Rect> into)
    {
        var i = a.Intersect(b);
        if (!NotEmpty(i))
        {
            into.Add(a);
            return;
        }
        if (i.Top > a.Top)
            into.Add(new Rect(a.Left, a.Top, a.Width, i.Top - a.Top));
        if (i.Bottom < a.Bottom)
            into.Add(new Rect(a.Left, i.Bottom, a.Width, a.Bottom - i.Bottom));
        if (i.Left > a.Left)
            into.Add(new Rect(a.Left, i.Top, i.Left - a.Left, i.Height));
        if (i.Right < a.Right)
            into.Add(new Rect(i.Right, i.Top, a.Right - i.Right, i.Height));
    }

    /// <summary>The rectangles of <paramref name="rects"/> intersected with <paramref name="clip"/>, dropping empty ones.</summary>
    public static List<Rect> Intersect(IEnumerable<Rect> rects, IReadOnlyList<Rect> clip)
    {
        var result = new List<Rect>();
        foreach (var r in rects)
        {
            // A rectangle inside one clip rectangle stays whole; otherwise it is cut by each.
            if (clip.Any(c => c.Contains(r)))
            {
                result.Add(r);
                continue;
            }
            foreach (var c in clip)
            {
                var i = r.Intersect(c);
                if (NotEmpty(i))
                    result.Add(i);
            }
        }
        return result;
    }

    /// <summary>
    /// The part of a rectangle with corners of radius <paramref name="corner"/> that is surely filled: the rectangle
    /// without its corner squares, as two rectangles (one when there is no corner).
    /// </summary>
    public static Rect[] Cross(Rect rect, double corner)
    {
        if (corner <= 0)
            return NotEmpty(rect) ? [rect] : [];
        corner = Math.Min(corner, Math.Min(rect.Width, rect.Height) / 2);
        Rect tall = new(rect.X + corner, rect.Y, rect.Width - 2 * corner, rect.Height);
        Rect wide = new(rect.X, rect.Y + corner, rect.Width, rect.Height - 2 * corner);
        return new[] { tall, wide }.Where(NotEmpty).ToArray();
    }

    public static bool NotEmpty(Rect r) => r.Width > 0 && r.Height > 0;
}
