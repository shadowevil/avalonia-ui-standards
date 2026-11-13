using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace AvaloniaUiStandards.Blocks;

/// <summary>The control themes of the Blocks tier and their density-dependent values.</summary>
internal static class BlocksThemes
{
    /// <param name="fluentTheme">Fluent's theme for a control type, for themes that build on Fluent's.</param>
    public static void AddTo(IResourceDictionary resources, Func<Type, ControlTheme?> fluentTheme)
    {
        resources[typeof(ListBox)] = ListItemTheme.ListBox(fluentTheme(typeof(ListBox)));
        resources[typeof(ListBoxItem)] = ListItemTheme.Item(typeof(ListBoxItem));
        resources[typeof(ComboBoxItem)] = ListItemTheme.Item(typeof(ComboBoxItem));
        resources[typeof(ComboBox)] = ComboBoxTheme.Create(fluentTheme(typeof(ComboBox)));
        resources[typeof(ScrollViewer)] = ScrollViewerTheme.Create(fluentTheme(typeof(ScrollViewer)));
        ScrollViewerTheme.AddResources(resources);
        resources[typeof(ScrollBar)] = ScrollBarTheme.Create(fluentTheme(typeof(ScrollBar)));
        ScrollBarTheme.AddResources(resources);

        resources[typeof(Button)] = ButtonTheme.Create();
        resources[typeof(RepeatButton)] = RepeatButtonTheme.Create();
        resources[typeof(ToggleButton)] = ToggleButtonTheme.Create();
        resources[typeof(HyperlinkButton)] = HyperlinkButtonTheme.Create();
        resources[typeof(DropDownButton)] = DropDownButtonTheme.Create();
        resources[typeof(SplitButton)] = SplitButtonTheme.Create();
        resources[typeof(TrafficLightButton)] = WindowButtonThemes.TrafficLight();
        resources[typeof(CaptionButton)] = WindowButtonThemes.Caption();
        WindowButtonThemes.AddResources(resources);
    }

    /// <summary>
    /// Sets the transitions of the Blocks themes for the current motion: <paramref name="fast"/>
    /// is <c>MotionFast</c>, or zero under reduce motion (no transitions).
    /// </summary>
    public static void ApplyMotion(IResourceDictionary resources, TimeSpan fast, Easing easing)
    {
        resources[ButtonParts.ChromeTransitionsKey] = ButtonParts.ChromeTransitions(fast, easing);
        resources[ButtonParts.FadeTransitionsKey] = ButtonParts.FadeTransitions(fast, easing);
        resources[ScrollBarTheme.PartTransitionsKey] = ScrollBarTheme.PartTransitions(fast, easing);
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
