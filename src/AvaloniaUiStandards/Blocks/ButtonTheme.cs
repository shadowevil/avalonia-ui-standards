using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The Button control theme: Fluent's Button (12.1.6) with its chrome drawn by a
/// <see cref="SquircleBorder"/>. Template part, behaviour, colour resources and state structure
/// follow Fluent; the variants are Standard (no class), <c>accent</c>, <c>subtle</c> and
/// <c>destructive</c>.
/// </summary>
/// <remarks>
/// Template: <c>SquircleBorder#PART_Chrome</c> (background, border, corner shape, pressed scale)
/// around Fluent's <c>ContentPresenter#PART_ContentPresenter</c> (content, padding, foreground).
/// </remarks>
internal static class ButtonTheme
{
    public const string ChromePart = "PART_Chrome";
    public const string ContentPart = "PART_ContentPresenter";

    // Density-dependent values, set by BlocksThemes.ApplyDensity under these non-public keys.
    internal static readonly object MinHeightKey = new();
    internal static readonly object PaddingKey = new();

    /// <summary>Normal density: height 32, SpacingM at the sides and SpacingXS top and bottom (replaces Fluent's 8,5,8,6).</summary>
    public static readonly (double MinHeight, Thickness Padding) Normal = (32, new Thickness(Spacing.M, Spacing.XS));

    /// <summary>Compact density: height 24, SpacingS at the sides and 2 top and bottom.</summary>
    public static readonly (double MinHeight, Thickness Padding) Compact = (24, new Thickness(Spacing.S, 2));

    private static readonly ITransform Rest = TransformOperations.Identity;
    private static readonly ITransform Pressed = TransformOperations.Parse("scale(0.98)");

    public static ControlTheme Create() => new(typeof(Button))
    {
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Resource("ButtonBackground")),
            new Setter(TemplatedControl.ForegroundProperty, Resource("ButtonForeground")),
            new Setter(TemplatedControl.BorderBrushProperty, Resource("ButtonBorderBrush")),
            new Setter(TemplatedControl.BorderThicknessProperty, Resource("ButtonBorderThemeThickness")),
            new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")),
            new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(PaddingKey)),
            new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(MinHeightKey)),
            new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Left),
            new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center),
            new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center),
            new Setter(Control.FocusAdornerProperty, FocusRing.Template),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Button>(BuildTemplate)),
        },
        Children =
        {
            new Avalonia.Styling.Style(x => Chrome(x.Nesting())) { Setters = { new Setter(Visual.RenderTransformProperty, Rest) } },
            new Avalonia.Styling.Style(x => Chrome(x.Nesting().Class(":pressed"))) { Setters = { new Setter(Visual.RenderTransformProperty, Pressed) } },

            // Standard: Fluent's Button resources.
            State(":pointerover", "ButtonBackgroundPointerOver", "ButtonBorderBrushPointerOver", "ButtonForegroundPointerOver"),
            State(":pressed", "ButtonBackgroundPressed", "ButtonBorderBrushPressed", "ButtonForegroundPressed"),
            State(":disabled", "ButtonBackgroundDisabled", "ButtonBorderBrushDisabled", "ButtonForegroundDisabled"),

            // Accent: the AA accent fills, with Fluent's AccentButton text, borders and disabled state.
            Variant("accent",
                rest: ("AccentFillColor", "AccentButtonBorderBrush", "AccentButtonForeground"),
                hover: ("AccentFillColorPointerOver", "AccentButtonBorderBrushPointerOver", "AccentButtonForegroundPointerOver"),
                pressed: ("AccentFillColorPressed", "AccentButtonBorderBrushPressed", "AccentButtonForegroundPressed"),
                disabled: ("AccentButtonBackgroundDisabled", "AccentButtonBorderBrushDisabled", "AccentButtonForegroundDisabled")),

            // Subtle: no fill at rest; Standard's hover, pressed and disabled resources.
            Variant("subtle",
                rest: (Brushes.Transparent, Brushes.Transparent, "ButtonForeground"),
                hover: ("ButtonBackgroundPointerOver", "ButtonBorderBrushPointerOver", "ButtonForegroundPointerOver"),
                pressed: ("ButtonBackgroundPressed", "ButtonBorderBrushPressed", "ButtonForegroundPressed"),
                disabled: (Brushes.Transparent, Brushes.Transparent, "ButtonForegroundDisabled")),

            // Destructive: the AA danger fills and DangerForeground, with Accent's borders and disabled state.
            Variant("destructive",
                rest: ("DangerFillColor", "AccentButtonBorderBrush", "DangerForeground"),
                hover: ("DangerFillColorPointerOver", "AccentButtonBorderBrushPointerOver", "DangerForeground"),
                pressed: ("DangerFillColorPressed", "AccentButtonBorderBrushPressed", "DangerForeground"),
                disabled: ("AccentButtonBackgroundDisabled", "AccentButtonBorderBrushDisabled", "AccentButtonForegroundDisabled")),
        },
    };

    private static Control BuildTemplate(Button button, INameScope scope)
    {
        var presenter = new ContentPresenter { Name = ContentPart, RecognizesAccessKey = true };
        presenter.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        presenter.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        presenter.Bind(ContentPresenter.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        presenter.Bind(ContentPresenter.HorizontalContentAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        presenter.Bind(ContentPresenter.VerticalContentAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));

        var chrome = new SquircleBorder { Name = ChromePart, Child = presenter };
        chrome.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        chrome.Bind(SquircleBorder.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        chrome.Bind(SquircleBorder.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        chrome.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        chrome.Transitions = MotionTransitions.Create(chrome, "MotionFast",
            new BrushTransition { Property = SquircleBorder.BackgroundProperty },
            new BrushTransition { Property = SquircleBorder.BorderBrushProperty },
            new TransformOperationsTransition { Property = Visual.RenderTransformProperty });

        chrome.RegisterInNameScope(scope);
        presenter.RegisterInNameScope(scope);
        return chrome;
    }

    private static Selector Chrome(Selector state) => state.Template().OfType<SquircleBorder>().Name(ChromePart);

    private static Selector Content(Selector state) => state.Template().OfType<ContentPresenter>().Name(ContentPart);

    // One state of the current variant: background and border on the chrome, foreground on the content.
    private static Avalonia.Styling.Style State(string? pseudoClass, object background, object borderBrush, object foreground)
    {
        Selector Activated(Selector? x) => pseudoClass is null ? x.Nesting() : x.Nesting().Class(pseudoClass);
        return new Avalonia.Styling.Style(x => Activated(x))
        {
            Children =
            {
                new Avalonia.Styling.Style(x => Chrome(x.Nesting()))
                {
                    Setters =
                    {
                        new Setter(SquircleBorder.BackgroundProperty, Value(background)),
                        new Setter(SquircleBorder.BorderBrushProperty, Value(borderBrush)),
                    },
                },
                new Avalonia.Styling.Style(x => Content(x.Nesting())) { Setters = { new Setter(ContentPresenter.ForegroundProperty, Value(foreground)) } },
            },
        };
    }

    private static Avalonia.Styling.Style Variant(string className,
        (object Background, object BorderBrush, object Foreground) rest,
        (object Background, object BorderBrush, object Foreground) hover,
        (object Background, object BorderBrush, object Foreground) pressed,
        (object Background, object BorderBrush, object Foreground) disabled) =>
        new(x => x.Nesting().Class(className))
        {
            Children =
            {
                State(null, rest.Background, rest.BorderBrush, rest.Foreground),
                State(":pointerover", hover.Background, hover.BorderBrush, hover.Foreground),
                State(":pressed", pressed.Background, pressed.BorderBrush, pressed.Foreground),
                State(":disabled", disabled.Background, disabled.BorderBrush, disabled.Foreground),
            },
        };

    private static object Value(object value) => value is string key ? Resource(key) : value;

    private static DynamicResourceExtension Resource(string key) => new(key);
}
