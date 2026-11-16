using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>Where a <see cref="StandardsWindow"/>'s inspector shows.</summary>
internal enum InspectorPlacement
{
    /// <summary>Not shown: no inspector, closed, or hidden from a narrow window's layout.</summary>
    None,

    /// <summary>Beside the content panel, full height, with the resize handle in the gap.</summary>
    Docked,

    /// <summary>Over the content panel at the right edge (a narrow window), full height.</summary>
    Overlay,
}

/// <summary>
/// The content area of a <see cref="StandardsWindow"/>: the glass content panel, the inspector to its right (full
/// height) and the bottom panel directly under the content panel, with a <c>SpacingS</c> gap between panels holding
/// each resize handle. With neither panel shown, the content panel fills the area.
/// </summary>
internal sealed class PaneLayout : Panel
{
    /// <summary>The gap between panels, which is the resize handle (<c>SpacingS</c>).</summary>
    public const double Gap = Spacing.S;

    public static readonly StyledProperty<InspectorPlacement> InspectorPlacementProperty =
        AvaloniaProperty.Register<PaneLayout, InspectorPlacement>(nameof(InspectorPlacement));

    public static readonly StyledProperty<double> InspectorWidthProperty =
        AvaloniaProperty.Register<PaneLayout, double>(nameof(InspectorWidth), StandardsWindow.InspectorDefaultWidth);

    public static readonly StyledProperty<bool> IsBottomShownProperty =
        AvaloniaProperty.Register<PaneLayout, bool>(nameof(IsBottomShown));

    public static readonly StyledProperty<double> BottomHeightProperty =
        AvaloniaProperty.Register<PaneLayout, double>(nameof(BottomHeight), StandardsWindow.BottomPanelDefaultHeight);

    private readonly Control _content;
    private readonly Control _inspector;
    private readonly Control _bottom;
    private readonly PaneSplitter _inspectorSplitter;
    private readonly PaneSplitter _bottomSplitter;

    static PaneLayout()
    {
        AffectsMeasure<PaneLayout>(InspectorPlacementProperty, InspectorWidthProperty, IsBottomShownProperty, BottomHeightProperty);
    }

    public PaneLayout(Control content, Control inspector, Control bottom, PaneSplitter inspectorSplitter, PaneSplitter bottomSplitter)
    {
        _content = content;
        _inspector = inspector;
        _bottom = bottom;
        _inspectorSplitter = inspectorSplitter;
        _bottomSplitter = bottomSplitter;
        // In reading and Tab order: the content, the bottom panel under it, then the inspector.
        Children.AddRange([content, bottomSplitter, bottom, inspectorSplitter, inspector]);
        UpdateVisibility();
    }

    public InspectorPlacement InspectorPlacement
    {
        get => GetValue(InspectorPlacementProperty);
        set => SetValue(InspectorPlacementProperty, value);
    }

    /// <summary>The inspector's width (the window keeps it within its minimum and maximum).</summary>
    public double InspectorWidth
    {
        get => GetValue(InspectorWidthProperty);
        set => SetValue(InspectorWidthProperty, value);
    }

    public bool IsBottomShown
    {
        get => GetValue(IsBottomShownProperty);
        set => SetValue(IsBottomShownProperty, value);
    }

    /// <summary>The bottom panel's asked height; it shows at most <see cref="BottomMaxHeight"/>.</summary>
    public double BottomHeight
    {
        get => GetValue(BottomHeightProperty);
        set => SetValue(BottomHeightProperty, value);
    }

    /// <summary>The bottom panel's largest height now: half the area's height, and never below its minimum.</summary>
    public double BottomMaxHeight => MaxBottom(Bounds.Height);

    private static double MaxBottom(double areaHeight) => Math.Max(StandardsWindow.BottomPanelMinHeight, areaHeight / 2);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InspectorPlacementProperty || change.Property == IsBottomShownProperty)
            UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        var placement = InspectorPlacement;
        _inspector.IsVisible = placement != InspectorPlacement.None;
        _inspectorSplitter.IsVisible = placement == InspectorPlacement.Docked;
        // The overlay is drawn over the content panel and the bottom panel.
        _inspector.ZIndex = placement == InspectorPlacement.Overlay ? 1 : 0;
        _bottom.IsVisible = IsBottomShown;
        _bottomSplitter.IsVisible = IsBottomShown;
    }

    /// <summary>Where each part goes in an area of <paramref name="size"/>.</summary>
    internal (Rect Content, Rect InspectorSplitter, Rect Inspector, Rect BottomSplitter, Rect Bottom) Arrangement(Size size)
    {
        var (w, h) = (size.Width, size.Height);
        var placement = InspectorPlacement;
        var inspectorWidth = placement == InspectorPlacement.None ? 0 : Math.Min(InspectorWidth, w);
        var docked = placement == InspectorPlacement.Docked;
        var column = docked ? Math.Max(0, w - inspectorWidth - Gap) : w;
        var bottom = IsBottomShown ? Math.Min(Math.Max(BottomHeight, StandardsWindow.BottomPanelMinHeight), MaxBottom(h)) : 0;
        var contentHeight = IsBottomShown ? Math.Max(0, h - bottom - Gap) : h;

        var inspector = placement switch
        {
            InspectorPlacement.Docked => new Rect(column + Gap, 0, inspectorWidth, h),
            InspectorPlacement.Overlay => new Rect(w - inspectorWidth, 0, inspectorWidth, h),
            _ => default,
        };
        return (
            new Rect(0, 0, column, contentHeight),
            docked ? new Rect(column, 0, Gap, h) : default,
            inspector,
            IsBottomShown ? new Rect(0, contentHeight, column, Gap) : default,
            IsBottomShown ? new Rect(0, contentHeight + Gap, column, Math.Max(0, h - contentHeight - Gap)) : default);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
        var (content, inspectorSplitter, inspector, bottomSplitter, bottom) = Arrangement(size);
        _content.Measure(content.Size);
        _inspectorSplitter.Measure(inspectorSplitter.Size);
        _inspector.Measure(inspector.Size);
        _bottomSplitter.Measure(bottomSplitter.Size);
        _bottom.Measure(bottom.Size);
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var (content, inspectorSplitter, inspector, bottomSplitter, bottom) = Arrangement(finalSize);
        _content.Arrange(content);
        _inspectorSplitter.Arrange(inspectorSplitter);
        _inspector.Arrange(inspector);
        _bottomSplitter.Arrange(bottomSplitter);
        _bottom.Arrange(bottom);
        return finalSize;
    }
}

/// <summary>
/// A panel's resize handle: a <see cref="GridSplitter"/> in its look (0039), filling the gap between two panels. It
/// does not resize grid tracks itself; it reports how far it was dragged from the press, along its axis, in its
/// parent's coordinates, and the arrow keys' steps (<see cref="GridSplitter.KeyboardIncrement"/>), and the window sizes
/// the panel. Escape during a drag puts the size back.
/// </summary>
internal sealed class PaneSplitter : GridSplitter
{
    private Point? _start;
    private IPointer? _pointer;

    protected override Type StyleKeyOverride => typeof(GridSplitter);

    /// <summary>A drag starts.</summary>
    public event Action? DragBegan;

    /// <summary>The drag moved: the distance from the press along the axis (right or down positive).</summary>
    public event Action<double>? Dragged;

    /// <summary>Escape during a drag: the size goes back to its value at the press.</summary>
    public event Action? DragCanceled;

    /// <summary>An arrow key: the step along the axis (right or down positive).</summary>
    public event Action<double>? Stepped;

    /// <summary>Whether a drag is in progress.</summary>
    internal bool IsDragging => _start is not null;

    private bool Upright => ResizeDirection == GridResizeDirection.Columns;

    /// <summary>Ends the drag in progress (its panel collapsed): the pointer is let go.</summary>
    public void EndDrag()
    {
        _start = null;
        var pointer = _pointer;
        _pointer = null;
        if (pointer?.Captured == this)
            pointer.Capture(null);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || GetVisualParentControl() is not { } parent)
            return;
        _start = e.GetPosition(parent);
        _pointer = e.Pointer;
        DragBegan?.Invoke();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_start is not { } start || GetVisualParentControl() is not { } parent)
            return;
        var delta = e.GetPosition(parent) - start;
        Dragged?.Invoke(Upright ? delta.X : delta.Y);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _start = null;
        _pointer = null;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _start = null;
        _pointer = null;
    }

    // The window sizes the panel, so GridSplitter's own track resizing (which looks for a Grid) does not run.
    protected override void OnDragStarted(Avalonia.Input.VectorEventArgs e)
    {
    }

    protected override void OnDragDelta(Avalonia.Input.VectorEventArgs e)
    {
    }

    protected override void OnDragCompleted(Avalonia.Input.VectorEventArgs e)
    {
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var step = KeyboardIncrement;
        double? move = (e.Key, Upright) switch
        {
            (Key.Left, true) => -step,
            (Key.Right, true) => step,
            (Key.Up, false) => -step,
            (Key.Down, false) => step,
            _ => null,
        };
        if (move is { } m && e.KeyModifiers == KeyModifiers.None)
        {
            Stepped?.Invoke(m);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _start is not null)
        {
            DragCanceled?.Invoke();
            EndDrag();
            e.Handled = true;
        }
        else if (e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down or Key.Escape))
            base.OnKeyDown(e);
    }

    private Visual? GetVisualParentControl() => Avalonia.VisualTree.VisualExtensions.GetVisualParent(this);
}
