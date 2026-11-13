using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards;

namespace Catalog;

/// <summary>The live theme controls: each one sets a property of the application's <see cref="StandardsTheme"/>.</summary>
public partial class ThemeToolbar : UserControl
{
    private static readonly (string Name, string Hex)[] Accents =
    [
        ("Default", Hex(StandardsTheme.DefaultAccent)),
        ("Teal", "#00899E"),
        ("Green", "#2E8B3E"),
        ("Orange", "#E0720B"),
        ("Purple", "#7A4FD6"),
        ("Pink", "#E91E63"),
    ];

    private readonly StandardsTheme? _theme = Application.Current?.Styles.OfType<StandardsTheme>().FirstOrDefault();
    private bool _syncing;
    private bool _collapsed;
    private bool _updatePending;
    private double _available = double.NaN;

    public ThemeToolbar()
    {
        InitializeComponent();
        foreach (var caption in Captions)
        {
            caption.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            caption.Bind(TextBlock.FontSizeProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("FontSizeCaption"));
        }
        AccentBox.ItemsSource = Accents.Select(a => a.Name).ToList();
        AccentBox.SelectedIndex = 0;
        AccentHex.Text = Accents[0].Hex;

        ThemeBox.SelectionChanged += (_, _) => Application.Current!.RequestedThemeVariant =
            ThemeBox.SelectedIndex switch { 1 => ThemeVariant.Light, 2 => ThemeVariant.Dark, _ => ThemeVariant.Default };
        DensityBox.SelectionChanged += (_, _) =>
        {
            if (_theme is not null)
                _theme.Density = DensityBox.SelectedIndex == 1 ? DensityStyle.Compact : DensityStyle.Normal;
        };
        MotionBox.SelectionChanged += (_, _) =>
        {
            if (_theme is not null)
                _theme.ReduceMotion = Tri(MotionBox.SelectedIndex);
        };
        TransparencyBox.SelectionChanged += (_, _) =>
        {
            if (_theme is not null)
                _theme.ReduceTransparency = Tri(TransparencyBox.SelectedIndex);
        };
        AccentBox.SelectionChanged += (_, _) =>
        {
            if (!_syncing && AccentBox.SelectedIndex >= 0)
                AccentHex.Text = Accents[AccentBox.SelectedIndex].Hex;
        };
        AccentHex.TextChanged += (_, _) => ApplyHex();
    }

    private static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private TextBlock[] Captions => [ThemeCaption, DensityCaption, AccentCaption, MotionCaption, TransparencyCaption];

    private StackPanel[] Groups => [ThemeGroup, DensityGroup, AccentGroup, MotionGroup, TransparencyGroup];

    /// <summary>
    /// One row of compact controls while it fits the space it is given; otherwise a single Settings
    /// button whose flyout holds the same controls, one per line, with captions. Measuring only
    /// records the space; the controls move in Update, outside layout.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (availableSize.Width != _available && !_updatePending)
        {
            _updatePending = true;
            Dispatcher.UIThread.Post(Update);
        }

        _available = availableSize.Width;
        return base.MeasureOverride(availableSize);
    }

    private void Update()
    {
        _updatePending = false;

        // The row's natural width is measured afresh each time, in row form, so a density or text
        // change while collapsed cannot leave it stale.
        SetCollapsed(false);
        Row.Measure(Size.Infinity);
        var rowWidth = Row.DesiredSize.Width;
        var collapse = _available < rowWidth;
        SetCollapsed(collapse);

        // An open flyout stays open while the toolbar stays collapsed.
        if (!collapse)
            MoreButton.Flyout?.Hide();
    }

    private void SetCollapsed(bool collapsed)
    {
        if (collapsed == _collapsed)
            return;
        _collapsed = collapsed;
        var target = collapsed ? FlyoutBody.Children : Row.Children;
        foreach (var group in Groups)
        {
            ((Panel)group.Parent!).Children.Remove(group);
            target.Add(group);
        }

        // Captions of self-explanatory controls show only in the flyout, where there is no row context.
        foreach (var caption in new[] { ThemeCaption, DensityCaption, AccentCaption })
            caption.IsVisible = collapsed;
        foreach (var caption in Captions)
            caption.Width = collapsed ? 110 : double.NaN;
        MoreButton.IsVisible = collapsed;
        Row.IsVisible = !collapsed;
    }

    // Follow OS (null), Reduced (true), Full (false).
    private static bool? Tri(int index) => index switch { 1 => true, 2 => false, _ => null };

    private void ApplyHex()
    {
        var text = AccentHex.Text?.Trim() ?? "";
        var valid = text.StartsWith('#') && text.Length is 7 && Color.TryParse(text, out _);
        if (!valid && Application.Current!.FindResource("DangerColor") is Color danger)
            AccentHex.Foreground = new SolidColorBrush(danger);
        else
            AccentHex.ClearValue(TemplatedControl.ForegroundProperty);
        if (!valid || _theme is null)
            return;

        _theme.Accent = Color.Parse(text);
        _syncing = true;
        AccentBox.SelectedIndex = Array.FindIndex(Accents, a => a.Hex.Equals(text, StringComparison.OrdinalIgnoreCase));
        _syncing = false;
    }
}
