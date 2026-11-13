using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// Builds labelled comparison sheets at 2x. Each cell is rendered in its own window (so a cell
/// can hold keyboard focus), then the captured cells are laid out in a grid with labels.
/// </summary>
internal sealed class Sheet : IDisposable
{
    public const double Scale = 2;

    private readonly List<Bitmap> _bitmaps = [];
    private readonly StackPanel _root = new() { Spacing = 16, Margin = new Thickness(16) };

    public Sheet(string title)
    {
        _root.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeight.SemiBold });
    }

    /// <summary>Renders <paramref name="content"/> on the region colour of <paramref name="variant"/>.</summary>
    public Control Cell(Control content, ThemeVariant variant, Action<Window>? beforeShow = null, Action<Window>? afterShow = null)
    {
        var surface = new Border { Padding = new Thickness(16, 12), Child = content };
        surface.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemRegionColor"));
        // Window styles and resources come first: a control picks its theme when it joins the tree.
        var window = new Window { SizeToContent = SizeToContent.WidthAndHeight, RequestedThemeVariant = variant };
        beforeShow?.Invoke(window);
        window.Content = surface;
        window.SetRenderScaling(Scale);
        window.Show();
        afterShow?.Invoke(window);
        Settle(window);

        // The capture is 192 DPI; drawing it as an image uses its size in pixels as the source
        // rectangle, so it is re-encoded at 96 DPI and stretched into a half-size box instead.
        using var capture = window.CaptureRenderedFrame()!;
        using var png = new MemoryStream();
        capture.Save(png, new PngBitmapEncoderOptions());
        png.Position = 0;
        var bitmap = new Bitmap(png);
        _bitmaps.Add(bitmap);
        window.Close();
        return new Image { Source = bitmap, Width = bitmap.PixelSize.Width / Scale, Height = bitmap.PixelSize.Height / Scale, Stretch = Stretch.Fill };
    }

    /// <summary>Adds a labelled grid: a header row, then one labelled row of cells per entry.</summary>
    public void AddSection(string heading, IReadOnlyList<string> columns, IEnumerable<(string Label, IReadOnlyList<Control> Cells)> rows)
    {
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 6 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        foreach (var _ in columns)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var c = 0; c < columns.Count; c++)
            Place(grid, Label(columns[c], HorizontalAlignment.Center), 0, c + 1);

        var row = 1;
        foreach (var (label, cells) in rows)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Place(grid, Label(label, HorizontalAlignment.Left), row, 0);
            for (var c = 0; c < cells.Count; c++)
                Place(grid, cells[c], row, c + 1);
            row++;
        }

        _root.Children.Add(new StackPanel
        {
            Spacing = 6,
            Children = { new TextBlock { Text = heading, FontSize = 14, FontWeight = FontWeight.SemiBold }, grid },
        });
    }

    /// <summary>Renders the sheet at 2x and saves it under the test output (screenshots/).</summary>
    public void Save(string name)
    {
        // Sized explicitly to the laid-out content, because SizeToContent is limited to the
        // headless screen size: shown large first, then shrunk to the content's bounds.
        _root.HorizontalAlignment = HorizontalAlignment.Left;
        _root.VerticalAlignment = VerticalAlignment.Top;
        var window = new Window
        {
            Width = 4000,
            Height = 4000,
            RequestedThemeVariant = ThemeVariant.Light,
            Background = Brushes.White,
            Foreground = Brushes.Black,
            Content = _root,
        };
        window.SetRenderScaling(Scale);
        window.Show();
        window.UpdateLayout();
        window.Width = Math.Ceiling(_root.Bounds.Right + _root.Margin.Right);
        window.Height = Math.Ceiling(_root.Bounds.Bottom + _root.Margin.Bottom);
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        using var frame = Frame.Capture(window);
        frame.Save(name);
        window.Close();
    }

    public void Dispose()
    {
        foreach (var bitmap in _bitmaps)
            bitmap.Dispose();
    }

    private static void Settle(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static TextBlock Label(string text, HorizontalAlignment alignment) => new()
    {
        Text = text,
        FontSize = 12,
        HorizontalAlignment = alignment,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static void Place(Grid grid, Control control, int row, int column)
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        grid.Children.Add(control);
    }
}
