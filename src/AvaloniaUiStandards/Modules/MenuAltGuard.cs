using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// Lets Alt open the menu row only for a clean press and release, as in native Windows menus. Avalonia's access key
/// handler already ignores an Alt release after another key; this covers the rest: a pointer press, release or drag, a
/// wheel turn, or the window losing focus while Alt is down. The release is then marked handled (while the row shows)
/// before the access key handler sees it. After another key the release is left to the handler, which resets its own
/// state on it. Alt with an access key and F10 are not affected.
/// </summary>
internal sealed class MenuAltGuard
{
    private readonly Func<bool> _menuRowShown;
    private bool _altDown;
    private bool _keyUsed;
    private bool _otherUsed;

    public MenuAltGuard(Window window, Func<bool> menuRowShown)
    {
        _menuRowShown = menuRowShown;
        window.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(InputElement.PointerPressedEvent, Spoil, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(InputElement.PointerReleasedEvent, Spoil, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(InputElement.PointerWheelChangedEvent, Spoil, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.Deactivated += (_, _) => _otherUsed = true;
    }

    private static bool IsAlt(Key key) => key is Key.LeftAlt or Key.RightAlt;

    private void Spoil(object? sender, RoutedEventArgs e) => _otherUsed = true;

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var properties = e.GetCurrentPoint(null).Properties;
        if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed || properties.IsMiddleButtonPressed
            || properties.IsXButton1Pressed || properties.IsXButton2Pressed)
            _otherUsed = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsAlt(e.Key))
            _keyUsed = true;
        else if (!_altDown)
        {
            // A repeat of the held Alt keeps the state; a first press starts a clean one.
            _altDown = true;
            _keyUsed = false;
            _otherUsed = false;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (!IsAlt(e.Key))
            return;
        _altDown = false;
        if (_otherUsed && !_keyUsed && _menuRowShown())
            e.Handled = true;
    }
}
