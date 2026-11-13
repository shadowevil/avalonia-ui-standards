using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The ToolTip theme: Fluent's tooltip (its delays, placement and open fade) drawn as a small
/// solid card in the card colour with a 1 px border in the border colour, the control radius
/// (<c>CornerRadiusControl</c>), caption text and <c>SpacingS</c> / <c>SpacingXS</c> padding.
/// The fade is the fixed <c>MotionFast</c> opacity transition, none under reduce motion.
/// </summary>
internal static class ToolTipTheme
{
    /// <summary>The padding around the text: <c>SpacingS</c> at the sides, <c>SpacingXS</c> above and below.</summary>
    public static readonly Thickness Padding = new(Spacing.S, Spacing.XS);

    public static ControlTheme Create(ControlTheme? fluentTheme) => new(typeof(ToolTip))
    {
        BasedOn = fluentTheme,
        Setters =
        {
            new Setter(TemplatedControl.PaddingProperty, Padding),
            new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")),
            new Setter(TemplatedControl.FontSizeProperty, Resource("FontSizeCaption")),
            new Setter(TemplatedControl.BackgroundProperty, Resource("SystemControlBackgroundChromeMediumLowBrush")),
            new Setter(TemplatedControl.BorderBrushProperty, Resource("SystemControlForegroundBaseLowBrush")),
            new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
            new Setter(Animatable.TransitionsProperty, new DynamicResourceExtension(FadeTransitionsKey)),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ToolTip>(Build)),
        },
    };

    private static Control Build(ToolTip tip, INameScope scope)
    {
        var content = new ContentPresenter { Name = ContentPart, VerticalAlignment = VerticalAlignment.Center, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        content.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        content.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        content.Bind(ContentPresenter.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));

        var card = CreateChrome(content);
        card.Name = OverlayParts.SurfacePart;
        card.RegisterInNameScope(scope);
        content.RegisterInNameScope(scope);
        return card;
    }
}
