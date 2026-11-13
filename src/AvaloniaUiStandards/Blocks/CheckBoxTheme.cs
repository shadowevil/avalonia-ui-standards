using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ToggleParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The CheckBox control theme: a squircle box (20 px, radius 5 at Normal; 16 px, radius 4 at
/// Compact) before the label. Unchecked is a 1 px outline in Fluent's unchecked colours; checked
/// and indeterminate are the AA accent fill with a white stroked check or dash. The focus ring
/// goes around the box. State keys are Fluent's CheckBox keys, with the accent fills for the
/// checked box and the disabled accent button's colours for a disabled checked box.
/// </summary>
internal static class CheckBoxTheme
{
    public const string CheckPart = "CheckGlyph";
    public const string DashPart = "DashGlyph";

    /// <summary>One state's colours: outline background and border, the checked fill, the mark, the label.</summary>
    private readonly record struct Look(object OutlineBackground, object OutlineBorder, object Fill, object Mark, object Label);

    public static ControlTheme Create()
    {
        var theme = new ControlTheme(typeof(CheckBox));
        AddRowSetters(theme);
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, new DynamicResourceExtension(BoxRadiusKey)));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<CheckBox>(Build)));
        FocusRing.AddTo(theme);

        string Unchecked(string part, string state) => $"CheckBox{part}Unchecked{state}";
        Look UncheckedLook(string state) => new(
            Unchecked("CheckBackgroundFill", state), Unchecked("CheckBackgroundStroke", state), "AccentFillColor",
            "CheckBoxCheckGlyphForegroundChecked", Unchecked("Foreground", state));
        AddGroup(theme, x => x.Nesting(), opacities: (0, 0, 0),
            UncheckedLook(""), UncheckedLook("PointerOver"), UncheckedLook("Pressed"), UncheckedLook("Disabled"));

        foreach (var (pseudoClass, name, opacities) in new[] { (":checked", "Checked", (1d, 1d, 0d)), (":indeterminate", "Indeterminate", (1d, 0d, 1d)) })
        {
            Look Filled(string fill, string state) => new(Brushes.Transparent, fill, fill, $"CheckBoxCheckGlyphForeground{name}{state}", $"CheckBoxForeground{name}{state}");
            AddGroup(theme, x => x.Nesting().Class(pseudoClass), opacities,
                Filled("AccentFillColor", ""), Filled("AccentFillColorPointerOver", "PointerOver"), Filled("AccentFillColorPressed", "Pressed"),
                Filled("AccentButtonBackgroundDisabled", "Disabled") with { Mark = "AccentButtonForegroundDisabled" });
        }
        return theme;
    }

    /// <summary>A checked state's group: its rest look and mark opacities, then hover, pressed and disabled.</summary>
    private static void AddGroup(ControlTheme theme, Func<Selector?, Selector> activate, (double Fill, double Check, double Dash) opacities,
        Look rest, Look hover, Look pressed, Look disabled)
    {
        var group = new Avalonia.Styling.Style(x => activate(x))
        {
            Children =
            {
                Colours(x => x.Nesting(), [.. Setters(rest),
                    (FillPart, Visual.OpacityProperty, opacities.Fill),
                    (CheckPart, Visual.OpacityProperty, opacities.Check),
                    (DashPart, Visual.OpacityProperty, opacities.Dash)]),
                Colours(x => x.Nesting().Class(":pointerover"), Setters(hover)),
                Colours(x => x.Nesting().Class(":pressed"), Setters(pressed)),
                Colours(x => x.Nesting().Class(":disabled"), Setters(disabled)),
            },
        };
        theme.Children.Add(group);
    }

    private static (string, AvaloniaProperty, object)[] Setters(Look look) =>
    [
        (OutlinePart, Border.BackgroundProperty, look.OutlineBackground),
        (OutlinePart, Border.BorderBrushProperty, look.OutlineBorder),
        (FillPart, Border.BackgroundProperty, look.Fill),
        (CheckPart, Shape.StrokeProperty, look.Mark),
        (DashPart, Shape.StrokeProperty, look.Mark),
        (ButtonParts.ContentPart, ContentPresenter.ForegroundProperty, look.Label),
    ];

    private static Control Build(CheckBox checkBox, INameScope scope)
    {
        var outline = Layer(OutlinePart);
        outline.BorderThickness = new Thickness(1);
        var fill = Layer(FillPart);
        var check = Glyph(CheckPart, CheckGlyphKey);
        var dash = Glyph(DashPart, DashGlyphKey);
        foreach (var part in new Control[] { outline, fill, check, dash })
            part.RegisterInNameScope(scope);

        var box = new Panel { Children = { outline, fill, check, dash } };
        box.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(BoxKey));
        box.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(BoxKey));
        return Row(box, Label(), scope);
    }
}
