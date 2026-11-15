using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaUiStandards.Blocks;

/// <summary>Which window buttons to show.</summary>
public enum WindowButtonStyle
{
    /// <summary>Traffic lights on macOS, caption buttons elsewhere.</summary>
    Auto,

    /// <summary>macOS traffic lights (close, minimise, zoom), placed on the left.</summary>
    TrafficLights,

    /// <summary>Windows and Linux caption buttons (minimise, maximise or restore, close), placed on the right.</summary>
    CaptionButtons,
}

/// <summary>What a window button does.</summary>
public enum WindowButtonKind
{
    Close,
    Minimize,

    /// <summary>Maximise or restore (zoom on macOS).</summary>
    Maximize,

    /// <summary>
    /// Enter or leave full screen: raised by the green traffic light (whose <see cref="TrafficLightButton.Kind"/> stays
    /// <see cref="Maximize"/>) while <see cref="WindowButtons.IsFullScreenAvailable"/>; Option-click raises Maximize.
    /// </summary>
    FullScreen,
}

/// <summary>What the green traffic light's glyph shows.</summary>
internal enum ZoomGlyph
{
    /// <summary>Zoom: a plus.</summary>
    Zoom,

    /// <summary>Enter full screen: two triangles pointing out.</summary>
    EnterFullScreen,

    /// <summary>Leave full screen: two triangles pointing in.</summary>
    ExitFullScreen,
}

/// <summary>A macOS traffic light: a coloured dot whose glyph shows while the group is hovered, grey while its window is inactive (unless hovered).</summary>
public class TrafficLightButton : Button
{
    public static readonly StyledProperty<WindowButtonKind> KindProperty =
        AvaloniaProperty.Register<TrafficLightButton, WindowButtonKind>(nameof(Kind));

    internal static readonly DirectProperty<TrafficLightButton, Geometry?> GlyphProperty =
        AvaloniaProperty.RegisterDirect<TrafficLightButton, Geometry?>(nameof(Glyph), b => b.Glyph);

    private Geometry? _glyph;
    private ZoomGlyph _zoomGlyph;

    static TrafficLightButton() =>
        KindProperty.Changed.AddClassHandler<TrafficLightButton>((b, _) => b.UpdateKind());

    public TrafficLightButton()
    {
        WindowButtonParts.MakeNonFocusable(this);
        UpdateKind();
    }

    public WindowButtonKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>Shows or hides the glyph; set by <see cref="WindowButtons"/> for the whole group.</summary>
    internal void SetGlyphVisible(bool visible) => PseudoClasses.Set(":glyph", visible);

    /// <summary>Greys the light while its window is inactive; set by <see cref="WindowButtons"/>.</summary>
    internal void SetInactive(bool inactive) => PseudoClasses.Set(":inactive", inactive);

    /// <summary>The glyph drawn on the light.</summary>
    internal Geometry? Glyph => _glyph;

    /// <summary>What the green light's glyph shows (zoom, or entering or leaving full screen); set by <see cref="WindowButtons"/>.</summary>
    internal ZoomGlyph ZoomGlyph
    {
        get => _zoomGlyph;
        set
        {
            if (_zoomGlyph == value)
                return;
            _zoomGlyph = value;
            UpdateKind();
        }
    }

    private void UpdateKind()
    {
        SetAndRaise(GlyphProperty, ref _glyph, WindowButtonGlyphs.TrafficLight(Kind, ZoomGlyph));
        WindowButtonParts.SetRole(this, Kind);
        AutomationProperties.SetName(this, Kind switch
        {
            WindowButtonKind.Close => "Close",
            WindowButtonKind.Minimize => "Minimize",
            _ => ZoomGlyph switch
            {
                ZoomGlyph.EnterFullScreen => "Enter Full Screen",
                ZoomGlyph.ExitFullScreen => "Exit Full Screen",
                _ => "Zoom",
            },
        });
    }
}

/// <summary>A Windows or Linux caption button. Close turns to the danger fill on hover.</summary>
public class CaptionButton : Button
{
    public static readonly StyledProperty<WindowButtonKind> KindProperty =
        AvaloniaProperty.Register<CaptionButton, WindowButtonKind>(nameof(Kind));

    /// <summary>Whether the window is maximised: the maximise button then shows the restore glyph.</summary>
    public static readonly StyledProperty<bool> IsMaximizedProperty =
        AvaloniaProperty.Register<CaptionButton, bool>(nameof(IsMaximized));

    internal static readonly DirectProperty<CaptionButton, Geometry?> GlyphProperty =
        AvaloniaProperty.RegisterDirect<CaptionButton, Geometry?>(nameof(Glyph), b => b.Glyph);

    private Geometry? _glyph;

    static CaptionButton()
    {
        KindProperty.Changed.AddClassHandler<CaptionButton>((b, _) => b.Update());
        IsMaximizedProperty.Changed.AddClassHandler<CaptionButton>((b, _) => b.Update());
    }

    public CaptionButton()
    {
        WindowButtonParts.MakeNonFocusable(this);
        Update();
    }

    /// <summary>Dims the glyph while its window is inactive; set by <see cref="WindowButtons"/>.</summary>
    internal void SetInactive(bool inactive) => PseudoClasses.Set(":inactive", inactive);

    public WindowButtonKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public bool IsMaximized
    {
        get => GetValue(IsMaximizedProperty);
        set => SetValue(IsMaximizedProperty, value);
    }

    internal Geometry? Glyph => _glyph;

    private void Update()
    {
        SetAndRaise(GlyphProperty, ref _glyph, WindowButtonGlyphs.Caption(Kind, IsMaximized));
        WindowButtonParts.SetRole(this, Kind);
        PseudoClasses.Set(":close", Kind == WindowButtonKind.Close);
        AutomationProperties.SetName(this, Kind switch
        {
            WindowButtonKind.Close => "Close",
            WindowButtonKind.Minimize => "Minimize",
            _ => IsMaximized ? "Restore" : "Maximize",
        });
    }
}

/// <summary>
/// The window buttons of a window: traffic lights or caption buttons, chosen by
/// <see cref="ButtonStyle"/> (by OS when <see cref="WindowButtonStyle.Auto"/>). Raises
/// <see cref="Clicked"/> with the button's kind; the window acts on it.
/// </summary>
public class WindowButtons : Decorator
{
    public static readonly StyledProperty<WindowButtonStyle> ButtonStyleProperty =
        AvaloniaProperty.Register<WindowButtons, WindowButtonStyle>(nameof(ButtonStyle));

    public static readonly StyledProperty<bool> IsMaximizedProperty =
        AvaloniaProperty.Register<WindowButtons, bool>(nameof(IsMaximized));

    /// <summary>Whether the window is active; inactive, traffic lights go grey and caption glyphs dim.</summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<WindowButtons, bool>(nameof(IsActive), true);

    public static readonly StyledProperty<bool> CanMinimizeProperty =
        AvaloniaProperty.Register<WindowButtons, bool>(nameof(CanMinimize), true);

    public static readonly StyledProperty<bool> CanMaximizeProperty =
        AvaloniaProperty.Register<WindowButtons, bool>(nameof(CanMaximize), true);

    public static readonly StyledProperty<bool> IsFullScreenAvailableProperty =
        AvaloniaProperty.Register<WindowButtons, bool>(nameof(IsFullScreenAvailable));

    public static readonly StyledProperty<bool> IsFullScreenProperty =
        AvaloniaProperty.Register<WindowButtons, bool>(nameof(IsFullScreen));

    private KeyModifiers _modifiers;
    private TopLevel? _topLevel;

    static WindowButtons()
    {
        ButtonStyleProperty.Changed.AddClassHandler<WindowButtons>((w, _) => w.Build());
        IsMaximizedProperty.Changed.AddClassHandler<WindowButtons>((w, _) => w.UpdateMaximized());
        IsActiveProperty.Changed.AddClassHandler<WindowButtons>((w, _) => w.UpdateActive());
        CanMinimizeProperty.Changed.AddClassHandler<WindowButtons>((w, _) => w.UpdateAvailability());
        CanMaximizeProperty.Changed.AddClassHandler<WindowButtons>((w, _) => w.UpdateAvailability());
        IsFullScreenAvailableProperty.Changed.AddClassHandler<WindowButtons>((w, _) => w.UpdateAvailability());
        IsFullScreenProperty.Changed.AddClassHandler<WindowButtons>((w, _) => w.UpdateAvailability());
    }

    public WindowButtons()
    {
        // The Option key: read from the pointer before a light's click, and from the keyboard while the pointer rests on the lights.
        AddHandler(PointerPressedEvent, (_, e) => SetModifiers(e.KeyModifiers), RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, (_, e) => SetModifiers(e.KeyModifiers), RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, (_, e) => SetModifiers(e.KeyModifiers), RoutingStrategies.Tunnel, handledEventsToo: true);
        Build();
    }

    public WindowButtonStyle ButtonStyle
    {
        get => GetValue(ButtonStyleProperty);
        set => SetValue(ButtonStyleProperty, value);
    }

    public bool IsMaximized
    {
        get => GetValue(IsMaximizedProperty);
        set => SetValue(IsMaximizedProperty, value);
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>
    /// Whether the window can be minimised (default true): without it the caption minimise button is gone and the
    /// minimise traffic light is disabled.
    /// </summary>
    public bool CanMinimize
    {
        get => GetValue(CanMinimizeProperty);
        set => SetValue(CanMinimizeProperty, value);
    }

    /// <summary>
    /// Whether the window can be maximised (default true): without it the caption maximise button is gone and the
    /// green traffic light is disabled (it neither zooms nor enters full screen).
    /// </summary>
    public bool CanMaximize
    {
        get => GetValue(CanMaximizeProperty);
        set => SetValue(CanMaximizeProperty, value);
    }

    /// <summary>
    /// Whether the green traffic light enters full screen, as on macOS (default false): its glyph shows the full screen
    /// triangles and a click raises <see cref="WindowButtonKind.FullScreen"/>; Option-click zooms
    /// (<see cref="WindowButtonKind.Maximize"/>), and while Option is held over the lights the glyph is the plus.
    /// </summary>
    public bool IsFullScreenAvailable
    {
        get => GetValue(IsFullScreenAvailableProperty);
        set => SetValue(IsFullScreenAvailableProperty, value);
    }

    /// <summary>
    /// Whether the window is in full screen: the green light leaves it (its triangles point in, and it stays enabled)
    /// and the minimise light is disabled, as on macOS.
    /// </summary>
    public bool IsFullScreen
    {
        get => GetValue(IsFullScreenProperty);
        set => SetValue(IsFullScreenProperty, value);
    }

    /// <summary>The style in use: <see cref="ButtonStyle"/>, with Auto resolved for this OS.</summary>
    public WindowButtonStyle ResolvedStyle => Resolve(ButtonStyle);

    /// <summary>Raised when a window button is clicked.</summary>
    public event EventHandler<WindowButtonKind>? Clicked;

    internal static WindowButtonStyle Resolve(WindowButtonStyle style) => style != WindowButtonStyle.Auto
        ? style
        : OperatingSystem.IsMacOS() ? WindowButtonStyle.TrafficLights : WindowButtonStyle.CaptionButtons;

    private void Build()
    {
        var trafficLights = ResolvedStyle == WindowButtonStyle.TrafficLights;
        WindowButtonKind[] order = trafficLights
            ? [WindowButtonKind.Close, WindowButtonKind.Minimize, WindowButtonKind.Maximize]
            : [WindowButtonKind.Minimize, WindowButtonKind.Maximize, WindowButtonKind.Close];

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = trafficLights ? WindowButtonThemes.TrafficLightGap : 0,
            VerticalAlignment = trafficLights ? VerticalAlignment.Center : VerticalAlignment.Top,
        };
        foreach (var kind in order)
        {
            Button button = trafficLights
                ? new TrafficLightButton { Kind = kind }
                : new CaptionButton { Kind = kind, IsMaximized = IsMaximized };
            button.Click += (_, e) =>
            {
                e.Handled = true;
                Clicked?.Invoke(this, ClickedKind(kind, trafficLights));
            };
            panel.Children.Add(button);
        }
        Child = panel;
        UpdateGlyphs();
        UpdateActive();
        UpdateAvailability();
    }

    // The green light enters or leaves full screen where it can; Option-click zooms instead, except in full screen.
    private WindowButtonKind ClickedKind(WindowButtonKind kind, bool trafficLights) =>
        trafficLights && kind == WindowButtonKind.Maximize && IsFullScreenAvailable
            && (IsFullScreen || !_modifiers.HasFlag(KeyModifiers.Alt))
            ? WindowButtonKind.FullScreen
            : kind;

    /// <summary>
    /// Shows and enables the buttons for what the window can do: a caption button the window cannot use is gone, as in
    /// Windows, and a traffic light is disabled, as macOS never removes one. In full screen, minimise is disabled and
    /// the green light only leaves full screen.
    /// </summary>
    private void UpdateAvailability()
    {
        if (Child is not Panel panel)
            return;
        foreach (var caption in panel.Children.OfType<CaptionButton>())
            caption.IsVisible = caption.Kind switch
            {
                WindowButtonKind.Minimize => CanMinimize,
                WindowButtonKind.Maximize => CanMaximize,
                _ => true,
            };
        foreach (var light in panel.Children.OfType<TrafficLightButton>())
        {
            light.IsEnabled = light.Kind switch
            {
                WindowButtonKind.Minimize => CanMinimize && !IsFullScreen,
                WindowButtonKind.Maximize => IsFullScreen || CanMaximize,
                _ => true,
            };
            if (light.Kind == WindowButtonKind.Maximize)
                light.ZoomGlyph = !IsFullScreenAvailable ? ZoomGlyph.Zoom
                    : IsFullScreen ? ZoomGlyph.ExitFullScreen
                    : _modifiers.HasFlag(KeyModifiers.Alt) ? ZoomGlyph.Zoom
                    : ZoomGlyph.EnterFullScreen;
        }
    }

    private void SetModifiers(KeyModifiers modifiers)
    {
        if (_modifiers == modifiers)
            return;
        _modifiers = modifiers;
        UpdateAvailability();
    }

    // Option pressed or released while the pointer rests on the lights changes the green glyph, as on macOS.
    private void OnTopLevelKey(object? sender, KeyEventArgs e)
    {
        if (IsPointerOver)
            SetModifiers(e.KeyModifiers);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnTopLevelKey, RoutingStrategies.Tunnel, handledEventsToo: true);
        _topLevel?.AddHandler(KeyUpEvent, OnTopLevelKey, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(KeyDownEvent, OnTopLevelKey);
        _topLevel?.RemoveHandler(KeyUpEvent, OnTopLevelKey);
        _topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void UpdateActive()
    {
        if (Child is not Panel panel)
            return;
        foreach (var light in panel.Children.OfType<TrafficLightButton>())
            light.SetInactive(!IsActive);
        foreach (var caption in panel.Children.OfType<CaptionButton>())
            caption.SetInactive(!IsActive);
    }

    private void UpdateMaximized()
    {
        if (Child is Panel panel)
            foreach (var button in panel.Children.OfType<CaptionButton>())
                button.IsMaximized = IsMaximized;
    }

    // As macOS does: all three glyphs show while the pointer is over the group.
    private void UpdateGlyphs()
    {
        var visible = IsPointerOver;
        if (Child is Panel panel)
            foreach (var light in panel.Children.OfType<TrafficLightButton>())
                light.SetGlyphVisible(visible);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsPointerOverProperty)
        {
            // Option held only counts while the pointer is on the lights.
            if (!IsPointerOver)
                SetModifiers(KeyModifiers.None);
            UpdateGlyphs();
        }
    }
}

internal static class WindowButtonParts
{
    /// <summary>
    /// Tags the button with its chrome role (close, minimise, maximise), so a window with an
    /// extended client area lets the OS treat it as that window button.
    /// </summary>
    public static void SetRole(Button button, WindowButtonKind kind) => WindowDecorationProperties.SetElementRole(button, kind switch
    {
        WindowButtonKind.Close => WindowDecorationsElementRole.CloseButton,
        WindowButtonKind.Minimize => WindowDecorationsElementRole.MinimizeButton,
        _ => WindowDecorationsElementRole.MaximizeButton,
    });

    /// <summary>Window buttons are not focus or Tab stops, as native ones; the window has the keyboard shortcuts.</summary>
    public static void MakeNonFocusable(Button button)
    {
        button.Focusable = false;
        KeyboardNavigation.SetIsTabStop(button, false);
    }
}

/// <summary>The glyphs the window buttons draw themselves (no icon set).</summary>
internal static class WindowButtonGlyphs
{
    /// <summary>Caption glyph, in a 10 × 10 box, drawn with a 1 px stroke.</summary>
    public static Geometry Caption(WindowButtonKind kind, bool maximized) => Geometry.Parse(kind switch
    {
        WindowButtonKind.Minimize => "M0,5.5 L10,5.5",
        WindowButtonKind.Maximize when maximized => "M0.5,2.5 H7.5 V9.5 H0.5 Z M2.5,2.5 V0.5 H9.5 V7.5 H7.5",
        WindowButtonKind.Maximize => "M0.5,0.5 H9.5 V9.5 H0.5 Z",
        _ => "M0,0 L10,10 M10,0 L0,10",
    });

    /// <summary>
    /// Traffic light glyph, in a 6 × 6 box, drawn with a 1 px round-capped stroke and filled (only the full screen
    /// triangles are closed, so only they fill). The green light shows a plus to zoom, or two triangles pointing out to
    /// enter full screen and in to leave it, as on macOS.
    /// </summary>
    public static Geometry TrafficLight(WindowButtonKind kind, ZoomGlyph zoom = ZoomGlyph.Zoom) => Geometry.Parse(kind switch
    {
        WindowButtonKind.Minimize => "M0,3 L6,3",
        WindowButtonKind.Close => "M0,0 L6,6 M6,0 L0,6",
        _ => zoom switch
        {
            ZoomGlyph.EnterFullScreen => "M0.5,0.5 L3.5,0.5 L0.5,3.5 Z M5.5,5.5 L2.5,5.5 L5.5,2.5 Z",
            ZoomGlyph.ExitFullScreen => "M2.5,2.5 L2.5,0 L0,2.5 Z M3.5,3.5 L3.5,6 L6,3.5 Z",
            _ => "M3,0 L3,6 M0,3 L6,3",
        },
    });
}
