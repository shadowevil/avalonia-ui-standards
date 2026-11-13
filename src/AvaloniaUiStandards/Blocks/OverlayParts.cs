using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The parts shared by the overlay Blocks (menus, flyouts, tooltips, notification cards): the
/// name of their surface part, the fades played when an overlay appears or closes itself, and
/// the rule that puts glass overlays in the window's overlay layer.
/// </summary>
internal static class OverlayParts
{
    public const string SurfacePart = "PART_Surface";

    /// <summary>
    /// Set by a theme on a control that Avalonia hosts in a popup it creates itself (ContextMenu,
    /// MenuFlyoutPresenter, FlyoutPresenter): when the control is placed in its popup, before the
    /// popup opens, the popup is told to use the window's overlay layer, so the glass blurs the app
    /// behind it. Popups in the library's own templates set <see cref="Popup.ShouldUseOverlayLayer"/> directly.
    /// </summary>
    internal static readonly AttachedProperty<bool> InOverlayLayerProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("InOverlayLayer", typeof(OverlayParts));

    static OverlayParts() => InOverlayLayerProperty.Changed.AddClassHandler<Control>((control, e) =>
    {
        control.AttachedToLogicalTree -= OnAttachedToLogicalTree;
        if (e.NewValue is true)
        {
            control.AttachedToLogicalTree += OnAttachedToLogicalTree;
            UseOverlayLayer(control);
        }
    });

    private static void OnAttachedToLogicalTree(object? sender, Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e) =>
        UseOverlayLayer((Control)sender!);

    private static void UseOverlayLayer(Control control)
    {
        if (control.Parent is Popup popup)
            popup.ShouldUseOverlayLayer = true;
    }

    // The fades, set by BlocksThemes.ApplyMotion under these non-public keys (null under reduce motion).
    internal static readonly object FadeInFastKey = new();
    internal static readonly object FadeInSlowKey = new();
    internal static readonly object FadeOutSlowKey = new();

    /// <summary>
    /// An opacity fade from 0 to 1 over <paramref name="duration"/>, or null when it is zero
    /// (reduce motion). One fixed object is run on every overlay that appears.
    /// </summary>
    public static Animation? FadeIn(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? null
        : new Animation
        {
            Duration = duration,
            Easing = easing,
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Visual.OpacityProperty, 0d) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Visual.OpacityProperty, 1d) } },
            },
        };

    /// <summary>
    /// An opacity fade from 1 to 0 over <paramref name="duration"/> that holds its end, or null when
    /// it is zero (reduce motion). For overlays that close themselves (notification cards).
    /// </summary>
    public static Animation? FadeOut(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? null
        : new Animation
        {
            Duration = duration,
            Easing = easing,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Visual.OpacityProperty, 1d) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Visual.OpacityProperty, 0d) } },
            },
        };

    /// <summary>
    /// Plays the fade-in under <paramref name="key"/> on <paramref name="control"/> each time it
    /// joins a visual tree, which for a popup's content is each time the popup opens. Popups close
    /// at once, so there is no fade-out.
    /// </summary>
    public static T FadesIn<T>(this T control, object key) where T : Control
    {
        control.AttachedToVisualTree += (_, _) =>
        {
            if (control.TryFindResource(key, out var value) && value is Animation fade)
                _ = fade.RunAsync(control);
        };
        return control;
    }
}
