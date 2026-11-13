using Avalonia.Controls;
using Avalonia.Themes.Fluent;

namespace AvaloniaUiStandards.Blocks;

/// <summary>The control themes of the Blocks tier and their density-dependent values.</summary>
internal static class BlocksThemes
{
    public static void AddTo(IResourceDictionary resources)
    {
        resources[typeof(Button)] = ButtonTheme.Create();
    }

    /// <summary>Sets the density-dependent values of the Blocks themes.</summary>
    public static void ApplyDensity(IResourceDictionary resources, DensityStyle density)
    {
        var button = density == DensityStyle.Compact ? ButtonTheme.Compact : ButtonTheme.Normal;
        resources[ButtonTheme.MinHeightKey] = button.MinHeight;
        resources[ButtonTheme.PaddingKey] = button.Padding;
    }
}
