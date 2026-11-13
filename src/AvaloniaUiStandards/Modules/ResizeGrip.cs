using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// A resize band or corner of the window frame. It is hit only where <see cref="IsExcluded"/>
/// does not claim the point, so controls it overlaps (the window buttons) stay clickable.
/// </summary>
internal sealed class ResizeGrip : Border, ICustomHitTest
{
    /// <summary>Points (in the grip's coordinates) the grip yields to other controls.</summary>
    public Func<Point, bool>? IsExcluded { get; set; }

    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point) && IsExcluded?.Invoke(point) != true;
}
