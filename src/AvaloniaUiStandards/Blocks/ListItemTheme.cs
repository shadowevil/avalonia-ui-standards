using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The list item themes (ListBoxItem, ComboBoxItem) and the ListBox theme. Items are squircle
/// pills inset from the list edges, transparent at rest, with Fluent's neutral fills on hover
/// (list medium) and press (base medium-low), the accent fill with white text when selected,
/// density heights, the focus ring and the optional accent flag (<see cref="AccentFlag"/>). The
/// ListBox is transparent and shows no focus visual of its own: its items carry the ring.
/// </summary>
internal static class ListItemTheme
{
    public static ControlTheme Item(Type itemType)
    {
        var theme = new ControlTheme(itemType);
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(PaddingKey)));
        theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(MinHeightKey)));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
        theme.Setters.Add(new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ContentControl>(BuildItem)));
        FocusRing.AddTo(theme);

        theme.Children.Add(ChromeMotion());
        theme.Children.Add(State(":pointerover", new Look("SystemControlHighlightListMediumBrush", Brushes.Transparent, "SystemControlHighlightAltBaseHighBrush")));
        theme.Children.Add(State(":pressed", new Look("SystemControlHighlightBaseMediumLowBrush", Brushes.Transparent, "SystemControlHighlightAltBaseHighBrush")));
        theme.Children.Add(State(":disabled", new Look(Brushes.Transparent, Brushes.Transparent, "SystemControlDisabledBaseMediumLowBrush")));

        // Selected: the AA accent fills with white text, darker on hover and press.
        theme.Children.Add(Group(x => x.Nesting().Class(":selected"),
            rest: new("AccentFillColor", Brushes.Transparent, "AccentButtonForeground"),
            hover: new("AccentFillColorPointerOver", Brushes.Transparent, "AccentButtonForeground"),
            pressed: new("AccentFillColorPressed", Brushes.Transparent, "AccentButtonForeground"),
            disabled: new("AccentButtonBackgroundDisabled", Brushes.Transparent, "AccentButtonForegroundDisabled")));
        AccentFlag.AddTo(theme);
        return theme;
    }

    /// <summary>The ListBox: Fluent's structure in a squircle, its items inset by <c>SpacingXS</c>.</summary>
    public static ControlTheme ListBox(ControlTheme? fluentTheme) => new(typeof(ListBox))
    {
        BasedOn = fluentTheme,
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.PaddingProperty, new Thickness(Spacing.XS)),
            new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")),
            new Setter(TemplatedControl.TemplateProperty, ListTemplate),
        },
    };

    private static Control BuildItem(ContentControl item, INameScope scope)
    {
        var presenter = CreatePresenter();
        var chrome = CreateChrome(presenter);
        chrome.RegisterInNameScope(scope);
        presenter.RegisterInNameScope(scope);
        return FocusRing.Wrap(AccentFlag.Add(chrome, scope), scope);
    }

    /// <summary>
    /// The list template shared by ListBox, TreeView and the Sidebar: the items, inset by the
    /// padding, in a scroller (which takes the control's scroll settings) in a squircle.
    /// </summary>
    internal static readonly FuncControlTemplate<ItemsControl> ListTemplate = new(BuildList);

    internal static Control BuildList(ItemsControl list, INameScope scope)
    {
        var items = new ItemsPresenter { Name = "PART_ItemsPresenter" };
        items.Bind(ItemsPresenter.ItemsPanelProperty, new TemplateBinding(ItemsControl.ItemsPanelProperty));
        items.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.PaddingProperty));

        var scroller = new ScrollViewer { Name = "PART_ScrollViewer", Content = items };
        foreach (var property in new AvaloniaProperty[]
                 {
                     ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollViewer.VerticalScrollBarVisibilityProperty,
                     ScrollViewer.IsScrollChainingEnabledProperty, ScrollViewer.IsScrollInertiaEnabledProperty,
                     ScrollViewer.AllowAutoHideProperty, ScrollViewer.IsDeferredScrollingEnabledProperty,
                     ScrollViewer.BringIntoViewOnFocusChangeProperty,
                     ScrollViewer.HorizontalSnapPointsTypeProperty, ScrollViewer.VerticalSnapPointsTypeProperty,
                     ScrollViewer.HorizontalSnapPointsAlignmentProperty, ScrollViewer.VerticalSnapPointsAlignmentProperty,
                     ScrollShadows.IsEnabledProperty,
                 })
            scroller.Bind(property, new TemplateBinding(property));

        var border = new SquircleBorder { Name = "border", Child = scroller };
        border.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        border.Bind(SquircleBorder.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        border.Bind(SquircleBorder.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        border.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));

        border.RegisterInNameScope(scope);
        scroller.RegisterInNameScope(scope);
        items.RegisterInNameScope(scope);
        return border;
    }
}
