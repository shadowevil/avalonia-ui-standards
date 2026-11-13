using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The ComboBox theme: Fluent's ComboBox, its closed field unchanged, with the drop-down drawn as
/// a squircle (<c>CornerRadiusOverlay</c>) that clips its content and insets the items by
/// <c>SpacingXS</c>, so the item squircles sit inside its curved corners.
/// </summary>
internal static class ComboBoxTheme
{
    public const string PopupChromePart = "PART_PopupChrome";

    public static ControlTheme Create(ControlTheme? fluentTheme)
    {
        var theme = new ControlTheme(typeof(ComboBox)) { BasedOn = fluentTheme };
        if (FluentTemplates.Extend(fluentTheme, Extend) is { } template)
            theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, template));
        return theme;
    }

    private static Control Extend(Control root, INameScope scope)
    {
        if (scope.Find<Popup>("PART_Popup") is not { Child: Border fluentChrome } popup)
            return root;

        // Fluent's rectangle-cornered drop-down border stays as a plain container inside the squircle.
        popup.Child = null;
        fluentChrome.Background = Brushes.Transparent;
        fluentChrome.BorderThickness = default;
        fluentChrome.CornerRadius = default;
        fluentChrome.Padding = default;
        if (scope.Find<ItemsPresenter>("PART_ItemsPresenter") is { } items)
            items.Margin = default;

        var chrome = new SquircleBorder { Name = PopupChromePart, Padding = new Thickness(Spacing.XS), Child = fluentChrome };
        chrome.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("ComboBoxDropDownBackground"));
        chrome.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("ComboBoxDropDownBorderBrush"));
        chrome.Bind(SquircleBorder.BorderThicknessProperty, new DynamicResourceExtension("ComboBoxDropdownBorderThickness"));
        chrome.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusOverlay"));
        popup.Child = chrome;
        return root;
    }
}
