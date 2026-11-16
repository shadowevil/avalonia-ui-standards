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
/// <remarks>
/// A panel shows and hides by sliding in from its edge (<see cref="InspectorRevealProperty"/>,
/// <see cref="BottomRevealProperty"/>, 0 hidden to 1 shown), over the theme's transitions when <see cref="Apply"/> is
/// asked to animate, at once otherwise. The bottom panel hides from the layout while the area is shorter than
/// <see cref="BottomRoom"/>.
/// </remarks>
internal sealed class PaneLayout : Panel
{
    /// <summary>The gap between panels, which is the resize handle (<c>SpacingS</c>).</summary>
    public const double Gap = Spacing.S;

    /// <summary>
    /// The shortest area that holds the bottom panel: its minimum height, the gap and a content panel at least as tall
    /// (2 × 96 + 8 = 200).
    /// </summary>
    public const double BottomRoom = 2 * StandardsWindow.BottomPanelMinHeight + Gap;

    public static readonly StyledProperty<double> InspectorRevealProperty =
        AvaloniaProperty.Register<PaneLayout, double>("InspectorReveal");

    public static readonly StyledProperty<double> BottomRevealProperty =
        AvaloniaProperty.Register<PaneLayout, double>("BottomReveal");

    private readonly Control _content;
    private readonly Control _inspector;
    private readonly Control _bottom;
    private readonly PaneSplitter _inspectorSplitter;
    private readonly PaneSplitter _bottomSplitter;
    private InspectorPlacement _placement;
    // The placement the inspector slides out of while it hides.
    private InspectorPlacement _shownPlacement = InspectorPlacement.Docked;
    private double _inspectorWidth = StandardsWindow.InspectorDefaultWidth;
    private bool _bottomOpen;
    private double _bottomHeight = StandardsWindow.BottomPanelDefaultHeight;
    private bool _hasBottomRoom = true;

    static PaneLayout()
    {
        AffectsMeasure<PaneLayout>(InspectorRevealProperty, BottomRevealProperty);
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
        // A panel sliding in or out is cut at the area's edge.
        ClipToBounds = true;
        UpdateVisibility();
    }

    /// <summary>Where the inspector shows (or is going to, while it slides).</summary>
    public InspectorPlacement InspectorPlacement => _placement;

    /// <summary>Whether the bottom panel shows (or is going to): open and with room for it.</summary>
    public bool IsBottomShown => _bottomOpen && _hasBottomRoom;

    /// <summary>Whether the area is tall enough for the bottom panel (<see cref="BottomRoom"/>).</summary>
    public bool HasBottomRoom => _hasBottomRoom;

    /// <summary>Raised when <see cref="HasBottomRoom"/> changes.</summary>
    public event Action? BottomRoomChanged;

    /// <summary>The bottom panel's largest height now: half the area less the gap, so the content panel is at least as tall.</summary>
    public double BottomMaxHeight => MaxBottom(Bounds.Height);

    private static double MaxBottom(double areaHeight) => Math.Max(StandardsWindow.BottomPanelMinHeight, (areaHeight - Gap) / 2);

    /// <summary>
    /// Sets what shows: the inspector's placement and width, whether the bottom panel is open and its asked height. A
    /// panel that shows or hides slides over the theme's transitions when <paramref name="animate"/>, else at once.
    /// </summary>
    public void Apply(InspectorPlacement placement, double inspectorWidth, bool bottomOpen, double bottomHeight, bool animate)
    {
        var changed = placement != _placement || inspectorWidth != _inspectorWidth || bottomOpen != _bottomOpen || bottomHeight != _bottomHeight;
        _placement = placement;
        if (placement != InspectorPlacement.None)
            _shownPlacement = placement;
        _inspectorWidth = inspectorWidth;
        _bottomOpen = bottomOpen;
        _bottomHeight = bottomHeight;
        SetReveals(animate);
        if (changed)
            InvalidateMeasure();
    }

    // Only a panel that shows or hides changes its reveal: a slide in progress is left to run otherwise.
    private void SetReveals(bool animate)
    {
        var inspector = _placement != InspectorPlacement.None ? 1d : 0d;
        var bottom = IsBottomShown ? 1d : 0d;
        if (inspector != _inspectorTarget || bottom != _bottomTarget)
        {
            _inspectorTarget = inspector;
            _bottomTarget = bottom;
            if (!animate)
                SetValue(TransitionsProperty, null);
            SetValue(InspectorRevealProperty, inspector);
            SetValue(BottomRevealProperty, bottom);
            if (!animate)
                ClearValue(TransitionsProperty);
        }
        UpdateVisibility();
    }

    private double _inspectorTarget, _bottomTarget;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InspectorRevealProperty || change.Property == BottomRevealProperty)
            UpdateVisibility();
    }

    // A panel shows while it is shown or still sliding out; the handles only while their panel is shown.
    private void UpdateVisibility()
    {
        _inspector.IsVisible = _placement != InspectorPlacement.None || GetValue(InspectorRevealProperty) > 0;
        _inspectorSplitter.IsVisible = _placement == InspectorPlacement.Docked;
        // The overlay is drawn over the content panel and the bottom panel.
        _inspector.ZIndex = _shownPlacement == InspectorPlacement.Overlay ? 1 : 0;
        _bottom.IsVisible = IsBottomShown || GetValue(BottomRevealProperty) > 0;
        _bottomSplitter.IsVisible = IsBottomShown;
    }

    /// <summary>Where each part goes in an area of <paramref name="size"/>.</summary>
    internal (Rect Content, Rect InspectorSplitter, Rect Inspector, Rect BottomSplitter, Rect Bottom) Arrangement(Size size)
    {
        var (w, h) = (size.Width, size.Height);
        var inspectorReveal = Math.Clamp(GetValue(InspectorRevealProperty), 0, 1);
        var bottomReveal = Math.Clamp(GetValue(BottomRevealProperty), 0, 1);
        var inspectorWidth = Math.Min(_inspectorWidth, w);
        var docked = _shownPlacement == InspectorPlacement.Docked;
        var column = docked ? Math.Max(0, w - (inspectorWidth + Gap) * inspectorReveal) : w;
        var bottom = Math.Min(Math.Max(_bottomHeight, StandardsWindow.BottomPanelMinHeight), MaxBottom(h));
        var contentHeight = Math.Max(0, h - (bottom + Gap) * bottomReveal);

        var inspector = inspectorReveal <= 0 && _placement == InspectorPlacement.None ? default
            : docked ? new Rect(column + Gap, 0, inspectorWidth, h)
            : new Rect(w - inspectorWidth * inspectorReveal, 0, inspectorWidth, h);
        return (
            new Rect(0, 0, column, contentHeight),
            _placement == InspectorPlacement.Docked ? new Rect(column, 0, Gap, h) : default,
            inspector,
            IsBottomShown ? new Rect(0, contentHeight, column, Gap) : default,
            bottomReveal > 0 || IsBottomShown ? new Rect(0, contentHeight + Gap, column, bottom) : default);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
        // Too short for the bottom panel: it leaves the layout at once, and returns when the area grows.
        if (size.Height > 0 && size.Height >= BottomRoom != _hasBottomRoom)
        {
            _hasBottomRoom = size.Height >= BottomRoom;
            SetReveals(animate: false);
            BottomRoomChanged?.Invoke();
        }
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
