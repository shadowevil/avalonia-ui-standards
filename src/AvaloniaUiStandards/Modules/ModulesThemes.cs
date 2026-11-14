using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace AvaloniaUiStandards.Modules;

/// <summary>The control themes of the Modules tier and their density-dependent values.</summary>
internal static class ModulesThemes
{
    /// <param name="windowBase">Fluent's Window theme, which StandardsWindow's theme is based on.</param>
    public static void AddTo(IResourceDictionary resources, ControlTheme? windowBase)
    {
        resources[typeof(StandardsWindow)] = StandardsWindowTheme.Create(windowBase);
        StandardsWindowTheme.AddResources(resources);
        resources[typeof(Sidebar)] = SidebarTheme.Create();
        resources[typeof(SidebarItem)] = SidebarTheme.Item();
        resources[typeof(SidebarHeader)] = SidebarTheme.Header();
        resources[typeof(SidebarFooter)] = SidebarTheme.Footer();
        resources[SidebarTheme.FooterTextKey] = SidebarTheme.FooterText;
        resources[typeof(ToolbarBand)] = ToolbarBandTheme.Create();
    }

    /// <summary>
    /// Sets the transitions of the Modules themes for the current motion: <paramref name="normal"/>
    /// is <c>MotionNormal</c>, or zero under reduce motion (no transitions).
    /// </summary>
    public static void ApplyMotion(IResourceDictionary resources, TimeSpan normal, Easing easing) =>
        resources[SidebarTheme.WidthTransitionsKey] = SidebarTheme.WidthTransitions(normal, easing);

    /// <summary>Sets the density-dependent values of the Modules themes.</summary>
    public static void ApplyDensity(IResourceDictionary resources, DensityStyle density)
    {
        StandardsWindowTheme.ApplyDensity(resources, density == DensityStyle.Compact);
        SidebarTheme.ApplyDensity(resources, density == DensityStyle.Compact);
        ToolbarBandTheme.ApplyDensity(resources, density == DensityStyle.Compact);
    }
}
