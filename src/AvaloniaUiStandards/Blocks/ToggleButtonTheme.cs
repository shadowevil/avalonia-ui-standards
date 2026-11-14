using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The ToggleButton control theme: Button's Standard look when unchecked (Fluent's ToggleButton
/// resources); checked uses the AA accent fills with Fluent's checked text and borders, and
/// indeterminate follows Fluent. Group order follows Fluent: indeterminate, then checked. The
/// <c>subtle</c> class has no fill when unchecked until hover, as Button's; it comes first, so
/// indeterminate and checked keep their fills.
/// </summary>
internal static class ToggleButtonTheme
{
    public static ControlTheme Create()
    {
        var looks = Family("ToggleButton");
        var theme = ContentButtonTheme(typeof(ToggleButton), "ToggleButtonBorderThemeThickness", looks);

        // Subtle: no fill when unchecked until hover; ToggleButton's hover and pressed resources.
        theme.Children.Add(Group(x => x.Nesting().Class("subtle"),
            rest: new(Brushes.Transparent, Brushes.Transparent, looks.Normal.Foreground),
            hover: looks.Hover,
            pressed: looks.Pressed,
            disabled: new(Brushes.Transparent, Brushes.Transparent, looks.Disabled.Foreground)));

        theme.Children.Add(Group(x => x.Nesting().Class(":indeterminate"),
            rest: new("ToggleButtonBackgroundIndeterminate", "ToggleButtonBorderBrushIndeterminate", "ToggleButtonForegroundIndeterminate"),
            hover: new("ToggleButtonBackgroundIndeterminatePointerOver", "ToggleButtonBorderBrushIndeterminatePointerOver", "ToggleButtonForegroundIndeterminatePointerOver"),
            pressed: new("ToggleButtonBackgroundIndeterminatePressed", "ToggleButtonBorderBrushIndeterminatePressed", "ToggleButtonForegroundIndeterminatePressed"),
            disabled: new("ToggleButtonBackgroundIndeterminateDisabled", "ToggleButtonBorderBrushIndeterminateDisabled", "ToggleButtonForegroundIndeterminateDisabled")));

        theme.Children.Add(Group(x => x.Nesting().Class(":checked"),
            rest: new("AccentFillColor", "ToggleButtonBorderBrushChecked", "ToggleButtonForegroundChecked"),
            hover: new("AccentFillColorPointerOver", "ToggleButtonBorderBrushCheckedPointerOver", "ToggleButtonForegroundCheckedPointerOver"),
            pressed: new("AccentFillColorPressed", "ToggleButtonBorderBrushCheckedPressed", "ToggleButtonForegroundCheckedPressed"),
            disabled: new("ToggleButtonBackgroundCheckedDisabled", "ToggleButtonBorderBrushCheckedDisabled", "ToggleButtonForegroundCheckedDisabled")));

        return theme;
    }
}
