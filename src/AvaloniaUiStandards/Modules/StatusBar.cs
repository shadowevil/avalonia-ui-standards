using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

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
/// A status bar: one 24 px row in three zones (<see cref="ZoneProperty"/>), for
/// <c>StandardsWindow.StatusBar</c> at the bottom of the window, with <c>SpacingL</c> at the sides.
/// The <see cref="Message"/> leads, as a polite live region. Items are text (TextBlocks, in
/// Caption size and secondary text), clickable items (subtle Buttons and DropDownButtons, whose
/// hover fill is 20 px tall, centred, with the full 24 px as the hit target), icons with text, and
/// <see cref="StatusProgress"/>; any other control can be hosted too.
/// </summary>
/// <remarks>
/// <para>
/// Text items are <c>SpacingL</c> (16) apart and clickable items <c>SpacingXS</c> (4) apart. A
/// clickable item's text is inset 8 px within its fill, and the fill reaches 8 px past its text, so
/// text lines up at 16 px from the edge and with a text item's text beside it, whichever the kind.
/// </para>
/// <para>
/// The bar never wraps. When the items do not fit, first every item takes its shorter texts
/// (<see cref="ShortTextsProperty"/>, longest first), highest <see cref="PriorityProperty"/> first and,
/// among equal priorities, from the trailing end; then items hide in the same order. The message
/// never hides; it is trimmed when nothing else is left to hide. There is no overflow menu.
/// </para>
/// <para>
/// Text items take no focus. Clickable items are Tab stops in visual order (leading, centre,
/// trailing), after the rest of the window. The bar reports itself to automation as a status bar
/// named "Status" (the <c>StatusBarText</c> resource) unless the app sets a name.
/// </para>
/// </remarks>
public class StatusBar : Control
{
    // A clickable item's text inset within its fill.
    internal const double ClickableInset = Spacing.S;

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
    private readonly ToolbarBand.Slot _messageSlot;
    private readonly Dictionary<Control, ToolbarBand.Slot> _slots = [];
    private readonly Dictionary<Control, IDisposable> _shortened = [];
    private State _applied = State.Empty;
    private State? _planned;

    public StatusBar()
    {
        Items.CollectionChanged += (_, _) => SyncItems();
        _message = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        AutomationProperties.SetLiveSetting(_message, AutomationLiveSetting.Polite);
        _message.Bind(TextBlock.TextProperty, this.GetObservable(MessageProperty));
        LogicalChildren.Add(_message);
        _messageSlot = new ToolbarBand.Slot(_message);
        VisualChildren.Add(_messageSlot);
    }

    /// <summary>The status message: the bar's leading text, announced politely when it changes.</summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>The items, in order within each zone.</summary>
    [Content]
    public Avalonia.Controls.Controls Items { get; } = [];

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

    // The message part, and whether an item is shown (for tests).
    internal TextBlock MessageBlock => _message;

    internal bool IsShown(Control item) => _slots.TryGetValue(item, out var slot) && slot.IsVisible && item.IsVisible;

    protected override AutomationPeer OnCreateAutomationPeer() => new ControlAutomationPeer(this);

    // --- Items ------------------------------------------------------------------------------

    private void SyncItems()
    {
        foreach (var gone in _slots.Keys.Where(item => !Items.Contains(item)).ToList())
        {
            var slot = _slots[gone];
            _slots.Remove(gone);
            gone.PropertyChanged -= OnItemPropertyChanged;
            Restore(gone);
            VisualChildren.Remove(slot);
            slot.Release();
            LogicalChildren.Remove(gone);
            gone.ClearValue(IsClickableItemProperty);
        }
        foreach (var item in Items.Where(item => !_slots.ContainsKey(item)))
        {
            LogicalChildren.Add(item);
            var slot = new ToolbarBand.Slot(item);
            _slots[item] = slot;
            item.PropertyChanged += OnItemPropertyChanged;
            if (item is Button)
                item.SetValue(IsClickableItemProperty, true);
        }
        // Visual order is Tab order: the message, then leading, centre and trailing items.
        var order = Ordered().Select(item => (Visual)_slots[item]).Prepend(_messageSlot).ToList();
        foreach (var slot in order)
            VisualChildren.Remove(slot);
        VisualChildren.AddRange(order);
        _applied = State.Empty;
        InvalidateMeasure();
    }

    private void OnItemPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ZoneProperty)
            SyncItems();
        else if (e.Property == PriorityProperty || e.Property == ShortTextsProperty || e.Property == IsVisibleProperty)
            InvalidateMeasure();
    }

    private IEnumerable<Control> Ordered() => Items.Select((item, index) => (item, index))
        .OrderBy(x => GetZone(x.item)).ThenBy(x => x.index).Select(x => x.item);

    // --- Layout -----------------------------------------------------------------------------

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = 0.0;
        _messageSlot.Measure(Size.Infinity);
        foreach (var slot in _slots.Values)
        {
            // A hidden slot is not measured by layout; its item still is, for its width.
            if (slot.IsVisible)
                slot.Measure(Size.Infinity);
            else
                slot.Item.Measure(Size.Infinity);
            height = Math.Max(height, slot.Item.DesiredSize.Height);
        }
        height = Math.Max(height, _message.DesiredSize.Height);
        var natural = Fit.Required(Entries(), new Plan([], []));
        return new Size(double.IsInfinity(availableSize.Width) ? natural : Math.Min(natural, availableSize.Width), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var entries = Entries();
        var plan = Fit.Plan(entries, finalSize.Width);
        var placed = Fit.Place(entries, plan, finalSize.Width);
        foreach (var (entry, x, width) in placed)
        {
            var size = entry.Slot.Item.DesiredSize;
            entry.Slot.Arrange(new Rect(x, Math.Max(0, (finalSize.Height - size.Height) / 2), width, size.Height));
        }
        foreach (var entry in entries.Where(e => plan.Hidden.Contains(e.Item)))
            entry.Slot.Arrange(default);
        if (string.IsNullOrEmpty(Message))
            _messageSlot.Arrange(default);

        var state = State.Of(plan);
        if (!state.Same(_applied))
        {
            var post = _planned is null;
            _planned = state;
            if (post)
                Dispatcher.UIThread.Post(ApplyPlan);
        }
        else
        {
            _planned = null;
        }
        return finalSize;
    }

    // Applied after layout, so layout never loops: hidden items leave the Tab order, and items show their chosen text.
    private void ApplyPlan()
    {
        if (_planned is not { } state)
            return;
        _planned = null;
        foreach (var (item, slot) in _slots)
        {
            slot.IsVisible = !state.Hidden.Contains(item);
            Restore(item);
            if (state.Texts.TryGetValue(item, out var form) && TextOf(item) is { } text
                && text.Target.SetValue(text.Property, form, BindingPriority.Animation) is { } undo)
                _shortened[item] = undo;
        }
        _applied = state;
    }

    // What the bar shows: the hidden items and the shorter text each shortened item shows.
    private sealed record State(HashSet<Control> Hidden, Dictionary<Control, string> Texts)
    {
        public static readonly State Empty = new([], []);

        public static State Of(Plan plan) => new(plan.Hidden, plan.Levels
            .Where(l => l.Value > 0 && GetShortTexts(l.Key) is { } forms && l.Value <= forms.Count)
            .ToDictionary(l => l.Key, l => GetShortTexts(l.Key)![l.Value - 1]));

        public bool Same(State other) => Hidden.SetEquals(other.Hidden) && Texts.Count == other.Texts.Count
            && Texts.All(t => other.Texts.TryGetValue(t.Key, out var o) && o == t.Value);
    }

    private void Restore(Control item)
    {
        if (_shortened.Remove(item, out var shortened))
            shortened.Dispose();
    }

    private List<Fit.Entry> Entries()
    {
        var entries = new List<Fit.Entry>();
        if (!string.IsNullOrEmpty(Message))
            entries.Add(new Fit.Entry(_message, _messageSlot, StatusZone.Leading, [_message.DesiredSize.Width], int.MinValue, 0, IsMessage: true));
        foreach (var item in Ordered().Where(i => i.IsVisible))
            entries.Add(new Fit.Entry(item, _slots[item], GetZone(item), Widths(item), GetPriority(item),
                item is Button ? ClickableInset : 0, IsMessage: false));
        return entries;
    }

    // The item's width with its full text and with each shorter form: its desired width with the text it shows now,
    // less that text's width, plus each form's.
    private double[] Widths(Control item)
    {
        var desired = item.DesiredSize.Width;
        if (GetShortTexts(item) is not { Count: > 0 } forms || TextOf(item) is not { } text)
            return [desired];
        var shown = TextWidth(text, text.Target.GetValue(text.Property) as string);
        var full = TextWidth(text, FullText(text));
        return [desired - shown + full, .. forms.Select(f => desired - shown + TextWidth(text, f))];
    }

    internal readonly record struct TextTarget(AvaloniaObject Target, AvaloniaProperty Property, Control FontSource);

    // Where an item's text is: a TextBlock's text, a string content, or the first TextBlock in its content or in a panel there.
    internal static TextTarget? TextOf(Control item) => item switch
    {
        TextBlock block => new(block, TextBlock.TextProperty, block),
        ContentControl { Content: string } control => new(control, ContentControl.ContentProperty, control),
        ContentControl { Content: TextBlock block } => new(block, TextBlock.TextProperty, block),
        ContentControl { Content: Panel panel } when panel.Children.OfType<TextBlock>().FirstOrDefault() is { } block => new(block, TextBlock.TextProperty, block),
        Panel panel when panel.Children.OfType<TextBlock>().FirstOrDefault() is { } block => new(block, TextBlock.TextProperty, block),
        _ => null,
    };

    private static string? FullText(TextTarget text) => text.Target switch
    {
        TextBlock block => block.GetBaseValue(TextBlock.TextProperty).GetValueOrDefault(),
        ContentControl control => control.GetBaseValue(ContentControl.ContentProperty).GetValueOrDefault() as string,
        _ => null,
    };

    private static double TextWidth(TextTarget text, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;
        var source = text.FontSource;
        var typeface = new Typeface(TextElement.GetFontFamily(source), TextElement.GetFontStyle(source),
            TextElement.GetFontWeight(source), TextElement.GetFontStretch(source));
        using var layout = new TextLayout(value, typeface, TextElement.GetFontSize(source), null);
        return Math.Ceiling(layout.WidthIncludingTrailingWhitespace);
    }

    internal sealed record Plan(HashSet<Control> Hidden, Dictionary<Control, int> Levels);

    internal static class Fit
    {
        internal sealed record Entry(Control Item, ToolbarBand.Slot Slot, StatusZone Zone, double[] Widths, int Priority, double Inset, bool IsMessage);

        // Shorten, then hide: highest priority first, ties from the trailing end. The message is never a candidate.
        public static Plan Plan(IReadOnlyList<Entry> entries, double width)
        {
            var plan = new Plan([], []);
            var candidates = entries.Select((e, i) => (e, i)).Where(x => !x.e.IsMessage)
                .OrderByDescending(x => x.e.Priority).ThenByDescending(x => x.i).Select(x => x.e).ToList();
            foreach (var entry in candidates)
                while (Required(entries, plan) > width + 0.01 && plan.Levels.GetValueOrDefault(entry.Item) < entry.Widths.Length - 1)
                    plan.Levels[entry.Item] = plan.Levels.GetValueOrDefault(entry.Item) + 1;
            foreach (var entry in candidates)
                if (Required(entries, plan) > width + 0.01)
                    plan.Hidden.Add(entry.Item);
            return plan;
        }

        public static double Width(Entry entry, Plan plan) => entry.Widths[plan.Levels.GetValueOrDefault(entry.Item)];

        // The bar's width for the shown entries: the side padding (less the end items' insets), the items and the gaps.
        public static double Required(IReadOnlyList<Entry> entries, Plan plan)
        {
            var shown = entries.Where(e => !plan.Hidden.Contains(e.Item)).ToList();
            if (shown.Count == 0)
                return 0;
            var total = 2 * Spacing.L - shown[0].Inset - shown[^1].Inset;
            for (var i = 0; i < shown.Count; i++)
                total += Width(shown[i], plan) + (i > 0 ? Gap(shown[i - 1], shown[i]) : 0);
            return total;
        }

        // Text items 16 apart (text to text), clickable items 4 apart (fill to fill); a text item and a clickable one
        // 8 apart, so their texts are 16 apart.
        public static double Gap(Entry a, Entry b) =>
            a.Inset > 0 && b.Inset > 0 ? Spacing.XS : Spacing.L - a.Inset - b.Inset;

        public static List<(Entry Entry, double X, double Width)> Place(IReadOnlyList<Entry> entries, Plan plan, double width)
        {
            var shown = entries.Where(e => !plan.Hidden.Contains(e.Item)).ToList();
            var placed = new List<(Entry, double, double)>();
            if (shown.Count == 0)
                return placed;
            var lead = shown.Where(e => e.Zone == StatusZone.Leading).ToList();
            var centre = shown.Where(e => e.Zone == StatusZone.Center).ToList();
            var trail = shown.Where(e => e.Zone == StatusZone.Trailing).ToList();

            // The message gives up what is still missing once nothing else can go.
            var excess = Math.Max(0, Required(entries, plan) - width);
            double W(Entry e) => e.IsMessage ? Math.Max(0, Width(e, plan) - excess) : Width(e, plan);
            double Span(List<Entry> zone) => zone.Select((e, i) => W(e) + (i > 0 ? Gap(zone[i - 1], e) : 0)).Sum();

            var x = lead.Count > 0 ? Spacing.L - lead[0].Inset : 0;
            for (var i = 0; i < lead.Count; i++)
            {
                if (i > 0)
                    x += Gap(lead[i - 1], lead[i]);
                placed.Add((lead[i], x, W(lead[i])));
                x += W(lead[i]);
            }
            var leadEnd = x;

            var trailStart = trail.Count > 0 ? width - Spacing.L + trail[^1].Inset - Span(trail) : width;
            var right = trailStart;
            for (var i = 0; i < trail.Count; i++)
            {
                placed.Add((trail[i], right, W(trail[i])));
                right += W(trail[i]) + (i + 1 < trail.Count ? Gap(trail[i], trail[i + 1]) : 0);
            }

            if (centre.Count > 0)
            {
                var span = Span(centre);
                var lo = lead.Count > 0 ? leadEnd + Gap(lead[^1], centre[0]) : Spacing.L - centre[0].Inset;
                var hi = (trail.Count > 0 ? trailStart - Gap(centre[^1], trail[0]) : width - Spacing.L + centre[^1].Inset) - span;
                var cx = Math.Clamp((width - span) / 2, lo, Math.Max(lo, hi));
                for (var i = 0; i < centre.Count; i++)
                {
                    if (i > 0)
                        cx += Gap(centre[i - 1], centre[i]);
                    placed.Add((centre[i], cx, W(centre[i])));
                    cx += W(centre[i]);
                }
            }
            return placed;
        }
    }
}
