using Avalonia;
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
/// The ComboBox theme. The closed field is the TextInput chrome (squircle, density height, 1 px
/// border) with Fluent's ComboBox colours and a trailing zone, as wide as the field is high,
/// holding the caret-down icon. Editable, it is a text field: an inner borderless TextBox, the
/// accent border while focused, the caret zone opening the list. The drop-down is a squircle
/// (<c>CornerRadiusOverlay</c>) that clips its content and insets the items by <c>SpacingXS</c>,
/// so the item squircles sit inside its curved corners. Keyboard focus, on the ComboBox or on its
/// inner TextBox, shows the ring around the field.
/// </summary>
internal static class ComboBoxTheme
{
    public const string PopupChromePart = "PART_PopupChrome";
    public const string PopupPart = "PART_Popup";
    public const string ItemsPresenterPart = "PART_ItemsPresenter";
    public const string EditableTextBoxPart = "PART_EditableTextBox";
    public const string PlaceholderPart = "PlaceholderTextBlock";
    public const string ContentPart = "ContentPresenter";
    public const string GlyphPart = "DropDownGlyph";
    public const string GlyphZonePart = "DropDownOverlay";

    // The padding per density: TextInput's, with no right padding, as the caret zone follows the content.
    internal static readonly object PaddingKey = new();

    public static Thickness Padding(Thickness fieldPadding) => new(fieldPadding.Left, fieldPadding.Top, 0, fieldPadding.Bottom);

    public static ControlTheme Create(ControlTheme innerTextBox)
    {
        FocusRing.TrackWithin(typeof(ComboBox));
        var theme = new ControlTheme(typeof(ComboBox));
        FieldParts.AddSizing(theme, PaddingKey);
        theme.Setters.Add(new Setter(ComboBox.MaxDropDownHeightProperty, 504d));
        theme.Setters.Add(new Setter(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled));
        theme.Setters.Add(new Setter(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Left));
        theme.Setters.Add(new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Top));
        theme.Setters.Add(new Setter(ComboBox.PlaceholderForegroundProperty, Resource("TextControlPlaceholderForeground")));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ComboBox>((box, scope) => Build(box, scope, innerTextBox))));

        FieldParts.AddColours(theme,
            new FieldParts.Colours(
                new("ComboBoxBackground", "ComboBoxBorderBrush", "ComboBoxForeground"),
                new("ComboBoxBackgroundPointerOver", "ComboBoxBorderBrushPointerOver", "ComboBoxForeground"),
                FieldParts.TextControl.Focused,
                new("ComboBoxBackgroundDisabled", "ComboBoxBorderBrushDisabled", "ComboBoxForegroundDisabled")),
            // Editable, it is a text field: focused while its text has focus. Pressed keeps the
            // hover look (the list opening is the feedback): Fluent's pressed fill would drop the
            // placeholder below 4.5:1.
            focus: x => x.Nesting().PropertyEquals(ComboBox.IsEditableProperty, true).Class(":focus-within"));
        theme.Children.Add(ChromeMotion(FieldParts.ChromePart));
        FocusRing.AddWithinTo(theme);

        theme.Children.Add(FieldParts.Part<Icon>(GlyphPart, null, TemplatedControl.ForegroundProperty, Resource("ComboBoxDropDownGlyphForeground")));
        theme.Children.Add(FieldParts.Part<Icon>(GlyphPart, x => x.Class(":disabled"), TemplatedControl.ForegroundProperty, Resource("ComboBoxDropDownGlyphForegroundDisabled")));
        theme.Children.Add(FieldParts.Part<TextBlock>(PlaceholderPart, x => x.Class(":disabled"), TextBlock.ForegroundProperty, Resource("TextControlForegroundDisabled")));

        // The placeholder shows while nothing is selected and the ComboBox is not editable (the
        // editable TextBox shows its own); the selection shows unless editable.
        theme.Children.Add(FieldParts.Part<TextBlock>(PlaceholderPart, null, Visual.IsVisibleProperty, false));
        theme.Children.Add(FieldParts.Part<TextBlock>(PlaceholderPart,
            x => x.PropertyEquals(ComboBox.SelectionBoxItemProperty, null).PropertyEquals(ComboBox.IsEditableProperty, false),
            Visual.IsVisibleProperty, true));

        // Editable: not a tab stop itself (the TextBox is). Local tab navigation is Fluent's, kept
        // so Tab moves through the ComboBox's parts as in Fluent.
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().PropertyEquals(ComboBox.IsEditableProperty, true))
        {
            Setters =
            {
                new Setter(InputElement.IsTabStopProperty, false),
                new Setter(KeyboardNavigation.TabNavigationProperty, KeyboardNavigationMode.Local),
            },
        });
        return theme;
    }

    private static Control Build(ComboBox box, INameScope scope, ControlTheme innerTextBox)
    {
        var chrome = FieldParts.CreateChrome();
        Grid.SetColumnSpan(chrome, 2);

        var placeholder = new TextBlock { Name = PlaceholderPart, TextTrimming = TextTrimming.CharacterEllipsis };
        placeholder.Bind(TextBlock.TextProperty, new TemplateBinding(ComboBox.PlaceholderTextProperty));
        placeholder.Bind(TextBlock.ForegroundProperty, new TemplateBinding(ComboBox.PlaceholderForegroundProperty));
        placeholder.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        placeholder.Bind(Layoutable.HorizontalAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        placeholder.Bind(Layoutable.VerticalAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));

        var content = new ContentControl { Name = ContentPart };
        content.Bind(ContentControl.ContentProperty, new TemplateBinding(ComboBox.SelectionBoxItemProperty));
        content.Bind(ContentControl.ContentTemplateProperty, new TemplateBinding(ComboBox.SelectionBoxItemTemplateProperty));
        content.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        content.Bind(ContentControl.HorizontalContentAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        content.Bind(ContentControl.VerticalContentAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));
        content.Bind(Visual.IsVisibleProperty, new TemplateBinding(ComboBox.IsEditableProperty) { Converter = Avalonia.Data.Converters.BoolConverters.Not });

        var text = new TextBox { Name = EditableTextBoxPart, Theme = innerTextBox, VerticalAlignment = VerticalAlignment.Stretch };
        text.Bind(TemplatedControl.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        text.Bind(TemplatedControl.ForegroundProperty, new TemplateBinding(TemplatedControl.ForegroundProperty));
        text.Bind(TemplatedControl.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));
        text.Bind(TextBox.TextProperty, new TemplateBinding(ComboBox.TextProperty) { Mode = BindingMode.TwoWay });
        text.Bind(TextBox.PlaceholderTextProperty, new TemplateBinding(ComboBox.PlaceholderTextProperty));
        text.Bind(TextBox.PlaceholderForegroundProperty, new TemplateBinding(ComboBox.PlaceholderForegroundProperty));
        text.Bind(ContentControl.VerticalContentAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));
        text.Bind(Visual.IsVisibleProperty, new TemplateBinding(ComboBox.IsEditableProperty));

        // The caret zone: square to the field height, transparent so it takes clicks when editable.
        var zone = new Border { Name = GlyphZonePart, Background = Brushes.Transparent };
        zone.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(MinHeightKey));
        Grid.SetColumn(zone, 1);
        var glyph = new Icon
        {
            Name = GlyphPart, Data = Icons.CaretDown, Width = FieldParts.IconSize, Height = FieldParts.IconSize,
            IsHitTestVisible = false, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(glyph, 1);

        var popup = CreatePopup(box, chrome);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { chrome, placeholder, content, text, zone, glyph, popup } };
        foreach (var part in new Control[] { chrome, placeholder, content, text, zone, glyph })
            part.RegisterInNameScope(scope);

        // Fluent's root, so validation errors show as in Fluent; it must not clip the ring.
        return new DataValidationErrors { Content = FocusRing.Wrap(grid, scope), ClipToBounds = false };

        Popup CreatePopup(ComboBox owner, Control target)
        {
            var items = new ItemsPresenter { Name = ItemsPresenterPart };
            items.Bind(ItemsPresenter.ItemsPanelProperty, new TemplateBinding(ItemsControl.ItemsPanelProperty));

            var scroller = new ScrollViewer { Content = items, IsScrollChainingEnabled = false };
            scroller.Bind(ScrollViewer.HorizontalScrollBarVisibilityProperty, new TemplateBinding(ScrollViewer.HorizontalScrollBarVisibilityProperty));
            scroller.Bind(ScrollViewer.VerticalScrollBarVisibilityProperty, new TemplateBinding(ScrollViewer.VerticalScrollBarVisibilityProperty));
            scroller.Bind(ScrollViewer.IsDeferredScrollingEnabledProperty, new TemplateBinding(ScrollViewer.IsDeferredScrollingEnabledProperty));

            var popupChrome = new SquircleBorder { Name = PopupChromePart, Padding = new Thickness(Spacing.XS), Child = scroller };
            popupChrome.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("ComboBoxDropDownBackground"));
            popupChrome.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("ComboBoxDropDownBorderBrush"));
            popupChrome.Bind(SquircleBorder.BorderThicknessProperty, new DynamicResourceExtension("ComboBoxDropdownBorderThickness"));
            popupChrome.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusOverlay"));

            var result = new Popup
            {
                Name = PopupPart,
                Child = popupChrome,
                PlacementTarget = target,
                WindowManagerAddShadowHint = false,
                IsLightDismissEnabled = true,
                InheritsTransform = true,
            };
            result.Bind(Popup.IsOpenProperty, new TemplateBinding(ComboBox.IsDropDownOpenProperty) { Mode = BindingMode.TwoWay });
            result.Bind(Layoutable.MinWidthProperty, new Binding("Bounds.Width") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            result.Bind(Layoutable.MaxHeightProperty, new TemplateBinding(ComboBox.MaxDropDownHeightProperty));
            foreach (var part in new Control[] { result, popupChrome, items })
                part.RegisterInNameScope(scope);
            return result;
        }
    }
}
