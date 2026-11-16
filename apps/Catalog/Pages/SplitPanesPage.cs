using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;

namespace Catalog.Pages;

/// <summary>
/// Split panes: while the page shows, the window has an inspector holding a PanelStack (Layers, Properties with sliders,
/// Colour collapsed) and a bottom panel holding a TabControl, with buttons that toggle them; leaving the page takes them
/// away again.
/// </summary>
internal sealed class SplitPanesPage : UserControl
{
    private readonly PanelStack _inspector = Inspector();
    private readonly TabControl _bottom = Bottom();
    private StandardsWindow? _window;

    public SplitPanesPage()
    {
        var inspector = new Button { Content = "Toggle inspector" };
        inspector.Click += (_, _) => _window?.ToggleInspector();
        var bottom = new Button { Content = "Toggle bottom panel" };
        bottom.Click += (_, _) => _window?.ToggleBottomPanel();

        Content = Ui.Page("Split panes",
            Ui.Section("Live", new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Children = { inspector, bottom } },
                    Ui.Caption("The View menu's Show Inspector and Show Bottom Panel items do the same (Opt+Cmd+I and Opt+Cmd+J in the " +
                        "macOS layout, Ctrl+Alt+I and Ctrl+J in the Windows layout)."),
                },
            }),
            Ui.Section("Use", Ui.Caption(
                "The inspector is a glass panel right of the content panel, the full height of the content area: 240 wide by default, " +
                "200 to 400. The bottom panel sits directly under the content panel: 200 high by default, at least 96 and never taller " +
                "than the content panel; in a content area under 200 it hides until the window grows. Each 8 px gap between panels is a resize handle; dragging it below the panel's minimum closes the " +
                "panel. The inspector docks from a window width of 1248; in a narrower window it opens over the content at the right " +
                "edge and closes on Escape or a click outside. A PanelStack holds solid cards with collapsible title strips; a tab " +
                "group is a TabControl in a card.")));

        AttachedToVisualTree += (_, _) =>
        {
            _window = TopLevel.GetTopLevel(this) as StandardsWindow;
            if (_window is null)
                return;
            _window.Inspector = _inspector;
            _window.BottomPanel = _bottom;
        };
        DetachedFromVisualTree += (_, _) =>
        {
            if (_window is not null)
            {
                if (_window.Inspector == _inspector)
                    _window.Inspector = null;
                if (_window.BottomPanel == _bottom)
                    _window.BottomPanel = null;
            }
            _window = null;
        };
    }

    private static PanelStack Inspector() => new()
    {
        Items =
        {
            new PanelStackItem
            {
                Header = "Layers",
                Icon = Icons.SquaresFour,
                IsExpanded = true,
                Content = new StackPanel
                {
                    Spacing = 4,
                    Children = { Ui.Body("Title"), Ui.Body("Photo"), Ui.Body("Background") },
                },
            },
            new PanelStackItem
            {
                Header = "Properties",
                Icon = Icons.SlidersHorizontal,
                IsExpanded = true,
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        Ui.Body("Opacity"), new Slider { Maximum = 100, Value = 80 },
                        Ui.Body("Blur"), new Slider { Maximum = 100, Value = 20 },
                    },
                },
            },
            new PanelStackItem { Header = "Colour", Icon = Icons.Palette, Content = new ColorPicker { Color = Avalonia.Media.Color.Parse("#0A84FF") } },
        },
    };

    private static TabControl Bottom() => new()
    {
        Items =
        {
            new TabItem { Header = "Output", Content = Ui.Body("Build succeeded.") },
            new TabItem { Header = "Problems", Content = Ui.Body("No problems.") },
            new TabItem { Header = "Terminal", Content = Ui.Body("$") },
        },
    };
}
