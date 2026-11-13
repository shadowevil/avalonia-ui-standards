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
using Avalonia.Media.Immutable;
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

    /// <summary>A corner larger than any side: the shapes clamp it to half the shorter side, a pill (or circle).</summary>
    public static readonly CornerRadius Pill = new(1000);
    public const string ContentPart = "PART_ContentPresenter";

    // Density-dependent values, set by BlocksThemes.ApplyDensity under these non-public keys.
    internal static readonly object MinHeightKey = new();
    internal static readonly object PaddingKey = new();
    internal static readonly object GlyphMarginKey = new();

    // The chrome and fade transitions, set by BlocksThemes.ApplyMotion under these non-public keys.
    internal static readonly object ChromeTransitionsKey = new();
    internal static readonly object FadeTransitionsKey = new();

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
    /// <paramref name="part"/> names the chrome squircle (<c>PART_Chrome</c> for buttons,
    /// <c>PART_BorderElement</c> for fields).
    /// </summary>
    public static IStyle ChromeMotion(string part = ChromePart) =>
        new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<SquircleBorder>().Name(part))
    {
        Setters = { new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(ChromeTransitionsKey)) },
    };

    /// <summary>
    /// The chrome transitions for the given motion: background and border fades (as
    /// <see cref="CrossFadeBrushTransition"/>) and the pressed scale over <paramref name="duration"/>,
    /// or none when it is zero (reduce motion).
    /// </summary>
    public static Transitions ChromeTransitions(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? []
        :
        [
            new CrossFadeBrushTransition { Property = SquircleBorder.BackgroundProperty, Duration = duration, Easing = easing },
            new CrossFadeBrushTransition { Property = SquircleBorder.BorderBrushProperty, Duration = duration, Easing = easing },
            new TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = duration, Easing = easing },
        ];

    /// <summary>An opacity fade (glyphs, scroll shadows) over <paramref name="duration"/>, or none when it is zero (reduce motion).</summary>
    public static Transitions FadeTransitions(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
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

/// <summary>
/// A brush transition whose every frame, drawn over any backdrop, is the cross-fade of the two
/// brushes drawn over it, so no frame is lighter or darker than both ends. Solid brushes are
/// interpolated as premultiplied colours with the brush opacity folded into alpha. Avalonia's
/// <see cref="BrushTransition"/> interpolates straight colours and the brush opacity separately: a
/// fade from <c>Transparent</c> (#00FFFFFF) passes through translucent white, and a fade to a
/// translucent brush (Fluent's 10 % hover fills) passes through a near-opaque mix, both of which
/// flash bright on a dark backdrop. A null brush is transparent; any other pair of brushes switches
/// at the midpoint.
/// </summary>
internal sealed class CrossFadeBrushTransition : InterpolatingTransitionBase<IBrush?>
{
    protected override IBrush? Interpolate(double progress, IBrush? from, IBrush? to) => At(progress, from, to);

    /// <summary>The brush at <paramref name="progress"/> (eased, 0..1) from <paramref name="from"/> to <paramref name="to"/>.</summary>
    internal static IBrush? At(double progress, IBrush? from, IBrush? to)
    {
        if (progress <= 0)
            return from;
        if (progress >= 1)
            return to;
        if (!TryPremultiplied(from, out var a) || !TryPremultiplied(to, out var b))
            return progress < 0.5 ? from : to;
        return new ImmutableSolidColorBrush(Mix(progress, a, b));
    }

    /// <summary>The colour at <paramref name="progress"/> between two premultiplied colours (alpha, red, green, blue in 0..1).</summary>
    private static Color Mix(double progress, (double A, double R, double G, double B) from, (double A, double R, double G, double B) to)
    {
        double Lerp(double x, double y) => x + (y - x) * progress;
        var alpha = Math.Clamp(Lerp(from.A, to.A), 0, 1);
        if (alpha <= 0)
            return Colors.Transparent;
        byte Channel(double premultiplied) => (byte)Math.Round(Math.Clamp(premultiplied / alpha, 0, 1) * 255);
        return Color.FromArgb((byte)Math.Round(alpha * 255),
            Channel(Lerp(from.R, to.R)), Channel(Lerp(from.G, to.G)), Channel(Lerp(from.B, to.B)));
    }

    /// <summary>A null or solid brush as a premultiplied colour, its opacity folded into alpha.</summary>
    private static bool TryPremultiplied(IBrush? brush, out (double A, double R, double G, double B) colour)
    {
        switch (brush)
        {
            case null:
                colour = default;
                return true;
            case ISolidColorBrush solid:
                var c = solid.Color;
                var alpha = c.A / 255.0 * Math.Clamp(solid.Opacity, 0, 1);
                colour = (alpha, c.R / 255.0 * alpha, c.G / 255.0 * alpha, c.B / 255.0 * alpha);
                return true;
            default:
                colour = default;
                return false;
        }
    }
}
