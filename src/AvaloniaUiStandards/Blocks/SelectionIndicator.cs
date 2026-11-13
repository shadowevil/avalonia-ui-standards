using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// A panel that lays its content out over the whole area and places one indicator element (the
/// segmented control's pill, the tab underline) over the selected container of its owner. When
/// the selection changes the indicator slides and resizes to the new container through
/// <see cref="OffsetProperty"/> and <see cref="SizeProperty"/>, whose transitions come from the
/// theme (fixed objects, none under reduce motion). When the layout changes (a resize, the
/// first placement) it jumps, so it never lags behind the content.
/// </summary>
internal sealed class SelectionIndicator : Panel
{
    // The slide transitions, set by BlocksThemes.ApplyMotion under this non-public key.
    internal static readonly object TransitionsKey = new();

    /// <summary>The indicator's position in the panel, animated on selection changes.</summary>
    public static readonly StyledProperty<Point> OffsetProperty =
        AvaloniaProperty.Register<SelectionIndicator, Point>(nameof(Offset));

    /// <summary>The indicator's size, animated on selection changes.</summary>
    public static readonly StyledProperty<Size> SizeProperty =
        AvaloniaProperty.Register<SelectionIndicator, Size>(nameof(Size));

    private readonly SelectingItemsControl _owner;
    private readonly Control _indicator;
    private readonly Func<Control, Rect, Size, Rect> _place;
    private Rect? _target;
    private int _targetIndex = -1;
    private Size _lastSize;

    static SelectionIndicator() => AffectsArrange<SelectionIndicator>(OffsetProperty, SizeProperty);

    /// <param name="owner">The control whose selected container the indicator follows.</param>
    /// <param name="indicator">The indicator, drawn under <paramref name="content"/>.</param>
    /// <param name="content">The items, laid out over the whole panel.</param>
    /// <param name="place">The indicator's rectangle for a container, its rectangle in the panel and the panel's size.</param>
    public SelectionIndicator(SelectingItemsControl owner, Control indicator, Control content, Func<Control, Rect, Size, Rect> place)
    {
        _owner = owner;
        _indicator = indicator;
        _place = place;
        indicator.IsHitTestVisible = false;
        Children.Add(indicator);
        Children.Add(content);
    }

    // The owner's selection is followed while the panel is in the tree.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner.PropertyChanged += OnOwnerChanged;
        InvalidateArrange();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _owner.PropertyChanged -= OnOwnerChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnOwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SelectingItemsControl.SelectedIndexProperty)
            InvalidateArrange();
    }

    public Point Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    public Size Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>The indicator's target rectangle (where it rests once any slide ends), or null with no selection.</summary>
    internal Rect? Target => _target;

    /// <summary>The slide: offset and size over <paramref name="duration"/>, or none when it is zero (reduce motion).</summary>
    public static Transitions Slide(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? []
        :
        [
            new PointTransition { Property = OffsetProperty, Duration = duration, Easing = easing },
            new SizeTransition { Property = SizeProperty, Duration = duration, Easing = easing },
        ];

    /// <summary>The style that gives every indicator panel in a theme's template the slide transitions.</summary>
    public static IStyle Motion() => new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<SelectionIndicator>())
    {
        Setters = { new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(TransitionsKey)) },
    };

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size();
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            if (child != _indicator)
                size = new Size(Math.Max(size.Width, child.DesiredSize.Width), Math.Max(size.Height, child.DesiredSize.Height));
        }
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
            if (child != _indicator)
                child.Arrange(new Rect(finalSize));

        var index = _owner.SelectedIndex;
        Rect? target = index >= 0 && _owner.ContainerFromIndex(index) is { IsVisible: true } container
            && container.TranslatePoint(default, this) is { } origin
            ? _place(container, new Rect(origin, container.Bounds.Size), finalSize)
            : null;

        if (target != _target)
        {
            // Slide only when the selection moved within an unchanged layout; otherwise jump.
            var slide = _target is not null && target is not null && index != _targetIndex && finalSize == _lastSize;
            _target = target;
            _targetIndex = index;
            if (target is { } rect)
                MoveTo(rect, slide);
        }
        _lastSize = finalSize;

        _indicator.IsVisible = _target is not null;
        _indicator.Arrange(new Rect(Offset, Size));
        return finalSize;
    }

    // Offset and Size are set as local values: a current value set on a property with no other
    // value is dropped when a transition on it ends, which would send the indicator back to the
    // property's default (the panel's origin, or no size) at the end of every slide.
    private void MoveTo(Rect rect, bool slide)
    {
        if (slide)
        {
            Offset = rect.Position;
            Size = rect.Size;
            return;
        }
        // A jump: the theme's transitions are lifted while the values change, then restored.
        SetValue(TransitionsProperty, null);
        Offset = rect.Position;
        Size = rect.Size;
        ClearValue(TransitionsProperty);
    }
}
