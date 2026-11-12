using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Reads the OS "reduce motion" and "reduce transparency" settings, which Avalonia does not
/// expose. Every read fails safely to "not reduced".
/// </summary>
internal static class OsAccessibility
{
    internal readonly record struct Settings(bool ReduceMotion, bool ReduceTransparency)
    {
        public static Settings NotReduced => default;
    }

    private static readonly Lazy<Settings> Cached = new(() => new(Safe(ReadReduceMotion), Safe(ReadReduceTransparency)));

    /// <summary>The OS settings, read once per process.</summary>
    public static Settings Read() => Cached.Value;

    private static bool Safe(Func<bool> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return false;
        }
    }

    private static bool ReadReduceMotion()
    {
        if (OperatingSystem.IsMacOS())
            return MacOS.WorkspaceFlag("accessibilityDisplayShouldReduceMotion");
        if (OperatingSystem.IsWindows())
            return Windows.ClientAreaAnimationOff();
        if (OperatingSystem.IsLinux())
            return Linux.AnimationsDisabled();
        return false;
    }

    private static bool ReadReduceTransparency()
    {
        if (OperatingSystem.IsMacOS())
            return MacOS.WorkspaceFlag("accessibilityDisplayShouldReduceTransparency");
        if (OperatingSystem.IsWindows())
            return Windows.TransparencyOff();
        // Linux desktops have no transparency setting.
        return false;
    }

    // [[NSWorkspace sharedWorkspace] <flag>] through the Objective-C runtime.
    private static class MacOS
    {
        private const string ObjC = "/usr/lib/libobjc.dylib";
        private const string AppKit = "/System/Library/Frameworks/AppKit.framework/AppKit";

        [DllImport(ObjC)]
        private static extern IntPtr objc_getClass(string name);

        [DllImport(ObjC)]
        private static extern IntPtr sel_registerName(string name);

        [DllImport(ObjC, EntryPoint = "objc_msgSend")]
        private static extern IntPtr SendReturningPointer(IntPtr receiver, IntPtr selector);

        // BOOL is a signed char on x64 and a bool on arm64; both fit a byte return.
        [DllImport(ObjC, EntryPoint = "objc_msgSend")]
        private static extern byte SendReturningBool(IntPtr receiver, IntPtr selector);

        public static bool WorkspaceFlag(string selector)
        {
            NativeLibrary.TryLoad(AppKit, out _);
            var workspaceClass = objc_getClass("NSWorkspace");
            if (workspaceClass == IntPtr.Zero)
                return false;
            var workspace = SendReturningPointer(workspaceClass, sel_registerName("sharedWorkspace"));
            if (workspace == IntPtr.Zero)
                return false;
            return SendReturningBool(workspace, sel_registerName(selector)) != 0;
        }
    }

    private static class Windows
    {
        private const uint SpiGetClientAreaAnimation = 0x1042;

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        private static extern int SystemParametersInfo(uint action, uint param, out int value, uint winIni);

        public static bool ClientAreaAnimationOff() =>
            SystemParametersInfo(SpiGetClientAreaAnimation, 0, out var enabled, 0) != 0 && enabled == 0;

        public static bool TransparencyOff() =>
            OperatingSystem.IsWindows()
            && Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "EnableTransparency", null) is int value
            && value == 0;
    }

    private static class Linux
    {
        // gsettings get org.gnome.desktop.interface enable-animations prints "true" or "false".
        public static bool AnimationsDisabled()
        {
            var start = new ProcessStartInfo("gsettings", "get org.gnome.desktop.interface enable-animations")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            const int timeoutMs = 1000;
            var clock = Stopwatch.StartNew();
            using var process = Process.Start(start);
            if (process is null)
                return false;
            var output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(timeoutMs))
            {
                process.Kill();
                return false;
            }
            var remaining = (int)Math.Max(0, timeoutMs - clock.ElapsedMilliseconds);
            return process.ExitCode == 0 && output.Wait(remaining) && output.Result.Trim() == "false";
        }
    }
}
