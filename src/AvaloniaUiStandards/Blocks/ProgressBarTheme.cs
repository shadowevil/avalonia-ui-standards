using System.Globalization;
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
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The ProgressBar control theme: a 4 px pill track (Fluent's base-low) with the AA accent fill.
/// Indeterminate, a segment 40 % of the track sweeps across it, driven by the window's frame ticks;
/// its still frame (outside a <c>StandardsWindow</c>, and under reduce motion) is the segment
/// centred, so it never reads as an amount. The progress text, when shown, sits after the bar.
/// </summary>
internal static class ProgressBarTheme
{
    public const string TrackPart = "PART_Track";
    public const string IndicatorPart = "PART_Indicator";
    public const string SweepPart = "PART_Sweep";
    public const string TextPart = "PART_Text";

    public static ControlTheme Create()
    {
        var theme = new ControlTheme(typeof(ProgressBar));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Resource("AccentFillColor")));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Resource("SystemControlBackgroundBaseLowBrush")));
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, RangeParts.Pill));
        theme.Setters.Add(new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center));
        foreach (var (orientation, pseudoClass) in new[] { (Orientation.Horizontal, ":horizontal"), (Orientation.Vertical, ":vertical") })
            theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(pseudoClass))
            {
                Setters =
                {
                    new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ProgressBar>((bar, scope) => Build(bar, scope, orientation))),
                    // Fluent's lengths: 200 along the bar.
                    new Setter(orientation == Orientation.Horizontal ? Layoutable.MinWidthProperty : Layoutable.MinHeightProperty, 200d),
                },
            });

        theme.Children.Add(Visible(x => x.Nesting(), SweepPart, false));
        theme.Children.Add(Visible(x => x.Nesting().Class(":indeterminate"), SweepPart, true));
        theme.Children.Add(Visible(x => x.Nesting().Class(":indeterminate"), IndicatorPart, false));
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":disabled"))
        {
            Setters = { new Setter(TemplatedControl.ForegroundProperty, Resource("SystemControlForegroundBaseMediumLowBrush")) },
        });
        return theme;
    }

    private static Avalonia.Styling.Style Visible(Func<Selector?, Selector> state, string part, bool visible) =>
        new(x => state(x).Template().Name(part)) { Setters = { new Setter(Visual.IsVisibleProperty, visible) } };

    private static Control Build(ProgressBar bar, INameScope scope, Orientation orientation)
    {
        var vertical = orientation == Orientation.Vertical;
        var indicator = new Border { Name = IndicatorPart };
        indicator.Bind(Border.BackgroundProperty, new TemplateBinding(TemplatedControl.ForegroundProperty));
        indicator.Bind(Border.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        if (vertical)
            indicator.VerticalAlignment = VerticalAlignment.Bottom;
        else
            indicator.HorizontalAlignment = HorizontalAlignment.Left;

        var sweep = new ProgressSweep { Name = SweepPart, Orientation = orientation };
        sweep.Bind(ProgressSweep.BrushProperty, new TemplateBinding(TemplatedControl.ForegroundProperty));

        var track = new SquircleBorder { Name = TrackPart, Child = new Panel { Children = { indicator, sweep } } };
        track.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        track.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        if (vertical)
            track.Width = RangeParts.TrackThickness;
        else
            track.Height = RangeParts.TrackThickness;

        var text = new TextBlock { Name = TextPart, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        text.Bind(Visual.IsVisibleProperty, new TemplateBinding(ProgressBar.ShowProgressTextProperty));
        text.Bind(TextBlock.FontSizeProperty, Resource("FontSizeCaption"));
        text.Bind(TextBlock.ForegroundProperty, Resource("SystemControlForegroundBaseMediumBrush"));
        void UpdateText() => text.Text = string.Format(CultureInfo.CurrentCulture, bar.ProgressTextFormat ?? "", bar.Value, bar.Percentage, bar.Minimum, bar.Maximum);
        bar.PropertyChanged += (_, e) =>
        {
            if (e.Property == RangeBase.ValueProperty || e.Property == ProgressBar.PercentageProperty || e.Property == ProgressBar.ProgressTextFormatProperty
                || e.Property == RangeBase.MinimumProperty || e.Property == RangeBase.MaximumProperty)
                UpdateText();
        };
        UpdateText();

        var grid = new Grid { Children = { track, text } };
        if (vertical)
        {
            grid.RowDefinitions = new RowDefinitions("*,Auto");
            text.Margin = new Thickness(0, Spacing.S, 0, 0);
            Grid.SetRow(text, 1);
        }
        else
        {
            grid.ColumnDefinitions = new ColumnDefinitions("*,Auto");
            text.Margin = new Thickness(Spacing.S, 0, 0, 0);
            Grid.SetColumn(text, 1);
        }

        foreach (var part in new Control[] { indicator, sweep, track, text })
            part.RegisterInNameScope(scope);
        return grid;
    }
}

/// <summary>
/// The indeterminate segment of a ProgressBar: a pill 40 % of the track long that sweeps from
/// before the start to past the end every <see cref="Period"/>, easing gently, on the window's
/// frame ticks (<see cref="ContinuousClock"/>). At time zero it is centred.
/// </summary>
internal sealed class ProgressSweep : Control
{
    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<ProgressSweep, IBrush?>(nameof(Brush));

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ProgressSweep, Orientation>(nameof(Orientation));

    /// <summary>One sweep across the track.</summary>
    public static readonly TimeSpan Period = TimeSpan.FromSeconds(2);

    /// <summary>The segment's length, as a share of the track.</summary>
    public const double Share = 0.4;

    static ProgressSweep() => AffectsRender<ProgressSweep>(BrushProperty, OrientationProperty);

    public ProgressSweep() => Clock = new ContinuousClock(this, InvalidateVisual);

    internal ContinuousClock Clock { get; }

    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>The segment's start and length along a track of <paramref name="length"/> at <paramref name="time"/>.</summary>
    internal static (double Start, double Length) Segment(double length, TimeSpan time)
    {
        var segment = length * Share;
        // Phase 0.5 is the middle of the sweep (the segment centred), so time zero is the still frame.
        var phase = (time.TotalSeconds / Period.TotalSeconds + 0.5) % 1;
        // A gentle ease (half linear, half sine in-out): it slows towards the ends without lingering
        // out of view, so part of the segment shows for nearly the whole period.
        var eased = 0.5 * phase + 0.5 * (1 - Math.Cos(Math.PI * phase)) / 2;
        return (-segment + (length + segment) * eased, segment);
    }

    public override void Render(DrawingContext context)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var length = horizontal ? Bounds.Width : Bounds.Height;
        if (Brush is null || length <= 0)
            return;
        var (start, size) = Segment(length, Clock.Time);
        // Vertical bars fill from the bottom, so the segment sweeps upwards.
        var rect = horizontal
            ? new Rect(start, 0, size, Bounds.Height)
            : new Rect(0, Bounds.Height - start - size, Bounds.Width, size);
        var radius = Math.Min(rect.Width, rect.Height) / 2;
        context.DrawRectangle(Brush, null, new RoundedRect(rect, radius));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Clock.Run(IsVisible);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Clock.Run(false);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty)
            Clock.Run(IsVisible && this.IsAttachedToVisualTree());
    }
}
