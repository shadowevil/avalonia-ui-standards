using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace Catalog.Pages;

/// <summary>The type ramp: each style set in its own size and weight, with the values read from the theme.</summary>
internal sealed class TypePage : UserControl
{
    private static readonly (string Name, string Style)[] Ramp =
    [
        ("Caption", "Caption"), ("Body", "Body"), ("Body strong", "BodyStrong"), ("Subtitle", "Subtitle"),
        ("Title", "Title"), ("Large title", "LargeTitle"), ("Display", "Display"),
    ];

    public TypePage()
    {
        var list = new StackPanel { Spacing = 12 };
        foreach (var (name, style) in Ramp)
            list.Children.Add(Row(name, style));
        Content = Ui.Page("Type", Ui.Section("Ramp", list));
    }

    private static Control Row(string name, string style)
    {
        var sample = new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center };
        sample.Bind(TextBlock.FontSizeProperty, new DynamicResourceExtension("FontSize" + style));
        // Caption and Body have no weight key: they use the regular weight.
        if (style is not ("Caption" or "Body"))
            sample.Bind(TextBlock.FontWeightProperty, new DynamicResourceExtension("FontWeight" + style));

        var size = 0d;
        var weight = FontWeight.Normal;
        var detail = Ui.Caption("");
        void Update() => detail.Text = $"{size:0} px, weight {(int)weight}";
        sample.Watch("FontSize" + style, v => { if (v is double d) { size = d; Update(); } });
        if (style is not ("Caption" or "Body"))
            sample.Watch("FontWeight" + style, v => { if (v is FontWeight w) { weight = w; Update(); } });

        return new StackPanel { Children = { detail, sample } };
    }
}
