using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUiStandards.Style;

namespace Catalog.Pages;

/// <summary>
/// Glass panels over a colourful backdrop. The backdrop scrolls (wheel, scrollbars or dragging it),
/// so the refraction and blur change as it moves under the panels.
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
        stage.Children.Add(Panel(HorizontalAlignment.Left, 220, 140, "Glass card"));
        stage.Children.Add(Panel(HorizontalAlignment.Center, 280, 200, "Glass panel"));
        stage.Children.Add(Panel(HorizontalAlignment.Right, 160, 100, "Glass chip"));

        var clipped = new Border { ClipToBounds = true, Child = stage };
        Content = Ui.Page("Glass",
            Ui.Section("Over a backdrop", clipped),
            Ui.Caption("Scroll or drag the backdrop. Reduce transparency in the toolbar draws the panels solid."));
    }

    private static Control Panel(HorizontalAlignment alignment, double width, double height, string text) => new GlassBorder
    {
        Width = width,
        Height = height,
        Margin = new Thickness(24),
        HorizontalAlignment = alignment,
        VerticalAlignment = VerticalAlignment.Center,
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
