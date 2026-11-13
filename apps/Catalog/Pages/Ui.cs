using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Reactive;

namespace Catalog.Pages;

/// <summary>Small builders shared by the pages. Values are bound to theme resources, never copied.</summary>
internal static class Ui
{
    /// <summary>Calls <paramref name="apply"/> with the resource's value now and whenever it or the theme changes.</summary>
    public static void Watch(this Control control, string key, Action<object?> apply) =>
        control.GetResourceObservable(key).Subscribe(new AnonymousObserver<object?>(apply));

    public static string Hex(Color c) => c.A == 0xFF ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    /// <summary>Body text, sized by <c>FontSizeBody</c>.</summary>
    public static TextBlock Body(string text) => Sized(text, "FontSizeBody");

    /// <summary>Secondary text, sized by <c>FontSizeCaption</c>.</summary>
    public static TextBlock Caption(string text)
    {
        var block = Sized(text, "FontSizeCaption");
        block.TextWrapping = TextWrapping.Wrap;
        block.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseMediumBrush"));
        return block;
    }

    /// <summary>Caption text whose content is set from a resource value.</summary>
    public static TextBlock CaptionOf(string key, Func<object?, string> format)
    {
        var block = Caption("");
        block.Watch(key, v => block.Text = format(v));
        return block;
    }

    private static TextBlock Sized(string text, string sizeKey)
    {
        var block = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
        block.Bind(TextBlock.FontSizeProperty, new DynamicResourceExtension(sizeKey));
        return block;
    }

    /// <summary>A page: a title and its sections, stacked.</summary>
    public static Control Page(string title, params Control[] sections)
    {
        // No margin: the window's content panel already pads its content.
        var stack = new StackPanel { Spacing = 24 };
        var heading = Sized(title, "FontSizeTitle");
        heading.TextWrapping = TextWrapping.Wrap;
        heading.Bind(TextBlock.FontWeightProperty, new DynamicResourceExtension("FontWeightTitle"));
        stack.Children.Add(heading);
        foreach (var section in sections)
            stack.Children.Add(section);
        return stack;
    }

    /// <summary>A heading above its content.</summary>
    public static Control Section(string heading, Control content)
    {
        var title = Sized(heading, "FontSizeSubtitle");
        title.Bind(TextBlock.FontWeightProperty, new DynamicResourceExtension("FontWeightSubtitle"));
        return new StackPanel { Spacing = 12, Children = { title, content } };
    }
}
