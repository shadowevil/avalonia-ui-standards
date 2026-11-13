using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The Sidebar, SidebarItem and SidebarHeader themes and the sidebar's values. Rows are the list
/// item design (squircle, accent selection, density heights) with the icon at a fixed place: centred
/// in the icon rail, and where it stays when the labels show, so nothing moves as the sidebar
/// collapses. The overlay is drawn on glass.
/// </summary>
internal static class SidebarTheme
{
    public const string OverlaySurfacePart = "PART_OverlaySurface";
    public const string IconPart = "PART_Icon";

    /// <summary>The gap from the window's left edge to the sidebar (<c>SpacingS</c>, 0022).</summary>
    public const double EdgeGap = Spacing.S;

    /// <summary>The expanded sidebar's width, items' inset included.</summary>
    public const double ExpandedWidth = 240;

    /// <summary>The gap between a row's icon and its label (<c>SpacingS</c>).</summary>
    public const double LabelGap = Spacing.S;

    /// <summary>
    /// Normal and Compact density: the icon rail's width and the row padding. The rail holds a
    /// row inset 4 on each side; the row's side padding centres the icon (20 or 16) in it.
    /// </summary>
    public static readonly (double Rail, Thickness Padding) Normal = (48, new Thickness(10, Spacing.XS));
    public static readonly (double Rail, Thickness Padding) Compact = (40, new Thickness(Spacing.S, 2));

    // Density values and the width transitions, under non-public keys.
    internal static readonly object RailWidthKey = new();
    internal static readonly object RowPaddingKey = new();
    internal static readonly object WidthTransitionsKey = new();

    public static void ApplyDensity(IResourceDictionary resources, bool compact)
    {
        var sizes = compact ? Compact : Normal;
        resources[RailWidthKey] = sizes.Rail;
        resources[RowPaddingKey] = sizes.Padding;
    }

    /// <summary>The width transitions over <paramref name="duration"/> (<c>MotionNormal</c>), or none when it is zero (reduce motion).</summary>
    public static Transitions WidthTransitions(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? []
        :
        [
            new DoubleTransition { Property = Sidebar.LayoutWidthProperty, Duration = duration, Easing = easing },
            new DoubleTransition { Property = Sidebar.PaneWidthProperty, Duration = duration, Easing = easing },
        ];

    public static ControlTheme Create() => new(typeof(Sidebar))
    {
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.PaddingProperty, new Thickness(Spacing.XS)),
            new Setter(Visual.ClipToBoundsProperty, false),
            new Setter(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled),
            new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(WidthTransitionsKey)),
            new Setter(Sidebar.RailWidthProperty, new DynamicResourceExtension(RailWidthKey)),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Sidebar>(BuildSidebar)),
        },
    };

    /// <summary>The row: the list item theme with the icon-and-label template and the sidebar's padding.</summary>
    public static ControlTheme Item()
    {
        var theme = ListItemTheme.Item(typeof(SidebarItem));
        theme.Setters.Remove(theme.Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.TemplateProperty));
        theme.Setters.Remove(theme.Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.PaddingProperty));
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(RowPaddingKey)));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<SidebarItem>(BuildItem)));

        // The label fades out in the icon rail.
        theme.Children.Add(new Avalonia.Styling.Style(x => ButtonParts.Content(x.Nesting()))
        {
            Setters = { new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(ButtonParts.FadeTransitionsKey)) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => ButtonParts.Content(x.Nesting().Class(":collapsed")))
        {
            Setters = { new Setter(Visual.OpacityProperty, 0d) },
        });
        return theme;
    }

    /// <summary>The section header: caption text in the secondary colour, aligned with the row icons, at the bottom of a row-high band.</summary>
    public static ControlTheme Header() => new(typeof(SidebarHeader))
    {
        Setters =
        {
            new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(ButtonParts.MinHeightKey)),
            new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(RowPaddingKey)),
            new Setter(TemplatedControl.FontSizeProperty, new DynamicResourceExtension("FontSizeCaption")),
            new Setter(TemplatedControl.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseMediumBrush")),
            new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Bottom),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<SidebarHeader>(BuildHeader)),
        },
        Children =
        {
            new Avalonia.Styling.Style(x => ButtonParts.Content(x.Nesting()))
            {
                Setters = { new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(ButtonParts.FadeTransitionsKey)) },
            },
            new Avalonia.Styling.Style(x => ButtonParts.Content(x.Nesting().Class(":collapsed")))
            {
                Setters = { new Setter(Visual.OpacityProperty, 0d) },
            },
        },
    };

    // The rows sit EdgeGap in from the window's edge; the glass behind them shows only for the overlay.
    private static Control BuildSidebar(Sidebar sidebar, INameScope scope)
    {
        var items = new ItemsPresenter { Name = "PART_ItemsPresenter" };
        items.Bind(ItemsPresenter.ItemsPanelProperty, new TemplateBinding(ItemsControl.ItemsPanelProperty));
        items.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.PaddingProperty));

        var scroller = new ScrollViewer { Name = "PART_ScrollViewer", Content = items, Margin = new Thickness(EdgeGap, 0, 0, 0) };
        foreach (var property in new AvaloniaProperty[]
                 {
                     ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollViewer.VerticalScrollBarVisibilityProperty,
                     ScrollShadows.IsEnabledProperty,
                 })
            scroller.Bind(property, new TemplateBinding(property));

        var surface = new GlassBorder { Name = OverlaySurfacePart, Margin = new Thickness(EdgeGap, 0, 0, 0), IsVisible = false };

        surface.RegisterInNameScope(scope);
        scroller.RegisterInNameScope(scope);
        items.RegisterInNameScope(scope);
        var root = new Panel { Children = { surface, scroller } };
        root.Bind(Panel.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        return root;
    }

    // The icon takes the label's colour, so it follows every row state (selected: white).
    private static Control BuildItem(SidebarItem item, INameScope scope)
    {
        var label = ButtonParts.CreatePresenter();
        label.Padding = default;
        label.Margin = new Thickness(LabelGap, 0, 0, 0);
        label.TextTrimming = TextTrimming.CharacterEllipsis;

        var icon = new Icon { Name = IconPart, VerticalAlignment = VerticalAlignment.Center };
        icon.Bind(PathIcon.DataProperty, new TemplateBinding(SidebarItem.IconProperty));
        icon.Bind(TemplatedControl.ForegroundProperty, label.GetObservable(ContentPresenter.ForegroundProperty).ToBinding());
        DockPanel.SetDock(icon, Dock.Left);

        var chrome = ButtonParts.CreateChrome(new DockPanel { Children = { icon, label } });
        chrome.Bind(Decorator.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));

        chrome.RegisterInNameScope(scope);
        label.RegisterInNameScope(scope);
        icon.RegisterInNameScope(scope);
        return FocusRing.Wrap(chrome, scope);
    }

    private static Control BuildHeader(SidebarHeader header, INameScope scope)
    {
        var presenter = ButtonParts.CreatePresenter();
        presenter.TextTrimming = TextTrimming.CharacterEllipsis;
        presenter.RegisterInNameScope(scope);
        return presenter;
    }
}
