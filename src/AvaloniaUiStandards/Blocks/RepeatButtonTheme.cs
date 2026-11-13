using Avalonia.Controls;
using Avalonia.Styling;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>The RepeatButton control theme: Button's Standard look, from Fluent's RepeatButton resources.</summary>
internal static class RepeatButtonTheme
{
    public static ControlTheme Create() =>
        ContentButtonTheme(typeof(RepeatButton), "ButtonBorderThemeThickness", Family("RepeatButton"));
}
