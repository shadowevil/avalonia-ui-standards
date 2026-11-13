using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// Options for how list selection is shown, as attached properties set on a ListBox (and the
/// Sidebar), a ComboBox, an AutoCompleteBox or a TreeView and inherited by its items; an item can
/// override them for itself (and, in a tree, its descendants).
/// </summary>
public static class SelectionOptions
{
    /// <summary>
    /// Whether a selected item shows the accent flag: a full-height bar on its leading edge, whose
    /// corners are then square. Default false.
    /// </summary>
    public static readonly AttachedProperty<bool> AccentFlagProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("AccentFlag", typeof(SelectionOptions), defaultValue: false, inherits: true);

    public static bool GetAccentFlag(Control control) => control.GetValue(AccentFlagProperty);

    public static void SetAccentFlag(Control control, bool value) => control.SetValue(AccentFlagProperty, value);
}
