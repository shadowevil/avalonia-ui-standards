using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using static AvaloniaUiStandards.Blocks.ToggleParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The RadioButton control theme: CheckBox's pattern on a circle (20 px at Normal, 16 at
/// Compact). Unchecked is a 1 px outline in Fluent's RadioButton colours; checked is the AA accent
/// fill with a white dot (8 px, 6 at Compact). The focus ring goes around the circle.
/// </summary>
internal static class RadioButtonTheme
{
    public const string DotPart = "CheckGlyph";

    // Larger than any circle: a squircle corner clamps to half the side, which is a true circle.
    private static readonly CornerRadius Round = new(1000);

    private readonly record struct Look(object OutlineBackground, object OutlineBorder, object Fill, object Dot, object Label);

    public static ControlTheme Create()
    {
        var theme = new ControlTheme(typeof(RadioButton));
        AddRowSetters(theme);
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, new DynamicResourceExtension(CircleRadiusKey)));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<RadioButton>(Build)));
        FocusRing.AddTo(theme);

        string Key(string part, string state) => $"RadioButton{part}{state}";
        Look Unchecked(string state) => new(Key("OuterEllipseFill", state), Key("OuterEllipseStroke", state), "AccentFillColor",
            Key("CheckGlyphFill", state), Key("Foreground", state));
        AddGroup(theme, x => x.Nesting(), checkedOpacity: 0,
            Unchecked(""), Unchecked("PointerOver"), Unchecked("Pressed"), Unchecked("Disabled"));

        Look Checked(string fill, string state) => new(Brushes.Transparent, fill, fill, Key("CheckGlyphFill", state), Key("Foreground", state));
        AddGroup(theme, x => x.Nesting().Class(":checked"), checkedOpacity: 1,
            Checked("AccentFillColor", ""), Checked("AccentFillColorPointerOver", "PointerOver"), Checked("AccentFillColorPressed", "Pressed"),
            Checked("AccentButtonBackgroundDisabled", "Disabled") with { Dot = "AccentButtonForegroundDisabled" });
        return theme;
    }

    private static void AddGroup(ControlTheme theme, Func<Selector?, Selector> activate, double checkedOpacity,
        Look rest, Look hover, Look pressed, Look disabled) =>
        theme.Children.Add(new Avalonia.Styling.Style(x => activate(x))
        {
            Children =
            {
                Colours(x => x.Nesting(), [.. Setters(rest),
                    (FillPart, Visual.OpacityProperty, checkedOpacity),
                    (DotPart, Visual.OpacityProperty, checkedOpacity)]),
                Colours(x => x.Nesting().Class(":pointerover"), Setters(hover)),
                Colours(x => x.Nesting().Class(":pressed"), Setters(pressed)),
                Colours(x => x.Nesting().Class(":disabled"), Setters(disabled)),
            },
        });

    private static (string, AvaloniaProperty, object)[] Setters(Look look) =>
    [
        (OutlinePart, Border.BackgroundProperty, look.OutlineBackground),
        (OutlinePart, Border.BorderBrushProperty, look.OutlineBorder),
        (FillPart, Border.BackgroundProperty, look.Fill),
        (DotPart, Border.BackgroundProperty, look.Dot),
        (ButtonParts.ContentPart, ContentPresenter.ForegroundProperty, look.Label),
    ];

    private static Control Build(RadioButton radio, INameScope scope)
    {
        var outline = Layer(OutlinePart);
        outline.BorderThickness = new Thickness(1);
        var fill = Layer(FillPart);
        var dot = Layer(DotPart, Round);
        dot.HorizontalAlignment = HorizontalAlignment.Center;
        dot.VerticalAlignment = VerticalAlignment.Center;
        dot.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(DotKey));
        dot.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(DotKey));
        foreach (var part in new Control[] { outline, fill, dot })
            part.RegisterInNameScope(scope);

        var circle = new Panel { Children = { outline, fill, dot } };
        circle.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(BoxKey));
        circle.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(BoxKey));
        return Row(circle, Label(), scope);
    }
}
