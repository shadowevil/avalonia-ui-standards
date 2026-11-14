using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The library's window: borderless and transparent, drawn as a squircle frame (radius 16, 1 px
/// border, soft shadow) whose background is the backdrop wash. A title band holds the window
/// buttons, the <see cref="Window.Title"/> and the <see cref="Toolbar"/>; below it are the menu row
/// (in the Windows layout, for the window's <see cref="NativeMenu"/>), the optional
/// <see cref="ToolbarBand"/> across the window, the optional <see cref="Sidebar"/> on the window background, the glass content panel holding
/// <see cref="ContentControl.Content"/> (scrolled by the panel, see <see cref="IsContentScrollable"/>),
/// and the optional <see cref="StatusBar"/>. An optional <see cref="Backdrop"/> sits behind them all.
/// </summary>
/// <remarks>
/// Drag the window by the title band (double-click maximises or restores; in the macOS layout it
/// follows the macOS setting for a title bar double-click, on macOS), resize it from the frame's edges and corners.
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

    public static readonly StyledProperty<object?> BackdropProperty =
        AvaloniaProperty.Register<StandardsWindow, object?>(nameof(Backdrop));

    public static readonly StyledProperty<bool> IsContentScrollableProperty =
        AvaloniaProperty.Register<StandardsWindow, bool>(nameof(IsContentScrollable), defaultValue: true);

    public static readonly StyledProperty<WindowButtonStyle> WindowButtonStyleProperty =
        AvaloniaProperty.Register<StandardsWindow, WindowButtonStyle>(nameof(WindowButtonStyle));

    public static readonly StyledProperty<WindowLayoutMode> LayoutModeProperty =
        AvaloniaProperty.Register<StandardsWindow, WindowLayoutMode>(nameof(LayoutMode));

    public static readonly StyledProperty<bool> HighRefreshRateProperty =
        AvaloniaProperty.Register<StandardsWindow, bool>(nameof(HighRefreshRate));

    // The theme's effective reduce-motion setting, fed by StandardsTheme.
    internal static readonly StyledProperty<bool> ReduceMotionProperty =
        AvaloniaProperty.Register<StandardsWindow, bool>("ReduceMotion");

    public static readonly DirectProperty<StandardsWindow, Rect> ContentPanelBoundsProperty =
        AvaloniaProperty.RegisterDirect<StandardsWindow, Rect>(nameof(ContentPanelBounds), w => w.ContentPanelBounds);

    private WindowButtons? _buttons;
    private Image? _titleIcon;
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
    private Rect _contentPanelBounds;
    private readonly FrameTicker _ticker;
    private EventHandler<FrameTickEventArgs>? _frameTick;

    public StandardsWindow()
        : this(nativeFrame: null)
    {
    }

    /// <param name="nativeFrame">Whether to use the native macOS frame; by OS and platform when null.</param>
    internal StandardsWindow(bool? nativeFrame)
    {
        _shortcuts = new MenuShortcuts(this);
        _ticker = new FrameTicker(new Scheduler(this), (time, elapsed) => _frameTick?.Invoke(this, new FrameTickEventArgs(time, elapsed)));
        HasNativeFrame = nativeFrame ?? UsesNativeFrame(OperatingSystem.IsMacOS(), TryGetPlatformHandle()?.HandleDescriptor);
        WindowDecorations = DecorationsFor(HasNativeFrame);
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        PseudoClasses.Set(":native-frame", HasNativeFrame);
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

    /// <summary>Optional content of the status bar at the bottom.</summary>
    public object? StatusBar
    {
        get => GetValue(StatusBarProperty);
        set => SetValue(StatusBarProperty, value);
    }

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

    /// <summary>The layout in use: <see cref="LayoutMode"/>, with Automatic resolved for this OS.</summary>
    internal WindowLayoutMode ResolvedLayoutMode => WindowLayout.Resolve(LayoutMode, OperatingSystem.IsMacOS());

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
        _frame = e.NameScope.Find<SquircleBorder>(StandardsWindowTheme.FramePart);
        _titleIcon = e.NameScope.Find<Image>(StandardsWindowTheme.TitleIconPart);
        UpdateTitleIcon();

        // The panel moves within the frame on resize, density and maximise: each changes its bounds.
        if (_panel is not null)
            _panel.PropertyChanged -= OnPanelPropertyChanged;
        _panel = e.NameScope.Find<Control>(StandardsWindowTheme.ContentPanelPart);
        if (_panel is not null)
            _panel.PropertyChanged += OnPanelPropertyChanged;

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
        if (change.Property == WindowStateProperty || change.Property == WindowButtonStyleProperty
            || change.Property == ActualTransparencyLevelProperty || change.Property == IsActiveProperty
            || change.Property == LayoutModeProperty)
            UpdateStates();
        if (change.Property == LayoutModeProperty || change.Property == NativeMenu.MenuProperty)
            UpdateMenuRow();
        if (change.Property == LayoutModeProperty || change.Property == IconProperty)
            UpdateTitleIcon();
        if (change.Property == SidebarProperty)
            PseudoClasses.Set(":sidebar-module", change.NewValue is Sidebar);
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
        _closed = true;
        // The menus outlive the window (the Application's for the whole app): let go of them.
        _menuRowPart?.Children.Remove(_menuRow);
        _menuRow.Source = null;
        _menuRow.AppMenu = null;
        _shortcuts.Dispose();
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
    /// The OS-standard window shortcuts: Cmd+W (close) and Cmd+M (minimise) on macOS; Alt+F4
    /// (close) elsewhere. Maximise has no app-level standard on either (macOS has none; Windows
    /// and Linux handle Win/Super+arrows in the shell), so none is added.
    /// </summary>
    internal bool HandleShortcut(Key key, KeyModifiers modifiers, bool macOS)
    {
        if (macOS && modifiers == KeyModifiers.Meta && key == Key.W)
            Close();
        else if (macOS && modifiers == KeyModifiers.Meta && key == Key.M)
            WindowState = WindowState.Minimized;
        else if (!macOS && modifiers == KeyModifiers.Alt && key == Key.F4)
            Close();
        else
            return false;
        return true;
    }

    private void OnPanelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
            UpdateContentPanelBounds();
    }

    // The backdrop fills the frame's layer panel from its origin, so the panel's position in
    // that layer is its position in the backdrop's coordinates.
    private void UpdateContentPanelBounds()
    {
        if (_panel is not null && _frame?.Child is Visual layers && _panel.TranslatePoint(default, layers) is { } origin)
            ContentPanelBounds = new Rect(origin, _panel.Bounds.Size);
    }

    // The window buttons win over the resize grips where they overlap and are visible: inside the
    // buttons and inside the frame's shape (outside the squircle corner there is nothing to click).
    private bool IsOverWindowButtons(Visual grip, Point point) =>
        _buttons is { IsEffectivelyVisible: true } buttons
        && grip.TranslatePoint(point, buttons) is { } local
        && new Rect(buttons.Bounds.Size).Contains(local)
        && (_frame?.FillGeometry is not { } shape || (grip.TranslatePoint(point, _frame) is { } inFrame && shape.FillContains(inFrame)));

    /// <summary>Maximises the window, or restores it when maximised.</summary>
    internal void ToggleMaximized() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnWindowButton(object? sender, WindowButtonKind kind)
    {
        switch (kind)
        {
            case WindowButtonKind.Close:
                Close();
                break;
            case WindowButtonKind.Minimize:
                WindowState = WindowState.Minimized;
                break;
            default:
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
        if (e.ClickCount == 2)
            OnTitleBandDoubleClick(HasNativeFrame && ResolvedLayoutMode == WindowLayoutMode.MacOS ? MacWindowSettings.TitleBarDoubleClickAction() : null);
        else
            BeginMoveDrag(e);
    }

    /// <summary>
    /// A double-click on the title band. <paramref name="macOSAction"/> is macOS's "double-click a
    /// window's title bar to" setting (<c>AppleActionOnDoubleClick</c>): <c>Minimize</c> minimises,
    /// <c>None</c> does nothing, and anything else (zoom, fill, unset, or not macOS) maximises or
    /// restores.
    /// </summary>
    internal void OnTitleBandDoubleClick(string? macOSAction)
    {
        if (macOSAction == "Minimize")
            WindowState = WindowState.Minimized;
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
        var show = Icon is not null && ResolvedLayoutMode == WindowLayoutMode.Windows;
        PseudoClasses.Set(":title-icon", show);
        if (_titleIcon is null)
            return;
        var old = _titleIcon.Source as IDisposable;
        _titleIcon.Source = null;
        old?.Dispose();
        if (!show)
            return;
        // A decorative element: an icon that cannot be decoded leaves the slot blank.
        try
        {
            _titleIcon.Source = (IconDecoder ?? DecodeIcon)(Icon!);
        }
        catch (ArgumentException)
        {
        }
    }

    /// <summary>Replaces <see cref="DecodeIcon"/>, for headless tests, whose icons cannot save themselves as an image.</summary>
    internal Func<WindowIcon, Avalonia.Media.IImage?>? IconDecoder { get; set; }

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
        PseudoClasses.Set(":maximized", WindowState is WindowState.Maximized or WindowState.FullScreen);
        PseudoClasses.Set(":opaque", IsInitialized && ActualTransparencyLevel == WindowTransparencyLevel.None && IsVisible);

        var trafficLights = ResolvedButtonStyle == WindowButtonStyle.TrafficLights;
        PseudoClasses.Set(":traffic-lights", trafficLights);
        if (_buttons is not null)
        {
            _buttons.ButtonStyle = ResolvedButtonStyle;
            _buttons.IsMaximized = WindowState == WindowState.Maximized;
            _buttons.IsActive = IsActive;
            Grid.SetColumn(_buttons, trafficLights ? 0 : 3);
        }
    }
}
