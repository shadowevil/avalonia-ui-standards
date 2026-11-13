using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The library's window: borderless and transparent, drawn as a squircle frame (radius 16, 1 px
/// border, soft shadow) whose background is the backdrop wash. A title band holds the window
/// buttons, the <see cref="Window.Title"/> and the <see cref="Toolbar"/>; below it are the optional
/// <see cref="Sidebar"/> on the window background, the glass content panel holding
/// <see cref="ContentControl.Content"/> (scrolled by the panel, see <see cref="IsContentScrollable"/>),
/// and the optional <see cref="StatusBar"/>. An optional <see cref="Backdrop"/> sits behind them all.
/// </summary>
/// <remarks>
/// Drag the window by the title band (double-click maximises or restores), resize it from the
/// frame's edges and corners. Maximised, or where the OS gives no transparency, the frame fills
/// the window with square corners and no shadow.
/// </remarks>
public class StandardsWindow : Window
{
    public static readonly StyledProperty<object?> ToolbarProperty =
        AvaloniaProperty.Register<StandardsWindow, object?>(nameof(Toolbar));

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

    public static readonly StyledProperty<bool> HighRefreshRateProperty =
        AvaloniaProperty.Register<StandardsWindow, bool>(nameof(HighRefreshRate));

    // The theme's effective reduce-motion setting, fed by StandardsTheme.
    internal static readonly StyledProperty<bool> ReduceMotionProperty =
        AvaloniaProperty.Register<StandardsWindow, bool>("ReduceMotion");

    public static readonly DirectProperty<StandardsWindow, Rect> ContentPanelBoundsProperty =
        AvaloniaProperty.RegisterDirect<StandardsWindow, Rect>(nameof(ContentPanelBounds), w => w.ContentPanelBounds);

    private WindowButtons? _buttons;
    private SquircleBorder? _frame;
    private Control? _panel;
    private Rect _contentPanelBounds;
    private readonly FrameTicker _ticker;
    private EventHandler<FrameTickEventArgs>? _frameTick;

    public StandardsWindow()
    {
        _ticker = new FrameTicker(new Scheduler(this), (time, elapsed) => _frameTick?.Invoke(this, new FrameTickEventArgs(time, elapsed)));
        WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        UpdateStates();
    }

    // Window reports typeof(Window) as its style key; this window (and app subclasses of it) uses its own theme.
    protected override Type StyleKeyOverride => typeof(StandardsWindow);

    /// <summary>Content of the title band, after the title (a unified toolbar).</summary>
    public object? Toolbar
    {
        get => GetValue(ToolbarProperty);
        set => SetValue(ToolbarProperty, value);
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

    /// <summary>Traffic lights or caption buttons; by OS when <see cref="WindowButtonStyle.Auto"/>.</summary>
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

        // The panel moves within the frame on resize, density and maximise: each changes its bounds.
        if (_panel is not null)
            _panel.PropertyChanged -= OnPanelPropertyChanged;
        _panel = e.NameScope.Find<Control>(StandardsWindowTheme.ContentPanelPart);
        if (_panel is not null)
            _panel.PropertyChanged += OnPanelPropertyChanged;

        if (e.NameScope.Find<Control>(StandardsWindowTheme.TitleBandPart) is { } band)
            band.PointerPressed += OnTitleBandPressed;
        foreach (var edge in Enum.GetValues<WindowEdge>())
            if (e.NameScope.Find<ResizeGrip>(StandardsWindowTheme.ResizePart(edge)) is { } grip)
            {
                grip.PointerPressed += (_, args) => OnGripPressed(edge, args);
                grip.IsExcluded = point => IsOverWindowButtons(grip, point);
            }

        UpdateStates();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty || change.Property == WindowButtonStyleProperty
            || change.Property == ActualTransparencyLevelProperty || change.Property == IsActiveProperty)
            UpdateStates();
        if (change.Property == IsActiveProperty || change.Property == WindowStateProperty
            || change.Property == ReduceMotionProperty || change.Property == HighRefreshRateProperty)
            UpdateTicker();
    }

    protected override void OnClosed(EventArgs e)
    {
        _ticker.Stop();
        base.OnClosed(e);
    }

    // Whether frame ticks are running now (for tests).
    internal bool IsFrameTickRunning => _ticker.IsRunning;

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
        base.OnKeyDown(e);
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

    private void OnTitleBandPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        e.Handled = true;
        if (e.ClickCount == 2)
            ToggleMaximized();
        else
            BeginMoveDrag(e);
    }

    private void OnGripPressed(WindowEdge edge, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || WindowState == WindowState.Maximized)
            return;
        e.Handled = true;
        BeginResizeDrag(edge, e);
    }

    private void UpdateStates()
    {
        PseudoClasses.Set(":maximized", WindowState is WindowState.Maximized or WindowState.FullScreen);
        PseudoClasses.Set(":opaque", IsInitialized && ActualTransparencyLevel == WindowTransparencyLevel.None && IsVisible);

        var trafficLights = WindowButtons.Resolve(WindowButtonStyle) == WindowButtonStyle.TrafficLights;
        PseudoClasses.Set(":traffic-lights", trafficLights);
        if (_buttons is not null)
        {
            _buttons.IsMaximized = WindowState == WindowState.Maximized;
            _buttons.IsActive = IsActive;
            Grid.SetColumn(_buttons, trafficLights ? 0 : 3);
        }
    }
}
