using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The TextInput Block, the TextBox theme: Fluent's template structure and part names, with the
/// field chrome a squircle (<c>PART_BorderElement</c>), the density height and padding of Button,
/// Fluent's TextControl colours, a constant 1 px border that turns accent on focus and danger on
/// error, the focus ring on keyboard focus, a legible placeholder, the accent selection with
/// white text, and clear and reveal-password buttons as subtle icon buttons that fade in.
/// </summary>
internal static class TextInputTheme
{
    public const string InnerDockPanelPart = "PART_InnerDockPanel";
    public const string FloatingPlaceholderPart = "PART_FloatingPlaceholder";
    public const string ScrollViewerPart = "PART_ScrollViewer";
    public const string PlaceholderPart = "PART_Placeholder";
    public const string TextPresenterPart = "PART_TextPresenter";
    public const string ButtonsPart = "PART_InnerButtons";
    public const string ClearButtonPart = "PART_ClearButton";
    public const string RevealButtonPart = "PART_RevealButton";
    public const string RevealIconPart = "PART_RevealIcon";

    /// <summary>The full TextInput theme.</summary>
    public static ControlTheme Create(ControlTheme fieldButton)
    {
        var theme = Base(fieldButton);
        FieldParts.AddSizing(theme, PaddingKey);
        FieldParts.AddColours(theme, FieldParts.TextControl, x => x.Nesting().Class(":focus"));
        theme.Children.Add(ChromeMotion(FieldParts.ChromePart));
        FocusRing.AddTo(theme);
        return theme;
    }

    /// <summary>
    /// The TextBox inside another field (an editable ComboBox, a NumericUpDown): no chrome, no
    /// ring and no state fills of its own, as the field around it draws them; the same text,
    /// placeholder, selection and padding as TextInput.
    /// </summary>
    public static ControlTheme CreateInner(ControlTheme fieldButton)
    {
        var theme = Base(fieldButton);
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Resource("TextControlForeground")));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":disabled"))
        {
            Setters = { new Setter(TemplatedControl.ForegroundProperty, Resource("TextControlForegroundDisabled")) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<SquircleBorder>().Name(FocusRing.Part))
        {
            Setters = { new Setter(Visual.IsVisibleProperty, false) },
        });
        return theme;
    }

    // What both themes share: the template, text colours, selection, placeholder, alignment and the inner buttons.
    private static ControlTheme Base(ControlTheme fieldButton)
    {
        var theme = new ControlTheme(typeof(TextBox));
        theme.Setters.Add(new Setter(TextBox.CaretBrushProperty, Resource("TextControlForeground")));
        theme.Setters.Add(new Setter(TextBox.SelectionBrushProperty, Resource("AccentFillColor")));
        theme.Setters.Add(new Setter(TextBox.SelectionForegroundBrushProperty, Resource("AccentButtonForeground")));
        theme.Setters.Add(new Setter(TextBox.PlaceholderForegroundProperty, Resource("TextControlPlaceholderForeground")));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(ScrollViewer.IsScrollChainingEnabledProperty, true));
        theme.Setters.Add(new Setter(Control.ContextFlyoutProperty, Resource("DefaultTextBoxContextFlyout")));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TextBox>((box, scope) => Build(box, scope, fieldButton))));

        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":touch-mode"))
        {
            Setters = { new Setter(Control.ContextFlyoutProperty, Resource("HorizontalTextBoxContextFlyout")) },
        });
        // Multi-line text starts at the top padding; single-line text is centred in the field.
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().PropertyEquals(TextBox.AcceptsReturnProperty, true))
        {
            Setters = { new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Top) },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":disabled"))
        {
            Setters = { new Setter(TextBox.PlaceholderForegroundProperty, Resource("TextControlForegroundDisabled")) },
        });

        // Placeholder: shown while empty (TextBox also clears :empty while an input method composes).
        theme.Children.Add(FieldParts.Part<TextBlock>(PlaceholderPart, null, Visual.IsVisibleProperty, false));
        theme.Children.Add(FieldParts.Part<TextBlock>(PlaceholderPart, x => x.Class(":empty"), Visual.IsVisibleProperty, true));
        theme.Children.Add(FieldParts.Part<TextBlock>(FloatingPlaceholderPart, null, Visual.IsVisibleProperty, false));
        theme.Children.Add(FieldParts.Part<TextBlock>(FloatingPlaceholderPart,
            x => x.PropertyEquals(TextBox.UseFloatingPlaceholderProperty, true).Not(y => y.Class(":empty")), Visual.IsVisibleProperty, true));

        // The inner buttons take their place in the layout as soon as their class is set (so
        // the text never reflows when they appear), then fade in when they apply.
        theme.Children.Add(FieldParts.Part<StackPanel>(ButtonsPart, null, Visual.IsVisibleProperty, false));
        theme.Children.Add(FieldParts.Part<StackPanel>(ButtonsPart, x => x.Class("clearButton"), Visual.IsVisibleProperty, true));
        theme.Children.Add(FieldParts.Part<StackPanel>(ButtonsPart, x => x.Class("revealPasswordButton"), Visual.IsVisibleProperty, true));
        AddInnerButton(theme, ClearButtonPart, "clearButton", x => x.Class(":focus").Not(y => y.Class(":empty")));
        AddInnerButton(theme, RevealButtonPart, "revealPasswordButton", x => x.Not(y => y.Class(":empty")));
        theme.Children.Add(FieldParts.Part<Icon>(RevealIconPart, null, PathIcon.DataProperty, Icons.Eye));
        theme.Children.Add(FieldParts.Part<Icon>(RevealIconPart, x => x.PropertyEquals(TextBox.RevealPasswordProperty, true), PathIcon.DataProperty, Icons.EyeSlash));
        return theme;
    }

    /// <summary>
    /// A clear or reveal button: hidden without its class, in the layout but transparent and
    /// click-through with it, and shown (fading over MotionFast) when <paramref name="shown"/>
    /// also holds. As in Fluent, neither applies to multi-line or read-only text.
    /// </summary>
    private static void AddInnerButton(ControlTheme theme, string part, string cls, Func<Selector, Selector> shown)
    {
        Selector Applies(Selector? x) => x.Nesting().Class(cls)
            .PropertyEquals(TextBox.AcceptsReturnProperty, false).PropertyEquals(TextBox.IsReadOnlyProperty, false);
        Selector Button(Selector state) => state.Template().Is<Button>().Name(part);

        theme.Children.Add(new Avalonia.Styling.Style(x => Button(x.Nesting()))
        {
            Setters =
            {
                new Setter(Visual.IsVisibleProperty, false),
                new Setter(Visual.OpacityProperty, 0d),
                new Setter(InputElement.IsHitTestVisibleProperty, false),
                new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(FadeTransitionsKey)),
            },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => Button(Applies(x))) { Setters = { new Setter(Visual.IsVisibleProperty, true) } });
        theme.Children.Add(new Avalonia.Styling.Style(x => Button(shown(Applies(x))))
        {
            Setters = { new Setter(Visual.OpacityProperty, 1d), new Setter(InputElement.IsHitTestVisibleProperty, true) },
        });
    }

    private static Control Build(TextBox box, INameScope scope, ControlTheme fieldButton)
    {
        var chrome = FieldParts.CreateChrome();
        chrome.Bind(Layoutable.MinWidthProperty, new TemplateBinding(Layoutable.MinWidthProperty));
        chrome.Bind(Layoutable.MinHeightProperty, new TemplateBinding(Layoutable.MinHeightProperty));

        var left = new ContentPresenter();
        left.Bind(ContentPresenter.ContentProperty, new TemplateBinding(TextBox.InnerLeftContentProperty));
        left.Bind(ContentPresenter.VerticalContentAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));

        var floating = new TextBlock { Name = FloatingPlaceholderPart, Cursor = FieldParts.IBeam };
        DockPanel.SetDock(floating, Dock.Top);
        floating.Bind(TextBlock.ForegroundProperty, new TemplateBinding(TextBox.PlaceholderForegroundProperty));
        floating.Bind(TextBlock.TextProperty, new TemplateBinding(TextBox.PlaceholderTextProperty));

        var placeholder = new TextBlock { Name = PlaceholderPart };
        placeholder.Bind(TextBlock.ForegroundProperty, new TemplateBinding(TextBox.PlaceholderForegroundProperty));
        placeholder.Bind(TextBlock.TextProperty, new TemplateBinding(TextBox.PlaceholderTextProperty));
        placeholder.Bind(TextBlock.TextAlignmentProperty, new TemplateBinding(TextBox.TextAlignmentProperty));
        placeholder.Bind(TextBlock.TextWrappingProperty, new TemplateBinding(TextBox.TextWrappingProperty));
        placeholder.Bind(Layoutable.HorizontalAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        placeholder.Bind(Layoutable.VerticalAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));

        var presenter = new TextPresenter { Name = TextPresenterPart };
        presenter.Bind(TextPresenter.TextProperty, new TemplateBinding(TextBox.TextProperty) { Mode = BindingMode.TwoWay });
        foreach (var (target, source) in new (AvaloniaProperty, AvaloniaProperty)[]
                 {
                     (TextPresenter.CaretBlinkIntervalProperty, TextBox.CaretBlinkIntervalProperty),
                     (TextPresenter.CaretIndexProperty, TextBox.CaretIndexProperty),
                     (TextPresenter.SelectionStartProperty, TextBox.SelectionStartProperty),
                     (TextPresenter.SelectionEndProperty, TextBox.SelectionEndProperty),
                     (TextPresenter.TextAlignmentProperty, TextBox.TextAlignmentProperty),
                     (TextPresenter.TextWrappingProperty, TextBox.TextWrappingProperty),
                     (TextPresenter.LineHeightProperty, TextBox.LineHeightProperty),
                     (TextPresenter.LetterSpacingProperty, TemplatedControl.LetterSpacingProperty),
                     (TextPresenter.PasswordCharProperty, TextBox.PasswordCharProperty),
                     (TextPresenter.RevealPasswordProperty, TextBox.RevealPasswordProperty),
                     (TextPresenter.SelectionBrushProperty, TextBox.SelectionBrushProperty),
                     (TextPresenter.SelectionForegroundBrushProperty, TextBox.SelectionForegroundBrushProperty),
                     (TextPresenter.CaretBrushProperty, TextBox.CaretBrushProperty),
                     (Layoutable.HorizontalAlignmentProperty, ContentControl.HorizontalContentAlignmentProperty),
                     (Layoutable.VerticalAlignmentProperty, ContentControl.VerticalContentAlignmentProperty),
                 })
            presenter.Bind(target, new TemplateBinding(source));

        var scroller = new ScrollViewer { Name = ScrollViewerPart, Cursor = FieldParts.IBeam, Content = new Panel { Children = { placeholder, presenter } } };
        foreach (var property in new AvaloniaProperty[]
                 {
                     ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollViewer.VerticalScrollBarVisibilityProperty,
                     ScrollViewer.IsScrollChainingEnabledProperty, ScrollViewer.AllowAutoHideProperty,
                     ScrollViewer.BringIntoViewOnFocusChangeProperty,
                 })
            scroller.Bind(property, new TemplateBinding(property));

        var dock = new DockPanel { Name = InnerDockPanelPart, Children = { floating, scroller } };
        dock.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        Grid.SetColumn(dock, 1);

        var clear = new Button { Name = ClearButtonPart, Theme = fieldButton, Focusable = false, Content = FieldParts.ButtonIcon(Icons.XCircle) };
        clear.Click += (_, _) => box.Clear();
        var revealIcon = FieldParts.ButtonIcon();
        revealIcon.Name = RevealIconPart;
        var reveal = new ToggleButton { Name = RevealButtonPart, Theme = fieldButton, Focusable = false, Content = revealIcon };
        reveal.Bind(ToggleButton.IsCheckedProperty, new TemplateBinding(TextBox.RevealPasswordProperty) { Mode = BindingMode.TwoWay });
        var buttons = new StackPanel { Name = ButtonsPart, Orientation = Orientation.Horizontal, Children = { clear, reveal } };
        buttons.Bind(Layoutable.MarginProperty, new DynamicResourceExtension(FieldParts.TrailingButtonsMarginKey));
        Grid.SetColumn(buttons, 2);

        var right = new ContentPresenter();
        right.Bind(ContentPresenter.ContentProperty, new TemplateBinding(TextBox.InnerRightContentProperty));
        right.Bind(ContentPresenter.VerticalContentAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));
        Grid.SetColumn(right, 3);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"), Children = { left, dock, buttons, right } };
        var inner = new Border { Child = grid };
        inner.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));

        foreach (var part in new Control[] { chrome, dock, floating, scroller, placeholder, presenter, buttons, clear, reveal, revealIcon })
            part.RegisterInNameScope(scope);

        var field = FocusRing.Wrap(new Panel { Children = { chrome, inner } }, scope);
        // Fluent's root, so validation errors show as in Fluent.
        return new DataValidationErrors { Content = field, ClipToBounds = false };
    }
}
