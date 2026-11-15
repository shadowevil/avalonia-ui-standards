using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The top of a <see cref="StandardsWindow"/>: the hiding rows (the title band and the menu row) above the toolbar band.
/// Windowed, it stacks them. In full screen it takes only the toolbar band's height: the rows sit above the window's top
/// edge and the band at the top. <see cref="Reveal"/> (0 to 1) slides the rows down over the content, the band moving
/// down below them, on the glass <see cref="Sheet"/>; the layout below never moves, since only the arrangement changes.
/// </summary>
internal sealed class WindowTop : Panel
{
    public static readonly StyledProperty<bool> IsFullScreenProperty =
        AvaloniaProperty.Register<WindowTop, bool>(nameof(IsFullScreen));

    public static readonly StyledProperty<double> RevealProperty =
        AvaloniaProperty.Register<WindowTop, double>(nameof(Reveal));

    public static readonly StyledProperty<double> TopInsetProperty =
        AvaloniaProperty.Register<WindowTop, double>(nameof(TopInset));

    static WindowTop()
    {
        AffectsMeasure<WindowTop>(IsFullScreenProperty);
        AffectsArrange<WindowTop>(RevealProperty, TopInsetProperty);
    }

    public WindowTop(Control rows, Control band, Control sheet)
    {
        Rows = rows;
        Band = band;
        Sheet = sheet;
        sheet.IsVisible = false;
        Children.Add(sheet);
        Children.Add(rows);
        Children.Add(band);
    }

    /// <summary>The rows that hide in full screen: the title band and the menu row.</summary>
    public Control Rows { get; }

    /// <summary>The toolbar band slot, which stays at the top in full screen.</summary>
    public Control Band { get; }

    /// <summary>The glass behind the revealed rows and band, shown only while they are revealed in full screen.</summary>
    public Control Sheet { get; }

    public bool IsFullScreen
    {
        get => GetValue(IsFullScreenProperty);
        set => SetValue(IsFullScreenProperty, value);
    }

    /// <summary>How far the rows are revealed in full screen: 0 hidden above the top edge, 1 fully in.</summary>
    public double Reveal
    {
        get => GetValue(RevealProperty);
        set => SetValue(RevealProperty, value);
    }

    /// <summary>Room left above the revealed rows (the macOS menu bar where it overlaps the window).</summary>
    public double TopInset
    {
        get => GetValue(TopInsetProperty);
        set => SetValue(TopInsetProperty, value);
    }

    /// <summary>The bottom of the revealed rows and band, in this panel's coordinates.</summary>
    public double RevealedBottom => TopInset + Rows.DesiredSize.Height + Band.DesiredSize.Height;

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size(availableSize.Width, double.PositiveInfinity);
        Rows.Measure(size);
        Band.Measure(size);
        Sheet.Measure(size);
        var width = Math.Max(Rows.DesiredSize.Width, Band.DesiredSize.Width);
        return new Size(width, IsFullScreen ? Band.DesiredSize.Height : Rows.DesiredSize.Height + Band.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rows = Rows.DesiredSize.Height;
        var band = Band.DesiredSize.Height;
        var reveal = IsFullScreen ? Math.Clamp(Reveal, 0, 1) : 1;
        var inset = IsFullScreen ? TopInset * reveal : 0;
        // Hidden, the rows end at the top edge; revealed, they start below the inset.
        var top = IsFullScreen ? -rows * (1 - reveal) + inset : 0;
        Rows.Arrange(new Rect(0, top, finalSize.Width, rows));
        Band.Arrange(new Rect(0, top + rows, finalSize.Width, band));
        Sheet.Arrange(new Rect(0, top - inset, finalSize.Width, inset + rows + band));
        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RevealProperty || change.Property == IsFullScreenProperty)
            Sheet.IsVisible = IsFullScreen && Reveal > 0;
    }
}
