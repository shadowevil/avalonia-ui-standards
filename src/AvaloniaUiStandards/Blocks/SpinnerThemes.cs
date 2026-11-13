using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The ButtonSpinner and NumericUpDown themes. Both are TextInput fields: the squircle chrome,
/// the density height and padding, Fluent's TextControl colours, the accent border while focus is
/// inside, danger on error, and the ring on keyboard focus. The spin buttons are field buttons
/// (subtle squircles) with caret-up and caret-down icons, side by side at the trailing end (or
/// the leading end with <c>ButtonSpinnerLocation.Left</c>).
/// </summary>
internal static class SpinnerThemes
{
    public const string SpinnerPanelPart = "PART_SpinnerPanel";
    public const string IncreaseButtonPart = "PART_IncreaseButton";
    public const string DecreaseButtonPart = "PART_DecreaseButton";
    public const string ContentPart = "PART_ContentPresenter";
    public const string SpinnerPart = "PART_Spinner";
    public const string TextBoxPart = "PART_TextBox";

    /// <summary>A ButtonSpinner on its own: a field around its content and spin buttons.</summary>
    public static ControlTheme ButtonSpinner(ControlTheme fieldButton)
    {
        var theme = Template(fieldButton);
        FieldParts.AddSizing(theme, PaddingKey);
        theme.Setters.Add(new Setter(InputElement.FocusableProperty, true));
        FieldParts.AddColours(theme, FieldParts.TextControl, x => x.Nesting().Class(":focus-within"));
        return theme;
    }

    /// <summary>
    /// The ButtonSpinner inside a NumericUpDown: the same template, its colours and sizes bound
    /// from the NumericUpDown, which holds the states.
    /// </summary>
    public static ControlTheme Template(ControlTheme fieldButton)
    {
        var theme = new ControlTheme(typeof(ButtonSpinner));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ButtonSpinner>((spinner, scope) => Build(scope, fieldButton))));
        theme.Children.Add(ChromeMotion(FieldParts.ChromePart));
        FocusRing.AddWithin(theme);
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Template().OfType<StackPanel>().Name(SpinnerPanelPart))
        {
            Setters =
            {
                new Setter(DockPanel.DockProperty, Dock.Right),
                new Setter(Layoutable.MarginProperty, new DynamicResourceExtension(FieldParts.TrailingButtonsMarginKey)),
            },
        });
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":left").Template().OfType<StackPanel>().Name(SpinnerPanelPart))
        {
            Setters =
            {
                new Setter(DockPanel.DockProperty, Dock.Left),
                new Setter(Layoutable.MarginProperty, new DynamicResourceExtension(FieldParts.LeadingButtonsMarginKey)),
            },
        });
        return theme;
    }

    /// <summary>NumericUpDown: a TextInput field holding a ButtonSpinner (on <paramref name="spinner"/>) around an inner TextBox.</summary>
    public static ControlTheme NumericUpDown(ControlTheme spinner, ControlTheme innerTextBox)
    {
        var theme = new ControlTheme(typeof(Avalonia.Controls.NumericUpDown));
        FieldParts.AddSizing(theme, PaddingKey);
        theme.Setters.Add(new Setter(Avalonia.Controls.NumericUpDown.PlaceholderForegroundProperty, Resource("TextControlPlaceholderForeground")));
        theme.Setters.Add(new Setter(Avalonia.Controls.NumericUpDown.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(Control.FocusAdornerProperty, null));
        theme.Setters.Add(new Setter(Visual.ClipToBoundsProperty, false));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty,
            new FuncControlTemplate<Avalonia.Controls.NumericUpDown>((box, scope) => BuildNumeric(scope, spinner, innerTextBox))));
        FieldParts.AddColours(theme, FieldParts.TextControl, x => x.Nesting().Class(":focus-within"));
        // Raised while focus is inside, so neighbours do not paint over its ButtonSpinner's ring.
        theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(":focus-within"))
        {
            Setters = { new Setter(Visual.ZIndexProperty, 1) },
        });
        return theme;
    }

    private static Control Build(INameScope scope, ControlTheme fieldButton)
    {
        var chrome = FieldParts.CreateChrome();
        chrome.Bind(Layoutable.MinHeightProperty, new TemplateBinding(Layoutable.MinHeightProperty));

        var increase = SpinButton(IncreaseButtonPart, Icons.CaretUp, fieldButton);
        var decrease = SpinButton(DecreaseButtonPart, Icons.CaretDown, fieldButton);
        // Drawn over the content's side padding, which the buttons reach into.
        var panel = new StackPanel { Name = SpinnerPanelPart, Orientation = Orientation.Horizontal, ZIndex = 1, Children = { increase, decrease } };
        panel.Bind(Visual.IsVisibleProperty, new TemplateBinding(Avalonia.Controls.ButtonSpinner.ShowButtonSpinnerProperty));

        var content = new ContentPresenter { Name = ContentPart };
        content.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        content.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        content.Bind(ContentPresenter.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        content.Bind(ContentPresenter.HorizontalContentAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
        content.Bind(ContentPresenter.VerticalContentAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));

        var dock = new DockPanel { Children = { panel, content } };
        dock.Bind(Layoutable.MarginProperty, new TemplateBinding(TemplatedControl.BorderThicknessProperty));

        foreach (var part in new Control[] { chrome, panel, increase, decrease, content })
            part.RegisterInNameScope(scope);
        return new DataValidationErrors { Content = FocusRing.Wrap(new Panel { Children = { chrome, dock } }, scope), ClipToBounds = false };
    }

    // A spin button: it takes focus on click (so a NumericUpDown commits typed text first, as in
    // Fluent) but is not a tab stop, as the arrow keys spin from the field.
    private static RepeatButton SpinButton(string name, Avalonia.Media.Geometry icon, ControlTheme fieldButton) => new()
    {
        Name = name,
        Theme = fieldButton,
        IsTabStop = false,
        Content = FieldParts.ButtonIcon(icon),
    };

    private static Control BuildNumeric(INameScope scope, ControlTheme spinnerTheme, ControlTheme innerTextBox)
    {
        var text = new TextBox { Name = TextBoxPart, Theme = innerTextBox, AcceptsReturn = false, TextWrapping = Avalonia.Media.TextWrapping.NoWrap, MinWidth = 0 };
        foreach (var (target, source) in new (AvaloniaProperty, AvaloniaProperty)[]
                 {
                     (TemplatedControl.PaddingProperty, TemplatedControl.PaddingProperty),
                     (TemplatedControl.ForegroundProperty, TemplatedControl.ForegroundProperty),
                     (TemplatedControl.CornerRadiusProperty, TemplatedControl.CornerRadiusProperty),
                     (TextBox.PlaceholderTextProperty, Avalonia.Controls.NumericUpDown.PlaceholderTextProperty),
                     (TextBox.PlaceholderForegroundProperty, Avalonia.Controls.NumericUpDown.PlaceholderForegroundProperty),
                     (TextBox.IsReadOnlyProperty, Avalonia.Controls.NumericUpDown.IsReadOnlyProperty),
                     (TextBox.TextAlignmentProperty, Avalonia.Controls.NumericUpDown.TextAlignmentProperty),
                     (ContentControl.VerticalContentAlignmentProperty, ContentControl.VerticalContentAlignmentProperty),
                     (ContentControl.HorizontalContentAlignmentProperty, ContentControl.HorizontalContentAlignmentProperty),
                 })
            text.Bind(target, new TemplateBinding(source));
        text.Bind(TextBox.TextProperty, new TemplateBinding(Avalonia.Controls.NumericUpDown.TextProperty) { Mode = BindingMode.TwoWay });

        var spinner = new ButtonSpinner
        {
            Name = SpinnerPart,
            Theme = spinnerTheme,
            Content = text,
            IsTabStop = false,
            Padding = new Thickness(0),
            MinWidth = 0,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };
        foreach (var (target, source) in new (AvaloniaProperty, AvaloniaProperty)[]
                 {
                     (TemplatedControl.BackgroundProperty, TemplatedControl.BackgroundProperty),
                     (TemplatedControl.BorderBrushProperty, TemplatedControl.BorderBrushProperty),
                     (TemplatedControl.BorderThicknessProperty, TemplatedControl.BorderThicknessProperty),
                     (TemplatedControl.CornerRadiusProperty, TemplatedControl.CornerRadiusProperty),
                     (TemplatedControl.ForegroundProperty, TemplatedControl.ForegroundProperty),
                     (Layoutable.MinHeightProperty, Layoutable.MinHeightProperty),
                     (Avalonia.Controls.ButtonSpinner.AllowSpinProperty, Avalonia.Controls.NumericUpDown.AllowSpinProperty),
                     (Avalonia.Controls.ButtonSpinner.ShowButtonSpinnerProperty, Avalonia.Controls.NumericUpDown.ShowButtonSpinnerProperty),
                     (Avalonia.Controls.ButtonSpinner.ButtonSpinnerLocationProperty, Avalonia.Controls.NumericUpDown.ButtonSpinnerLocationProperty),
                 })
            spinner.Bind(target, new TemplateBinding(source));

        spinner.RegisterInNameScope(scope);
        text.RegisterInNameScope(scope);
        return new DataValidationErrors { Content = spinner, ClipToBounds = false };
    }
}
