using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using AvaloniaUiStandards.Style;

namespace Catalog.Pages;

/// <summary>Separator and GridSplitter: the hairline, and the splitter's grip in every state next to live splitters.</summary>
internal sealed class SeparatorPage : UserControl
{
    private static readonly (string Label, string[] PseudoClasses, bool Enabled)[] States =
    [
        ("Rest", [], true),
        ("Hover", [":pointerover"], true),
        ("Dragging", [":pointerover", ":pressed"], true),
        ("Focused", [":focus-visible"], true),
        ("Disabled", [], false),
    ];

    public SeparatorPage() => Content = Ui.Page("Separator and GridSplitter",
        Ui.Section("Separator", new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new StackPanel { Width = 200, HorizontalAlignment = HorizontalAlignment.Left, Children = { Ui.Body("Above"), new Separator(), Ui.Body("Below") } },
                Ui.Caption("1 px in the border colour, 4 px above and below. In a menu it is inset 8 px further from the sides."),
            },
        }),
        Ui.Section("GridSplitter states", StatesGrid()),
        Ui.Section("Live", new WrapPanel
        {
            Children =
            {
                Ui.Spaced(Split(columns: true, width: 200, height: 140)),
                Ui.Spaced(Split(columns: false, width: 160, height: 180)),
            },
        }),
        Ui.Caption("Invisible at rest; a pill grip shows on hover, darker while dragging. Tab to a splitter and use the arrow keys to resize."));

    private static Control StatesGrid()
    {
        var grid = Ui.StateTable(States.Select(s => s.Label).ToList(), 2);
        Ui.Place(grid, Ui.Body("Between columns"), 1, 0);
        Ui.Place(grid, Ui.Body("Between rows"), 2, 0);
        for (var c = 0; c < States.Length; c++)
        {
            Ui.Place(grid, Forced(Split(columns: true, width: 140, height: 90), States[c]), 1, c + 1);
            Ui.Place(grid, Forced(Split(columns: false, width: 110, height: 110), States[c]), 2, c + 1);
        }
        return Ui.SideScroll(grid);
    }

    // A non-interactive copy of a split grid with its splitter forced into a state once attached.
    private static Control Forced(Grid grid, (string Label, string[] PseudoClasses, bool Enabled) state)
    {
        var splitter = grid.Children.OfType<GridSplitter>().Single();
        splitter.IsEnabled = state.Enabled;
        splitter.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            foreach (var pseudoClass in state.PseudoClasses)
                ((IPseudoClasses)splitter.Classes).Set(pseudoClass, true);
        });
        return new Border { Padding = new Thickness(4), IsHitTestVisible = false, Child = grid };
    }

    private static Grid Split(bool columns, double width, double height)
    {
        var grid = new Grid { Width = width, Height = height, HorizontalAlignment = HorizontalAlignment.Left };
        var splitter = new GridSplitter();
        var first = Pane("One");
        var second = Pane("Two");
        if (columns)
        {
            grid.ColumnDefinitions = new ColumnDefinitions("*,Auto,*");
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(second, 2);
        }
        else
        {
            grid.RowDefinitions = new RowDefinitions("*,Auto,*");
            Grid.SetRow(splitter, 1);
            Grid.SetRow(second, 2);
        }
        grid.Children.Add(first);
        grid.Children.Add(splitter);
        grid.Children.Add(second);
        return grid;
    }

    private static Control Pane(string text)
    {
        var pane = new SquircleBorder { Child = new TextBlock { Text = text, Margin = new Thickness(8) } };
        pane.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusControl"));
        pane.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("SystemControlBackgroundChromeMediumLowBrush"));
        return pane;
    }
}
