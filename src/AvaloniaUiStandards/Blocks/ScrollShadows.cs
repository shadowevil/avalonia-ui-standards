using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The scroll shadows every ScrollViewer shows along the edges that have content out of view.
/// Turn them off per scroller with <c>ScrollShadows.IsEnabled="False"</c> on the ScrollViewer, or
/// on a control that hosts one and passes it on (ListBox).
/// </summary>
public static class ScrollShadows
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(ScrollShadows), defaultValue: true);

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);
}
