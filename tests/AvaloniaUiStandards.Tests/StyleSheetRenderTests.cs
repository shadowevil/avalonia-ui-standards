using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the Style tier sheets to PNG under the test output (screenshots/).</summary>
public class StyleSheetRenderTests
{
    private static readonly ThemeVariant[] Variants = [ThemeVariant.Light, ThemeVariant.Dark];
    private static readonly StandardsTheme Theme = new();

    [AvaloniaFact]
    public void Colour_RendersSheet()
    {
        string[] keys =
        [
            "RegionColor", "AltHigh", "BaseHigh", "BaseMediumHigh", "BaseMedium", "BaseMediumLow", "BaseLow",
            "ChromeLow", "ChromeMediumLow", "ChromeMedium", "ChromeHigh", "ListLow", "ListMedium", "Accent",
        ];

        RenderSheet("m2-style-colour.png", variant =>
        {
            var list = new StackPanel { Spacing = 6 };
            foreach (var key in keys)
            {
                var color = (Color)Resource(key == "RegionColor" ? "SystemRegionColor" : $"System{key}Color", variant);
                list.Children.Add(new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    Children =
                    {
                        new Rectangle
                        {
                            Width = 72, Height = 28, RadiusX = 4, RadiusY = 4,
                            Fill = new SolidColorBrush(color),
                            Stroke = Brush("SystemBaseLowColor", variant), StrokeThickness = 1,
                        },
                        Text($"{key}", "FontSizeBody", width: 140),
                        Text($"#{color.R:X2}{color.G:X2}{color.B:X2}", "FontSizeBody"),
                    },
                });
            }
            return list;
        }, horizontal: true);
    }

    [AvaloniaFact]
    public void Type_RendersSheet()
    {
        (string Name, string Key, string? WeightKey)[] ramp =
        [
            ("Caption 12", "FontSizeCaption", null),
            ("Body 14", "FontSizeBody", null),
            ("Body strong 14", "FontSizeBodyStrong", "FontWeightBodyStrong"),
            ("Subtitle 20", "FontSizeSubtitle", "FontWeightSubtitle"),
            ("Title 28", "FontSizeTitle", "FontWeightTitle"),
            ("Large title 40", "FontSizeLargeTitle", "FontWeightLargeTitle"),
            ("Display 68", "FontSizeDisplay", "FontWeightDisplay"),
        ];

        RenderSheet("m2-style-type.png", _ =>
        {
            var list = new StackPanel { Spacing = 8, Width = 560 };
            foreach (var (name, key, weightKey) in ramp)
            {
                var text = Text(name, key);
                list.Children.Add(weightKey is null ? text : Weighted(text, weightKey));
            }
            list.Children.Add(Text("The quick brown fox jumps over the lazy dog. 0123456789", "FontSizeBody"));
            return list;
        });
    }

    [AvaloniaFact]
    public void Spacing_RendersSheet()
    {
        string[] keys = ["SpacingXS", "SpacingS", "SpacingM", "SpacingL", "SpacingXL", "SpacingXXL", "SpacingXXXL"];

        RenderSheet("m2-style-spacing.png", variant =>
        {
            var list = new StackPanel { Spacing = 8 };
            foreach (var key in keys)
            {
                var bar = new Rectangle { Height = 24, Fill = Brush("SystemAccentColor", variant), HorizontalAlignment = HorizontalAlignment.Left };
                bar.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(key));
                list.Children.Add(new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    Children = { Text($"{key} {Resource(key, variant)}", "FontSizeBody", width: 130), bar },
                });
            }
            return list;
        });
    }

    [AvaloniaFact]
    public void ShapeAndGlass_RendersSheet()
    {
        RenderSheet("m2-style-shape-glass.png", variant =>
        {
            var labels = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24 };
            var samples = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24, Margin = new Thickness(24) };

            void Add(string label, Control sample, double width)
            {
                labels.Children.Add(Text(label, "FontSizeCaption", width));
                samples.Children.Add(new Panel { Width = width, Children = { Centered(sample) } });
            }

            var control = new SquircleBorder
            {
                Width = 120, Height = 40,
                Background = Brush("SystemChromeMediumLowColor", variant),
                BorderBrush = Brush("SystemBaseLowColor", variant),
                BorderThickness = new Thickness(1),
                Child = Centered(Text("Button", "FontSizeBody")),
            };
            control.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusControl"));
            Add("SquircleBorder, radius 8", control, 200);

            var card = new SquircleBorder
            {
                Width = 200, Height = 140,
                Background = Brush("SystemChromeMediumLowColor", variant),
                BorderBrush = Brush("SystemBaseLowColor", variant),
                BorderThickness = new Thickness(1),
                Child = Centered(Text("Card", "FontSizeSubtitle")),
            };
            card.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusOverlay"));
            Add("SquircleBorder, radius 16", card, 200);

            Add("GlassBorder", new GlassBorder
            {
                Width = 240, Height = 140,
                Child = Centered(Text("Glass", "FontSizeSubtitle")),
            }, 240);

            var reduced = new Border
            {
                Child = new GlassBorder
                {
                    Width = 240, Height = 140,
                    Child = Centered(Text("Solid fallback", "FontSizeSubtitle")),
                },
            };
            reduced.Styles.Add(new StandardsTheme { ReduceTransparency = true });
            Add("GlassBorder, ReduceTransparency", reduced, 240);

            labels.Margin = new Thickness(24, 0);
            return new StackPanel
            {
                Spacing = 6,
                Children = { labels, new Panel { Children = { Backdrop.Colourful(), samples } } },
            };
        }, padding: 0);
    }

    [AvaloniaFact]
    public void Glass_RendersFinalSheet()
    {
        RenderSheet("m2-glass-final.png", _ => new Panel
        {
            Width = 560,
            Height = 280,
            Children =
            {
                Backdrop.Colourful(),
                new GlassBorder
                {
                    Margin = new Thickness(48, 40),
                    Padding = new Thickness(24),
                    Child = new StackPanel
                    {
                        Spacing = 8,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            Weighted(Text("Glass", "FontSizeTitle"), "FontWeightTitle"),
                            Wrapped(Text("Light frost, strong bend: sigma 6, refraction 28 px over a 28 px rim, dispersion 0.35.", "FontSizeBody")),
                        },
                    },
                },
            },
        });
    }

    // Renders one block per theme variant, stacked or side by side, each on the variant's region colour.
    private static void RenderSheet(string name, Func<ThemeVariant, Control> content, double padding = 16, bool horizontal = false)
    {
        var sheet = new StackPanel { Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical };
        foreach (var variant in Variants)
        {
            var block = new Border
            {
                Background = Brush("SystemRegionColor", variant),
                Padding = new Thickness(16),
                Child = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        Text(variant == ThemeVariant.Light ? "Light" : "Dark", "FontSizeBodyStrong"),
                        new Border { Padding = new Thickness(padding), Child = content(variant) },
                    },
                },
            };
            block.Bind(TextElement.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseHighBrush"));
            sheet.Children.Add(new ThemeVariantScope { RequestedThemeVariant = variant, Child = block });
        }

        var window = new Window { SizeToContent = SizeToContent.WidthAndHeight, Content = sheet };
        using var frame = Frame.Capture(window);
        frame.Save(name);
    }

    private static Control Centered(Control c)
    {
        c.HorizontalAlignment = HorizontalAlignment.Center;
        c.VerticalAlignment = VerticalAlignment.Center;
        return c;
    }

    private static TextBlock Text(string text, string sizeKey, double width = double.NaN)
    {
        var block = new TextBlock { Text = text, Width = width, VerticalAlignment = VerticalAlignment.Center };
        block.Bind(TextBlock.FontSizeProperty, new DynamicResourceExtension(sizeKey));
        return block;
    }

    private static TextBlock Weighted(TextBlock block, string weightKey)
    {
        block.Bind(TextBlock.FontWeightProperty, new DynamicResourceExtension(weightKey));
        return block;
    }

    private static TextBlock Wrapped(TextBlock block)
    {
        block.TextWrapping = TextWrapping.Wrap;
        return block;
    }

    private static object Resource(string key, ThemeVariant variant)
    {
        Assert.True(Theme.TryGetResource(key, variant, out var value), key);
        return value!;
    }

    private static IBrush Brush(string colorKey, ThemeVariant variant) => new SolidColorBrush((Color)Resource(colorKey, variant));
}
