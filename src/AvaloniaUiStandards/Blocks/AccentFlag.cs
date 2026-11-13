using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The accent flag of a selected list item (<see cref="SelectionOptions.AccentFlagProperty"/>): a
/// full-height bar in a lighter accent (the disabled text colour when disabled) on the chrome's
/// leading edge, the chrome's leading corners square and the focus ring following that shape. The
/// bar fades in and out with the chrome's fill (the chrome transitions, none under reduce motion).
/// The bar has no fill of its own outside the flagged state: a value set in the template would
/// outrank the theme's styles.
/// </summary>
internal static class AccentFlag
{
    public const string Part = "PART_AccentFlag";

    /// <summary>The bar's colour on the selected fill: a lighter step of the accent, distinct from both the fill and the surface beside it.</summary>
    public const string BarKey = "SystemAccentColorLight2";

    /// <summary>The bar's width.</summary>
    public const double Width = 3;

    // The chrome's corners with the leading ones square, and the ring's grown around that shape.
    private static readonly IValueConverter SquareLeading = new FuncValueConverter<CornerRadius, CornerRadius>(r =>
        new CornerRadius(0, r.TopRight, r.BottomRight, 0));

    private static readonly IValueConverter SquareLeadingRing = new FuncValueConverter<CornerRadius, CornerRadius>(r =>
        new CornerRadius(FocusRing.Offset, r.TopRight + FocusRing.Offset, r.BottomRight + FocusRing.Offset, FocusRing.Offset));

    /// <summary>The chrome with the bar over its leading edge.</summary>
    public static Control Add(Control chrome, INameScope scope)
    {
        var bar = new SquircleBorder
        {
            Name = Part,
            Width = Width,
            HorizontalAlignment = HorizontalAlignment.Left,
            IsHitTestVisible = false,
        };
        bar.RegisterInNameScope(scope);
        return new Panel { Children = { chrome, bar } };
    }

    /// <summary>
    /// Adds the flag's states to an item theme whose template holds <see cref="Add"/>'s bar and
    /// <paramref name="chromePart"/>, wrapped with <see cref="FocusRing.Wrap"/>.
    /// </summary>
    public static void AddTo(ControlTheme theme, string chromePart = ChromePart)
    {
        theme.Children.Add(ChromeMotion(Part));
        theme.Children.Add(new Avalonia.Styling.Style(x => Flagged(x))
        {
            Children =
            {
                PartStyle(Part, new Setter(SquircleBorder.BackgroundProperty, Resource(BarKey))),
                PartStyle(chromePart, new Setter(SquircleBorder.CornerRadiusProperty,
                    new TemplateBinding(TemplatedControl.CornerRadiusProperty) { Converter = SquareLeading })),
                PartStyle(FocusRing.Part, new Setter(SquircleBorder.CornerRadiusProperty,
                    new TemplateBinding(TemplatedControl.CornerRadiusProperty) { Converter = SquareLeadingRing })),
            },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => Flagged(x).Class(":disabled"))
        {
            Children = { PartStyle(Part, new Setter(SquircleBorder.BackgroundProperty, Resource("AccentButtonForegroundDisabled"))) },
        });
    }

    private static Selector Flagged(Selector? x) =>
        x.Nesting().Class(":selected").PropertyEquals(SelectionOptions.AccentFlagProperty, true);

    private static Avalonia.Styling.Style PartStyle(string part, Setter setter) =>
        new(x => x.Nesting().Template().OfType<SquircleBorder>().Name(part)) { Setters = { setter } };
}
