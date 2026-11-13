using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// A decorator that shows <see cref="Progress"/> (0 to 1) of its child's length along
/// <see cref="Orientation"/>, clipping the rest, so content below or beside it moves with the
/// reveal. The child is laid out at its full size, anchored at the start (or the end, with
/// <see cref="FromEnd"/>), and is hidden at 0 so nothing in it takes focus while closed.
/// </summary>
internal sealed class Reveal : Decorator
{
    // The reveal transition, set by BlocksThemes.ApplyMotion under this non-public key.
    internal static readonly object TransitionsKey = new();

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<Reveal, double>(nameof(Progress));

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<Reveal, Orientation>(nameof(Orientation), Orientation.Vertical);

    public static readonly StyledProperty<bool> FromEndProperty =
        AvaloniaProperty.Register<Reveal, bool>(nameof(FromEnd));

    static Reveal()
    {
        ClipToBoundsProperty.OverrideDefaultValue<Reveal>(true);
        AffectsMeasure<Reveal>(ProgressProperty, OrientationProperty);
        AffectsArrange<Reveal>(FromEndProperty);
    }

    /// <summary>How much of the child shows: 0 closed, 1 open.</summary>
    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Anchors the child at the end (bottom or right), for content that opens upwards or leftwards.</summary>
    public bool FromEnd
    {
        get => GetValue(FromEndProperty);
        set => SetValue(FromEndProperty, value);
    }

    /// <summary>The reveal over <paramref name="duration"/>, or none when it is zero (reduce motion).</summary>
    public static Transitions Opening(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? []
        : [new DoubleTransition { Property = ProgressProperty, Duration = duration, Easing = easing }];

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ProgressProperty || change.Property == ChildProperty)
            UpdateChildVisibility();
    }

    private void UpdateChildVisibility()
    {
        if (Child is { } child)
            child.IsVisible = Progress > 0;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child || Progress <= 0)
        {
            Child?.Measure(availableSize);
            return default;
        }
        var vertical = Orientation == Orientation.Vertical;
        var progress = Math.Clamp(Progress, 0, 1);
        // Open, the child takes the space offered; while revealing, its natural length.
        child.Measure(progress >= 1 ? availableSize
            : vertical ? availableSize.WithHeight(double.PositiveInfinity) : availableSize.WithWidth(double.PositiveInfinity));
        var size = child.DesiredSize;
        return vertical ? size.WithHeight(size.Height * progress) : size.WithWidth(size.Width * progress);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is not { } child)
            return finalSize;
        var vertical = Orientation == Orientation.Vertical;
        var full = vertical
            ? new Size(finalSize.Width, Math.Max(finalSize.Height, child.DesiredSize.Height))
            : new Size(Math.Max(finalSize.Width, child.DesiredSize.Width), finalSize.Height);
        var origin = !FromEnd ? default : vertical
            ? new Point(0, finalSize.Height - full.Height)
            : new Point(finalSize.Width - full.Width, 0);
        child.Arrange(new Rect(origin, full));
        return finalSize;
    }
}
