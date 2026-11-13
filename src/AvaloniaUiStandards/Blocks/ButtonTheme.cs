using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The Button control theme: Fluent's Button (12.1.6) with its chrome drawn by a
/// <see cref="Style.SquircleBorder"/>. Template part, behaviour, colour resources and state structure
/// follow Fluent; the variants are Standard (no class), <c>accent</c>, <c>subtle</c> and
/// <c>destructive</c>.
/// </summary>
/// <remarks>
/// Template: <c>SquircleBorder#PART_Chrome</c> (background, border, corner shape, pressed scale)
/// around Fluent's <c>ContentPresenter#PART_ContentPresenter</c> (content, padding, foreground).
/// </remarks>
internal static class ButtonTheme
{
    public static ControlTheme Create()
    {
        // Standard: Fluent's Button resources.
        var theme = ContentButtonTheme(typeof(Button), "ButtonBorderThemeThickness", Family("Button"));

        // Accent: the AA accent fills, with Fluent's AccentButton text, borders and disabled state.
        theme.Children.Add(Group(x => x.Nesting().Class("accent"),
            rest: new("AccentFillColor", "AccentButtonBorderBrush", "AccentButtonForeground"),
            hover: new("AccentFillColorPointerOver", "AccentButtonBorderBrushPointerOver", "AccentButtonForegroundPointerOver"),
            pressed: new("AccentFillColorPressed", "AccentButtonBorderBrushPressed", "AccentButtonForegroundPressed"),
            disabled: new("AccentButtonBackgroundDisabled", "AccentButtonBorderBrushDisabled", "AccentButtonForegroundDisabled")));

        // Subtle: no fill at rest; Standard's hover, pressed and disabled resources.
        var standard = Family("Button");
        theme.Children.Add(Group(x => x.Nesting().Class("subtle"),
            rest: new(Brushes.Transparent, Brushes.Transparent, "ButtonForeground"),
            hover: standard.Hover,
            pressed: standard.Pressed,
            disabled: new(Brushes.Transparent, Brushes.Transparent, "ButtonForegroundDisabled")));

        // Destructive: the AA danger fills and DangerForeground, with Accent's borders and disabled state.
        theme.Children.Add(Group(x => x.Nesting().Class("destructive"),
            rest: new("DangerFillColor", "AccentButtonBorderBrush", "DangerForeground"),
            hover: new("DangerFillColorPointerOver", "AccentButtonBorderBrushPointerOver", "DangerForeground"),
            pressed: new("DangerFillColorPressed", "AccentButtonBorderBrushPressed", "DangerForeground"),
            disabled: new("AccentButtonBackgroundDisabled", "AccentButtonBorderBrushDisabled", "AccentButtonForegroundDisabled")));

        return theme;
    }
}
