using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The Slider control theme: a 4 px pill track whose value part is the AA accent fill, and a white
/// circular thumb (20 px at Normal, 16 at Compact) with a hairline edge and a soft shadow, which grows
/// 10 % on hover and 20 % while pressed. Ticks are Fluent's, optional, outside the thumb's reach.
/// Horizontal and vertical. The focus ring goes around the thumb.
/// </summary>
internal static class SliderTheme
{
    public const string TrackPart = "PART_Track";
    public const string DecreasePart = "PART_DecreaseButton";
    public const string IncreasePart = "PART_IncreaseButton";

    /// <summary>
    /// Set on the thumb by the slider's styles while the slider shows keyboard focus; the thumb's
    /// template shows its focus ring from it (a theme style cannot reach into the thumb's template).
    /// </summary>
    internal static readonly AttachedProperty<bool> ShowsFocusRingProperty =
        AvaloniaProperty.RegisterAttached<Thumb, bool>("ShowsFocusRing", typeof(SliderTheme));

    /// <summary>How far each track part runs under the thumb (half the Compact thumb), so its round end is hidden.</summary>
    public const double UnderThumb = 8;

    /// <summary>The tick marks' length and their gap to the thumb's row.</summary>
    public const double TickLength = Spacing.XS, TickGap = Spacing.XS;

    /// <summary>The thumb's hairline edge and shadow, in both themes.</summary>
    internal static readonly Color ThumbEdge = Color.Parse("#1F000000");
    internal static readonly BoxShadows ThumbShadow = BoxShadows.Parse("0 1 3 0 #40000000");

    internal static readonly ITransform Rest = TransformOperations.Identity;
    internal static readonly ITransform Hover = TransformOperations.Parse("scale(1.1)");
    internal static readonly ITransform Pressed = TransformOperations.Parse("scale(1.2)");

    // Bare themes for the parts, so no other theme styles them.
    private static readonly ControlTheme HorizontalPart = PartTheme(Orientation.Horizontal);
    private static readonly ControlTheme VerticalPart = PartTheme(Orientation.Vertical);
    private static readonly ControlTheme ThumbTheme = new(typeof(Thumb))
    {
        Setters =
        {
            new Setter(TemplatedControl.CornerRadiusProperty, ButtonParts.Pill),
            // The focus ring lies outside the thumb.
            new Setter(Visual.ClipToBoundsProperty, false),
            new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(ChromeTransitionsKey)),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Thumb>(BuildThumb)),
        },
    };

    public static ControlTheme Create()
    {
        var theme = new ControlTheme(typeof(Slider));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(Visual.ClipToBoundsProperty, false));
        foreach (var (orientation, pseudoClass) in new[] { (Orientation.Horizontal, ":horizontal"), (Orientation.Vertical, ":vertical") })
            theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(pseudoClass))
            {
                Setters =
                {
                    new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Slider>((slider, scope) => Build(slider, scope, orientation))),
                    new Setter(orientation == Orientation.Horizontal ? Layoutable.MinHeightProperty : Layoutable.MinWidthProperty,
                        new DynamicResourceExtension(MinHeightKey)),
                    // Along the track it stretches; across it, it is as thick as its row, centred.
                    orientation == Orientation.Horizontal
                        ? new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center)
                        : new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Center),
                },
            });

        // The track parts run under the thumb on its side; which side depends on orientation and direction.
        foreach (var (pseudoClass, reversed, decrease, increase) in new[]
                 {
                     (":horizontal", false, new Thickness(0, 0, -UnderThumb, 0), new Thickness(-UnderThumb, 0, 0, 0)),
                     (":horizontal", true, new Thickness(-UnderThumb, 0, 0, 0), new Thickness(0, 0, -UnderThumb, 0)),
                     (":vertical", false, new Thickness(0, -UnderThumb, 0, 0), new Thickness(0, 0, 0, -UnderThumb)),
                     (":vertical", true, new Thickness(0, 0, 0, -UnderThumb), new Thickness(0, -UnderThumb, 0, 0)),
                 })
        {
            Selector Direction(Selector? x) => x.Nesting().Class(pseudoClass).PropertyEquals(Slider.IsDirectionReversedProperty, reversed);
            theme.Children.Add(Part(x => Direction(x), DecreasePart, TemplatedControl.PaddingProperty, decrease));
            theme.Children.Add(Part(x => Direction(x), IncreasePart, TemplatedControl.PaddingProperty, increase));
        }

        // Ticks show by placement, as in Fluent.
        foreach (var name in new[] { "TopTickBar", "LeftTickBar", "BottomTickBar", "RightTickBar" })
            theme.Children.Add(Part(x => x.Nesting(), name, Visual.IsVisibleProperty, false));
        foreach (var (placement, names) in new[]
                 {
                     (TickPlacement.TopLeft, new[] { "TopTickBar", "LeftTickBar" }),
                     (TickPlacement.BottomRight, new[] { "BottomTickBar", "RightTickBar" }),
                     (TickPlacement.Outside, new[] { "TopTickBar", "LeftTickBar", "BottomTickBar", "RightTickBar" }),
                 })
            foreach (var name in names)
                theme.Children.Add(Part(x => x.Nesting().PropertyEquals(Slider.TickPlacementProperty, placement), name, Visual.IsVisibleProperty, true));

        AddState(theme, x => x.Nesting(), "AccentFillColor", "SliderTrackFill", Brushes.White, "SliderTickBarFill", Rest);
        AddState(theme, x => x.Nesting().Class(":pointerover"), "AccentFillColorPointerOver", "SliderTrackFillPointerOver", null, null, Hover);
        AddState(theme, x => x.Nesting().Class(":pressed"), "AccentFillColorPressed", "SliderTrackFillPressed", null, null, Pressed);
        AddState(theme, x => x.Nesting().Class(":disabled"), "SystemControlForegroundBaseMediumLowBrush", "SliderTrackFillDisabled",
            "SystemControlBackgroundBaseLowBrush", "SliderTickBarFillDisabled", Rest);
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":error").Template().OfType<Thumb>())
        {
            Setters = { new Setter(TemplatedControl.BackgroundProperty, Resource("SystemControlErrorTextForegroundBrush")) },
        });

        // The focus ring sits in the thumb's template; it shows when the slider has keyboard focus.
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":focus-visible").Template().OfType<Thumb>())
        {
            Setters = { new Setter(ShowsFocusRingProperty, true) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":focus-visible")) { Setters = { new Setter(Visual.ZIndexProperty, 1) } });
        return theme;
    }

    /// <summary>One state: the value and rest track colours, the thumb's fill and scale, the ticks' fill (null keeps the rest value).</summary>
    private static void AddState(ControlTheme theme, Func<Selector?, Selector> state, object value, object rest, object? thumb, object? ticks, ITransform scale)
    {
        var style = new Avalonia.Styling.Style(x => state(x))
        {
            Children =
            {
                Part(x => x.Nesting(), DecreasePart, TemplatedControl.BackgroundProperty, Value(value)),
                Part(x => x.Nesting(), IncreasePart, TemplatedControl.BackgroundProperty, Value(rest)),
                new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<Thumb>())
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, scale) },
                },
            },
        };
        if (thumb is not null)
            style.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<Thumb>())
            {
                Setters = { new Setter(TemplatedControl.BackgroundProperty, Value(thumb)) },
            });
        if (ticks is not null)
            style.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<TickBar>())
            {
                Setters = { new Setter(TickBar.FillProperty, Value(ticks)) },
            });
        theme.Children.Add(style);
    }

    private static Avalonia.Styling.Style Part(Func<Selector?, Selector> state, string name, AvaloniaProperty property, object value) =>
        new(x => state(x).Template().Name(name)) { Setters = { new Setter(property, value) } };

    /// <summary>A track part: a transparent hit area holding a 4 px pill in its background colour, run under the thumb by its padding.</summary>
    private static ControlTheme PartTheme(Orientation orientation) => new(typeof(RepeatButton))
    {
        Setters =
        {
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<RepeatButton>((_, _) =>
            {
                var pill = new SquircleBorder { CornerRadius = ButtonParts.Pill };
                if (orientation == Orientation.Horizontal)
                    pill.Height = RangeParts.TrackThickness;
                else
                    pill.Width = RangeParts.TrackThickness;
                pill.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
                pill.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
                pill.Bind(Animatable.TransitionsProperty, new DynamicResourceExtension(ChromeTransitionsKey));
                return new Panel { Background = Brushes.Transparent, Children = { pill } };
            })),
        },
    };

    private static Control BuildThumb(Thumb thumb, INameScope scope)
    {
        var visual = new Border
        {
            BorderBrush = new SolidColorBrush(ThumbEdge),
            BorderThickness = new Thickness(1),
            BoxShadow = ThumbShadow,
        };
        visual.Bind(Border.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        visual.Bind(Border.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));

        var root = FocusRing.Wrap(visual, scope);
        scope.Find<SquircleBorder>(FocusRing.Part)!.Bind(Visual.IsVisibleProperty, new TemplateBinding(ShowsFocusRingProperty));
        return root;
    }

    private static Control Build(Slider slider, INameScope scope, Orientation orientation)
    {
        var vertical = orientation == Orientation.Vertical;
        var thumb = new Thumb { Theme = ThumbTheme };
        thumb.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(RangeParts.ThumbKey));
        thumb.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(RangeParts.ThumbKey));
        var partTheme = vertical ? VerticalPart : HorizontalPart;
        var decrease = new RepeatButton { Name = DecreasePart, Theme = partTheme, Focusable = false };
        var increase = new RepeatButton { Name = IncreasePart, Theme = partTheme, Focusable = false };

        var track = new Track
        {
            Name = TrackPart,
            Orientation = orientation,
            Thumb = thumb,
            DecreaseButton = decrease,
            IncreaseButton = increase,
        };
        track.Bind(Track.MinimumProperty, new TemplateBinding(RangeBase.MinimumProperty));
        track.Bind(Track.MaximumProperty, new TemplateBinding(RangeBase.MaximumProperty));
        track.Bind(Track.ValueProperty, new TemplateBinding(RangeBase.ValueProperty) { Mode = BindingMode.TwoWay });
        track.Bind(Track.IsDirectionReversedProperty, new TemplateBinding(Slider.IsDirectionReversedProperty));
        track.Bind(vertical ? Layoutable.WidthProperty : Layoutable.HeightProperty, new DynamicResourceExtension(RangeParts.ThumbKey));

        var before = Ticks(vertical ? "LeftTickBar" : "TopTickBar", vertical ? TickBarPlacement.Left : TickBarPlacement.Top, orientation, thumb);
        var after = Ticks(vertical ? "RightTickBar" : "BottomTickBar", vertical ? TickBarPlacement.Right : TickBarPlacement.Bottom, orientation, thumb);
        before.Margin = vertical ? new Thickness(0, 0, TickGap, 0) : new Thickness(0, 0, 0, TickGap);
        after.Margin = vertical ? new Thickness(TickGap, 0, 0, 0) : new Thickness(0, TickGap, 0, 0);

        var grid = new Grid { Background = Brushes.Transparent, Children = { before, track, after } };
        if (vertical)
        {
            grid.ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto");
            grid.HorizontalAlignment = HorizontalAlignment.Center;
            Grid.SetColumn(track, 1);
            Grid.SetColumn(after, 2);
        }
        else
        {
            grid.RowDefinitions = new RowDefinitions("Auto,Auto,Auto");
            grid.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(track, 1);
            Grid.SetRow(after, 2);
        }

        foreach (var part in new Control[] { track, decrease, increase, before, after })
            part.RegisterInNameScope(scope);
        return new DataValidationErrors { Content = grid };
    }

    private static TickBar Ticks(string name, TickBarPlacement placement, Orientation orientation, Thumb thumb)
    {
        var ticks = new TickBar { Name = name, Placement = placement, Orientation = orientation };
        if (orientation == Orientation.Horizontal)
            ticks.Height = TickLength;
        else
            ticks.Width = TickLength;
        ticks.Bind(TickBar.MinimumProperty, new TemplateBinding(RangeBase.MinimumProperty));
        ticks.Bind(TickBar.MaximumProperty, new TemplateBinding(RangeBase.MaximumProperty));
        ticks.Bind(TickBar.TickFrequencyProperty, new TemplateBinding(Slider.TickFrequencyProperty));
        ticks.Bind(TickBar.TicksProperty, new TemplateBinding(Slider.TicksProperty));
        ticks.Bind(TickBar.IsDirectionReversedProperty, new TemplateBinding(Slider.IsDirectionReversedProperty));
        // Ticks line up with the thumb's centre at each end.
        ticks.Bind(TickBar.ReservedSpaceProperty, thumb.GetObservable(Visual.BoundsProperty).ToBinding());
        return ticks;
    }
}
