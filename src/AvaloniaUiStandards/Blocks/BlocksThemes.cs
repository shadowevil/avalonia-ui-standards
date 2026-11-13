using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Themes.Fluent;

namespace AvaloniaUiStandards.Blocks;

/// <summary>The control themes of the Blocks tier and their density-dependent values.</summary>
internal static class BlocksThemes
{
    public static void AddTo(IResourceDictionary resources)
    {
        resources[typeof(Button)] = ButtonTheme.Create();
        resources[typeof(RepeatButton)] = RepeatButtonTheme.Create();
        resources[typeof(ToggleButton)] = ToggleButtonTheme.Create();
        resources[typeof(HyperlinkButton)] = HyperlinkButtonTheme.Create();
        resources[typeof(DropDownButton)] = DropDownButtonTheme.Create();
        resources[typeof(SplitButton)] = SplitButtonTheme.Create();
    }

    /// <summary>
    /// Sets the transitions of the Blocks themes for the current motion: <paramref name="fast"/>
    /// is <c>MotionFast</c>, or zero under reduce motion (no transitions).
    /// </summary>
    public static void ApplyMotion(IResourceDictionary resources, TimeSpan fast, Easing easing)
    {
        resources[ButtonParts.ChromeTransitionsKey] = ButtonParts.ChromeTransitions(fast, easing);
    }

    /// <summary>Sets the density-dependent values of the Blocks themes.</summary>
    public static void ApplyDensity(IResourceDictionary resources, DensityStyle density)
    {
        var sizes = density == DensityStyle.Compact ? ButtonParts.Compact : ButtonParts.Normal;
        resources[ButtonParts.MinHeightKey] = sizes.MinHeight;
        resources[ButtonParts.PaddingKey] = sizes.Padding;
        resources[ButtonParts.GlyphMarginKey] = new Thickness(0, 0, sizes.Padding.Right, 0);
    }
}
