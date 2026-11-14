using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The View menu's full screen item, for a <see cref="StandardsWindow"/>'s <see cref="NativeMenu"/>: "Enter Full Screen",
/// or "Exit Full Screen" while the window is in full screen, with Ctrl+Cmd+F. Choosing it calls
/// <see cref="StandardsWindow.ToggleFullScreen"/>. Full screen is a macOS feature: on other OSes the item is hidden. It is
/// disabled while the window cannot enter full screen (<see cref="Window.CanMaximize"/> false). The headers come from the
/// <c>FullScreenEnterHeader</c> and <c>FullScreenExitHeader</c> resources, so an app can translate them.
/// </summary>
/// <remarks>The window finds the item anywhere in its menu, at any depth, and keeps it up to date.</remarks>
public class FullScreenMenuItem : NativeMenuItem
{
    public const string EnterHeaderKey = "FullScreenEnterHeader";
    public const string ExitHeaderKey = "FullScreenExitHeader";

    /// <summary>The default headers (the macOS menu item's).</summary>
    internal const string EnterHeader = "Enter Full Screen";
    internal const string ExitHeader = "Exit Full Screen";

    private StandardsWindow? _window;

    public FullScreenMenuItem()
    {
        Header = EnterHeader;
        Gesture = new KeyGesture(Key.F, KeyModifiers.Control | KeyModifiers.Meta);
        IsVisible = OperatingSystem.IsMacOS();
        Click += (_, _) => _window?.ToggleFullScreen();
    }

    /// <summary>The window whose menu holds the item, or null; set by the window.</summary>
    internal StandardsWindow? Window
    {
        get => _window;
        set
        {
            if (_window == value)
                return;
            if (_window is not null)
                _window.PropertyChanged -= OnWindowPropertyChanged;
            _window = value;
            if (_window is not null)
                _window.PropertyChanged += OnWindowPropertyChanged;
            Update();
        }
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Avalonia.Controls.Window.WindowStateProperty || e.Property == Avalonia.Controls.Window.CanMaximizeProperty)
            Update();
    }

    private void Update()
    {
        if (_window is null)
            return;
        var fullScreen = _window.WindowState == WindowState.FullScreen;
        var key = fullScreen ? ExitHeaderKey : EnterHeaderKey;
        Header = _window.TryFindResource(key, out var value) && value is string text ? text : fullScreen ? ExitHeader : EnterHeader;
        IsVisible = _window.RunsOnMacOS;
        IsEnabled = _window.IsFullScreenAvailable;
    }
}
