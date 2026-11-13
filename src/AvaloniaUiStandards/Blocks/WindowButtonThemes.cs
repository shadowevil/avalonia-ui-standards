using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The themes and values of the window buttons. Traffic lights are 12 px dots in the macOS
/// colours, 8 px apart, with a dark glyph; caption buttons are 46 × 32 (Compact 40 × 28), at the top of
/// the title band, with a 10 px glyph, and close turns to the danger fill on hover.
/// </summary>
internal static class WindowButtonThemes
{
    public const string GlyphPart = "PART_Glyph";

    public const double TrafficLightSize = 12;
    public const double TrafficLightGap = 8;
    public const double TrafficLightGlyphSize = 6;
    public const double CaptionButtonWidth = 46;
    public const double CaptionButtonHeight = 32;
    public const double CaptionButtonWidthCompact = 40;
    public const double CaptionButtonHeightCompact = 28;
    public const double CaptionGlyphSize = 10;

    // The macOS traffic light colours (the same in light and dark) and the glyph drawn on them.
    internal static readonly Color CloseColor = Color.Parse("#FF5F57");
    internal static readonly Color MinimizeColor = Color.Parse("#FEBC2E");
    internal static readonly Color ZoomColor = Color.Parse("#28C840");
    internal static readonly Color GlyphColor = Color.Parse("#99000000");

    /// <summary>The HSL lightness drop of a pressed traffic light (the AA fill rule's pressed step).</summary>
    private const double PressedStep = 2 * AccessibleColors.StateStep;

    private static readonly Dictionary<(WindowButtonKind, bool Pressed), object> FillKeys = new()
    {
        [(WindowButtonKind.Close, false)] = new object(),
        [(WindowButtonKind.Close, true)] = new object(),
        [(WindowButtonKind.Minimize, false)] = new object(),
        [(WindowButtonKind.Minimize, true)] = new object(),
        [(WindowButtonKind.Maximize, false)] = new object(),
        [(WindowButtonKind.Maximize, true)] = new object(),
    };

    internal static readonly object GlyphKey = new();

    // The caption button size, set by ApplyDensity under these non-public keys.
    internal static readonly object CaptionWidthKey = new();
    internal static readonly object CaptionHeightKey = new();

    /// <summary>Sets the density-dependent caption button size.</summary>
    public static void ApplyDensity(IResourceDictionary resources, bool compact)
    {
        resources[CaptionWidthKey] = compact ? CaptionButtonWidthCompact : CaptionButtonWidth;
        resources[CaptionHeightKey] = compact ? CaptionButtonHeightCompact : CaptionButtonHeight;
    }

    private static readonly IValueConverter TrafficLightGlyph =
        new FuncValueConverter<WindowButtonKind, Geometry>(kind => WindowButtonGlyphs.TrafficLight(kind));

    /// <summary>Adds the traffic light colours under non-public keys.</summary>
    public static void AddResources(IResourceDictionary resources)
    {
        foreach (var (kind, color) in new[]
                 {
                     (WindowButtonKind.Close, CloseColor), (WindowButtonKind.Minimize, MinimizeColor), (WindowButtonKind.Maximize, ZoomColor),
                 })
        {
            resources[FillKeys[(kind, false)]] = color;
            resources[FillKeys[(kind, true)]] = Darken(color, PressedStep);
        }
        resources[GlyphKey] = GlyphColor;
    }

    public static ControlTheme TrafficLight()
    {
        var theme = new ControlTheme(typeof(TrafficLightButton))
        {
            Setters =
            {
                new Setter(Layoutable.WidthProperty, TrafficLightSize),
                new Setter(Layoutable.HeightProperty, TrafficLightSize),
                new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(TrafficLightSize / 2)),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(0)),
                new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center),
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TrafficLightButton>(BuildTrafficLight)),
            },
            Children =
            {
                ChromeMotion(),
                new Avalonia.Styling.Style(x => Glyph(x.Nesting()))
                {
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 0d),
                        new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(FadeTransitionsKey)),
                    },
                },
                new Avalonia.Styling.Style(x => Glyph(x.Nesting().Class(":glyph"))) { Setters = { new Setter(Visual.OpacityProperty, 1d) } },
            },
        };
        foreach (var ((kind, pressed), key) in FillKeys)
        {
            theme.Children.Add(new Avalonia.Styling.Style(x =>
                pressed
                    ? x.Nesting().PropertyEquals(TrafficLightButton.KindProperty, kind).Class(":pressed")
                    : x.Nesting().PropertyEquals(TrafficLightButton.KindProperty, kind))
            {
                Setters = { new Setter(TemplatedControl.BackgroundProperty, new DynamicResourceExtension(key)) },
            });
        }

        // Inactive window: all three lights are the neutral grey with no glyph, until the group is hovered (as macOS).
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":inactive").Not(y => y.Class(":glyph")))
        {
            Setters = { new Setter(TemplatedControl.BackgroundProperty, Resource("SystemBaseLowColor")) },
        });
        return theme;
    }

    public static ControlTheme Caption() => new(typeof(CaptionButton))
    {
        Setters =
        {
            new Setter(Layoutable.WidthProperty, new DynamicResourceExtension(CaptionWidthKey)),
            new Setter(Layoutable.HeightProperty, new DynamicResourceExtension(CaptionHeightKey)),
            new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Top),
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            new Setter(TemplatedControl.ForegroundProperty, Resource("ButtonForeground")),
            new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
            new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(0)),
            new Setter(TemplatedControl.PaddingProperty, new Thickness(0)),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<CaptionButton>(BuildCaption)),
        },
        Children =
        {
            ChromeMotion(),
            CaptionState(x => x.Nesting().Class(":pointerover"), "ButtonBackgroundPointerOver", null),
            CaptionState(x => x.Nesting().Class(":pressed"), "ButtonBackgroundPressed", null),
            CaptionState(x => x.Nesting().Class(":disabled"), null, "ButtonForegroundDisabled"),
            CaptionState(x => x.Nesting().Class(":close").Class(":pointerover"), "DangerFillColor", "DangerForeground"),
            CaptionState(x => x.Nesting().Class(":close").Class(":pressed"), "DangerFillColorPressed", "DangerForeground"),

            // Inactive window: the glyph dims (unless hovered), as Windows does.
            CaptionState(x => x.Nesting().Class(":inactive").Not(y => y.Class(":pointerover")), null, "ButtonForegroundDisabled"),
        },
    };

    private static Control BuildTrafficLight(TrafficLightButton button, INameScope scope)
    {
        var glyph = new Path
        {
            Name = GlyphPart,
            Width = TrafficLightGlyphSize,
            Height = TrafficLightGlyphSize,
            StrokeThickness = 1,
            StrokeLineCap = PenLineCap.Round,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        glyph.Bind(Path.DataProperty, new TemplateBinding(TrafficLightButton.KindProperty) { Converter = TrafficLightGlyph });
        glyph.Bind(Shape.StrokeProperty, new DynamicResourceExtension(GlyphKey));
        return Chrome(glyph, scope);
    }

    private static Control BuildCaption(CaptionButton button, INameScope scope)
    {
        var glyph = new Path
        {
            Name = GlyphPart,
            Width = CaptionGlyphSize,
            Height = CaptionGlyphSize,
            StrokeThickness = 1,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        glyph.Bind(Path.DataProperty, new TemplateBinding(CaptionButton.GlyphProperty));
        glyph.Bind(Shape.StrokeProperty, new TemplateBinding(TemplatedControl.ForegroundProperty));
        return Chrome(glyph, scope);
    }

    private static Control Chrome(Path glyph, INameScope scope)
    {
        var chrome = CreateChrome(glyph);
        chrome.RegisterInNameScope(scope);
        glyph.RegisterInNameScope(scope);
        return chrome;
    }

    private static Selector Glyph(Selector state) => state.Template().OfType<Path>().Name(GlyphPart);

    private static Avalonia.Styling.Style CaptionState(Func<Selector?, Selector> activate, string? backgroundKey, string? foregroundKey)
    {
        var style = new Avalonia.Styling.Style(x => activate(x));
        if (foregroundKey is not null)
            style.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Resource(foregroundKey)));
        if (backgroundKey is not null)
            style.Children.Add(new Avalonia.Styling.Style(x => ButtonParts.Chrome(x.Nesting()))
            {
                Setters = { new Setter(SquircleBorder.BackgroundProperty, Resource(backgroundKey)) },
            });
        return style;
    }

    private static Color Darken(Color color, double step)
    {
        var hsl = color.ToHsl();
        return HslColor.ToRgb(hsl.H, hsl.S, Math.Max(0, hsl.L - step), hsl.A);
    }
}
