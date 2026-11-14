namespace AvaloniaUiStandards.Modules;

/// <summary>
/// Which OS's window layout a <see cref="StandardsWindow"/> shows: its window buttons, where its menus
/// go, whether its toolbar band drags the window and what a title band double-click does. The native
/// frame and the keyboard shortcuts always follow the OS the app runs on.
/// </summary>
public enum WindowLayoutMode
{
    /// <summary>The OS's own layout: <see cref="MacOS"/> on macOS, <see cref="Windows"/> elsewhere (Windows and Linux).</summary>
    Automatic,

    /// <summary>
    /// Traffic lights on the left; menus in the macOS system menu bar (no in-window menu row); the toolbar
    /// band's empty area drags the window; a title band double-click follows the macOS system setting
    /// where the app runs on macOS.
    /// </summary>
    MacOS,

    /// <summary>
    /// Caption buttons on the right; menus in the in-window menu row below the title band; the toolbar
    /// band does not drag the window; a title band double-click maximises or restores.
    /// </summary>
    Windows,
}

internal static class WindowLayout
{
    /// <summary>The layout <paramref name="mode"/> stands for on this OS (<paramref name="macOS"/>): never <see cref="WindowLayoutMode.Automatic"/>.</summary>
    public static WindowLayoutMode Resolve(WindowLayoutMode mode, bool macOS) => mode switch
    {
        WindowLayoutMode.MacOS or WindowLayoutMode.Windows => mode,
        _ => macOS ? WindowLayoutMode.MacOS : WindowLayoutMode.Windows,
    };
}
