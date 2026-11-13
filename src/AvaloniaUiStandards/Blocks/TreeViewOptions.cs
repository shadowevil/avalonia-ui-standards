using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUiStandards.Blocks;

/// <summary>Which clicks on a tree row expand or collapse it (<see cref="TreeViewOptions"/>).</summary>
[Flags]
public enum TreeViewClickAction
{
    /// <summary>No click on the row; the caret and the keyboard still toggle.</summary>
    None = 0,

    /// <summary>A single click on the row.</summary>
    SingleClick = 1,

    /// <summary>A double click on the row.</summary>
    DoubleClick = 2,
}

/// <summary>
/// Options for the TreeView Block, as attached properties set on a <see cref="TreeView"/> and
/// inherited by its items; an item can override them for itself and its descendants.
/// </summary>
public static class TreeViewOptions
{
    /// <summary>
    /// The clicks on a collapsed row that expand it. Default <see cref="TreeViewClickAction.DoubleClick"/>.
    /// The caret expands with one click whatever this is, and rows without children ignore it.
    /// </summary>
    public static readonly AttachedProperty<TreeViewClickAction> ExpandOnProperty =
        AvaloniaProperty.RegisterAttached<Control, TreeViewClickAction>("ExpandOn", typeof(TreeViewOptions),
            defaultValue: TreeViewClickAction.DoubleClick, inherits: true);

    /// <summary>
    /// The clicks on an expanded row that collapse it. Default <see cref="TreeViewClickAction.DoubleClick"/>.
    /// The caret collapses with one click whatever this is, and rows without children ignore it.
    /// </summary>
    public static readonly AttachedProperty<TreeViewClickAction> CollapseOnProperty =
        AvaloniaProperty.RegisterAttached<Control, TreeViewClickAction>("CollapseOn", typeof(TreeViewOptions),
            defaultValue: TreeViewClickAction.DoubleClick, inherits: true);

    public static TreeViewClickAction GetExpandOn(Control control) => control.GetValue(ExpandOnProperty);

    public static void SetExpandOn(Control control, TreeViewClickAction value) => control.SetValue(ExpandOnProperty, value);

    public static TreeViewClickAction GetCollapseOn(Control control) => control.GetValue(CollapseOnProperty);

    public static void SetCollapseOn(Control control, TreeViewClickAction value) => control.SetValue(CollapseOnProperty, value);
}
