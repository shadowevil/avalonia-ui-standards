using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AvaloniaUiStandards.Modules;

/// <summary>A menu item that acts on the <see cref="StandardsWindow"/> whose menu holds it; the window's menu shortcuts set it.</summary>
internal interface IWindowMenuItem
{
    StandardsWindow? Window { get; set; }
}

/// <summary>
/// The View menu's inspector item, for a <see cref="StandardsWindow"/>'s <see cref="NativeMenu"/>: "Show Inspector", or
/// "Hide Inspector" while the inspector shows (docked or as the overlay). Choosing it calls
/// <see cref="StandardsWindow.ToggleInspector"/>. Its gesture is Opt+Cmd+I in the macOS layout and Ctrl+Alt+I in the
/// Windows layout. It is disabled while the window has no <see cref="StandardsWindow.Inspector"/>. The headers come from
/// the <c>InspectorShowHeader</c> and <c>InspectorHideHeader</c> resources, so an app can translate them.
/// </summary>
/// <remarks>The window finds the item anywhere in its menu, at any depth, and keeps it up to date.</remarks>
public class InspectorMenuItem : NativeMenuItem, IWindowMenuItem
{
    public const string ShowHeaderKey = "InspectorShowHeader";
    public const string HideHeaderKey = "InspectorHideHeader";

    /// <summary>The default headers.</summary>
    internal const string ShowHeader = "Show Inspector";
    internal const string HideHeader = "Hide Inspector";

    /// <summary>The gesture in the macOS layout (Opt+Cmd+I, as in Pages and Keynote) and in the Windows layout (Ctrl+Alt+I).</summary>
    internal static readonly KeyGesture MacOSGesture = new(Key.I, KeyModifiers.Alt | KeyModifiers.Meta);
    internal static readonly KeyGesture WindowsGesture = new(Key.I, KeyModifiers.Control | KeyModifiers.Alt);

    private readonly PaneMenuItemState _state;

    public InspectorMenuItem()
    {
        _state = new PaneMenuItemState(this, new PaneMenuItemState.Texts(ShowHeaderKey, HideHeaderKey, ShowHeader, HideHeader),
            MacOSGesture, WindowsGesture, w => w.Inspector is not null, w => w.IsInspectorShown);
        Click += (_, _) => Window?.ToggleInspector();
    }

    /// <summary>The window whose menu holds the item, or null; set by the window.</summary>
    internal StandardsWindow? Window
    {
        get => _state.Window;
        set => _state.Window = value;
    }

    StandardsWindow? IWindowMenuItem.Window
    {
        get => Window;
        set => Window = value;
    }
}

/// <summary>
/// The View menu's bottom panel item, for a <see cref="StandardsWindow"/>'s <see cref="NativeMenu"/>: "Show Bottom Panel",
/// or "Hide Bottom Panel" while the bottom panel shows. Choosing it calls <see cref="StandardsWindow.ToggleBottomPanel"/>.
/// Its gesture is Opt+Cmd+J in the macOS layout and Ctrl+J in the Windows layout. It is disabled while the window has no
/// <see cref="StandardsWindow.BottomPanel"/>, and while its content area is too short for the panel (the toggle would only
/// lose the kept setting). The headers come from the <c>BottomPanelShowHeader</c> and
/// <c>BottomPanelHideHeader</c> resources, so an app can translate them.
/// </summary>
/// <remarks>The window finds the item anywhere in its menu, at any depth, and keeps it up to date.</remarks>
public class BottomPanelMenuItem : NativeMenuItem, IWindowMenuItem
{
    public const string ShowHeaderKey = "BottomPanelShowHeader";
    public const string HideHeaderKey = "BottomPanelHideHeader";

    /// <summary>The default headers.</summary>
    internal const string ShowHeader = "Show Bottom Panel";
    internal const string HideHeader = "Hide Bottom Panel";

    /// <summary>The gesture in the macOS layout (Opt+Cmd+J) and in the Windows layout (Ctrl+J, as in VS Code).</summary>
    internal static readonly KeyGesture MacOSGesture = new(Key.J, KeyModifiers.Alt | KeyModifiers.Meta);
    internal static readonly KeyGesture WindowsGesture = new(Key.J, KeyModifiers.Control);

    private readonly PaneMenuItemState _state;

    public BottomPanelMenuItem()
    {
        _state = new PaneMenuItemState(this, new PaneMenuItemState.Texts(ShowHeaderKey, HideHeaderKey, ShowHeader, HideHeader),
            MacOSGesture, WindowsGesture, w => w.CanToggleBottomPanel, w => w.IsBottomPanelShown);
        Click += (_, _) => Window?.ToggleBottomPanel();
    }

    /// <summary>The window whose menu holds the item, or null; set by the window.</summary>
    internal StandardsWindow? Window
    {
        get => _state.Window;
        set => _state.Window = value;
    }

    StandardsWindow? IWindowMenuItem.Window
    {
        get => Window;
        set => Window = value;
    }
}

/// <summary>
/// Keeps a panel's menu item in step with its window: the show or hide header, enabled while its toggle can act, and
/// the gesture of the window's layout (before it has a window, the layout of this OS).
/// </summary>
internal sealed class PaneMenuItemState
{
    internal readonly record struct Texts(string ShowKey, string HideKey, string Show, string Hide);

    private readonly NativeMenuItem _item;
    private readonly Texts _texts;
    private readonly KeyGesture _macOS;
    private readonly KeyGesture _windows;
    private readonly Func<StandardsWindow, bool> _canToggle;
    private readonly Func<StandardsWindow, bool> _isShown;
    private StandardsWindow? _window;

    public PaneMenuItemState(NativeMenuItem item, Texts texts, KeyGesture macOS, KeyGesture windows,
        Func<StandardsWindow, bool> canToggle, Func<StandardsWindow, bool> isShown)
    {
        _item = item;
        _texts = texts;
        _macOS = macOS;
        _windows = windows;
        _canToggle = canToggle;
        _isShown = isShown;
        item.Header = texts.Show;
        item.IsEnabled = false;
        item.Gesture = WindowLayout.Resolve(WindowLayoutMode.Automatic, OperatingSystem.IsMacOS()) == WindowLayoutMode.MacOS ? macOS : windows;
    }

    public StandardsWindow? Window
    {
        get => _window;
        set
        {
            if (_window == value)
                return;
            if (_window is not null)
            {
                _window.PanesChanged -= Update;
                _window.PropertyChanged -= OnWindowPropertyChanged;
            }
            _window = value;
            if (_window is not null)
            {
                _window.PanesChanged += Update;
                _window.PropertyChanged += OnWindowPropertyChanged;
            }
            Update();
        }
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == StandardsWindow.ResolvedLayoutModeProperty)
            Update();
    }

    private void Update()
    {
        if (_window is not { } window)
            return;
        var shown = _isShown(window);
        var key = shown ? _texts.HideKey : _texts.ShowKey;
        _item.Header = window.TryFindResource(key, out var value) && value is string text ? text : shown ? _texts.Hide : _texts.Show;
        _item.IsEnabled = _canToggle(window);
        var gesture = window.ResolvedLayoutMode == WindowLayoutMode.MacOS ? _macOS : _windows;
        if (!gesture.Equals(_item.Gesture))
            _item.Gesture = gesture;
    }
}
