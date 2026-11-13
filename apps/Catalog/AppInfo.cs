using System.Reflection;
using AvaloniaUiStandards;

namespace Catalog;

internal static class AppInfo
{
    public const string Name = "Avalonia UI Standards Catalog";

    /// <summary>The library version, as shown in the status bar and the About window.</summary>
    public static string LibraryText
    {
        get
        {
            var version = typeof(StandardsTheme).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
            return $"AvaloniaUiStandards {version.Split('+')[0]}";
        }
    }
}
