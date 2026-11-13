using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Path = Avalonia.Controls.Shapes.Path;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The <see cref="Icon"/> theme: a viewbox over a fixed 256 x 256 canvas holding the path
/// unstretched, so the design box, not the geometry's bounds, is scaled to the icon's size.
/// The size follows density; no foreground is set, so the inherited one fills the path.
/// </summary>
internal static class IconTheme
{
    /// <summary>The design box of the icon set, in its own units.</summary>
    public const double DesignBox = 256;

    /// <summary>The icon size at Normal and at Compact density.</summary>
    public const double Normal = 20, Compact = 16;

    // The density-dependent size, set by BlocksThemes.ApplyDensity under this non-public key.
    internal static readonly object SizeKey = new();

    public static ControlTheme Create() => new(typeof(Icon))
    {
        Setters =
        {
            new Setter(Layoutable.WidthProperty, new DynamicResourceExtension(SizeKey)),
            new Setter(Layoutable.HeightProperty, new DynamicResourceExtension(SizeKey)),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Icon>(Build)),
        },
    };

    private static Control Build(Icon icon, INameScope scope)
    {
        var path = new Path { Stretch = Stretch.None };
        path.Bind(Path.DataProperty, new TemplateBinding(PathIcon.DataProperty));
        path.Bind(Shape.FillProperty, new TemplateBinding(TemplatedControl.ForegroundProperty));

        var box = new Canvas { Width = DesignBox, Height = DesignBox, Children = { path } };
        var border = new Border { Child = new Viewbox { Stretch = Stretch.Uniform, Child = box } };
        border.Bind(Border.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        return border;
    }
}
