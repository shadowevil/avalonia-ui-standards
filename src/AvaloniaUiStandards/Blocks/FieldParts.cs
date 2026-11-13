using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The parts shared by the field Blocks (TextInput, ComboBox, NumericUpDown, ButtonSpinner,
/// AutoCompleteBox): the squircle field chrome, the field state colours, and the small subtle
/// icon buttons that sit inside a field (clear, reveal password, spin, drop-down chevron zone).
/// Field state styles set the field control's own Background, BorderBrush and Foreground, and the
/// chrome binds to them, so a template that sets local values on an inner field (a borderless
/// TextBox inside another control) overrides every state.
/// </summary>
internal static class FieldParts
{
    public const string ChromePart = "PART_BorderElement";

    /// <summary>The icon size inside field buttons, at both densities.</summary>
    public const double IconSize = 16;

    /// <summary>The gap between the text and the first button inside a field.</summary>
    public const double ButtonGap = Spacing.XS;

    // Density-dependent values, set by ApplyDensity under these non-public keys.
    internal static readonly object ButtonSizeKey = new();
    internal static readonly object ButtonCornerRadiusKey = new();
    internal static readonly object TrailingButtonsMarginKey = new();
    internal static readonly object LeadingButtonsMarginKey = new();

    /// <summary>
    /// The inner button size per density: centred in the field height (32 or 24) with an equal
    /// inset on every side (4 or 2), its corner radius the field's less that inset so the curves
    /// stay concentric.
    /// </summary>
    public static double ButtonSize(DensityStyle density) => density == DensityStyle.Compact ? 20 : 24;

    public static void ApplyDensity(IResourceDictionary resources, DensityStyle density)
    {
        var sizes = density == DensityStyle.Compact ? ButtonParts.Compact : ButtonParts.Normal;
        var size = ButtonSize(density);
        var inset = (sizes.MinHeight - size) / 2;
        const double border = 1;
        resources[ButtonSizeKey] = size;
        resources[ButtonCornerRadiusKey] = new CornerRadius(8 - inset);
        // The buttons reach into the text's side padding, leaving ButtonGap after the text, and
        // sit the inset from the field's outer edge.
        resources[TrailingButtonsMarginKey] = new Thickness(ButtonGap - sizes.Padding.Right, 0, inset - border, 0);
        resources[LeadingButtonsMarginKey] = new Thickness(inset - border, 0, ButtonGap - sizes.Padding.Left, 0);
    }

    /// <summary>The four colour families of a field's states.</summary>
    internal readonly record struct Colours(Look Rest, Look Hover, Look Focused, Look Disabled);

    /// <summary>Fluent's TextControl keys: TextControlBackground, TextControlBackgroundPointerOver, ….</summary>
    public static readonly Colours TextControl = new(
        new("TextControlBackground", "TextControlBorderBrush", "TextControlForeground"),
        new("TextControlBackgroundPointerOver", "TextControlBorderBrushPointerOver", "TextControlForegroundPointerOver"),
        new("TextControlBackgroundFocused", "TextControlBorderBrushFocused", "TextControlForegroundFocused"),
        new("TextControlBackgroundDisabled", "TextControlBorderBrushDisabled", "TextControlForegroundDisabled"));

    /// <summary>The danger colour, the border of a field in error.</summary>
    public const string ErrorBorderKey = "DangerColor";

    /// <summary>
    /// The field's rest colours and sizes as theme setters: <c>CornerRadiusControl</c>, the density
    /// height and padding, a 1 px border, Fluent's minimum width.
    /// </summary>
    public static void AddSizing(ControlTheme theme, object paddingKey)
    {
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(paddingKey)));
        theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(MinHeightKey)));
        theme.Setters.Add(new Setter(Layoutable.MinWidthProperty, Resource("TextControlThemeMinWidth")));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, Resource("TextControlBorderThemeThickness")));
    }

    /// <summary>
    /// The field state colours on the control's own properties, in rising precedence: rest,
    /// hover, pressed (when given), focused (under <paramref name="focus"/>, null for none),
    /// error (the border only), disabled.
    /// </summary>
    public static void AddColours(ControlTheme theme, Colours colours, Func<Selector?, Selector>? focus, Look? pressed = null)
    {
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Value(colours.Rest.Background)));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Value(colours.Rest.BorderBrush)));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Value(colours.Rest.Foreground)));
        theme.Children.Add(Colour(x => x.Nesting().Class(":pointerover"), colours.Hover));
        if (pressed is { } look)
            theme.Children.Add(Colour(x => x.Nesting().Class(":pressed"), look));
        if (focus is not null)
            theme.Children.Add(Colour(focus, colours.Focused));
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":error"))
        {
            Setters = { new Setter(TemplatedControl.BorderBrushProperty, Resource(ErrorBorderKey)) },
        });
        theme.Children.Add(Colour(x => x.Nesting().Class(":disabled"), colours.Disabled));
    }

    private static Avalonia.Styling.Style Colour(Func<Selector?, Selector> state, Look look) => new(x => state(x))
    {
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Value(look.Background)),
            new Setter(TemplatedControl.BorderBrushProperty, Value(look.BorderBrush)),
            new Setter(TemplatedControl.ForegroundProperty, Value(look.Foreground)),
        },
    };

    /// <summary>The field chrome: a squircle bound to the templated control's brushes and shape. Its fades come from <see cref="ChromeMotion"/>.</summary>
    public static SquircleBorder CreateChrome()
    {
        var chrome = new SquircleBorder { Name = ChromePart };
        chrome.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        chrome.Bind(SquircleBorder.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        chrome.Bind(SquircleBorder.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        chrome.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        return chrome;
    }

    /// <summary>The chrome's background and border fades (MotionFast; none under reduce motion), as fixed transitions.</summary>
    public static IStyle ChromeMotion() => new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<SquircleBorder>().Name(ChromePart))
    {
        Setters = { new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(ChromeTransitionsKey)) },
    };

    /// <summary>
    /// The theme of the buttons inside a field: a squircle icon button with no fill until hover,
    /// using the Subtle button colours (Standard's hover and pressed fills), sized by density,
    /// with no pressed scale and no focus ring (they never take keyboard focus).
    /// </summary>
    public static ControlTheme ButtonTheme()
    {
        var looks = Family("Button");
        var theme = new ControlTheme(typeof(Button));
        theme.Setters.Add(new Setter(Layoutable.WidthProperty, new DynamicResourceExtension(ButtonSizeKey)));
        theme.Setters.Add(new Setter(Layoutable.HeightProperty, new DynamicResourceExtension(ButtonSizeKey)));
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, new DynamicResourceExtension(ButtonCornerRadiusKey)));
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new Thickness(0)));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Resource("TextControlButtonForeground")));
        theme.Setters.Add(new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty,
            new FuncControlTemplate<ContentControl>((control, scope) => BuildContentTemplate(control, scope))));
        theme.Children.Add(ButtonParts.ChromeMotion());
        theme.Children.Add(State((string?)null, new Look(Brushes.Transparent, Brushes.Transparent, "TextControlButtonForeground")));
        theme.Children.Add(State(":pointerover", looks.Hover with { BorderBrush = Brushes.Transparent }));
        theme.Children.Add(State(":pressed", looks.Pressed with { BorderBrush = Brushes.Transparent }));
        theme.Children.Add(State(":disabled", new Look(Brushes.Transparent, Brushes.Transparent, "ButtonForegroundDisabled")));
        return theme;
    }

    /// <summary>An icon sized for a field button.</summary>
    public static Icon ButtonIcon(Geometry? data = null)
    {
        var icon = new Icon { Width = IconSize, Height = IconSize };
        if (data is not null)
            icon.Data = data;
        return icon;
    }

    private static Cursor? _iBeam;

    /// <summary>The text cursor, shared, created on first use (when the platform is up).</summary>
    public static Cursor IBeam => _iBeam ??= new Cursor(StandardCursorType.Ibeam);
}
