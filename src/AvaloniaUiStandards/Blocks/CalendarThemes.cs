using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The Calendar themes: Fluent's Calendar and CalendarItem with the panel a squircle
/// (<c>CornerRadiusOverlay</c>) on the card colour with a hairline border, the header's title and
/// arrows as subtle Buttons (<c>Icons.CaretLeft</c>, <c>Icons.CaretRight</c>), Body text, and
/// the day, month and year cells as squircle pills with the list item hover and press, the accent
/// fill with white text when selected, accent text for today and secondary text outside the
/// shown month. Fluent's keyboard handling is kept; the focused cell shows the focus ring.
/// </summary>
internal static class CalendarThemes
{
    public const string ChromePart = "PART_CalendarChrome";

    /// <summary>The gap around each cell, so neighbouring pills stand apart.</summary>
    public const double CellMargin = 2;

    /// <summary>The weekday row: a Caption line (15 px in Inter) with <c>SpacingXS</c> above and below.</summary>
    public const double WeekdayRowHeight = 15 + 2 * Spacing.XS;

    // Density-dependent values, set by ApplyDensity under these non-public keys.
    internal static readonly object BodyWidthKey = new();
    internal static readonly object MonthHeightKey = new();

    /// <summary>The month's size from the cell size: seven columns, and the weekday row over six weeks.</summary>
    public static void ApplyDensity(IResourceDictionary resources, DensityStyle density)
    {
        var pitch = (density == DensityStyle.Compact ? ButtonParts.Compact : ButtonParts.Normal).MinHeight + 2 * CellMargin;
        resources[BodyWidthKey] = 7 * pitch;
        resources[MonthHeightKey] = WeekdayRowHeight + 6 * pitch;
    }

    /// <summary>
    /// The Calendar: the panel colours and Body text. Keyboard focus stays on the cell it arrived
    /// at while the arrow keys move Fluent's keyboard cursor (<c>:dayfocused</c>, <c>:btnfocused</c>),
    /// so the ring follows the cursor while focus inside the Calendar came from the keyboard.
    /// </summary>
    public static ControlTheme Calendar(ControlTheme? fluentTheme)
    {
        var theme = Panel(fluentTheme);
        FocusRing.TrackWithin(typeof(Calendar));
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().PropertyEquals(FocusRing.IsVisibleWithinProperty, true))
        {
            Setters = { new Setter(CursorRingProperty, true) },
        });
        return theme;
    }

    /// <summary>
    /// Inherited by the cells from their Calendar: true while focus inside it came from the
    /// keyboard, so the keyboard cursor's cell shows the ring.
    /// </summary>
    internal static readonly AttachedProperty<bool> CursorRingProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("CursorRing", typeof(CalendarThemes), inherits: true);

    private static ControlTheme Panel(ControlTheme? fluentTheme) => new(typeof(Calendar))
    {
        BasedOn = fluentTheme,
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Resource("SystemChromeMediumLowColor")),
            new Setter(TemplatedControl.BorderBrushProperty, Resource("SystemControlForegroundBaseLowBrush")),
            new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
            new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusOverlay")),
            new Setter(TemplatedControl.ForegroundProperty, Resource("SystemControlForegroundBaseHighBrush")),
            new Setter(TemplatedControl.FontSizeProperty, Resource("FontSizeBody")),
        },
        Children =
        {
            new Avalonia.Styling.Style(x => x.Nesting().Class(":disabled"))
            {
                Setters = { new Setter(TemplatedControl.ForegroundProperty, Resource("SystemControlDisabledBaseMediumLowBrush")) },
            },
        },
    };

    public static ControlTheme Item(ControlTheme? fluentTheme)
    {
        var theme = new ControlTheme(typeof(CalendarItem)) { BasedOn = fluentTheme };
        theme.Setters.Add(new Setter(CalendarItem.DayTitleTemplateProperty, new FuncTemplate<Control>(DayTitle)));
        if (FluentTemplates.Extend(fluentTheme, ExtendItem) is { } template)
            theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, template));
        return theme;
    }

    /// <summary>A weekday heading: Caption size in secondary text, a cell wide.</summary>
    private static Control DayTitle()
    {
        var title = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, Spacing.XS),
        };
        title.Bind(TextBlock.TextProperty, new Binding());
        title.Bind(TextBlock.FontSizeProperty, Resource("FontSizeCaption"));
        title.Bind(TextBlock.ForegroundProperty, Resource("SystemControlForegroundBaseMediumBrush"));
        return title;
    }

    private static Control ExtendItem(Control root, INameScope scope)
    {
        if (root is not Border { Child: Control content } fluentChrome)
            return root;

        if (scope.Find<Button>("PART_HeaderButton") is { } header)
        {
            HeaderButton(header);
            header.HorizontalAlignment = HorizontalAlignment.Stretch;
            header.HorizontalContentAlignment = HorizontalAlignment.Left;
            header.Bind(TemplatedControl.FontWeightProperty, Resource("FontWeightBodyStrong"));
            header.Bind(TemplatedControl.PaddingProperty, new DynamicResourceExtension(PaddingKey));
        }
        foreach (var (name, icon) in new[] { ("PART_PreviousButton", Icons.CaretLeft), ("PART_NextButton", Icons.CaretRight) })
        {
            if (scope.Find<Button>(name) is not { } arrow)
                continue;
            HeaderButton(arrow);
            arrow.Content = new Icon { Data = icon };
            arrow.Padding = default;
            arrow.HorizontalContentAlignment = HorizontalAlignment.Center;
            arrow.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(MinHeightKey));
        }
        // Fluent's grey band behind the days is dropped: the days sit on the panel.
        if (scope.Find<Border>("BackgroundLayer") is { Parent: Panel bandHost } band)
            bandHost.Children.Remove(band);
        // Fluent's fixed sizes (a 294 px wide body, a 290 px high month, a 40 px header and a
        // 38 px weekday row) give way to the cells' own sizes.
        if (scope.Find<Grid>("PART_MonthView") is { Parent: Grid body } month)
        {
            month.MinHeight = 0;
            month.RowDefinitions[0].Height = GridLength.Auto;
            body.RowDefinitions[0].Height = GridLength.Auto;
            // Seven cells wide in every view, so the header and the year and decade views keep the month's width.
            body.Bind(Layoutable.MinWidthProperty, new DynamicResourceExtension(BodyWidthKey));
        }
        if (scope.Find<Grid>("PART_YearView") is { } year)
        {
            // The year and decade views take the month's height, their pills spread over it.
            year.Background = null;
            year.Bind(Layoutable.MinHeightProperty, new DynamicResourceExtension(MonthHeightKey));
        }

        fluentChrome.Child = null;
        var chrome = new SquircleBorder { Name = ChromePart, Padding = new Thickness(Spacing.XS), Child = content };
        chrome.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        chrome.Bind(SquircleBorder.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        chrome.Bind(SquircleBorder.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        chrome.Bind(SquircleBorder.CornerRadiusProperty, Resource("CornerRadiusOverlay"));
        return chrome;
    }

    /// <summary>A header button: the library's subtle Button at the density height, with a margin below the header.</summary>
    private static void HeaderButton(Button button)
    {
        button.SetValue(StyledElement.ThemeProperty, null);
        button.Classes.Add("subtle");
        button.VerticalAlignment = VerticalAlignment.Center;
        button.Margin = new Thickness(CellMargin);
    }

    /// <summary>A day cell: a square pill the density height.</summary>
    public static ControlTheme DayButton()
    {
        var theme = Cell(typeof(CalendarDayButton), ":dayfocused", HorizontalAlignment.Center);
        theme.Setters.Add(new Setter(Layoutable.WidthProperty, new DynamicResourceExtension(MinHeightKey)));
        theme.Setters.Add(new Setter(Layoutable.HeightProperty, new DynamicResourceExtension(MinHeightKey)));
        return theme;
    }

    /// <summary>A month or year cell: a pill the density height, as wide as its column.</summary>
    public static ControlTheme MonthButton()
    {
        var theme = Cell(typeof(CalendarButton), ":btnfocused", HorizontalAlignment.Stretch);
        theme.Setters.Add(new Setter(Layoutable.HeightProperty, new DynamicResourceExtension(MinHeightKey)));
        return theme;
    }

    private static ControlTheme Cell(Type target, string cursor, HorizontalAlignment alignment)
    {
        var theme = new ControlTheme(target);
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new Thickness(0)));
        theme.Setters.Add(new Setter(Layoutable.MarginProperty, new Thickness(CellMargin)));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Resource("SystemControlForegroundBaseHighBrush")));
        theme.Setters.Add(new Setter(TemplatedControl.FontSizeProperty, Resource("FontSizeBody")));
        theme.Setters.Add(new Setter(Layoutable.HorizontalAlignmentProperty, alignment));
        theme.Setters.Add(new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty,
            new FuncControlTemplate<ContentControl>((control, scope) => FocusRing.Wrap(BuildContentTemplate(control, scope), scope))));
        // The ring shows on the keyboard cursor's cell (Fluent's :dayfocused or :btnfocused) while
        // focus inside the Calendar came from the keyboard; focus itself stays where it arrived.
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(Visual.ClipToBoundsProperty, false));
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<SquircleBorder>().Name(FocusRing.Part))
        {
            Setters = { new Setter(Visual.IsVisibleProperty, false) },
        });
        Selector Cursor(Selector? x) => x.Nesting().Class(cursor).PropertyEquals(CursorRingProperty, true);
        theme.Children.Add(new Avalonia.Styling.Style(x => Cursor(x).Template().OfType<SquircleBorder>().Name(FocusRing.Part))
        {
            Setters = { new Setter(Visual.IsVisibleProperty, true) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => Cursor(x)) { Setters = { new Setter(Visual.ZIndexProperty, 1) } });
        theme.Children.Add(ChromeMotion());

        // Rising precedence: rest, out of month, today, hover, press, selected, unavailable, disabled.
        var text = "SystemControlForegroundBaseHighBrush";
        theme.Children.Add(State((string?)null, new Look(Brushes.Transparent, Brushes.Transparent, text)));
        theme.Children.Add(State(":inactive", new Look(Brushes.Transparent, Brushes.Transparent, "SystemControlForegroundBaseMediumBrush")));
        theme.Children.Add(State(":today", new Look(Brushes.Transparent, Brushes.Transparent, "AccentTextColor")));
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":today"))
        {
            Setters = { new Setter(TemplatedControl.FontWeightProperty, Resource("FontWeightBodyStrong")) },
        });
        theme.Children.Add(Hover(":pointerover", "SystemControlHighlightListMediumBrush"));
        theme.Children.Add(Hover(":pressed", "SystemControlHighlightBaseMediumLowBrush"));
        theme.Children.Add(Group(x => x.Nesting().Class(":selected"),
            rest: new("AccentFillColor", Brushes.Transparent, "AccentButtonForeground"),
            hover: new("AccentFillColorPointerOver", Brushes.Transparent, "AccentButtonForeground"),
            pressed: new("AccentFillColorPressed", Brushes.Transparent, "AccentButtonForeground"),
            disabled: new("AccentButtonBackgroundDisabled", Brushes.Transparent, "AccentButtonForegroundDisabled")));
        theme.Children.Add(State(":blackout", new Look(Brushes.Transparent, Brushes.Transparent, "SystemControlDisabledBaseMediumLowBrush")));
        theme.Children.Add(State(":disabled", new Look(Brushes.Transparent, Brushes.Transparent, "SystemControlDisabledBaseMediumLowBrush")));
        return theme;
    }

    /// <summary>A hover or press fill that keeps the cell's text colour (out of month, today).</summary>
    private static Avalonia.Styling.Style Hover(string pseudoClass, string fill) => new(x => Chrome(x.Nesting().Class(pseudoClass)))
    {
        Setters = { new Setter(SquircleBorder.BackgroundProperty, Resource(fill)) },
    };
}
