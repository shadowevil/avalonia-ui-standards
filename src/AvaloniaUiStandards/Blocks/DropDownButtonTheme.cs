using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The DropDownButton control theme: Fluent's template (content, then the chevron glyph) inside
/// the squircle chrome, with Fluent's Button resources and glyph colours. The glyph's right
/// margin follows density (the side padding) in place of Fluent's off-grid 10.
/// </summary>
internal static class DropDownButtonTheme
{
    public const string GlyphPart = "DropDownGlyph";

    public static ControlTheme Create()
    {
        var looks = Family("Button");
        var theme = new ControlTheme(typeof(DropDownButton));
        foreach (var setter in Sizing())
            theme.Setters.Add(setter);
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Resource("ButtonBackground")));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Resource("ButtonForeground")));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Resource("ButtonBorderBrush")));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, Resource("ButtonBorderThemeThickness")));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, FocusRing.Template));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<DropDownButton>(BuildTemplate)));

        theme.Children.Add(ChromeMotion());
        theme.Children.Add(PressedScale());
        theme.Children.Add(Glyph(null, "SystemControlForegroundBaseMediumHighBrush", margin: true));
        theme.Children.Add(State(":pointerover", looks.Hover));
        theme.Children.Add(Glyph(":pointerover", "ButtonForegroundPointerOver"));
        theme.Children.Add(State(":pressed", looks.Pressed));
        theme.Children.Add(Glyph(":pressed", "ButtonForegroundPressed"));
        theme.Children.Add(State(":disabled", looks.Disabled));
        theme.Children.Add(Glyph(":disabled", "ButtonForegroundDisabled"));
        return theme;
    }

    private static Control BuildTemplate(DropDownButton button, INameScope scope)
    {
        var presenter = CreatePresenter();
        var glyph = CreateChevron();
        glyph.Name = GlyphPart;
        glyph.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(glyph, 1);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { presenter, glyph } };
        var chrome = CreateChrome(grid);
        chrome.RegisterInNameScope(scope);
        presenter.RegisterInNameScope(scope);
        glyph.RegisterInNameScope(scope);
        return chrome;
    }

    private static Avalonia.Styling.Style Glyph(string? pseudoClass, string foregroundKey, bool margin = false)
    {
        var style = new Avalonia.Styling.Style(x =>
            (pseudoClass is null ? x.Nesting() : x.Nesting().Class(pseudoClass)).Template().OfType<PathIcon>().Name(GlyphPart))
        {
            Setters = { new Setter(TemplatedControl.ForegroundProperty, Resource(foregroundKey)) },
        };
        if (margin)
            style.Setters.Add(new Setter(Layoutable.MarginProperty, new DynamicResourceExtension(GlyphMarginKey)));
        return style;
    }
}
