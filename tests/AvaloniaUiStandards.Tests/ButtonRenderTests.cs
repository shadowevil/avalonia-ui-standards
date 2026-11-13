using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Avalonia.Themes.Fluent;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the Button sheets (2x, labelled) to the test output (screenshots/).</summary>
public class ButtonRenderTests
{
    private static readonly (string Label, ThemeVariant Variant)[] Themes = [("Light", ThemeVariant.Light), ("Dark", ThemeVariant.Dark)];

    private static readonly (string Label, string? Class)[] Variants =
        [("Standard", null), ("Accent", "accent"), ("Subtle", "subtle"), ("Destructive", "destructive")];

    private enum State { Rest, Hover, Pressed, Disabled, Focused }

    [AvaloniaFact]
    public void States_RendersSheet()
    {
        using var sheet = new Sheet("Button: variants × states (AA fills from accent #007CEA and danger #E1332A)");
        var states = Enum.GetValues<State>();
        foreach (var (themeLabel, theme) in Themes)
            sheet.AddSection(themeLabel, states.Select(s => s.ToString()).ToList(),
                Variants.Select(v => (v.Label, (IReadOnlyList<Control>)states.Select(s => ButtonCell(sheet, theme, v.Class, s)).ToList())));
        sheet.Save("m3-button-states.png");
    }

    [AvaloniaFact]
    public void BeforeAfter_RendersFluentNextToLibrary()
    {
        var states = Enum.GetValues<State>();
        (string Label, string? Class)[] variants = [("Standard", null), ("Accent", "accent")];

        using var sheet = new Sheet("Button: Fluent default (before) and the library (after)");
        foreach (var (themeLabel, theme) in Themes)
        {
            var rows = new List<(string, IReadOnlyList<Control>)>();
            foreach (var (label, cls) in variants)
            {
                rows.Add(($"Fluent {label}", states.Select(s => ButtonCell(sheet, theme, cls, s, beforeShow: w => w.Styles.Add(new FluentTheme()))).ToList()));
                rows.Add(($"Library {label}", states.Select(s => ButtonCell(sheet, theme, cls, s)).ToList()));
            }
            sheet.AddSection(themeLabel, states.Select(s => s.ToString()).ToList(), rows);
        }
        sheet.Save("m3-button-before-after.png");
    }

    private static Control ButtonCell(Sheet sheet, ThemeVariant theme, string? cls, State state, Action<Window>? beforeShow = null)
    {
        var button = new Button { Content = "Button" };
        if (cls is not null)
            button.Classes.Add(cls);
        if (state == State.Disabled)
            button.IsEnabled = false;

        // Hover, pressed and focus are applied once shown: Button resets :pressed from its real
        // state when it joins the window. The sheets show end states, so transitions (tested in
        // ButtonThemeTests) are removed first; headless animation timing is not dependable.
        return sheet.Cell(button, theme, beforeShow, afterShow: _ =>
        {
            button.Transitions = null;
            foreach (var part in button.GetVisualDescendants().OfType<Animatable>())
                part.Transitions = null;

            if (state == State.Hover)
                ((IPseudoClasses)button.Classes).Set(":pointerover", true);
            if (state == State.Pressed)
                ((IPseudoClasses)button.Classes).Set(":pressed", true);
            if (state == State.Focused)
                button.Focus(NavigationMethod.Tab);
        });
    }
}
