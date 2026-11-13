using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AvaloniaUiStandards.Tests;

internal enum CellState { Rest, Hover, Pressed, Disabled, Focused }

/// <summary>Renders a control in one interaction state as a sheet cell.</summary>
internal static class StateCells
{
    public static readonly CellState[] All = Enum.GetValues<CellState>();

    public static Control Render(Sheet sheet, ThemeVariant theme, Control control, CellState state, Action<Window>? beforeShow = null)
    {
        if (state == CellState.Disabled)
            control.IsEnabled = false;

        // Hover, pressed and focus are applied once shown: Button resets :pressed from its real
        // state when it joins the window. The sheets show end states, so transitions (tested in
        // the theme tests) are removed first; headless animation timing is not dependable.
        return sheet.Cell(control, theme, beforeShow, afterShow: _ =>
        {
            control.Transitions = null;
            foreach (var part in control.GetVisualDescendants().OfType<Animatable>())
                part.Transitions = null;

            // A SplitButton's hover and press happen on its primary part.
            var target = control.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == "PART_PrimaryButton") ?? control;
            if (state == CellState.Hover)
                ((IPseudoClasses)target.Classes).Set(":pointerover", true);
            if (state == CellState.Pressed)
                ((IPseudoClasses)target.Classes).Set(":pressed", true);
            if (state == CellState.Focused)
                control.Focus(NavigationMethod.Tab);
        });
    }
}
