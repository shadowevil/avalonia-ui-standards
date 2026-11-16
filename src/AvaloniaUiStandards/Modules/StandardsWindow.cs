using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The library's window: borderless and transparent, drawn as a squircle frame (radius 16, 1 px
/// border, soft shadow) whose background is the backdrop wash. A title band holds the window
/// buttons, the <see cref="Window.Title"/> and the <see cref="Toolbar"/>; below it are the menu row
/// (in the Windows layout, for the window's <see cref="NativeMenu"/>), the optional
/// <see cref="ToolbarBand"/> across the window, the optional <see cref="Sidebar"/> on the window background, the glass content panel holding
/// <see cref="ContentControl.Content"/> (scrolled by the panel, see <see cref="IsContentScrollable"/>), the optional
/// <see cref="Inspector"/> and <see cref="BottomPanel"/> glass panels beside and under it, and the optional <see cref="StatusBar"/>. An optional <see cref="Backdrop"/> sits behind them all.
/// </summary>
/// <remarks>
/// Drag the window by the title band (double-click maximises or restores; in the macOS layout it
/// follows the macOS setting for a title bar double-click, on macOS), resize it from the frame's edges and corners.
/// The window buttons follow what the window can do (<see cref="Window.CanMinimize"/>, <see cref="Window.CanMaximize"/>).
/// On macOS the window has full screen (<see cref="ToggleFullScreen"/>): the title band and the menu row hide above the
/// top edge and slide in when the pointer reaches it.
/// Maximised, or where the OS gives no transparency, the frame fills the window with square
/// corners and no shadow, except on macOS, where the system keeps its shadow and rounds the
/// corners with its own mask. On macOS the window is a native resizable window with its title bar
/// hidden: the system draws the shadow and handles resizing, and the window is exactly the frame.
/// </remarks>
public class StandardsWindow : Window, IFrameTickSource
{
    public static readonly StyledProperty<object?> ToolbarProperty =
        AvaloniaProperty.Register<StandardsWindow, object?>(nameof(Toolbar));

    public static readonly StyledProperty<object?> ToolbarBandProperty =
        AvaloniaProperty.Register<StandardsWindow, object?>(nameof(ToolbarBand));

    public static readonly StyledProperty<object?> SidebarProperty =
        AvaloniaProperty.Register<StandardsWindow, object?>(nameof(Sidebar));

    public static readonly StyledProperty<object?> StatusBarProperty =
        AvaloniaProperty.Register<StandardsWindow, object?>(nameof(StatusBar));

    public static readonly StyledProperty<object?> InspectorProperty =
        AvaloniaProperty.Register<StandardsWindow, object?>(nameof(Inspector));

    public static readonly StyledProperty<object?> BottomPanelProperty =
        AvaloniaProperty.Register<StandardsWindow, object?>(nameof(BottomPanel));

    public static readonly StyledProperty<bool> IsInspectorOpenProperty =
        AvaloniaProperty.Register<StandardsWindow, bool>(nameof(IsInspectorOpen), defaultValue: true,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> InspectorWidthProperty =
        AvaloniaProperty.Register<StandardsWindow, double>(nameof(InspectorWidth), InspectorDefaultWidth,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay,
            coerce: (_, v) => double.IsFinite(v) ? Math.Clamp(v, InspectorMinWidth, InspectorMaxWidth) : InspectorDefaultWidth);

    public static readonly StyledProperty<bool> IsBottomPanelOpenProperty =
        AvaloniaProperty.Register<StandardsWindow, bool>(nameof(IsBottomPanelOpen), defaultValue: true,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> BottomPanelHeightProperty =
        AvaloniaProperty.Register<StandardsWindow, double>(nameof(BottomPanelHeight), BottomPanelDefaultHeight,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay,
            coerce: (_, v) => double.IsFinite(v) ? Math.Max(v, BottomPanelMinHeight) : BottomPanelDefaultHeight);

    /// <summary>The inspector's smallest, default and largest width, in px.</summary>
    internal const double InspectorMinWidth = 200, InspectorDefaultWidth = 240, InspectorMaxWidth = 400;

    /// <summary>The bottom panel's smallest and default height, in px; it shows at most (content area height - 8) / 2, so the content panel is at least as tall.</summary>
    internal const double BottomPanelMinHeight = 96, BottomPanelDefaultHeight = 200;

    /// <summary>
    /// The window width from which the inspector shows docked: the sidebar's expanded threshold plus the inspector's
    /// default width (1008 + 240). Below it the inspector shows only as an overlay.
    /// </summary>
    internal const double InspectorThreshold = Modules.Sidebar.DefaultExpandedThreshold + InspectorDefaultWidth;

    public static readonly StyledProperty<object?> BackdropProperty =
        AvaloniaProperty.Register<StandardsWindow, object?>(nameof(Backdrop));

    public static readonly StyledProperty<bool> IsContentScrollableProperty =
        AvaloniaProperty.Register<StandardsWindow, bool>(nameof(IsContentScrollable), defaultValue: true);

    public static readonly StyledProperty<WindowButtonStyle> WindowButtonStyleProperty =
        AvaloniaProperty.Register<StandardsWindow, WindowButtonStyle>(nameof(WindowButtonStyle));

    public static readonly StyledProperty<WindowLayoutMode> LayoutModeProperty =
        AvaloniaProperty.Register<StandardsWindow, WindowLayoutMode>(nameof(LayoutMode));

    public static readonly DirectProperty<StandardsWindow, WindowLayoutMode> ResolvedLayoutModeProperty =
        AvaloniaProperty.RegisterDirect<StandardsWindow, WindowLayoutMode>(nameof(ResolvedLayoutMode), w => w.ResolvedLayoutMode);

    public static readonly StyledProperty<bool> HighRefreshRateProperty =
        AvaloniaProperty.Register<StandardsWindow, bool>(nameof(HighRefreshRate));

    // The theme's effective reduce-motion setting, fed by StandardsTheme.
    internal static readonly StyledProperty<bool> ReduceMotionProperty =
        AvaloniaProperty.Register<StandardsWindow, bool>("ReduceMotion");

    public static readonly DirectProperty<StandardsWindow, Rect> ContentPanelBoundsProperty =
        AvaloniaProperty.RegisterDirect<StandardsWindow, Rect>(nameof(ContentPanelBounds), w => w.ContentPanelBounds);

    public static readonly DirectProperty<StandardsWindow, BackdropRegion> BackdropVisibleRegionProperty =
        AvaloniaProperty.RegisterDirect<StandardsWindow, BackdropRegion>(nameof(BackdropVisibleRegion), w => w.BackdropVisibleRegion);

    private WindowButtons? _buttons;
    private Image? _titleIcon;
    private WindowLayoutMode _resolvedLayoutMode = WindowLayout.Resolve(WindowLayoutMode.Automatic, OperatingSystem.IsMacOS());
    private Panel? _menuRowPart;
    private readonly MenuShortcuts _shortcuts;
    private bool _opened;
    private bool _closed;
    private readonly MenuRow _menuRow = new()
    {
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        Margin = new Thickness(Spacing.L, 0),
    };
    private SquircleBorder? _frame;
    private Control? _panel;
    private WindowTop? _top;
    private WindowState _stateBeforeFullScreen;
    private bool _revealed;
    private bool _alwaysRevealed;
    private bool _hidePending;
    private int _hideGeneration;
    private double _lastPointerY = double.PositiveInfinity;
    private bool _runsOnMacOS = OperatingSystem.IsMacOS();
    // The panel and its containers up to the frame's layer panel: any of them moving moves the panel.
    private List<Visual> _panelChain = [];
    private Rect _contentPanelBounds;
    private BackdropRegion _backdropVisibleRegion = BackdropRegion.Empty;
    private Control? _backdrop;
    // The backdrop part and region the clip was last set for: null until the first, and again after a new template.
    private Control? _clippedBackdrop;
    private BackdropRegion? _clippedRegion;
    private IDisposable? _regionsListener;
    private readonly FrameTicker _ticker;
    private EventHandler<FrameTickEventArgs>? _frameTick;
    private PaneLayout? _panes;
    private Control? _inspectorPanel;
    private PaneSplitter? _inspectorSplitter;
    private PaneSplitter? _bottomSplitter;
    // Below the inspector threshold: the inspector shows only as the overlay.
    private bool _narrow;
    private bool _inspectorOverlay;
    private Control? _focusBeforeOverlay;
    private double _dragStartSize;
    // The asked bottom panel height at a drag's start, which Escape puts back.
    private double _dragStartAsked;

    public StandardsWindow()
        : this(nativeFrame: null)
    {
    }

    /// <param name="nativeFrame">Whether to use the native macOS frame; by OS and platform when null.</param>
    internal StandardsWindow(bool? nativeFrame)
    {
        _shortcuts = new MenuShortcuts(this);
        // Not stored: the window's own event handlers keep the guard alive.
        _ = new MenuAltGuard(this, () => IsMenuRowShown);
        _ticker = new FrameTicker(new Scheduler(this), (time, elapsed) => _frameTick?.Invoke(this, new FrameTickEventArgs(time, elapsed)));
        HasNativeFrame = nativeFrame ?? UsesNativeFrame(OperatingSystem.IsMacOS(), TryGetPlatformHandle()?.HandleDescriptor);
        WindowDecorations = DecorationsFor(HasNativeFrame);
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        PseudoClasses.Set(":native-frame", HasNativeFrame);
        // The full screen reveal follows the pointer wherever it is in the window, whoever handles the move.
        AddHandler(PointerMovedEvent, OnRevealPointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        // Raised as the window starts to show, before its size goes to the platform.
        AddHandler(WindowOpenedEvent, (_, _) => FitOpeningSize());
        // The menu row's menu bar opening (Alt, F10, a click) reveals the rows; closing lets them hide.
        _menuRow.Opened += (_, _) =>
        {
            if (WindowState == WindowState.FullScreen)
                SetRevealed(true);
        };
        _menuRow.Closed += (_, _) =>
        {
            if (WindowState == WindowState.FullScreen && _revealed)
                ScheduleHide();
        };
        UpdateStates();
    }

    /// <summary>
    /// Whether the window uses the native macOS frame: on macOS, in a native macOS window (not,
    /// for example, a headless one). The system then draws the shadow and resizes the window, and
    /// the library draws no shadow, margin or resize grips. Elsewhere the library draws them.
    /// </summary>
    internal static bool UsesNativeFrame(bool macOS, string? platformHandle) => macOS && platformHandle == "NSWindow";

    /// <summary>
    /// The native frame is a titled, resizable macOS window whose title bar and buttons Avalonia
    /// hides (<see cref="WindowDecorations.BorderOnly"/>): the system window shadow, derived from
    /// the frame's shape, and native resizing from the edges and corners. Elsewhere the window is
    /// borderless (<see cref="WindowDecorations.None"/>).
    /// </summary>
    internal static WindowDecorations DecorationsFor(bool nativeFrame) =>
        nativeFrame ? Avalonia.Controls.WindowDecorations.BorderOnly : Avalonia.Controls.WindowDecorations.None;

    /// <summary>Whether this window uses the native macOS frame (see <see cref="UsesNativeFrame"/>).</summary>
    internal bool HasNativeFrame { get; }

    // Window reports typeof(Window) as its style key; this window (and app subclasses of it) uses its own theme.
    protected override Type StyleKeyOverride => typeof(StandardsWindow);

    /// <summary>Content of the title band, after the title (a unified toolbar).</summary>
    public object? Toolbar
    {
        get => GetValue(ToolbarProperty);
        set => SetValue(ToolbarProperty, value);
    }

    /// <summary>
    /// Optional content of the band directly below the title band, across the window on the window
    /// background (normally a <see cref="Modules.ToolbarBand"/>); shown only when set, with the
    /// sidebar and the glass panel below it. In the macOS layout its empty area drags the window, as a
    /// unified toolbar's does; in the Windows layout it does not, as a command bar's does not.
    /// </summary>
    public object? ToolbarBand
    {
        get => GetValue(ToolbarBandProperty);
        set => SetValue(ToolbarBandProperty, value);
    }

    /// <summary>Optional content on the left, on the window background.</summary>
    public object? Sidebar
    {
        get => GetValue(SidebarProperty);
        set => SetValue(SidebarProperty, value);
    }

    /// <summary>Optional content of the status bar at the bottom: a <see cref="Modules.StatusBar"/> Module, or any content.</summary>
    public object? StatusBar
    {
        get => GetValue(StatusBarProperty);
        set => SetValue(StatusBarProperty, value);
    }

    /// <summary>
    /// Optional content of the inspector: a second glass panel to the right of the content panel, the full height of
    /// the content area, <see cref="InspectorWidth"/> wide, with a resize handle in the gap between them (normally a
    /// <see cref="PanelStack"/>). It shows docked while <see cref="IsInspectorOpen"/> in a window at least 1248 px wide;
    /// in a narrower window it is hidden from the layout, and <see cref="ToggleInspector"/> shows it as an overlay at
    /// the right edge. The panel pads its content by 12 px and gives it the panel's size.
    /// </summary>
    public object? Inspector
    {
        get => GetValue(InspectorProperty);
        set => SetValue(InspectorProperty, value);
    }

    /// <summary>
    /// Optional content of the bottom panel: a glass panel directly under the content panel, <see cref="BottomPanelHeight"/>
    /// high, with a resize handle in the gap between them; shown while <see cref="IsBottomPanelOpen"/>. The panel pads its
    /// content by 12 px and gives it the panel's size.
    /// </summary>
    public object? BottomPanel
    {
        get => GetValue(BottomPanelProperty);
        set => SetValue(BottomPanelProperty, value);
    }

    /// <summary>
    /// Whether the inspector shows docked (default true) where the window is wide enough. Dragging its resize handle
    /// below the inspector's minimum width (200) sets it false. Two-way, so an app can keep it.
    /// </summary>
    public bool IsInspectorOpen
    {
        get => GetValue(IsInspectorOpenProperty);
        set => SetValue(IsInspectorOpenProperty, value);
    }

    /// <summary>The inspector's width: 240 by default, kept between 200 and 400. Two-way, so an app can keep it.</summary>
    public double InspectorWidth
    {
        get => GetValue(InspectorWidthProperty);
        set => SetValue(InspectorWidthProperty, value);
    }

    /// <summary>
    /// Whether the bottom panel shows (default true). Dragging its resize handle below the panel's minimum height (96)
    /// sets it false. Two-way, so an app can keep it.
    /// </summary>
    public bool IsBottomPanelOpen
    {
        get => GetValue(IsBottomPanelOpenProperty);
        set => SetValue(IsBottomPanelOpenProperty, value);
    }

    /// <summary>
    /// The bottom panel's height: 200 by default, at least 96; it shows at most (content area height - 8) / 2, and hides while the content area is shorter than 200. Two-way,
    /// so an app can keep it.
    /// </summary>
    public double BottomPanelHeight
    {
        get => GetValue(BottomPanelHeightProperty);
        set => SetValue(BottomPanelHeightProperty, value);
    }

    /// <summary>
    /// Shows or hides the inspector: where the window is wide enough to dock it, toggles <see cref="IsInspectorOpen"/>;
    /// in a narrower window, opens or closes it as an overlay at the right edge. Does nothing without an
    /// <see cref="Inspector"/>. An <see cref="InspectorMenuItem"/> calls it.
    /// </summary>
    public void ToggleInspector()
    {
        if (Inspector is null)
            return;
        if (_narrow)
            SetInspectorOverlay(!_inspectorOverlay, restoreFocus: true, NavigationMethod.Tab);
        else
            IsInspectorOpen = !IsInspectorOpen;
    }

    /// <summary>
    /// Shows or hides the bottom panel (<see cref="IsBottomPanelOpen"/>); does nothing without a <see cref="BottomPanel"/>,
    /// or while the content area is too short for it (the panel cannot show, so a toggle would only lose the kept
    /// setting). A <see cref="BottomPanelMenuItem"/> calls it.
    /// </summary>
    public void ToggleBottomPanel()
    {
        if (CanToggleBottomPanel)
            IsBottomPanelOpen = !IsBottomPanelOpen;
    }

    /// <summary>Whether the content area is tall enough for the bottom panel.</summary>
    internal bool HasBottomRoom => _panes?.HasBottomRoom ?? true;

    /// <summary>Whether <see cref="ToggleBottomPanel"/> acts: with a bottom panel and room for it.</summary>
    internal bool CanToggleBottomPanel => BottomPanel is not null && HasBottomRoom;

    /// <summary>Where the inspector shows now.</summary>
    internal InspectorPlacement InspectorPlacement => Inspector is null ? InspectorPlacement.None
        : _narrow ? _inspectorOverlay ? InspectorPlacement.Overlay : InspectorPlacement.None
        : IsInspectorOpen ? InspectorPlacement.Docked : InspectorPlacement.None;

    /// <summary>Whether the inspector shows, docked or as the overlay.</summary>
    internal bool IsInspectorShown => InspectorPlacement != InspectorPlacement.None;

    /// <summary>Whether the bottom panel shows.</summary>
    internal bool IsBottomPanelShown => BottomPanel is not null && IsBottomPanelOpen && (_panes?.HasBottomRoom ?? true);

    /// <summary>Whether the inspector is open as the overlay (a narrow window).</summary>
    internal bool IsInspectorOverlayOpen => _inspectorOverlay;

    /// <summary>Whether the window is narrower than the inspector threshold.</summary>
    internal bool IsInspectorNarrow => _narrow;

    /// <summary>Raised when what the inspector or the bottom panel shows changes.</summary>
    internal event Action? PanesChanged;

    /// <summary>
    /// Optional content drawn over the frame's wash and behind everything else in the frame (the
    /// title band, sidebar, status bar and glass panel), inside the frame's shape. The glass
    /// panel blurs and refracts it. Empty by default.
    /// </summary>
    public object? Backdrop
    {
        get => GetValue(BackdropProperty);
        set => SetValue(BackdropProperty, value);
    }

    /// <summary>
    /// Whether the glass content panel scrolls its content vertically (default true): the content
    /// scrolls edge to edge under the panel's shape, with the panel's 12 px padding inside the
    /// scrolled content and the scroll bar and scroll shadows at the panel's edge. Set false for
    /// content that scrolls itself; the content then gets the panel's size, inside the padding.
    /// </summary>
    public bool IsContentScrollable
    {
        get => GetValue(IsContentScrollableProperty);
        set => SetValue(IsContentScrollableProperty, value);
    }

    /// <summary>
    /// Where the glass content panel is, in the coordinates of the <see cref="Backdrop"/> (the
    /// frame's interior), so a backdrop can place things relative to the panel. Updated, with a
    /// property change, whenever layout moves or resizes the panel (window size, density,
    /// maximising).
    /// </summary>
    public Rect ContentPanelBounds
    {
        get => _contentPanelBounds;
        private set => SetAndRaise(ContentPanelBoundsProperty, ref _contentPanelBounds, value);
    }

    /// <summary>
    /// The part of the <see cref="Backdrop"/> that can be seen or that glass reads, in the backdrop's coordinates: the
    /// frame's interior less the opaque regions above the backdrop (<see cref="OpaqueRegion"/>, and solid glass with an
    /// opaque tint), each shrunk by how far glass reads beyond what it shows. The backdrop is clipped to it. Updated,
    /// with a property change, when it changes, so an animated backdrop can skip work that cannot be seen.
    /// </summary>
    public BackdropRegion BackdropVisibleRegion
    {
        get => _backdropVisibleRegion;
        private set => SetAndRaise(BackdropVisibleRegionProperty, ref _backdropVisibleRegion, value);
    }

    /// <summary>
    /// Whether <see cref="FrameTick"/> runs at the display rate (true) or at 30 ticks per second
    /// (false, the default). Set it in code, per window, or app-wide through
    /// <c>StandardsTheme.HighRefreshRate</c>; a value set on the window wins. Only continuous
    /// animation driven by <see cref="FrameTick"/> is paced; interactive rendering (scrolling,
    /// hover and press fades, typing) always runs at the display rate.
    /// </summary>
    public bool HighRefreshRate
    {
        get => GetValue(HighRefreshRateProperty);
        set => SetValue(HighRefreshRateProperty, value);
    }

    /// <summary>
    /// Frame ticks for continuous animation (looping or ambient, for example a
    /// <see cref="Backdrop"/>'s): 30 per second, or the display rate with
    /// <see cref="HighRefreshRate"/>. Update the animation in the handler; it renders on that frame.
    /// Ticks run while there are handlers, stop while motion is reduced
    /// (<c>StandardsTheme.ReduceMotion</c>) and pause while the window is inactive or minimised;
    /// the first tick after a pause has a zero <see cref="FrameTickEventArgs.Elapsed"/>.
    /// </summary>
    public event EventHandler<FrameTickEventArgs>? FrameTick
    {
        add
        {
            _frameTick += value;
            UpdateTicker();
        }
        remove
        {
            _frameTick -= value;
            UpdateTicker();
        }
    }

    /// <summary>
    /// Which OS's window layout the window shows (default <see cref="WindowLayoutMode.Automatic"/>, the
    /// OS's own): the window buttons (while <see cref="WindowButtonStyle"/> is Auto), where the window's
    /// <see cref="NativeMenu"/> shows, whether the toolbar band drags the window and what a title band
    /// double-click does. The native frame and the keyboard shortcuts follow the OS the app runs on.
    /// </summary>
    public WindowLayoutMode LayoutMode
    {
        get => GetValue(LayoutModeProperty);
        set => SetValue(LayoutModeProperty, value);
    }

    /// <summary>
    /// The layout in use (read-only): <see cref="LayoutMode"/> with Automatic resolved for this OS, so
    /// never <see cref="WindowLayoutMode.Automatic"/>. Raises a property change when <see cref="LayoutMode"/> changes it.
    /// </summary>
    public WindowLayoutMode ResolvedLayoutMode => _resolvedLayoutMode;

    /// <summary>The pointer's distance from the top edge, in px, at which the hidden rows slide in in full screen.</summary>
    internal const double RevealEdge = 2;

    /// <summary>How long the revealed rows stay after the pointer leaves them, in full screen.</summary>
    internal static readonly TimeSpan HideDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>How far a native macOS window that opens larger than the screen's working area stays from its edges, in px.</summary>
    internal const double OpeningInset = Spacing.L;

    /// <summary>
    /// The size a window opens at, never larger than the screen's working area (<paramref name="workingArea"/>).
    /// On the native macOS frame a side that reaches the working area becomes the working area's less
    /// <see cref="OpeningInset"/> at each edge; on a drawn frame a side that exceeds it becomes the working area.
    /// A side that fits stays.
    /// </summary>
    /// <remarks>
    /// Avalonia.Native 12.1.6 counts a native window as maximised whenever its frame equals the screen's visible frame.
    /// macOS squeezes a window that opens larger than the working area to exactly that frame, so the window started out
    /// maximised natively while reading Normal, with no frame to restore to, and maximise and restore then acted backwards.
    /// The drawn frame has no such case, so a window as large as the working area is left as it is.
    /// </remarks>
    internal static Size FitOpeningSize(Size requested, Size workingArea, bool nativeMacOSFrame)
    {
        if (nativeMacOSFrame)
            return new Size(FitInset(requested.Width, workingArea.Width), FitInset(requested.Height, workingArea.Height));
        return new Size(Math.Min(requested.Width, workingArea.Width), Math.Min(requested.Height, workingArea.Height));
    }

    private static double FitInset(double side, double area)
    {
        if (side < area)
            return side;
        return Math.Max(0, area - 2 * OpeningInset);
    }

    // A normal window that would open larger than the working area opens inside it, and is centred again when its
    // start location asks for it (the platform may place a window before or after this resize, so the position is set here).
    private void FitOpeningSize()
    {
        if (WindowState != WindowState.Normal || (Screens.ScreenFromWindow(this) ?? Screens.Primary) is not { } screen)
            return;
        var requested = new Size(double.IsNaN(Width) ? ClientSize.Width : Width, double.IsNaN(Height) ? ClientSize.Height : Height);
        var fitted = FitOpeningSize(requested, screen.WorkingArea.Size.ToSize(screen.Scaling), HasNativeFrame);
        if (fitted.Width == requested.Width && fitted.Height == requested.Height)
            return;
        if (fitted.Width != requested.Width)
            Width = Math.Max(fitted.Width, MinWidth);
        if (fitted.Height != requested.Height)
            Height = Math.Max(fitted.Height, MinHeight);

        var area = screen.WorkingArea;
        var size = new PixelSize((int)Math.Round(Width * screen.Scaling), (int)Math.Round(Height * screen.Scaling));
        PixelRect? centreOn = WindowStartupLocation switch
        {
            WindowStartupLocation.CenterScreen => area,
            WindowStartupLocation.CenterOwner when Owner is Window owner =>
                new PixelRect(owner.Position, PixelSize.FromSize(owner.Bounds.Size, owner.RenderScaling)),
            _ => null,
        };
        if (centreOn is not { } target)
            return;
        var x = target.X + (target.Width - size.Width) / 2;
        var y = target.Y + (target.Height - size.Height) / 2;
        Position = new PixelPoint(
            Math.Clamp(x, area.X, Math.Max(area.X, area.Right - size.Width)),
            Math.Clamp(y, area.Y, Math.Max(area.Y, area.Bottom - size.Height)));
    }

    /// <summary>Whether the app runs on macOS, where the window has full screen (settable for tests).</summary>
    internal bool RunsOnMacOS
    {
        get => _runsOnMacOS;
        set
        {
            _runsOnMacOS = value;
            UpdateStates();
        }
    }

    /// <summary>Runs an action after a delay: the reveal's hide timer (replaceable for tests).</summary>
    internal Action<TimeSpan, Action> RevealDelay { get; set; } = (delay, action) => DispatcherTimer.RunOnce(action, delay);

    /// <summary>Whether macOS keeps its menu bar shown in full screen (replaceable for tests, so they do not read the machine's setting).</summary>
    internal Func<bool> MenuBarStaysInFullScreen { get; set; } = MacWindowSettings.MenuBarVisibleInFullScreen;

    /// <summary>The menu bar's height in points, or null when unknown (replaceable for tests, so they start no native app).</summary>
    internal Func<double?> MenuBarHeightSource { get; set; } = MacWindowSettings.MenuBarHeight;

    /// <summary>Whether the window can enter full screen now, or is in it: on macOS, while it can be maximised.</summary>
    internal bool IsFullScreenAvailable => RunsOnMacOS && (CanMaximize || WindowState == WindowState.FullScreen);

    /// <summary>Whether the hidden rows are revealed (or revealing) in full screen.</summary>
    internal bool IsTopRevealed => _revealed;

    /// <summary>The top of the window: the title band and the menu row above the toolbar band.</summary>
    internal WindowTop? Top => _top;

    /// <summary>
    /// Enters full screen, or leaves it for the state the window had before (normal or maximised). Full screen is a
    /// macOS feature (its own Space, the camera housing handled by the system): elsewhere this does nothing, and so it
    /// does while the window cannot be maximised (<see cref="Window.CanMaximize"/>). The green traffic light, Ctrl+Cmd+F
    /// and a <see cref="FullScreenMenuItem"/> call it.
    /// </summary>
    public void ToggleFullScreen() => ToggleFullScreen(RunsOnMacOS);

    /// <summary>Toggles full screen when <paramref name="macOS"/>; returns whether it did.</summary>
    internal bool ToggleFullScreen(bool macOS)
    {
        if (!macOS)
            return false;
        if (WindowState == WindowState.FullScreen)
            WindowState = _stateBeforeFullScreen;
        else if (CanMaximize)
            WindowState = WindowState.FullScreen;
        else
            return false;
        return true;
    }

    /// <summary>The window shortcuts made from the menu's gestures.</summary>
    internal MenuShortcuts MenuShortcuts => _shortcuts;

    /// <summary>The menu bar of the menu row (shown in the Windows layout when the window has a menu).</summary>
    internal MenuRow MenuRow => _menuRow;

    /// <summary>Whether the menu row shows: the Windows layout, with a menu set on the window.</summary>
    internal bool IsMenuRowShown => ResolvedLayoutMode == WindowLayoutMode.Windows && NativeMenu.GetMenu(this) is not null;

    /// <summary>Traffic lights or caption buttons; by layout mode when <see cref="WindowButtonStyle.Auto"/>.</summary>
    public WindowButtonStyle WindowButtonStyle
    {
        get => GetValue(WindowButtonStyleProperty);
        set => SetValue(WindowButtonStyleProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_buttons is not null)
            _buttons.Clicked -= OnWindowButton;
        _buttons = e.NameScope.Find<WindowButtons>(StandardsWindowTheme.WindowButtonsPart);
        if (_buttons is not null)
            _buttons.Clicked += OnWindowButton;
        if (_frame is not null)
            _frame.PropertyChanged -= OnFramePropertyChanged;
        _frame = e.NameScope.Find<SquircleBorder>(StandardsWindowTheme.FramePart);
        if (_frame is not null)
            _frame.PropertyChanged += OnFramePropertyChanged;
        _backdrop = e.NameScope.Find<Control>(StandardsWindowTheme.BackdropPart);
        ApplyPanesTemplate(e.NameScope);
        ListenToRegions();
        ReleaseTitleIcon();
        _titleIcon = e.NameScope.Find<Image>(StandardsWindowTheme.TitleIconPart);
        UpdateTitleIcon();

        // The panel moves within the frame on resize, density and maximise: each changes the bounds
        // of the panel or of a container around it.
        foreach (var visual in _panelChain)
            visual.PropertyChanged -= OnPanelPropertyChanged;
        _panel = e.NameScope.Find<Control>(StandardsWindowTheme.ContentPanelPart);
        var layers = _frame?.Child;
        _panelChain = _panel is null ? [] : [_panel, .. _panel.GetVisualAncestors().TakeWhile(v => v != layers)];
        foreach (var visual in _panelChain)
            visual.PropertyChanged += OnPanelPropertyChanged;

        // Keyboard focus moving into the hidden rows (Tab, Alt, F10) reveals them; leaving them lets them hide.
        _top = e.NameScope.Find<WindowTop>(StandardsWindowTheme.TopPart);
        if (_top is not null)
        {
            _top.Rows.AddHandler(GotFocusEvent, (_, _) =>
            {
                if (WindowState == WindowState.FullScreen)
                    SetRevealed(true);
            });
            _top.AddHandler(LostFocusEvent, (_, _) =>
            {
                if (WindowState == WindowState.FullScreen && _revealed)
                    ScheduleHide();
            });
        }

        if (e.NameScope.Find<Control>(StandardsWindowTheme.TitleBandPart) is { } band)
            band.PointerPressed += OnTitleBandPressed;
        _menuRowPart?.Children.Clear();
        _menuRowPart = e.NameScope.Find<Panel>(StandardsWindowTheme.MenuRowPart);
        if (_menuRowPart is { } menuRow)
            menuRow.PointerPressed += (_, args) =>
            {
                // Only the row's own empty area drags: the menu titles are the menu's.
                if (args.Source == menuRow)
                    OnTitleBandPressed(menuRow, args);
            };
        if (e.NameScope.Find<ContentPresenter>(StandardsWindowTheme.ToolbarBandPart) is { } toolbarBand)
            toolbarBand.PointerPressed += (_, args) => OnToolbarBandPressed(toolbarBand, args);
        if (e.NameScope.Find<Button>(StandardsWindowTheme.SidebarTogglePart) is { } sidebarToggle)
            sidebarToggle.Click += (_, _) => (Sidebar as Sidebar)?.Toggle();
        foreach (var edge in Enum.GetValues<WindowEdge>())
            if (e.NameScope.Find<ResizeGrip>(StandardsWindowTheme.ResizePart(edge)) is { } grip)
            {
                grip.PointerPressed += (_, args) => OnGripPressed(edge, args);
                grip.IsExcluded = point => IsOverWindowButtons(grip, point);
            }

        UpdateStates();
        UpdateMenuRow();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LayoutModeProperty)
            SetAndRaise(ResolvedLayoutModeProperty, ref _resolvedLayoutMode, WindowLayout.Resolve(LayoutMode, OperatingSystem.IsMacOS()));
        if (change.Property == WindowStateProperty)
            OnWindowStateChanged(change.GetOldValue<WindowState>(), change.GetNewValue<WindowState>());
        if (change.Property == WindowStateProperty || change.Property == WindowButtonStyleProperty
            || change.Property == ActualTransparencyLevelProperty || change.Property == IsActiveProperty
            || change.Property == LayoutModeProperty || change.Property == CanMinimizeProperty
            || change.Property == CanMaximizeProperty)
            UpdateStates();
        if (change.Property == ToolbarBandProperty)
            PseudoClasses.Set(":toolbar-band", change.NewValue is not null);
        if (change.Property == LayoutModeProperty || change.Property == NativeMenu.MenuProperty)
            UpdateMenuRow();
        if (change.Property == ResolvedLayoutModeProperty || change.Property == IconProperty)
            UpdateTitleIcon();
        if (change.Property == SidebarProperty)
            PseudoClasses.Set(":sidebar-module", change.NewValue is Sidebar);
        if (change.Property == StatusBarProperty)
            PseudoClasses.Set(":status-bar-module", change.NewValue is StatusBar);
        // Opening and closing a panel slides it (instant under reduce motion); a new slot or size applies at once.
        if (change.Property == IsInspectorOpenProperty || change.Property == IsBottomPanelOpenProperty)
            UpdatePanes(animate: true);
        else if (change.Property == InspectorProperty || change.Property == BottomPanelProperty
            || change.Property == InspectorWidthProperty || change.Property == BottomPanelHeightProperty)
            UpdatePanes();
        if (change.Property == IsActiveProperty || change.Property == WindowStateProperty
            || change.Property == ReduceMotionProperty || change.Property == HighRefreshRateProperty)
            UpdateTicker();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _opened = true;
        UpdateMenuRow();
    }

    protected override void OnClosed(EventArgs e)
    {
        _ticker.Stop();
        _stripTimer?.Stop();
        _stripTimer = null;
        _closed = true;
        // The menus outlive the window (the Application's for the whole app): let go of them.
        _menuRowPart?.Children.Remove(_menuRow);
        _menuRow.Source = null;
        _menuRow.AppMenu = null;
        _shortcuts.Dispose();
        ReleaseTitleIcon();
        base.OnClosed(e);
    }

    // Whether frame ticks are running now (for tests).
    internal bool IsFrameTickRunning => _ticker.IsRunning;

    // Frame ticks for Blocks that animate continuously (an indeterminate ProgressBar or ProgressRing).
    IDisposable IFrameTickSource.SubscribeFrameTicks(Action<TimeSpan> tick)
    {
        EventHandler<FrameTickEventArgs> handler = (_, e) => tick(e.Elapsed);
        FrameTick += handler;
        return FrameTicks.Subscription(() => FrameTick -= handler);
    }

    // Ticks run while someone listens, motion is not reduced, and the window is active and not minimised.
    private void UpdateTicker()
    {
        _ticker.HighRate = HighRefreshRate;
        if (_frameTick is not null && !GetValue(ReduceMotionProperty) && IsActive && WindowState != WindowState.Minimized)
            _ticker.Start();
        else
            _ticker.Stop();
    }

    // The window's frames (TopLevel.RequestAnimationFrame) and the dispatcher's timers.
    private sealed class Scheduler(TopLevel topLevel) : IFrameScheduler
    {
        public void RequestFrame(Action<TimeSpan> callback) => topLevel.RequestAnimationFrame(callback);

        public void Delay(TimeSpan delay, Action callback) => DispatcherTimer.RunOnce(callback, delay);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && HandleShortcut(e.Key, e.KeyModifiers, OperatingSystem.IsMacOS()))
            e.Handled = true;
        // F10 opens the menu row's menu bar at its first title, as in Windows apps (Alt does so through Avalonia's access keys).
        if (!e.Handled && e.Key == Key.F10 && e.KeyModifiers == KeyModifiers.None && _menuRow.Parent is not null)
        {
            _menuRow.ToggleFromKeyboard();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Shows the menu row in the Windows layout of a window with a menu, with the menu bar in it;
    /// otherwise the row is gone (its menu bar out of the window, so Alt and F10 find no menu) and
    /// nothing below it moves.
    /// </summary>
    private void UpdateMenuRow()
    {
        if (_closed)
            return;
        // The menu's shortcuts work in either layout while the menu is not exported, once the window is open
        // (a window never shown is not kept alive by the Application's menu).
        _shortcuts.Menu = _opened ? NativeMenu.GetMenu(this) : null;
        if (_menuRowPart is null)
            return;
        var shown = IsMenuRowShown;
        _menuRowPart.IsVisible = shown;
        if (shown)
        {
            _menuRow.Source = NativeMenu.GetMenu(this);
            _menuRow.AppMenu = Application.Current is { } app ? NativeMenu.GetMenu(app) : null;
            if (_menuRow.Parent is null)
                _menuRowPart.Children.Add(_menuRow);
        }
        else
        {
            _menuRowPart.Children.Remove(_menuRow);
            _menuRow.Source = null;
            _menuRow.AppMenu = null;
        }
    }

    /// <summary>
    /// The OS-standard window shortcuts: Cmd+W (close), Cmd+M (minimise, while the window can be
    /// minimised and is not in full screen) and Ctrl+Cmd+F (full screen, see <see cref="ToggleFullScreen"/>)
    /// on macOS; Alt+F4 (close) elsewhere. Maximise has no app-level standard on either (macOS has
    /// none; Windows and Linux handle Win/Super+arrows in the shell), so none is added. A shortcut
    /// that cannot act leaves the key unhandled.
    /// </summary>
    internal bool HandleShortcut(Key key, KeyModifiers modifiers, bool macOS)
    {
        if (macOS && modifiers == KeyModifiers.Meta && key == Key.W)
            Close();
        else if (macOS && modifiers == KeyModifiers.Meta && key == Key.M && CanMinimize && WindowState != WindowState.FullScreen)
            WindowState = WindowState.Minimized;
        else if (macOS && modifiers == (KeyModifiers.Control | KeyModifiers.Meta) && key == Key.F)
            return ToggleFullScreen(macOS);
        else if (!macOS && modifiers == KeyModifiers.Alt && key == Key.F4)
            Close();
        else
            return false;
        return true;
    }

    // Entering full screen: the rows hide (or stay revealed while macOS keeps its menu bar shown in full screen), and
    // leaving it restores the state before. Leaving: the rows are back in the layout.
    private void OnWindowStateChanged(WindowState oldState, WindowState newState)
    {
        if (newState == WindowState.FullScreen)
        {
            _stateBeforeFullScreen = oldState == WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
            _alwaysRevealed = HasNativeFrame && MenuBarStaysInFullScreen();
            SetRevealed(_alwaysRevealed);
            // AppKit's strip already exists once full screen is entered; it is hidden now and again on each reveal.
            HideSystemFullScreenTitleBar();
        }
        else if (oldState == WindowState.FullScreen)
        {
            _alwaysRevealed = false;
            SetRevealed(false);
            _lastPointerY = double.PositiveInfinity;
        }
    }

    // In full screen: the pointer at the top edge reveals the rows; below the revealed rows and band it starts the hide
    // delay, and back over them it cancels it.
    private void OnRevealPointerMoved(object? sender, PointerEventArgs e)
    {
        if (WindowState != WindowState.FullScreen || _top is null || _alwaysRevealed)
            return;
        _lastPointerY = e.GetPosition(this).Y;
        if (_lastPointerY <= RevealEdge)
            SetRevealed(true);
        else if (_revealed && e.GetPosition(_top).Y > _top.RevealedBottom)
            ScheduleHide();
        else if (_revealed)
            CancelHide();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        // Leaving through the top edge (onto the macOS menu bar) keeps the rows; leaving elsewhere lets them hide.
        if (WindowState == WindowState.FullScreen && _revealed && !_alwaysRevealed && _lastPointerY > RevealEdge)
            ScheduleHide();
    }

    private void SetRevealed(bool revealed)
    {
        _revealed = revealed;
        CancelHide();
        if (_top is null)
            return;
        if (revealed)
        {
            _top.TopInset = MenuBarOverlap();
            HideSystemFullScreenTitleBar();
        }
        _top.Reveal = revealed ? 1 : 0;
        UpdateStripTimer();
    }

    // On macOS in full screen, AppKit's own title bar strip slides in below the menu bar with each reveal, over the title
    // band, and would take its clicks: it is made invisible and mouse-transparent each time (nothing when it is not found).
    private void HideSystemFullScreenTitleBar()
    {
        if (HasNativeFrame && WindowState == WindowState.FullScreen && TryGetPlatformHandle() is { HandleDescriptor: "NSWindow" } handle)
            MacWindowSettings.HideFullScreenTitleBarWindow(handle.Handle);
    }

    /// <summary>How often the strip is hidden again while the rows are revealed (AppKit shows it again with the menu bar).</summary>
    internal static readonly TimeSpan StripCheckInterval = TimeSpan.FromMilliseconds(250);

    private DispatcherTimer? _stripTimer;

    // While revealed on the native macOS frame, AppKit may show its strip again when the pointer goes onto the menu bar,
    // where the window sees no pointer moves; a light timer hides it again until the rows hide or full screen ends.
    private void UpdateStripTimer()
    {
        var run = _revealed && HasNativeFrame && WindowState == WindowState.FullScreen;
        if (run && _stripTimer is null)
        {
            _stripTimer = new DispatcherTimer { Interval = StripCheckInterval };
            _stripTimer.Tick += (_, _) => HideSystemFullScreenTitleBar();
            _stripTimer.Start();
        }
        else if (!run && _stripTimer is not null)
        {
            _stripTimer.Stop();
            _stripTimer = null;
        }
    }

    /// <summary>Whether the strip timer runs (for tests).</summary>
    internal bool IsStripTimerRunning => _stripTimer is not null;

    // Hides the rows after HideDelay, unless the pointer comes back first; it waits on while they are in use.
    private void ScheduleHide()
    {
        if (_hidePending || _alwaysRevealed)
            return;
        _hidePending = true;
        var generation = ++_hideGeneration;
        RevealDelay(HideDelay, () =>
        {
            if (generation != _hideGeneration)
                return;
            _hidePending = false;
            if (WindowState != WindowState.FullScreen || !_revealed || _top is null)
                return;
            if (IsRevealInUse())
                ScheduleHide();
            // The pointer resting over the rows keeps them; it starts the delay again when it moves below them.
            else if (_lastPointerY > _top.RevealedBottom)
                SetRevealed(false);
        });
    }

    private void CancelHide()
    {
        _hideGeneration++;
        _hidePending = false;
    }

    // The revealed rows stay while keyboard focus (focus shown, not from a click) is in the rows that hide (the title
    // band and the menu row, not the toolbar band), or a menu or flyout opened from them is open.
    private bool IsRevealInUse()
    {
        if (_top is null)
            return false;
        var rows = _top.Rows;
        if (FocusManager?.GetFocusedElement() is Control focused && focused.Classes.Contains(":focus-visible")
            && (focused == rows || rows.IsVisualAncestorOf(focused)))
            return true;
        return _menuRow.IsOpen
            || rows.GetLogicalDescendants().OfType<Popup>().Any(p => p.IsOpen)
            || rows.GetVisualDescendants().OfType<Popup>().Any(p => p.IsOpen);
    }

    // On macOS, where the full screen window reaches the screen's top (no camera housing), the menu bar slides down over
    // it with the rows, so they reveal below it.
    private double MenuBarOverlap()
    {
        if (!HasNativeFrame || Screens.ScreenFromWindow(this) is not { } screen || Position.Y > screen.Bounds.Y)
            return 0;
        return MenuBarHeightSource() ?? 24;
    }

    private void OnPanelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
            UpdateContentPanelBounds();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ListenToRegions();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _regionsListener?.Dispose();
        _regionsListener = null;
    }

    private void ListenToRegions()
    {
        _regionsListener?.Dispose();
        _regionsListener = _backdrop is not null && BackdropRegions.For(this) is { } regions
            ? regions.Listen(() => UpdateBackdropRegion(regions))
            : null;
    }

    // The frame's interior less the opaque regions drawn above the backdrop, each shrunk by the glass read reach, and
    // the backdrop clipped to it.
    private void UpdateBackdropRegion(BackdropRegions regions)
    {
        if (_backdrop is not { } backdrop || _frame?.Child is not Visual layers)
            return;
        var reach = regions.Reach;
        var holes = regions.OpaqueIn(layers, s => s != backdrop && !backdrop.IsVisualAncestorOf(s))
            .Select(r => r.Deflate(reach));
        var area = new Rect(layers.Bounds.Size);
        var region = BackdropRegion.Create(area, holes);
        BackdropVisibleRegion = region;
        if (backdrop == _clippedBackdrop && region.Equals(_clippedRegion))
            return;
        _clippedBackdrop = backdrop;
        _clippedRegion = region;
        backdrop.Clip = region.Rects.Count == 1 && region.Rects[0] == area ? null : BackdropClip(region);
    }

    private static Avalonia.Media.Geometry BackdropClip(BackdropRegion region)
    {
        var geometry = new Avalonia.Media.StreamGeometry();
        using (var c = geometry.Open())
        {
            foreach (var r in region.Rects)
            {
                c.BeginFigure(r.TopLeft, true);
                c.LineTo(r.TopRight);
                c.LineTo(r.BottomRight);
                c.LineTo(r.BottomLeft);
                c.EndFigure(true);
            }
        }
        return geometry;
    }

    // The backdrop fills the frame's layer panel from its origin, so the panel's position in
    // that layer is its position in the backdrop's coordinates.
    private void UpdateContentPanelBounds()
    {
        if (_panel is not null && _frame?.Child is Visual layers && _panel.TranslatePoint(default, layers) is { } origin)
            ContentPanelBounds = new Rect(origin, _panel.Bounds.Size);
    }

    private void ApplyPanesTemplate(INameScope scope)
    {
        if (_inspectorSplitter is not null)
        {
            _inspectorSplitter.DragBegan -= OnInspectorDragBegan;
            _inspectorSplitter.Dragged -= OnInspectorDragged;
            _inspectorSplitter.DragCanceled -= OnDragCanceledInspector;
            _inspectorSplitter.Stepped -= OnInspectorStepped;
        }
        if (_bottomSplitter is not null)
        {
            _bottomSplitter.DragBegan -= OnBottomDragBegan;
            _bottomSplitter.Dragged -= OnBottomDragged;
            _bottomSplitter.DragCanceled -= OnDragCanceledBottom;
            _bottomSplitter.Stepped -= OnBottomStepped;
        }
        if (_panes is not null)
            _panes.BottomRoomChanged -= OnBottomRoomChanged;
        _panes = scope.Find<PaneLayout>(StandardsWindowTheme.PanesPart);
        if (_panes is not null)
            _panes.BottomRoomChanged += OnBottomRoomChanged;
        _inspectorPanel = scope.Find<Control>(StandardsWindowTheme.InspectorPart);
        _inspectorSplitter = scope.Find<PaneSplitter>(StandardsWindowTheme.InspectorSplitterPart);
        _bottomSplitter = scope.Find<PaneSplitter>(StandardsWindowTheme.BottomPanelSplitterPart);
        if (_inspectorSplitter is not null)
        {
            _inspectorSplitter.DragBegan += OnInspectorDragBegan;
            _inspectorSplitter.Dragged += OnInspectorDragged;
            _inspectorSplitter.DragCanceled += OnDragCanceledInspector;
            _inspectorSplitter.Stepped += OnInspectorStepped;
        }
        if (_bottomSplitter is not null)
        {
            _bottomSplitter.DragBegan += OnBottomDragBegan;
            _bottomSplitter.Dragged += OnBottomDragged;
            _bottomSplitter.DragCanceled += OnDragCanceledBottom;
            _bottomSplitter.Stepped += OnBottomStepped;
        }
        UpdatePanes();
    }

    // The inspector's handle: dragging it left widens the inspector, up to its maximum; dragging it below the minimum
    // closes the inspector. The arrow keys step the width within the minimum and maximum.
    private void OnInspectorDragBegan() => _dragStartSize = InspectorWidth;

    private void OnInspectorDragged(double moved)
    {
        if (InspectorPlacement != InspectorPlacement.Docked)
            return;
        var proposed = _dragStartSize - moved;
        if (proposed < InspectorMinWidth)
        {
            IsInspectorOpen = false;
            _inspectorSplitter?.EndDrag();
        }
        else
            InspectorWidth = Math.Min(proposed, InspectorMaxWidth);
    }

    private void OnDragCanceledInspector() => InspectorWidth = _dragStartSize;

    private void OnInspectorStepped(double moved) => InspectorWidth -= moved;

    // The bottom panel's handle: as the inspector's, upwards, with the largest height that keeps the content panel at least as tall.
    private double ShownBottomHeight => Math.Min(BottomPanelHeight, _panes?.BottomMaxHeight ?? BottomPanelHeight);

    private void OnBottomDragBegan()
    {
        _dragStartSize = ShownBottomHeight;
        _dragStartAsked = BottomPanelHeight;
    }

    private void OnBottomDragged(double moved)
    {
        if (!IsBottomPanelShown)
            return;
        var proposed = _dragStartSize - moved;
        if (proposed < BottomPanelMinHeight)
        {
            IsBottomPanelOpen = false;
            _bottomSplitter?.EndDrag();
        }
        else
            BottomPanelHeight = Math.Min(proposed, _panes?.BottomMaxHeight ?? proposed);
    }

    private void OnDragCanceledBottom() => BottomPanelHeight = _dragStartAsked;

    private void OnBottomStepped(double moved) =>
        BottomPanelHeight = Math.Clamp(ShownBottomHeight - moved, BottomPanelMinHeight, _panes?.BottomMaxHeight ?? double.MaxValue);

    // The frame's width (the visible window) sets whether the inspector can dock.
    private void OnFramePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != BoundsProperty || _frame is not { } frame || frame.Bounds.Width <= 0)
            return;
        var narrow = frame.Bounds.Width < InspectorThreshold;
        if (narrow == _narrow)
            return;
        _narrow = narrow;
        // Crossing the threshold closes the overlay and docks or hides the inspector at once.
        SetInspectorOverlay(false, restoreFocus: true, animate: false);
        UpdatePanes();
    }

    // The area became too short for the bottom panel, or tall enough again.
    private void OnBottomRoomChanged() => PanesChanged?.Invoke();

    private void UpdatePanes(bool animate = false)
    {
        if (_inspectorOverlay && (Inspector is null || !_narrow))
        {
            SetInspectorOverlay(false, restoreFocus: true, animate: false);
            return;
        }
        _panes?.Apply(InspectorPlacement, InspectorWidth, BottomPanel is not null && IsBottomPanelOpen, BottomPanelHeight, animate);
        PanesChanged?.Invoke();
    }

    // Opens or closes the inspector overlay. Closing with restoreFocus gives the keyboard back to what had it when the
    // overlay opened, when focus was in the overlay (it goes away).
    // Opening it moves focus to the first focusable control in it, as the sidebar overlay focuses a row. It slides in and
    // out over MotionNormal when animate (instant under reduce motion).
    private void SetInspectorOverlay(bool open, bool restoreFocus = false, NavigationMethod focusMethod = NavigationMethod.Unspecified,
        bool animate = true)
    {
        if (open && (!_narrow || Inspector is null))
            open = false;
        if (open == _inspectorOverlay)
            return;
        var focused = FocusManager?.GetFocusedElement() as Control;
        var focusInside = focused is not null && _inspectorPanel is not null && (focused == _inspectorPanel || _inspectorPanel.IsVisualAncestorOf(focused));
        if (open)
            _focusBeforeOverlay = focusInside ? null : focused;
        _inspectorOverlay = open;
        if (open)
        {
            AddHandler(PointerPressedEvent, OnOverlayPointerPressed, RoutingStrategies.Tunnel);
            AddHandler(KeyDownEvent, OnOverlayKeyDown, RoutingStrategies.Tunnel);
        }
        else
        {
            RemoveHandler(PointerPressedEvent, OnOverlayPointerPressed);
            RemoveHandler(KeyDownEvent, OnOverlayKeyDown);
        }
        UpdatePanes(animate);
        if (open)
            FocusIntoInspector();
        else
        {
            var previous = _focusBeforeOverlay;
            _focusBeforeOverlay = null;
            if (restoreFocus && focusInside && previous is { IsEffectivelyVisible: true, IsEffectivelyEnabled: true })
                previous.Focus(focusMethod);
        }
    }

    // The first focusable control in the inspector takes focus. Content that was never laid out (it was hidden) is
    // focused once layout has run.
    private void FocusIntoInspector()
    {
        if (FirstFocusableInInspector() is { } now)
        {
            now.Focus();
            return;
        }
        Dispatcher.UIThread.Post(() =>
        {
            if (_inspectorOverlay && FirstFocusableInInspector() is { } late)
                late.Focus();
        }, DispatcherPriority.Loaded);
    }

    private InputElement? FirstFocusableInInspector() =>
        _inspectorPanel?.GetVisualDescendants().OfType<InputElement>()
            .FirstOrDefault(c => c.Focusable && c.IsTabStop && c.IsEffectivelyEnabled && c.IsEffectivelyVisible);

    // Whether a popup is open that Escape should close first: one in the window's overlay layer (a drop-down or flyout
    // opened from the inspector), or one opened from a control in the inspector.
    private bool IsPopupOpen()
    {
        if (OverlayLayer.GetOverlayLayer(this) is { } layer && layer.Children.OfType<OverlayPopupHost>().Any(h => h.IsVisible))
            return true;
        return _inspectorPanel is { } panel
               && (panel.GetVisualDescendants().OfType<Popup>().Any(p => p.IsOpen) || panel.GetLogicalDescendants().OfType<Popup>().Any(p => p.IsOpen));
    }

    // Whether a press or key comes from inside the inspector, or from a popup (a drop-down, a menu), which is its own.
    private bool IsInspectorOrPopup(object? source) =>
        source is Visual visual && ((_inspectorPanel is { } panel && (visual == panel || panel.IsVisualAncestorOf(visual)))
                                    || visual.GetSelfAndVisualAncestors().Any(v => v is OverlayPopupHost or PopupRoot));

    // A press outside the overlay closes it and is not passed on, as a light-dismiss popup does.
    private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsInspectorOrPopup(e.Source))
            return;
        SetInspectorOverlay(false, restoreFocus: true, NavigationMethod.Pointer);
        e.Handled = true;
    }

    private void OnOverlayKeyDown(object? sender, KeyEventArgs e)
    {
        // Escape closes an open popup first (its owner may keep focus while it is open).
        if (e.Key != Key.Escape || IsPopupOpen() || (e.Source is Visual v && v.GetSelfAndVisualAncestors().Any(a => a is OverlayPopupHost or PopupRoot)))
            return;
        SetInspectorOverlay(false, restoreFocus: true, NavigationMethod.Tab);
        e.Handled = true;
    }

    // The window buttons win over the resize grips where they overlap and are visible: inside the
    // buttons and inside the frame's shape (outside the squircle corner there is nothing to click).
    private bool IsOverWindowButtons(Visual grip, Point point) =>
        _buttons is { IsEffectivelyVisible: true } buttons
        && grip.TranslatePoint(point, buttons) is { } local
        && new Rect(buttons.Bounds.Size).Contains(local)
        && (_frame?.FillGeometry is not { } shape || (grip.TranslatePoint(point, _frame) is { } inFrame && shape.FillContains(inFrame)));

    /// <summary>Maximises the window (while it can be maximised), or restores it when maximised.</summary>
    internal void ToggleMaximized()
    {
        if (WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal;
        else if (CanMaximize)
            WindowState = WindowState.Maximized;
    }

    private void OnWindowButton(object? sender, WindowButtonKind kind)
    {
        switch (kind)
        {
            case WindowButtonKind.Close:
                Close();
                break;
            case WindowButtonKind.Minimize:
                if (CanMinimize)
                    WindowState = WindowState.Minimized;
                break;
            case WindowButtonKind.FullScreen:
                ToggleFullScreen();
                break;
            default:
                // In full screen the caption maximise button (its restore glyph) leaves it for the state before, also off
                // macOS where an app set full screen itself and ToggleFullScreen does nothing.
                if (WindowState == WindowState.FullScreen)
                {
                    if (!ToggleFullScreen(RunsOnMacOS))
                        WindowState = _stateBeforeFullScreen;
                }
                else
                    ToggleMaximized();
                break;
        }
    }

    /// <summary>
    /// Whether the toolbar band's empty area drags the window (and double-clicking it acts as on the
    /// title band): in the macOS layout, where a unified toolbar's empty area is part of the title bar,
    /// and not in the Windows layout, where a command bar below the title bar is not.
    /// </summary>
    internal bool ToolbarBandDragsWindow => ResolvedLayoutMode == WindowLayoutMode.MacOS;

    // Only a press on the band's own empty area counts: not on an item, a separator or anything
    // hosted in it. Hit testing skips disabled items, so a press over one reaches the band as if
    // it were empty; the band's item areas are checked instead.
    private void OnToolbarBandPressed(ContentPresenter slot, PointerPressedEventArgs e)
    {
        if (!ToolbarBandDragsWindow || (e.Source != slot && e.Source != slot.Child))
            return;
        if (slot.Child is Modules.ToolbarBand band && band.IsOverItem(e.GetPosition(band)))
            return;
        OnTitleBandPressed(slot, e);
    }

    private void OnTitleBandPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        e.Handled = true;
        // A full screen window neither moves nor zooms.
        if (WindowState == WindowState.FullScreen)
            return;
        if (e.ClickCount == 2)
            OnTitleBandDoubleClick(HasNativeFrame && ResolvedLayoutMode == WindowLayoutMode.MacOS ? MacWindowSettings.TitleBarDoubleClickAction() : null);
        else
            BeginMoveDrag(e);
    }

    /// <summary>
    /// A double-click on the title band. <paramref name="macOSAction"/> is macOS's "double-click a
    /// window's title bar to" setting (<c>AppleActionOnDoubleClick</c>): <c>Minimize</c> minimises
    /// (while the window can be minimised), <c>None</c> does nothing, and anything else (zoom, fill,
    /// unset, or not macOS) maximises or restores (see <see cref="ToggleMaximized"/>).
    /// </summary>
    internal void OnTitleBandDoubleClick(string? macOSAction)
    {
        if (macOSAction == "Minimize")
        {
            if (CanMinimize)
                WindowState = WindowState.Minimized;
        }
        else if (macOSAction != "None")
            ToggleMaximized();
    }

    private void OnGripPressed(WindowEdge edge, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || WindowState == WindowState.Maximized)
            return;
        e.Handled = true;
        BeginResizeDrag(edge, e);
    }

    /// <summary>The window buttons in use: <see cref="WindowButtonStyle"/>, or the layout's when Auto.</summary>
    internal WindowButtonStyle ResolvedButtonStyle => WindowButtonStyle != WindowButtonStyle.Auto
        ? WindowButtonStyle
        : ResolvedLayoutMode == WindowLayoutMode.MacOS ? WindowButtonStyle.TrafficLights : WindowButtonStyle.CaptionButtons;

    /// <summary>
    /// Shows <see cref="Window.Icon"/> in the title band in the Windows layout, 16 px, before the title;
    /// nothing without an icon or in the macOS layout.
    /// </summary>
    private void UpdateTitleIcon()
    {
        ReleaseTitleIcon();
        if (_titleIcon is null || Icon is null || ResolvedLayoutMode != WindowLayoutMode.Windows)
            return;
        // A decorative element: an icon that cannot be decoded looks like no icon (no gap).
        try
        {
            _titleIcon.Source = (TitleIconDecoderForTests ?? DecodeIcon)(Icon);
        }
        catch (ArgumentException)
        {
        }
        PseudoClasses.Set(":title-icon", _titleIcon.Source is not null);
    }

    // Clears the icon and disposes the bitmap decoded for it.
    private void ReleaseTitleIcon()
    {
        PseudoClasses.Set(":title-icon", false);
        if (_titleIcon is null)
            return;
        var old = _titleIcon.Source as IDisposable;
        _titleIcon.Source = null;
        old?.Dispose();
    }

    private Func<WindowIcon, Avalonia.Media.IImage?>? _titleIconDecoderForTests;

    /// <summary>
    /// For headless tests only: turns <see cref="Window.Icon"/> into the image the title band shows, because a headless
    /// platform's window icons hold no image data, so the Windows layout's title icon would not render. Null (the
    /// default) decodes the icon itself; apps leave it null. Set it per window, before or after <see cref="Window.Icon"/>;
    /// the title icon updates at once. Like <c>StandardsTheme.CreateForTests</c>, it lets an app's headless tests render
    /// what users see.
    /// </summary>
    public Func<WindowIcon, Avalonia.Media.IImage?>? TitleIconDecoderForTests
    {
        get => _titleIconDecoderForTests;
        set
        {
            _titleIconDecoderForTests = value;
            UpdateTitleIcon();
        }
    }

    // WindowIcon only saves itself as an image stream; decode that to draw it.
    private static Avalonia.Media.Imaging.Bitmap DecodeIcon(WindowIcon icon)
    {
        using var stream = new MemoryStream();
        icon.Save(stream);
        stream.Position = 0;
        return new Avalonia.Media.Imaging.Bitmap(stream);
    }

    private void UpdateStates()
    {
        PseudoClasses.Set(":maximized", WindowState == WindowState.Maximized);
        PseudoClasses.Set(":fullscreen", WindowState == WindowState.FullScreen);
        PseudoClasses.Set(":opaque", IsInitialized && ActualTransparencyLevel == WindowTransparencyLevel.None && IsVisible);

        var trafficLights = ResolvedButtonStyle == WindowButtonStyle.TrafficLights;
        PseudoClasses.Set(":traffic-lights", trafficLights);
        if (_buttons is not null)
        {
            _buttons.ButtonStyle = ResolvedButtonStyle;
            // In full screen the caption maximise button shows restore: it leaves full screen.
            _buttons.IsMaximized = WindowState is WindowState.Maximized or WindowState.FullScreen;
            _buttons.IsActive = IsActive;
            _buttons.CanMinimize = CanMinimize;
            _buttons.CanMaximize = CanMaximize;
            _buttons.IsFullScreenAvailable = RunsOnMacOS;
            _buttons.IsFullScreen = WindowState == WindowState.FullScreen;
            Grid.SetColumn(_buttons, trafficLights ? 0 : 3);
        }
    }
}
