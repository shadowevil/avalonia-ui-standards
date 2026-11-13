using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
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
/// The Expander theme: one squircle card (card colour, a 1 px line border) holding a header row
/// and the content. The header row has a caret before the header that turns towards the content
/// as it opens, list-item hover and pressed fills, and the density height. The content reveals
/// (and the layout around it moves) over <c>MotionNormal</c>, behind a hairline under the header.
/// The focus ring is drawn around the whole card while the header has keyboard focus.
/// </summary>
/// <remarks>
/// Template: <c>SquircleBorder#PART_Card</c> around a <c>DockPanel</c> with
/// <c>ToggleButton#ExpanderHeader</c> (docked towards the content's opposite side) and
/// <c>Reveal#PART_Reveal</c>, which holds <c>Border#PART_Separator</c> and
/// <c>ContentPresenter#PART_ContentPresenter</c>. Header: <c>SquircleBorder#PART_Chrome</c>
/// around <c>Icon#PART_Caret</c> and <c>ContentPresenter#PART_ContentPresenter</c>.
/// </remarks>
internal static class ExpanderTheme
{
    public const string CardPart = "PART_Card";
    public const string HeaderPart = "ExpanderHeader";
    public const string RevealPart = "PART_Reveal";
    public const string SeparatorPart = "PART_Separator";
    public const string CaretPart = "PART_Caret";

    // The content's padding by density, and the caret's turn, under non-public keys.
    internal static readonly object ContentPaddingKey = new();
    internal static readonly object CaretTransitionsKey = new();

    /// <summary>The caret's angle, closed and open, per expand direction: it points at the content when open.</summary>
    internal static readonly (string Pseudo, double Closed, double Open)[] CaretAngles =
    [
        (":down", -90, 0),
        (":up", -90, -180),
        (":right", -90, 90),
        (":left", 90, -90),
    ];

    private static readonly ControlTheme HeaderTheme = Header();

    public static ControlTheme Create()
    {
        var theme = new ControlTheme(typeof(Expander))
        {
            Setters =
            {
                new Setter(InputElement.IsTabStopProperty, false),
                new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")),
                new Setter(TemplatedControl.BackgroundProperty, Resource("ExpanderContentBackground")),
                new Setter(TemplatedControl.BorderBrushProperty, Resource("ExpanderContentBorderBrush")),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
                new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(ContentPaddingKey)),
                new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),
                new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Top),
                new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch),
                new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Stretch),
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Expander>(Build)),
            },
        };
        FocusRing.AddWithin(theme);

        theme.Children.Add(Part<Reveal>(x => x.Nesting(), RevealPart, (Animatable.TransitionsProperty, new DynamicResourceExtension(Reveal.TransitionsKey))));
        theme.Children.Add(Part<Reveal>(x => x.Nesting().Class(":expanded"), RevealPart, (Reveal.ProgressProperty, 1d)));

        // Placement per direction: the header on the side away from the content, the hairline between them.
        foreach (var (pseudo, dock, vertical, fromEnd) in new[]
                 {
                     (":down", Dock.Top, true, false), (":up", Dock.Bottom, true, true),
                     (":right", Dock.Left, false, false), (":left", Dock.Right, false, true),
                 })
        {
            theme.Children.Add(Part<ToggleButton>(x => x.Nesting().Class(pseudo), HeaderPart, (DockPanel.DockProperty, dock)));
            theme.Children.Add(Part<Reveal>(x => x.Nesting().Class(pseudo), RevealPart,
                (Reveal.OrientationProperty, vertical ? Orientation.Vertical : Orientation.Horizontal), (Reveal.FromEndProperty, fromEnd)));
            theme.Children.Add(Part<Border>(x => x.Nesting().Class(pseudo), SeparatorPart,
                (DockPanel.DockProperty, dock),
                (Layoutable.WidthProperty, vertical ? double.NaN : 1d),
                (Layoutable.HeightProperty, vertical ? 1d : double.NaN)));
        }
        return theme;
    }

    /// <summary>The header row: a caret and the header, with the list item fills.</summary>
    private static ControlTheme Header()
    {
        var theme = new ControlTheme(typeof(ToggleButton))
        {
            Setters =
            {
                new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(PaddingKey)),
                new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(MinHeightKey)),
                new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
                new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
                new Setter(Avalonia.Controls.Control.FocusAdornerProperty, null),
                new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),
                new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Stretch),
                new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Left),
                new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ToggleButton>(BuildHeader)),
            },
        };
        theme.Children.Add(ChromeMotion());
        theme.Children.Add(State((string?)null, new Look(Brushes.Transparent, Brushes.Transparent, "SystemControlForegroundBaseHighBrush")));
        theme.Children.Add(State(":pointerover", new Look("SystemControlHighlightListMediumBrush", Brushes.Transparent, "SystemControlHighlightAltBaseHighBrush")));
        theme.Children.Add(State(":pressed", new Look("SystemControlHighlightBaseMediumLowBrush", Brushes.Transparent, "SystemControlHighlightAltBaseHighBrush")));
        theme.Children.Add(State(":disabled", new Look(Brushes.Transparent, Brushes.Transparent, "SystemControlDisabledBaseMediumLowBrush")));
        return theme;
    }

    /// <summary>The caret's turn over <paramref name="duration"/>, or none when it is zero (reduce motion).</summary>
    public static Transitions CaretTransitions(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? []
        : [new TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = duration, Easing = easing }];

    private static Control Build(Expander expander, INameScope scope)
    {
        var header = new ToggleButton { Name = HeaderPart, Theme = HeaderTheme };
        header.Bind(ToggleButton.IsCheckedProperty, new TemplateBinding(Expander.IsExpandedProperty) { Mode = BindingMode.TwoWay });
        header.Bind(ContentControl.ContentProperty, new TemplateBinding(HeaderedContentControl.HeaderProperty));
        header.Bind(ContentControl.ContentTemplateProperty, new TemplateBinding(HeaderedContentControl.HeaderTemplateProperty));

        // The caret follows the expander's state and direction.
        Icon? caret = null;
        header.TemplateApplied += (_, e) =>
        {
            caret = e.NameScope.Find<Icon>(CaretPart);
            Turn();
        };
        void Turn()
        {
            if (caret is null)
                return;
            var angles = CaretAngles.FirstOrDefault(a => expander.Classes.Contains(a.Pseudo), CaretAngles[0]);
            caret.RenderTransform = TransformOperations.Parse(FormattableString.Invariant($"rotate({(expander.IsExpanded ? angles.Open : angles.Closed)}deg)"));
        }
        expander.PropertyChanged += (_, e) =>
        {
            if (e.Property == Expander.IsExpandedProperty || e.Property == Expander.ExpandDirectionProperty)
                Turn();
        };
        expander.Classes.CollectionChanged += (_, _) => Turn();
        Turn();

        var content = new ContentPresenter { Name = "PART_ContentPresenter" };
        content.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        content.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        content.Bind(ContentPresenter.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        content.Bind(ContentPresenter.HorizontalContentAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        content.Bind(ContentPresenter.VerticalContentAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));

        // Docking and the hairline's thickness come from the direction styles.
        var separator = new Border { Name = SeparatorPart };
        separator.Bind(Border.BackgroundProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        var reveal = new Reveal { Name = RevealPart, Child = new DockPanel { Children = { separator, content } } };

        var card = new SquircleBorder { Name = CardPart, Child = new DockPanel { Children = { header, reveal } } };
        card.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        card.Bind(SquircleBorder.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        card.Bind(SquircleBorder.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));
        card.Bind(SquircleBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));

        foreach (var part in new Control[] { card, header, reveal, separator, content })
            part.RegisterInNameScope(scope);
        return FocusRing.Wrap(card, scope);
    }

    private static Control BuildHeader(ToggleButton header, INameScope scope)
    {
        var caret = new Icon { Name = CaretPart, Data = Icons.CaretDown, VerticalAlignment = VerticalAlignment.Center };
        caret.Bind(Animatable.TransitionsProperty, new DynamicResourceExtension(CaretTransitionsKey));
        var presenter = new ContentPresenter { Name = ContentPart, RecognizesAccessKey = true };
        presenter.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        presenter.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        presenter.Bind(ContentPresenter.HorizontalContentAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        presenter.Bind(ContentPresenter.VerticalContentAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));
        caret.Bind(TemplatedControl.ForegroundProperty, presenter.GetObservable(ContentPresenter.ForegroundProperty).ToBinding());

        var row = new DockPanel { Children = { caret, presenter } };
        DockPanel.SetDock(caret, Dock.Left);
        caret.Margin = new Thickness(0, 0, Spacing.S, 0);
        var chrome = CreateChrome(row);
        chrome.Bind(Decorator.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        chrome.RegisterInNameScope(scope);
        caret.RegisterInNameScope(scope);
        presenter.RegisterInNameScope(scope);
        return chrome;
    }

    private static Avalonia.Styling.Style Part<T>(Func<Selector?, Selector> state, string name, params (AvaloniaProperty Property, object? Value)[] setters)
        where T : Control
    {
        var style = new Avalonia.Styling.Style(x => state(x).Template().OfType<T>().Name(name));
        foreach (var (property, value) in setters)
            style.Setters.Add(new Setter(property, value));
        return style;
    }
}
