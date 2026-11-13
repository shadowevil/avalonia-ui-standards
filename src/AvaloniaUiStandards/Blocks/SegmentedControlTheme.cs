using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The <see cref="SegmentedControl"/> and <see cref="SegmentedControlItem"/> themes. The control
/// is a squircle track in Button's Standard fill, holding equal-width segments inset by
/// <see cref="Inset"/>. One neutral pill, raised from the track (white in light, a lighter grey
/// in dark), sits under the selected segment and slides between segments. Segments are
/// transparent, with Button's hover and pressed fills when not selected; their text colour
/// cross-fades with the pill's slide. The focus ring is drawn around the whole control.
/// </summary>
/// <remarks>
/// Template: <c>SquircleBorder#PART_Track</c> around a <see cref="SelectionIndicator"/> holding
/// <c>SquircleBorder#PART_Pill</c> under <c>ItemsPresenter#PART_ItemsPresenter</c> (a one-row
/// <see cref="UniformGrid"/>). Segments: <c>SquircleBorder#PART_Chrome</c> around
/// <c>ContentPresenter#PART_ContentPresenter</c>.
/// </remarks>
internal static class SegmentedControlTheme
{
    public const string TrackPart = "PART_Track";
    public const string PillPart = "PART_Pill";

    /// <summary>The gap between the track's edge and the segments.</summary>
    public const double Inset = 2;

    /// <summary>The pill per theme: the palette's AltHigh (light) and BaseMediumLow (dark), raised from the Button fill of the track.</summary>
    internal static readonly Color PillLight = Color.Parse("#FFFFFF"), PillDark = Color.Parse("#6B6B6B");

    /// <summary>
    /// The pill of a disabled control per theme: the palette's AltHigh (light) and ChromeHigh
    /// (dark), so the disabled text (BaseMediumLow) stays visible on it in dark.
    /// </summary>
    internal static readonly Color PillDisabledLight = Color.Parse("#FFFFFF"), PillDisabledDark = Color.Parse("#525252");

    /// <summary>The selected segment's text per theme: the palette's BaseHigh, as on the track.</summary>
    internal static readonly Color SelectedTextLight = Color.Parse("#1A1A1A"), SelectedTextDark = Color.Parse("#F2F2F2");

    // Per-theme colours, the inner radius, density values and the text fade, under non-public keys.
    internal static readonly object PillKey = new();
    internal static readonly object PillDisabledKey = new();
    internal static readonly object SelectedTextKey = new();
    internal static readonly object InnerRadiusKey = new();
    internal static readonly object ItemMinHeightKey = new();
    internal static readonly object ItemPaddingKey = new();
    internal static readonly object TextTransitionsKey = new();

    /// <summary>The segment height (the control's density height less the inset) and side padding, at Normal and Compact.</summary>
    public static (double MinHeight, Thickness Padding) Item((double MinHeight, Thickness Padding) density) =>
        (density.MinHeight - 2 * Inset, new Thickness(density.Padding.Left, 0));

    public static ControlTheme Control()
    {
        var theme = new ControlTheme(typeof(SegmentedControl))
        {
            Setters =
            {
                new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(Inset)),
                new Setter(TemplatedControl.BackgroundProperty, Resource("ButtonBackground")),
                new Setter(TemplatedControl.BorderBrushProperty, Resource("ButtonBorderBrush")),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
                new Setter(TemplatedControl.ForegroundProperty, Resource("ButtonForeground")),
                new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Left),
                new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center),
                new Setter(ItemsControl.ItemsPanelProperty, new FuncTemplate<Panel?>(() => new UniformGrid { Rows = 1 })),
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<SegmentedControl>(Build)),
            },
        };
        FocusRing.AddWithin(theme);
        theme.Children.Add(SelectionIndicator.Motion());
        theme.Children.Add(new Avalonia.Styling.Style(x => Pill(x.Nesting()))
        {
            Setters = { new Setter(SquircleBorder.BackgroundProperty, new DynamicResourceExtension(PillKey)) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":disabled"))
        {
            Setters = { new Setter(TemplatedControl.BackgroundProperty, Resource("ButtonBackgroundDisabled")) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => Pill(x.Nesting().Class(":disabled")))
        {
            Setters = { new Setter(SquircleBorder.BackgroundProperty, new DynamicResourceExtension(PillDisabledKey)) },
        });
        return theme;
    }

    public static ControlTheme Segment()
    {
        var theme = new ControlTheme(typeof(SegmentedControlItem))
        {
            Setters =
            {
                new Setter(TemplatedControl.CornerRadiusProperty, new DynamicResourceExtension(InnerRadiusKey)),
                new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(ItemPaddingKey)),
                new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(ItemMinHeightKey)),
                new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
                new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
                new Setter(Avalonia.Controls.Control.FocusAdornerProperty, null),
                new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),
                new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ContentControl>(BuildSegment)),
            },
        };
        theme.Children.Add(ChromeMotion());
        theme.Children.Add(new Avalonia.Styling.Style(x => Content(x.Nesting()))
        {
            Setters = { new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(TextTransitionsKey)) },
        });
        theme.Children.Add(State((string?)null, new Look(Brushes.Transparent, Brushes.Transparent, "ButtonForeground")));
        theme.Children.Add(State(":pointerover", new Look("ButtonBackgroundPointerOver", Brushes.Transparent, "ButtonForeground")));
        theme.Children.Add(State(":pressed", new Look("ButtonBackgroundPressed", Brushes.Transparent, "ButtonForeground")));
        theme.Children.Add(State(":disabled", new Look(Brushes.Transparent, Brushes.Transparent, "ButtonForegroundDisabled")));

        // Selected: the pill shows through; the segment adds no fill of its own.
        var selected = new Look(Brushes.Transparent, Brushes.Transparent, new DynamicResourceExtension(SelectedTextKey));
        theme.Children.Add(Group(x => x.Nesting().Class(":selected"), selected, selected, selected,
            new Look(Brushes.Transparent, Brushes.Transparent, "ButtonForegroundDisabled")));
        return theme;
    }

    public static void AddResources(IResourceDictionary resources)
    {
        // Concentric with the track: its corner radius less the inset.
        var outer = resources.TryGetResource("CornerRadiusControl", null, out var value) && value is CornerRadius r ? r : new CornerRadius(8);
        resources[InnerRadiusKey] = new CornerRadius(
            Math.Max(0, outer.TopLeft - Inset), Math.Max(0, outer.TopRight - Inset),
            Math.Max(0, outer.BottomRight - Inset), Math.Max(0, outer.BottomLeft - Inset));

        foreach (var (variant, pill, disabled, text) in new[]
                 {
                     (ThemeVariant.Light, PillLight, PillDisabledLight, SelectedTextLight),
                     (ThemeVariant.Dark, PillDark, PillDisabledDark, SelectedTextDark),
                 })
        {
            var dictionary = (IResourceDictionary)resources.ThemeDictionaries[variant];
            dictionary[PillKey] = new SolidColorBrush(pill);
            dictionary[PillDisabledKey] = new SolidColorBrush(disabled);
            dictionary[SelectedTextKey] = new SolidColorBrush(text);
        }
    }

    private static Selector Pill(Selector state) => state.Template().OfType<SquircleBorder>().Name(PillPart);

    /// <summary>The segments' text colour fade, with the pill's slide, or none when it is zero (reduce motion).</summary>
    public static Transitions TextTransitions(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? []
        : [new CrossFadeBrushTransition { Property = ContentPresenter.ForegroundProperty, Duration = duration, Easing = easing }];

    private static Control Build(SegmentedControl control, INameScope scope)
    {
        var pill = new SquircleBorder { Name = PillPart };
        pill.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension(InnerRadiusKey));

        var items = new ItemsPresenter { Name = "PART_ItemsPresenter" };
        items.Bind(ItemsPresenter.ItemsPanelProperty, new TemplateBinding(ItemsControl.ItemsPanelProperty));

        var indicator = new SelectionIndicator(control, pill, items, (_, rect) => rect);

        var track = new SquircleBorder { Name = TrackPart, Child = indicator };
        track.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        track.Bind(SquircleBorder.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        track.Bind(SquircleBorder.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        track.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        track.Bind(Decorator.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));

        track.RegisterInNameScope(scope);
        pill.RegisterInNameScope(scope);
        items.RegisterInNameScope(scope);
        return FocusRing.Wrap(track, scope);
    }

    private static Control BuildSegment(ContentControl item, INameScope scope)
    {
        var presenter = CreatePresenter();
        var chrome = CreateChrome(presenter);
        chrome.RegisterInNameScope(scope);
        presenter.RegisterInNameScope(scope);
        return chrome;
    }
}
