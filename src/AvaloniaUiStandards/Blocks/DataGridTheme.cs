using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The DataGrid themes (DataGrid, DataGridRow, DataGridCell, DataGridColumnHeader,
/// DataGridRowHeader), restyling the DataGrid package's Fluent theme. Rows are flat full-width
/// bands at the list item height with the list item's hover and accent selection; horizontal
/// hairlines in the border colour separate them. Column headers are Body strong text in the
/// secondary colour with a sort caret, a hover fill and a resize grip that shows near a
/// resizable edge. The current cell shows the focus ring, inset, while the grid is used from the
/// keyboard; editing turns the cell into a TextInput field. The scroll bars are the overlay bars.
/// </summary>
internal static class DataGridTheme
{
    public const string SortIconPart = "PART_SortIcon";
    public const string GripLeftPart = "PART_ResizeGripLeft";
    public const string GripRightPart = "PART_ResizeGripRight";
    public const string InvalidPart = "PART_Invalid";
    public const string SeparatorPart = "PART_HeaderSeparator";
    public const string RingPart = FocusRing.Part;

    /// <summary>The cell text and edit field themes, under the keys the DataGrid package looks up.</summary>
    public const string CellTextBlockThemeKey = "DataGridCellTextBlockTheme";
    public const string CellTextBoxThemeKey = "DataGridCellTextBoxTheme";

    /// <summary>The width of the zone at a header's edge that resizes a column (the package's resize region).</summary>
    public const double ResizeZone = 5;

    /// <summary>The resize grip's width; its length is the header height less <c>SpacingXS</c> above and below.</summary>
    public const double GripThickness = GridSplitterTheme.GripThickness;

    /// <summary>The current cell's focus ring is inset this far from the cell's edges, so neighbours and clipping never cover it.</summary>
    public const double RingInset = 2;

    /// <summary>The header's sort caret size, the field-button icon size at both densities.</summary>
    public const double SortIconSize = FieldParts.IconSize;

    // Pseudo-classes the resize grips set on a header.
    public const string ResizeLeft = ":resize-left";
    public const string ResizeRight = ":resize-right";
    public const string Resizing = ":resizing";

    // Density-dependent values, set by ApplyDensity under these non-public keys.
    internal static readonly object RowHeightKey = new();
    internal static readonly object CellPaddingKey = new();
    internal static readonly object EditPaddingKey = new();
    internal static readonly object HeaderPaddingKey = new();
    internal static readonly object GripLengthKey = new();

    /// <summary>
    /// True while the grid is used from the keyboard: set by Tab or arrow focus and by a key press
    /// in the grid, cleared by a pointer press and when focus leaves the grid. Inherited by the
    /// cells, so the current cell shows the ring only then.
    /// </summary>
    internal static readonly AttachedProperty<bool> KeyboardCursorProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("KeyboardCursor", typeof(DataGridTheme), inherits: true);

    /// <summary>True inside a disabled DataGrid; the cell check boxes take their disabled look from it.</summary>
    internal static readonly AttachedProperty<bool> GridDisabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("GridDisabled", typeof(DataGridTheme), inherits: true);

    /// <summary>True inside a DataGrid that shows vertical grid lines; the headers then show their separators.</summary>
    internal static readonly AttachedProperty<bool> VerticalLinesProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("VerticalLines", typeof(DataGridTheme), inherits: true);

    static DataGridTheme()
    {
        InputElement.GotFocusEvent.AddClassHandler<DataGrid>((grid, e) =>
        {
            if (e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional)
                grid.SetValue(KeyboardCursorProperty, true);
        }, handledEventsToo: true);
        InputElement.LostFocusEvent.AddClassHandler<DataGrid>((grid, e) =>
        {
            if (e.NewFocusedElement is not Visual focused || !grid.IsVisualAncestorOf(focused))
                grid.SetValue(KeyboardCursorProperty, false);
        }, handledEventsToo: true);
        InputElement.KeyDownEvent.AddClassHandler<DataGrid>((grid, _) => grid.SetValue(KeyboardCursorProperty, true),
            RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerPressedEvent.AddClassHandler<DataGrid>((grid, _) => grid.SetValue(KeyboardCursorProperty, false),
            RoutingStrategies.Tunnel, handledEventsToo: true);
        ResizeGrips.Register();
    }

    public static void ApplyDensity(IResourceDictionary resources, bool compact)
    {
        var sizes = compact ? ButtonParts.Compact : ButtonParts.Normal;
        var side = sizes.Padding.Left;
        resources[RowHeightKey] = sizes.MinHeight;
        resources[CellPaddingKey] = new Thickness(side, 0);
        // The field's 1 px border plus this padding puts the text where the cell's text was.
        resources[EditPaddingKey] = new Thickness(side - 1, sizes.Padding.Top);
        resources[HeaderPaddingKey] = new Thickness(side, 0);
        resources[GripLengthKey] = sizes.MinHeight - 2 * Spacing.XS;
    }

    /// <param name="fluentTheme">The DataGrid package's Fluent theme, whose template is kept.</param>
    public static ControlTheme Grid(ControlTheme? fluentTheme)
    {
        var theme = new ControlTheme(typeof(DataGrid)) { BasedOn = fluentTheme };
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        theme.Setters.Add(new Setter(DataGrid.GridLinesVisibilityProperty, DataGridGridLinesVisibility.Horizontal));
        theme.Setters.Add(new Setter(DataGrid.HorizontalGridLinesBrushProperty, Resource(LineBrush)));
        theme.Setters.Add(new Setter(DataGrid.VerticalGridLinesBrushProperty, Resource(LineBrush)));
        theme.Setters.Add(new Setter(DataGrid.RowBackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(DataGrid.HeadersVisibilityProperty, DataGridHeadersVisibility.Column));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(TemplatedControl.FontSizeProperty, Resource("FontSizeBody")));
        if (FluentTemplates.Extend(fluentTheme, ExtendGrid) is { } template)
            theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, template));
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":disabled"))
        {
            Setters =
            {
                new Setter(TemplatedControl.ForegroundProperty, Resource(DisabledText)),
                new Setter(GridDisabledProperty, true),
            },
        });
        foreach (var lines in new[] { DataGridGridLinesVisibility.Vertical, DataGridGridLinesVisibility.All })
            theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().PropertyEquals(DataGrid.GridLinesVisibilityProperty, lines))
            {
                Setters = { new Setter(VerticalLinesProperty, true) },
            });
        return theme;
    }

    // The border colour (BaseLow), the Separator's hairline.
    private const string LineBrush = "SystemControlForegroundBaseLowBrush";
    private const string DisabledText = "SystemControlDisabledBaseMediumLowBrush";

    private static readonly ControlTheme TopLeftHeader = new(typeof(DataGridColumnHeader))
    {
        Setters =
        {
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<DataGridColumnHeader>((_, _) =>
            {
                var line = new Rectangle { Width = 1, HorizontalAlignment = HorizontalAlignment.Right };
                line.Bind(Shape.FillProperty, Resource(LineBrush));
                return new Panel { Children = { line } };
            })),
        },
    };

    // Fluent's template, with the library's shape, the hairline under the headers in the border
    // colour, the overlay scroll bars at their own thickness, and no corner square or disabled wash.
    private static Control ExtendGrid(Control root, INameScope scope)
    {
        if (scope.Find<Rectangle>("PART_ColumnHeadersAndRowsSeparator") is { } separator)
            separator.Bind(Shape.FillProperty, Resource(LineBrush));
        if (scope.Find<Visual>("PART_BottomRightCorner") is { } corner)
            corner.IsVisible = false;
        if (scope.Find<ScrollBar>("PART_VerticalScrollbar") is { } vertical)
            vertical.Width = ScrollBarTheme.Thickness;
        if (scope.Find<ScrollBar>("PART_HorizontalScrollbar") is { } horizontal)
            horizontal.Height = ScrollBarTheme.Thickness;
        if (scope.Find<DataGridColumnHeader>("PART_TopLeftCornerHeader") is { } topLeft)
            topLeft.Theme = TopLeftHeader;
        if (root is not Decorator { Child: Panel grid } border)
            return root;
        if (grid.Children.OfType<Border>().FirstOrDefault(c => c.Name == "PART_DisabledVisualElement") is { } wash)
            grid.Children.Remove(wash);

        border.Child = null;
        var shape = new SquircleBorder { Child = grid };
        shape.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        shape.Bind(SquircleBorder.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        shape.Bind(SquircleBorder.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        shape.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        return shape;
    }

    // --- Rows ------------------------------------------------------------------------------

    /// <summary>
    /// The row: a flat band (<c>PART_Chrome</c>) behind the cells and details, transparent at rest,
    /// list medium on hover, the AA accent fill with white text when selected; the horizontal
    /// hairline lies over the band's bottom edge, so a row is exactly the row height.
    /// </summary>
    public static ControlTheme Row()
    {
        var theme = new ControlTheme(typeof(DataGridRow));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, new Binding(nameof(DataGrid.RowBackground))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(DataGrid) },
        }));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<DataGridRow>(BuildRow)));
        theme.Children.Add(ChromeMotion());

        Avalonia.Styling.Style Fill(Func<Selector?, Selector> state, object brush, object? text = null)
        {
            var style = new Avalonia.Styling.Style(x => state(x))
            {
                Children = { new Avalonia.Styling.Style(x => Chrome(x.Nesting())) { Setters = { new Setter(SquircleBorder.BackgroundProperty, Value(brush)) } } },
            };
            if (text is not null)
                style.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Value(text)));
            return style;
        }

        theme.Children.Add(Fill(x => x.Nesting(), Brushes.Transparent));
        theme.Children.Add(Fill(x => x.Nesting().Class(":pointerover"), "SystemControlHighlightListMediumBrush"));
        theme.Children.Add(Fill(x => x.Nesting().Class(":disabled"), Brushes.Transparent));
        theme.Children.Add(Fill(x => x.Nesting().Class(":selected"), "AccentFillColor", "AccentButtonForeground"));
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":selected")) { Setters = { new Setter(OnAccentProperty, true) } });
        theme.Children.Add(Fill(x => x.Nesting().Class(":selected").Class(":pointerover"), "AccentFillColorPointerOver", "AccentButtonForeground"));
        theme.Children.Add(Fill(x => x.Nesting().Class(":selected").Class(":disabled"), "AccentButtonBackgroundDisabled", "AccentButtonForegroundDisabled"));
        // Fluent's invalid row tint, in the danger colour.
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":invalid").Template().OfType<Rectangle>().Name(InvalidPart))
        {
            Setters = { new Setter(Visual.OpacityProperty, 0.4) },
        });
        return theme;
    }

    private static Control BuildRow(DataGridRow row, INameScope scope)
    {
        var chrome = new SquircleBorder { Name = ChromePart };
        Avalonia.Controls.Grid.SetRowSpan(chrome, 2);
        Avalonia.Controls.Grid.SetColumnSpan(chrome, 2);

        var invalid = new Rectangle { Name = InvalidPart, Opacity = 0 };
        invalid.Bind(Shape.FillProperty, Resource(FieldParts.ErrorBorderKey));
        Avalonia.Controls.Grid.SetRowSpan(invalid, 2);
        Avalonia.Controls.Grid.SetColumnSpan(invalid, 2);

        var header = new DataGridRowHeader { Name = "PART_RowHeader" };
        Avalonia.Controls.Grid.SetRowSpan(header, 3);
        DataGridFrozenGrid.SetIsFrozen(header, true);

        var cells = new DataGridCellsPresenter { Name = "PART_CellsPresenter" };
        Avalonia.Controls.Grid.SetColumn(cells, 1);
        DataGridFrozenGrid.SetIsFrozen(cells, true);

        var details = new DataGridDetailsPresenter { Name = "PART_DetailsPresenter", Background = Brushes.Transparent };
        Avalonia.Controls.Grid.SetRow(details, 1);
        Avalonia.Controls.Grid.SetColumn(details, 1);

        // Over the bottom of the cells or details, so the line takes no height of its own.
        var line = new Rectangle { Name = "PART_BottomGridLine", Height = 1, VerticalAlignment = VerticalAlignment.Bottom };
        Avalonia.Controls.Grid.SetRowSpan(line, 2);
        Avalonia.Controls.Grid.SetColumn(line, 1);

        var root = new DataGridFrozenGrid
        {
            Name = "PART_Root",
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("*,Auto,Auto"),
            Children = { chrome, invalid, line, header, cells, details },
        };
        foreach (var part in new Control[] { chrome, root, header, cells, details, line })
            part.RegisterInNameScope(scope);

        var border = new Border { Child = root };
        border.Bind(Border.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        border.Bind(Border.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        border.Bind(Border.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        return border;
    }

    /// <summary>The row header, shown only when an app turns row headers on: Fluent's, with its separator in the border colour.</summary>
    public static ControlTheme RowHeader(ControlTheme? fluentTheme) => new(typeof(DataGridRowHeader))
    {
        BasedOn = fluentTheme,
        Setters = { new Setter(DataGridRowHeader.SeparatorBrushProperty, Resource(LineBrush)) },
    };

    // --- Cells -----------------------------------------------------------------------------

    /// <summary>
    /// The cell: transparent, the row height, Body text; a 1 px danger border when invalid; the
    /// focus ring inset inside it while it is the current cell and the grid is used from the
    /// keyboard, in the selected text colour on a selected row.
    /// </summary>
    public static ControlTheme Cell()
    {
        var theme = new ControlTheme(typeof(DataGridCell));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Stretch));
        theme.Setters.Add(new Setter(TemplatedControl.FontSizeProperty, Resource("FontSizeBody")));
        theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(RowHeightKey)));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<DataGridCell>(BuildCell)));

        Selector Ring(Selector state) => state.Template().OfType<SquircleBorder>().Name(RingPart);
        theme.Children.Add(new Avalonia.Styling.Style(x => Ring(x.Nesting()))
        {
            Setters =
            {
                new Setter(Visual.IsVisibleProperty, false),
                new Setter(SquircleBorder.BorderBrushProperty, Resource("SystemControlHighlightAccentBrush")),
            },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => Ring(x.Nesting().Class(":current").Not(y => y.Class(":edited"))
            .PropertyEquals(KeyboardCursorProperty, true)))
        {
            Setters = { new Setter(Visual.IsVisibleProperty, true) },
        });
        // On the accent band the accent ring would vanish: it takes the selected text colour.
        theme.Children.Add(new Avalonia.Styling.Style(x => Ring(x.Nesting().PropertyEquals(OnAccentProperty, true)))
        {
            Setters = { new Setter(SquircleBorder.BorderBrushProperty, Resource("AccentButtonForeground")) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":invalid").Template().OfType<SquircleBorder>().Name(InvalidPart))
        {
            Setters = { new Setter(Visual.IsVisibleProperty, true) },
        });
        return theme;
    }

    private static Control BuildCell(DataGridCell cell, INameScope scope)
    {
        var presenter = new ContentPresenter { Name = ContentPart };
        presenter.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        presenter.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        presenter.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        presenter.Bind(Layoutable.HorizontalAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        presenter.Bind(Layoutable.VerticalAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));
        presenter.Bind(ContentPresenter.ForegroundProperty, new TemplateBinding(TemplatedControl.ForegroundProperty));

        var invalid = new SquircleBorder { Name = InvalidPart, BorderThickness = new Thickness(1), IsVisible = false, IsHitTestVisible = false };
        invalid.Bind(SquircleBorder.BorderBrushProperty, Resource(FieldParts.ErrorBorderKey));

        var ring = new SquircleBorder
        {
            Name = RingPart,
            BorderThickness = new Thickness(FocusRing.Width),
            Margin = new Thickness(RingInset),
            IsHitTestVisible = false,
        };
        ring.Bind(SquircleBorder.CornerRadiusProperty, Resource("CornerRadiusControl"));

        var line = new Rectangle { Name = "PART_RightGridLine", Width = 1, VerticalAlignment = VerticalAlignment.Stretch };
        Avalonia.Controls.Grid.SetColumn(line, 1);

        var content = new Panel { Children = { presenter, invalid, ring } };
        var root = new Avalonia.Controls.Grid { Name = "PART_CellRoot", ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { content, line } };
        foreach (var part in new Control[] { presenter, invalid, ring, line, root })
            part.RegisterInNameScope(scope);

        var border = new Border { Child = root };
        border.Bind(Border.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        border.Bind(Border.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        border.Bind(Border.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        return border;
    }

    /// <summary>A text column's text: the density side padding, centred, trimmed with an ellipsis.</summary>
    public static ControlTheme CellTextBlock() => new(typeof(TextBlock))
    {
        Setters =
        {
            new Setter(Layoutable.MarginProperty, new DynamicResourceExtension(CellPaddingKey)),
            new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center),
            new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis),
        },
    };

    /// <summary>
    /// A text column's edit field: TextInput filling the cell (the row height is the field
    /// height), its text where the cell's text was, with no ring of its own; validation errors
    /// show as Fluent's tooltip.
    /// </summary>
    public static ControlTheme CellTextBox(ControlTheme textInput) => new(typeof(TextBox))
    {
        BasedOn = textInput,
        Setters =
        {
            new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),
            new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Stretch),
            new Setter(Layoutable.MinWidthProperty, 0d),
            new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(EditPaddingKey)),
        },
        Children =
        {
            new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<SquircleBorder>().Name(FocusRing.Part))
            {
                Setters = { new Setter(Visual.IsVisibleProperty, false) },
            },
            new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<DataValidationErrors>())
            {
                Setters = { new Setter(StyledElement.ThemeProperty, Resource("TooltipDataValidationErrors")) },
            },
        },
    };

    /// <summary>
    /// The check box a check column shows: the library CheckBox, whose disabled look applies only
    /// inside a disabled grid; outside editing the column shows it as a disabled control.
    /// On a selected row (the accent band) it inverts, in the selected text colour: a white
    /// outline, or a white box with an accent mark.
    /// </summary>
    public static ControlTheme CellCheckBox()
    {
        var theme = CheckBoxTheme.Create(x => x.PropertyEquals(GridDisabledProperty, true));
        Selector OnBand(Selector? x) => x.Nesting().PropertyEquals(OnAccentProperty, true).Not(y => y.PropertyEquals(GridDisabledProperty, true));
        void Part(string part, params (AvaloniaProperty Property, object Value)[] setters)
        {
            var style = new Avalonia.Styling.Style(x => OnBand(x).Template().Name(part));
            foreach (var (property, value) in setters)
                style.Setters.Add(new Setter(property, Value(value)));
            theme.Children.Add(style);
        }
        Part(ToggleParts.OutlinePart, (Border.BackgroundProperty, Brushes.Transparent), (Border.BorderBrushProperty, "AccentButtonForeground"));
        Part(ToggleParts.FillPart, (Border.BackgroundProperty, "AccentButtonForeground"));
        Part(CheckBoxTheme.CheckPart, (Shape.StrokeProperty, "AccentFillColor"));
        Part(CheckBoxTheme.DashPart, (Shape.StrokeProperty, "AccentFillColor"));
        return theme;
    }

    /// <summary>
    /// True inside a selected row (on the accent band), set by the row: the package updates the
    /// cells' own <c>:selected</c> only for the current cell. The ring and the cell check boxes take
    /// the selected text colour on it.
    /// </summary>
    internal static readonly AttachedProperty<bool> OnAccentProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("OnAccent", typeof(DataGridTheme), inherits: true);

    /// <summary>
    /// Styles for controls inside cells: check boxes take the cell check theme (the check column
    /// centres them); combo boxes in an editing template fill the cell, as the text field does.
    /// </summary>
    public static IEnumerable<IStyle> Styles(ControlTheme cellCheckBox) =>
    [
        new Avalonia.Styling.Style(x => x.OfType<DataGridCell>().Descendant().OfType<CheckBox>())
        {
            Setters = { new Setter(StyledElement.ThemeProperty, cellCheckBox) },
        },
        new Avalonia.Styling.Style(x => x.OfType<DataGridCell>().Descendant().OfType<ComboBox>())
        {
            Setters =
            {
                new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),
                new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Stretch),
            },
        },
    ];

    // --- Column headers ----------------------------------------------------------------------

    /// <summary>
    /// The column header: transparent, Body strong text in the secondary colour, the list item's
    /// hover and press fills, the sort caret after the text, the resize grips, and the focus ring
    /// inset inside it.
    /// </summary>
    public static ControlTheme ColumnHeader()
    {
        var theme = new ControlTheme(typeof(DataGridColumnHeader));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Resource("SystemControlForegroundBaseMediumBrush")));
        theme.Setters.Add(new Setter(TemplatedControl.FontSizeProperty, Resource("FontSizeBodyStrong")));
        theme.Setters.Add(new Setter(TemplatedControl.FontWeightProperty, Resource("FontWeightBodyStrong")));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(DataGridColumnHeader.SeparatorBrushProperty, Resource(LineBrush)));
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(HeaderPaddingKey)));
        theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(RowHeightKey)));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<DataGridColumnHeader>(BuildHeader)));
        theme.Children.Add(ChromeMotion());

        Avalonia.Styling.Style ChromeFill(string pseudoClass, string brush) => new(x => Chrome(x.Nesting().Class(pseudoClass)))
        {
            Setters = { new Setter(SquircleBorder.BackgroundProperty, Resource(brush)) },
        };
        theme.Children.Add(new Avalonia.Styling.Style(x => Chrome(x.Nesting())) { Setters = { new Setter(SquircleBorder.BackgroundProperty, Brushes.Transparent) } });
        theme.Children.Add(ChromeFill(":pointerover", "SystemControlHighlightListMediumBrush"));
        theme.Children.Add(ChromeFill(":pressed", "SystemControlHighlightBaseMediumLowBrush"));
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":disabled"))
        {
            Setters = { new Setter(TemplatedControl.ForegroundProperty, Resource(DisabledText)) },
        });
        // The separators between headers follow the vertical grid lines (the package sets their visibility).
        Selector Separator(Selector state) => state.Template().OfType<Rectangle>().Name(SeparatorPart);
        theme.Children.Add(new Avalonia.Styling.Style(x => Separator(x.Nesting())) { Setters = { new Setter(Visual.OpacityProperty, 0d) } });
        theme.Children.Add(new Avalonia.Styling.Style(x => Separator(x.Nesting().PropertyEquals(VerticalLinesProperty, true)))
        {
            Setters = { new Setter(Visual.OpacityProperty, 1d) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":dragIndicator")) { Setters = { new Setter(Visual.OpacityProperty, 0.5) } });

        // The sort caret: up for ascending, down for descending.
        Selector SortIcon(Selector state) => state.Template().OfType<Icon>().Name(SortIconPart);
        theme.Children.Add(new Avalonia.Styling.Style(x => SortIcon(x.Nesting())) { Setters = { new Setter(Visual.IsVisibleProperty, false) } });
        foreach (var (pseudoClass, caret) in new[] { (":sortascending", Icons.CaretUp), (":sortdescending", Icons.CaretDown) })
            theme.Children.Add(new Avalonia.Styling.Style(x => SortIcon(x.Nesting().Class(pseudoClass)))
            {
                Setters = { new Setter(Visual.IsVisibleProperty, true), new Setter(PathIcon.DataProperty, caret) },
            });

        // The grips: hidden at rest; the hover colour near a resizable edge, the drag colour while resizing.
        foreach (var (part, zone) in new[] { (GripLeftPart, ResizeLeft), (GripRightPart, ResizeRight) })
        {
            Selector Grip(Selector state) => state.Template().OfType<PillBorder>().Name(part);
            theme.Children.Add(GripState(x => Grip(x.Nesting()), 0, ScrollBarTheme.ThumbHoverKey));
            theme.Children.Add(GripState(x => Grip(x.Nesting().Class(zone)), 1, ScrollBarTheme.ThumbHoverKey));
            theme.Children.Add(GripState(x => Grip(x.Nesting().Class(zone).Class(Resizing)), 1, ScrollBarTheme.ThumbDragKey));
            theme.Children.Add(GripState(x => Grip(x.Nesting().Class(":disabled")), 0, ScrollBarTheme.ThumbHoverKey));
        }

        Selector HeaderRing(Selector state) => state.Template().OfType<SquircleBorder>().Name(RingPart);
        theme.Children.Add(new Avalonia.Styling.Style(x => HeaderRing(x.Nesting())) { Setters = { new Setter(Visual.IsVisibleProperty, false) } });
        theme.Children.Add(new Avalonia.Styling.Style(x => HeaderRing(x.Nesting().Class(":focus-visible"))) { Setters = { new Setter(Visual.IsVisibleProperty, true) } });
        return theme;
    }

    private static Avalonia.Styling.Style GripState(Func<Selector?, Selector> selector, double opacity, object brushKey) => new(x => selector(x))
    {
        Setters =
        {
            new Setter(Visual.OpacityProperty, opacity),
            new Setter(Border.BackgroundProperty, new DynamicResourceExtension(brushKey)),
        },
    };

    private static Control BuildHeader(DataGridColumnHeader header, INameScope scope)
    {
        var chrome = new SquircleBorder { Name = ChromePart };

        var presenter = new ContentPresenter { Name = ContentPart };
        presenter.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        presenter.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));

        var sort = new Icon { Name = SortIconPart, Width = SortIconSize, Height = SortIconSize, Margin = new Thickness(Spacing.XS, 0, 0, 0) };
        Avalonia.Controls.Grid.SetColumn(sort, 1);

        // Text then caret; the caret stays visible when the text is cut short.
        var label = new Avalonia.Controls.Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { presenter, sort } };
        label.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        label.Bind(Layoutable.HorizontalAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        label.Bind(Layoutable.VerticalAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));

        var separator = new Rectangle { Name = SeparatorPart, Width = 1, VerticalAlignment = VerticalAlignment.Stretch };
        separator.Bind(Shape.FillProperty, new TemplateBinding(DataGridColumnHeader.SeparatorBrushProperty));
        separator.Bind(Visual.IsVisibleProperty, new TemplateBinding(DataGridColumnHeader.AreSeparatorsVisibleProperty));
        Avalonia.Controls.Grid.SetColumn(separator, 1);
        var row = new Avalonia.Controls.Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { label, separator } };

        var left = Grip(GripLeftPart, HorizontalAlignment.Left);
        var right = Grip(GripRightPart, HorizontalAlignment.Right);

        var ring = new SquircleBorder
        {
            Name = RingPart,
            BorderThickness = new Thickness(FocusRing.Width),
            Margin = new Thickness(RingInset),
            IsHitTestVisible = false,
        };
        ring.Bind(SquircleBorder.BorderBrushProperty, Resource("SystemControlHighlightAccentBrush"));
        ring.Bind(SquircleBorder.CornerRadiusProperty, Resource("CornerRadiusControl"));

        foreach (var part in new Control[] { chrome, presenter, sort, separator, left, right, ring })
            part.RegisterInNameScope(scope);
        return new Panel { Children = { chrome, row, left, right, ring } };
    }

    private static PillBorder Grip(string name, HorizontalAlignment side)
    {
        var grip = new PillBorder
        {
            Name = name,
            Width = GripThickness,
            HorizontalAlignment = side,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };
        grip.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(GripLengthKey));
        grip.Bind(Avalonia.Animation.Animatable.TransitionsProperty, new DynamicResourceExtension(ScrollBarTheme.PartTransitionsKey));
        return grip;
    }

    /// <summary>
    /// Sets <see cref="ResizeLeft"/> or <see cref="ResizeRight"/> on a column header while the
    /// pointer is within <see cref="ResizeZone"/> of an edge that resizes a column (the right edge
    /// resizes the header's column, the left edge the column before it), and <see cref="Resizing"/>
    /// while a press that started there is held.
    /// </summary>
    private static class ResizeGrips
    {
        public static void Register()
        {
            InputElement.PointerMovedEvent.AddClassHandler<DataGridColumnHeader>((h, e) => Update(h, e), handledEventsToo: true);
            InputElement.PointerExitedEvent.AddClassHandler<DataGridColumnHeader>((h, _) => Clear(h), handledEventsToo: true);
            InputElement.PointerPressedEvent.AddClassHandler<DataGridColumnHeader>((h, _) =>
            {
                if (h.Classes.Contains(ResizeLeft) || h.Classes.Contains(ResizeRight))
                    Set(h, Resizing, true);
            }, handledEventsToo: true);
            InputElement.PointerReleasedEvent.AddClassHandler<DataGridColumnHeader>((h, e) =>
            {
                Set(h, Resizing, false);
                Update(h, e);
            }, handledEventsToo: true);
            InputElement.PointerCaptureLostEvent.AddClassHandler<DataGridColumnHeader>((h, _) => Set(h, Resizing, false), handledEventsToo: true);
        }

        private static void Update(DataGridColumnHeader header, PointerEventArgs e)
        {
            if (header.Classes.Contains(Resizing))
                return;
            var x = e.GetPosition(header).X;
            var grid = header.FindAncestorOfType<DataGrid>();
            var column = DataGridColumn.GetColumnContainingElement(header);
            var right = header.Bounds.Width - x <= ResizeZone && CanResize(grid, column, previous: false);
            var left = !right && x <= ResizeZone && CanResize(grid, column, previous: true);
            Set(header, ResizeRight, right);
            Set(header, ResizeLeft, left);
        }

        private static void Clear(DataGridColumnHeader header)
        {
            if (header.Classes.Contains(Resizing))
                return;
            Set(header, ResizeRight, false);
            Set(header, ResizeLeft, false);
        }

        // As the package decides: the grid and the column allow resizing, and the filler column never resizes.
        private static bool CanResize(DataGrid? grid, DataGridColumn? column, bool previous)
        {
            if (grid is null || column is null || !grid.CanUserResizeColumns)
                return false;
            var isFiller = !grid.Columns.Contains(column);
            if (!previous)
                return !isFiller && column.CanUserResize;
            var before = grid.Columns.Where(c => c.IsVisible && (isFiller || c.DisplayIndex < column.DisplayIndex))
                .MaxBy(c => c.DisplayIndex);
            return before?.CanUserResize == true;
        }

        private static void Set(DataGridColumnHeader header, string pseudoClass, bool value) =>
            ((IPseudoClasses)header.Classes).Set(pseudoClass, value);
    }
}
