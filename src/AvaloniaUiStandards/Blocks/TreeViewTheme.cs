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
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The TreeView and TreeViewItem themes. Each item's row spans the tree's width and holds the
/// indent (one caret width per level), a caret that turns from right to down as the item expands,
/// and the content pill: the list item design (a squircle pill, list-medium hover, base-medium-low
/// press, accent selection with white text, density heights, the focus ring, the optional accent
/// flag) from the content's start to the row's right edge, so the carets and the node lines
/// (<see cref="TreeLines"/>) stay visible beside it. Hover and press are the whole row's, shown on the pill.
/// Keyboard and selection are Fluent's; clicks on the row expand and collapse it as
/// <see cref="TreeViewOptions"/> sets (<see cref="RowClicks"/>).
/// </summary>
internal static class TreeViewTheme
{
    /// <summary>The whole row: it takes the pointer (hover, press, clicks) for the pill.</summary>
    public const string RowPart = "PART_Row";
    public const string HeaderPart = "PART_Header";
    // Not Fluent's PART_HeaderPresenter: Fluent toggles on a double tap there, and RowClicks owns
    // the row's clicks instead.
    public const string HeaderContentPart = "PART_HeaderContent";
    public const string CaretPart = "PART_ExpandCollapseChevron";
    public const string ItemsPart = "PART_ItemsPresenter";

    /// <summary>
    /// Normal and Compact density: the indent per level (the caret's width, the icon size), the
    /// row's inset before the indent, and the pill's padding (the item's <c>Padding</c>). The inset
    /// is small because the caret's own box already insets its glyph.
    /// </summary>
    public static readonly (double Indent, double Inset, Thickness Padding) Normal = (IconTheme.Normal, Spacing.XS, new Thickness(Spacing.S, Spacing.XS, Spacing.M, Spacing.XS));
    public static readonly (double Indent, double Inset, Thickness Padding) Compact = (IconTheme.Compact, 2, new Thickness(Spacing.S, 2, Spacing.S, 2));

    /// <summary>The caret's turn when expanded.</summary>
    private static readonly ITransform Collapsed = TransformOperations.Parse("rotate(0deg)");
    private static readonly ITransform Expanded = TransformOperations.Parse("rotate(90deg)");

    // Density values and the caret's transitions, under non-public keys.
    internal static readonly object IndentKey = new();
    internal static readonly object RowInsetKey = new();
    internal static readonly object PillPaddingKey = new();
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
        resources[RowInsetKey] = new Thickness(sizes.Inset, 0, 0, 0);
        resources[PillPaddingKey] = sizes.Padding;
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
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(PillPaddingKey)));
        theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(MinHeightKey)));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TreeViewItem>(BuildItem)));
        FocusRing.AddTo(theme);
        theme.Children.Add(ChromeMotion());

        // States are the row's own, shown on its pill, not the item's: an item's bounds include its children's rows.
        theme.Children.Add(RowState(x => x, ":pointerover", "SystemControlHighlightListMediumBrush", null));
        theme.Children.Add(RowState(x => x, ":pressed", "SystemControlHighlightBaseMediumLowBrush", null));
        theme.Children.Add(RowState(x => x.Class(":selected"), null, "AccentFillColor", "AccentButtonForeground"));
        theme.Children.Add(RowState(x => x.Class(":selected"), ":pointerover", "AccentFillColorPointerOver", "AccentButtonForeground"));
        theme.Children.Add(RowState(x => x.Class(":selected"), ":pressed", "AccentFillColorPressed", "AccentButtonForeground"));
        theme.Children.Add(RowState(x => x.Class(":disabled"), null, Brushes.Transparent, "SystemControlDisabledBaseMediumLowBrush"));
        theme.Children.Add(RowState(x => x.Class(":selected").Class(":disabled"), null, "AccentButtonBackgroundDisabled", "AccentButtonForegroundDisabled"));
        // The accent flag goes on the pill's leading edge.
        AccentFlag.AddTo(theme);
        // The caret lies outside the pill: it dims with a disabled row itself.
        theme.Children.Add(new Avalonia.Styling.Style(x => Caret(x.Nesting().Class(":disabled")))
        {
            Setters = { new Setter(TemplatedControl.ForegroundProperty, Resource("SystemControlDisabledBaseMediumLowBrush")) },
        });

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

    /// <summary>A row state (the item's, and the row's own pointer class): fill on the pill, and the text colour (null: inherited) for the header.</summary>
    private static Avalonia.Styling.Style RowState(Func<Selector, Selector> item, string? rowClass, object background, object? foreground)
    {
        var style = new Avalonia.Styling.Style(x => rowClass is null
            ? Chrome(item(x.Nesting()))
            : item(x.Nesting()).Template().OfType<Border>().Name(RowPart).Class(rowClass)
                .Descendant().OfType<SquircleBorder>().Name(ChromePart));
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
            Name = HeaderContentPart,
            VerticalContentAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        header.Bind(ContentPresenter.ContentProperty, new TemplateBinding(HeaderedItemsControl.HeaderProperty));
        header.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(HeaderedItemsControl.HeaderTemplateProperty));

        // The pill: from the content's start (after the caret) to the row's right edge.
        var chrome = new SquircleBorder { Name = ChromePart, Child = header };
        chrome.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        chrome.Bind(SquircleBorder.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        chrome.Bind(SquircleBorder.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        chrome.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        chrome.Bind(Decorator.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        chrome.Bind(Layoutable.MinHeightProperty, new TemplateBinding(Layoutable.MinHeightProperty));
        var pill = FocusRing.Wrap(AccentFlag.Add(chrome, scope), scope);
        Grid.SetColumn(pill, 2);

        var grid = new Grid { Name = HeaderPart, ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"), Children = { indent, caret, pill } };

        // The whole row takes the pointer, so a click anywhere on it selects and counts for the click modes.
        var row = new Border { Name = RowPart, Background = Brushes.Transparent, Child = grid };
        row.Bind(Decorator.PaddingProperty, new DynamicResourceExtension(RowInsetKey));
        TrackPress(row);
        RowClicks.Attach(item, row);

        var items = new ItemsPresenter { Name = ItemsPart };
        items.Bind(ItemsPresenter.ItemsPanelProperty, new TemplateBinding(ItemsControl.ItemsPanelProperty));
        items.Bind(Visual.IsVisibleProperty, new TemplateBinding(TreeViewItem.IsExpandedProperty));

        // The node lines lie under the rows (TreeViewOptions.ShowLines), beside the pills.
        var lines = new TreeLines(item, row, caret, chrome);
        lines.Bind(TreeLines.StrokeProperty, Resource("SystemControlForegroundBaseLowBrush"));

        foreach (var part in new Control[] { caret, header, grid, chrome, row, items, lines })
            part.RegisterInNameScope(scope);
        return new Panel { Children = { lines, new StackPanel { Children = { row, items } } } };
    }

    // The row shows :pressed while the left button is held on it (the item has no pressed state of its own).
    private static void TrackPress(Border row)
    {
        void Set(bool pressed) => ((IPseudoClasses)row.Classes).Set(":pressed", pressed);
        row.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (e.GetCurrentPoint(row).Properties.IsLeftButtonPressed)
                Set(true);
        }, handledEventsToo: true);
        row.AddHandler(InputElement.PointerReleasedEvent, (_, _) => Set(false), handledEventsToo: true);
        row.AddHandler(InputElement.PointerCaptureLostEvent, (_, _) => Set(false), handledEventsToo: true);
        row.AddHandler(InputElement.PointerExitedEvent, (_, _) => Set(false), handledEventsToo: true);
    }
}

/// <summary>
/// Expands and collapses a row on the clicks <see cref="TreeViewOptions.ExpandOnProperty"/> and
/// <see cref="TreeViewOptions.CollapseOnProperty"/> set. A click anywhere on the row counts, inside
/// the pill or not, except where the press was handled inside the row: the caret (which toggles on
/// every click of its own) and content such as a button in the header. Rows without children and
/// clicks with a modifier key (extending a selection) are ignored too. One gesture toggles at most
/// once: when the first click of a double click toggled the row, the double click does nothing.
/// </summary>
internal static class RowClicks
{
    private const KeyModifiers Selecting = KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Meta;

    public static void Attach(TreeViewItem item, Control row)
    {
        // Whether the first click of the current click sequence toggled the row.
        var toggledByClick = false;
        // Whether the latest press was handled inside the row (Tapped and DoubleTapped follow even a handled press).
        var pressHandled = false;
        row.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            pressHandled = e.Handled;
            if (e.ClickCount % 2 == 1)
                toggledByClick = false;
        }, handledEventsToo: true);
        row.AddHandler(InputElement.TappedEvent, (_, e) =>
        {
            if (!pressHandled && Toggle(item, e, TreeViewClickAction.SingleClick))
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
            if (!pressHandled)
                Toggle(item, e, TreeViewClickAction.DoubleClick);
        });
    }

    private static bool Toggle(TreeViewItem item, TappedEventArgs e, TreeViewClickAction click)
    {
        if (item.ItemCount == 0 || (e.KeyModifiers & Selecting) != 0)
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
