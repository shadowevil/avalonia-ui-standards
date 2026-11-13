using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The CalendarDatePicker theme: Fluent's template drawn as a field (the field chrome, colours,
/// focus border and ring of the field Blocks) around Fluent's text box, with the calendar button
/// a field button showing <c>Icons.CalendarBlank</c>, and the Calendar in a glass drop-down.
/// </summary>
internal static class CalendarDatePickerTheme
{
    public const string PopupChromePart = "PART_PopupChrome";

    public static ControlTheme Create(ControlTheme? fluentTheme, ControlTheme fieldButton)
    {
        var theme = new ControlTheme(typeof(CalendarDatePicker)) { BasedOn = fluentTheme };
        FieldParts.AddSizing(theme, PaddingKey);
        if (FluentTemplates.Extend(fluentTheme, (root, scope) => Extend(root, scope, fieldButton)) is { } template)
            theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, template));
        FieldParts.AddColours(theme, FieldParts.TextControl, x => x.Nesting().Class(":focus-within"));
        theme.Children.Add(FieldParts.ChromeMotion());
        FocusRing.AddWithinTo(theme);
        FocusRing.TrackWithin(typeof(CalendarDatePicker));
        return theme;
    }

    private static Control Extend(Control root, INameScope scope, ControlTheme fieldButton)
    {
        // Fluent's rectangle-cornered field border gives way to the squircle field chrome.
        if (scope.Find<Border>("Background") is { Parent: Panel layout } fluentChrome)
            layout.Children[layout.Children.IndexOf(fluentChrome)] = FieldParts.CreateChrome();

        if (scope.Find<TextBox>("PART_TextBox") is { } text)
        {
            text.SetValue(Layoutable.MinHeightProperty, 0d);
            text.VerticalAlignment = VerticalAlignment.Center;
        }

        if (scope.Find<Button>("PART_Button") is { } button)
        {
            button.Theme = fieldButton;
            button.Content = FieldParts.ButtonIcon(Icons.CalendarBlank);
            button.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(FieldParts.ButtonSizeKey));
            button.Bind(Layoutable.MarginProperty, new DynamicResourceExtension(FieldParts.TrailingButtonsMarginKey));
        }

        if (scope.Find<Popup>("PART_Popup") is { Child: Calendar calendar } popup)
        {
            // The Calendar sits on glass: no panel fill or border of its own.
            popup.Child = null;
            popup.ShouldUseOverlayLayer = true;
            // A SpacingXS gap below the field, so the drop-down reads as its own surface.
            popup.VerticalOffset = Spacing.XS;
            calendar.Background = Avalonia.Media.Brushes.Transparent;
            calendar.BorderThickness = default;
            popup.Child = new GlassBorder { Name = PopupChromePart, Child = calendar };
        }
        return FocusRing.Wrap(root, new NameScope());
    }
}
