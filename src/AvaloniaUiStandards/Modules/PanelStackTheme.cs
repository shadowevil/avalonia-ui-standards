using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The PanelStack and PanelStackItem themes. The stack is a vertical scroller over a stack of cards <c>SpacingS</c>
/// apart; each item is the Expander's card (0033), with the item's icon before the header in the title strip.
/// </summary>
internal static class PanelStackTheme
{
    public const string ScrollViewerPart = "PART_ScrollViewer";
    public const string IconPart = "PART_Icon";
    public const string HeaderPart = "PART_Header";

    /// <summary>The gap between cards, and between the icon and the header (<c>SpacingS</c>).</summary>
    public const double Gap = Spacing.S;

    public static ControlTheme Create() => new(typeof(PanelStack))
    {
        Setters =
        {
            new Setter(ItemsControl.ItemsPanelProperty, new FuncTemplate<Panel?>(() => new StackPanel { Spacing = Gap })),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<PanelStack>(BuildStack)),
        },
    };

    /// <param name="expander">The Expander theme, whose look and states the items keep.</param>
    public static ControlTheme Item(ControlTheme? expander) => new(typeof(PanelStackItem))
    {
        BasedOn = expander,
        Setters =
        {
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<PanelStackItem>(BuildItem)),
        },
    };

    private static Control BuildStack(PanelStack stack, INameScope scope)
    {
        var items = new ItemsPresenter { Name = "PART_ItemsPresenter" };
        items.Bind(ItemsPresenter.ItemsPanelProperty, new TemplateBinding(ItemsControl.ItemsPanelProperty));
        items.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        var scroller = new ScrollViewer
        {
            Name = ScrollViewerPart,
            Content = items,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        items.RegisterInNameScope(scope);
        scroller.RegisterInNameScope(scope);
        return scroller;
    }

    // The Expander's card, with the icon (when set) before the header; the icon takes the header's colour.
    private static Control BuildItem(PanelStackItem item, INameScope scope)
    {
        var icon = new Icon { Name = IconPart, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, Gap, 0) };
        icon.Bind(PathIcon.DataProperty, item.GetObservable(PanelStackItem.IconProperty).ToBinding());
        icon.Bind(Visual.IsVisibleProperty, item.GetObservable(PanelStackItem.IconProperty, g => g is not null).ToBinding());
        var header = new ContentPresenter { Name = HeaderPart, VerticalAlignment = VerticalAlignment.Center };
        header.Bind(ContentPresenter.ContentProperty, item.GetObservable(HeaderedContentControl.HeaderProperty).ToBinding());
        header.Bind(ContentPresenter.ContentTemplateProperty, item.GetObservable(HeaderedContentControl.HeaderTemplateProperty).ToBinding());
        var strip = new StackPanel { Orientation = Orientation.Horizontal, Children = { icon, header } };
        var card = ExpanderTheme.Build(item, scope, strip);
        icon.RegisterInNameScope(scope);
        header.RegisterInNameScope(scope);
        return card;
    }
}
