using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The SplitButton control theme (also used by ToggleSplitButton), following Fluent's structure:
/// a primary and a secondary (chevron) part, each a Button with an explicit component theme,
/// with a separator between them filled by the border brush. Each part's squircle keeps only its
/// outer corners, so together they form the control's squircle. Fluent's <c>Tag</c> states
/// (<c>flyout-open</c>, <c>checked</c>, <c>checked-flyout-open</c>) drive the parts; checked
/// uses the AA accent fills.
/// </summary>
internal static class SplitButtonTheme
{
    public const string PrimaryPart = "PART_PrimaryButton";
    public const string SecondaryPart = "PART_SecondaryButton";
    public const string SeparatorPart = "SeparatorBorder";

    private static readonly IValueConverter LeftCorners =
        new FuncValueConverter<CornerRadius, CornerRadius>(r => new CornerRadius(r.TopLeft, 0, 0, r.BottomLeft));
    private static readonly IValueConverter RightCorners =
        new FuncValueConverter<CornerRadius, CornerRadius>(r => new CornerRadius(0, r.TopRight, r.BottomRight, 0));
    private static readonly IValueConverter LeftThickness =
        new FuncValueConverter<Thickness, Thickness>(t => new Thickness(t.Left, t.Top, 0, t.Bottom));
    private static readonly IValueConverter RightThickness =
        new FuncValueConverter<Thickness, Thickness>(t => new Thickness(0, t.Top, t.Right, t.Bottom));

    /// <summary>The theme of the two parts, as Fluent's FluentSplitButtonComponent.</summary>
    internal static readonly ControlTheme Component = CreateComponent();

    public static ControlTheme Create()
    {
        var theme = new ControlTheme(typeof(SplitButton));
        foreach (var setter in Sizing())
            theme.Setters.Add(setter);
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Resource("SplitButtonBackground")));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Resource("SplitButtonForeground")));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Resource("SplitButtonBorderBrush")));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, Resource("SplitButtonBorderThemeThickness")));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(KeyboardNavigation.IsTabStopProperty, true));
        theme.Setters.Add(new Setter(InputElement.FocusableProperty, true));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, FocusRing.Template));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<SplitButton>(BuildTemplate)));

        theme.Children.Add(PartTag(x => x.Nesting().Class(":flyout-open"), "flyout-open"));
        theme.Children.Add(PartTag(x => x.Nesting().Class(":checked"), "checked"));
        theme.Children.Add(PartTag(x => x.Nesting().Class(":checked").Class(":flyout-open"), "checked-flyout-open"));
        return theme;
    }

    private static Control BuildTemplate(SplitButton splitButton, INameScope scope)
    {
        var primary = Part(PrimaryPart, LeftCorners, LeftThickness);
        primary.Bind(ContentControl.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        primary.Bind(ContentControl.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        primary.Bind(TemplatedControl.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        primary.Bind(ContentControl.HorizontalContentAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        primary.Bind(ContentControl.VerticalContentAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));

        var separator = new Border { Name = SeparatorPart, BorderThickness = new Thickness(0, 1, 0, 1) };
        separator.Bind(Layoutable.WidthProperty, new DynamicResourceExtension("SplitButtonSeparatorWidth"));
        separator.Bind(Border.BackgroundProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        separator.Bind(Border.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        Grid.SetColumn(separator, 1);

        var secondary = Part(SecondaryPart, RightCorners, RightThickness);
        secondary.Content = CreateChevron();
        secondary.Padding = default;
        secondary.HorizontalContentAlignment = HorizontalAlignment.Center;
        secondary.VerticalContentAlignment = VerticalAlignment.Center;
        Grid.SetColumn(secondary, 2);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), Children = { primary, separator, secondary } };
        primary.RegisterInNameScope(scope);
        separator.RegisterInNameScope(scope);
        secondary.RegisterInNameScope(scope);
        return grid;
    }

    // A part: a Button with the component theme, the control's brushes, its side of the
    // corners and border, and the density height as its minimum width (Fluent's 32 square).
    private static Button Part(string name, IValueConverter corners, IValueConverter thickness)
    {
        var part = new Button
        {
            Name = name,
            Theme = Component,
            Focusable = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        part.Bind(TemplatedControl.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        part.Bind(TemplatedControl.BorderBrushProperty, new TemplateBinding(TemplatedControl.BorderBrushProperty));
        part.Bind(TemplatedControl.BorderThicknessProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty) { Converter = thickness });
        part.Bind(TemplatedControl.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty) { Converter = corners });
        part.Bind(Layoutable.MinWidthProperty, new DynamicResourceExtension(MinHeightKey));
        return part;
    }

    private static ControlTheme CreateComponent()
    {
        var looks = Family("SplitButton");
        var checkedPressed = new Look("AccentFillColorPressed", "SplitButtonBorderBrushCheckedPressed", "SplitButtonForegroundCheckedPressed");
        return new ControlTheme(typeof(Button))
        {
            Setters = { new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ContentControl>(BuildContentTemplate)) },
            Children =
            {
                ChromeMotion(),
                State(":pointerover", looks.Hover),
                State(":pressed", looks.Pressed),
                State(":disabled", looks.Disabled),
                State(x => x.Nesting().PropertyEquals(Control.TagProperty, "flyout-open"), looks.Pressed),
                Group(x => x.Nesting().PropertyEquals(Control.TagProperty, "checked"),
                    rest: new("AccentFillColor", "SplitButtonBorderBrushChecked", "SplitButtonForegroundChecked"),
                    hover: new("AccentFillColorPointerOver", "SplitButtonBorderBrushCheckedPointerOver", "SplitButtonForegroundCheckedPointerOver"),
                    pressed: checkedPressed,
                    disabled: new("SplitButtonBackgroundCheckedDisabled", "SplitButtonBorderBrushCheckedDisabled", "SplitButtonForegroundCheckedDisabled")),
                State(x => x.Nesting().PropertyEquals(Control.TagProperty, "checked-flyout-open"), checkedPressed),
            },
        };
    }

    private static Avalonia.Styling.Style PartTag(Func<Selector?, Selector> state, string tag) =>
        new(x => state(x).Template().OfType<Button>()) { Setters = { new Setter(Control.TagProperty, tag) } };
}
