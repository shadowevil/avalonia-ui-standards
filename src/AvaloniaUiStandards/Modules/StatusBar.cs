using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;

namespace AvaloniaUiStandards.Modules;

/// <summary>Where an item of a <see cref="StatusBar"/> sits.</summary>
public enum StatusZone
{
    /// <summary>From the bar's leading edge, after the message.</summary>
    Leading,

    /// <summary>Centred in the bar, moved aside only where the leading or trailing items need the room.</summary>
    Center,

    /// <summary>At the bar's trailing edge.</summary>
    Trailing,
}

/// <summary>
/// A status bar: a 24 px main row in three zones (<see cref="ZoneProperty"/>), for
/// <c>StandardsWindow.StatusBar</c> at the bottom of the window, with <c>SpacingL</c> at the sides, and any
/// <see cref="Rows"/> above it (each 24 px, no divider, nothing between). The <see cref="Message"/> leads the main row,
/// as the bar's one polite live region. Items are text (TextBlocks, in Caption size and secondary text), clickable
/// items (subtle Buttons and DropDownButtons, whose hover fill is 20 px tall, centred, with the full 24 px as the hit
/// target), icons with text, and <see cref="StatusProgress"/>; any other control can be hosted too.
/// </summary>
/// <remarks>
/// <para>
/// The main row (the bar's own <see cref="StatusBarRow.Items"/>) and each row fit on their own, as described at
/// <see cref="StatusBarRow"/>; the message never hides and is trimmed when nothing else is left to hide. The bar never
/// wraps and has no overflow menu.
/// </para>
/// <para>
/// Text items take no focus. Clickable items are Tab stops in visual order (the rows from the top, then the main row;
/// within a row leading, centre, trailing), after the rest of the window. The bar reports itself to automation as one
/// status bar named "Status" (the <c>StatusBarText</c> resource) unless the app sets a name; rows are not separate
/// elements.
/// </para>
/// </remarks>
public class StatusBar : StatusBarRow
{
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<StatusBar, string?>(nameof(Message));

    public static readonly AttachedProperty<StatusZone> ZoneProperty =
        AvaloniaProperty.RegisterAttached<StatusBar, Control, StatusZone>("Zone");

    public static readonly AttachedProperty<int> PriorityProperty =
        AvaloniaProperty.RegisterAttached<StatusBar, Control, int>("Priority");

    public static readonly AttachedProperty<IReadOnlyList<string>?> ShortTextsProperty =
        AvaloniaProperty.RegisterAttached<StatusBar, Control, IReadOnlyList<string>?>("ShortTexts");

    // True on the bar's clickable items (and a StatusProgress's cancel button), so the bar's styles
    // size them and inset their fill.
    internal static readonly AttachedProperty<bool> IsClickableItemProperty =
        AvaloniaProperty.RegisterAttached<StatusBar, Control, bool>("IsClickableItem");

    static StatusBar()
    {
        IsClickableItemProperty.Changed.AddClassHandler<Button>((button, e) =>
        {
            button.TemplateApplied -= OnClickableTemplateApplied;
            if (e.NewValue is true)
                button.TemplateApplied += OnClickableTemplateApplied;
            SetRingMargin(button, e.NewValue is true ? StatusBarTheme.RingMargin : new Thickness(-FocusRing.Offset));
        });
    }

    // The focus ring follows the inset fill; its margin is a local value in the template.
    private static void OnClickableTemplateApplied(object? sender, Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        if (e.NameScope.Find<Control>(FocusRing.Part) is { } ring)
            ring.Margin = StatusBarTheme.RingMargin;
    }

    private static void SetRingMargin(Button button, Thickness margin)
    {
        if (button.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == FocusRing.Part) is { } ring)
            ring.Margin = margin;
    }

    private readonly TextBlock _message;
    private readonly List<StatusBarRow> _rowViews = [];

    public StatusBar()
    {
        _message = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        AutomationProperties.SetLiveSetting(_message, AutomationLiveSetting.Polite);
        _message.Bind(TextBlock.TextProperty, this.GetObservable(MessageProperty));
        LogicalChildren.Add(_message);
        MessageBlockCore = _message;
        MessageSlot = new ToolbarBand.Slot(_message);
        VisualChildren.Add(MessageSlot);
        Rows.CollectionChanged += (_, _) => SyncRows();
    }

    /// <summary>The status message: the bar's leading text, announced politely when it changes.</summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// The rows above the main row, top to bottom. Each is 24 px high, with its own zones and priority hiding; hide a row
    /// with its <see cref="Visual.IsVisible"/>. The bar's own <see cref="StatusBarRow.Items"/> and <see cref="Message"/> are the bottom row.
    /// </summary>
    public AvaloniaList<StatusBarRow> Rows { get; } = [];

    public static StatusZone GetZone(Control item) => item.GetValue(ZoneProperty);

    /// <summary>Sets which zone <paramref name="item"/> sits in (default <see cref="StatusZone.Leading"/>).</summary>
    public static void SetZone(Control item, StatusZone value) => item.SetValue(ZoneProperty, value);

    public static int GetPriority(Control item) => item.GetValue(PriorityProperty);

    /// <summary>Sets the priority of <paramref name="item"/> (default 0): higher shortens, then hides, first.</summary>
    public static void SetPriority(Control item, int value) => item.SetValue(PriorityProperty, value);

    public static IReadOnlyList<string>? GetShortTexts(Control item) => item.GetValue(ShortTextsProperty);

    /// <summary>
    /// Sets the shorter forms of <paramref name="item"/>'s text, longest first, shown in turn when the bar is too narrow.
    /// The item's text is a TextBlock's text, a string content, or the first TextBlock in its content or in a panel there.
    /// </summary>
    public static void SetShortTexts(Control item, IReadOnlyList<string>? value) => item.SetValue(ShortTextsProperty, value);

    // The message part (for tests).
    internal TextBlock MessageBlock => _message;

    private protected override string? MessageValue => Message;

    private protected override double RowTop => VisibleRows().Count() * StandardsWindowTheme.StatusBarHeight;

    protected override AutomationPeer OnCreateAutomationPeer() => new ControlAutomationPeer(this);

    // --- Rows -------------------------------------------------------------------------------

    private IEnumerable<StatusBarRow> VisibleRows() => Rows.Where(row => row.IsVisible);

    // The rows come first in the visual order, which is the Tab order: top to bottom, then the main row.
    private void SyncRows()
    {
        foreach (var gone in _rowViews.Where(row => !Rows.Contains(row)).ToList())
        {
            _rowViews.Remove(gone);
            VisualChildren.Remove(gone);
            LogicalChildren.Remove(gone);
        }
        foreach (var added in Rows.Where(row => !_rowViews.Contains(row)))
        {
            _rowViews.Add(added);
            LogicalChildren.Add(added);
            VisualChildren.Add(added);
        }
        foreach (var row in Rows)
            VisualChildren.Remove(row);
        VisualChildren.InsertRange(0, Rows);
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var main = base.MeasureOverride(availableSize);
        var width = main.Width;
        foreach (var row in VisibleRows())
        {
            row.Measure(availableSize);
            width = Math.Max(width, row.DesiredSize.Width);
        }
        return new Size(width, main.Height + VisibleRows().Count() * StandardsWindowTheme.StatusBarHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var y = 0.0;
        foreach (var row in VisibleRows())
        {
            row.Arrange(new Rect(0, y, finalSize.Width, StandardsWindowTheme.StatusBarHeight));
            y += StandardsWindowTheme.StatusBarHeight;
        }
        return base.ArrangeOverride(finalSize);
    }
}
