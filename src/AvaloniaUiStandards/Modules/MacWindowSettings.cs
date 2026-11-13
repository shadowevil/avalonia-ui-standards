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
