using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ToggleParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The ToggleSwitch control theme: Fluent's structure (header above; track, then the On or Off
/// content) on a full-pill track, 40 x 20 at Normal and 32 x 16 at Compact. Off is a 1 px outline
/// with the knob in the outline's colour; on is the AA accent fill with a white knob, 2 px larger.
/// Pressing stretches the knob 4 px wide; the knob slides over <c>MotionNormal</c> with ease-out,
/// instantly under reduce motion. The focus ring goes around the track.
/// </summary>
internal static class ToggleSwitchTheme
{
    public const string KnobCanvasPart = "PART_SwitchKnob";
    public const string MovingKnobsPart = "PART_MovingKnobs";
    public const string KnobPart = "PART_Knob";
    public const string OnContentPart = "PART_OnContentPresenter";
    public const string OffContentPart = "PART_OffContentPresenter";

    private readonly record struct Look(object OutlineBackground, object OutlineBorder, object Fill, object Knob);

    public static ControlTheme Create()
    {
        var theme = new ControlTheme(typeof(ToggleSwitch));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, ButtonParts.Resource("ToggleSwitchContentForeground")));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, new DynamicResourceExtension(TrackRadiusKey)));
        theme.Setters.Add(new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Left));
        theme.Setters.Add(new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(ToggleSwitch.KnobTransitionsProperty, new DynamicResourceExtension(KnobTransitionsKey)));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ToggleSwitch>(Build)));
        FocusRing.AddTo(theme);

        // The header sits SpacingXS above the switch, or takes no room when empty.
        theme.Children.Add(new Avalonia.Styling.Style(x => ButtonParts.Content(x.Nesting()))
        {
            Setters = { new Setter(Layoutable.MarginProperty, new Thickness(0, 0, 0, Spacing.XS)) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => ButtonParts.Content(x.Nesting()).Class(":empty"))
        {
            Setters = { new Setter(Layoutable.MarginProperty, new Thickness(0)) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":disabled"))
        {
            Setters = { new Setter(TemplatedControl.ForegroundProperty, ButtonParts.Resource("ToggleSwitchHeaderForegroundDisabled")) },
        });

        Look Off(string state) => new($"ToggleSwitchFillOff{state}", $"ToggleSwitchStrokeOff{state}", "AccentFillColor", $"ToggleSwitchStrokeOff{state}");
        AddGroup(theme, x => x.Nesting(), on: false,
            Off(""), Off("PointerOver"), Off("Pressed"), Off("Disabled") with { OutlineBackground = Brushes.Transparent });

        Look On(string fill, string state) => new(Brushes.Transparent, fill, fill, $"ToggleSwitchKnobFillOn{state}");
        AddGroup(theme, x => x.Nesting().Class(":checked"), on: true,
            On("AccentFillColor", ""), On("AccentFillColorPointerOver", "PointerOver"), On("AccentFillColorPressed", "Pressed"),
            On("AccentButtonBackgroundDisabled", "Disabled") with { Knob = "AccentButtonForegroundDisabled" });
        return theme;
    }

    /// <summary>The off or on group: its rest look, knob size and content, then hover, pressed (knob stretched) and disabled.</summary>
    private static void AddGroup(ControlTheme theme, Func<Selector?, Selector> activate, bool on, Look rest, Look hover, Look pressed, Look disabled)
    {
        var knob = on ? KnobOnKey : KnobOffKey;
        var stretched = on ? KnobOnPressedKey : KnobOffPressedKey;
        theme.Children.Add(new Avalonia.Styling.Style(x => activate(x))
        {
            Children =
            {
                Colours(x => x.Nesting(), [.. Setters(rest),
                    (FillPart, Visual.OpacityProperty, on ? 1d : 0d),
                    (KnobPart, Layoutable.WidthProperty, new DynamicResourceExtension(knob)),
                    (KnobPart, Layoutable.HeightProperty, new DynamicResourceExtension(knob)),
                    (OnContentPart, Visual.OpacityProperty, on ? 1d : 0d),
                    (OffContentPart, Visual.OpacityProperty, on ? 0d : 1d)]),
                Colours(x => x.Nesting().Class(":pointerover"), Setters(hover)),
                Colours(x => x.Nesting().Class(":pressed"), [.. Setters(pressed),
                    (KnobPart, Layoutable.WidthProperty, new DynamicResourceExtension(stretched))]),
                Colours(x => x.Nesting().Class(":disabled"), [.. Setters(disabled),
                    (KnobPart, Layoutable.WidthProperty, new DynamicResourceExtension(knob))]),
            },
        });
    }

    private static (string, AvaloniaProperty, object)[] Setters(Look look) =>
    [
        (OutlinePart, Border.BackgroundProperty, look.OutlineBackground),
        (OutlinePart, Border.BorderBrushProperty, look.OutlineBorder),
        (FillPart, Border.BackgroundProperty, look.Fill),
        (KnobPart, Border.BackgroundProperty, look.Knob),
    ];

    private static Control Build(ToggleSwitch toggle, INameScope scope)
    {
        var outline = Layer(OutlinePart);
        outline.BorderThickness = new Thickness(1);
        var fill = Layer(FillPart);
        var knob = Layer(KnobPart, ButtonParts.Pill);
        knob.HorizontalAlignment = HorizontalAlignment.Center;
        knob.VerticalAlignment = VerticalAlignment.Center;

        // Fluent's knob parts: the moving cell (a square of the track's height) slides across the
        // canvas, whose width is the travel; ToggleSwitch sets the cell's Canvas.Left.
        var moving = new Panel { Name = MovingKnobsPart, Background = Brushes.Transparent, Children = { knob } };
        moving.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(TrackHeightKey));
        moving.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(TrackHeightKey));
        var canvas = new Canvas { Name = KnobCanvasPart, HorizontalAlignment = HorizontalAlignment.Left, Children = { moving } };
        canvas.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(KnobTravelKey));
        canvas.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(TrackHeightKey));

        var track = new Panel { Name = IndicatorPart, Children = { outline, fill, canvas } };
        track.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(TrackWidthKey));
        track.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(TrackHeightKey));
        var ringed = FocusRing.Wrap(track, scope);
        ringed.VerticalAlignment = VerticalAlignment.Center;

        var off = StateContent(OffContentPart, ToggleSwitch.OffContentProperty, ToggleSwitch.OffContentTemplateProperty);
        var on = StateContent(OnContentPart, ToggleSwitch.OnContentProperty, ToggleSwitch.OnContentTemplateProperty);
        var contents = new Panel { Margin = new Thickness(LabelGap, 0, 0, 0), Children = { off, on } };
        Grid.SetColumn(contents, 1);
        var switchRow = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto"), HorizontalAlignment = HorizontalAlignment.Left, Children = { ringed, contents } };
        switchRow.Bind(Layoutable.MinHeightProperty, new DynamicResourceExtension(ButtonParts.MinHeightKey));
        Grid.SetRow(switchRow, 1);

        var header = ButtonParts.CreatePresenter();
        header.Padding = default;
        header.Bind(Layoutable.HorizontalAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));

        foreach (var part in new Control[] { outline, fill, knob, moving, canvas, track, off, on, header })
            part.RegisterInNameScope(scope);

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto"), Children = { header, switchRow } };
        root.Bind(Panel.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        return root;
    }

    private static ContentPresenter StateContent(string name, AvaloniaProperty content, AvaloniaProperty template)
    {
        var presenter = new ContentPresenter { Name = name, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        presenter.Bind(ContentPresenter.ContentProperty, new TemplateBinding(content));
        presenter.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(template));
        return presenter;
    }
}
