using Avalonia;
using Avalonia.Controls;
using System.Collections.Specialized;
using Avalonia.Input;
using Avalonia.Controls.Selection;
using Avalonia.Interactivity;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// A segmented control: a row of equal-width segments for choosing one of a few options of a
/// single setting, with text or icon segments. It is a <see cref="ListBox"/> with one selection
/// that is always kept, so it has one Tab stop and its items, selection and data binding work as
/// in a list. Left and Right (and Up and Down) move to the previous or next enabled segment and
/// select it, stopping at the ends; Home and End select the first and last. A click selects its
/// segment whatever the modifier keys; removing the selected segment selects the nearest
/// remaining one, and replacing it keeps its index. The selected segment
/// is shown by one pill that slides between segments. Icon-only segments carry a
/// <see cref="ToolTip"/> and an automation name, as the icon alone does not name the option.
/// </summary>
public class SegmentedControl : ListBox
{
    static SegmentedControl()
    {
        SelectionModeProperty.OverrideDefaultValue<SegmentedControl>(SelectionMode.Single | SelectionMode.AlwaysSelected);
        // Every segment is always in view, so a selection never scrolls the page around the control.
        AutoScrollToSelectedItemProperty.OverrideDefaultValue<SegmentedControl>(false);
    }

    // The segment to select when the selection model loses its selection to the collection change
    // in progress; null when that change does not touch the selected segment.
    private int? _reselect;
    private ISelectionModel? _model;

    public SegmentedControl()
    {
        ItemsView.CollectionChanged += OnItemsChanged;
        Watch(Selection);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectionProperty)
            Watch(Selection);
    }

    // Subscribed after the list's own handler, so the reselection here comes last.
    private void Watch(ISelectionModel model)
    {
        if (ReferenceEquals(model, _model))
            return;
        if (_model is not null)
            _model.LostSelection -= OnLostSelection;
        _model = model;
        model.LostSelection += OnLostSelection;
    }

    /// <summary>
    /// Pointer selection ignores the toggle and range modifiers (Ctrl, Cmd, Shift): a click always
    /// selects its segment, and never clears the selection.
    /// </summary>
    public override bool UpdateSelectionFromEvent(Control container, RoutedEventArgs eventArgs)
    {
        if (eventArgs is not PointerEventArgs || IndexFromContainer(container) is not (>= 0 and var index))
            return base.UpdateSelectionFromEvent(container, eventArgs);
        if (index != SelectedIndex)
            SelectedIndex = index;
        return true;
    }

    // Removing the selected segment selects the nearest remaining one (the one that takes its
    // place, or the new last one); replacing it keeps its index. The control hears of the change
    // while the old selection still stands, so the target is recorded here; the selection model
    // then reports the lost selection, and the target is selected there instead of the first.
    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var selected = SelectedIndex;
        _reselect = e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace
            && selected >= e.OldStartingIndex && selected < e.OldStartingIndex + (e.OldItems?.Count ?? 0)
            ? e.Action == NotifyCollectionChangedAction.Replace ? selected : e.OldStartingIndex
            : null;
    }

    private void OnLostSelection(object? sender, EventArgs e)
    {
        if (_reselect is not { } index)
            return;
        _reselect = null;
        if (ItemCount > 0)
            SelectedIndex = Math.Min(index, ItemCount - 1);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new SegmentedControlItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<SegmentedControlItem>(item, out recycleKey);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.None || e.Key is not (Key.Home or Key.End or Key.Left or Key.Right or Key.Up or Key.Down))
        {
            base.OnKeyDown(e);
            return;
        }
        // Navigation keys are always handled, so they never scroll a page around the control.
        e.Handled = true;
        if (Target(e.Key) is not { } index)
            return;
        if (index != SelectedIndex)
            SelectedIndex = index;
        (ContainerFromIndex(index) as InputElement)?.Focus(NavigationMethod.Directional);
    }

    /// <summary>The segment a navigation key moves to, or null when there is nowhere to go.</summary>
    private int? Target(Key key)
    {
        var count = ItemCount;
        var rtl = FlowDirection == Avalonia.Media.FlowDirection.RightToLeft;
        var from = SelectedIndex;
        return key switch
        {
            Key.Home => Next(-1, +1),
            Key.End => Next(count, -1),
            Key.Left => Next(from, rtl ? +1 : -1),
            Key.Right => Next(from, rtl ? -1 : +1),
            Key.Up => Next(from, -1),
            _ => Next(from, +1),
        };

        // The first enabled segment from 'start' (exclusive) in 'step' direction, or null at the end.
        int? Next(int start, int step)
        {
            for (var i = start + step; i >= 0 && i < count; i += step)
                if (ContainerFromIndex(i) is not { IsEnabled: false })
                    return i;
            return null;
        }
    }
}

/// <summary>A segment of a <see cref="SegmentedControl"/>.</summary>
public class SegmentedControlItem : ListBoxItem
{
}
