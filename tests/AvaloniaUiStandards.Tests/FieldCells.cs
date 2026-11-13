using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Tests;

internal enum FieldState { Rest, Hover, Pressed, Focused, KeyboardFocused, Error, Disabled }

/// <summary>Renders a field Block (TextInput, ComboBox, NumericUpDown, …) in one state and density as a sheet cell.</summary>
internal static class FieldCells
{
    public static readonly (string Label, DensityStyle Density)[] Densities = [("Normal", DensityStyle.Normal), ("Compact", DensityStyle.Compact)];

    public static string Label(FieldState state) => state switch
    {
        FieldState.Focused => "Focused (pointer)",
        FieldState.KeyboardFocused => "Focused (keyboard)",
        _ => state.ToString(),
    };

    /// <summary>
    /// The control in <paramref name="state"/>. Focus goes to <paramref name="focusTarget"/> (the
    /// control by default); <paramref name="afterShow"/> runs once shown, before the state is set.
    /// </summary>
    public static Control Render(Sheet sheet, ThemeVariant theme, DensityStyle density, Control control, FieldState state,
        Func<Control, InputElement>? focusTarget = null, Action<Window>? afterShow = null)
    {
        if (state == FieldState.Disabled)
            control.IsEnabled = false;
        return sheet.Cell(control, theme,
            beforeShow: w =>
            {
                if (density != DensityStyle.Normal)
                    w.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = density });
            },
            afterShow: w =>
            {
                afterShow?.Invoke(w);
                // End states only: transitions are removed first (headless animation timing is not dependable).
                control.Transitions = null;
                foreach (var part in control.GetVisualDescendants().OfType<Animatable>())
                    part.Transitions = null;
                var pseudo = (IPseudoClasses)control.Classes;
                switch (state)
                {
                    case FieldState.Hover:
                        pseudo.Set(":pointerover", true);
                        break;
                    case FieldState.Pressed:
                        pseudo.Set(":pressed", true);
                        break;
                    case FieldState.Error:
                        pseudo.Set(":error", true);
                        break;
                    case FieldState.Focused:
                        (focusTarget?.Invoke(control) ?? control).Focus(NavigationMethod.Pointer);
                        break;
                    case FieldState.KeyboardFocused:
                        (focusTarget?.Invoke(control) ?? control).Focus(NavigationMethod.Tab);
                        break;
                }
            });
    }

    /// <summary>A cell of the same control under Fluent's own theme, for before/after sheets.</summary>
    public static Control RenderFluent(Sheet sheet, ThemeVariant theme, Control control, FieldState state, Func<Control, InputElement>? focusTarget = null) =>
        sheet.Cell(control, theme, beforeShow: w => w.Styles.Add(new FluentTheme()), afterShow: _ =>
        {
            if (state == FieldState.Disabled)
                control.IsEnabled = false;
            if (state == FieldState.Hover)
                ((IPseudoClasses)control.Classes).Set(":pointerover", true);
            if (state is FieldState.Focused or FieldState.KeyboardFocused)
                (focusTarget?.Invoke(control) ?? control).Focus(state == FieldState.Focused ? NavigationMethod.Pointer : NavigationMethod.Tab);
        });
}
