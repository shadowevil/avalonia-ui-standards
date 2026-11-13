using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The <see cref="ProgressRing"/> theme: a track circle in ProgressBar's track colour under an AA
/// accent arc with round ends. The size follows density (20 / 16, as icons); the stroke is a tenth
/// of the size, at least 2 px.
/// </summary>
internal static class ProgressRingTheme
{
    public const string ArcPart = "PART_Arc";

    public static ControlTheme Create() => new(typeof(ProgressRing))
    {
        Setters =
        {
            new Setter(TemplatedControl.ForegroundProperty, Resource("AccentFillColor")),
            new Setter(TemplatedControl.BackgroundProperty, Resource("SystemControlBackgroundBaseLowBrush")),
            new Setter(Layoutable.WidthProperty, new DynamicResourceExtension(RangeParts.RingKey)),
            new Setter(Layoutable.HeightProperty, new DynamicResourceExtension(RangeParts.RingKey)),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ProgressRing>(Build)),
        },
        Children =
        {
            new Avalonia.Styling.Style(x => x.Nesting().Class(":disabled"))
            {
                Setters = { new Setter(TemplatedControl.ForegroundProperty, Resource("SystemControlForegroundBaseMediumLowBrush")) },
            },
        },
    };

    private static Control Build(ProgressRing ring, INameScope scope)
    {
        var arc = new RingArc { Name = ArcPart };
        arc.Bind(RingArc.TrackProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        arc.Bind(RingArc.ArcProperty, new TemplateBinding(TemplatedControl.ForegroundProperty));
        arc.Bind(RingArc.IsIndeterminateProperty, new TemplateBinding(ProgressRing.IsIndeterminateProperty));
        void UpdateFraction() => arc.Fraction = ring.Maximum > ring.Minimum
            ? Math.Clamp((ring.Value - ring.Minimum) / (ring.Maximum - ring.Minimum), 0, 1)
            : 0;
        ring.PropertyChanged += (_, e) =>
        {
            if (e.Property == RangeBase.ValueProperty || e.Property == RangeBase.MinimumProperty || e.Property == RangeBase.MaximumProperty)
                UpdateFraction();
        };
        UpdateFraction();
        arc.RegisterInNameScope(scope);
        return arc;
    }
}

/// <summary>
/// Draws a ProgressRing: the track circle and the arc. Determinate, the arc runs clockwise from the
/// top for <see cref="Fraction"/> of the turn. Indeterminate, a quarter arc turns once per
/// <see cref="Period"/> on the window's frame ticks (<see cref="ContinuousClock"/>); its still frame
/// (time zero) is centred on the top, so it never reads as an amount.
/// </summary>
internal sealed class RingArc : Control
{
    public static readonly StyledProperty<IBrush?> TrackProperty =
        AvaloniaProperty.Register<RingArc, IBrush?>(nameof(Track));

    public static readonly StyledProperty<IBrush?> ArcProperty =
        AvaloniaProperty.Register<RingArc, IBrush?>(nameof(Arc));

    public static readonly StyledProperty<double> FractionProperty =
        AvaloniaProperty.Register<RingArc, double>(nameof(Fraction));

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<RingArc, bool>(nameof(IsIndeterminate));

    /// <summary>One turn of the indeterminate arc.</summary>
    public static readonly TimeSpan Period = TimeSpan.FromSeconds(1);

    /// <summary>The indeterminate arc's length, in degrees.</summary>
    public const double IndeterminateSweep = 90;

    static RingArc() => AffectsRender<RingArc>(TrackProperty, ArcProperty, FractionProperty, IsIndeterminateProperty);

    public RingArc() => Clock = new ContinuousClock(this, InvalidateVisual);

    internal ContinuousClock Clock { get; }

    public IBrush? Track
    {
        get => GetValue(TrackProperty);
        set => SetValue(TrackProperty, value);
    }

    public IBrush? Arc
    {
        get => GetValue(ArcProperty);
        set => SetValue(ArcProperty, value);
    }

    public double Fraction
    {
        get => GetValue(FractionProperty);
        set => SetValue(FractionProperty, value);
    }

    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>The stroke for a ring of <paramref name="size"/>: a tenth of it, at least 2 px.</summary>
    internal static double Stroke(double size) => Math.Max(2, Math.Round(size / 10));

    /// <summary>The arc's start (degrees clockwise from the top) and sweep.</summary>
    internal (double Start, double Sweep) Span() => IsIndeterminate
        ? (-IndeterminateSweep / 2 + 360 * (Clock.Time.TotalSeconds / Period.TotalSeconds % 1), IndeterminateSweep)
        : (0, 360 * Fraction);

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        var stroke = Stroke(size);
        var radius = (size - stroke) / 2;
        if (radius <= 0)
            return;
        var centre = new Point(Bounds.Width / 2, Bounds.Height / 2);
        if (Track is { } track)
            context.DrawEllipse(null, new Pen(track, stroke), centre, radius, radius);

        var (start, sweep) = Span();
        if (Arc is not { } brush || sweep <= 0)
            return;
        var pen = new Pen(brush, stroke, lineCap: PenLineCap.Round);
        if (sweep >= 360)
        {
            context.DrawEllipse(null, pen, centre, radius, radius);
            return;
        }
        Point At(double degrees)
        {
            var radians = degrees * Math.PI / 180;
            return new Point(centre.X + radius * Math.Sin(radians), centre.Y - radius * Math.Cos(radians));
        }
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(At(start), false);
            g.ArcTo(At(start + sweep), new Size(radius, radius), 0, sweep > 180, SweepDirection.Clockwise);
            g.EndFigure(false);
        }
        context.DrawGeometry(null, pen, geometry);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Clock.Run(IsIndeterminate);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Clock.Run(false);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsIndeterminateProperty)
            Clock.Run(IsIndeterminate && this.IsAttachedToVisualTree());
    }
}
