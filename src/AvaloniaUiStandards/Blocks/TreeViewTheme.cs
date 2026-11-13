using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The TreeView and TreeViewItem themes. Each item's row is the list item design (a squircle
/// pill inset from the tree's edges, list-medium hover, base-medium-low press, accent selection
/// with white text, density heights, the focus ring), full width at every level. The row holds
/// the indent (one caret width per level), a caret that turns from right to down as the item
/// expands, and the header. Node lines, when on, lie under the rows (<see cref="TreeLines"/>).
/// Keyboard and selection are Fluent's; clicks on the row expand and collapse it as
/// <see cref="TreeViewOptions"/> sets (<see cref="RowClicks"/>).
/// </summary>
internal static class TreeViewTheme
{
    public const string HeaderPart = "PART_Header";
    // Not Fluent's PART_HeaderPresenter: Fluent toggles on a double tap there, and RowClicks owns
    // the row's clicks instead.
    public const string HeaderPresenterPart = "PART_HeaderContent";
    public const string CaretPart = "PART_ExpandCollapseChevron";
    public const string ItemsPart = "PART_ItemsPresenter";

    /// <summary>The gap between the caret and the header (<c>SpacingXS</c>).</summary>
    public const double HeaderGap = Spacing.XS;

    /// <summary>
    /// Normal and Compact density: the indent per level (the caret's width, the icon size) and the
    /// row padding. The left padding is small because the caret's own box already insets its glyph.
    /// </summary>
    public static readonly (double Indent, Thickness Padding) Normal = (IconTheme.Normal, new Thickness(Spacing.XS, Spacing.XS, Spacing.M, Spacing.XS));
    public static readonly (double Indent, Thickness Padding) Compact = (IconTheme.Compact, new Thickness(2, 2, Spacing.S, 2));

    /// <summary>The caret's turn when expanded.</summary>
    private static readonly ITransform Collapsed = TransformOperations.Parse("rotate(0deg)");
    private static readonly ITransform Expanded = TransformOperations.Parse("rotate(90deg)");

    // Density values and the caret's transitions, under non-public keys.
    internal static readonly object IndentKey = new();
    internal static readonly object RowPaddingKey = new();
    internal static readonly object CaretTransitionsKey = new();

    // The caret button: only the icon, no chrome, never a Tab stop (the row has the keyboard).
    private static readonly ControlTheme CaretTheme = new(typeof(ToggleButton))
    {
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            new Setter(InputElement.FocusableProperty, false),
            new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center),
            new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(CaretTransitionsKey)),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ToggleButton>((_, _) =>
            {
                var icon = new Icon { Data = Icons.CaretRight };
                var box = new Border { Child = icon };
                box.Bind(Border.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
                return box;
            })),
        },
    };

    public static void ApplyDensity(IResourceDictionary resources, bool compact)
    {
        var sizes = compact ? Compact : Normal;
        resources[IndentKey] = sizes.Indent;
        resources[RowPaddingKey] = sizes.Padding;
    }

    /// <summary>The caret's turn over <paramref name="duration"/> (<c>MotionNormal</c>), or none when it is zero (reduce motion).</summary>
    public static Transitions CaretTransitions(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? []
        : [new TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = duration, Easing = easing }];

    /// <summary>The TreeView: transparent, its items inset by <c>SpacingXS</c>, no focus visual of its own (the rows carry the ring).</summary>
    public static ControlTheme TreeView(ControlTheme? fluentTheme) => new(typeof(TreeView))
    {
        BasedOn = fluentTheme,
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.PaddingProperty, new Thickness(Spacing.XS)),
            new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")),
            new Setter(TemplatedControl.TemplateProperty, ListItemTheme.ListTemplate),
        },
    };

    public static ControlTheme Item()
    {
        var theme = new ControlTheme(typeof(TreeViewItem));
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(RowPaddingKey)));
        theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(MinHeightKey)));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TreeViewItem>(BuildItem)));
        FocusRing.AddTo(theme);
        theme.Children.Add(ChromeMotion());

        // States are the row's own (its chrome's), not the item's: an item's bounds include its children's rows.
        theme.Children.Add(RowState(x => x, ":pointerover", "SystemControlHighlightListMediumBrush", null));
        theme.Children.Add(RowState(x => x, ":pressed", "SystemControlHighlightBaseMediumLowBrush", null));
        theme.Children.Add(RowState(x => x.Class(":selected"), null, "AccentFillColor", "AccentButtonForeground"));
        theme.Children.Add(RowState(x => x.Class(":selected"), ":pointerover", "AccentFillColorPointerOver", "AccentButtonForeground"));
        theme.Children.Add(RowState(x => x.Class(":selected"), ":pressed", "AccentFillColorPressed", "AccentButtonForeground"));
        theme.Children.Add(RowState(x => x.Class(":disabled"), null, Brushes.Transparent, "SystemControlDisabledBaseMediumLowBrush"));
        theme.Children.Add(RowState(x => x.Class(":selected").Class(":disabled"), null, "AccentButtonBackgroundDisabled", "AccentButtonForegroundDisabled"));

        // The caret turns down when expanded, and leaves its room empty on items with no children.
        theme.Children.Add(new Avalonia.Styling.Style(x => Caret(x.Nesting()))
        {
            Setters = { new Setter(Visual.RenderTransformProperty, Collapsed) },
        });
        // (TreeViewItem has no :expanded; the caret is checked while the item is expanded.)
        theme.Children.Add(new Avalonia.Styling.Style(x => Caret(x.Nesting()).Class(":checked"))
        {
            Setters = { new Setter(Visual.RenderTransformProperty, Expanded) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => Caret(x.Nesting().Class(":empty")))
        {
            Setters =
            {
                new Setter(Visual.OpacityProperty, 0d),
                new Setter(InputElement.IsHitTestVisibleProperty, false),
            },
        });
        return theme;
    }

    /// <summary>A row state: fill on the chrome, and the text colour (null: inherited) for the caret and header.</summary>
    private static Avalonia.Styling.Style RowState(Func<Selector, Selector> item, string? rowClass, object background, object? foreground)
    {
        var style = new Avalonia.Styling.Style(x =>
        {
            var chrome = Chrome(item(x.Nesting()));
            return rowClass is null ? chrome : chrome.Class(rowClass);
        });
        style.Setters.Add(new Setter(SquircleBorder.BackgroundProperty, Value(background)));
        if (foreground is not null)
            style.Setters.Add(new Setter(TextElement.ForegroundProperty, Value(foreground)));
        return style;
    }

    private static Selector Caret(Selector item) => item.Template().OfType<ToggleButton>().Name(CaretPart);

    private static Control BuildItem(TreeViewItem item, INameScope scope)
    {
        var indent = new TreeIndent();
        indent.Bind(TreeIndent.LevelProperty, new TemplateBinding(TreeViewItem.LevelProperty));
        indent.Bind(TreeIndent.IndentProperty, new DynamicResourceExtension(IndentKey));

        var caret = new ToggleButton { Name = CaretPart, Theme = CaretTheme };
        caret.Bind(ToggleButton.IsCheckedProperty, new TemplateBinding(TreeViewItem.IsExpandedProperty) { Mode = BindingMode.TwoWay });
        Grid.SetColumn(caret, 1);

        var header = new ContentPresenter
        {
            Name = HeaderPresenterPart,
            Margin = new Thickness(HeaderGap, 0, 0, 0),
            VerticalContentAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        header.Bind(ContentPresenter.ContentProperty, new TemplateBinding(HeaderedItemsControl.HeaderProperty));
        header.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(HeaderedItemsControl.HeaderTemplateProperty));
        Grid.SetColumn(header, 2);

        var row = new Grid { Name = HeaderPart, ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"), Children = { indent, caret, header } };

        var chrome = new SquircleBorder { Name = ChromePart, Child = row };
        chrome.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        chrome.Bind(SquircleBorder.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        chrome.Bind(SquircleBorder.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        chrome.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        chrome.Bind(Decorator.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        chrome.Bind(Layoutable.MinHeightProperty, new TemplateBinding(Layoutable.MinHeightProperty));
        TrackPress(chrome);
        RowClicks.Attach(item, chrome, caret);

        var items = new ItemsPresenter { Name = ItemsPart };
        items.Bind(ItemsPresenter.ItemsPanelProperty, new TemplateBinding(ItemsControl.ItemsPanelProperty));
        items.Bind(Visual.IsVisibleProperty, new TemplateBinding(TreeViewItem.IsExpandedProperty));

        // The node lines lie under the row and the children's rows (TreeViewOptions.ShowLines).
        var lines = new TreeLines(item, chrome, caret, header);
        lines.Bind(TreeLines.StrokeProperty, Resource("SystemControlForegroundBaseLowBrush"));

        foreach (var part in new Control[] { caret, header, row, chrome, items, lines })
            part.RegisterInNameScope(scope);
        return new Panel { Children = { lines, new StackPanel { Children = { FocusRing.Wrap(chrome, scope), items } } } };
    }

    // The row shows :pressed while the left button is held on it (the item has no pressed state of its own).
    private static void TrackPress(SquircleBorder chrome)
    {
        void Set(bool pressed) => ((IPseudoClasses)chrome.Classes).Set(":pressed", pressed);
        chrome.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (e.GetCurrentPoint(chrome).Properties.IsLeftButtonPressed)
                Set(true);
        }, handledEventsToo: true);
        chrome.AddHandler(InputElement.PointerReleasedEvent, (_, _) => Set(false), handledEventsToo: true);
        chrome.AddHandler(InputElement.PointerCaptureLostEvent, (_, _) => Set(false), handledEventsToo: true);
        chrome.AddHandler(InputElement.PointerExitedEvent, (_, _) => Set(false), handledEventsToo: true);
    }
}

/// <summary>
/// Expands and collapses a row on the clicks <see cref="TreeViewOptions.ExpandOnProperty"/> and
/// <see cref="TreeViewOptions.CollapseOnProperty"/> set. A click anywhere on the row counts except
/// on the caret, which toggles on every click of its own. Rows without children, clicks with a
/// modifier key (extending a selection) and clicks the row's content handles are ignored. One
/// gesture toggles at most once: when the first click of a double click toggled the row, the
/// double click does nothing.
/// </summary>
internal static class RowClicks
{
    private const KeyModifiers Selecting = KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Meta;

    public static void Attach(TreeViewItem item, Control row, Control caret)
    {
        // Whether the first click of the current click sequence toggled the row.
        var toggledByClick = false;
        row.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (e.ClickCount % 2 == 1)
                toggledByClick = false;
        }, handledEventsToo: true);
        row.AddHandler(InputElement.TappedEvent, (_, e) =>
        {
            if (Toggle(item, caret, e, TreeViewClickAction.SingleClick))
                toggledByClick = true;
        });
        row.AddHandler(InputElement.DoubleTappedEvent, (_, e) =>
        {
            if (toggledByClick)
            {
                // The first click already toggled: the gesture is done.
                toggledByClick = false;
                e.Handled = true;
                return;
            }
            Toggle(item, caret, e, TreeViewClickAction.DoubleClick);
        });
    }

    private static bool Toggle(TreeViewItem item, Control caret, TappedEventArgs e, TreeViewClickAction click)
    {
        if (item.ItemCount == 0 || (e.KeyModifiers & Selecting) != 0
            || (e.Source is Visual source && (source == caret || caret.IsVisualAncestorOf(source))))
            return false;
        var actions = item.GetValue(item.IsExpanded ? TreeViewOptions.CollapseOnProperty : TreeViewOptions.ExpandOnProperty);
        if ((actions & click) == 0)
            return false;
        item.SetCurrentValue(TreeViewItem.IsExpandedProperty, !item.IsExpanded);
        e.Handled = true;
        return true;
    }
}

/// <summary>The indent of a tree row: <see cref="Level"/> times <see cref="Indent"/> wide.</summary>
internal sealed class TreeIndent : Control
{
    public static readonly StyledProperty<int> LevelProperty =
        AvaloniaProperty.Register<TreeIndent, int>(nameof(Level));

    public static readonly StyledProperty<double> IndentProperty =
        AvaloniaProperty.Register<TreeIndent, double>(nameof(Indent));

    static TreeIndent()
    {
        AffectsMeasure<TreeIndent>(LevelProperty, IndentProperty);
    }

    public int Level
    {
        get => GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public double Indent
    {
        get => GetValue(IndentProperty);
        set => SetValue(IndentProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(Level * Indent, 0);
}
