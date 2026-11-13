using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Catalog.Pages;

/// <summary>The neutral ramp, the accent and the danger colours, with their current hex values.</summary>
internal sealed class ColoursPage : UserControl
{
    private static readonly string[] Neutral =
    [
        "SystemRegionColor", "SystemAltHighColor", "SystemBaseHighColor", "SystemBaseMediumHighColor",
        "SystemBaseMediumColor", "SystemBaseMediumLowColor", "SystemBaseLowColor", "SystemChromeLowColor",
        "SystemChromeMediumLowColor", "SystemChromeMediumColor", "SystemChromeHighColor", "SystemListLowColor",
        "SystemListMediumColor",
    ];

    private static readonly string[] Accent =
    [
        "SystemAccentColor", "AccentFillColor", "AccentFillColorPointerOver", "AccentFillColorPressed",
        "AccentTextColor", "AccentTextColorPressed",
    ];

    private static readonly string[] Danger =
    [
        "DangerColor", "DangerFillColor", "DangerFillColorPointerOver", "DangerFillColorPressed", "DangerForeground",
    ];

    public ColoursPage() => Content = Ui.Page("Colours",
        Ui.Section("Neutral", Swatches(Neutral)),
        Ui.Section("Accent", Swatches(Accent)),
        Ui.Section("Danger", Swatches(Danger)));

    private static WrapPanel Swatches(string[] keys)
    {
        var panel = new WrapPanel();
        foreach (var key in keys)
            panel.Children.Add(Swatch(key));
        return panel;
    }

    private static Control Swatch(string key)
    {
        var chip = new Border
        {
            Width = 72, Height = 32, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
        };
        chip.Bind(Border.BorderBrushProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("SystemControlForegroundBaseLowBrush"));
        var hex = Ui.Caption("");
        chip.Watch(key, v =>
        {
            if (v is Color color)
            {
                chip.Background = new SolidColorBrush(color);
                hex.Text = Ui.Hex(color);
            }
        });
        var name = Ui.Body(key);
        name.TextWrapping = TextWrapping.Wrap;
        var text = new StackPanel { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Children = { name, hex } };
        Grid.SetColumn(text, 1);
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 12,
            Width = 340,
            Margin = new Thickness(0, 0, 24, 8),
            Children = { chip, text },
        };
    }
}
