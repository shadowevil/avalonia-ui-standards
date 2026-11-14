using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// One 24 px row of a <see cref="StatusBar"/>: items in three zones (<see cref="StatusBar.ZoneProperty"/>), each row
/// shortening and hiding its own items by priority. A <see cref="StatusBar"/> is itself the bottom (main) row, with the
/// message; rows added to <see cref="StatusBar.Rows"/> sit above it. A row has no message and is not a live region.
/// </summary>
/// <remarks>
/// <para>
/// Text items are <c>SpacingL</c> (16) apart and clickable items <c>SpacingXS</c> (4) apart. A
/// clickable item's text is inset 8 px within its fill, and the fill reaches 8 px past its text, so
/// text lines up at 16 px from the edge and with a text item's text beside it, whichever the kind.
/// </para>
/// <para>
/// A row never wraps. When its items do not fit, first every item takes its shorter texts
/// (<see cref="StatusBar.ShortTextsProperty"/>, longest first), highest <see cref="StatusBar.PriorityProperty"/> first and,
/// among equal priorities, from the trailing end; then items hide in the same order, and the room a
/// hidden item leaves goes back to the shortened items still shown, the last shortened first. The main row's message
/// never hides; it is trimmed when nothing else is left to hide. There is no overflow menu.
/// </para>
/// </remarks>
public class StatusBarRow : Control
{
    // A clickable item's text inset within its fill.
    internal const double ClickableInset = Spacing.S;

    private protected TextBlock? MessageBlockCore;
    private protected ToolbarBand.Slot? MessageSlot;
    private readonly Dictionary<Control, ToolbarBand.Slot> _slots = [];
    private readonly Dictionary<Control, Shortened> _shortened = [];
    private bool _showingForm;
    private State _applied = State.Empty;
    private State? _planned;

    public StatusBarRow()
    {
        Items.CollectionChanged += (_, _) => SyncItems();
    }

    /// <summary>The items, in order within each zone.</summary>
    [Content]
    public Avalonia.Controls.Controls Items { get; } = [];

    // The main row's message text; null in a row without one. Where the row's content starts (a bar's main row sits
    // below its other rows).
    private protected virtual string? MessageValue => null;

    private protected virtual double RowTop => 0;

    // Whether an item is shown (for tests).
    internal bool IsShown(Control item) => _slots.TryGetValue(item, out var slot) && slot.IsVisible && item.IsVisible;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty)
            (this.GetLogicalParent() as StatusBar)?.InvalidateMeasure();
    }

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
            gone.ClearValue(StatusBar.IsClickableItemProperty);
        }
        foreach (var item in Items.Where(item => !_slots.ContainsKey(item)))
        {
            LogicalChildren.Add(item);
            var slot = new ToolbarBand.Slot(item);
            _slots[item] = slot;
            item.PropertyChanged += OnItemPropertyChanged;
            if (item is Button)
                item.SetValue(StatusBar.IsClickableItemProperty, true);
        }
        // Visual order is Tab order: the message, then leading, centre and trailing items.
        var order = Ordered().Select(item => (Visual)_slots[item]).ToList();
        if (MessageSlot is not null)
            order.Insert(0, MessageSlot);
        foreach (var slot in order)
            VisualChildren.Remove(slot);
        VisualChildren.AddRange(order);
        _applied = State.Empty;
        InvalidateMeasure();
    }

    private void OnItemPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == StatusBar.ZoneProperty)
            SyncItems();
        else if (e.Property == StatusBar.PriorityProperty || e.Property == StatusBar.ShortTextsProperty || e.Property == IsVisibleProperty)
            InvalidateMeasure();
    }

    private IEnumerable<Control> Ordered() => Items.Select((item, index) => (item, index))
        .OrderBy(x => StatusBar.GetZone(x.item)).ThenBy(x => x.index).Select(x => x.item);

    // --- Layout -----------------------------------------------------------------------------

    protected override Size MeasureOverride(Size availableSize)
    {
        MessageSlot?.Measure(Size.Infinity);
        foreach (var slot in _slots.Values)
        {
            // A hidden slot is not measured by layout; its item still is, for its width.
            if (slot.IsVisible)
                slot.Measure(Size.Infinity);
            else
                slot.Item.Measure(Size.Infinity);
        }
        var natural = Fit.Required(Entries(), new Plan([], []));
        return new Size(double.IsInfinity(availableSize.Width) ? natural : Math.Min(natural, availableSize.Width), StandardsWindowTheme.StatusBarHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var entries = Entries();
        var plan = Fit.Plan(entries, finalSize.Width);
        var placed = Fit.Place(entries, plan, finalSize.Width);
        foreach (var (entry, x, width) in placed)
        {
            var size = entry.Slot.Item.DesiredSize;
            entry.Slot.Arrange(new Rect(x, RowTop + Math.Max(0, (finalSize.Height - RowTop - size.Height) / 2), width, size.Height));
        }
        foreach (var entry in entries.Where(e => plan.Hidden.Contains(e.Item)))
            entry.Slot.Arrange(default);
        if (string.IsNullOrEmpty(MessageValue))
            MessageSlot?.Arrange(default);

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
            Show(item, state.Texts.GetValueOrDefault(item));
        }
        _applied = state;
    }

    // An item showing a shorter form (Level, from 1, in its ShortTexts), and the app's own text, shown again when
    // there is room.
    private sealed class Shortened(TextTarget text, string? full)
    {
        public TextTarget Text { get; } = text;
        public string? Full { get; set; } = full;
        public string? Form { get; set; }
        public int Level { get; set; }
    }

    // Shows a shorter form, or the app's own text when null. The form is set as the current value, which keeps the
    // app's bindings; a text the app sets becomes the full text (see OnShortenedTextChanged).
    private void Show(Control item, string? form)
    {
        _shortened.TryGetValue(item, out var shortened);
        if (shortened?.Form == form)
            return;
        if (form is null)
        {
            Restore(item);
            return;
        }
        if (shortened is null)
        {
            if (TextOf(item) is not { } text)
                return;
            shortened = new Shortened(text, text.Target.GetValue(text.Property) as string);
            _shortened[item] = shortened;
            text.Target.PropertyChanged += OnShortenedTextChanged;
        }
        shortened.Form = form;
        shortened.Level = (StatusBar.GetShortTexts(item)?.ToList().IndexOf(form) ?? -1) + 1;
        SetText(shortened.Text, form);
    }

    private void SetText(TextTarget text, string? value)
    {
        _showingForm = true;
        try
        {
            text.Target.SetCurrentValue(text.Property, value);
        }
        finally
        {
            _showingForm = false;
        }
    }

    // The app set a new text where a shorter form shows: it is the item's full text now. The form at the same level
    // of the item's current ShortTexts shows again at once (this runs outside layout), so the full text never shows
    // in the short slot; the bar measures again, and a full text that fits returns at the next layout.
    private void OnShortenedTextChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_showingForm)
            return;
        var (item, shortened) = _shortened.FirstOrDefault(s => s.Value.Text.Target == sender && s.Value.Text.Property == e.Property);
        if (item is null)
            return;
        shortened.Full = e.NewValue as string;
        if (StatusBar.GetShortTexts(item) is { Count: > 0 } forms)
        {
            shortened.Level = Math.Clamp(shortened.Level, 1, forms.Count);
            shortened.Form = forms[shortened.Level - 1];
            _applied = _applied.WithText(item, shortened.Form);
            SetText(shortened.Text, shortened.Form);
        }
        else
        {
            shortened.Text.Target.PropertyChanged -= OnShortenedTextChanged;
            _shortened.Remove(item);
            _applied = _applied.WithText(item, null);
        }
        InvalidateMeasure();
    }

    // What the bar shows: the hidden items and the shorter text each shortened item shows. A State is never changed
    // in place (Empty is shared by every bar); a change makes a new one.
    internal sealed record State(HashSet<Control> Hidden, Dictionary<Control, string> Texts)
    {
        public static readonly State Empty = new([], []);

        // This state with item showing form, or its full text when form is null.
        public State WithText(Control item, string? form)
        {
            var texts = new Dictionary<Control, string>(Texts);
            if (form is null)
                texts.Remove(item);
            else
                texts[item] = form;
            return this with { Texts = texts };
        }

        public static State Of(Plan plan) => new(plan.Hidden, plan.Levels
            .Where(l => l.Value > 0 && StatusBar.GetShortTexts(l.Key) is { } forms && l.Value <= forms.Count)
            .ToDictionary(l => l.Key, l => StatusBar.GetShortTexts(l.Key)![l.Value - 1]));

        public bool Same(State other) => Hidden.SetEquals(other.Hidden) && Texts.Count == other.Texts.Count
            && Texts.All(t => other.Texts.TryGetValue(t.Key, out var o) && o == t.Value);
    }

    private void Restore(Control item)
    {
        if (!_shortened.Remove(item, out var shortened))
            return;
        shortened.Text.Target.PropertyChanged -= OnShortenedTextChanged;
        SetText(shortened.Text, shortened.Full);
    }

    private List<Fit.Entry> Entries()
    {
        var entries = new List<Fit.Entry>();
        if (MessageBlockCore is { } message && MessageSlot is { } messageSlot && !string.IsNullOrEmpty(MessageValue))
            entries.Add(new Fit.Entry(message, messageSlot, StatusZone.Leading, [message.DesiredSize.Width], int.MinValue, 0, IsMessage: true));
        foreach (var item in Ordered().Where(i => i.IsVisible))
            entries.Add(new Fit.Entry(item, _slots[item], StatusBar.GetZone(item), Widths(item), StatusBar.GetPriority(item),
                item is Button ? ClickableInset : 0, IsMessage: false));
        return entries;
    }

    // The item's width with its full text and with each shorter form: its desired width with the text it shows now,
    // less that text's width, plus each form's.
    private double[] Widths(Control item)
    {
        var desired = item.DesiredSize.Width;
        if (StatusBar.GetShortTexts(item) is not { Count: > 0 } forms || TextOf(item) is not { } text)
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

    // The app's own text: kept by the bar while a shorter form shows.
    private string? FullText(TextTarget text) =>
        _shortened.Values.FirstOrDefault(s => s.Text.Target == text.Target)?.Full ?? text.Target.GetValue(text.Property) as string;

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
        // Then the room a hidden item freed goes back to the shortened items still shown, in reverse order (the
        // last to shorten lengthens first), a step at a time while the bar still fits.
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
            if (plan.Hidden.Count == 0)
                return plan;
            foreach (var entry in Enumerable.Reverse(candidates).Where(e => !plan.Hidden.Contains(e.Item)))
                while (plan.Levels.GetValueOrDefault(entry.Item) is > 0 and var level)
                {
                    plan.Levels[entry.Item] = level - 1;
                    if (Required(entries, plan) <= width + 0.01)
                        continue;
                    plan.Levels[entry.Item] = level;
                    break;
                }
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
