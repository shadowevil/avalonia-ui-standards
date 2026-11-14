using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The StatusBar theme and the styles of its items: the bar is 24 high at every density and a
/// status bar to automation; text is Caption size in secondary text; icons are 16 px; clickable
/// items span the bar's height as their hit target, with their fill inset to 20 px, centred, and
/// their text 8 px into the fill. The focus ring follows the fill (<see cref="RingMargin"/>).
/// </summary>
internal static class StatusBarTheme
{
    /// <summary>The bar's automation name unless the app gives one, and the cancel button's name.</summary>
    public const string TextKey = "StatusBarText";
    public const string Text = "Status";
    public const string CancelTextKey = "StatusBarCancelText";
    public const string CancelText = "Cancel";

    /// <summary>A clickable item's fill height, centred in the bar; the icon size.</summary>
    public const double FillHeight = 20, IconSize = 16;

    private static readonly double FillInset = (StandardsWindowTheme.StatusBarHeight - FillHeight) / 2;

    /// <summary>
    /// The focus ring's margin on a clickable item: around the inset fill. The ring's margin is a
    /// local value in its template, so the bar sets it when the item's template is applied.
    /// </summary>
    public static readonly Thickness RingMargin = new(-FocusRing.Offset, FillInset - FocusRing.Offset);

    // The button family's border (ButtonBorderThemeThickness), inside the fill: the padding is the inset less it.
    private const double BorderWidth = 1;
    private const double Inset = StatusBar.ClickableInset - BorderWidth;

    public static ControlTheme Create() => new(typeof(StatusBar))
    {
        Setters =
        {
            new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Top),
            new Setter(AutomationProperties.ControlTypeOverrideProperty, AutomationControlType.StatusBar),
            new Setter(AutomationProperties.NameProperty, new DynamicResourceExtension(TextKey)),
        },
    };

    /// <summary>
    /// The item styles. A control theme reaches only its template, so these are theme-wide styles on
    /// the bar's items (its logical children) and on what their content holds, at most two levels
    /// down, so they do not reach into the menus the items open.
    /// </summary>
    public static IEnumerable<IStyle> ItemStyles()
    {
        foreach (var depth in new[] { 0, 1, 2 })
        {
            yield return new Avalonia.Styling.Style(x => Within(x, depth).OfType<TextBlock>())
            {
                Setters = { new Setter(TextBlock.FontSizeProperty, new DynamicResourceExtension("FontSizeCaption")) },
            };
            yield return new Avalonia.Styling.Style(x => Within(x, depth).OfType<TextBlock>().Not(n => n.Class(":disabled")))
            {
                Setters = { new Setter(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseMediumBrush")) },
            };
            yield return new Avalonia.Styling.Style(x => Within(x, depth).OfType<Icon>())
            {
                Setters =
                {
                    new Setter(Layoutable.WidthProperty, IconSize),
                    new Setter(Layoutable.HeightProperty, IconSize),
                    new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center),
                },
            };
        }

        // Clickable items: the bar's height as the hit target (the transparent template root), the
        // fill and the focus ring inset to 20 px, centred; secondary text unless disabled.
        yield return new Avalonia.Styling.Style(x => Clickable(x))
        {
            Setters =
            {
                new Setter(Layoutable.HeightProperty, StandardsWindowTheme.StatusBarHeight),
                new Setter(Layoutable.MinHeightProperty, 0d),
                new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(Inset, 0)),
                new Setter(TemplatedControl.FontSizeProperty, new DynamicResourceExtension("FontSizeCaption")),
            },
        };
        yield return new Avalonia.Styling.Style(x => Clickable(x).Template().OfType<Panel>())
        {
            Setters = { new Setter(Panel.BackgroundProperty, Avalonia.Media.Brushes.Transparent) },
        };
        yield return new Avalonia.Styling.Style(x => Clickable(x).Template().OfType<SquircleBorder>().Name(ButtonParts.ChromePart))
        {
            Setters = { new Setter(Layoutable.MarginProperty, new Thickness(0, FillInset)) },
        };
        yield return new Avalonia.Styling.Style(x => Clickable(x).Not(n => n.Class(":disabled")).Template().OfType<ContentPresenter>().Name(ButtonParts.ContentPart))
        {
            Setters = { new Setter(ContentPresenter.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseMediumBrush")) },
        };

        // A drop-down: the chevron SpacingXS after the text and 8 px from the fill's end.
        yield return new Avalonia.Styling.Style(x => x.OfType<DropDownButton>().PropertyEquals(StatusBar.IsClickableItemProperty, true))
        {
            Setters = { new Setter(TemplatedControl.PaddingProperty, new Thickness(Inset, 0, Spacing.XS, 0)) },
        };
        yield return new Avalonia.Styling.Style(x => x.OfType<DropDownButton>().PropertyEquals(StatusBar.IsClickableItemProperty, true)
            .Template().OfType<PathIcon>().Name(DropDownButtonTheme.GlyphPart))
        {
            Setters = { new Setter(Layoutable.MarginProperty, new Thickness(0, 0, Inset, 0)) },
        };
    }

    private static Selector Clickable(Selector? x) => x.Is<Button>().PropertyEquals(StatusBar.IsClickableItemProperty, true);

    // The bar's children (depth 0), their children (1) and those children's children (2).
    private static Selector Within(Selector? x, int depth)
    {
        var selector = x.Is<StatusBarRow>().Child();
        for (var i = 0; i < depth; i++)
            selector = selector.Is<Control>().Child();
        return selector;
    }
}
