using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The ScrollBar theme: an overlay bar with no line buttons. It is invisible at rest; while
/// scrolling a thin pill thumb shows and fades out after <see cref="HideDelay"/>. As the pointer
/// comes within <see cref="ProximityZone"/> of the bar, the thumb fades in and widens with
/// closeness to <see cref="FullWidth"/>, over a faint pill track. Clicking the track pages, the
/// thumb drags, and the context menu is Fluent's.
/// </summary>
internal static class ScrollBarTheme
{
    /// <summary>The gap between the pills and the scroller's edge (<c>SpacingS</c>).</summary>
    public const double EdgeGap = Spacing.S;

    /// <summary>The gap between the full-width pills and the bar's inner side.</summary>
    public const double InnerGap = 2;

    /// <summary>The thumb's width while scrolling with the pointer away.</summary>
    public const double ThinWidth = 3;

    /// <summary>The thumb's and track's width with the pointer on the bar.</summary>
    public const double FullWidth = 8;

    /// <summary>The distance from the bar within which it reacts to the pointer.</summary>
    public const double ProximityZone = 24;

    /// <summary>The thumb's shortest length.</summary>
    public const double MinThumbLength = 24;

    /// <summary>How long the thumb stays after the last scroll.</summary>
    public static readonly TimeSpan HideDelay = TimeSpan.FromSeconds(1);

    /// <summary>Thumb at rest, on hover and while dragging, and the track: black (light) or white (dark).</summary>
    internal static readonly (Color Thumb, Color Hover, Color Drag, Color Track) Light =
        (Color.Parse("#61000000"), Color.Parse("#85000000"), Color.Parse("#A3000000"), Color.Parse("#0F000000"));
    internal static readonly (Color Thumb, Color Hover, Color Drag, Color Track) Dark =
        (Color.Parse("#61FFFFFF"), Color.Parse("#85FFFFFF"), Color.Parse("#A3FFFFFF"), Color.Parse("#14FFFFFF"));

    // Colours per theme, and the parts' transitions, under non-public keys.
    internal static readonly object ThumbKey = new();
    internal static readonly object ThumbHoverKey = new();
    internal static readonly object ThumbDragKey = new();
    internal static readonly object TrackKey = new();
    internal static readonly object PartTransitionsKey = new();

    /// <summary>The bar's thickness: its grab area runs from the scroller's edge to past the full-width pill.</summary>
    public const double Thickness = EdgeGap + FullWidth + InnerGap;

    /// <summary>
    /// How far the pills stay from the bar's ends: the edge gap, or half the panel corner
    /// (<c>CornerRadiusOverlay</c>) when that is larger, so the ends clear the rounded corners.
    /// </summary>
    public static double EndInset(CornerRadius corner) => Math.Max(EdgeGap, corner.TopLeft / 2);

    // Bare themes for the parts, so no other theme styles them.
    private static readonly ControlTheme BareThumb = new(typeof(Thumb));
    private static readonly ControlTheme BarePageButton = new(typeof(RepeatButton));

    public static void AddResources(IResourceDictionary resources)
    {
        foreach (var (variant, colours) in new[] { (ThemeVariant.Light, Light), (ThemeVariant.Dark, Dark) })
        {
            var dictionary = (IResourceDictionary)resources.ThemeDictionaries[variant];
            dictionary[ThumbKey] = new SolidColorBrush(colours.Thumb);
            dictionary[ThumbHoverKey] = new SolidColorBrush(colours.Hover);
            dictionary[ThumbDragKey] = new SolidColorBrush(colours.Drag);
            dictionary[TrackKey] = new SolidColorBrush(colours.Track);
        }
    }

    /// <summary>
    /// The pills' fades, widening and colour changes over <paramref name="duration"/>, or none when
    /// it is zero (reduce motion). Colours change through <see cref="CrossFadeBrushTransition"/>.
    /// </summary>
    public static Transitions PartTransitions(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? []
        :
        [
            new DoubleTransition { Property = Visual.OpacityProperty, Duration = duration, Easing = easing },
            new DoubleTransition { Property = Layoutable.WidthProperty, Duration = duration, Easing = easing },
            new DoubleTransition { Property = Layoutable.HeightProperty, Duration = duration, Easing = easing },
            new CrossFadeBrushTransition { Property = Border.BackgroundProperty, Duration = duration, Easing = easing },
        ];

    /// <param name="fluentTheme">Fluent's ScrollBar theme, whose context menus the bar keeps.</param>
    public static ControlTheme Create(ControlTheme? fluentTheme)
    {
        var theme = new ControlTheme(typeof(ScrollBar));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        foreach (var (orientation, pseudoClass) in new[] { (Orientation.Vertical, ":vertical"), (Orientation.Horizontal, ":horizontal") })
        {
            var style = new Avalonia.Styling.Style(x => x.Nesting().Class(pseudoClass))
            {
                Setters =
                {
                    new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ScrollBar>((bar, scope) => Build(bar, scope, orientation))),
                    new Setter(orientation == Orientation.Vertical ? Layoutable.WidthProperty : Layoutable.HeightProperty, Thickness),
                },
            };
            if (FluentContextFlyout(fluentTheme, pseudoClass) is { } flyout)
                style.Setters.Add(new Setter(Control.ContextFlyoutProperty, flyout));
            theme.Children.Add(style);
        }
        return theme;
    }

    private static object? FluentContextFlyout(ControlTheme? fluentTheme, string pseudoClass) =>
        fluentTheme?.Children.OfType<Avalonia.Styling.Style>()
            .Where(s => s.Selector?.ToString()?.EndsWith(pseudoClass, StringComparison.Ordinal) == true)
            .SelectMany(s => s.Setters.OfType<Setter>())
            .FirstOrDefault(s => s.Property == Control.ContextFlyoutProperty)?.Value;

    private static Control Build(ScrollBar bar, INameScope scope, Orientation orientation)
    {
        var vertical = orientation == Orientation.Vertical;
        var pageUp = PageButton("PART_PageUpButton");
        var pageDown = PageButton("PART_PageDownButton");
        var thumb = new Thumb { Theme = BareThumb };
        if (vertical)
            thumb.MinHeight = MinThumbLength;
        else
            thumb.MinWidth = MinThumbLength;

        var track = new Track
        {
            Name = "PART_Track",
            Orientation = orientation,
            IsDirectionReversed = vertical,
            Thumb = thumb,
            DecreaseButton = pageUp,
            IncreaseButton = pageDown,
        };
        track.Bind(Track.MinimumProperty, new TemplateBinding(RangeBase.MinimumProperty));
        track.Bind(Track.MaximumProperty, new TemplateBinding(RangeBase.MaximumProperty));
        track.Bind(Track.ValueProperty, new TemplateBinding(RangeBase.ValueProperty) { Mode = BindingMode.TwoWay });
        track.Bind(Track.ViewportSizeProperty, new TemplateBinding(ScrollBar.ViewportSizeProperty));
        track.Bind(Track.DeferThumbDragProperty, new TemplateBinding(ScrollViewer.IsDeferredScrollingEnabledProperty));

        var chrome = new ScrollBarChrome(bar, track, thumb, vertical);
        track.RegisterInNameScope(scope);
        pageUp.RegisterInNameScope(scope);
        pageDown.RegisterInNameScope(scope);
        return chrome;
    }

    private static RepeatButton PageButton(string name) => new()
    {
        Name = name,
        Theme = BarePageButton,
        Focusable = false,
        Template = new FuncControlTemplate<RepeatButton>((_, _) => new Border { Background = Brushes.Transparent }),
    };
}

/// <summary>A border whose corner radius is always half its shorter side: a pill at every size.</summary>
internal sealed class PillBorder : Border
{
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        CornerRadius = new CornerRadius(Math.Min(e.NewSize.Width, e.NewSize.Height) / 2);
    }
}

/// <summary>
/// The ScrollBar template root: the track pill behind the track, the thumb pill inside the thumb,
/// and the logic that shows, widens and colours them from scrolling, pointer proximity, hover and drag.
/// </summary>
internal sealed class ScrollBarChrome : Panel
{
    public static readonly StyledProperty<IBrush?> ThumbBrushProperty =
        AvaloniaProperty.Register<ScrollBarChrome, IBrush?>(nameof(ThumbBrush));

    public static readonly StyledProperty<IBrush?> ThumbHoverBrushProperty =
        AvaloniaProperty.Register<ScrollBarChrome, IBrush?>(nameof(ThumbHoverBrush));

    public static readonly StyledProperty<IBrush?> ThumbDragBrushProperty =
        AvaloniaProperty.Register<ScrollBarChrome, IBrush?>(nameof(ThumbDragBrush));

    public static readonly StyledProperty<CornerRadius> PanelCornerProperty =
        AvaloniaProperty.Register<ScrollBarChrome, CornerRadius>(nameof(PanelCorner));

    private readonly ScrollBar _bar;
    private readonly Track _track;
    private readonly bool _vertical;
    private readonly DispatcherTimer _hideTimer = new() { Interval = ScrollBarTheme.HideDelay };
    private Control? _host;
    private bool _scrolling;
    private bool _dragging;
    private bool _settled;

    public ScrollBarChrome(ScrollBar bar, Track track, Thumb thumb, bool vertical)
    {
        _bar = bar;
        _vertical = vertical;
        _track = track;
        TrackPill = Pill("PART_TrackPill");
        TrackPill.Bind(Border.BackgroundProperty, new DynamicResourceExtension(ScrollBarTheme.TrackKey));
        ThumbPill = Pill("PART_ThumbPill");
        thumb.Template = new FuncControlTemplate<Thumb>((_, _) => new Panel { Background = Brushes.Transparent, Children = { ThumbPill } });
        thumb.AddHandler(Thumb.DragStartedEvent, (_, _) => SetDragging(true));
        thumb.AddHandler(Thumb.DragCompletedEvent, (_, _) => SetDragging(false));

        Bind(ThumbBrushProperty, new DynamicResourceExtension(ScrollBarTheme.ThumbKey));
        Bind(ThumbHoverBrushProperty, new DynamicResourceExtension(ScrollBarTheme.ThumbHoverKey));
        Bind(ThumbDragBrushProperty, new DynamicResourceExtension(ScrollBarTheme.ThumbDragKey));
        Bind(PanelCornerProperty, new DynamicResourceExtension("CornerRadiusOverlay"));
        Background = Brushes.Transparent;
        UpdateInsets();
        Children.AddRange([TrackPill, track]);
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            _scrolling = false;
            Update();
        };
        Update();
    }

    public IBrush? ThumbBrush
    {
        get => GetValue(ThumbBrushProperty);
        set => SetValue(ThumbBrushProperty, value);
    }

    public IBrush? ThumbHoverBrush
    {
        get => GetValue(ThumbHoverBrushProperty);
        set => SetValue(ThumbHoverBrushProperty, value);
    }

    public IBrush? ThumbDragBrush
    {
        get => GetValue(ThumbDragBrushProperty);
        set => SetValue(ThumbDragBrushProperty, value);
    }

    /// <summary>The panel corner the pills' ends keep clear of.</summary>
    public CornerRadius PanelCorner
    {
        get => GetValue(PanelCornerProperty);
        set => SetValue(PanelCornerProperty, value);
    }

    internal PillBorder ThumbPill { get; }

    internal PillBorder TrackPill { get; }

    /// <summary>The pointer's closeness to the bar: 0 at <see cref="ScrollBarTheme.ProximityZone"/> or beyond, 1 on the bar.</summary>
    internal double Proximity { get; private set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _host = (_bar.TemplatedParent as Control) ?? _bar.GetVisualParent() as Control;
        _host?.AddHandler(PointerMovedEvent, OnHostPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
        _host?.AddHandler(PointerExitedEvent, OnHostPointerExited, RoutingStrategies.Direct, handledEventsToo: true);
        _bar.PropertyChanged += OnBarPropertyChanged;

        // Values set while the first layout settles are not scrolling; fades start after it.
        _settled = false;
        Dispatcher.UIThread.Post(() =>
        {
            if (_host is null)
                return;
            _settled = true;
            foreach (var pill in new[] { ThumbPill, TrackPill })
                pill.Bind(Animatable.TransitionsProperty, new DynamicResourceExtension(ScrollBarTheme.PartTransitionsKey));
        }, DispatcherPriority.Background);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _host?.RemoveHandler(PointerMovedEvent, OnHostPointerMoved);
        _host?.RemoveHandler(PointerExitedEvent, OnHostPointerExited);
        _bar.PropertyChanged -= OnBarPropertyChanged;
        _host = null;
        _hideTimer.Stop();
        foreach (var pill in new[] { ThumbPill, TrackPill })
            pill.ClearValue(Animatable.TransitionsProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ThumbBrushProperty || change.Property == ThumbHoverBrushProperty || change.Property == ThumbDragBrushProperty)
            Update();
        else if (change.Property == PanelCornerProperty)
            UpdateInsets();
    }

    // The pills sit EdgeGap from the scroller's edge, their ends clear of the panel corner; the
    // thumb travels the same length as the track pill.
    private void UpdateInsets()
    {
        var gap = ScrollBarTheme.EdgeGap;
        var end = ScrollBarTheme.EndInset(PanelCorner);
        _track.Margin = _vertical ? new Thickness(0, end) : new Thickness(end, 0);
        ThumbPill.Margin = _vertical ? new Thickness(0, 0, gap, 0) : new Thickness(0, 0, 0, gap);
        TrackPill.Margin = _vertical ? new Thickness(0, end, gap, end) : new Thickness(end, 0, end, gap);
    }

    private void OnBarPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == RangeBase.ValueProperty && _settled)
        {
            _scrolling = true;
            _hideTimer.Stop();
            _hideTimer.Start();
            Update();
        }
        else if (e.Property == InputElement.IsPointerOverProperty || e.Property == ScrollBar.AllowAutoHideProperty)
            Update();
    }

    private void OnHostPointerMoved(object? sender, PointerEventArgs e) => SetProximity(ProximityAt(e.GetPosition(_bar)));

    private void OnHostPointerExited(object? sender, PointerEventArgs e) => SetProximity(0);

    /// <summary>The proximity of a point in the bar's coordinates.</summary>
    internal double ProximityAt(Point point)
    {
        var size = _bar.Bounds.Size;
        var dx = point.X < 0 ? -point.X : point.X > size.Width ? point.X - size.Width : 0;
        var dy = point.Y < 0 ? -point.Y : point.Y > size.Height ? point.Y - size.Height : 0;
        return Math.Clamp(1 - Math.Sqrt(dx * dx + dy * dy) / ScrollBarTheme.ProximityZone, 0, 1);
    }

    internal void SetProximity(double proximity)
    {
        if (Proximity == proximity)
            return;
        Proximity = proximity;
        Update();
    }

    private void SetDragging(bool dragging)
    {
        _dragging = dragging;
        Update();
    }

    // Width grows with closeness (full on hover and drag); the thumb shows while scrolling, near
    // the pointer, or always when auto-hide is off; the track shows with closeness.
    private void Update()
    {
        var closeness = _dragging || _bar.IsPointerOver ? 1 : Proximity;
        var width = ScrollBarTheme.ThinWidth + (ScrollBarTheme.FullWidth - ScrollBarTheme.ThinWidth) * closeness;
        var shown = _scrolling || !_bar.AllowAutoHide ? 1 : 0;
        ThumbPill.Opacity = Math.Max(shown, Math.Min(1, 2 * closeness));
        TrackPill.Opacity = closeness;
        ThumbPill.Background = _dragging ? ThumbDragBrush : _bar.IsPointerOver ? ThumbHoverBrush : ThumbBrush;
        foreach (var pill in new[] { ThumbPill, TrackPill })
        {
            if (_vertical)
                pill.Width = width;
            else
                pill.Height = width;
        }
    }

    private PillBorder Pill(string name) => new()
    {
        Name = name,
        IsHitTestVisible = false,
        HorizontalAlignment = _vertical ? HorizontalAlignment.Right : HorizontalAlignment.Stretch,
        VerticalAlignment = _vertical ? VerticalAlignment.Stretch : VerticalAlignment.Bottom,
    };
}
