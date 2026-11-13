using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using AvaloniaUiStandards.Style;

namespace Catalog.Pages;

/// <summary>The corner radii drawn as squircles, at the theme's corner smoothing.</summary>
internal sealed class ShapePage : UserControl
{
    public ShapePage()
    {
        var shapes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 32 };
        shapes.Children.Add(Sample("CornerRadiusControl", 160, 96));
        shapes.Children.Add(Sample("CornerRadiusOverlay", 160, 96));

        var smoothing = Ui.CaptionOf("CornerSmoothing", v => v is double d ? $"CornerSmoothing {d:0.##}" : "");
        Content = Ui.Page("Shape", Ui.Section("Corner radii", new StackPanel { Spacing = 12, Children = { shapes, smoothing } }));
    }

    private static Control Sample(string key, double width, double height)
    {
        var shape = new SquircleBorder { Width = width, Height = height, BorderThickness = new Thickness(1) };
        shape.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension(key));
        shape.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("SystemControlBackgroundChromeMediumLowBrush"));
        shape.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("SystemControlHighlightAccentBrush"));
        var value = Ui.CaptionOf(key, v => v is CornerRadius r ? $"{key} {r.TopLeft:0} px" : "");
        return new StackPanel { Spacing = 8, Children = { shape, value } };
    }
}
