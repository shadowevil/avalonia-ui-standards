using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;

namespace Catalog.Pages;

/// <summary>
/// Glass panels over a colourful backdrop. The backdrop scrolls (wheel, scrollbars or dragging it),
/// so the refraction and blur change as it moves under the panels. Below, an opaque card in the
/// window's glass panel, with a live readout of the window's visible backdrop region.
/// </summary>
internal sealed class GlassPage : UserControl
{
    public GlassPage()
    {
        var scroller = new ScrollViewer
        {
            Height = 360,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new GlassBackdrop(),
        };
        EnableDragging(scroller);

        var stage = new Grid { Children = { scroller } };
        // The panels wrap onto further rows when the page is narrow.
        stage.Children.Add(new WrapPanel
        {
            Margin = new Thickness(12, 12, 0, 0),
            IsHitTestVisible = false,
            VerticalAlignment = VerticalAlignment.Top,
            Children =
            {
                Panel(180, 140, "Glass card"),
                Panel(220, 160, "Glass panel"),
                Panel(140, 100, "Glass chip"),
            },
        });

        var clipped = new Border { ClipToBounds = true, Child = stage };
        Content = Ui.Page("Glass",
            Ui.Section("Over a backdrop", clipped),
            Ui.Caption("Scroll or drag the backdrop. Reduce transparency in the toolbar band draws the panels solid."),
            Ui.Section("Opaque regions", OpaqueDemo()),
            Ui.Caption("Marked opaque, the card is left out of the panel's glass fill, and the window's backdrop is clipped around it, " +
                "shrunk by how far glass reads beyond what it shows. Scroll the page to see the region follow the card."));
    }

    // An opaque card the glass panel skips, a switch to mark it, and the window's visible backdrop region, live.
    private static Control OpaqueDemo()
    {
        var readout = Ui.Body("");
        readout.TextWrapping = TextWrapping.Wrap;
        var mark = new ToggleSwitch { Content = "Marked opaque", IsChecked = true };
        var card = new Border
        {
            Height = 220,
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(8),
            Child = new StackPanel { Spacing = 12, Children = { mark, readout } },
        };
        card.Bind(Border.BackgroundProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("SystemControlBackgroundAltHighBrush"));
        OpaqueRegion.SetIsOpaque(card, true);
        mark.IsCheckedChanged += (_, _) => OpaqueRegion.SetIsOpaque(card, mark.IsChecked == true);

        StandardsWindow? window = null;
        void Show()
        {
            if (window is null)
                return;
            var region = window.BackdropVisibleRegion;
            var total = region.Bounds.Width * region.Bounds.Height;
            var visible = region.Rects.Sum(r => r.Width * r.Height);
            readout.Text = region.IsEmpty
                ? "Visible backdrop: none"
                : $"Visible backdrop: {region.Rects.Count} rectangle{(region.Rects.Count == 1 ? "" : "s")}, " +
                  $"{100 * visible / Math.Max(1, total):F0} % of the frame";
        }
        void OnWindowChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == StandardsWindow.BackdropVisibleRegionProperty)
                Show();
        }
        card.AttachedToVisualTree += (_, _) =>
        {
            window = TopLevel.GetTopLevel(card) as StandardsWindow;
            if (window is not null)
                window.PropertyChanged += OnWindowChanged;
            Show();
        };
        card.DetachedFromVisualTree += (_, _) =>
        {
            if (window is not null)
                window.PropertyChanged -= OnWindowChanged;
            window = null;
        };
        return card;
    }

    private static Control Panel(double width, double height, string text) => new GlassBorder
    {
        Width = width,
        Height = height,
        Margin = new Thickness(0, 0, 12, 12),
        IsHitTestVisible = false,
        Child = new TextBlock { Text = text, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
    };

    private static void EnableDragging(ScrollViewer scroller)
    {
        Point? last = null;
        scroller.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(scroller).Properties.IsLeftButtonPressed)
                last = e.GetPosition(scroller);
        };
        scroller.PointerMoved += (_, e) =>
        {
            if (last is not { } from)
                return;
            var now = e.GetPosition(scroller);
            scroller.Offset = new Vector(
                Math.Max(0, scroller.Offset.X - (now.X - from.X)),
                Math.Max(0, scroller.Offset.Y - (now.Y - from.Y)));
            last = now;
        };
        scroller.PointerReleased += (_, _) => last = null;
        scroller.PointerCaptureLost += (_, _) => last = null;
    }

    /// <summary>A gradient with a grid, stripes and shapes, so blur and bending are visible.</summary>
    private sealed class GlassBackdrop : Control
    {
        private static readonly IBrush Gradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.Parse("#FF4D4D"), 0),
                new GradientStop(Color.Parse("#FFC800"), 0.3),
                new GradientStop(Color.Parse("#3CCB5A"), 0.6),
                new GradientStop(Color.Parse("#1E7BFF"), 1),
            },
        };

        private static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0)), 1);
        private static readonly IPen WhitePen = new Pen(Brushes.White, 4);
        private static readonly IPen BlackPen = new Pen(Brushes.Black, 4);

        public GlassBackdrop()
        {
            Width = 1800;
            Height = 900;
        }

        public override void Render(DrawingContext context)
        {
            var b = new Rect(Bounds.Size);
            context.FillRectangle(Gradient, b);
            for (var x = 0.5; x < b.Width; x += 16)
                context.DrawLine(GridPen, new Point(x, 0), new Point(x, b.Height));
            for (var y = 0.5; y < b.Height; y += 16)
                context.DrawLine(GridPen, new Point(0, y), new Point(b.Width, y));
            for (var x = -b.Height; x < b.Width; x += 64)
                context.DrawLine(WhitePen, new Point(x, b.Height), new Point(x + b.Height, 0));
            for (var y = 60.0; y < b.Height; y += 120)
                context.DrawLine(BlackPen, new Point(0, y), new Point(b.Width, y));
            for (var x = 100.0; x < b.Width; x += 260)
            {
                context.FillRectangle(Brushes.Navy, new Rect(x, 40, 28, b.Height - 80));
                context.DrawEllipse(Brushes.Magenta, null, new Point(x + 120, b.Height * 0.35), 36, 36);
                context.DrawEllipse(Brushes.White, null, new Point(x + 170, b.Height * 0.7), 30, 30);
            }
        }
    }
}
