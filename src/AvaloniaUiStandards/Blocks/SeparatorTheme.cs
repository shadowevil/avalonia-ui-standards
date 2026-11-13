using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The Separator theme: a 1 px hairline in the border colour (<c>BaseLow</c>), with
/// <c>SpacingXS</c> above and below. Inside a menu it is inset from the menu's sides as well
/// (<see cref="InMenuStyle"/>), so it stops short of the rounded corners and lines up with the items.
/// </summary>
internal static class SeparatorTheme
{
    /// <summary>The hairline's thickness.</summary>
    public const double Thickness = 1;

    /// <summary>The margin outside a menu: <c>SpacingXS</c> above and below, none at the sides.</summary>
    public static readonly Thickness Margin = new(0, Spacing.XS);

    /// <summary>
    /// The margin inside a menu: <c>SpacingS</c> at the sides, so with the menu's <c>SpacingXS</c>
    /// padding the line starts 12 px in from the menu's edge, and <c>SpacingXS</c> above and below.
    /// </summary>
    public static readonly Thickness MenuMargin = new(Spacing.S, Spacing.XS);

    public static ControlTheme Create() => new(typeof(Separator))
    {
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Resource("SystemControlForegroundBaseLowBrush")),
            new Setter(InputElement.FocusableProperty, false),
            new Setter(Layoutable.HeightProperty, Thickness),
            new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),
            new Setter(Layoutable.MarginProperty, Margin),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Separator>((separator, _) =>
            {
                var line = new Border();
                line.Bind(Border.BackgroundProperty, new Avalonia.Data.TemplateBinding(TemplatedControl.BackgroundProperty));
                return line;
            })),
        },
    };

    /// <summary>A separator placed directly in a menu, context menu, menu flyout or submenu takes the menu inset.</summary>
    public static IStyle InMenuStyle() => new Avalonia.Styling.Style(x => Selectors.Or(
        x.Is<MenuBase>().Child().OfType<Separator>(),
        x.Is<MenuItem>().Child().OfType<Separator>()))
    {
        Setters = { new Setter(Layoutable.MarginProperty, MenuMargin) },
    };
}
