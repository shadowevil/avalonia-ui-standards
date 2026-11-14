using Avalonia.Controls;

namespace Catalog;

/// <summary>
/// The status bar's frame readout: the window's rendered frames per second, whether its continuous animation runs at
/// the display rate (<c>StandardsWindow.HighRefreshRate</c>), and why that animation is paused when it is.
/// </summary>
internal static class FrameStatus
{
    /// <summary>
    /// Why the window's frame ticks are paused, or null while they run: reduce motion stops them, and they pause
    /// while the window is minimised or inactive.
    /// </summary>
    public static string? PauseReason(bool reduceMotion, bool active, WindowState state) =>
        reduceMotion ? "reduce motion"
        : state == WindowState.Minimized ? "minimised"
        : !active ? "window inactive"
        : null;

    /// <summary>The readout's texts, longest first, so a narrow status bar can take the first that fits.</summary>
    public static string[] Texts(int framesPerSecond, bool highRefreshRate, string? pauseReason)
    {
        var fps = $"{framesPerSecond} fps";
        var onOff = highRefreshRate ? "on" : "off";
        return pauseReason is null
            ? [$"{fps} · High refresh rate: {onOff}", $"{fps} · High refresh: {onOff}", fps]
            : [$"{fps} · High refresh rate: {onOff} · Animation paused ({pauseReason})", $"{fps} · High refresh: {onOff} · Paused", $"{fps} · Paused", fps];
    }
}
