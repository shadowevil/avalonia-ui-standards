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
    }

    /// <summary>Sets the density-dependent values of the Modules themes.</summary>
    public static void ApplyDensity(IResourceDictionary resources, DensityStyle density) =>
        StandardsWindowTheme.ApplyDensity(resources, density == DensityStyle.Compact);
}
