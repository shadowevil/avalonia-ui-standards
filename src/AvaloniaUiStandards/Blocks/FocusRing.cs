using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The keyboard focus ring of every Block: a 2 px accent ring with a 2 px gap outside the
/// control's squircle, its corner radius the control's grown by the ring's offset so the curves
/// stay concentric.
/// </summary>
/// <remarks>
/// The ring is part of the control's template, drawn just outside the control and shown on
/// <c>:focus-visible</c>, rather than in the window's adorner layer. It is therefore clipped by
/// the control's clipping ancestors (scroll viewers, the glass panel, the window frame) exactly
/// as the control is, including while scrolling. The focused control is raised above its siblings
/// so neighbours do not paint over the ring.
/// </remarks>
internal static class FocusRing
{
    public const string Part = "PART_FocusRing";

    private const double Gap = 2;
    internal const double Width = 2;
    internal const double Offset = Gap + Width;

    private static readonly IValueConverter Grow = new FuncValueConverter<CornerRadius, CornerRadius>(r =>
        new CornerRadius(r.TopLeft + Offset, r.TopRight + Offset, r.BottomRight + Offset, r.BottomLeft + Offset));

    /// <summary>Puts <paramref name="root"/> and the ring together as a template root.</summary>
    public static Control Wrap(Control root, INameScope scope)
    {
        var ring = new SquircleBorder
        {
            Name = Part,
            BorderThickness = new Thickness(Width),
            Margin = new Thickness(-Offset),
            IsHitTestVisible = false,
        };
        ring.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("SystemControlHighlightAccentBrush"));
        ring.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty) { Converter = Grow });
        ring.RegisterInNameScope(scope);
        return new Panel { Children = { root, ring } };
    }

    /// <summary>Adds the ring's states to a theme whose template is wrapped with <see cref="Wrap"/>.</summary>
    public static void AddTo(ControlTheme theme)
    {
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        // Button clips to its bounds by default; the ring lies outside them. The chrome still clips
        // the content to the squircle, so only the ring draws outside the control.
        theme.Setters.Add(new Setter(Visual.ClipToBoundsProperty, false));
        theme.Children.Add(new Avalonia.Styling.Style(x => Ring(x.Nesting())) { Setters = { new Setter(Visual.IsVisibleProperty, false) } });
        theme.Children.Add(new Avalonia.Styling.Style(x => Ring(x.Nesting().Class(":focus-visible"))) { Setters = { new Setter(Visual.IsVisibleProperty, true) } });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":focus-visible")) { Setters = { new Setter(Visual.ZIndexProperty, 1) } });
    }

    /// <summary>The pseudo-class a control gets while a control inside it shows keyboard focus (<see cref="AddWithin"/>).</summary>
    public const string FocusVisibleWithin = ":focus-visible-within";

    /// <summary>
    /// Adds the ring's states to a theme whose template is wrapped with <see cref="Wrap"/>, for a
    /// control whose focus lands on a part or item inside it (a segment, a header): the ring is
    /// drawn around the whole control while the focused element inside it shows keyboard focus.
    /// </summary>
    /// <param name="counts">
    /// Which focused elements inside the control light the ring (the control, then the focused
    /// element); null counts every element inside it. A control that holds other focusable
    /// content (an Expander's content) counts only its own part.
    /// </param>
    public static void AddWithin(ControlTheme theme, Func<Control, Control, bool>? counts = null)
    {
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(Visual.ClipToBoundsProperty, false));
        theme.Setters.Add(new Setter(TracksFocusWithinProperty, counts ?? AnyWithin));
        theme.Children.Add(new Avalonia.Styling.Style(x => Ring(x.Nesting())) { Setters = { new Setter(Visual.IsVisibleProperty, false) } });
        theme.Children.Add(new Avalonia.Styling.Style(x => Ring(x.Nesting().Class(FocusVisibleWithin))) { Setters = { new Setter(Visual.IsVisibleProperty, true) } });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(FocusVisibleWithin)) { Setters = { new Setter(Visual.ZIndexProperty, 1) } });
    }

    private static readonly Func<Control, Control, bool> AnyWithin = (_, _) => true;

    /// <summary>
    /// Set by <see cref="AddWithin"/>: which focused elements inside the control it tracks as
    /// <see cref="FocusVisibleWithin"/>; null tracks none.
    /// </summary>
    private static readonly AttachedProperty<Func<Control, Control, bool>?> TracksFocusWithinProperty =
        AvaloniaProperty.RegisterAttached<Control, Func<Control, Control, bool>?>("TracksFocusWithin", typeof(FocusRing));

    static FocusRing()
    {
        TracksFocusWithinProperty.Changed.AddClassHandler<Control>((control, e) =>
        {
            control.RemoveHandler(InputElement.GotFocusEvent, OnFocusChanged);
            control.RemoveHandler(InputElement.LostFocusEvent, OnFocusChanged);
            if (e.NewValue is not null)
            {
                control.AddHandler(InputElement.GotFocusEvent, OnFocusChanged, handledEventsToo: true);
                control.AddHandler(InputElement.LostFocusEvent, OnFocusChanged, handledEventsToo: true);
            }
            else
            {
                ((IPseudoClasses)control.Classes).Set(FocusVisibleWithin, false);
            }
        });
    }

    // Focus events bubble from the focused element after its own handler has set :focus-visible.
    // The ring shows while the newly focused element is inside the control and shows keyboard focus.
    private static void OnFocusChanged(object? sender, FocusChangedEventArgs e)
    {
        var control = (Control)sender!;
        var focused = e.NewFocusedElement as Control;
        var within = focused is not null && (focused == control || control.IsVisualAncestorOf(focused));
        if (e.RoutedEvent == InputElement.LostFocusEvent && within)
            return; // the GotFocus of the element inside decides
        var counts = control.GetValue(TracksFocusWithinProperty) ?? AnyWithin;
        ((IPseudoClasses)control.Classes).Set(FocusVisibleWithin,
            within && focused!.Classes.Contains(":focus-visible") && counts(control, focused));
    }

    private static Selector Ring(Selector state) => state.Template().OfType<SquircleBorder>().Name(Part);
}
