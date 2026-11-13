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
using Avalonia.VisualTree;
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

    // --- Strings -----------------------------------------------------------------------------

    // The view's shown and spoken strings, as theme resources with English defaults. Parts look
    // them up dynamically, so an app overrides one by putting the key in its own resources.
    public const string SpectrumTextKey = "ColorViewSpectrumText";
    public const string PaletteTextKey = "ColorViewPaletteText";
    public const string ComponentsTextKey = "ColorViewComponentsText";
    public const string RgbTextKey = "ColorViewRgbText";
    public const string HsvTextKey = "ColorViewHsvText";
    public const string HexTextKey = "ColorViewHexText";
    public const string RedTextKey = "ColorViewRedText";
    public const string GreenTextKey = "ColorViewGreenText";
    public const string BlueTextKey = "ColorViewBlueText";
    public const string HueTextKey = "ColorViewHueText";
    public const string SaturationTextKey = "ColorViewSaturationText";
    public const string ValueTextKey = "ColorViewValueText";
    public const string AlphaTextKey = "ColorViewAlphaText";
    public const string RedLetterKey = "ColorViewRedLetter";
    public const string GreenLetterKey = "ColorViewGreenLetter";
    public const string BlueLetterKey = "ColorViewBlueLetter";
    public const string HueLetterKey = "ColorViewHueLetter";
    public const string SaturationLetterKey = "ColorViewSaturationLetter";
    public const string ValueLetterKey = "ColorViewValueLetter";
    public const string AlphaLetterKey = "ColorViewAlphaLetter";

    internal static readonly (string Key, string Text)[] Strings =
    [
        (SpectrumTextKey, "Spectrum"), (PaletteTextKey, "Palette"), (ComponentsTextKey, "Components"),
        (RgbTextKey, "RGB"), (HsvTextKey, "HSV"), (HexTextKey, "Hexadecimal colour"),
        (RedTextKey, "Red"), (GreenTextKey, "Green"), (BlueTextKey, "Blue"),
        (HueTextKey, "Hue"), (SaturationTextKey, "Saturation"), (ValueTextKey, "Value"), (AlphaTextKey, "Alpha"),
        (RedLetterKey, "R"), (GreenLetterKey, "G"), (BlueLetterKey, "B"),
        (HueLetterKey, "H"), (SaturationLetterKey, "S"), (ValueLetterKey, "V"), (AlphaLetterKey, "A"),
    ];

    public static void AddResources(IResourceDictionary resources)
    {
        foreach (var (key, text) in Strings)
            resources[key] = text;
    }

    /// <summary>What picks a part's string: the colour model or the spectrum's third component, bound per part.</summary>
    private static readonly AttachedProperty<object?> TextSelectorProperty =
        AvaloniaProperty.RegisterAttached<Control, object?>("TextSelector", typeof(ColorPickerThemes));

    /// <summary>
    /// Sets <paramref name="property"/> on <paramref name="target"/> to the string resource that
    /// <paramref name="key"/> picks for the current value of <paramref name="selector"/>, following
    /// both the selector and the resources.
    /// </summary>
    private static void BindText(Control target, AvaloniaProperty property, BindingBase selector, Func<object?, string> key, params string[] keys)
    {
        var values = new Dictionary<string, object?>();
        void Update() => target.SetValue(property, values.GetValueOrDefault(key(target.GetValue(TextSelectorProperty))));
        foreach (var candidate in keys)
            target.GetResourceObservable(candidate).Subscribe(new Avalonia.Reactive.AnonymousObserver<object?>(v =>
            {
                values[candidate] = v;
                Update();
            }));
        target.GetObservable(TextSelectorProperty).Subscribe(new Avalonia.Reactive.AnonymousObserver<object?>(_ => Update()));
        target.Bind(TextSelectorProperty, selector);
    }

    /// <summary>The string for a colour component in a model: the full name or the letter.</summary>
    private static string ComponentKey(ColorComponent component, ColorModel model, bool letter) => (component, model) switch
    {
        (ColorComponent.Alpha, _) => letter ? AlphaLetterKey : AlphaTextKey,
        (ColorComponent.Component1, ColorModel.Hsva) => letter ? HueLetterKey : HueTextKey,
        (ColorComponent.Component2, ColorModel.Hsva) => letter ? SaturationLetterKey : SaturationTextKey,
        (ColorComponent.Component3, ColorModel.Hsva) => letter ? ValueLetterKey : ValueTextKey,
        (ColorComponent.Component1, _) => letter ? RedLetterKey : RedTextKey,
        (ColorComponent.Component2, _) => letter ? GreenLetterKey : GreenTextKey,
        _ => letter ? BlueLetterKey : BlueTextKey,
    };

    private static readonly string[] ComponentTextKeys = [RedTextKey, GreenTextKey, BlueTextKey, HueTextKey, SaturationTextKey, ValueTextKey, AlphaTextKey];
    private static readonly string[] ComponentLetterKeys = [RedLetterKey, GreenLetterKey, BlueLetterKey, HueLetterKey, SaturationLetterKey, ValueLetterKey, AlphaLetterKey];

    // --- ColorView ---------------------------------------------------------------------------

    public static ControlTheme ColorView()
    {
        var theme = new ControlTheme(typeof(ColorView));
        AddViewSetters(theme);
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ColorView>(Body)));
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

    /// <summary>The view: the tabs, then the previewer, all bound to <paramref name="view"/>.</summary>
    private static Control Body(ColorView view, INameScope scope)
    {
        Binding Of(string path, BindingMode mode = BindingMode.OneWay, IValueConverter? converter = null, object? parameter = null) =>
            new(path) { Source = view, Mode = mode, Converter = converter, ConverterParameter = parameter };
        MultiBinding Both(string a, string b) => new() { Converter = BoolConverters.And, Bindings = { Of(a), Of(b) } };

        var tabs = new TabControl { Name = TabControlPart, Padding = new Thickness(0) };
        tabs.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(ColorPartsThemes.ViewWidthKey));
        tabs.Items.Add(Tab(SpectrumTextKey, Icons.Drop, SpectrumPage(scope, Of), Of(nameof(Avalonia.Controls.ColorView.IsColorSpectrumVisible))));
        tabs.Items.Add(Tab(PaletteTextKey, Icons.SquaresFour, PalettePage(Of), Of(nameof(Avalonia.Controls.ColorView.IsColorPaletteVisible))));
        tabs.Items.Add(Tab(ComponentsTextKey, Icons.SlidersHorizontal, ComponentsPage(scope, Of, Both), Of(nameof(Avalonia.Controls.ColorView.IsColorComponentsVisible))));
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

    /// <summary>An icon tab, named (by the string resource <paramref name="nameKey"/>) by its tooltip and for automation.</summary>
    private static TabItem Tab(string nameKey, Geometry icon, Control page, Binding visible)
    {
        var tab = new TabItem { Header = new Icon { Data = icon }, Content = page };
        tab.Bind(ToolTip.TipProperty, new DynamicResourceExtension(nameKey));
        tab.Bind(AutomationProperties.NameProperty, new DynamicResourceExtension(nameKey));
        tab.Bind(Visual.IsVisibleProperty, visible);
        // Every page has the spectrum's height, so the view keeps its size between tabs.
        page.Height = ColorPartsThemes.SpectrumSize;
        page.Margin = new Thickness(0, Spacing.M, 0, 0);
        return tab;
    }

    private static Control SpectrumPage(INameScope scope, Func<string, BindingMode, IValueConverter?, object?, Binding> of)
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
        spectrum.Bind(AutomationProperties.NameProperty, new DynamicResourceExtension(SpectrumTextKey));
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
        third.Bind(ColorSlider.ColorComponentProperty, new Binding(nameof(ColorSpectrum.ThirdComponent)) { Source = spectrum });
        // Named by the component it sets, which the spectrum's components decide (HSV).
        BindText(third, AutomationProperties.NameProperty, new Binding(nameof(ColorSpectrum.ThirdComponent)) { Source = spectrum },
            c => ComponentKey(c is ColorComponent component ? component : ColorComponent.Component3, ColorModel.Hsva, letter: false), ComponentTextKeys);
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
        alpha.Bind(AutomationProperties.NameProperty, new DynamicResourceExtension(AlphaTextKey));
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
        list.Bind(AutomationProperties.NameProperty, new DynamicResourceExtension(PaletteTextKey));
        list.Bind(ItemsControl.ItemsSourceProperty, of(nameof(Avalonia.Controls.ColorView.PaletteColors), BindingMode.OneWay, null, null));
        list.Bind(SelectingItemsControl.SelectedItemProperty, of(nameof(Avalonia.Controls.ColorView.Color), BindingMode.TwoWay, DoNothingForNull, null));
        // Before the list's own handling, which has no arrow navigation in a uniform grid.
        list.AddHandler(InputElement.KeyDownEvent, OnPaletteKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        return list;
    }

    /// <summary>
    /// The palette's keys, as on a grid: Left and Right move along a row (swapped right to left),
    /// Up and Down along a column, Home and End to the first and last colour. Movement stops at
    /// the grid's edges. The colour under the keyboard is selected, as on the segmented control.
    /// </summary>
    private static void OnPaletteKeyDown(object? sender, KeyEventArgs e)
    {
        var list = (ListBox)sender!;
        if (e.KeyModifiers != KeyModifiers.None || e.Key is not (Key.Home or Key.End or Key.Left or Key.Right or Key.Up or Key.Down))
            return;
        // Navigation keys are always handled, so they never scroll a page around the palette.
        e.Handled = true;
        var count = list.ItemCount;
        if (count == 0)
            return;
        var columns = Math.Max(1, (list.ItemsPanelRoot as UniformGrid)?.Columns ?? 1);
        var container = (e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(includeSelf: true);
        var focused = container is null ? -1 : list.IndexFromContainer(container);
        var from = focused >= 0 ? focused : list.SelectedIndex;
        var rtl = list.FlowDirection == FlowDirection.RightToLeft;
        int? target = from < 0
            ? 0
            : e.Key switch
            {
                Key.Home => 0,
                Key.End => count - 1,
                Key.Left => Step(rtl ? +1 : -1),
                Key.Right => Step(rtl ? -1 : +1),
                Key.Up => from - columns >= 0 ? from - columns : null,
                _ => from + columns < count ? from + columns : null,
            };
        if (target is not { } index || index == from)
            return;
        list.SelectedIndex = index;
        list.ScrollIntoView(index);
        (list.ContainerFromIndex(index) as InputElement)?.Focus(NavigationMethod.Directional);

        // Along the row, stopping at its ends.
        int? Step(int step)
        {
            var next = from + step;
            return next >= 0 && next < count && next / columns == from / columns ? next : null;
        }
    }

    private static Control ComponentsPage(INameScope scope,
        Func<string, BindingMode, IValueConverter?, object?, Binding> of, Func<string, string, MultiBinding> both)
    {
        Binding Of(string path, BindingMode mode = BindingMode.OneWay, IValueConverter? converter = null) => of(path, mode, converter, null);

        // The colour model is a setting with two values, so it is a segmented control.
        SegmentedControlItem Segment(string key)
        {
            var segment = new SegmentedControlItem();
            segment.Bind(ContentControl.ContentProperty, new DynamicResourceExtension(key));
            return segment;
        }
        var model = new SegmentedControl
        {
            Name = ColorModelPart,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Items = { Segment(RgbTextKey), Segment(HsvTextKey) },
        };
        model.Bind(SelectingItemsControl.SelectedIndexProperty, Of(nameof(Avalonia.Controls.ColorView.ColorModel), BindingMode.TwoWay, ModelIndex));
        model.Bind(Visual.IsVisibleProperty, Of(nameof(Avalonia.Controls.ColorView.IsColorModelVisible)));

        var hash = new TextBlock { Text = "#", Classes = { LabelClass }, VerticalAlignment = VerticalAlignment.Center };
        hash.Bind(Layoutable.MarginProperty, new DynamicResourceExtension(ColorPartsThemes.HashMarginKey));
        var hex = new TextBox { Name = HexTextBoxPart, MaxLength = 9, InnerLeftContent = hash, HorizontalAlignment = HorizontalAlignment.Stretch };
        hex.Bind(AutomationProperties.NameProperty, new DynamicResourceExtension(HexTextKey));
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

        // Fluent's element names; the shown and spoken names follow the colour model.
        var rows = new (string Name, ColorComponent Component)[]
        {
            ("Component1", ColorComponent.Component1),
            ("Component2", ColorComponent.Component2),
            ("Component3", ColorComponent.Component3),
            ("AlphaComponent", ColorComponent.Alpha),
        };
        for (var i = 0; i < rows.Length; i++)
        {
            var (name, component) = rows[i];
            var isAlpha = component == ColorComponent.Alpha;
            Func<object?, string> Key(bool letter) => m => ComponentKey(component, m is ColorModel model ? model : ColorModel.Rgba, letter);

            // A 16 px letter column, SpacingS before the field.
            var label = new TextBlock { Classes = { LabelClass }, Width = Spacing.L, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, Spacing.S, 0) };
            BindText(label, TextBlock.TextProperty, Of(nameof(Avalonia.Controls.ColorView.ColorModel)), Key(letter: true), ComponentLetterKeys);

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
            BindText(slider, AutomationProperties.NameProperty, Of(nameof(Avalonia.Controls.ColorView.ColorModel)), Key(letter: false), ComponentTextKeys);

            var number = new NumericUpDown
            {
                Name = name + "NumericUpDown",
                AllowSpin = true,
                ShowButtonSpinner = false,
                NumberFormat = ComponentFormat,
                Width = 64,
                Margin = new Thickness(0, 0, Spacing.M, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            BindText(number, AutomationProperties.NameProperty, Of(nameof(Avalonia.Controls.ColorView.ColorModel)), Key(letter: false), ComponentTextKeys);
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
