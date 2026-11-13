using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>How much room the <see cref="Sidebar"/> takes beside the content.</summary>
public enum SidebarStage
{
    /// <summary>Icons and labels, 240 px wide.</summary>
    Expanded,

    /// <summary>An icon rail, 48 px wide (40 at Compact density); each row's label is its tooltip.</summary>
    Compact,

    /// <summary>No room at all; the window's sidebar button opens the sidebar over the content.</summary>
    Hidden,
}

/// <summary>
/// The navigation sidebar: a list of <see cref="SidebarItem"/> rows (icon and label) with
/// optional <see cref="SidebarHeader"/> section headers, for <c>StandardsWindow.Sidebar</c>.
/// It is a <see cref="ListBox"/>, so selection and keyboard navigation are the ListBox's.
/// </summary>
/// <remarks>
/// <para>
/// The stage follows the window's width: <see cref="SidebarStage.Expanded"/> at
/// <see cref="ExpandedThreshold"/> and wider, <see cref="SidebarStage.Compact"/> at
/// <see cref="CompactThreshold"/> and wider, <see cref="SidebarStage.Hidden"/> below. The window
/// is the <c>StandardsWindow</c> frame the sidebar sits in, or else its top level.
/// </para>
/// <para>
/// <see cref="Toggle"/> (the window's sidebar button) collapses an expanded sidebar to the
/// icon rail and back, and opens a compact or hidden one as an overlay over the content, on
/// glass. The user's choice holds until the window next crosses a threshold. The overlay closes
/// on choosing a row, on Escape, on a click outside it and on the button.
/// </para>
/// <para>
/// A toggle animates the width over <c>MotionNormal</c> and the labels fade over
/// <c>MotionFast</c>; a stage change from resizing applies at once. Motion is instant under
/// reduce motion.
/// </para>
/// </remarks>
public class Sidebar : ListBox
{
    /// <summary>The default <see cref="ExpandedThreshold"/>: 1008 px.</summary>
    public const double DefaultExpandedThreshold = 1008;

    /// <summary>The default <see cref="CompactThreshold"/>: 640 px.</summary>
    public const double DefaultCompactThreshold = 640;

    public static readonly StyledProperty<double> ExpandedThresholdProperty =
        AvaloniaProperty.Register<Sidebar, double>(nameof(ExpandedThreshold), DefaultExpandedThreshold);

    public static readonly StyledProperty<double> CompactThresholdProperty =
        AvaloniaProperty.Register<Sidebar, double>(nameof(CompactThreshold), DefaultCompactThreshold);

    public static readonly DirectProperty<Sidebar, SidebarStage> StageProperty =
        AvaloniaProperty.RegisterDirect<Sidebar, SidebarStage>(nameof(Stage), s => s.Stage);

    public static readonly DirectProperty<Sidebar, bool> IsOverlayOpenProperty =
        AvaloniaProperty.RegisterDirect<Sidebar, bool>(nameof(IsOverlayOpen), s => s.IsOverlayOpen);

    // The width taken in layout and the width drawn (wider while the overlay is out); both animate.
    internal static readonly StyledProperty<double> LayoutWidthProperty =
        AvaloniaProperty.Register<Sidebar, double>("LayoutWidth", SidebarTheme.EdgeGap + SidebarTheme.ExpandedWidth);

    internal static readonly StyledProperty<double> PaneWidthProperty =
        AvaloniaProperty.Register<Sidebar, double>("PaneWidth", SidebarTheme.EdgeGap + SidebarTheme.ExpandedWidth);

    // The icon rail's width at the current density, from the theme.
    internal static readonly StyledProperty<double> RailWidthProperty =
        AvaloniaProperty.Register<Sidebar, double>("RailWidth", SidebarTheme.Normal.Rail);

    // True on the sidebar and its rows while only the icon rail shows (labels hidden).
    internal static readonly AttachedProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.RegisterAttached<Sidebar, Control, bool>("IsCollapsed", inherits: true);

    // Marks the window's sidebar button, so pressing it does not count as a click outside the overlay.
    internal static readonly AttachedProperty<bool> IsToggleProperty =
        AvaloniaProperty.RegisterAttached<Sidebar, Control, bool>("IsToggle");

    private SidebarStage _auto = SidebarStage.Expanded;
    private SidebarStage? _userStage;
    private SidebarStage _stage = SidebarStage.Expanded;
    private bool _isOverlayOpen;
    private Visual? _widthSource;
    private TopLevel? _topLevel;
    private Control? _surface;
    private Control? _scroller;

    static Sidebar()
    {
        AffectsMeasure<Sidebar>(LayoutWidthProperty, PaneWidthProperty);
    }

    public Sidebar()
    {
        AddHandler(PointerReleasedEvent, OnRowReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(KeyDownEvent, OnRowKeyDown, RoutingStrategies.Bubble, handledEventsToo: true);
        UpdateStates();
    }

    /// <summary>The window width at and above which the sidebar is expanded (default 1008).</summary>
    public double ExpandedThreshold
    {
        get => GetValue(ExpandedThresholdProperty);
        set => SetValue(ExpandedThresholdProperty, value);
    }

    /// <summary>The window width at and above which the sidebar is the icon rail, below <see cref="ExpandedThreshold"/> (default 640).</summary>
    public double CompactThreshold
    {
        get => GetValue(CompactThresholdProperty);
        set => SetValue(CompactThresholdProperty, value);
    }

    /// <summary>The current stage: from the window width, or the user's <see cref="Toggle"/> until the width next crosses a threshold.</summary>
    public SidebarStage Stage
    {
        get => _stage;
        private set => SetAndRaise(StageProperty, ref _stage, value);
    }

    /// <summary>Whether the expanded sidebar is shown over the content (only in the compact and hidden stages).</summary>
    public bool IsOverlayOpen
    {
        get => _isOverlayOpen;
        private set => SetAndRaise(IsOverlayOpenProperty, ref _isOverlayOpen, value);
    }

    /// <summary>
    /// The user's show/hide action: collapses an expanded sidebar to the icon rail and expands it
    /// again; in the compact and hidden stages opens or closes the overlay.
    /// </summary>
    public void Toggle()
    {
        if (IsOverlayOpen)
            SetOverlay(false);
        else if (Stage == SidebarStage.Expanded)
        {
            _userStage = SidebarStage.Compact;
            Apply(animate: true);
        }
        else if (_userStage is not null)
        {
            _userStage = null;
            Apply(animate: true);
        }
        else
            SetOverlay(true);
    }

    // The window width the stage was last taken from (for tests).
    internal double WindowWidth { get; private set; }

    protected override Type StyleKeyOverride => typeof(Sidebar);

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        // Headers are their own containers, so the list never wraps them in a selectable row.
        if (item is SidebarHeader)
        {
            recycleKey = null;
            return false;
        }
        return NeedsContainer<SidebarItem>(item, out recycleKey);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new SidebarItem();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _surface = e.NameScope.Find<Control>(SidebarTheme.OverlaySurfacePart);
        _scroller = e.NameScope.Find<Control>("PART_ScrollViewer");
        UpdateParts();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        // Inside a StandardsWindow the window is its frame (without the shadow margin); else the top level.
        _widthSource = this.GetVisualAncestors().OfType<SquircleBorder>().FirstOrDefault(b => b.Name == StandardsWindowTheme.FramePart)
                       ?? (Visual?)_topLevel;
        if (_widthSource is not null)
            _widthSource.PropertyChanged += OnWidthSourceChanged;
        UpdateFromWidth();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        SetOverlay(false, animate: false);
        if (_widthSource is not null)
            _widthSource.PropertyChanged -= OnWidthSourceChanged;
        _widthSource = null;
        _topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ExpandedThresholdProperty || change.Property == CompactThresholdProperty)
            UpdateFromWidth();
        else if (change.Property == RailWidthProperty)
            Apply(animate: false);
        else if (change.Property == PaneWidthProperty || change.Property == LayoutWidthProperty)
            UpdateParts();
    }

    // Choosing a row with the keyboard closes the overlay, as a click does (the row may have handled the key).
    private void OnRowKeyDown(object? sender, KeyEventArgs e)
    {
        if (IsOverlayOpen && e.Key is Key.Enter or Key.Space && e.Source is Visual source
            && source.FindAncestorOfType<SidebarItem>(includeSelf: true) is not null)
        {
            SetOverlay(false);
            e.Handled = true;
        }
    }

    // The template is measured and drawn at the pane width, but takes only the layout width:
    // while the overlay is out it extends over the content beside the sidebar.
    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = base.MeasureOverride(new Size(GetValue(PaneWidthProperty), availableSize.Height));
        return new Size(GetValue(LayoutWidthProperty), desired.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        base.ArrangeOverride(new Size(GetValue(PaneWidthProperty), finalSize.Height));
        return finalSize;
    }

    private void OnWidthSourceChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty || e.Property == TopLevel.ClientSizeProperty)
            UpdateFromWidth();
    }

    private void UpdateFromWidth()
    {
        var width = _widthSource switch
        {
            TopLevel top => top.ClientSize.Width,
            { } visual => visual.Bounds.Width,
            _ => 0,
        };
        // Not laid out yet: keep the current stage.
        if (width <= 0)
            return;
        WindowWidth = width;
        var auto = width >= ExpandedThreshold ? SidebarStage.Expanded
            : width >= CompactThreshold ? SidebarStage.Compact
            : SidebarStage.Hidden;
        if (auto == _auto && _stage == (_userStage ?? auto))
            return;

        // Crossing a threshold ends the user's choice and closes the overlay, at once.
        _auto = auto;
        _userStage = null;
        SetOverlay(false, animate: false);
        Apply(animate: false);
    }

    private void SetOverlay(bool open, bool animate = true)
    {
        if (open && Stage == SidebarStage.Expanded)
            open = false;
        if (open == IsOverlayOpen)
            return;
        IsOverlayOpen = open;
        if (_topLevel is not null)
        {
            if (open)
            {
                _topLevel.AddHandler(PointerPressedEvent, OnTopLevelPointerPressed, RoutingStrategies.Tunnel);
                _topLevel.AddHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel);
            }
            else
            {
                _topLevel.RemoveHandler(PointerPressedEvent, OnTopLevelPointerPressed);
                _topLevel.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
            }
        }
        Apply(animate);

        // Opened, the keyboard goes to the chosen row (or the first), so arrows move through the rows.
        if (open)
            (ContainerFromIndex(Math.Max(SelectedIndex, 0)) as SidebarItem)?.Focus();
    }

    // Sets the stage and the widths it takes; without animation the widths jump to their values.
    private void Apply(bool animate)
    {
        Stage = _userStage ?? _auto;
        var rail = GetValue(RailWidthProperty);
        var expanded = SidebarTheme.EdgeGap + SidebarTheme.ExpandedWidth;
        var layout = Stage switch
        {
            SidebarStage.Expanded => expanded,
            SidebarStage.Compact => SidebarTheme.EdgeGap + rail,
            _ => 0,
        };
        var pane = IsOverlayOpen ? expanded : layout;

        if (!animate)
            SetValue(TransitionsProperty, null);
        SetValue(LayoutWidthProperty, layout);
        SetValue(PaneWidthProperty, pane);
        if (!animate)
            ClearValue(TransitionsProperty);
        UpdateStates();
    }

    private void UpdateStates()
    {
        SetValue(IsCollapsedProperty, Stage != SidebarStage.Expanded && !IsOverlayOpen);
        PseudoClasses.Set(":expanded", Stage == SidebarStage.Expanded);
        PseudoClasses.Set(":compact", Stage == SidebarStage.Compact);
        PseudoClasses.Set(":hidden", Stage == SidebarStage.Hidden);
        PseudoClasses.Set(":overlay", IsOverlayOpen);
    }

    // The glass shows while the sidebar is drawn wider than it lays out (the overlay, and while
    // it slides back in); the rows are hidden once nothing of them shows, so they take no focus.
    private void UpdateParts()
    {
        var pane = GetValue(PaneWidthProperty);
        if (_surface is not null)
            _surface.IsVisible = pane > GetValue(LayoutWidthProperty) + 0.5;
        if (_scroller is not null)
            _scroller.IsVisible = pane > SidebarTheme.EdgeGap + 0.5;
    }

    private void OnRowReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (IsOverlayOpen && e.InitialPressMouseButton == MouseButton.Left && e.Source is Visual source
            && source.FindAncestorOfType<SidebarItem>(includeSelf: true) is not null)
            SetOverlay(false);
    }

    // A press outside the overlay closes it and is not passed on, as a light-dismiss popup does.
    private void OnTopLevelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Visual source || this.IsVisualAncestorOf(source) || source == this)
            return;
        if (source.GetSelfAndVisualAncestors().Any(v => v is Control c && c.GetValue(IsToggleProperty)))
            return;
        SetOverlay(false);
        e.Handled = true;
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        SetOverlay(false);
        e.Handled = true;
    }
}

/// <summary>
/// A sidebar row: an <see cref="Icon"/> geometry (from <c>Icons</c>) and a label (the content).
/// In the icon rail only the icon shows, and the label is the row's tooltip.
/// </summary>
public class SidebarItem : ListBoxItem
{
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<SidebarItem, Geometry?>(nameof(Icon));

    static SidebarItem()
    {
        ToolTip.PlacementProperty.OverrideDefaultValue<SidebarItem>(PlacementMode.Right);
    }

    /// <summary>The row's icon, drawn in a 256 x 256 design box as <c>Icons</c> are.</summary>
    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(SidebarItem);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == Sidebar.IsCollapsedProperty || change.Property == ContentProperty)
        {
            var collapsed = GetValue(Sidebar.IsCollapsedProperty);
            PseudoClasses.Set(":collapsed", collapsed);
            ToolTip.SetTip(this, collapsed ? Content : null);
        }
    }
}

/// <summary>
/// A section header between sidebar rows: caption text in the secondary colour. It is not
/// selectable and takes no focus. In the icon rail its text hides and its height stays, so the
/// sections stay apart.
/// </summary>
public class SidebarHeader : ContentControl
{
    static SidebarHeader()
    {
        FocusableProperty.OverrideDefaultValue<SidebarHeader>(false);
    }

    protected override Type StyleKeyOverride => typeof(SidebarHeader);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == Sidebar.IsCollapsedProperty)
            PseudoClasses.Set(":collapsed", change.GetNewValue<bool>());
    }
}
