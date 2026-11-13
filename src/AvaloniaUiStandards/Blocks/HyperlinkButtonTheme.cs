using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The HyperlinkButton control theme: no fill in any state; the text is <c>AccentTextColor</c>
/// (AA on the page and on cards), <c>AccentTextColorPressed</c> while pressed, underlined as in
/// Fluent, with Fluent's hand cursor, disabled and visited colours.
/// </summary>
internal static class HyperlinkButtonTheme
{
    public static ControlTheme Create()
    {
        var theme = ContentButtonTheme(typeof(HyperlinkButton), "HyperlinkButtonBorderThemeThickness",
            (Normal: new(Brushes.Transparent, "HyperlinkButtonBorderBrush", "AccentTextColor"),
             Hover: new(Brushes.Transparent, "HyperlinkButtonBorderBrushPointerOver", "AccentTextColor"),
             Pressed: new(Brushes.Transparent, "HyperlinkButtonBorderBrushPressed", "AccentTextColorPressed"),
             Disabled: new(Brushes.Transparent, "HyperlinkButtonBorderBrushDisabled", "HyperlinkButtonForegroundDisabled")));
        theme.Setters.Add(new Setter(InputElement.CursorProperty, new Cursor(StandardCursorType.Hand)));
        theme.Setters.Add(new Setter(Inline.TextDecorationsProperty, TextDecorations.Underline));

        theme.Children.Add(Group(x => x.Nesting().Class(":visited"),
            rest: new(Brushes.Transparent, "HyperlinkButtonBorderBrush", "HyperlinkButtonForegroundVisited"),
            hover: new(Brushes.Transparent, "HyperlinkButtonBorderBrushPointerOver", "HyperlinkButtonForegroundVisitedPointerOver"),
            pressed: new(Brushes.Transparent, "HyperlinkButtonBorderBrushPressed", "HyperlinkButtonForegroundVisitedPressed"),
            disabled: new(Brushes.Transparent, "HyperlinkButtonBorderBrushDisabled", "HyperlinkButtonForegroundDisabled")));
        return theme;
    }
}
