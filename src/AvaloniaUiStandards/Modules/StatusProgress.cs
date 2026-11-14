using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>Which progress Block a <see cref="StatusProgress"/> shows.</summary>
public enum StatusProgressKind
{
    /// <summary>A 160 px ProgressBar.</summary>
    Bar,

    /// <summary>A 16 px ProgressRing.</summary>
    Ring,
}

/// <summary>
/// A <see cref="StatusBar"/> progress item: a ProgressBar (160 px) or a ProgressRing (16 px), its
/// text <c>SpacingS</c> after it, and an optional cancel button (a subtle 20 px icon button with
/// <c>Icons.X</c>, named by the <c>StatusBarCancelText</c> resource) <c>SpacingXS</c> after that.
/// <see cref="Value"/> is a percentage.
/// </summary>
public class StatusProgress : Control
{
    /// <summary>The bar's width and the ring's size.</summary>
    public const double BarWidth = 160, RingSize = 16;

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<StatusProgress, double>(nameof(Value));

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<StatusProgress, bool>(nameof(IsIndeterminate));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<StatusProgress, string?>(nameof(Text));

    public static readonly StyledProperty<StatusProgressKind> KindProperty =
        AvaloniaProperty.Register<StatusProgress, StatusProgressKind>(nameof(Kind));

    public static readonly StyledProperty<bool> IsCancelableProperty =
        AvaloniaProperty.Register<StatusProgress, bool>(nameof(IsCancelable));

    private readonly StackPanel _row;
    private readonly ProgressBar _bar;
    private readonly ProgressRing _ring;
    private readonly TextBlock _text;
    private readonly Button _cancel;

    public StatusProgress()
    {
        _bar = new ProgressBar { Width = BarWidth, MinWidth = 0, VerticalAlignment = VerticalAlignment.Center };
        _ring = new ProgressRing { Width = RingSize, Height = RingSize, VerticalAlignment = VerticalAlignment.Center, IsVisible = false };
        _bar.Bind(ProgressBar.ValueProperty, this.GetObservable(ValueProperty));
        _ring.Bind(ProgressRing.ValueProperty, this.GetObservable(ValueProperty));
        _bar.Bind(ProgressBar.IsIndeterminateProperty, this.GetObservable(IsIndeterminateProperty));
        _ring.Bind(ProgressRing.IsIndeterminateProperty, this.GetObservable(IsIndeterminateProperty));
        _text = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        _text.Bind(TextBlock.TextProperty, this.GetObservable(TextProperty));
        _text.Bind(IsVisibleProperty, this.GetObservable(TextProperty, t => !string.IsNullOrEmpty(t)));

        _cancel = new Button
        {
            Content = new Icon { Data = Icons.X, Width = StatusBarTheme.IconSize, Height = StatusBarTheme.IconSize },
            Width = StatusBarTheme.FillHeight,
            Padding = default,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            IsVisible = false,
        };
        _cancel.Classes.Add("subtle");
        _cancel.SetValue(StatusBar.IsClickableItemProperty, true);
        _cancel.Bind(AutomationProperties.NameProperty, new DynamicResourceExtension(StatusBarTheme.CancelTextKey));
        _cancel.Bind(ToolTip.TipProperty, new DynamicResourceExtension(StatusBarTheme.CancelTextKey));
        _cancel.Bind(IsVisibleProperty, this.GetObservable(IsCancelableProperty));
        _cancel.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);

        // The text is SpacingS after the progress; the cancel button's fill SpacingXS after the text.
        _text.Margin = new Thickness(Spacing.S, 0, 0, 0);
        _cancel.Margin = new Thickness(Spacing.XS, 0, 0, 0);
        _row = new StackPanel { Orientation = Orientation.Horizontal, Children = { _bar, _ring, _text, _cancel } };
        LogicalChildren.Add(_row);
        VisualChildren.Add(_row);
    }

    /// <summary>Raised when the cancel button is clicked.</summary>
    public event EventHandler? CancelRequested;

    /// <summary>The progress, from 0 to 100.</summary>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Whether the progress is indeterminate (an animated segment or arc, paced by the window's frame ticks).</summary>
    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>The text after the progress, for example "Exporting 42 %".</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Whether a ProgressBar or a ProgressRing shows the progress.</summary>
    public StatusProgressKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>Whether the cancel button shows.</summary>
    public bool IsCancelable
    {
        get => GetValue(IsCancelableProperty);
        set => SetValue(IsCancelableProperty, value);
    }

    // The parts (for tests).
    internal ProgressBar BarPart => _bar;

    internal ProgressRing RingPart => _ring;

    internal TextBlock TextPart => _text;

    internal Button CancelButton => _cancel;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == KindProperty)
        {
            _bar.IsVisible = Kind == StatusProgressKind.Bar;
            _ring.IsVisible = Kind == StatusProgressKind.Ring;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _row.Measure(availableSize);
        return _row.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _row.Arrange(new Rect(finalSize));
        return finalSize;
    }
}
