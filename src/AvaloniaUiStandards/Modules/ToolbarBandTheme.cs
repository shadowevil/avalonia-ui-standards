using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using AvaloniaUiStandards.Blocks;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The ToolbarBand theme and the styles of its items: the band is the title band's height (48,
/// or 40 at Compact) and a toolbar to automation; icon buttons are square at the button height,
/// with the icon centred; an icon drop-down keeps the same inset with its chevron after the icon;
/// separators stand upright, 1 px wide and the icon's height.
/// </summary>
internal static class ToolbarBandTheme
{
    /// <summary>The band's automation name unless the app gives one.</summary>
    public const string AutomationName = "Toolbar";

    // The button family's border (ButtonBorderThemeThickness).
    private const double BorderWidth = 1;

    // Density-dependent values under non-public keys: the icon drop-down's padding and chevron
    // margin, which inset the icon and the chevron as far from the edges as a square icon button
    // insets its icon (the button height less the icon size, halved, less the 1 px border), with
    // SpacingXS between them.
    internal static readonly object DropDownPaddingKey = new();
    internal static readonly object DropDownGlyphMarginKey = new();

    public static void ApplyDensity(IResourceDictionary resources, bool compact)
    {
        var (height, icon) = compact ? (ButtonParts.Compact.MinHeight, IconTheme.Compact) : (ButtonParts.Normal.MinHeight, IconTheme.Normal);
        var inset = (height - icon) / 2 - BorderWidth;
        resources[DropDownPaddingKey] = new Thickness(inset, 0, Style.Spacing.XS, 0);
        resources[DropDownGlyphMarginKey] = new Thickness(0, 0, inset, 0);
    }

    public static ControlTheme Create() => new(typeof(ToolbarBand))
    {
        Setters =
        {
            new Setter(Layoutable.HeightProperty, new DynamicResourceExtension(StandardsWindowTheme.TitleBandHeightKey)),
            new Setter(AutomationProperties.ControlTypeOverrideProperty, AutomationControlType.ToolBar),
            new Setter(AutomationProperties.NameProperty, AutomationName),
        },
    };

    /// <summary>
    /// The item styles. A control theme reaches only its template, so these are theme-wide
    /// styles on the band's items (its logical children).
    /// </summary>
    public static IEnumerable<IStyle> ItemStyles() =>
    [
        Item<Button>(Square()),
        Item<ToggleButton>(Square()),
        Item<DropDownButton>(
        [
            new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(DropDownPaddingKey)),
        ]),
        new Avalonia.Styling.Style(x => x.OfType<ToolbarBand>().Child().OfType<DropDownButton>()
            .PropertyEquals(ToolbarBand.IsIconItemProperty, true).Template().OfType<PathIcon>().Name(DropDownButtonTheme.GlyphPart))
        {
            Setters = { new Setter(Layoutable.MarginProperty, new DynamicResourceExtension(DropDownGlyphMarginKey)) },
        },
        new Avalonia.Styling.Style(x => x.OfType<ToolbarBand>().Child().OfType<Separator>())
        {
            Setters =
            {
                new Setter(Layoutable.WidthProperty, SeparatorTheme.Thickness),
                new Setter(Layoutable.HeightProperty, new DynamicResourceExtension(IconTheme.SizeKey)),
                new Setter(Layoutable.MarginProperty, new Thickness(0)),
                new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Center),
                new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center),
            },
        },
    ];

    // As wide as the button height, with the icon centred.
    private static SetterBase[] Square() =>
    [
        new Setter(TemplatedControl.PaddingProperty, new Thickness(0)),
        new Setter(Layoutable.MinWidthProperty, new DynamicResourceExtension(ButtonParts.MinHeightKey)),
        new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
    ];

    // An icon item of exactly type T directly in a band.
    private static Avalonia.Styling.Style Item<T>(SetterBase[] setters) where T : Control
    {
        var style = new Avalonia.Styling.Style(x => x.OfType<ToolbarBand>().Child().OfType<T>().PropertyEquals(ToolbarBand.IsIconItemProperty, true));
        foreach (var setter in setters)
            style.Setters.Add(setter);
        return style;
    }
}
