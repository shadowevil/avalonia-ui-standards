using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The ScrollViewer theme: Fluent's ScrollViewer, unchanged, with a shadow layer over its content
/// cell (below the scroll bars). Along each edge with content out of view a soft shadow shows, a
/// gradient from a darkened accent to transparent; it fades in and out as you scroll.
/// </summary>
internal static class ScrollViewerTheme
{
    public const string ShadowsPart = "PART_ScrollShadows";

    /// <summary>The shadow depth in px.</summary>
    public const double ShadowSize = 12;

    /// <summary>The shadow tone: the accent's hue and saturation at lightness 0.35 and 8 % (light) or 0.12 and 18 % (dark).</summary>
    internal static readonly ShadowTone ShadowLight = new(0.35, 0.08);
    internal static readonly ShadowTone ShadowDark = new(0.12, 0.18);

    // The shadow depth, and its tone per theme, under non-public keys.
    internal static readonly object ShadowSizeKey = new();
    internal static readonly object ShadowToneKey = new();

    public static void AddResources(IResourceDictionary resources)
    {
        resources[ShadowSizeKey] = ShadowSize;
        ((IResourceDictionary)resources.ThemeDictionaries[ThemeVariant.Light])[ShadowToneKey] = ShadowLight;
        ((IResourceDictionary)resources.ThemeDictionaries[ThemeVariant.Dark])[ShadowToneKey] = ShadowDark;
    }

    public static ControlTheme Create(ControlTheme? fluentTheme)
    {
        var theme = new ControlTheme(typeof(ScrollViewer)) { BasedOn = fluentTheme };
        if (FluentTemplates.Extend(fluentTheme, Extend) is { } template)
            theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, template));
        return theme;
    }

    private static Control Extend(Control root, INameScope scope)
    {
        if (root is not Panel panel || scope.Find<ScrollContentPresenter>("PART_ContentPresenter") is not { } presenter)
            return root;

        // Over the content cell, below the scroll bars; it follows the cell the presenter takes.
        var shadows = new ScrollShadowOverlay { Name = ShadowsPart };
        shadows.Bind(Grid.RowProperty, presenter.GetObservable(Grid.RowProperty).ToBinding());
        shadows.Bind(Grid.ColumnProperty, presenter.GetObservable(Grid.ColumnProperty).ToBinding());
        shadows.Bind(Grid.RowSpanProperty, presenter.GetObservable(Grid.RowSpanProperty).ToBinding());
        shadows.Bind(Grid.ColumnSpanProperty, presenter.GetObservable(Grid.ColumnSpanProperty).ToBinding());
        shadows.Bind(Visual.IsVisibleProperty, new TemplateBinding(ScrollShadows.IsEnabledProperty));
        shadows.Bind(ScrollShadowOverlay.ShadowToneProperty, new DynamicResourceExtension(ShadowToneKey));
        shadows.Bind(ScrollShadowOverlay.ShadowSizeProperty, new DynamicResourceExtension(ShadowSizeKey));
        shadows.Bind(ScrollShadowOverlay.AccentColorProperty, new DynamicResourceExtension("SystemAccentColor"));
        panel.Children.Insert(panel.Children.IndexOf(presenter) + 1, shadows);
        return root;
    }
}

/// <summary>A scroll shadow tone: the HSL lightness and the alpha the accent's hue and saturation are shown at.</summary>
internal readonly record struct ShadowTone(double Lightness, double Alpha);

/// <summary>Four edge shadows over a ScrollViewer's content, each shown while content is out of view on its side.</summary>
internal sealed class ScrollShadowOverlay : Panel
{
    /// <summary>Offsets within this many px of an end count as at the end.</summary>
    private const double Tolerance = 0.5;

    public static readonly StyledProperty<ShadowTone> ShadowToneProperty =
        AvaloniaProperty.Register<ScrollShadowOverlay, ShadowTone>(nameof(ShadowTone));

    public static readonly StyledProperty<double> ShadowSizeProperty =
        AvaloniaProperty.Register<ScrollShadowOverlay, double>(nameof(ShadowSize), ScrollViewerTheme.ShadowSize);

    /// <summary>The accent colour, whose hue and saturation the shadow takes.</summary>
    public static readonly StyledProperty<Color> AccentColorProperty =
        AvaloniaProperty.Register<ScrollShadowOverlay, Color>(nameof(AccentColor));

    private readonly Border _top = Edge("PART_ShadowTop", VerticalAlignment.Top, HorizontalAlignment.Stretch);
    private readonly Border _bottom = Edge("PART_ShadowBottom", VerticalAlignment.Bottom, HorizontalAlignment.Stretch);
    private readonly Border _left = Edge("PART_ShadowLeft", VerticalAlignment.Stretch, HorizontalAlignment.Left);
    private readonly Border _right = Edge("PART_ShadowRight", VerticalAlignment.Stretch, HorizontalAlignment.Right);
    private ScrollViewer? _viewer;

    public ScrollShadowOverlay()
    {
        IsHitTestVisible = false;
        Children.AddRange([_top, _bottom, _left, _right]);
        UpdateSize();
    }

    public ShadowTone ShadowTone
    {
        get => GetValue(ShadowToneProperty);
        set => SetValue(ShadowToneProperty, value);
    }

    public double ShadowSize
    {
        get => GetValue(ShadowSizeProperty);
        set => SetValue(ShadowSizeProperty, value);
    }

    public Color AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    /// <summary>The colour the shadows start from at the edge: the accent's hue and saturation in the theme's tone.</summary>
    internal Color EdgeColor
    {
        get
        {
            var hsl = AccentColor.ToHsl();
            return new HslColor(ShadowTone.Alpha, hsl.H, hsl.S, ShadowTone.Lightness).ToRgb();
        }
    }

    internal Border Top => _top;
    internal Border Bottom => _bottom;
    internal Border Left => _left;
    internal Border Right => _right;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _viewer = TemplatedParent as ScrollViewer;
        if (_viewer is not null)
            _viewer.PropertyChanged += OnViewerPropertyChanged;

        // The first layout passes go through passing sizes; the shadows take their state at once
        // until layout has settled (no fades yet), and fade only for real scrolling after that.
        Update();
        Dispatcher.UIThread.Post(EnableFades, DispatcherPriority.Background);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        foreach (var edge in Edges)
            edge.ClearValue(Animatable.TransitionsProperty);
        if (_viewer is not null)
            _viewer.PropertyChanged -= OnViewerPropertyChanged;
        _viewer = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShadowToneProperty || change.Property == AccentColorProperty)
            UpdateBrushes();
        else if (change.Property == ShadowSizeProperty)
            UpdateSize();
    }

    private void OnViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.OffsetProperty || e.Property == ScrollViewer.ExtentProperty || e.Property == ScrollViewer.ViewportProperty)
            Update();
    }

    // Each edge shows while there is content out of view beyond it.
    private void Update()
    {
        if (_viewer is null)
            return;
        var (offset, extent, viewport) = (_viewer.Offset, _viewer.Extent, _viewer.Viewport);
        _top.Opacity = offset.Y > Tolerance ? 1 : 0;
        _bottom.Opacity = offset.Y + viewport.Height < extent.Height - Tolerance ? 1 : 0;
        _left.Opacity = offset.X > Tolerance ? 1 : 0;
        _right.Opacity = offset.X + viewport.Width < extent.Width - Tolerance ? 1 : 0;
    }

    private Border[] Edges => [_top, _bottom, _left, _right];

    private void EnableFades()
    {
        if (_viewer is null)
            return;
        foreach (var edge in Edges)
            edge.Bind(Animatable.TransitionsProperty, new DynamicResourceExtension(ButtonParts.FadeTransitionsKey));
    }

    private void UpdateSize()
    {
        _top.Height = _bottom.Height = ShadowSize;
        _left.Width = _right.Width = ShadowSize;
    }

    // From the shadow colour at the edge to the same colour fully transparent (no grey fringe).
    private void UpdateBrushes()
    {
        var edge = EdgeColor;
        var clear = Color.FromArgb(0, edge.R, edge.G, edge.B);
        IBrush Gradient(double x0, double y0, double x1, double y1) => new LinearGradientBrush
        {
            StartPoint = new RelativePoint(x0, y0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(x1, y1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(edge, 0), new GradientStop(clear, 1) },
        };
        _top.Background = Gradient(0, 0, 0, 1);
        _bottom.Background = Gradient(0, 1, 0, 0);
        _left.Background = Gradient(0, 0, 1, 0);
        _right.Background = Gradient(1, 0, 0, 0);
    }

    private static Border Edge(string name, VerticalAlignment vertical, HorizontalAlignment horizontal)
    {
        var edge = new Border
        {
            Name = name,
            VerticalAlignment = vertical,
            HorizontalAlignment = horizontal,
            Opacity = 0,
            IsHitTestVisible = false,
        };
        return edge;
    }
}
