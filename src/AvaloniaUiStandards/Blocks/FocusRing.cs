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
    private const double Width = 2;
    private const double Offset = Gap + Width;

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

    /// <summary>
    /// Set on a composite field (ComboBox, ButtonSpinner) while one of its parts (an inner TextBox,
    /// a spin button) has keyboard focus, so the field shows the ring around itself.
    /// </summary>
    public static readonly AttachedProperty<bool> IsVisibleWithinProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsVisibleWithin", typeof(FocusRing));

    private static readonly HashSet<Type> Tracked = [];

    /// <summary>
    /// Keeps <see cref="IsVisibleWithinProperty"/> on every <paramref name="type"/> control: true
    /// while the element focused inside it got focus from the keyboard (as <c>:focus-visible</c>),
    /// false once focus leaves it or arrives by pointer. Focus events bubble from the focused part.
    /// </summary>
    public static void TrackWithin(Type type)
    {
        lock (Tracked)
            if (!Tracked.Add(type))
                return;
        // Only the field's own parts count: focus on an item in its drop-down shows that item's ring.
        InputElement.GotFocusEvent.AddClassHandler(type, (sender, e) =>
        {
            if (sender is Control control)
                control.SetValue(IsVisibleWithinProperty,
                    e is FocusChangedEventArgs { NavigationMethod: NavigationMethod.Tab or NavigationMethod.Directional, Source: Visual source }
                    && (source == control || control.IsVisualAncestorOf(source)));
        }, handledEventsToo: true);
        InputElement.LostFocusEvent.AddClassHandler(type, (sender, _) => (sender as Control)?.SetValue(IsVisibleWithinProperty, false),
            handledEventsToo: true);
    }

    /// <summary>
    /// Adds the ring's states to a composite field's theme whose template is wrapped with
    /// <see cref="Wrap"/>: shown on its own keyboard focus or on a part's (<see cref="TrackWithin"/>).
    /// </summary>
    public static void AddWithinTo(ControlTheme theme)
    {
        AddTo(theme);
        theme.Children.Add(new Avalonia.Styling.Style(x => Ring(x.Nesting().PropertyEquals(IsVisibleWithinProperty, true)))
        {
            Setters = { new Setter(Visual.IsVisibleProperty, true) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().PropertyEquals(IsVisibleWithinProperty, true))
        {
            Setters = { new Setter(Visual.ZIndexProperty, 1) },
        });
    }

    private static Selector Ring(Selector state) => state.Template().OfType<SquircleBorder>().Name(Part);
}
