using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUiStandards.Tests;

/// <summary>Backdrops to render glass over.</summary>
internal static class Backdrop
{
    /// <summary>A colourful backdrop with a grid, strong straight lines and shapes, so blur and bending show.</summary>
    public static Control Colourful() => new ColourfulBackdrop();

    /// <summary>Sharp vertical black and white stripes, <paramref name="period"/> px wide per pair.</summary>
    public static Control Stripes(int period = 8) => new StripesBackdrop(period);

    private sealed class ColourfulBackdrop : Control
    {
        public ColourfulBackdrop() => ClipToBounds = true;

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
        private static readonly IPen LinePen = new Pen(Brushes.Black, 4);
        private static readonly IPen WhitePen = new Pen(Brushes.White, 4);

        public override void Render(DrawingContext context)
        {
            var b = new Rect(Bounds.Size);
            context.FillRectangle(Gradient, b);

            for (var x = 0.5; x < b.Width; x += 16)
                context.DrawLine(GridPen, new Point(x, 0), new Point(x, b.Height));
            for (var y = 0.5; y < b.Height; y += 16)
                context.DrawLine(GridPen, new Point(0, y), new Point(b.Width, y));

            for (var x = -b.Height; x < b.Width; x += 48)
                context.DrawLine(WhitePen, new Point(x, b.Height), new Point(x + b.Height, 0));

            context.FillRectangle(Brushes.Navy, new Rect(b.Width * 0.35, b.Height * 0.15, 28, b.Height * 0.7));
            context.DrawLine(LinePen, new Point(0, b.Height * 0.5), new Point(b.Width, b.Height * 0.5));
            context.DrawEllipse(Brushes.Magenta, null, new Point(b.Width * 0.75, b.Height * 0.6), 36, 36);
            context.DrawEllipse(Brushes.White, null, new Point(b.Width * 0.15, b.Height * 0.3), 30, 30);
        }
    }

    private sealed class StripesBackdrop(int period) : Control
    {
        public override void Render(DrawingContext context)
        {
            context.FillRectangle(Brushes.White, new Rect(Bounds.Size));
            for (var x = 0; x < Bounds.Width; x += period)
                context.FillRectangle(Brushes.Black, new Rect(x, 0, period / 2.0, Bounds.Height));
        }
    }
}
