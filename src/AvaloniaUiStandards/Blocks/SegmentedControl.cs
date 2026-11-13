using Avalonia;
using Avalonia.Controls;
using System.Collections.Specialized;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// A segmented control: a row of equal-width segments for choosing one of a few options of a
/// single setting, with text or icon segments. It is a <see cref="ListBox"/> with one selection
/// that is always kept, so it has one Tab stop and its items, selection and data binding work as
/// in a list. Left and Right (and Up and Down) move to the previous or next enabled segment and
/// select it, stopping at the ends; Home and End select the first and last. A click selects its
/// segment whatever the modifier keys, and removing the selected segment selects the nearest
/// remaining one. The selected segment
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

    public SegmentedControl()
    {
        ItemsView.CollectionChanged += OnItemsChanged;
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
    // place, or the new last one), instead of the first. The control hears of the removal while
    // the old selection still stands; the selection model then reselects the first segment, so
    // the nearest one is selected right after, before the next layout.
    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var selected = SelectedIndex;
        if (e.Action is not NotifyCollectionChangedAction.Remove || selected < e.OldStartingIndex
            || selected >= e.OldStartingIndex + (e.OldItems?.Count ?? 0))
            return;
        Dispatcher.UIThread.Post(() =>
        {
            if (ItemCount > 0)
                SelectedIndex = Math.Min(e.OldStartingIndex, ItemCount - 1);
        }, DispatcherPriority.Send);
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
