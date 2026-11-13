using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using Path = Avalonia.Controls.Shapes.Path;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The parts shared by the toggle themes (CheckBox, RadioButton, ToggleSwitch): density sizes,
/// the indicator layers, the check glyphs and the transitions. Each indicator is an unchecked
/// layer (outline) under a checked layer (accent fill) and its mark: checking fades the checked
/// layer and the mark over <c>MotionNormal</c>; hover and press cross-fade colours over
/// <c>MotionFast</c>.
/// </summary>
internal static class ToggleParts
{
    public const string OutlinePart = "PART_Outline";
    public const string FillPart = "PART_Fill";
    public const string IndicatorPart = "PART_Indicator";

    /// <summary>The gap between an indicator and its label (<c>SpacingS</c>).</summary>
    public const double LabelGap = Spacing.S;

    /// <summary>The density sizes of the toggles.</summary>
    internal sealed record Sizes(
        double Box, double BoxRadius, double Dot, double Glyph,
        double TrackWidth, double TrackHeight, double KnobOff, double KnobOn, double KnobStretch);

    /// <summary>Normal: box and circle 20 (box radius 5), dot 8, glyph stroke 2; track 40 x 20, knob 12 off and 14 on.</summary>
    public static readonly Sizes Normal = new(20, 5, 8, 2, 40, 20, 12, 14, 4);

    /// <summary>Compact: box and circle 16 (box radius 4), dot 6, glyph stroke 1.5; track 32 x 16, knob 10 off and 12 on.</summary>
    public static readonly Sizes Compact = new(16, 4, 6, 1.5, 32, 16, 10, 12, 4);

    // Density values, set by ApplyDensity under these non-public keys.
    internal static readonly object BoxKey = new();
    internal static readonly object BoxRadiusKey = new();
    internal static readonly object CircleRadiusKey = new();
    internal static readonly object DotKey = new();
    internal static readonly object GlyphThicknessKey = new();
    internal static readonly object CheckGlyphKey = new();
    internal static readonly object DashGlyphKey = new();
    internal static readonly object TrackWidthKey = new();
    internal static readonly object TrackHeightKey = new();
    internal static readonly object TrackRadiusKey = new();
    internal static readonly object KnobTravelKey = new();
    internal static readonly object KnobOffKey = new();
    internal static readonly object KnobOnKey = new();
    internal static readonly object KnobOffPressedKey = new();
    internal static readonly object KnobOnPressedKey = new();

    // Transitions, set by ApplyMotion under these non-public keys.
    internal static readonly object PartTransitionsKey = new();
    internal static readonly object KnobTransitionsKey = new();

    public static void ApplyDensity(IResourceDictionary resources, Sizes sizes)
    {
        resources[BoxKey] = sizes.Box;
        resources[BoxRadiusKey] = new CornerRadius(sizes.BoxRadius);
        resources[CircleRadiusKey] = new CornerRadius(sizes.Box / 2);
        resources[DotKey] = sizes.Dot;
        resources[GlyphThicknessKey] = sizes.Glyph;
        resources[CheckGlyphKey] = CheckGlyph(sizes.Box);
        resources[DashGlyphKey] = DashGlyph(sizes.Box);
        resources[TrackWidthKey] = sizes.TrackWidth;
        resources[TrackHeightKey] = sizes.TrackHeight;
        resources[TrackRadiusKey] = new CornerRadius(sizes.TrackHeight / 2);
        // The moving knob cell is a square of the track's height; the knob canvas is the rest of the track.
        resources[KnobTravelKey] = sizes.TrackWidth - sizes.TrackHeight;
        resources[KnobOffKey] = sizes.KnobOff;
        resources[KnobOnKey] = sizes.KnobOn;
        resources[KnobOffPressedKey] = sizes.KnobOff + sizes.KnobStretch;
        resources[KnobOnPressedKey] = sizes.KnobOn + sizes.KnobStretch;
    }

    /// <summary>
    /// The toggle parts' transitions: colour cross-fades and knob size changes over
    /// <paramref name="fast"/>, and the checked layers' and marks' opacity over
    /// <paramref name="normal"/>; none when motion is reduced (zero durations).
    /// </summary>
    public static Transitions PartTransitions(TimeSpan fast, TimeSpan normal, Easing easing) => fast == TimeSpan.Zero
        ? []
        :
        [
            new CrossFadeBrushTransition { Property = Border.BackgroundProperty, Duration = fast, Easing = easing },
            new CrossFadeBrushTransition { Property = Border.BorderBrushProperty, Duration = fast, Easing = easing },
            new CrossFadeBrushTransition { Property = Shape.StrokeProperty, Duration = fast, Easing = easing },
            new DoubleTransition { Property = Layoutable.WidthProperty, Duration = fast, Easing = easing },
            new DoubleTransition { Property = Layoutable.HeightProperty, Duration = fast, Easing = easing },
            new DoubleTransition { Property = Visual.OpacityProperty, Duration = normal, Easing = easing },
        ];

    /// <summary>The ToggleSwitch knob's slide over <paramref name="normal"/>; none when it is zero (reduce motion).</summary>
    public static Transitions KnobTransitions(TimeSpan normal, Easing easing) => normal == TimeSpan.Zero
        ? []
        : [new DoubleTransition { Property = Canvas.LeftProperty, Duration = normal, Easing = easing }];

    // The check's centre line in Phosphor's 256 box: (40,144) to (96,200) to (224,72), 184 x 128.
    private static readonly Point[] CheckLine = [new(40, 144), new(96, 200), new(224, 72)];

    /// <summary>The check mark's centre line, 0.6 of the box wide, centred in it.</summary>
    internal static Geometry CheckGlyph(double box)
    {
        var width = box * 0.6;
        var height = width * 128 / 184;
        var origin = new Point((box - width) / 2, (box - height) / 2);
        Point Map(Point p) => new(origin.X + (p.X - 40) / 184 * width, origin.Y + (p.Y - 72) / 128 * height);
        return Line(CheckLine.Select(Map).ToArray());
    }

    /// <summary>The indeterminate dash's centre line, half the box wide, centred in it.</summary>
    internal static Geometry DashGlyph(double box) =>
        Line([new Point(box / 4, box / 2), new Point(box * 3 / 4, box / 2)]);

    private static StreamGeometry Line(Point[] points)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(points[0], false);
        foreach (var point in points.Skip(1))
            context.LineTo(point);
        context.EndFigure(false);
        return geometry;
    }

    /// <summary>A squircle layer of an indicator, filling it, with the toggle part transitions.</summary>
    public static SquircleBorder Layer(string name, CornerRadius? radius = null)
    {
        var layer = new SquircleBorder { Name = name, IsHitTestVisible = false };
        if (radius is { } r)
            layer.CornerRadius = r;
        else
            layer.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        layer.Bind(Animatable.TransitionsProperty, new DynamicResourceExtension(PartTransitionsKey));
        return layer;
    }

    /// <summary>A stroked mark (check or dash) drawn on the box, with round caps and joins.</summary>
    public static Path Glyph(string name, object geometryKey)
    {
        var path = new Path
        {
            Name = name,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
            IsHitTestVisible = false,
        };
        path.Bind(Path.DataProperty, new DynamicResourceExtension(geometryKey));
        path.Bind(Shape.StrokeThicknessProperty, new DynamicResourceExtension(GlyphThicknessKey));
        path.Bind(Animatable.TransitionsProperty, new DynamicResourceExtension(PartTransitionsKey));
        return path;
    }

    /// <summary>The label: Fluent's PART_ContentPresenter, after the indicator by the label gap (the control's padding).</summary>
    public static ContentPresenter Label()
    {
        var presenter = ButtonParts.CreatePresenter();
        presenter.Padding = default;
        presenter.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        presenter.Bind(Layoutable.VerticalAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));
        presenter.Bind(Layoutable.HorizontalAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        return presenter;
    }

    /// <summary>
    /// The indicator and label in a row: the indicator in a cell of the row height (so it lines up
    /// with the first line of a wrapped label), the label after it, and the focus ring around the indicator.
    /// </summary>
    public static Control Row(Control indicator, ContentPresenter label, INameScope scope)
    {
        indicator.Name = IndicatorPart;
        indicator.RegisterInNameScope(scope);
        var ringed = FocusRing.Wrap(indicator, scope);
        ringed.VerticalAlignment = VerticalAlignment.Center;
        var cell = new Panel { Children = { ringed }, VerticalAlignment = VerticalAlignment.Top };
        cell.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(ButtonParts.MinHeightKey));
        Grid.SetColumn(label, 1);
        label.RegisterInNameScope(scope);

        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Children = { cell, label } };
        root.Bind(Panel.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        return root;
    }

    /// <summary>Theme setters shared by CheckBox and RadioButton: alignment, label gap, row height, transparent background.</summary>
    public static void AddRowSetters(ControlTheme theme)
    {
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new Thickness(LabelGap, 0, 0, 0)));
        theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(ButtonParts.MinHeightKey)));
        theme.Setters.Add(new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Left));
        theme.Setters.Add(new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        // No label: no gap, so the indicator stands alone.
        theme.Children.Add(new Avalonia.Styling.Style(x => ButtonParts.Content(x.Nesting()).Class(":empty"))
        {
            Setters = { new Setter(Layoutable.MarginProperty, new Thickness(0)) },
        });
    }

    /// <summary>One state's colours: a part's background and border, the label's foreground, and a mark's stroke or fill.</summary>
    public static Avalonia.Styling.Style Colours(Func<Selector?, Selector> state, params (string Part, AvaloniaProperty Property, object Value)[] setters)
    {
        var style = new Avalonia.Styling.Style(x => state(x));
        foreach (var group in setters.GroupBy(s => s.Part))
        {
            var part = group.Key;
            var child = new Avalonia.Styling.Style(x => part == ButtonParts.ContentPart
                ? ButtonParts.Content(x.Nesting())
                : x.Nesting().Template().Name(part));
            foreach (var (_, property, value) in group)
                child.Setters.Add(new Setter(property, ButtonParts.Value(value)));
            style.Children.Add(child);
        }
        return style;
    }
}
