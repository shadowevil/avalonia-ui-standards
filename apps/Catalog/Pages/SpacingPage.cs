using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace Catalog.Pages;

/// <summary>The spacing scale as bars, each as wide as its resource value.</summary>
internal sealed class SpacingPage : UserControl
{
    private static readonly string[] Steps = ["XS", "S", "M", "L", "XL", "XXL", "XXXL"];

    public SpacingPage()
    {
        var list = new StackPanel { Spacing = 12 };
        foreach (var step in Steps)
            list.Children.Add(Row("Spacing" + step));
        Content = Ui.Page("Spacing", Ui.Section("Scale", list));
    }

    private static Control Row(string key)
    {
        var bar = new Border { Height = 20, HorizontalAlignment = HorizontalAlignment.Left };
        bar.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(key));
        bar.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemControlHighlightAccentBrush"));

        var label = Ui.Body(key);
        label.Width = 110;
        var value = Ui.CaptionOf(key, v => v is double d ? $"{d:0} px" : "");
        value.Width = 56;
        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        stack.Children.Add(label);
        stack.Children.Add(value);
        stack.Children.Add(bar);
        return stack;
    }
}
