using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// Extends Fluent's own template for a control so a theme can keep it and change only a part
/// (the ScrollViewer's shadow layer, the ComboBox popup's chrome), keeping Fluent's structure,
/// behaviour, styles and named parts.
/// </summary>
internal static class FluentTemplates
{
    /// <summary>
    /// A template that builds the one <paramref name="fluentTheme"/> sets, then lets
    /// <paramref name="extend"/> change the built tree. The result keeps Fluent's own name scope,
    /// so Fluent's named parts resolve as in Fluent; parts the extension adds are named but found
    /// in the visual tree (a built template's scope takes no further names). Null when the
    /// Fluent theme sets no template.
    /// </summary>
    public static IControlTemplate? Extend(ControlTheme? fluentTheme, Func<Control, INameScope, Control> extend) =>
        Of(fluentTheme) is { } template ? new Extended(template, extend) : null;

    private static IControlTemplate? Of(ControlTheme? fluentTheme) =>
        fluentTheme?.Setters.OfType<Setter>().FirstOrDefault(s => s.Property == TemplatedControl.TemplateProperty)?.Value as IControlTemplate;

    private sealed class Extended(IControlTemplate fluent, Func<Control, INameScope, Control> extend) : IControlTemplate
    {
        public TemplateResult<Control>? Build(TemplatedControl param) =>
            fluent.Build(param) is { } built ? new TemplateResult<Control>(extend(built.Result, built.NameScope), built.NameScope) : null;
    }
}
