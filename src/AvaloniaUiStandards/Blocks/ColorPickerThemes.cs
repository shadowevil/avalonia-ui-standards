using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Converters;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The ColorView and ColorPicker themes. A ColorView is a tab strip (spectrum, palette,
/// components, as icon tabs) over pages of one size, then the previewer. A ColorPicker is a
/// field that shows the colour as a chip with a caret, opening the same view in a glass flyout
/// below it. Both build the view from one template, keeping Fluent's part and element names.
/// </summary>
/// <remarks>
/// The view's parts are bound to the ColorView itself rather than by template bindings, because
/// in a ColorPicker they sit in a flyout outside the template's visual tree. The hex box is
/// still registered as <c>PART_HexTextBox</c> in the template's name scope, so the control's
/// code finds and wires it.
/// </remarks>
internal static class ColorPickerThemes
{
    public const string TabControlPart = "PART_TabControl";
    public const string HexTextBoxPart = "PART_HexTextBox";
    public const string DropDownButtonPart = "DropDownButton";
    public const string GlyphPart = "DropDownGlyph";
    public const string ColorModelPart = "ColorModelSegmentedControl";

    private static readonly NumberFormatInfo ComponentFormat = new() { NumberDecimalDigits = 0 };
    private static readonly ToBrushConverter ToBrush = new();
    private static readonly ColorToDisplayNameConverter DisplayName = new();
    private static readonly DoNothingForNullConverter DoNothingForNull = new();

    /// <summary>The colour model as the model switch's index: RGB first, then HSV.</summary>
    private static readonly IValueConverter ModelIndex = new FuncValueConverter<ColorModel, int>(
        m => m == ColorModel.Hsva ? 1 : 0, i => i == 1 ? ColorModel.Hsva : ColorModel.Rgba);

    private static IValueConverter Letter(string rgb, string hsv) =>
        new FuncValueConverter<ColorModel, string>(m => m == ColorModel.Hsva ? hsv : rgb);

    // --- ColorView ---------------------------------------------------------------------------

    public static ControlTheme ColorView()
    {
        var theme = new ControlTheme(typeof(ColorView));
        AddViewSetters(theme);
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ColorView>(BuildView)));
        return theme;
    }

    private static void AddViewSetters(ControlTheme theme)
    {
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        // Focus rings lie outside the parts at the view's edges and outside the picker's field.
        theme.Setters.Add(new Setter(Visual.ClipToBoundsProperty, false));
        // Fluent's: alpha last in hex (as in CSS) and the Windows palette.
        theme.Setters.Add(new Setter(Avalonia.Controls.ColorView.HexInputAlphaPositionProperty, AlphaComponentPosition.Trailing));
        theme.Setters.Add(new Setter(Avalonia.Controls.ColorView.PaletteProperty, new FluentColorPalette()));
    }

    private static Control BuildView(ColorView view, INameScope scope) => Body(view, scope);

    /// <summary>The view: the tabs, then the previewer, all bound to <paramref name="view"/>.</summary>
    private static Control Body(ColorView view, INameScope scope)
    {
        Binding Of(string path, BindingMode mode = BindingMode.OneWay, IValueConverter? converter = null, object? parameter = null) =>
            new(path) { Source = view, Mode = mode, Converter = converter, ConverterParameter = parameter };
        MultiBinding Both(string a, string b) => new() { Converter = BoolConverters.And, Bindings = { Of(a), Of(b) } };

        var tabs = new TabControl { Name = TabControlPart, Padding = new Thickness(0) };
        tabs.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(ColorPartsThemes.ViewWidthKey));
        tabs.Items.Add(Tab("Spectrum", Icons.Drop, SpectrumPage(view, scope, Of, Both), Of(nameof(Avalonia.Controls.ColorView.IsColorSpectrumVisible))));
        tabs.Items.Add(Tab("Palette", Icons.SquaresFour, PalettePage(Of), Of(nameof(Avalonia.Controls.ColorView.IsColorPaletteVisible))));
        tabs.Items.Add(Tab("Components", Icons.SlidersHorizontal, ComponentsPage(scope, Of, Both), Of(nameof(Avalonia.Controls.ColorView.IsColorComponentsVisible))));
        // Bound once the tabs are in, so the view's index is not lost to an empty control.
        tabs.Bind(SelectingItemsControl.SelectedIndexProperty, Of(nameof(Avalonia.Controls.ColorView.SelectedIndex), BindingMode.TwoWay));

        var previewer = new ColorPreviewer { Margin = new Thickness(0, Spacing.M, 0, 0) };
        previewer.Bind(ColorPreviewer.HsvColorProperty, Of(nameof(Avalonia.Controls.ColorView.HsvColor), BindingMode.TwoWay));
        previewer.Bind(ColorPreviewer.IsAccentColorsVisibleProperty, Of(nameof(Avalonia.Controls.ColorView.IsAccentColorsVisible)));
        previewer.Bind(Visual.IsVisibleProperty, Of(nameof(Avalonia.Controls.ColorView.IsColorPreviewVisible)));

        tabs.RegisterInNameScope(scope);
        // The labels' colours are styles on the view's root, which a ColorPicker's flyout also holds.
        return new StackPanel { Children = { tabs, previewer }, Styles = { LabelStyle(null, "TextControlPlaceholderForeground"), LabelStyle(":disabled", "TextControlForegroundDisabled") } };
    }

    /// <summary>The component letters and the hex box's <c>#</c>: secondary text, the disabled text colour when disabled.</summary>
    private const string LabelClass = "colorLabel";

    private static Avalonia.Styling.Style LabelStyle(string? state, string key) => new(x =>
    {
        var selector = x.OfType<TextBlock>().Class(LabelClass);
        return state is null ? selector : selector.Class(state);
    })
    {
        Setters = { new Setter(TextBlock.ForegroundProperty, Resource(key)) },
    };

    /// <summary>An icon tab, named by its tooltip and for automation.</summary>
    private static TabItem Tab(string name, Geometry icon, Control page, Binding visible)
    {
        var tab = new TabItem { Header = new Icon { Data = icon }, Content = page };
        ToolTip.SetTip(tab, name);
        AutomationProperties.SetName(tab, name);
        tab.Bind(Visual.IsVisibleProperty, visible);
        // Every page has the spectrum's height, so the view keeps its size between tabs.
        page.Height = ColorPartsThemes.SpectrumSize;
        page.Margin = new Thickness(0, Spacing.M, 0, 0);
        return tab;
    }

    private static Control SpectrumPage(ColorView view, INameScope scope,
        Func<string, BindingMode, IValueConverter?, object?, Binding> of, Func<string, string, MultiBinding> both)
    {
        Binding Of(string path, BindingMode mode = BindingMode.OneWay) => of(path, mode, null, null);

        var spectrum = new ColorSpectrum { Name = "ColorSpectrum", HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        spectrum.Bind(ColorSpectrum.ComponentsProperty, Of(nameof(Avalonia.Controls.ColorView.ColorSpectrumComponents)));
        spectrum.Bind(ColorSpectrum.HsvColorProperty, Of(nameof(Avalonia.Controls.ColorView.HsvColor), BindingMode.TwoWay));
        spectrum.Bind(ColorSpectrum.ShapeProperty, Of(nameof(Avalonia.Controls.ColorView.ColorSpectrumShape)));
        foreach (var (target, source) in new (AvaloniaProperty, string)[]
                 {
                     (ColorSpectrum.MinHueProperty, nameof(Avalonia.Controls.ColorView.MinHue)),
                     (ColorSpectrum.MaxHueProperty, nameof(Avalonia.Controls.ColorView.MaxHue)),
                     (ColorSpectrum.MinSaturationProperty, nameof(Avalonia.Controls.ColorView.MinSaturation)),
                     (ColorSpectrum.MaxSaturationProperty, nameof(Avalonia.Controls.ColorView.MaxSaturation)),
                     (ColorSpectrum.MinValueProperty, nameof(Avalonia.Controls.ColorView.MinValue)),
                     (ColorSpectrum.MaxValueProperty, nameof(Avalonia.Controls.ColorView.MaxValue)),
                 })
            spectrum.Bind(target, Of(source));
        Grid.SetColumn(spectrum, 1);

        var third = new ColorSlider
        {
            Name = "ColorSpectrumThirdComponentSlider",
            IsAlphaVisible = false,
            IsPerceptive = true,
            Orientation = Orientation.Vertical,
            ColorModel = ColorModel.Hsva,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 0, Spacing.M, 0),
        };
        AutomationProperties.SetName(third, "Third Component");
        third.Bind(ColorSlider.ColorComponentProperty, new Binding(nameof(ColorSpectrum.ThirdComponent)) { Source = spectrum });
        third.Bind(ColorSlider.HsvColorProperty, new Binding(nameof(ColorSpectrum.HsvColor)) { Source = spectrum, Mode = BindingMode.TwoWay });
        third.Bind(Visual.IsVisibleProperty, Of(nameof(Avalonia.Controls.ColorView.IsColorSpectrumSliderVisible)));

        var alpha = new ColorSlider
        {
            Name = "ColorSpectrumAlphaSlider",
            Orientation = Orientation.Vertical,
            ColorModel = ColorModel.Hsva,
            ColorComponent = ColorComponent.Alpha,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(Spacing.M, 0, 0, 0),
        };
        AutomationProperties.SetName(alpha, "Alpha Component");
        alpha.Bind(ColorSlider.HsvColorProperty, new Binding(nameof(ColorSpectrum.HsvColor)) { Source = spectrum, Mode = BindingMode.TwoWay });
        alpha.Bind(InputElement.IsEnabledProperty, Of(nameof(Avalonia.Controls.ColorView.IsAlphaEnabled)));
        alpha.Bind(Visual.IsVisibleProperty, Of(nameof(Avalonia.Controls.ColorView.IsAlphaVisible)));
        Grid.SetColumn(alpha, 2);

        foreach (var part in new Control[] { spectrum, third, alpha })
            part.RegisterInNameScope(scope);
        return new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Children = { third, spectrum, alpha } };
    }

    private static Control PalettePage(Func<string, BindingMode, IValueConverter?, object?, Binding> of)
    {
        var list = new ListBox
        {
            ItemContainerTheme = PaletteItem,
            ItemTemplate = PaletteChip,
            ItemsPanel = new FuncTemplate<Panel?>(() =>
            {
                var grid = new UniformGrid();
                grid.Bind(UniformGrid.ColumnsProperty, of(nameof(Avalonia.Controls.ColorView.PaletteColumnCount), BindingMode.OneWay, null, null));
                return grid;
            }),
            Padding = new Thickness(Spacing.XS),
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        list.Bind(ItemsControl.ItemsSourceProperty, of(nameof(Avalonia.Controls.ColorView.PaletteColors), BindingMode.OneWay, null, null));
        list.Bind(SelectingItemsControl.SelectedItemProperty, of(nameof(Avalonia.Controls.ColorView.Color), BindingMode.TwoWay, DoNothingForNull, null));
        return list;
    }

    private static Control ComponentsPage(INameScope scope,
        Func<string, BindingMode, IValueConverter?, object?, Binding> of, Func<string, string, MultiBinding> both)
    {
        Binding Of(string path, BindingMode mode = BindingMode.OneWay, IValueConverter? converter = null) => of(path, mode, converter, null);

        // The colour model is a setting with two values, so it is a segmented control.
        var model = new SegmentedControl
        {
            Name = ColorModelPart,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Items = { new SegmentedControlItem { Content = "RGB" }, new SegmentedControlItem { Content = "HSV" } },
        };
        model.Bind(SelectingItemsControl.SelectedIndexProperty, Of(nameof(Avalonia.Controls.ColorView.ColorModel), BindingMode.TwoWay, ModelIndex));
        model.Bind(Visual.IsVisibleProperty, Of(nameof(Avalonia.Controls.ColorView.IsColorModelVisible)));

        var hash = new TextBlock { Text = "#", Classes = { LabelClass }, VerticalAlignment = VerticalAlignment.Center };
        hash.Bind(Layoutable.MarginProperty, new DynamicResourceExtension(ColorPartsThemes.HashMarginKey));
        var hex = new TextBox { Name = HexTextBoxPart, MaxLength = 9, InnerLeftContent = hash, HorizontalAlignment = HorizontalAlignment.Stretch };
        AutomationProperties.SetName(hex, "Hexadecimal Color");
        hex.Bind(Visual.IsVisibleProperty, Of(nameof(Avalonia.Controls.ColorView.IsHexInputVisible)));
        Grid.SetColumn(hex, 2);
        var top = new Grid { ColumnDefinitions = new ColumnDefinitions($"*,{Spacing.M},*"), Children = { model, hex } };
        Grid.SetColumnSpan(top, 3);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions($"Auto,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto"),
            RowSpacing = Spacing.M,
            VerticalAlignment = VerticalAlignment.Top,
            Children = { top },
        };

        var rows = new (string Name, ColorComponent Component, string Rgb, string Hsv)[]
        {
            ("Component1", ColorComponent.Component1, "R", "H"),
            ("Component2", ColorComponent.Component2, "G", "S"),
            ("Component3", ColorComponent.Component3, "B", "V"),
            ("AlphaComponent", ColorComponent.Alpha, "A", "A"),
        };
        for (var i = 0; i < rows.Length; i++)
        {
            var (name, component, rgb, hsv) = rows[i];
            var isAlpha = component == ColorComponent.Alpha;

            var label = new TextBlock { Classes = { LabelClass }, Width = Spacing.L, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, Spacing.S, 0) };
            label.Bind(TextBlock.TextProperty, Of(nameof(Avalonia.Controls.ColorView.ColorModel), converter: Letter(rgb, hsv)));

            var slider = new ColorSlider
            {
                Name = name + "Slider",
                Orientation = Orientation.Horizontal,
                IsRoundingEnabled = true,
                IsSnapToTickEnabled = true,
                TickFrequency = 1,
                ColorComponent = component,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };
            slider.Bind(ColorSlider.ColorModelProperty, Of(nameof(Avalonia.Controls.ColorView.ColorModel)));
            slider.Bind(ColorSlider.HsvColorProperty, Of(nameof(Avalonia.Controls.ColorView.HsvColor), BindingMode.TwoWay));

            var number = new NumericUpDown
            {
                Name = name + "NumericUpDown",
                AllowSpin = true,
                ShowButtonSpinner = false,
                NumberFormat = ComponentFormat,
                Width = Spacing.XXXL + Spacing.L,
                Margin = new Thickness(0, 0, Spacing.M, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            AutomationProperties.SetName(number, isAlpha ? "Alpha" : name);
            number.Bind(NumericUpDown.MinimumProperty, new Binding(nameof(ColorSlider.Minimum)) { Source = slider });
            number.Bind(NumericUpDown.MaximumProperty, new Binding(nameof(ColorSlider.Maximum)) { Source = slider });
            number.Bind(NumericUpDown.ValueProperty, new Binding(nameof(ColorSlider.Value)) { Source = slider, Mode = BindingMode.TwoWay });

            if (isAlpha)
            {
                label.Bind(Visual.IsVisibleProperty, both(nameof(Avalonia.Controls.ColorView.IsAlphaVisible), nameof(Avalonia.Controls.ColorView.IsComponentTextInputVisible)));
                number.Bind(Visual.IsVisibleProperty, both(nameof(Avalonia.Controls.ColorView.IsAlphaVisible), nameof(Avalonia.Controls.ColorView.IsComponentTextInputVisible)));
                slider.Bind(Visual.IsVisibleProperty, both(nameof(Avalonia.Controls.ColorView.IsAlphaVisible), nameof(Avalonia.Controls.ColorView.IsComponentSliderVisible)));
                foreach (var part in new Control[] { label, number, slider })
                    part.Bind(InputElement.IsEnabledProperty, Of(nameof(Avalonia.Controls.ColorView.IsAlphaEnabled)));
            }
            else
            {
                label.Bind(Visual.IsVisibleProperty, Of(nameof(Avalonia.Controls.ColorView.IsComponentTextInputVisible)));
                number.Bind(Visual.IsVisibleProperty, Of(nameof(Avalonia.Controls.ColorView.IsComponentTextInputVisible)));
                slider.Bind(Visual.IsVisibleProperty, Of(nameof(Avalonia.Controls.ColorView.IsComponentSliderVisible)));
            }

            Grid.SetRow(label, i + 1);
            Grid.SetRow(number, i + 1);
            Grid.SetRow(slider, i + 1);
            Grid.SetColumn(number, 1);
            Grid.SetColumn(slider, 2);
            grid.Children.Add(label);
            grid.Children.Add(number);
            grid.Children.Add(slider);
            number.RegisterInNameScope(scope);
            slider.RegisterInNameScope(scope);
        }

        model.RegisterInNameScope(scope);
        hex.RegisterInNameScope(scope);
        return grid;
    }

    // --- Palette -----------------------------------------------------------------------------

    /// <summary>A palette colour: a chip filling its cell, named by the colour's display name.</summary>
    private static readonly FuncDataTemplate<Color> PaletteChip = new((_, _) =>
    {
        var colour = new Border();
        colour.Bind(Border.BackgroundProperty, new Binding { Converter = ToBrush });
        var chip = ColorPartsThemes.Chip(colour);
        chip.Bind(SquircleBorder.CornerRadiusProperty, new Binding(nameof(TemplatedControl.CornerRadius))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(ListBoxItem) },
            Converter = Inset,
        });
        chip.Bind(ToolTip.TipProperty, new Binding { Converter = DisplayName });
        chip.Bind(AutomationProperties.NameProperty, new Binding { Converter = DisplayName });
        return chip;
    }, supportsRecycling: true);

    /// <summary>The chip's corner inside an item: the item's less the item padding (<c>SpacingXS</c>), so the curves are concentric.</summary>
    private static readonly IValueConverter Inset = new FuncValueConverter<CornerRadius, CornerRadius>(r =>
        new CornerRadius(Math.Max(0, r.TopLeft - Spacing.XS), Math.Max(0, r.TopRight - Spacing.XS),
            Math.Max(0, r.BottomRight - Spacing.XS), Math.Max(0, r.BottomLeft - Spacing.XS)));

    /// <summary>
    /// A palette item: a squircle cell holding its chip <c>SpacingXS</c> in, with the list item's
    /// hover and press fills around the chip and a 2 px accent ring on the cell when selected
    /// (the gap between it and the chip is the surface). Keyboard focus shows the focus ring.
    /// </summary>
    private static readonly ControlTheme PaletteItem = CreatePaletteItem();

    private static ControlTheme CreatePaletteItem()
    {
        var theme = new ControlTheme(typeof(ListBoxItem));
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        // The chip sits SpacingXS in from the cell edge: the ring's width, then a gap of the rest.
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new Thickness(Spacing.XS - FocusRing.Width)));
        theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(FieldParts.ButtonSizeKey)));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(FocusRing.Width)));
        theme.Setters.Add(new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        theme.Setters.Add(new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Stretch));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ContentControl>((item, scope) =>
        {
            var presenter = CreatePresenter();
            var chrome = CreateChrome(presenter);
            chrome.RegisterInNameScope(scope);
            presenter.RegisterInNameScope(scope);
            return FocusRing.Wrap(chrome, scope);
        })));
        FocusRing.AddTo(theme);
        theme.Children.Add(ChromeMotion());
        theme.Children.Add(PaletteState(":pointerover", "SystemControlHighlightListMediumBrush", null));
        theme.Children.Add(PaletteState(":pressed", "SystemControlHighlightBaseMediumLowBrush", null));
        theme.Children.Add(PaletteState(":selected", null, "AccentFillColor"));
        theme.Children.Add(PaletteState(":selected:disabled", null, "AccentButtonBackgroundDisabled"));
        return theme;
    }

    private static Avalonia.Styling.Style PaletteState(string state, string? fill, string? ring)
    {
        var style = new Avalonia.Styling.Style(x =>
        {
            var selector = x.Nesting();
            foreach (var pseudo in state.Split(':', StringSplitOptions.RemoveEmptyEntries))
                selector = selector.Class(":" + pseudo);
            return Chrome(selector);
        });
        if (fill is not null)
            style.Setters.Add(new Setter(SquircleBorder.BackgroundProperty, Resource(fill)));
        if (ring is not null)
            style.Setters.Add(new Setter(SquircleBorder.BorderBrushProperty, Resource(ring)));
        return style;
    }

    // --- ColorPicker -------------------------------------------------------------------------

    /// <summary>
    /// ColorPicker: a field (the ComboBox's closed look) holding the colour chip and a caret
    /// zone, opening the ColorView in a glass flyout below it.
    /// </summary>
    public static ControlTheme ColorPicker()
    {
        var theme = new ControlTheme(typeof(ColorPicker));
        AddViewSetters(theme);
        theme.Setters.Add(new Setter(Layoutable.MinWidthProperty, 64d));
        theme.Setters.Add(new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Left));
        theme.Setters.Add(new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center));
        theme.Setters.Add(new Setter(Avalonia.Controls.ColorPicker.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        theme.Setters.Add(new Setter(Avalonia.Controls.ColorPicker.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        // The default content is the chip; an app's Content replaces it, as in Fluent.
        theme.Setters.Add(new Setter(Avalonia.Controls.ColorPicker.ContentProperty, new FuncTemplate<Control>(PickerChip)));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ColorPicker>(BuildPicker)));
        return theme;
    }

    private static Control PickerChip()
    {
        var colour = new Border();
        colour.Bind(Border.BackgroundProperty, new Binding(nameof(Avalonia.Controls.ColorView.HsvColor))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(ColorPicker) },
            Converter = ToBrush,
        });
        var chip = ColorPartsThemes.Chip(colour);
        chip.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(FieldParts.ButtonSizeKey));
        chip.Bind(SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension(FieldParts.ButtonCornerRadiusKey));
        return chip;
    }

    private static Control BuildPicker(ColorPicker picker, INameScope scope)
    {
        var flyout = new Flyout
        {
            Content = Body(picker, scope),
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            VerticalOffset = Spacing.XS,
        };
        var button = new DropDownButton
        {
            Name = DropDownButtonPart,
            Theme = PickerButton,
            Flyout = flyout,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        button.Bind(ContentControl.ContentProperty, new TemplateBinding(Avalonia.Controls.ColorPicker.ContentProperty));
        button.Bind(ContentControl.ContentTemplateProperty, new TemplateBinding(Avalonia.Controls.ColorPicker.ContentTemplateProperty));
        button.Bind(ContentControl.HorizontalContentAlignmentProperty, new TemplateBinding(Avalonia.Controls.ColorPicker.HorizontalContentAlignmentProperty));
        button.Bind(ContentControl.VerticalContentAlignmentProperty, new TemplateBinding(Avalonia.Controls.ColorPicker.VerticalContentAlignmentProperty));
        button.RegisterInNameScope(scope);
        return button;
    }

    /// <summary>
    /// The ColorPicker's button: the closed ComboBox field (squircle chrome, density height, a
    /// 1 px border, Fluent's ComboBox colours with no pressed look) with the content after the
    /// field-button inset and a square caret zone showing <c>Icons.CaretDown</c>. The focus ring
    /// shows on keyboard focus.
    /// </summary>
    private static readonly ControlTheme PickerButton = CreatePickerButton();

    private static ControlTheme CreatePickerButton()
    {
        var theme = new ControlTheme(typeof(DropDownButton));
        theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusControl")));
        theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, new DynamicResourceExtension(MinHeightKey)));
        theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new DynamicResourceExtension(ColorPartsThemes.ChipMarginKey)));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, Resource("TextControlBorderThemeThickness")));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<DropDownButton>(BuildPickerButton)));
        FieldParts.AddColours(theme, new FieldParts.Colours(
            new("ComboBoxBackground", "ComboBoxBorderBrush", "ComboBoxForeground"),
            new("ComboBoxBackgroundPointerOver", "ComboBoxBorderBrushPointerOver", "ComboBoxForegroundPointerOver"),
            new("ComboBoxBackgroundPointerOver", "ComboBoxBorderBrushPointerOver", "ComboBoxForegroundPointerOver"),
            new("ComboBoxBackgroundDisabled", "ComboBoxBorderBrushDisabled", "ComboBoxForegroundDisabled")), focus: null);
        FocusRing.AddTo(theme);
        theme.Children.Add(ChromeMotion(FieldParts.ChromePart));
        theme.Children.Add(FieldParts.Part<Icon>(GlyphPart, null, TemplatedControl.ForegroundProperty, Resource("ComboBoxDropDownGlyphForeground")));
        theme.Children.Add(FieldParts.Part<Icon>(GlyphPart, x => x.Class(":disabled"), TemplatedControl.ForegroundProperty, Resource("ComboBoxDropDownGlyphForegroundDisabled")));
        return theme;
    }

    private static Control BuildPickerButton(DropDownButton button, INameScope scope)
    {
        var chrome = FieldParts.CreateChrome();
        var presenter = CreatePresenter();
        var zone = new Border { Background = Brushes.Transparent };
        zone.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(MinHeightKey));
        Grid.SetColumn(zone, 1);
        var glyph = new Icon
        {
            Name = GlyphPart,
            Data = Icons.CaretDown,
            Width = FieldParts.IconSize,
            Height = FieldParts.IconSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };
        Grid.SetColumn(glyph, 1);
        // The caret zone is square to the field's outer edge, as on the ComboBox.
        var content = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { presenter, zone, glyph } };
        foreach (var part in new Control[] { chrome, presenter, glyph })
            part.RegisterInNameScope(scope);
        return FocusRing.Wrap(new Panel { Children = { chrome, content } }, scope);
    }
}
