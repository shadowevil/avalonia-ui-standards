using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The menu themes: MenuItem, the top-level items of a Menu bar, Menu, ContextMenu,
/// MenuFlyoutPresenter and FlyoutPresenter. Menus and flyouts are glass overlays with the overlay
/// radius and <c>SpacingXS</c> padding, as the ComboBox drop-down. Their items use the list item
/// design: a squircle pill, transparent at rest, list medium when highlighted (pointer or
/// keyboard) and base medium-low when pressed, with density heights. Columns: the check or radio
/// mark, the icon, the header, the input gesture in secondary text and the submenu caret.
/// Top-level items are subtle buttons. Each overlay fades in over <c>MotionFast</c>.
/// </summary>
internal static class MenuTheme
{
    public const string ToggleMarkPart = "PART_ToggleIconPresenter";
    public const string CheckMarkPart = "PART_CheckMark";
    public const string RadioMarkPart = "PART_RadioMark";
    public const string IconPart = "PART_IconPresenter";
    public const string HeaderPart = "PART_HeaderPresenter";
    public const string GesturePart = "PART_InputGestureText";
    public const string CaretPart = "PART_ChevronPath";
    public const string PopupPart = "PART_Popup";
    public const string ItemsPart = "PART_ItemsPresenter";

    /// <summary>The radio mark's diameter.</summary>
    public const double RadioMarkSize = 8;

    /// <summary>The gap after the mark and icon columns and before the caret: <c>SpacingS</c>.</summary>
    public const double ColumnGap = Spacing.S;

    /// <summary>The gap before the input gesture: <c>SpacingXL</c>.</summary>
    public const double GestureGap = Spacing.XL;

    /// <summary>The overlay's padding, which insets the items from its edges: <c>SpacingXS</c>, as the ComboBox drop-down.</summary>
    public static readonly Thickness OverlayPadding = new(Spacing.XS);

    /// <summary>The gap between a menu bar item and its menu.</summary>
    public const double BarMenuOffset = Spacing.XS;

    private static readonly IValueConverter GestureText =
        new FuncValueConverter<KeyGesture?, string?>(gesture => gesture?.ToString("p", null));

    // --- MenuItem in a menu ------------------------------------------------------------------

    public static ControlTheme Item()
    {
        var theme = new ControlTheme(typeof(MenuItem));
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(PaddingKey)));
        theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(MinHeightKey)));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Resource("MenuFlyoutItemForeground")));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<MenuItem>(BuildItem)));

        theme.Children.Add(ChromeMotion());
        theme.Children.Add(Fill(":selected", "SystemControlHighlightListMediumBrush"));
        theme.Children.Add(Fill(":pressed", "SystemControlHighlightBaseMediumLowBrush"));
        theme.Children.Add(Fill(":disabled", null));
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":disabled"))
        {
            Setters = { new Setter(TemplatedControl.ForegroundProperty, Resource("SystemControlDisabledBaseMediumLowBrush")) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<TextBlock>().Name(GesturePart))
        {
            Setters = { new Setter(TextBlock.ForegroundProperty, Resource("MenuFlyoutItemKeyboardAcceleratorTextForeground")) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":disabled").Template().OfType<TextBlock>().Name(GesturePart))
        {
            Setters = { new Setter(TextBlock.ForegroundProperty, Resource("SystemControlDisabledBaseMediumLowBrush")) },
        });

        // The mark and icon columns are hidden unless the item has them; the mark column shows for
        // every toggle or radio item, checked or not, so their headers line up.
        theme.Children.Add(Hidden(x => x.Nesting().Template().OfType<Viewbox>().Name(IconPart)));
        theme.Children.Add(Hidden(x => x.Nesting().Template().OfType<Panel>().Name(ToggleMarkPart)));
        theme.Children.Add(Hidden(x => x.Nesting().Template().OfType<Icon>().Name(CheckMarkPart)));
        theme.Children.Add(Hidden(x => x.Nesting().Template().OfType<Ellipse>().Name(RadioMarkPart)));
        theme.Children.Add(Visible(x => x.Nesting().Class(":icon").Template().OfType<Viewbox>().Name(IconPart)));
        theme.Children.Add(Visible(x => x.Nesting().Class(":toggle").Template().OfType<Panel>().Name(ToggleMarkPart)));
        theme.Children.Add(Visible(x => x.Nesting().Class(":radio").Template().OfType<Panel>().Name(ToggleMarkPart)));
        theme.Children.Add(Visible(x => x.Nesting().Class(":checked").Class(":toggle").Template().OfType<Icon>().Name(CheckMarkPart)));
        theme.Children.Add(Visible(x => x.Nesting().Class(":checked").Class(":radio").Template().OfType<Ellipse>().Name(RadioMarkPart)));
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":empty").Template().OfType<Icon>().Name(CaretPart))
        {
            Setters = { new Setter(Visual.IsVisibleProperty, false) },
        });

        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":separator"))
        {
            Setters =
            {
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<MenuItem>((_, _) => new Separator { Margin = SeparatorTheme.MenuMargin })),
                new Setter(Layoutable.MinHeightProperty, 0d),
                new Setter(InputElement.FocusableProperty, false),
            },
        });
        return theme;
    }

    private static Control BuildItem(MenuItem item, INameScope scope)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto) { SharedSizeGroup = "ToggleIcon" },
                new ColumnDefinition(GridLength.Auto) { SharedSizeGroup = "MenuItemIcon" },
                new ColumnDefinition(1, GridUnitType.Star),
                new ColumnDefinition(GridLength.Auto) { SharedSizeGroup = "MenuItemIGT" },
                new ColumnDefinition(GridLength.Auto) { SharedSizeGroup = "MenuItemChevron" },
            },
        };

        var check = new Icon { Name = CheckMarkPart, Data = Icons.Check };
        var radio = new Ellipse { Name = RadioMarkPart, Width = RadioMarkSize, Height = RadioMarkSize };
        radio.Bind(Shape.FillProperty, new TemplateBinding(TemplatedControl.ForegroundProperty));
        var toggle = new Panel { Name = ToggleMarkPart, Margin = new Thickness(0, 0, ColumnGap, 0), Children = { check, radio } };
        toggle.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(IconTheme.SizeKey));
        toggle.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(IconTheme.SizeKey));

        var iconContent = new ContentPresenter();
        iconContent.Bind(ContentPresenter.ContentProperty, new TemplateBinding(MenuItem.IconProperty));
        var icon = new Viewbox { Name = IconPart, Margin = new Thickness(0, 0, ColumnGap, 0), Child = iconContent };
        icon.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(IconTheme.SizeKey));
        icon.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(IconTheme.SizeKey));
        Grid.SetColumn(icon, 1);

        var header = new ContentPresenter { Name = HeaderPart, RecognizesAccessKey = true, VerticalAlignment = VerticalAlignment.Center };
        header.Bind(ContentPresenter.ContentProperty, new TemplateBinding(HeaderedSelectingItemsControl.HeaderProperty));
        header.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(HeaderedSelectingItemsControl.HeaderTemplateProperty));
        Grid.SetColumn(header, 2);

        var gesture = new TextBlock
        {
            Name = GesturePart,
            Margin = new Thickness(GestureGap, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        gesture.Bind(TextBlock.TextProperty, new TemplateBinding(MenuItem.InputGestureProperty) { Converter = GestureText });
        Grid.SetColumn(gesture, 3);

        var caret = new Icon { Name = CaretPart, Data = Icons.CaretRight, Margin = new Thickness(ColumnGap, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(caret, 4);

        grid.Children.Add(toggle);
        grid.Children.Add(icon);
        grid.Children.Add(header);
        grid.Children.Add(gesture);
        grid.Children.Add(caret);

        var chrome = CreateChrome(grid);
        chrome.Bind(Decorator.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));

        var popup = SubmenuPopup(PlacementMode.RightEdgeAlignedTop);
        // The submenu's first item lines up with this item: the overlay's padding and edge are taken back.
        popup.VerticalOffset = -(OverlayPadding.Top + GlassBorder.BorderWidth);

        foreach (var named in new StyledElement[] { chrome, toggle, check, radio, icon, header, gesture, caret, popup })
            named.RegisterInNameScope(scope);
        RegisterItems(popup, scope);
        return new Panel { Children = { chrome, popup } };
    }

    private static Avalonia.Styling.Style Fill(string pseudoClass, string? brushKey) =>
        new(x => Chrome(x.Nesting().Class(pseudoClass)))
        {
            Setters = { new Setter(SquircleBorder.BackgroundProperty, brushKey is null ? Brushes.Transparent : Resource(brushKey)) },
        };

    private static Avalonia.Styling.Style Visible(Func<Selector?, Selector> selector) =>
        new(x => selector(x)) { Setters = { new Setter(Visual.IsVisibleProperty, true) } };

    private static Avalonia.Styling.Style Hidden(Func<Selector?, Selector> selector) =>
        new(x => selector(x)) { Setters = { new Setter(Visual.IsVisibleProperty, false) } };

    // --- Menu bar ------------------------------------------------------------------------------

    /// <summary>The Menu bar: Fluent's, its height following its items, with top-level items as subtle buttons.</summary>
    public static ControlTheme Menu(ControlTheme? fluentTheme) => new(typeof(Menu))
    {
        BasedOn = fluentTheme,
        Setters =
        {
            new Setter(Layoutable.HeightProperty, double.NaN),
            new Setter(ItemsControl.ItemContainerThemeProperty, BarItem()),
        },
    };

    /// <summary>A top-level item of a Menu bar: a subtle button whose menu opens below it.</summary>
    public static ControlTheme BarItem()
    {
        var subtle = new Look(Brushes.Transparent, Brushes.Transparent, "MenuFlyoutItemForeground");
        var hover = new Look("ButtonBackgroundPointerOver", Brushes.Transparent, "MenuFlyoutItemForeground");
        var pressed = new Look("ButtonBackgroundPressed", Brushes.Transparent, "MenuFlyoutItemForeground");

        var theme = new ControlTheme(typeof(MenuItem));
        foreach (var setter in Sizing())
            theme.Setters.Add(setter);
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, subtle.Background));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Resource("MenuFlyoutItemForeground")));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<MenuItem>(BuildBarItem)));
        theme.Children.Add(ChromeMotion());
        theme.Children.Add(BarState(":selected", hover));
        theme.Children.Add(BarState(":pressed", pressed));
        theme.Children.Add(BarState(":open", pressed));
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":disabled"))
        {
            Setters = { new Setter(TemplatedControl.ForegroundProperty, Resource("SystemControlDisabledBaseMediumLowBrush")) },
        });
        return theme;
    }

    private static Avalonia.Styling.Style BarState(string pseudoClass, Look look) => new(x => Chrome(x.Nesting().Class(pseudoClass)))
    {
        Setters = { new Setter(SquircleBorder.BackgroundProperty, Value(look.Background)) },
    };

    private static Control BuildBarItem(MenuItem item, INameScope scope)
    {
        var header = new ContentPresenter { Name = HeaderPart, RecognizesAccessKey = true, VerticalAlignment = VerticalAlignment.Center };
        header.Bind(ContentPresenter.ContentProperty, new TemplateBinding(HeaderedSelectingItemsControl.HeaderProperty));
        header.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(HeaderedSelectingItemsControl.HeaderTemplateProperty));

        var chrome = CreateChrome(header);
        chrome.Bind(Decorator.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));

        var popup = SubmenuPopup(PlacementMode.BottomEdgeAlignedLeft);
        // The menu sits just below the bar, its item text in line with this item's header.
        popup.VerticalOffset = BarMenuOffset;
        popup.HorizontalOffset = -(OverlayPadding.Left + GlassBorder.BorderWidth);
        popup.IsLightDismissEnabled = true;
        popup.Bind(Popup.OverlayInputPassThroughElementProperty, new Binding
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(Menu) },
        });

        chrome.RegisterInNameScope(scope);
        header.RegisterInNameScope(scope);
        popup.RegisterInNameScope(scope);
        RegisterItems(popup, scope);
        return new Panel { Children = { chrome, popup } };
    }

    // --- Shared submenu popup ------------------------------------------------------------------

    private static Popup SubmenuPopup(PlacementMode placement)
    {
        var items = new ItemsPresenter { Name = ItemsPart };
        items.Bind(ItemsPresenter.ItemsPanelProperty, new TemplateBinding(ItemsControl.ItemsPanelProperty));
        Grid.SetIsSharedSizeScope(items, true);

        var surface = MenuSurface(items, maxWidthKey: "FlyoutThemeMaxWidth");
        surface.Padding = OverlayPadding;
        surface.Bind(GlassBorder.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusOverlay"));

        var popup = new Popup
        {
            Name = PopupPart,
            Placement = placement,
            IsLightDismissEnabled = false,
            WindowManagerAddShadowHint = false,
            Child = surface,
        };
        popup.Bind(Popup.IsOpenProperty, new TemplateBinding(MenuItem.IsSubMenuOpenProperty) { Mode = BindingMode.TwoWay });
        return popup;
    }

    private static void RegisterItems(Popup popup, INameScope scope)
    {
        var surface = (GlassBorder)popup.Child!;
        surface.RegisterInNameScope(scope);
        var items = ((ScrollViewer)surface.Child!).Content as ItemsPresenter;
        items?.RegisterInNameScope(scope);
    }

    /// <summary>The glass surface of a menu: a scroller around <paramref name="content"/>, fading in when shown.</summary>
    private static GlassBorder MenuSurface(Control content, string maxWidthKey)
    {
        var scroller = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content,
        };
        var surface = new GlassBorder { Name = OverlayParts.SurfacePart, Child = scroller }.FadesIn(OverlayParts.FadeInFastKey);
        surface.Bind(Layoutable.MaxWidthProperty, new DynamicResourceExtension(maxWidthKey));
        return surface;
    }

    // --- ContextMenu and MenuFlyoutPresenter -----------------------------------------------------

    /// <summary>A menu overlay (ContextMenu or MenuFlyoutPresenter): Fluent's behaviour and sizes, drawn as a glass surface.</summary>
    public static ControlTheme MenuOverlay(Type type, ControlTheme? fluentTheme) => new(type)
    {
        BasedOn = fluentTheme,
        Setters =
        {
            new Setter(TemplatedControl.PaddingProperty, OverlayPadding),
            new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusOverlay")),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ItemsControl>(BuildMenuOverlay)),
        },
    };

    private static Control BuildMenuOverlay(ItemsControl menu, INameScope scope)
    {
        var items = new ItemsPresenter { Name = ItemsPart };
        items.Bind(ItemsPresenter.ItemsPanelProperty, new TemplateBinding(ItemsControl.ItemsPanelProperty));
        Grid.SetIsSharedSizeScope(items, true);

        var scroller = new ScrollViewer { Content = items };
        scroller.Bind(ScrollViewer.HorizontalScrollBarVisibilityProperty, new TemplateBinding(ScrollViewer.HorizontalScrollBarVisibilityProperty));
        scroller.Bind(ScrollViewer.VerticalScrollBarVisibilityProperty, new TemplateBinding(ScrollViewer.VerticalScrollBarVisibilityProperty));

        var surface = new GlassBorder { Name = OverlayParts.SurfacePart, Child = scroller }.FadesIn(OverlayParts.FadeInFastKey);
        surface.Bind(Decorator.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        surface.Bind(GlassBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        surface.RegisterInNameScope(scope);
        items.RegisterInNameScope(scope);
        return surface;
    }

    // --- FlyoutPresenter -----------------------------------------------------------------------

    /// <summary>The padding of a flyout's content: <c>SpacingL</c>.</summary>
    public static readonly Thickness FlyoutPadding = new(Spacing.L);

    /// <summary>A flyout: Fluent's behaviour and sizes, drawn as a glass card with its content padded by <c>SpacingL</c>.</summary>
    public static ControlTheme FlyoutPresenter(ControlTheme? fluentTheme) => new(typeof(FlyoutPresenter))
    {
        BasedOn = fluentTheme,
        Setters =
        {
            new Setter(TemplatedControl.PaddingProperty, FlyoutPadding),
            new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusOverlay")),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<FlyoutPresenter>(BuildFlyout)),
        },
    };

    private static Control BuildFlyout(FlyoutPresenter flyout, INameScope scope)
    {
        var content = new ContentPresenter { Name = ContentPart };
        content.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        content.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        content.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        content.Bind(ContentPresenter.HorizontalContentAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        content.Bind(ContentPresenter.VerticalContentAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));

        var scroller = new ScrollViewer { Content = content };
        scroller.Bind(ScrollViewer.HorizontalScrollBarVisibilityProperty, new TemplateBinding(ScrollViewer.HorizontalScrollBarVisibilityProperty));
        scroller.Bind(ScrollViewer.VerticalScrollBarVisibilityProperty, new TemplateBinding(ScrollViewer.VerticalScrollBarVisibilityProperty));

        var surface = new GlassBorder { Name = OverlayParts.SurfacePart, Child = scroller }.FadesIn(OverlayParts.FadeInFastKey);
        surface.Bind(GlassBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        surface.RegisterInNameScope(scope);
        content.RegisterInNameScope(scope);
        return surface;
    }
}
