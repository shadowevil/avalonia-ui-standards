using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>Where an item of a <see cref="ToolbarBand"/> sits.</summary>
public enum ToolbarZone
{
    /// <summary>From the band's leading edge.</summary>
    Leading,

    /// <summary>Centred in the band, moved aside only where the leading or trailing items need the room.</summary>
    Center,

    /// <summary>At the band's trailing edge, before the overflow button.</summary>
    Trailing,
}

/// <summary>
/// Flexible space in a <see cref="ToolbarBand"/>: it takes the free room on its side of the band,
/// so the items after it in a leading zone (or before it in a trailing zone) move towards the
/// centre. It has no width of its own and never shows in the overflow menu.
/// </summary>
public class ToolbarSpacer : Control
{
}

/// <summary>
/// A toolbar band: one row of commands and settings in three zones (<see cref="ZoneProperty"/>),
/// 48 high (40 at Compact density) with <c>SpacingL</c> at the sides, for
/// <c>StandardsWindow.ToolbarBand</c> directly below the title band. Items are icon buttons
/// (subtle Buttons with an <see cref="Icon"/>, square), subtle ToggleButtons, SegmentedControls,
/// subtle DropDownButtons with a menu, vertical Separators and <see cref="ToolbarSpacer"/>s; any
/// other control can be hosted too. Items are <c>SpacingS</c> apart within a group and
/// <c>SpacingL</c> apart between groups (<see cref="BeginsGroupProperty"/>) and between zones.
/// </summary>
/// <remarks>
/// <para>
/// The band never wraps. When the items do not fit, they move into a trailing "…" button's menu,
/// highest <see cref="PriorityProperty"/> first and, among equal priorities, from the trailing end.
/// In the menu a button is a menu item with its icon and label (<see cref="LabelProperty"/>, or
/// else its tooltip or automation name), a toggle is a checkable item, a SegmentedControl is a
/// header with one radio item per segment, a drop-down's menu is a submenu (another flyout opens
/// at the "…" button), and separators and group boundaries are menu separators. Other controls
/// have no menu form and stay in the band, as do trailing items with
/// <see cref="CanOverflowProperty"/> false.
/// </para>
/// <para>
/// Each item is one Tab stop, in visual order (leading, centre, trailing, then "…"); items in the
/// menu take no focus. The "…" button opens its menu with Enter, Space or Down. The band reports
/// itself to automation as a toolbar.
/// </para>
/// </remarks>
public class ToolbarBand : Control
{
    // The overflow button's tooltip and automation name.
    internal const string MoreLabel = "More";

    public static readonly AttachedProperty<ToolbarZone> ZoneProperty =
        AvaloniaProperty.RegisterAttached<ToolbarBand, Control, ToolbarZone>("Zone");

    public static readonly AttachedProperty<int> PriorityProperty =
        AvaloniaProperty.RegisterAttached<ToolbarBand, Control, int>("Priority");

    public static readonly AttachedProperty<string?> LabelProperty =
        AvaloniaProperty.RegisterAttached<ToolbarBand, Control, string?>("Label");

    public static readonly AttachedProperty<bool> CanOverflowProperty =
        AvaloniaProperty.RegisterAttached<ToolbarBand, Control, bool>("CanOverflow", defaultValue: true);

    public static readonly AttachedProperty<bool> BeginsGroupProperty =
        AvaloniaProperty.RegisterAttached<ToolbarBand, Control, bool>("BeginsGroup");

    // True on button items whose content is an icon, so the band's style makes them square.
    internal static readonly AttachedProperty<bool> IsIconItemProperty =
        AvaloniaProperty.RegisterAttached<ToolbarBand, Control, bool>("IsIconItem");

    private readonly Dictionary<Control, Slot> _slots = [];
    private readonly Button _more;
    private readonly Slot _moreSlot;
    private readonly MenuFlyout _menu;
    private HashSet<Control> _shown = [];
    private bool _moreShown;
    private (HashSet<Control> Shown, bool More)? _planned;

    // Numbers the radio groups of segmented controls in the menu, so each group's name is unique.
    private static int s_groups;

    public ToolbarBand()
    {
        Items.CollectionChanged += (_, _) => SyncItems();

        _menu = new OverflowFlyout(BuildMenu) { Placement = PlacementMode.BottomEdgeAlignedRight };
        _more = new Button { Content = new Icon { Data = Icons.DotsThree }, Flyout = _menu };
        _more.Classes.Add("subtle");
        _more.SetValue(IsIconItemProperty, true);
        ToolTip.SetTip(_more, MoreLabel);
        AutomationProperties.SetName(_more, MoreLabel);
        _more.AddHandler(KeyDownEvent, OnMoreKeyDown);
        LogicalChildren.Add(_more);
        _moreSlot = new Slot(_more) { IsVisible = false };
        VisualChildren.Add(_moreSlot);
    }

    /// <summary>The items, in order within each zone.</summary>
    [Content]
    public Avalonia.Controls.Controls Items { get; } = [];

    public static ToolbarZone GetZone(Control item) => item.GetValue(ZoneProperty);

    /// <summary>Sets which zone <paramref name="item"/> sits in (default <see cref="ToolbarZone.Leading"/>).</summary>
    public static void SetZone(Control item, ToolbarZone value) => item.SetValue(ZoneProperty, value);

    public static int GetPriority(Control item) => item.GetValue(PriorityProperty);

    /// <summary>Sets the overflow priority of <paramref name="item"/> (default 0): higher moves into the "…" menu first.</summary>
    public static void SetPriority(Control item, int value) => item.SetValue(PriorityProperty, value);

    public static string? GetLabel(Control item) => item.GetValue(LabelProperty);

    /// <summary>Sets the label <paramref name="item"/> shows in the "…" menu; by default its tooltip or automation name.</summary>
    public static void SetLabel(Control item, string? value) => item.SetValue(LabelProperty, value);

    public static bool GetCanOverflow(Control item) => item.GetValue(CanOverflowProperty);

    /// <summary>Sets whether a trailing <paramref name="item"/> may move into the "…" menu (default true). Items in the other zones always may.</summary>
    public static void SetCanOverflow(Control item, bool value) => item.SetValue(CanOverflowProperty, value);

    public static bool GetBeginsGroup(Control item) => item.GetValue(BeginsGroupProperty);

    /// <summary>Sets whether <paramref name="item"/> starts a new group: <c>SpacingL</c> before it instead of <c>SpacingS</c>.</summary>
    public static void SetBeginsGroup(Control item, bool value) => item.SetValue(BeginsGroupProperty, value);

    // The overflow button and its menu, and the items now in the menu (for tests and the window).
    internal Button MoreButton => _more;

    internal MenuFlyout OverflowMenu => _menu;

    internal IReadOnlyList<Control> MenuEntries => _menu.ItemsSource as IReadOnlyList<Control> ?? [];

    /// <summary>Whether <paramref name="point"/> (in the band's coordinates) is over a shown item or the "…" button, enabled or not.</summary>
    internal bool IsOverItem(Point point) =>
        _slots.Values.Append(_moreSlot).Any(slot => slot.IsVisible && slot.Bounds.Width > 0 && slot.Bounds.Contains(point));

    internal bool IsOverflowed(Control item) =>
        _moreShown && _slots.ContainsKey(item) && item.IsVisible && IsMenuItem(item) && !_shown.Contains(item);

    protected override AutomationPeer OnCreateAutomationPeer() => new ControlAutomationPeer(this);

    // --- Items ------------------------------------------------------------------------------

    private void SyncItems()
    {
        foreach (var gone in _slots.Keys.Where(item => !Items.Contains(item)).ToList())
        {
            var slot = _slots[gone];
            _slots.Remove(gone);
            gone.PropertyChanged -= OnItemPropertyChanged;
            VisualChildren.Remove(slot);
            slot.Release();
            LogicalChildren.Remove(gone);
            _shown.Remove(gone);
        }
        foreach (var item in Items.Where(item => !_slots.ContainsKey(item)))
        {
            LogicalChildren.Add(item);
            var slot = new Slot(item);
            _slots[item] = slot;
            item.PropertyChanged += OnItemPropertyChanged;
            UpdateIconItem(item);
        }
        OrderSlots();
        InvalidateMeasure();
    }

    private void OnItemPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Control item)
            return;
        if (e.Property == ContentControl.ContentProperty)
            UpdateIconItem(item);
        else if (e.Property == ZoneProperty)
            OrderSlots();
        if (e.Property == ZoneProperty || e.Property == PriorityProperty || e.Property == CanOverflowProperty
            || e.Property == BeginsGroupProperty || e.Property == IsVisibleProperty || e.Property == ContentControl.ContentProperty)
            InvalidateMeasure();
    }

    private static void UpdateIconItem(Control item) =>
        item.SetValue(IsIconItemProperty, item is Button { Content: PathIcon } && item.GetType() != typeof(RepeatButton));

    // The slots in visual order (leading, centre, trailing; each in item order), then "…": the
    // visual children's order is the Tab order.
    private void OrderSlots()
    {
        var order = Ordered().Select(item => (Visual)_slots[item]).Append(_moreSlot).ToList();
        if (VisualChildren.SequenceEqual(order))
            return;
        for (var i = 0; i < order.Count; i++)
        {
            var index = VisualChildren.IndexOf(order[i]);
            if (index == i)
                continue;
            if (index >= 0)
                VisualChildren.RemoveAt(index);
            VisualChildren.Insert(i, order[i]);
        }
    }

    private IEnumerable<Control> Ordered() => Items.Select((item, index) => (item, index))
        .OrderBy(x => GetZone(x.item)).ThenBy(x => x.index).Select(x => x.item);

    // --- Layout -----------------------------------------------------------------------------

    /// <summary>
    /// Measures every item at its natural width, including those now in the "…" menu, so the
    /// arrangement can decide what fits. Measuring changes nothing; see <see cref="ArrangeOverride"/>.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var height = 0.0;
        foreach (var slot in _slots.Values.Append(_moreSlot))
        {
            // A slot the band hides is not measured by layout, and changes inside it do not
            // invalidate its item; its item is invalidated and measured directly, so its width is current.
            if (slot.IsVisible)
            {
                slot.Measure(Size.Infinity);
            }
            else
            {
                foreach (var layoutable in slot.Item.GetSelfAndVisualDescendants().OfType<Layoutable>())
                    layoutable.InvalidateMeasure();
                slot.Item.Measure(Size.Infinity);
            }
            height = Math.Max(height, slot.Item.DesiredSize.Height);
        }
        var natural = NaturalWidth;
        return new Size(double.IsInfinity(availableSize.Width) ? natural : Math.Min(natural, availableSize.Width), height);
    }

    /// <summary>
    /// Places the items that fit and leaves the rest out. Which items show is applied outside
    /// layout (posted), as hiding or showing an item changes layout again; until then an item
    /// that no longer fits is arranged empty, so nothing draws over its neighbours.
    /// </summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var entries = Entries();
        var plan = Fit.Plan(entries, finalSize.Width, MoreWidth);
        var placed = Fit.Place(entries, plan, finalSize.Width, MoreWidth);
        foreach (var (entry, x) in placed)
            ArrangeAt(entry.Slot, x, finalSize.Height);
        foreach (var entry in entries.Where(e => !plan.Shown.Contains(e.Item)))
            entry.Slot.Arrange(default);
        if (plan.More)
            ArrangeAt(_moreSlot, finalSize.Width - Padding - MoreWidth, finalSize.Height);
        else
            _moreSlot.Arrange(default);

        if (!plan.Shown.SetEquals(_shown) || plan.More != _moreShown)
        {
            var post = _planned is null;
            _planned = (plan.Shown, plan.More);
            if (post)
                Dispatcher.UIThread.Post(ApplyPlan);
        }
        else
        {
            _planned = null;
        }
        return finalSize;
    }

    private static void ArrangeAt(Slot slot, double x, double height)
    {
        var size = slot.Item.DesiredSize;
        slot.Arrange(new Rect(x, Math.Max(0, (height - size.Height) / 2), size.Width, size.Height));
    }

    // Shows the items that fit and the "…" button when needed, and hides the rest, so hidden
    // items take no focus. Focus inside an item that moves into the menu goes to "…".
    private void ApplyPlan()
    {
        if (_planned is not { } plan)
            return;
        _planned = null;
        var focusLost = false;
        foreach (var (item, slot) in _slots)
        {
            var show = plan.Shown.Contains(item);
            if (!show && slot.IsVisible && slot.IsKeyboardFocusWithin)
                focusLost = true;
            slot.IsVisible = show;
        }
        _moreSlot.IsVisible = plan.More;
        _shown = plan.Shown;
        _moreShown = plan.More;
        if (!plan.More)
            _menu.Hide();
        if (focusLost && plan.More)
            _more.Focus(NavigationMethod.Tab);
    }

    private static double Padding => Spacing.L;

    // The width with every item shown and no "…" button, from the last measure.
    internal double NaturalWidth => Fit.Required(Entries(), new HashSet<Control>(), more: false, MoreWidth);

    private double MoreWidth => _more.DesiredSize.Width;

    private List<Fit.Entry> Entries() => Ordered().Select(item => new Fit.Entry(
        item,
        _slots[item],
        GetZone(item),
        item switch { Separator => Fit.Kind.Separator, ToolbarSpacer => Fit.Kind.Spacer, _ => Fit.Kind.Item },
        item.IsVisible ? item.DesiredSize.Width : 0,
        GetPriority(item),
        IsMenuItem(item) && (GetZone(item) != ToolbarZone.Trailing || GetCanOverflow(item)),
        GetBeginsGroup(item),
        item.IsVisible)).ToList();

    /// <summary>The layout rules: what fits, and where it goes.</summary>
    internal static class Fit
    {
        internal enum Kind { Item, Separator, Spacer }

        internal sealed record Entry(Control Item, Slot Slot, ToolbarZone Zone, Kind Kind, double Width,
            int Priority, bool CanOverflow, bool BeginsGroup, bool Present);

        internal sealed record Result(HashSet<Control> Shown, bool More);

        /// <summary>
        /// Moves items into the menu, highest priority first and, among equals, from the trailing
        /// end, until the rest fit <paramref name="width"/>. Separators show only between two shown
        /// items of their zone; spacers always show.
        /// </summary>
        public static Result Plan(IReadOnlyList<Entry> entries, double width, double moreWidth)
        {
            var overflow = new HashSet<Control>();
            var candidates = entries.Select((e, i) => (e, i))
                .Where(x => x.e.Kind == Kind.Item && x.e.Present && x.e.CanOverflow)
                .OrderByDescending(x => x.e.Priority).ThenByDescending(x => x.i)
                .Select(x => x.e.Item).ToList();
            foreach (var candidate in candidates)
            {
                if (Required(entries, overflow, overflow.Count > 0, moreWidth) <= width + 0.01)
                    break;
                overflow.Add(candidate);
            }
            return new Result(Shown(entries, overflow).Select(e => e.Item).ToHashSet(), overflow.Count > 0);
        }

        /// <summary>The width the shown entries need, with the side padding and, when <paramref name="more"/>, the "…" button.</summary>
        public static double Required(IReadOnlyList<Entry> entries, IReadOnlySet<Control> overflow, bool more, double moreWidth)
        {
            var shown = Shown(entries, overflow);
            var zones = Zones(shown);
            var filled = zones.Count(z => z.Width > 0);
            var total = 2 * Padding + zones.Sum(z => z.Width) + Spacing.L * Math.Max(0, filled - 1);
            if (more)
                total += moreWidth + (filled > 0 ? Spacing.S : 0);
            return total;
        }

        /// <summary>
        /// Where each shown entry goes. Leading items start at the padding, trailing items end
        /// before "…" (or the padding), and the centre zone is centred in the band, moved aside
        /// only as far as the other zones need. Spacers share the free room on their side; a
        /// spacer in the centre zone makes it span the room between the other two.
        /// </summary>
        public static List<(Entry Entry, double X)> Place(IReadOnlyList<Entry> entries, Result plan, double width, double moreWidth)
        {
            var shown = entries.Where(e => plan.Shown.Contains(e.Item)).ToList();
            var zones = Zones(shown);
            var (lead, centre, trail) = (zones[0], zones[1], zones[2]);
            var right = width - Padding - (plan.More ? moreWidth + (zones.Any(z => z.Width > 0) ? Spacing.S : 0) : 0);
            var leadEnd = Padding + lead.Width;
            var trailStart = right - trail.Width;

            double leadFlex = 0, trailFlex = 0, centreFlex = 0;
            var lo = lead.Width > 0 ? leadEnd + Spacing.L : Padding;
            var hi = trail.Width > 0 ? trailStart - Spacing.L : right;
            var centreX = Math.Clamp((width - centre.Width) / 2, lo, Math.Max(lo, hi - centre.Width));
            if (centre.Spacers > 0)
            {
                centreX = lo;
                centreFlex = Math.Max(0, hi - lo - centre.Width);
            }
            else if (centre.Width > 0)
            {
                leadFlex = Math.Max(0, centreX - Spacing.L - leadEnd);
                trailFlex = Math.Max(0, trailStart - (centreX + centre.Width + Spacing.L));
            }
            else if (lead.Spacers + trail.Spacers > 0)
            {
                var free = Math.Max(0, trailStart - leadEnd - (lead.Width > 0 && trail.Width > 0 ? Spacing.L : 0));
                leadFlex = free * lead.Spacers / (lead.Spacers + trail.Spacers);
                trailFlex = free - leadFlex;
            }

            var placed = new List<(Entry, double)>();
            Lay(lead.Entries, Padding, lead.Spacers > 0 ? leadFlex / lead.Spacers : 0, placed);
            Lay(centre.Entries, centreX, centre.Spacers > 0 ? centreFlex / centre.Spacers : 0, placed);
            Lay(trail.Entries, trail.Spacers > 0 ? trailStart - trailFlex : trailStart, trail.Spacers > 0 ? trailFlex / trail.Spacers : 0, placed);
            return placed;
        }

        private static void Lay(List<Entry> zone, double x, double spacer, List<(Entry, double)> placed)
        {
            Entry? previous = null;
            foreach (var entry in zone)
            {
                if (entry.Kind == Kind.Spacer)
                {
                    placed.Add((entry, x));
                    x += spacer;
                    continue;
                }
                if (previous is not null)
                    x += Gap(previous, entry);
                placed.Add((entry, x));
                x += entry.Width;
                previous = entry;
            }
        }

        // SpacingL before an item that begins a group, SpacingS otherwise and on either side of a separator.
        private static double Gap(Entry previous, Entry next) =>
            next.BeginsGroup && next.Kind == Kind.Item && previous.Kind != Kind.Separator ? Spacing.L : Spacing.S;

        // The present entries not in the menu, without separators that have no shown item on each side within their zone.
        private static List<Entry> Shown(IReadOnlyList<Entry> entries, IReadOnlySet<Control> overflow)
        {
            var kept = entries.Where(e => e.Present && !overflow.Contains(e.Item)).ToList();
            var result = new List<Entry>();
            for (var i = 0; i < kept.Count; i++)
            {
                var e = kept[i];
                if (e.Kind == Kind.Separator)
                {
                    var before = result.LastOrDefault(r => r.Kind != Kind.Spacer);
                    var after = kept.Skip(i + 1).FirstOrDefault(r => r.Kind != Kind.Spacer);
                    if (before is not { Kind: Kind.Item } || before.Zone != e.Zone || after is not { Kind: Kind.Item } || after.Zone != e.Zone)
                        continue;
                }
                result.Add(e);
            }
            return result;
        }

        private static List<(List<Entry> Entries, double Width, int Spacers)> Zones(List<Entry> shown) =>
            Enum.GetValues<ToolbarZone>().Select(zone =>
            {
                var list = shown.Where(e => e.Zone == zone).ToList();
                var width = 0.0;
                Entry? previous = null;
                foreach (var e in list.Where(e => e.Kind != Kind.Spacer))
                {
                    width += e.Width + (previous is null ? 0 : Gap(previous, e));
                    previous = e;
                }
                return (list, width, list.Count(e => e.Kind == Kind.Spacer));
            }).ToList();
    }

    // --- Overflow menu ----------------------------------------------------------------------

    // Items with a menu form: buttons (with drop-downs), toggles and segmented controls; separators
    // and spacers are not items. Other controls stay in the band.
    private static bool IsMenuItem(Control item) => item is SegmentedControl or Button && item.GetType() != typeof(RepeatButton);

    private void OnMoreKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Down || e.KeyModifiers != KeyModifiers.None)
            return;
        e.Handled = true;
        _menu.ShowAt(_more);
    }

    /// <summary>
    /// Fills the "…" menu with the items now in it, in visual order. A separator goes between two
    /// of them where a separator, a group start or a zone boundary lies between them in the band,
    /// and around a segmented control's group.
    /// </summary>
    private List<Control> BuildMenu()
    {
        var entries = new List<Control>();
        var emitted = false;
        var boundary = false;
        var lastWasGroup = false;
        ToolbarZone? zone = null;
        foreach (var item in Ordered().Where(i => i.IsVisible))
        {
            if (zone is { } z && z != GetZone(item))
                boundary = true;
            zone = GetZone(item);
            if (item is Separator)
            {
                boundary = true;
                continue;
            }
            if (item is ToolbarSpacer)
                continue;
            if (GetBeginsGroup(item))
                boundary = true;
            if (!IsOverflowed(item))
                continue;

            var group = item is SegmentedControl;
            if (emitted && (boundary || group || lastWasGroup))
                entries.Add(new Separator());
            entries.AddRange(MenuFor(item));
            emitted = true;
            boundary = false;
            lastWasGroup = group;
        }
        return entries;
    }

    private IEnumerable<Control> MenuFor(Control item) => item switch
    {
        SegmentedControl segmented => SegmentedMenu(segmented),
        ToggleButton toggle => [ToggleMenuItem(toggle)],
        Button button => [ButtonMenuItem(button)],
        _ => [],
    };

    /// <summary>
    /// The label of an item in the menu: its <see cref="LabelProperty"/>, its tooltip text, its
    /// automation name, or else the text of its content (a ContentControl's) or header (a menu item's).
    /// </summary>
    internal static string LabelOf(Control item) =>
        GetLabel(item) ?? ToolTip.GetTip(item) as string ?? AutomationProperties.GetName(item)
        ?? TextOf((item as ContentControl)?.Content ?? (item as HeaderedSelectingItemsControl)?.Header) ?? "";

    // Text content: a string, or a TextBlock's or AccessText's text.
    private static string? TextOf(object? content) => content switch
    {
        string text => text,
        TextBlock block => block.Text,
        _ => null,
    };

    private static MenuItem MenuEntry(Control item, object? content)
    {
        var menuItem = new MenuItem { Header = LabelOf(item) };
        if (content is PathIcon { Data: { } data })
            menuItem.Icon = new Icon { Data = data };
        menuItem.Bind(IsEnabledProperty, new Binding(nameof(IsEnabled)) { Source = item, Mode = BindingMode.OneWay });
        return menuItem;
    }

    private MenuItem ButtonMenuItem(Button button)
    {
        var menuItem = MenuEntry(button, button.Content);
        if (button.Flyout is MenuFlyout { } dropDown)
        {
            foreach (var child in dropDown.Items)
                if (Proxy(child, dropDown.Items) is { } proxy)
                    menuItem.Items.Add(proxy);
            return menuItem;
        }
        menuItem.Click += (_, _) =>
        {
            if (button.Flyout is { } flyout)
                Dispatcher.UIThread.Post(() => flyout.ShowAt(_more));
            else if (ControlAutomationPeer.CreatePeerForElement(button) is IInvokeProvider invoke)
                invoke.Invoke();
        };
        return menuItem;
    }

    private static MenuItem ToggleMenuItem(ToggleButton toggle)
    {
        var menuItem = MenuEntry(toggle, toggle.Content);
        menuItem.ToggleType = MenuItemToggleType.CheckBox;
        menuItem.Bind(MenuItem.IsCheckedProperty, new Binding(nameof(ToggleButton.IsChecked))
        {
            Source = toggle,
            Mode = BindingMode.OneWay,
            Converter = new FuncValueConverter<bool?, bool>(on => on == true),
        });
        menuItem.Click += (_, _) =>
        {
            if (ControlAutomationPeer.CreatePeerForElement(toggle) is IToggleProvider provider)
                provider.Toggle();
        };
        return menuItem;
    }

    // A non-selectable header with the control's label, then one radio item per segment, the
    // selected one marked. Choosing one selects its segment.
    private static IEnumerable<Control> SegmentedMenu(SegmentedControl segmented)
    {
        var header = new TextBlock { Text = LabelOf(segmented) };
        header.Bind(TextBlock.FontSizeProperty, new DynamicResourceExtension("FontSizeCaption"));
        header.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseMediumBrush"));
        yield return new MenuItem { Header = header, IsEnabled = false };

        var group = $"ToolbarBand.Segments.{++s_groups}";
        for (var i = 0; i < segmented.ItemCount; i++)
        {
            var index = i;
            var segment = segmented.ContainerFromIndex(i) as Control;
            var data = segmented.Items[i];
            var content = (segment as ContentControl)?.Content ?? data;
            var radio = new MenuItem
            {
                Header = segment is not null && (GetLabel(segment) ?? ToolTip.GetTip(segment) as string ?? AutomationProperties.GetName(segment)) is { } named
                    ? named
                    : content as string ?? data?.ToString() ?? "",
                ToggleType = MenuItemToggleType.Radio,
                GroupName = group,
            };
            if (content is PathIcon { Data: { } geometry })
                radio.Icon = new Icon { Data = geometry };
            if (segment is not null)
                radio.Bind(IsEnabledProperty, new Binding(nameof(IsEnabled)) { Source = segment, Mode = BindingMode.OneWay });
            radio.Bind(MenuItem.IsCheckedProperty, new Binding(nameof(SegmentedControl.SelectedIndex))
            {
                Source = segmented,
                Mode = BindingMode.OneWay,
                Converter = new FuncValueConverter<int, bool>(selected => selected == index),
            });
            radio.Click += (_, _) => segmented.SelectedIndex = index;
            yield return radio;
        }
    }

    // A drop-down's menu entry in the "…" menu's submenu: the same header (a non-text header as its
    // label), icon, gesture, check state and enabled state. Choosing it chooses the original entry:
    // a check item toggles and a radio item is checked, clearing the others of its group among
    // <paramref name="siblings"/>, then the original's click is raised (its handlers and command).
    private static Control? Proxy(object? child, IEnumerable<object?> siblings)
    {
        switch (child)
        {
            case Separator:
                return new Separator();
            case MenuItem original:
                var proxy = new MenuItem
                {
                    Header = original.Header is string or null ? original.Header : LabelOf(original),
                    InputGesture = original.InputGesture,
                    ToggleType = original.ToggleType,
                    GroupName = original.GroupName,
                };
                if (original.Icon is PathIcon { Data: { } data })
                    proxy.Icon = new Icon { Data = data };
                proxy.Bind(IsEnabledProperty, new Binding(nameof(IsEnabled)) { Source = original, Mode = BindingMode.OneWay });
                proxy.Bind(MenuItem.IsCheckedProperty, new Binding(nameof(MenuItem.IsChecked)) { Source = original, Mode = BindingMode.OneWay });
                foreach (var grandchild in original.Items)
                    if (Proxy(grandchild, original.Items) is { } sub)
                        proxy.Items.Add(sub);
                if (original.ItemCount == 0)
                    proxy.Click += (_, _) =>
                    {
                        Choose(original, siblings);
                        original.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    };
                return proxy;
            default:
                return null;
        }
    }

    // A menu item that is not open does not toggle itself when its click is raised, so the check
    // state is set here, as choosing it in its own menu would.
    private static void Choose(MenuItem original, IEnumerable<object?> siblings)
    {
        if (original.ToggleType == MenuItemToggleType.CheckBox)
        {
            original.SetCurrentValue(MenuItem.IsCheckedProperty, !original.IsChecked);
        }
        else if (original.ToggleType == MenuItemToggleType.Radio)
        {
            foreach (var other in siblings.OfType<MenuItem>())
                if (other != original && other.ToggleType == MenuItemToggleType.Radio && other.GroupName == original.GroupName)
                    other.SetCurrentValue(MenuItem.IsCheckedProperty, false);
            original.SetCurrentValue(MenuItem.IsCheckedProperty, true);
        }
    }

    /// <summary>
    /// The "…" menu: rebuilt from the band each time it opens. A MenuFlyout's presenter, once
    /// created, keeps the item list it was created with, so the new entries go to the presenter too.
    /// </summary>
    private sealed class OverflowFlyout(Func<List<Control>> build) : MenuFlyout
    {
        protected override void OnOpening(System.ComponentModel.CancelEventArgs args)
        {
            var entries = build();
            ItemsSource = entries;
            if (Popup.Child is ItemsControl presenter)
                presenter.ItemsSource = entries;
            base.OnOpening(args);
        }
    }

    /// <summary>Holds one item in the visual tree, so the band can hide it without touching the item's own visibility.</summary>
    internal sealed class Slot : Control
    {
        public Slot(Control item)
        {
            Item = item;
            VisualChildren.Add(item);
        }

        public Control Item { get; }

        public void Release() => VisualChildren.Remove(Item);

        protected override Size MeasureOverride(Size availableSize)
        {
            Item.Measure(availableSize);
            return Item.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Item.Arrange(new Rect(finalSize));
            return finalSize;
        }
    }
}
