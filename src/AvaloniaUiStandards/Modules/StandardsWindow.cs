using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The library's window: borderless and transparent, drawn as a squircle frame (radius 16, 1 px
/// border, soft shadow) whose background is the backdrop wash. A title band holds the window
/// buttons, the <see cref="Window.Title"/> and the <see cref="Toolbar"/>; below it are the optional
/// <see cref="Sidebar"/> on the window background, the glass content panel holding
/// <see cref="ContentControl.Content"/>, and the optional <see cref="StatusBar"/>.
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

    public static readonly StyledProperty<WindowButtonStyle> WindowButtonStyleProperty =
        AvaloniaProperty.Register<StandardsWindow, WindowButtonStyle>(nameof(WindowButtonStyle));

    private WindowButtons? _buttons;
    private SquircleBorder? _frame;

    public StandardsWindow()
    {
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
