using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
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
/// The parts shared by the button-family themes (Button, RepeatButton, ToggleButton,
/// HyperlinkButton, DropDownButton, SplitButton): part names, density values, the
/// squircle chrome template, and the state styles that colour it.
/// </summary>
internal static class ButtonParts
{
    public const string ChromePart = "PART_Chrome";
    public const string ContentPart = "PART_ContentPresenter";

    // Density-dependent values, set by BlocksThemes.ApplyDensity under these non-public keys.
    internal static readonly object MinHeightKey = new();
    internal static readonly object PaddingKey = new();
    internal static readonly object GlyphMarginKey = new();

    // The chrome and glyph transitions, set by BlocksThemes.ApplyMotion under these non-public keys.
    internal static readonly object ChromeTransitionsKey = new();
    internal static readonly object GlyphTransitionsKey = new();

    /// <summary>Normal density: height 32, SpacingM at the sides and SpacingXS top and bottom (replaces Fluent's 8,5,8,6).</summary>
    public static readonly (double MinHeight, Thickness Padding) Normal = (32, new Thickness(Spacing.M, Spacing.XS));

    /// <summary>Compact density: height 24, SpacingS at the sides and 2 top and bottom.</summary>
    public static readonly (double MinHeight, Thickness Padding) Compact = (24, new Thickness(Spacing.S, 2));

    /// <summary>Fluent's chevron glyph (DropDownButton and SplitButton).</summary>
    public const string ChevronData = "M1939 486L2029 576L1024 1581L19 576L109 486L1024 1401L1939 486Z";

    /// <summary>The chevron's size, as in Fluent.</summary>
    public const double GlyphSize = 12;

    private static readonly ITransform Rest = TransformOperations.Identity;
    private static readonly ITransform Pressed = TransformOperations.Parse("scale(0.98)");

    /// <summary>Background, border brush and foreground of one state: a resource key or a brush.</summary>
    internal readonly record struct Look(object Background, object BorderBrush, object Foreground);

    /// <summary>The four Fluent state looks of a key family, e.g. "Button" gives ButtonBackground, ButtonBackgroundPointerOver, ….</summary>
    public static (Look Normal, Look Hover, Look Pressed, Look Disabled) Family(string prefix) =>
        (new($"{prefix}Background", $"{prefix}BorderBrush", $"{prefix}Foreground"),
         new($"{prefix}BackgroundPointerOver", $"{prefix}BorderBrushPointerOver", $"{prefix}ForegroundPointerOver"),
         new($"{prefix}BackgroundPressed", $"{prefix}BorderBrushPressed", $"{prefix}ForegroundPressed"),
         new($"{prefix}BackgroundDisabled", $"{prefix}BorderBrushDisabled", $"{prefix}ForegroundDisabled"));

    /// <summary>
    /// A theme for a button whose template is the chrome around a content presenter, with the
    /// given rest look as its defaults and Fluent's hover, pressed and disabled states and pressed scale.
    /// </summary>
    public static ControlTheme ContentButtonTheme(Type target, string borderThicknessKey,
        (Look Normal, Look Hover, Look Pressed, Look Disabled) looks)
    {
        var theme = new ControlTheme(target);
        foreach (var setter in Sizing())
            theme.Setters.Add(setter);
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Value(looks.Normal.Background)));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Value(looks.Normal.Foreground)));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Value(looks.Normal.BorderBrush)));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, Resource(borderThicknessKey)));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty,
            new FuncControlTemplate<ContentControl>((control, scope) => FocusRing.Wrap(BuildContentTemplate(control, scope), scope))));
        FocusRing.AddTo(theme);
        theme.Children.Add(ChromeMotion());
        theme.Children.Add(PressedScale());
        theme.Children.Add(State(":pointerover", looks.Hover));
        theme.Children.Add(State(":pressed", looks.Pressed));
        theme.Children.Add(State(":disabled", looks.Disabled));
        return theme;
    }

    /// <summary>Corner radius, density height and padding, and Fluent's alignment.</summary>
    public static IEnumerable<SetterBase> Sizing() =>
    [
        new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")),
        new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(PaddingKey)),
        new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(MinHeightKey)),
        new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Left),
        new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center),
    ];

    /// <summary>
    /// The chrome's transitions, from a theme resource, as Fluent declares transitions in its themes:
    /// fixed transition objects, so their duration and easing never depend on where the chrome is
    /// in the tree. The resource is replaced when motion changes (empty under reduce motion).
    /// </summary>
    public static IStyle ChromeMotion() => new Avalonia.Styling.Style(x => Chrome(x.Nesting()))
    {
        Setters = { new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(ChromeTransitionsKey)) },
    };

    /// <summary>
    /// The chrome transitions for the given motion: background and border fades and the pressed
    /// scale over <paramref name="duration"/>, or none when it is zero (reduce motion).
    /// </summary>
    public static Transitions ChromeTransitions(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? []
        :
        [
            new BrushTransition { Property = SquircleBorder.BackgroundProperty, Duration = duration, Easing = easing },
            new BrushTransition { Property = SquircleBorder.BorderBrushProperty, Duration = duration, Easing = easing },
            new TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = duration, Easing = easing },
        ];

    /// <summary>A glyph's opacity fade over <paramref name="duration"/>, or none when it is zero (reduce motion).</summary>
    public static Transitions GlyphTransitions(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? []
        : [new DoubleTransition { Property = Visual.OpacityProperty, Duration = duration, Easing = easing }];

    /// <summary>Fluent's 0.98 pressed scale, applied to the chrome.</summary>
    public static IStyle PressedScale() => new Avalonia.Styling.Style(x => x.Nesting())
    {
        Children =
        {
            new Avalonia.Styling.Style(x => Chrome(x.Nesting())) { Setters = { new Setter(Visual.RenderTransformProperty, Rest) } },
            new Avalonia.Styling.Style(x => Chrome(x.Nesting().Class(":pressed"))) { Setters = { new Setter(Visual.RenderTransformProperty, Pressed) } },
        },
    };

    /// <summary>The squircle chrome around Fluent's content presenter.</summary>
    public static Control BuildContentTemplate(ContentControl control, INameScope scope)
    {
        var presenter = CreatePresenter();
        var chrome = CreateChrome(presenter);
        chrome.RegisterInNameScope(scope);
        presenter.RegisterInNameScope(scope);
        return chrome;
    }

    /// <summary>Fluent's PART_ContentPresenter, bound to the templated control's content and padding.</summary>
    public static ContentPresenter CreatePresenter()
    {
        var presenter = new ContentPresenter { Name = ContentPart, RecognizesAccessKey = true };
        presenter.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        presenter.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        presenter.Bind(ContentPresenter.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        presenter.Bind(ContentPresenter.HorizontalContentAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        presenter.Bind(ContentPresenter.VerticalContentAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));
        return presenter;
    }

    /// <summary>The PART_Chrome squircle, bound to the templated control's brushes and shape. Its transitions come from <see cref="ChromeMotion"/>.</summary>
    public static SquircleBorder CreateChrome(Control child)
    {
        var chrome = new SquircleBorder { Name = ChromePart, Child = child };
        chrome.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        chrome.Bind(SquircleBorder.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        chrome.Bind(SquircleBorder.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        chrome.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        return chrome;
    }

    /// <summary>Fluent's chevron glyph.</summary>
    public static PathIcon CreateChevron() => new()
    {
        Data = StreamGeometry.Parse(ChevronData),
        Width = GlyphSize,
        Height = GlyphSize,
    };

    public static Selector Chrome(Selector state) => state.Template().OfType<SquircleBorder>().Name(ChromePart);

    public static Selector Content(Selector state) => state.Template().OfType<ContentPresenter>().Name(ContentPart);

    /// <summary>One state: background and border on the chrome, foreground on the content. A null pseudo-class is the group's rest state.</summary>
    public static Avalonia.Styling.Style State(string? pseudoClass, Look look) => State(x => Activate(x, pseudoClass), look);

    /// <summary>One state under any selector, e.g. a Tag value.</summary>
    public static Avalonia.Styling.Style State(Func<Selector?, Selector> activate, Look look) => new(x => activate(x))
    {
        Children =
        {
            new Avalonia.Styling.Style(x => Chrome(x.Nesting()))
            {
                Setters =
                {
                    new Setter(SquircleBorder.BackgroundProperty, Value(look.Background)),
                    new Setter(SquircleBorder.BorderBrushProperty, Value(look.BorderBrush)),
                },
            },
            new Avalonia.Styling.Style(x => Content(x.Nesting())) { Setters = { new Setter(ContentPresenter.ForegroundProperty, Value(look.Foreground)) } },
        },
    };

    /// <summary>A class or pseudo-class group (a variant, :checked, …) with its own rest, hover, pressed and disabled looks.</summary>
    public static Avalonia.Styling.Style Group(Func<Selector?, Selector> activate, Look rest, Look hover, Look pressed, Look disabled) =>
        new(x => activate(x))
        {
            Children =
            {
                State((string?)null, rest),
                State(":pointerover", hover),
                State(":pressed", pressed),
                State(":disabled", disabled),
            },
        };

    public static object Value(object value) => value is string key ? Resource(key) : value;

    public static DynamicResourceExtension Resource(string key) => new(key);

    private static Selector Activate(Selector? x, string? pseudoClass) => pseudoClass is null ? x.Nesting() : x.Nesting().Class(pseudoClass);
}
