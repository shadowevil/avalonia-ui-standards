using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUiStandards.Blocks;

namespace Catalog.Pages;

/// <summary>Icon: the whole set with names, the sizes, and an icon in a button.</summary>
internal sealed class IconPage : UserControl
{
    public IconPage() => Content = Ui.Page("Icon",
        Ui.Section("Set (Phosphor Regular)", Set()),
        Ui.Section("Sizes", new StackPanel
        {
            Spacing = 12,
            Children = { Sizes(), Ui.Caption("20 px at Normal density and 16 px at Compact, as in the set above; 24 px for larger use.") },
        }),
        Ui.Section("In a button", StateGrid.Build([("Accent, with icon", AccentButton)])));

    /// <summary>Every icon in <see cref="Icons"/>, by property name.</summary>
    private static IEnumerable<(string Name, Geometry Data)> All() =>
        typeof(Icons).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(Geometry))
            .Select(p => (p.Name, (Geometry)p.GetValue(null)!));

    private static Control Set()
    {
        var set = new WrapPanel();
        foreach (var (name, data) in All())
            set.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Width = 232,
                Margin = new Thickness(0, 0, 0, 12),
                Children = { new Icon { Data = data }, Ui.Body(name) },
            });
        return set;
    }

    private static Control Sizes()
    {
        var sizes = new WrapPanel();
        foreach (var (size, label) in new[] { (16d, "16 px"), (20d, "20 px"), (24d, "24 px") })
            sizes.Children.Add(new StackPanel
            {
                Spacing = 8,
                Margin = new Thickness(0, 0, 32, 12),
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children =
                        {
                            new Icon { Data = Icons.Gear, Width = size, Height = size },
                            new Icon { Data = Icons.CaretDown, Width = size, Height = size },
                            new Icon { Data = Icons.House, Width = size, Height = size },
                        },
                    },
                    Ui.Caption(label),
                },
            });
        return sizes;
    }

    private static Control AccentButton()
    {
        var button = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children = { new Icon { Data = Icons.Plus }, new TextBlock { Text = "Add", VerticalAlignment = VerticalAlignment.Center } },
            },
        };
        button.Classes.Add("accent");
        return button;
    }
}
