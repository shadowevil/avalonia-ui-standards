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
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The DatePicker and TimePicker themes and their flyout presenters. The closed picker is a field
/// (the field chrome, colours, focus border and ring of the field Blocks) holding Fluent's
/// segments. The flyout is a glass overlay inset by <c>SpacingXS</c>, its looping selectors
/// scrolling inside a squircle concentric with the glass, list item rows, one squircle accent
/// highlight behind the selected row, opaque squircle step buttons with caret icons, and Accent
/// and Standard Buttons with check and x icons to accept and dismiss.
/// </summary>
internal static class PickerThemes
{
    public const string HighlightPart = "PART_Highlight";
    public const string GlassPart = "PART_Glass";
    public const string ViewportPart = "PART_SelectorViewport";

    /// <summary>The number of rows a looping selector shows; the middle one is the selected row.</summary>
    public const int VisibleRows = 7;

    // Density-dependent values, set by ApplyDensity under these non-public keys.
    internal static readonly object SelectorHeightKey = new();
    internal static readonly object LeadingSegmentPaddingKey = new();
    internal static readonly object ColumnRingHeightKey = new();
    internal static readonly object ColumnRingRadiusKey = new();
    internal static readonly object ColumnRingGapRadiusKey = new();

    public const string ColumnFocusRingPart = "PART_ColumnFocusRing";

    // The ring and gap widths of the focus ring ([0019]).
    private const double FocusRingWidth = 2;

    public static void ApplyDensity(IResourceDictionary resources, DensityStyle density)
    {
        var sizes = density == DensityStyle.Compact ? ButtonParts.Compact : ButtonParts.Normal;
        resources[SelectorHeightKey] = VisibleRows * sizes.MinHeight;
        resources[LeadingSegmentPaddingKey] = new Thickness(sizes.Padding.Left, 0, 0, 0);
        // The column ring reaches the ring and gap widths above and below the row; its curves
        // stay concentric with the row's (radius 8).
        var reach = 2 * FocusRingWidth;
        resources[ColumnRingHeightKey] = sizes.MinHeight + 2 * reach;
        resources[ColumnRingRadiusKey] = new CornerRadius(8 + reach);
        resources[ColumnRingGapRadiusKey] = new CornerRadius(8 + FocusRingWidth);
    }

    // --- The closed field --------------------------------------------------------------------

    public static ControlTheme DatePicker(ControlTheme? fluentTheme) => Field(typeof(DatePicker), fluentTheme, ExtendDatePicker);

    public static ControlTheme TimePicker(ControlTheme? fluentTheme) => Field(typeof(TimePicker), fluentTheme, ExtendTimePicker);

    /// <summary>
    /// A picker drawn as a field: the field chrome behind Fluent's flyout button, the field
    /// colours with focus anywhere inside it, and the ring while a part has keyboard focus.
    /// </summary>
    private static ControlTheme Field(Type target, ControlTheme? fluentTheme, Func<Control, INameScope, Control> extend)
    {
        var theme = new ControlTheme(target) { BasedOn = fluentTheme };
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, Resource("TextControlBorderThemeThickness")));
        theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(MinHeightKey)));
        if (FluentTemplates.Extend(fluentTheme, extend) is { } template)
            theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, template));
        FieldParts.AddColours(theme, FieldParts.TextControl, x => x.Nesting().Class(":focus-within"));
        theme.Children.Add(ChromeMotion(FieldParts.ChromePart));
        FocusRing.AddWithin(theme);
        return theme;
    }

    private static Control ExtendDatePicker(Control root, INameScope scope)
    {
        foreach (var name in new[] { "PART_DayTextBlock", "PART_YearTextBlock" })
            if (scope.Find<TextBlock>(name) is { } segment)
                Segment(segment);
        if (scope.Find<TextBlock>("PART_MonthTextBlock") is { } month)
        {
            Segment(month);
            month.Bind(TextBlock.PaddingProperty, new DynamicResourceExtension(LeadingSegmentPaddingKey));
        }
        return ExtendField(root, scope);
    }

    private static Control ExtendTimePicker(Control root, INameScope scope)
    {
        foreach (var name in new[] { "PART_HourTextBlock", "PART_MinuteTextBlock", "PART_SecondTextBlock", "PART_PeriodTextBlock" })
            if (scope.Find<TextBlock>(name) is { } segment)
                Segment(segment);
        return ExtendField(root, scope);
    }

    /// <summary>A segment's text, centred in the field height in place of Fluent's off-grid 3,6 padding.</summary>
    private static void Segment(TextBlock segment)
    {
        segment.Padding = default;
        segment.VerticalAlignment = VerticalAlignment.Center;
    }

    private static Control ExtendField(Control root, INameScope scope)
    {
        if (scope.Find<Grid>("LayoutRoot") is { } layout && scope.Find<Button>("PART_FlyoutButton") is { } button)
        {
            // The field chrome is drawn behind the button, which keeps Fluent's behaviour but no chrome of its own.
            var chrome = FieldParts.CreateChrome();
            layout.Children.Insert(0, chrome);
            button.Theme = BareButton;
            button.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
            button.SetValue(Layoutable.MinHeightProperty, 0d);
        }
        if (scope.Find<Popup>("PART_Popup") is { } popup)
            popup.ShouldUseOverlayLayer = true;
        return FocusRing.Wrap(root, new NameScope());
    }

    /// <summary>
    /// The flyout button inside a picker field: Fluent's button behaviour (focus, keyboard, click)
    /// with only its content presenter, as the field draws the chrome.
    /// </summary>
    internal static readonly ControlTheme BareButton = new(typeof(Button))
    {
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Button>((_, scope) =>
            {
                var presenter = CreatePresenter();
                presenter.Bind(ContentPresenter.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
                presenter.RegisterInNameScope(scope);
                return presenter;
            })),
        },
    };

    // --- The flyout ----------------------------------------------------------------------------

    public static ControlTheme Presenter(Type target, ControlTheme? fluentTheme, ControlTheme item, ControlTheme stepButton)
    {
        var theme = new ControlTheme(target) { BasedOn = fluentTheme };
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
        if (FluentTemplates.Extend(fluentTheme, (root, scope) => ExtendPresenter(root, scope, item, stepButton)) is { } template)
            theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, template));
        return theme;
    }

    private static Control ExtendPresenter(Control root, INameScope scope, ControlTheme item, ControlTheme stepButton)
    {
        if (root is not Border { Child: Grid content } fluentChrome)
            return root;

        if (scope.Find<Grid>("PART_PickerContainer") is { } container)
        {
            // The selectors show VisibleRows rows of the density's item height, the selected row in the middle.
            container.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(SelectorHeightKey));
            container.Styles.Clear();
            container.Styles.Add(new Avalonia.Styling.Style(x => x.OfType<DateTimePickerPanel>().Child().OfType<ListBoxItem>())
            {
                Setters = { new Setter(StyledElement.ThemeProperty, item) },
            });

            foreach (var child in container.Children.ToList())
            {
                switch (child)
                {
                    case Panel host when host.Name?.EndsWith("Host", StringComparison.Ordinal) == true:
                        ExtendSelectorHost(host, stepButton);
                        break;
                    case Rectangle { Name: "HighlightRect" } rectangle:
                        container.Children[container.Children.IndexOf(rectangle)] = Highlight(rectangle);
                        break;
                    case Rectangle spacer:
                        // Column dividers would cut through the highlight; the columns are told apart by their content.
                        spacer.Fill = Brushes.Transparent;
                        break;
                }
            }
        }

        if (scope.Find<Grid>("AcceptDismissGrid") is { } buttons)
        {
            buttons.SetValue(Layoutable.HeightProperty, double.NaN);
            buttons.Margin = new Thickness(0, Spacing.XS, 0, 0);
            buttons.ColumnSpacing = Spacing.XS;
            foreach (var divider in buttons.Children.OfType<Rectangle>())
                divider.IsVisible = false;
            if (scope.Find<Button>("PART_AcceptButton") is { } accept)
                FlyoutButton(accept, Icons.Check, "accent");
            if (scope.Find<Button>("PART_DismissButton") is { } dismiss)
                FlyoutButton(dismiss, Icons.X, null);
        }

        // Fluent's border is replaced by glass, inset by SpacingXS like the drop-down ([0024]).
        fluentChrome.Child = null;
        var glass = new GlassBorder { Name = GlassPart, Padding = new Thickness(Spacing.XS), Child = content };

        // The selectors scroll inside a squircle concentric with the glass (its radius less the
        // inset), so a row crossing the top or bottom edge is cut by that curve, never by the
        // glass's own corner.
        if (scope.Find<Grid>("PART_PickerContainer") is { Parent: Panel contentRoot } selectors)
        {
            var index = contentRoot.Children.IndexOf(selectors);
            contentRoot.Children.RemoveAt(index);
            var viewport = new SquircleBorder { Name = ViewportPart, Child = selectors };
            viewport.Bind(SquircleBorder.CornerRadiusProperty, new Binding(nameof(GlassBorder.CornerRadius)) { Source = glass, Converter = Inset });
            Grid.SetRow(viewport, Grid.GetRow(selectors));
            contentRoot.Children.Insert(index, viewport);
            // The selectors keep their row count in a taller presenter.
            if (contentRoot is Grid { RowDefinitions.Count: > 0 } rows)
                rows.RowDefinitions[0].Height = GridLength.Auto;
        }
        return glass;
    }

    private static readonly IValueConverter Inset = new FuncValueConverter<CornerRadius, CornerRadius>(r =>
        new CornerRadius(Math.Max(0, r.TopLeft - Spacing.XS), Math.Max(0, r.TopRight - Spacing.XS),
            Math.Max(0, r.BottomRight - Spacing.XS), Math.Max(0, r.BottomLeft - Spacing.XS)));

    private static void ExtendSelectorHost(Panel host, ControlTheme stepButton)
    {
        foreach (var part in host.Children)
        {
            switch (part)
            {
                case ScrollViewer scroller:
                    // The selectors loop, so there is always more content: no scroll-edge shadow.
                    ScrollShadows.SetIsEnabled(scroller, false);
                    if (scroller.Content is DateTimePickerPanel panel)
                    {
                        panel.Bind(DateTimePickerPanel.ItemHeightProperty, new DynamicResourceExtension(MinHeightKey));
                        // The column ring shows keyboard focus in place of Fluent's rectangle adorner.
                        panel.FocusAdorner = null;
                    }
                    break;
                case RepeatButton step:
                    step.Theme = stepButton;
                    step.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(MinHeightKey));
                    var up = step.Name?.EndsWith("UpButton", StringComparison.Ordinal) == true;
                    step.VerticalAlignment = up ? VerticalAlignment.Top : VerticalAlignment.Bottom;
                    step.HorizontalAlignment = HorizontalAlignment.Stretch;
                    step.HorizontalContentAlignment = HorizontalAlignment.Center;
                    step.Content = new Icon { Data = up ? Icons.CaretUp : Icons.CaretDown };
                    break;
            }
        }
        if (host.Children.OfType<ScrollViewer>().FirstOrDefault() is { Content: DateTimePickerPanel selector } selectorScroller)
            host.Children.Insert(host.Children.IndexOf(selectorScroller) + 1, ColumnFocusRing(selector));
    }

    /// <summary>
    /// The focus ring of a selector column ([0019]): the selected row's highlight runs on through
    /// the neighbouring columns, so the ring sits around this column's part of it, 2 px inside the
    /// column's sides (and so inside the viewport's curve): a 2 px accent ring, then a 2 px gap in the
    /// glass tint that sets this column's part of the highlight apart. Shown while the selector
    /// has keyboard focus.
    /// </summary>
    private static SquircleBorder ColumnFocusRing(DateTimePickerPanel selector)
    {
        var gap = new SquircleBorder { BorderThickness = new Thickness(FocusRingWidth) };
        gap.Bind(SquircleBorder.BorderBrushProperty, Resource("GlassTintColor"));
        gap.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension(ColumnRingGapRadiusKey));
        var ring = new SquircleBorder
        {
            Name = ColumnFocusRingPart,
            BorderThickness = new Thickness(FocusRingWidth),
            VerticalAlignment = VerticalAlignment.Center,
            // 2 px in from the column's sides, so it clears the viewport's edge and the neighbouring column.
            Margin = new Thickness(FocusRingWidth, 0),
            IsHitTestVisible = false,
            IsVisible = false,
            Child = gap,
        };
        ring.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("SystemControlHighlightAccentBrush"));
        ring.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension(ColumnRingRadiusKey));
        ring.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(ColumnRingHeightKey));
        // The ring follows the selector's :focus-visible pseudo-class.
        selector.Classes.CollectionChanged += (_, _) => ring.IsVisible = selector.Classes.Contains(":focus-visible");
        return ring;
    }

    /// <summary>The selected row's highlight: a squircle in the accent fill, the list item shape and height.</summary>
    private static SquircleBorder Highlight(Rectangle fluentHighlight)
    {
        var highlight = new SquircleBorder { Name = HighlightPart, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        highlight.Bind(SquircleBorder.BackgroundProperty, Resource("AccentFillColor"));
        highlight.Bind(SquircleBorder.CornerRadiusProperty, Resource("CornerRadiusControl"));
        highlight.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(MinHeightKey));
        highlight.ZIndex = fluentHighlight.ZIndex;
        Grid.SetColumn(highlight, Grid.GetColumn(fluentHighlight));
        Grid.SetColumnSpan(highlight, Grid.GetColumnSpan(fluentHighlight));
        return highlight;
    }

    /// <summary>Accept or dismiss: the library Button (its type theme), full width of its column, with an icon.</summary>
    private static void FlyoutButton(Button button, Geometry icon, string? variant)
    {
        button.SetValue(StyledElement.ThemeProperty, null);
        button.SetValue(Layoutable.HeightProperty, double.NaN);
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.Content = new Icon { Data = icon };
        if (variant is not null)
            button.Classes.Add(variant);
    }

    /// <summary>
    /// A selector row: the list item shape and height ([0024]) with its hover and press fills.
    /// The selected row shows no fill of its own and white text, over the shared highlight.
    /// </summary>
    public static ControlTheme Item()
    {
        var theme = new ControlTheme(typeof(ListBoxItem));
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(PaddingKey)));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Resource("SystemControlForegroundBaseHighBrush")));
        theme.Setters.Add(new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ContentControl>((control, scope) => BuildContentTemplate(control, scope))));
        // Months read from the left edge, as in the field.
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class("MonthItem"))
        {
            Setters = { new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Left) },
        });
        theme.Children.Add(ChromeMotion());
        theme.Children.Add(State((string?)null, new Look(Brushes.Transparent, Brushes.Transparent, "SystemControlForegroundBaseHighBrush")));
        theme.Children.Add(State(":pointerover", new Look("SystemControlHighlightListMediumBrush", Brushes.Transparent, "SystemControlHighlightAltBaseHighBrush")));
        theme.Children.Add(State(":pressed", new Look("SystemControlHighlightBaseMediumLowBrush", Brushes.Transparent, "SystemControlHighlightAltBaseHighBrush")));
        theme.Children.Add(State(":selected", new Look(Brushes.Transparent, Brushes.Transparent, "AccentButtonForeground")));
        theme.Children.Add(State(":disabled", new Look(Brushes.Transparent, Brushes.Transparent, "SystemControlDisabledBaseMediumLowBrush")));
        return theme;
    }

    /// <summary>
    /// The step buttons over the top and bottom row of a selector (shown on hover, as in Fluent):
    /// a squircle the height of one row, opaque in the glass tint so the row under it does not
    /// show through, with the list item hover and press fills and a caret icon in secondary text.
    /// </summary>
    public static ControlTheme StepButton()
    {
        var theme = ContentButtonTheme(typeof(RepeatButton), "ButtonBorderThemeThickness",
            (new("GlassTintColor", Brushes.Transparent, "SystemControlForegroundBaseMediumBrush"),
             new("SystemControlHighlightListMediumBrush", Brushes.Transparent, "SystemControlHighlightAltBaseHighBrush"),
             new("SystemControlHighlightBaseMediumLowBrush", Brushes.Transparent, "SystemControlHighlightAltBaseHighBrush"),
             new("GlassTintColor", Brushes.Transparent, "SystemControlDisabledBaseMediumLowBrush")));
        // Hidden until the pointer is over its selector (Fluent's presenter shows it then).
        theme.Setters.Add(new Setter(Visual.IsVisibleProperty, false));
        theme.Setters.Add(new Setter(InputElement.FocusableProperty, false));
        return theme;
    }
}
