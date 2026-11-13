using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.MarkupExtensions;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The keyboard focus ring of every Block: a 2 px accent ring with a 2 px gap outside the
/// control's squircle. Its corner radius is the control's radius grown by the ring's offset,
/// so the curves stay concentric.
/// </summary>
internal static class FocusRing
{
    private const double Gap = 2;
    private const double Width = 2;
    private const double Offset = Gap + Width;

    public static ITemplate<Control> Template { get; } = new FuncTemplate<Control>(Build);

    private static Control Build()
    {
        var ring = new SquircleBorder
        {
            BorderThickness = new Thickness(Width),
            Margin = new Thickness(-Offset),
            IsHitTestVisible = false,
        };
        // The ring lies outside the control, so the adorner layer must not clip it to the control's bounds.
        AdornerLayer.SetIsClipEnabled(ring, false);
        ring.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("SystemControlHighlightAccentBrush"));

        // Follow the adorned control's corner radius while the ring is shown.
        TemplatedControl? adorned = null;
        void Follow(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TemplatedControl.CornerRadiusProperty)
                ring.CornerRadius = Grow(adorned!.CornerRadius);
        }
        ring.AttachedToVisualTree += (_, _) =>
        {
            adorned = AdornerLayer.GetAdornedElement(ring) as TemplatedControl;
            if (adorned is null)
                return;
            ring.CornerRadius = Grow(adorned.CornerRadius);
            adorned.PropertyChanged += Follow;
        };
        ring.DetachedFromVisualTree += (_, _) =>
        {
            if (adorned is not null)
                adorned.PropertyChanged -= Follow;
            adorned = null;
        };
        return ring;
    }

    private static CornerRadius Grow(CornerRadius r) =>
        new(r.TopLeft + Offset, r.TopRight + Offset, r.BottomRight + Offset, r.BottomLeft + Offset);
}
