using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The GridSplitter theme: a transparent handle, invisible at rest, that shows a pill grip on
/// hover, as the overlay scrollbar shows its thumb (<see cref="ScrollBarTheme"/>): the scrollbar's
/// hover colour on hover and its drag colour while dragging, fading over the scrollbar's fixed
/// transitions. The grip runs along the splitter (upright between columns, level between rows).
/// Keyboard focus shows the grip with the focus ring around it.
/// </summary>
internal static class GridSplitterTheme
{
    public const string GripPart = "PART_Grip";
    public const string RingPart = FocusRing.Part;

    /// <summary>The grip's thickness and length.</summary>
    public const double GripThickness = 4, GripLength = 24;

    /// <summary>The handle's minimum thickness: <c>SpacingS</c>.</summary>
    public const double MinThickness = Spacing.S;

    public static ControlTheme Create()
    {
        var theme = new ControlTheme(typeof(GridSplitter))
        {
            Setters =
            {
                new Setter(InputElement.FocusableProperty, true),
                new Setter(Layoutable.MinWidthProperty, MinThickness),
                new Setter(Layoutable.MinHeightProperty, MinThickness),
                new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
                new Setter(Control.FocusAdornerProperty, null),
                new Setter(Visual.ClipToBoundsProperty, false),
                new Setter(GridSplitter.PreviewContentProperty, new FuncTemplate<Control>(Preview)),
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<GridSplitter>(Build)),
            },
        };
        theme.Children.Add(Grip(null, 0, ScrollBarTheme.ThumbHoverKey));
        theme.Children.Add(Grip(":pointerover", 1, ScrollBarTheme.ThumbHoverKey));
        theme.Children.Add(Grip(":focus-visible", 1, ScrollBarTheme.ThumbHoverKey));
        theme.Children.Add(Grip(":pressed", 1, ScrollBarTheme.ThumbDragKey));
        theme.Children.Add(Grip(":disabled", 0, ScrollBarTheme.ThumbHoverKey));
        theme.Children.Add(new Avalonia.Styling.Style(x => Ring(x.Nesting())) { Setters = { new Setter(Visual.IsVisibleProperty, false) } });
        theme.Children.Add(new Avalonia.Styling.Style(x => Ring(x.Nesting().Class(":focus-visible"))) { Setters = { new Setter(Visual.IsVisibleProperty, true) } });
        // Raised while focused, so the panes beside it do not cover the ring.
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":focus-visible")) { Setters = { new Setter(Visual.ZIndexProperty, 1) } });
        return theme;
    }

    private static Avalonia.Styling.Style Grip(string? pseudoClass, double opacity, object brushKey) =>
        new(x => (pseudoClass is null ? x.Nesting() : x.Nesting().Class(pseudoClass)).Template().OfType<PillBorder>().Name(GripPart))
        {
            Setters =
            {
                new Setter(Visual.OpacityProperty, opacity),
                new Setter(Border.BackgroundProperty, new DynamicResourceExtension(brushKey)),
            },
        };

    private static Selector Ring(Selector state) => state.Template().OfType<PillBorder>().Name(RingPart);

    private static Control Build(GridSplitter splitter, INameScope scope)
    {
        var grip = new PillBorder { Name = GripPart };
        grip.Bind(Animatable.TransitionsProperty, new DynamicResourceExtension(ScrollBarTheme.PartTransitionsKey));
        var ring = new PillBorder { Name = RingPart, BorderThickness = new Thickness(FocusRing.Width), IsHitTestVisible = false };
        ring.Bind(Border.BorderBrushProperty, new DynamicResourceExtension("SystemControlHighlightAccentBrush"));

        var root = new GripPanel(grip, ring);
        root.Bind(Panel.BackgroundProperty, new Avalonia.Data.TemplateBinding(TemplatedControl.BackgroundProperty));
        grip.RegisterInNameScope(scope);
        ring.RegisterInNameScope(scope);
        return root;
    }

    // The preview shown while dragging with ShowsPreview: the grip in its drag colour.
    private static Control Preview()
    {
        var grip = new PillBorder();
        grip.Bind(Border.BackgroundProperty, new DynamicResourceExtension(ScrollBarTheme.ThumbDragKey));
        return new GripPanel(grip, null);
    }

    /// <summary>
    /// Centres the grip, upright when the panel is taller than wide (between columns) and level
    /// otherwise (between rows), with the focus ring around it.
    /// </summary>
    internal sealed class GripPanel : Panel
    {
        private readonly Border _grip;
        private readonly Border? _ring;

        public GripPanel(Border grip, Border? ring)
        {
            _grip = grip;
            _ring = ring;
            Children.Add(grip);
            if (ring is not null)
                Children.Add(ring);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in Children)
                child.Measure(availableSize);
            return default;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var upright = finalSize.Height >= finalSize.Width;
            var size = upright ? new Size(GripThickness, GripLength) : new Size(GripLength, GripThickness);
            var grip = new Rect(new Point((finalSize.Width - size.Width) / 2, (finalSize.Height - size.Height) / 2), size);
            _grip.Arrange(grip);
            _ring?.Arrange(grip.Inflate(FocusRing.Offset));
            return finalSize;
        }
    }
}
