using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The ToggleButton control theme: Button's Standard look when unchecked (Fluent's ToggleButton
/// resources); checked uses the AA accent fills with Fluent's checked text and borders, and
/// indeterminate follows Fluent. Group order follows Fluent: indeterminate, then checked.
/// </summary>
internal static class ToggleButtonTheme
{
    public static ControlTheme Create()
    {
        var theme = ContentButtonTheme(typeof(ToggleButton), "ToggleButtonBorderThemeThickness", Family("ToggleButton"));

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
