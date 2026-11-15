using System.Runtime.InteropServices;

namespace AvaloniaUiStandards.Modules;

/// <summary>macOS window settings that Avalonia does not expose. Every read fails safely to null.</summary>
internal static class MacWindowSettings
{
    private const string ObjC = "/usr/lib/libobjc.dylib";
    private const string Foundation = "/System/Library/Frameworks/Foundation.framework/Foundation";

    [DllImport(ObjC)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector, string argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern double SendDouble(IntPtr receiver, IntPtr selector);

    private const string AppKit = "/System/Library/Frameworks/AppKit.framework/AppKit";

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern long SendLong(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector, long argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendBool(IntPtr receiver, IntPtr selector, byte argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendDouble(IntPtr receiver, IntPtr selector, double argument);

    /// <summary>
    /// Makes AppKit's full screen title bar window for <paramref name="window"/> (an <c>NSWindow</c>) invisible and
    /// transparent to the mouse: its content hidden, its alpha zero and its mouse events passed through. AppKit adds that
    /// window (<c>NSToolbarFullScreenWindow</c>, 44 pt high) to a titled window in full screen and slides it in below the
    /// menu bar whenever the menu bar reveals, over the library's own title band. AppKit shows it again on each reveal,
    /// so this is called on each reveal. Returns whether it was found; fails safely to false.
    /// </summary>
    /// <summary>
    /// Whether a full screen strip belongs to <paramref name="window"/>: its parent is the window; or, with no parent, it is
    /// on the window's screen (never a missing one) and both are on the active Space, so another full screen window's
    /// strip in another Space is never touched.
    /// </summary>
    internal static bool IsOwnStrip(IntPtr window, IntPtr stripParent, IntPtr windowScreen, IntPtr stripScreen,
        bool windowOnActiveSpace, bool stripOnActiveSpace) =>
        stripParent != IntPtr.Zero
            ? stripParent == window
            : windowScreen != IntPtr.Zero && stripScreen == windowScreen && windowOnActiveSpace && stripOnActiveSpace;

    public static bool HideFullScreenTitleBarWindow(IntPtr window)
    {
        if (window == IntPtr.Zero)
            return false;
        try
        {
            NativeLibrary.TryLoad(AppKit, out _);
            var appClass = objc_getClass("NSApplication");
            if (appClass == IntPtr.Zero)
                return false;
            var app = Send(appClass, sel_registerName("sharedApplication"));
            var windows = app == IntPtr.Zero ? IntPtr.Zero : Send(app, sel_registerName("windows"));
            if (windows == IntPtr.Zero)
                return false;
            var screen = Send(window, sel_registerName("screen"));
            var found = false;
            for (long i = 0, count = SendLong(windows, sel_registerName("count")); i < count; i++)
            {
                var candidate = Send(windows, sel_registerName("objectAtIndex:"), i);
                var name = Send(Send(candidate, sel_registerName("className")), sel_registerName("UTF8String"));
                if (name == IntPtr.Zero || Marshal.PtrToStringUTF8(name) != "NSToolbarFullScreenWindow")
                    continue;
                var parent = Send(candidate, sel_registerName("parentWindow"));
                var onActiveSpace = sel_registerName("isOnActiveSpace");
                if (!IsOwnStrip(window, parent, screen, Send(candidate, sel_registerName("screen")),
                        parent == IntPtr.Zero && (Send(window, onActiveSpace) & 0xFF) != 0,
                        parent == IntPtr.Zero && (Send(candidate, onActiveSpace) & 0xFF) != 0))
                    continue;
                SendBool(candidate, sel_registerName("setIgnoresMouseEvents:"), 1);
                SendDouble(candidate, sel_registerName("setAlphaValue:"), 0);
                var content = Send(candidate, sel_registerName("contentView"));
                if (content != IntPtr.Zero)
                    SendBool(content, sel_registerName("setHidden:"), 1);
                found = true;
            }
            return found;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Whether the menu bar stays shown in full screen: the "Automatically hide and show the menu bar" setting is
    /// Never (<c>AppleMenuBarVisibleInFullscreen</c> in the global domain); false when unset or unreadable.
    /// </summary>
    public static bool MenuBarVisibleInFullScreen()
    {
        try
        {
            NativeLibrary.TryLoad(Foundation, out _);
            var defaultsClass = objc_getClass("NSUserDefaults");
            var stringClass = objc_getClass("NSString");
            if (defaultsClass == IntPtr.Zero || stringClass == IntPtr.Zero)
                return false;
            var defaults = Send(defaultsClass, sel_registerName("standardUserDefaults"));
            var key = Send(stringClass, sel_registerName("stringWithUTF8String:"), "AppleMenuBarVisibleInFullscreen");
            var value = Send(defaults, sel_registerName("objectForKey:"), key);
            return value != IntPtr.Zero && (Send(value, sel_registerName("boolValue")) & 0xFF) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>The height of the app's menu bar (<c>NSApp.mainMenu.menuBarHeight</c>), in points; null when unreadable.</summary>
    public static double? MenuBarHeight()
    {
        try
        {
            NativeLibrary.TryLoad(AppKit, out _);
            var appClass = objc_getClass("NSApplication");
            if (appClass == IntPtr.Zero)
                return null;
            var app = Send(appClass, sel_registerName("sharedApplication"));
            var menu = app == IntPtr.Zero ? IntPtr.Zero : Send(app, sel_registerName("mainMenu"));
            var height = menu == IntPtr.Zero ? 0 : SendDouble(menu, sel_registerName("menuBarHeight"));
            return height > 0 ? height : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// The "double-click a window's title bar to" setting (<c>AppleActionOnDoubleClick</c> in the
    /// global domain), for example <c>Maximize</c>, <c>Minimize</c> or <c>None</c>; null when unset
    /// or unreadable. Read on each call, so a change in System Settings applies at once.
    /// </summary>
    public static string? TitleBarDoubleClickAction()
    {
        try
        {
            NativeLibrary.TryLoad(Foundation, out _);
            var defaultsClass = objc_getClass("NSUserDefaults");
            var stringClass = objc_getClass("NSString");
            if (defaultsClass == IntPtr.Zero || stringClass == IntPtr.Zero)
                return null;
            var defaults = Send(defaultsClass, sel_registerName("standardUserDefaults"));
            var key = Send(stringClass, sel_registerName("stringWithUTF8String:"), "AppleActionOnDoubleClick");
            var value = Send(defaults, sel_registerName("stringForKey:"), key);
            return value == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(Send(value, sel_registerName("UTF8String")));
        }
        catch
        {
            return null;
        }
    }
}
