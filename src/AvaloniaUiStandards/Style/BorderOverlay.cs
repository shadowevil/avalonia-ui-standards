using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// A visual-only layer that a decorator keeps above its child, so the border is drawn over
/// the child where the corners curve inward. It is a <see cref="Layoutable"/>, not a control,
/// so it is not an input element (never hit) and has no automation peer (invisible to
/// screen readers). It is not a logical child.
/// </summary>
internal sealed class BorderOverlay(Action<DrawingContext> draw) : Layoutable
{
    public override void Render(DrawingContext context) => draw(context);

    /// <summary>Adds the overlay to <paramref name="visualChildren"/>, or moves it back on top.</summary>
    public void MoveToTop(IList<Visual> visualChildren)
    {
        visualChildren.Remove(this);
        visualChildren.Add(this);
    }
}
