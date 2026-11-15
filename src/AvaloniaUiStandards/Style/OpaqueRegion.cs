using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Marks content that paints its whole rectangle opaquely, so glass under it and the window's backdrop skip the work
/// it hides. Set it once; the region follows the control as it moves, resizes, scrolls, is clipped, hidden or faded.
/// </summary>
/// <remarks>
/// A marked control counts as opaque while it is effectively visible, it and its ancestors are at full opacity with no
/// opacity mask or effect, and its transform to the window keeps it axis-aligned. Its rounded corners (a
/// <see cref="CornerRadius"/> on a border or templated control) and the clips of its ancestors are left out.
/// A glass surface skips the opaque regions of its own content; <c>StandardsWindow</c> clips its <c>Backdrop</c> to
/// the rest of the window.
/// </remarks>
public static class OpaqueRegion
{
    public static readonly AttachedProperty<bool> IsOpaqueProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsOpaque", typeof(OpaqueRegion));

    static OpaqueRegion()
    {
        IsOpaqueProperty.Changed.AddClassHandler<Control>((control, e) =>
        {
            if (e.NewValue is true)
            {
                control.AttachedToVisualTree += OnAttached;
                control.DetachedFromVisualTree += OnDetached;
                BackdropRegions.AddSource(control);
            }
            else
            {
                control.AttachedToVisualTree -= OnAttached;
                control.DetachedFromVisualTree -= OnDetached;
                BackdropRegions.RemoveSource(control);
            }
        });
    }

    /// <summary>Whether <paramref name="control"/> paints its whole rectangle opaquely.</summary>
    public static bool GetIsOpaque(Control control) => control.GetValue(IsOpaqueProperty);

    /// <summary>Marks <paramref name="control"/> as painting its whole rectangle opaquely, or not.</summary>
    public static void SetIsOpaque(Control control, bool value) => control.SetValue(IsOpaqueProperty, value);

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => BackdropRegions.AddSource((Visual)sender!);

    private static void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => BackdropRegions.RemoveSource((Visual)sender!, e.RootVisual);
}
