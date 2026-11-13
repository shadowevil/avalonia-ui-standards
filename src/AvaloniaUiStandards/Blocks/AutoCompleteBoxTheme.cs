using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The AutoCompleteBox theme: Fluent's structure, its TextBox a TextInput (sizes, colours, focus
/// and ring are TextInput's), and the suggestions in the ComboBox drop-down: a squircle
/// (<c>CornerRadiusOverlay</c>) with Fluent's drop-down colours that clips the library ListBox,
/// whose padding insets the item squircles inside the curved corners.
/// </summary>
internal static class AutoCompleteBoxTheme
{
    public const string SuggestionsPart = "PART_SuggestionsContainer";

    public static ControlTheme Create(ControlTheme? fluentTheme)
    {
        var theme = new ControlTheme(typeof(AutoCompleteBox)) { BasedOn = fluentTheme };
        FieldParts.AddSizing(theme, PaddingKey);
        // The TextBox's focus ring lies outside the field; nothing around it may clip it.
        theme.Setters.Add(new Setter(Visual.ClipToBoundsProperty, false));
        // Raised while focus is inside, so neighbours do not paint over the TextBox's ring.
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":focus-within"))
        {
            Setters = { new Setter(Visual.ZIndexProperty, 1) },
        });
        theme.Setters.Add(new Setter(AutoCompleteBox.PlaceholderForegroundProperty, Resource("TextControlPlaceholderForeground")));
        if (FluentTemplates.Extend(fluentTheme, Extend) is { } template)
            theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, template));
        return theme;
    }

    private static Control Extend(Control root, INameScope scope)
    {
        root.ClipToBounds = false;
        if (scope.Find<Popup>("PART_Popup") is not { Child: Border fluentChrome } popup || fluentChrome.Child is not ListBox list)
            return root;

        // Fluent's rectangle-cornered container is replaced; the list's own padding insets the items.
        fluentChrome.Child = null;
        popup.Child = null;
        list.Margin = default;

        var chrome = new SquircleBorder { Name = SuggestionsPart, Child = list };
        chrome.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("ComboBoxDropDownBackground"));
        chrome.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("ComboBoxDropDownBorderBrush"));
        chrome.Bind(SquircleBorder.BorderThicknessProperty, new DynamicResourceExtension("ComboBoxDropdownBorderThickness"));
        chrome.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusOverlay"));
        popup.Child = chrome;
        return root;
    }
}
