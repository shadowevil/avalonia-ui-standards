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
/// The TabControl, TabItem, TabStrip and TabStripItem themes. Tab headers are text (and an
/// optional icon) with no fill, in Fluent's tab text colours; the selected tab is marked by one
/// accent underline that slides between headers, on a hairline running along the strip. The
/// content sits directly on the surface below, aligned with the header text.
/// </summary>
/// <remarks>
/// TabControl template: a <c>DockPanel</c> with the strip (docked by <c>TabStripPlacement</c>:
/// <c>Border#PART_StripLine</c> under a <see cref="SelectionIndicator"/> holding
/// <c>SquircleBorder#PART_Indicator</c> and <c>ItemsPresenter#PART_ItemsPresenter</c>) and
/// Fluent's two content hosts, <c>PART_SelectedContentHost</c> and <c>PART_SelectedContentHost2</c>
/// (for <c>PageTransition</c>). Tab template: <c>SquircleBorder#PART_Chrome</c> around the icon
/// and <c>ContentPresenter#PART_ContentPresenter</c>, with the focus ring.
/// </remarks>
internal static class TabThemes
{
    public const string IndicatorPart = "PART_Indicator";
    public const string StripLinePart = "PART_StripLine";

    /// <summary>The underline's thickness.</summary>
    public const double IndicatorThickness = 3;

    /// <summary>The hairline along the strip.</summary>
    public const double LineThickness = 1;

    // The content's padding by density: the tab's side padding, so the content lines up with the header text.
    internal static readonly object ContentPaddingKey = new();

    public static ControlTheme TabControl() => new(typeof(TabControl))
    {
        Setters =
        {
            new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(ContentPaddingKey)),
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            new Setter(ItemsControl.ItemsPanelProperty, new FuncTemplate<Panel?>(() => new WrapPanel())),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TabControl>(BuildTabControl)),
            new Setter(Visual.ClipToBoundsProperty, false),
        },
        Children =
        {
            SelectionIndicator.Motion(),
            new Avalonia.Styling.Style(x => x.Nesting().PropertyEquals(Avalonia.Controls.TabControl.TabStripPlacementProperty, Dock.Left)
                .Template().OfType<ItemsPresenter>().Name("PART_ItemsPresenter").Child().OfType<WrapPanel>())
            {
                Setters = { new Setter(WrapPanel.OrientationProperty, Orientation.Vertical) },
            },
            new Avalonia.Styling.Style(x => x.Nesting().PropertyEquals(Avalonia.Controls.TabControl.TabStripPlacementProperty, Dock.Right)
                .Template().OfType<ItemsPresenter>().Name("PART_ItemsPresenter").Child().OfType<WrapPanel>())
            {
                Setters = { new Setter(WrapPanel.OrientationProperty, Orientation.Vertical) },
            },
        },
    };

    public static ControlTheme TabStrip() => new(typeof(TabStrip))
    {
        Setters =
        {
            new Setter(ItemsControl.ItemsPanelProperty, new FuncTemplate<Panel?>(() => new WrapPanel())),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TabStrip>((strip, scope) => BuildStrip(strip, scope, () => Dock.Top))),
            // The control clips by default; the headers' focus rings lie partly outside it.
            new Setter(Visual.ClipToBoundsProperty, false),
        },
        Children = { SelectionIndicator.Motion() },
    };

    /// <summary>A tab header theme (TabItem or TabStripItem), in Fluent's TabItem text colours.</summary>
    public static ControlTheme Tab(Type type)
    {
        var theme = new ControlTheme(type);
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(PaddingKey)));
        theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(MinHeightKey)));
        theme.Setters.Add(new Setter(TemplatedControl.FontSizeProperty, Resource("FontSizeBody")));
        theme.Setters.Add(new Setter(TemplatedControl.FontWeightProperty, FontWeight.Normal));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Resource("TabItemHeaderForegroundUnselected")));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ContentControl>(BuildTab)));
        FocusRing.AddTo(theme);
        theme.Children.Add(ChromeMotion());
        theme.Children.Add(Text(null, "TabItemHeaderForegroundUnselected"));
        theme.Children.Add(Text(":pointerover", "TabItemHeaderForegroundUnselectedPointerOver"));
        theme.Children.Add(Text(":pressed", "TabItemHeaderForegroundUnselectedPressed"));
        theme.Children.Add(Text(":selected", "TabItemHeaderForegroundSelected"));
        theme.Children.Add(Text(":selected:pointerover", "TabItemHeaderForegroundSelectedPointerOver"));
        theme.Children.Add(Text(":selected:pressed", "TabItemHeaderForegroundSelectedPressed"));
        theme.Children.Add(Text(":disabled", "TabItemHeaderForegroundDisabled"));
        return theme;
    }

    /// <summary>The header text colour in a state: <paramref name="state"/> is null (rest) or pseudo-classes, e.g. ":selected:pointerover".</summary>
    private static Avalonia.Styling.Style Text(string? state, string key) => new(x =>
    {
        var selector = x.Nesting();
        foreach (var pseudo in (state ?? "").Split(':', StringSplitOptions.RemoveEmptyEntries))
            selector = selector.Class(":" + pseudo);
        return Content(selector);
    })
    {
        Setters = { new Setter(ContentPresenter.ForegroundProperty, Resource(key)) },
    };

    private static Control BuildTabControl(TabControl control, INameScope scope)
    {
        var strip = BuildStrip(control, scope, () => control.TabStripPlacement);
        strip.Bind(DockPanel.DockProperty, new TemplateBinding(Avalonia.Controls.TabControl.TabStripPlacementProperty));

        ContentPresenter Host(string name)
        {
            var host = new ContentPresenter { Name = name };
            host.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
            host.Bind(ContentPresenter.HorizontalContentAlignmentProperty, new TemplateBinding(Avalonia.Controls.TabControl.HorizontalContentAlignmentProperty));
            host.Bind(ContentPresenter.VerticalContentAlignmentProperty, new TemplateBinding(Avalonia.Controls.TabControl.VerticalContentAlignmentProperty));
            host.RegisterInNameScope(scope);
            return host;
        }
        var second = Host("PART_SelectedContentHost2");
        var first = Host("PART_SelectedContentHost");
        first.Bind(ContentPresenter.ContentProperty, new TemplateBinding(Avalonia.Controls.TabControl.SelectedContentProperty));
        first.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(Avalonia.Controls.TabControl.SelectedContentTemplateProperty));

        var border = new Border
        {
            Child = new DockPanel { Children = { strip, new Panel { Children = { second, first } } } },
        };
        border.Bind(Border.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        border.Bind(Border.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        border.Bind(Border.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        border.Bind(Border.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        return border;
    }

    /// <summary>The strip: the hairline, the sliding underline and the headers.</summary>
    private static Panel BuildStrip(SelectingItemsControl control, INameScope scope, Func<Dock> placement)
    {
        var items = new ItemsPresenter { Name = "PART_ItemsPresenter" };
        items.Bind(ItemsPresenter.ItemsPanelProperty, new TemplateBinding(ItemsControl.ItemsPanelProperty));

        var indicator = new SquircleBorder { Name = IndicatorPart, CornerRadius = new CornerRadius(IndicatorThickness / 2) };
        indicator.Bind(SquircleBorder.BackgroundProperty, Resource("TabItemHeaderSelectedPipeFill"));

        // The underline spans the header's content (inside its padding), on the edge facing the
        // content; beside a vertical strip it runs on the strip's inner edge, over the hairline.
        var follower = new SelectionIndicator(control, indicator, items, (container, rect, strip) =>
        {
            var padding = (container as TemplatedControl)?.Padding ?? default;
            return placement() switch
            {
                Dock.Bottom => new Rect(rect.X + padding.Left, rect.Y, Math.Max(0, rect.Width - padding.Left - padding.Right), IndicatorThickness),
                Dock.Left => new Rect(strip.Width - IndicatorThickness, rect.Y + padding.Top, IndicatorThickness, Math.Max(0, rect.Height - padding.Top - padding.Bottom)),
                Dock.Right => new Rect(0, rect.Y + padding.Top, IndicatorThickness, Math.Max(0, rect.Height - padding.Top - padding.Bottom)),
                _ => new Rect(rect.X + padding.Left, rect.Bottom - IndicatorThickness, Math.Max(0, rect.Width - padding.Left - padding.Right), IndicatorThickness),
            };
        });

        var line = new Border { Name = StripLinePart, IsHitTestVisible = false };
        line.Bind(Border.BackgroundProperty, Resource("SystemControlForegroundBaseLowBrush"));
        void PlaceLine()
        {
            var dock = placement();
            var vertical = dock is Dock.Left or Dock.Right;
            line.Width = vertical ? LineThickness : double.NaN;
            line.Height = vertical ? double.NaN : LineThickness;
            line.HorizontalAlignment = dock switch { Dock.Left => HorizontalAlignment.Right, Dock.Right => HorizontalAlignment.Left, _ => HorizontalAlignment.Stretch };
            line.VerticalAlignment = dock switch { Dock.Top => VerticalAlignment.Bottom, Dock.Bottom => VerticalAlignment.Top, _ => VerticalAlignment.Stretch };
            // Beside a vertical strip the headers keep clear of the underline, so a focus ring never crosses it.
            items.Margin = dock switch { Dock.Left => new Thickness(0, 0, Spacing.S, 0), Dock.Right => new Thickness(Spacing.S, 0, 0, 0), _ => default };
        }
        PlaceLine();

        items.RegisterInNameScope(scope);
        indicator.RegisterInNameScope(scope);
        line.RegisterInNameScope(scope);
        // Nothing in the strip clips, so a focused header's ring shows outside the strip's edges.
        items.ClipToBounds = false;
        follower.ClipToBounds = false;
        var strip = new Panel { ClipToBounds = false, Children = { line, follower } };

        // The placement is followed while the strip is in the tree.
        if (control is TabControl tabs)
        {
            void OnTabsChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Property != Avalonia.Controls.TabControl.TabStripPlacementProperty)
                    return;
                PlaceLine();
                follower.InvalidateArrange();
            }
            strip.AttachedToVisualTree += (_, _) =>
            {
                tabs.PropertyChanged += OnTabsChanged;
                PlaceLine();
            };
            strip.DetachedFromVisualTree += (_, _) => tabs.PropertyChanged -= OnTabsChanged;
        }
        return strip;
    }

    private static Control BuildTab(ContentControl tab, INameScope scope)
    {
        // The header, after the tab's icon when it has one; the chrome holds the padding.
        var presenter = new ContentPresenter { Name = ContentPart, RecognizesAccessKey = true, VerticalAlignment = VerticalAlignment.Center };
        // A TabItem shows its Header (its Content is the page); a TabStripItem shows its Content.
        var headered = tab is HeaderedContentControl;
        presenter.Bind(ContentPresenter.ContentProperty,
            new TemplateBinding(headered ? HeaderedContentControl.HeaderProperty : ContentControl.ContentProperty));
        presenter.Bind(ContentPresenter.ContentTemplateProperty,
            new TemplateBinding(headered ? HeaderedContentControl.HeaderTemplateProperty : ContentControl.ContentTemplateProperty));

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Spacing.S };
        if (tab is TabItem)
        {
            var icon = new ContentPresenter { Name = "PART_IconPresenter", VerticalAlignment = VerticalAlignment.Center };
            icon.Bind(ContentPresenter.ContentProperty, new TemplateBinding(TabItem.IconProperty));
            icon.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(TabItem.IconTemplateProperty));
            icon.Bind(Visual.IsVisibleProperty, new TemplateBinding(TabItem.IconProperty) { Converter = Avalonia.Data.Converters.ObjectConverters.IsNotNull });
            icon.Bind(ContentPresenter.ForegroundProperty, presenter.GetObservable(ContentPresenter.ForegroundProperty).ToBinding());
            row.Children.Add(icon);
            icon.RegisterInNameScope(scope);
        }
        row.Children.Add(presenter);

        var chrome = CreateChrome(row);
        chrome.Bind(Decorator.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        chrome.RegisterInNameScope(scope);
        presenter.RegisterInNameScope(scope);
        return FocusRing.Wrap(chrome, scope);
    }
}
