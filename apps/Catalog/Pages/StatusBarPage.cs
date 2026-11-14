using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;

namespace Catalog.Pages;

/// <summary>
/// StatusBar: a live bar on the window background in a card, with a stats row above the main row (a checkbox toggles it), every item kind, an export that runs a cancelable
/// progress item, and a slider that narrows the bar so its items shorten and hide; and the two progress kinds.
/// </summary>
internal sealed class StatusBarPage : UserControl
{
    private readonly TextBlock _last = Ui.Caption("Last command: none");
    private readonly StatusBar _live;
    private readonly StatusBarRow _stats = new();
    private readonly StatusProgress _export = new() { Text = "Exporting 0 %", IsCancelable = true, IsVisible = false };
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    public StatusBarPage()
    {
        _live = Live();
        _live.HorizontalAlignment = HorizontalAlignment.Left;
        var card = Card(_live);
        var width = new Slider { Minimum = 30, Maximum = 100, Value = 100, Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
        AutomationProperties.SetName(width, "Bar width");
        void Resize() => _live.Width = Math.Max(0, (card.Bounds.Width - 2) * width.Value / 100);
        card.SizeChanged += (_, _) => Resize();
        width.ValueChanged += (_, _) => Resize();

        var showStats = new CheckBox { Content = "Show stats row", IsChecked = true };
        showStats.IsCheckedChanged += (_, _) => _stats.IsVisible = showStats.IsChecked == true;

        var run = new Button { Content = "Run export" };
        run.Click += (_, _) => Start();
        _timer.Tick += (_, _) => Advance();
        _export.CancelRequested += (_, _) => Stop("Export cancelled");
        DetachedFromVisualTree += (_, _) => _timer.Stop();

        var syncing = new StatusBar { Message = "Syncing…" };
        var ring = new StatusProgress { Kind = StatusProgressKind.Ring, IsIndeterminate = true, Text = "Syncing 3 files" };
        StatusBar.SetZone(ring, StatusZone.Center);
        syncing.Items.Add(ring);
        var exporting = new StatusBar { Message = "Exporting poster.png…" };
        var bar = new StatusProgress { Value = 42, Text = "42 %", IsCancelable = true };
        StatusBar.SetZone(bar, StatusZone.Center);
        exporting.Items.Add(bar);

        Content = Ui.Page("Status bar",
            Ui.Section("Live", new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    card,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 12,
                        Children = { Ui.Body("Width"), width, run },
                    },
                    showStats,
                    _last,
                },
            }),
            Ui.Section("Progress", new StackPanel { Spacing = 12, Children = { Card(syncing), Card(exporting) } }),
            Ui.Section("Use", Ui.Caption(
                "The bottom of the window, on the window background: the main row, 24 high at every density, and any rows above it " +
                "(the bar's Rows, each 24 high, shown or hidden by the app). The message leads the main row and is announced " +
                "politely; leading, centre and trailing zones in every row. Text in Caption size and secondary text; clickable items are " +
                "subtle buttons and drop-downs with a 20 px hover fill. When a row is too narrow, its items take their shorter texts, then " +
                "hide, highest priority first, each row on its own. Clickable items are the window's last Tab stops, the rows from the " +
                "top and then the main row; text takes no focus.")));
    }

    private StatusBar Live()
    {
        var layer = new TextBlock { Text = "Layer: Background" };
        StatusBar.SetPriority(layer, 3);

        var size = new Button { Content = "4000 × 3000 px · 300 ppi" };
        Subtle(size, "Image size");
        StatusBar.SetShortTexts(size, ["4000 × 3000 px"]);
        StatusBar.SetPriority(size, 2);
        size.Click += (_, _) => _last.Text = "Last command: Image size";

        StatusBar.SetZone(_export, StatusZone.Center);

        var modeText = new TextBlock { Text = "RGB/8 · sRGB" };
        var mode = new DropDownButton
        {
            Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { new Icon { Data = Icons.Palette }, modeText } },
        };
        mode.Flyout = Menu(["RGB/8", "RGB/16", "CMYK/8", "Grayscale/8"], "RGB/8", v =>
        {
            StatusBar.SetShortTexts(mode, [v]);
            modeText.Text = $"{v} · sRGB";
        });
        Subtle(mode, "Colour mode");
        StatusBar.SetZone(mode, StatusZone.Trailing);
        StatusBar.SetShortTexts(mode, ["RGB/8"]);
        StatusBar.SetPriority(mode, 1);

        var zoom = new DropDownButton { Content = "100 %" };
        zoom.Flyout = Menu(["25 %", "50 %", "100 %", "200 %", "400 %"], "100 %", v => zoom.Content = v);
        Subtle(zoom, "Zoom");
        StatusBar.SetZone(zoom, StatusZone.Trailing);

        var cursor = new TextBlock { Text = "Cursor 1280, 640 px" };
        StatusBar.SetShortTexts(cursor, ["1280, 640"]);
        StatusBar.SetPriority(cursor, 2);
        var selection = new TextBlock { Text = "Selection 1200 × 800 px" };
        StatusBar.SetShortTexts(selection, ["1200 × 800"]);
        StatusBar.SetPriority(selection, 3);
        var memory = new TextBlock { Text = "Memory 412 MB" };
        StatusBar.SetShortTexts(memory, ["412 MB"]);
        StatusBar.SetPriority(memory, 1);
        var fps = new TextBlock { Text = "60 fps" };
        StatusBar.SetZone(memory, StatusZone.Trailing);
        StatusBar.SetZone(fps, StatusZone.Trailing);
        foreach (var item in new Control[] { cursor, selection, memory, fps })
            _stats.Items.Add(item);

        return new StatusBar { Message = "Ready", Items = { layer, size, _export, mode, zoom }, Rows = { _stats } };
    }

    private void Start()
    {
        _export.Value = 0;
        _export.Text = "Exporting 0 %";
        _export.IsVisible = true;
        _live.Message = "Exporting poster.png…";
        _timer.Start();
    }

    private void Advance()
    {
        _export.Value = Math.Min(100, _export.Value + 2);
        _export.Text = $"Exporting {_export.Value:0} %";
        if (_export.Value >= 100)
            Stop("Export finished");
    }

    private void Stop(string message)
    {
        _timer.Stop();
        _export.IsVisible = false;
        _live.Message = message;
    }

    // A menu of radio items, with the current one checked; choosing one calls back with its text.
    private MenuFlyout Menu(string[] values, string current, Action<string> chosen)
    {
        var flyout = new MenuFlyout();
        var group = Guid.NewGuid().ToString();
        foreach (var value in values)
        {
            var item = new MenuItem { Header = value, ToggleType = MenuItemToggleType.Radio, GroupName = group, IsChecked = value == current };
            item.Click += (_, _) =>
            {
                chosen(value);
                _last.Text = $"Last command: {value}";
            };
            flyout.Items.Add(item);
        }
        return flyout;
    }

    // The bar on the window background, in a card with the window's corner radius.
    private static SquircleBorder Card(StatusBar bar)
    {
        var card = new SquircleBorder { Child = bar, BorderThickness = new Thickness(1), ClipToBounds = true };
        card.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("BackdropWash"));
        card.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("SystemControlForegroundBaseLowBrush"));
        card.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusOverlay"));
        return card;
    }

    // A subtle item with a tooltip and automation name.
    private static void Subtle(Control item, string name)
    {
        item.Classes.Add("subtle");
        ToolTip.SetTip(item, name);
        AutomationProperties.SetName(item, name);
    }
}
