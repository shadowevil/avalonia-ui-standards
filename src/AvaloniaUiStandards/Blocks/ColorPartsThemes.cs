using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Converters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.Converters;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The colour primitives of the ColorPicker package (ColorSpectrum, ColorSlider, ColorPreviewer)
/// and the parts they share with ColorPicker and ColorView: the colour chip (a squircle over the
/// checkerboard with a hairline edge) and the colour thumb (a white ring with hairline edges and
/// the Slider thumb's shadow, the Slider thumb's size, growing on hover and press). Fluent's part
/// names are kept, so the controls' code finds them.
/// </summary>
internal static class ColorPartsThemes
{
    public const string RingPart = "PART_Ring";
    public const string ThumbPart = "ColorSliderThumb";
    public const string SelectionPanelPart = "PART_SelectionEllipsePanel";
    public const string SpectrumBoxPart = "PART_SpectrumBox";
    public const string TrackPart = "PART_Track";

    /// <summary>The spectrum's side; the ColorView's pages share its height.</summary>
    public const double SpectrumSize = 256;

    /// <summary>The white ring's width, between its two hairline edges.</summary>
    public const double RingWidth = 2;

    /// <summary>Fluent's checkerboard under transparent colours (from the ColorPicker package's Fluent styles).</summary>
    public const string CheckerKey = "ColorControlCheckeredBackgroundBrush";

    // Density values, set by ApplyDensity under these non-public keys.
    internal static readonly object ViewWidthKey = new();
    internal static readonly object ChipMarginKey = new();
    internal static readonly object HashMarginKey = new();

    private static readonly ITransform Rest = TransformOperations.Identity;
    private static readonly ITransform Hover = TransformOperations.Parse("scale(1.1)");
    private static readonly ITransform Pressed = TransformOperations.Parse("scale(1.2)");

    private static readonly ToBrushConverter ToBrush = new();
    private static readonly AccentColorConverter Accent = new();

    public static void ApplyDensity(IResourceDictionary resources, DensityStyle density)
    {
        var thumb = density == DensityStyle.Compact ? RangeParts.Compact.Thumb : RangeParts.Normal.Thumb;
        // The spectrum and a vertical slider either side of it, SpacingM apart.
        resources[ViewWidthKey] = SpectrumSize + 2 * (thumb + Spacing.M);
        // The picker's chip sits the field-button inset from the field's outer edge.
        var height = density == DensityStyle.Compact ? ButtonParts.Compact.MinHeight : ButtonParts.Normal.MinHeight;
        resources[ChipMarginKey] = new Thickness((height - FieldParts.ButtonSize(density)) / 2, 0, 0, 0);
        // The hex box's "#" sits in the text's side padding, SpacingXS before the text.
        var side = density == DensityStyle.Compact ? ButtonParts.Compact.Padding.Left : ButtonParts.Normal.Padding.Left;
        resources[HashMarginKey] = new Thickness(side, 0, Spacing.XS - side, 0);
    }

    // --- Shared parts ------------------------------------------------------------------------

    /// <summary>
    /// A colour chip: a squircle that clips the checkerboard and <paramref name="colour"/> laid
    /// over it, with a 1 px hairline edge (the Slider thumb's edge) drawn over both.
    /// </summary>
    public static SquircleBorder Chip(Control colour)
    {
        var checker = new Border();
        checker.Bind(Border.BackgroundProperty, new DynamicResourceExtension(CheckerKey));
        return new SquircleBorder
        {
            BorderBrush = new SolidColorBrush(SliderTheme.ThumbEdge),
            BorderThickness = new Thickness(1),
            Child = new Panel { Children = { checker, colour } },
        };
    }

    /// <summary>
    /// The colour thumb's look: a white ring (<see cref="RingPart"/>) between two hairline edges,
    /// with the Slider thumb's shadow, filling its parent. The centre is open, so the colour under
    /// it shows.
    /// </summary>
    public static Panel Ring(INameScope scope)
    {
        var edge = new SolidColorBrush(SliderTheme.ThumbEdge);
        var outer = new Border { CornerRadius = Pill, BorderBrush = edge, BorderThickness = new Thickness(1), BoxShadow = SliderTheme.ThumbShadow };
        var ring = new Border { Name = RingPart, CornerRadius = Pill, BorderThickness = new Thickness(RingWidth), Margin = new Thickness(1) };
        var inner = new Border { CornerRadius = Pill, BorderBrush = edge, BorderThickness = new Thickness(1), Margin = new Thickness(1 + RingWidth) };
        ring.RegisterInNameScope(scope);
        return new Panel { IsHitTestVisible = false, Children = { outer, ring, inner } };
    }

    /// <summary>A circular focus ring around a thumb, hidden until a style shows it.</summary>
    private static SquircleBorder ThumbFocusRing(INameScope scope)
    {
        var ring = new SquircleBorder
        {
            Name = FocusRing.Part,
            BorderThickness = new Thickness(FocusRing.Width),
            Margin = new Thickness(-FocusRing.Offset),
            CornerRadius = Pill,
            IsHitTestVisible = false,
        };
        ring.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("SystemControlHighlightAccentBrush"));
        ring.RegisterInNameScope(scope);
        return ring;
    }

    private static Avalonia.Styling.Style On(Func<Selector?, Selector> selector, params Setter[] setters)
    {
        var style = new Avalonia.Styling.Style(x => selector(x));
        foreach (var setter in setters)
            style.Setters.Add(setter);
        return style;
    }

    // --- ColorSlider -------------------------------------------------------------------------

    private static readonly ControlTheme TrackPartTheme = new(typeof(RepeatButton))
    {
        Setters =
        {
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<RepeatButton>((_, _) => new Panel { Background = Brushes.Transparent })),
        },
    };

    private static readonly ControlTheme ThumbTheme = new(typeof(Thumb))
    {
        Setters =
        {
            new Setter(TemplatedControl.CornerRadiusProperty, Pill),
            new Setter(Visual.ClipToBoundsProperty, false),
            new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(ChromeTransitionsKey)),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Thumb>((thumb, scope) =>
            {
                var focus = ThumbFocusRing(scope);
                focus.Bind(Visual.IsVisibleProperty, new TemplateBinding(SliderTheme.ShowsFocusRingProperty));
                // Transparent, so the whole circle takes the pointer for dragging.
                return new Panel { Background = Brushes.Transparent, Children = { Ring(scope), focus } };
            })),
        },
        Children =
        {
            On(x => x.Nesting().Template().OfType<Border>().Name(RingPart), new Setter(Border.BorderBrushProperty, Brushes.White)),
            On(x => x.Nesting().Class(":disabled").Template().OfType<Border>().Name(RingPart),
                new Setter(Border.BorderBrushProperty, Resource("SystemControlBackgroundBaseLowBrush"))),
        },
    };

    /// <summary>
    /// ColorSlider: a full pill track as thick as the thumb, showing the slider's gradient (its
    /// <c>Background</c>, drawn by the control) over the checkerboard with a hairline edge, and
    /// the colour thumb. The focus ring goes around the thumb, as on the Slider.
    /// </summary>
    public static ControlTheme Slider()
    {
        var theme = new ControlTheme(typeof(ColorSlider));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(Visual.ClipToBoundsProperty, false));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Pill));
        foreach (var (orientation, pseudoClass) in new[] { (Orientation.Horizontal, ":horizontal"), (Orientation.Vertical, ":vertical") })
            theme.Children.Add(On(x => x.Nesting().Class(pseudoClass),
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ColorSlider>((slider, scope) => BuildSlider(scope, orientation))),
                new Setter(orientation == Orientation.Horizontal ? Layoutable.HeightProperty : Layoutable.WidthProperty,
                    new DynamicResourceExtension(RangeParts.ThumbKey))));

        theme.Children.Add(On(x => x.Nesting().Template().OfType<Thumb>(), new Setter(Visual.RenderTransformProperty, Rest)));
        theme.Children.Add(On(x => x.Nesting().Class(":pointerover").Template().OfType<Thumb>(), new Setter(Visual.RenderTransformProperty, Hover)));
        theme.Children.Add(On(x => x.Nesting().Class(":pressed").Template().OfType<Thumb>(), new Setter(Visual.RenderTransformProperty, Pressed)));
        theme.Children.Add(On(x => x.Nesting().Class(":focus-visible").Template().OfType<Thumb>(), new Setter(SliderTheme.ShowsFocusRingProperty, true)));
        theme.Children.Add(On(x => x.Nesting().Class(":focus-visible"), new Setter(Visual.ZIndexProperty, 1)));
        return theme;
    }

    private static Control BuildSlider(INameScope scope, Orientation orientation)
    {
        var checker = new SquircleBorder { CornerRadius = Pill };
        checker.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension(CheckerKey));
        // The control draws its gradient into Background, sized to the track.
        var gradient = new SquircleBorder { CornerRadius = Pill, BorderBrush = new SolidColorBrush(SliderTheme.ThumbEdge), BorderThickness = new Thickness(1) };
        gradient.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));

        var thumb = new Thumb { Name = ThumbPart, Theme = ThumbTheme };
        thumb.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(RangeParts.ThumbKey));
        thumb.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(RangeParts.ThumbKey));
        var decrease = new RepeatButton { Name = SliderTheme.DecreasePart, Theme = TrackPartTheme, Focusable = false };
        var increase = new RepeatButton { Name = SliderTheme.IncreasePart, Theme = TrackPartTheme, Focusable = false };
        var track = new Track { Name = TrackPart, Orientation = orientation, Thumb = thumb, DecreaseButton = decrease, IncreaseButton = increase };
        track.Bind(Track.MinimumProperty, new TemplateBinding(RangeBase.MinimumProperty));
        track.Bind(Track.MaximumProperty, new TemplateBinding(RangeBase.MaximumProperty));
        track.Bind(Track.ValueProperty, new TemplateBinding(RangeBase.ValueProperty) { Mode = BindingMode.TwoWay });
        track.Bind(Track.IsDirectionReversedProperty, new TemplateBinding(Avalonia.Controls.Slider.IsDirectionReversedProperty));

        foreach (var part in new Control[] { track, thumb, decrease, increase })
            part.RegisterInNameScope(scope);
        return new Panel { Children = { checker, gradient, track } };
    }

    // --- ColorSpectrum -----------------------------------------------------------------------

    private static readonly IValueConverter IsBox = new FuncValueConverter<ColorSpectrumShape, bool>(s => s == ColorSpectrumShape.Box);
    private static readonly IValueConverter IsRing = new FuncValueConverter<ColorSpectrumShape, bool>(s => s == ColorSpectrumShape.Ring);

    /// <summary>
    /// ColorSpectrum: the box spectrum is a squircle (<c>CornerRadiusControl</c>) with a hairline
    /// edge; the ring spectrum keeps Fluent's ellipse with the same edge. The selector is the
    /// colour thumb, which shows the focus ring on keyboard focus. Fluent's black or white
    /// selector by luminance is dropped: the white ring with its edges and shadow reads on both.
    /// </summary>
    public static ControlTheme Spectrum()
    {
        var theme = new ControlTheme(typeof(ColorSpectrum));
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(Visual.ClipToBoundsProperty, false));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ColorSpectrum>((_, scope) => BuildSpectrum(scope))));

        Selector Selection(Selector x) => x.Template().OfType<Panel>().Name(SelectionPanelPart);
        theme.Children.Add(On(x => Selection(x.Nesting()),
            new Setter(Layoutable.WidthProperty, new DynamicResourceExtension(RangeParts.ThumbKey)),
            new Setter(Layoutable.HeightProperty, new DynamicResourceExtension(RangeParts.ThumbKey)),
            new Setter(Visual.RenderTransformProperty, Rest)));
        theme.Children.Add(On(x => Selection(x.Nesting().Class(":pointerover")), new Setter(Visual.RenderTransformProperty, Hover)));
        theme.Children.Add(On(x => Selection(x.Nesting().Class(":pressed")), new Setter(Visual.RenderTransformProperty, Pressed)));
        // Fluent's large selector under a touch press (so a finger does not hide it).
        theme.Children.Add(On(x => Selection(x.Nesting().Class(":large-selector")),
            new Setter(Layoutable.WidthProperty, 48d), new Setter(Layoutable.HeightProperty, 48d)));
        theme.Children.Add(On(x => x.Nesting().Template().OfType<SquircleBorder>().Name(FocusRing.Part), new Setter(Visual.IsVisibleProperty, false)));
        theme.Children.Add(On(x => x.Nesting().Class(":focus-visible").Template().OfType<SquircleBorder>().Name(FocusRing.Part),
            new Setter(Visual.IsVisibleProperty, true)));
        theme.Children.Add(On(x => x.Nesting().Class(":focus-visible"), new Setter(Visual.ZIndexProperty, 1)));
        theme.Children.Add(On(x => x.Nesting().Template().OfType<Border>().Name(RingPart), new Setter(Border.BorderBrushProperty, Brushes.White)));
        theme.Children.Add(On(x => x.Nesting().Class(":disabled").Template().OfType<Border>().Name(RingPart),
            new Setter(Border.BorderBrushProperty, Resource("SystemControlBackgroundBaseLowBrush"))));
        return theme;
    }

    private static Control BuildSpectrum(INameScope scope)
    {
        var edge = new SolidColorBrush(SliderTheme.ThumbEdge);
        // The control sizes these to the square it fits, and fills them with its bitmaps.
        var rectangle = new Rectangle { Name = "PART_SpectrumRectangle", IsHitTestVisible = false };
        var overlayRectangle = new Rectangle { Name = "PART_SpectrumOverlayRectangle", IsHitTestVisible = false };
        var box = new SquircleBorder
        {
            Name = SpectrumBoxPart,
            IsHitTestVisible = false,
            BorderBrush = edge,
            BorderThickness = new Thickness(1),
            Child = new Panel { Children = { rectangle, overlayRectangle } },
        };
        box.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        box.Bind(Visual.IsVisibleProperty, new TemplateBinding(ColorSpectrum.ShapeProperty) { Converter = IsBox });

        Ellipse RingShape(string? name)
        {
            var ellipse = new Ellipse { Name = name, IsHitTestVisible = false };
            ellipse.Bind(Visual.IsVisibleProperty, new TemplateBinding(ColorSpectrum.ShapeProperty) { Converter = IsRing });
            return ellipse;
        }
        var ellipse = RingShape("PART_SpectrumEllipse");
        var overlayEllipse = RingShape("PART_SpectrumOverlayEllipse");
        var borderEllipse = RingShape("BorderEllipse");
        borderEllipse.Stroke = edge;
        borderEllipse.StrokeThickness = 1;

        var selection = new Panel { Name = SelectionPanelPart, Children = { Ring(scope), ThumbFocusRing(scope) } };
        selection.Bind(Animatable.TransitionsProperty, new DynamicResourceExtension(ChromeTransitionsKey));
        ToolTip.SetPlacement(selection, PlacementMode.Top);
        ToolTip.SetVerticalOffset(selection, -10);
        var input = new Canvas { Name = "PART_InputTarget", Background = Brushes.Transparent, Children = { selection } };

        // The selector is not clipped, so it shows in full at the spectrum's edges.
        var sizing = new Panel
        {
            Name = "PART_SizingPanel",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { box, ellipse, overlayEllipse, borderEllipse, input },
        };
        var root = new Panel { Name = "PART_LayoutRoot", Children = { sizing } };

        foreach (var part in new Control[] { root, sizing, rectangle, overlayRectangle, box, ellipse, overlayEllipse, borderEllipse, input, selection })
            part.RegisterInNameScope(scope);
        return root;
    }

    // --- ColorPreviewer ----------------------------------------------------------------------

    /// <summary>
    /// ColorPreviewer: the colour as a squircle chip as high as a control row (32, or 24 at
    /// Compact), with the four accent chips (two darker, two lighter) as square chips either side,
    /// <c>SpacingXS</c> apart. Fluent's drop shadow is dropped.
    /// </summary>
    public static ControlTheme Previewer() => new(typeof(ColorPreviewer))
    {
        Setters =
        {
            new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")),
            new Setter(Layoutable.HeightProperty, new DynamicResourceExtension(MinHeightKey)),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ColorPreviewer>((_, scope) => BuildPreviewer(scope))),
        },
    };

    private static Control BuildPreviewer(INameScope scope)
    {
        SquircleBorder Swatch(string? name, int step)
        {
            var colour = new Border { Name = name, Tag = step.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            colour.Bind(Border.BackgroundProperty, step == 0
                ? new TemplateBinding(ColorPreviewer.HsvColorProperty) { Converter = ToBrush }
                : new TemplateBinding(ColorPreviewer.HsvColorProperty) { Converter = Accent, ConverterParameter = colour.Tag });
            if (name is not null)
                colour.RegisterInNameScope(scope);
            var chip = Chip(colour);
            chip.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
            if (step != 0)
            {
                chip.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(MinHeightKey));
                chip.Bind(Visual.IsVisibleProperty, new TemplateBinding(ColorPreviewer.IsAccentColorsVisibleProperty));
                chip.Margin = step < 0 ? new Thickness(0, 0, Spacing.XS, 0) : new Thickness(Spacing.XS, 0, 0, 0);
            }
            return chip;
        }

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto,Auto") };
        var swatches = new[]
        {
            Swatch("PART_AccentDecrement2Border", -2), Swatch("PART_AccentDecrement1Border", -1), Swatch(null, 0),
            Swatch("PART_AccentIncrement1Border", 1), Swatch("PART_AccentIncrement2Border", 2),
        };
        for (var i = 0; i < swatches.Length; i++)
        {
            Grid.SetColumn(swatches[i], i);
            grid.Children.Add(swatches[i]);
        }
        return grid;
    }
}
