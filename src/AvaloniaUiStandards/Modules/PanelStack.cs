using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// A stack of panels for an inspector or another side panel: <see cref="PanelStackItem"/> cards, one under the other
/// with <c>SpacingS</c> between them, each with a title strip that collapses and expands it. The stack scrolls
/// vertically when its cards are taller than it. A tab group is a <see cref="TabControl"/> placed in an item.
/// </summary>
public class PanelStack : ItemsControl
{
    protected override Type StyleKeyOverride => typeof(PanelStack);

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<PanelStackItem>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new PanelStackItem();
}

/// <summary>
/// A panel in a <see cref="PanelStack"/>: a solid card with a title strip in the Expander's look (the caret, then the
/// optional <see cref="Icon"/>, then the <see cref="HeaderedContentControl.Header"/>) over its content. Choosing the
/// strip collapses and expands the card (<see cref="Expander.IsExpanded"/>).
/// </summary>
public class PanelStackItem : Expander
{
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<PanelStackItem, Geometry?>(nameof(Icon));

    /// <summary>The title strip's optional icon, drawn in a 256 x 256 design box as <c>Icons</c> are.</summary>
    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(PanelStackItem);
}
