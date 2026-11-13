using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.ColorPickerRenderTests;
using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>ColorPicker, ColorView, ColorSpectrum, ColorSlider and ColorPreviewer (decision 0041).</summary>
public class ColorPickerThemeTests
{
    private static (Window Window, T Control) Show<T>(T control, ThemeVariant? variant = null, DensityStyle density = DensityStyle.Normal,
        bool reduceMotion = true) where T : Control
    {
        var window = Host(variant ?? ThemeVariant.Light, density, control, new Size(520, 640), reduceMotion);
        return (window, control);
    }

    // --- Inclusion and parts -----------------------------------------------------------------

    [AvaloniaFact]
    public void StandardsTheme_IncludesThePackagesFluentStyles_AndRestylesItsControls()
    {
        var (window, picker) = Show(new ColorPicker());
        try
        {
            // The package's Fluent resources come with StandardsTheme, no extra include.
            Assert.True(window.TryFindResource(ColorPartsThemes.CheckerKey, out var checker));
            Assert.IsAssignableFrom<IBrush>(checker);
            // The package's styles are a child of the theme, after Fluent; the library's themes win over them.
            var theme = window.Styles.OfType<StandardsTheme>().Single();
            var include = theme.OfType<Avalonia.Markup.Xaml.Styling.StyleInclude>().Single();
            Assert.Equal("avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml", include.Source!.ToString());
            Assert.True(theme.IndexOf(include) > theme.IndexOf(theme.OfType<FluentTheme>().Single()));
            foreach (var type in new[] { typeof(ColorPicker), typeof(ColorView), typeof(ColorSpectrum), typeof(ColorSlider), typeof(ColorPreviewer) })
            {
                Assert.True(window.TryFindResource(type, out var found));
                Assert.True(include.TryGetResource(type, null, out var fluent));
                Assert.NotSame(fluent, found);
                Assert.Same(theme.Resources[type], found);
            }
            Assert.Equal(ColorPickerThemes.DropDownButtonPart, Assert.IsType<DropDownButton>(picker.GetVisualChildren().Single()).Name);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ColorView_KeepsFluentsPartNames()
    {
        var (window, view) = Show(new ColorView { SelectedIndex = (int)ColorViewTab.Components });
        try
        {
            Part<TabControl>(view, ColorPickerThemes.TabControlPart);
            Part<TextBox>(view, ColorPickerThemes.HexTextBoxPart);
            view.SelectedIndex = (int)ColorViewTab.Spectrum;
            Settle(window);
            var spectrum = Part<ColorSpectrum>(view, "ColorSpectrum");
            foreach (var name in new[] { "PART_LayoutRoot", "PART_SizingPanel", "PART_SelectionEllipsePanel" })
                Part<Panel>(spectrum, name);
            Part<Canvas>(spectrum, "PART_InputTarget");
            Part<Rectangle>(spectrum, "PART_SpectrumRectangle");
            Part<Rectangle>(spectrum, "PART_SpectrumOverlayRectangle");
            Part<Ellipse>(spectrum, "PART_SpectrumEllipse");
            Part<Ellipse>(spectrum, "PART_SpectrumOverlayEllipse");
            var slider = Part<ColorSlider>(view, "ColorSpectrumAlphaSlider");
            Part<Track>(slider, "PART_Track");
            var previewer = view.GetVisualDescendants().OfType<ColorPreviewer>().Single();
            foreach (var name in new[] { "PART_AccentDecrement2Border", "PART_AccentDecrement1Border", "PART_AccentIncrement1Border", "PART_AccentIncrement2Border" })
                Part<Border>(previewer, name);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Behaviour ---------------------------------------------------------------------------

    [AvaloniaFact]
    public void ColorView_Parts_FollowAndSetTheColour()
    {
        var (window, view) = Show(new ColorView { Color = Blue });
        try
        {
            var spectrum = Part<ColorSpectrum>(view, "ColorSpectrum");
            Assert.Equal(Blue, spectrum.HsvColor.ToRgb());

            // The spectrum sets the view's colour.
            spectrum.HsvColor = HsvColor.FromHsv(120, 1, 1);
            Assert.Equal(Color.FromRgb(0, 255, 0), view.Color);

            // The hex box commits on Enter.
            view.SelectedIndex = (int)ColorViewTab.Components;
            Settle(window);
            var hex = Part<TextBox>(view, ColorPickerThemes.HexTextBoxPart);
            Assert.Equal("00FF00FF", hex.Text);
            hex.Text = "FF8800";
            hex.Focus();
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Equal(Color.FromRgb(0xFF, 0x88, 0x00), view.Color);

            // The model switch is the ColorModel.
            var model = Part<SegmentedControl>(view, ColorPickerThemes.ColorModelPart);
            Assert.Equal(0, model.SelectedIndex);
            model.SelectedIndex = 1;
            Assert.Equal(ColorModel.Hsva, view.ColorModel);
            var label = Part<ColorSlider>(view, "Component1Slider");
            Assert.Equal(ColorModel.Hsva, label.ColorModel);

            // A palette colour sets the view's colour.
            view.SelectedIndex = (int)ColorViewTab.Palette;
            Settle(window);
            var palette = view.GetVisualDescendants().OfType<ListBox>().Single(l => l is not SegmentedControl);
            var first = view.Palette!.GetColor(0, 0);
            palette.SelectedIndex = 0;
            Assert.Equal(first, view.Color);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ColorPicker_HexBoxInTheFlyout_IsWired()
    {
        // The flyout's parts sit outside the template's visual tree but in its name scope, so the
        // control's code finds the hex box and commits typed values.
        var (window, picker) = Show(new ColorPicker { Color = Blue });
        try
        {
            Open(picker);
            Settle(window);
            picker.SelectedIndex = (int)ColorViewTab.Components;
            Settle(window);
            var presenter = OpenPresenter(window);
            var hex = Part<TextBox>(presenter, ColorPickerThemes.HexTextBoxPart);
            Assert.Equal("007CEAFF", hex.Text);
            hex.Text = "336699";
            hex.Focus();
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Equal(Color.FromRgb(0x33, 0x66, 0x99), picker.Color);
            // The chip on the field follows.
            var chip = Button(picker).GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name is null);
            var colour = (Border)((Panel)chip.Child!).Children[1];
            Assert.Equal(picker.Color, ((ISolidColorBrush)colour.Background!).Color);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Previewer_AccentChip_StepsTheColour()
    {
        var (window, view) = Show(new ColorView { Color = Blue });
        try
        {
            var previewer = view.GetVisualDescendants().OfType<ColorPreviewer>().Single();
            var lighter = Part<Border>(previewer, "PART_AccentIncrement1Border");
            var before = view.HsvColor.V;
            var centre = lighter.TranslatePoint(new Point(lighter.Bounds.Width / 2, lighter.Bounds.Height / 2), window)!.Value;
            window.MouseDown(centre, MouseButton.Left);
            window.MouseUp(centre, MouseButton.Left);
            Assert.True(view.HsvColor.V > before, $"{view.HsvColor.V} after {before}");
        }
        finally
        {
            window.Close();
        }
    }

    // --- The flyout --------------------------------------------------------------------------

    [AvaloniaFact]
    public void Flyout_IsGlass_InTheOverlayLayer_BelowTheField()
    {
        var (window, picker) = Show(new ColorPicker { Color = Blue });
        try
        {
            Open(picker);
            Settle(window);
            var presenter = OpenPresenter(window);
            var popup = Assert.IsType<Popup>(presenter.Parent);
            Assert.True(popup.ShouldUseOverlayLayer);
            Assert.True(popup.IsUsingOverlayLayer);
            var glass = presenter.GetVisualDescendants().OfType<GlassBorder>().First();
            Assert.Equal(new CornerRadius(16), glass.CornerRadius);
            Assert.Equal(new Thickness(Spacing.L), presenter.Padding);

            // SpacingXS below the field, left edges aligned.
            var field = Button(picker);
            var fieldBottom = field.TranslatePoint(new Point(0, field.Bounds.Height), window)!.Value;
            var top = glass.TranslatePoint(default, window)!.Value;
            Assert.Equal(fieldBottom.Y + Spacing.XS, top.Y, 0.5);
            Assert.Equal(fieldBottom.X, top.X, 0.5);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Flyout_FadesIn_UnlessMotionIsReduced(bool reduceMotion)
    {
        var (window, picker) = Show(new ColorPicker { Color = Blue }, reduceMotion: reduceMotion);
        var fade = reduceMotion ? null : Overlays.SlowFadeIn(window);
        try
        {
            Overlays.AssertFadesIn(window, nameof(ColorPicker), () => Open(picker),
                () => OpenPresenter(window).GetVisualDescendants().OfType<GlassBorder>().First(), fade);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Density and themes ------------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData(DensityStyle.Normal, 32, 24, 20, 320)]
    [InlineData(DensityStyle.Compact, 24, 20, 16, 312)]
    public void Sizes_FollowDensity(DensityStyle density, double field, double chip, double thumb, double width)
    {
        var picker = new ColorPicker { Color = Blue };
        var view = new ColorView { Color = Blue };
        var (window, _) = Show(new StackPanel { Spacing = 8, Children = { picker, view } }, density: density);
        try
        {
            var button = Button(picker);
            Assert.Equal(field, button.Bounds.Height);
            var chipBorder = button.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name is null);
            Assert.Equal(chip, chipBorder.Bounds.Height);
            // Square caret zone: the chip ends a field height from the right edge.
            var chipRight = chipBorder.TranslatePoint(new Point(chipBorder.Bounds.Width, 0), button)!.Value.X;
            Assert.Equal(button.Bounds.Width - field, chipRight, 0.5);

            Assert.Equal(width, Part<TabControl>(view, ColorPickerThemes.TabControlPart).Bounds.Width);
            var spectrum = Part<ColorSpectrum>(view, "ColorSpectrum");
            Assert.Equal(new Size(thumb, thumb), Part<Panel>(spectrum, ColorPartsThemes.SelectionPanelPart).Bounds.Size);
            Assert.Equal(ColorPartsThemes.SpectrumSize, Part<Panel>(spectrum, "PART_SizingPanel").Bounds.Width);
            Assert.Equal(thumb, Part<ColorSlider>(view, "ColorSpectrumAlphaSlider").Bounds.Width);
            Assert.Equal(field, view.GetVisualDescendants().OfType<ColorPreviewer>().Single().Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Field_UsesTheComboBoxColours(string theme)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, picker) = Show(new ColorPicker { Color = Blue }, variant);
        try
        {
            var button = Button(picker);
            var chrome = Part<SquircleBorder>(button, FieldParts.ChromePart);
            void Check(string background, string border)
            {
                Settle(window);
                Assert.Equal(Effective(Resolve(window, background, variant)), Effective(chrome.GetBaseValue(SquircleBorder.BackgroundProperty).Value));
                Assert.Equal(Effective(Resolve(window, border, variant)), Effective(chrome.GetBaseValue(SquircleBorder.BorderBrushProperty).Value));
            }
            Check("ComboBoxBackground", "ComboBoxBorderBrush");
            ((IPseudoClasses)button.Classes).Set(":pointerover", true);
            Check("ComboBoxBackgroundPointerOver", "ComboBoxBorderBrushPointerOver");
            // No pressed look: pressing keeps the hover look.
            ((IPseudoClasses)button.Classes).Set(":pressed", true);
            Check("ComboBoxBackgroundPointerOver", "ComboBoxBorderBrushPointerOver");
            ((IPseudoClasses)button.Classes).Set(":pressed", false);
            ((IPseudoClasses)button.Classes).Set(":pointerover", false);
            picker.IsEnabled = false;
            Check("ComboBoxBackgroundDisabled", "ComboBoxBorderBrushDisabled");
        }
        finally
        {
            window.Close();
        }
    }

    // --- Focus ring --------------------------------------------------------------------------

    [AvaloniaFact]
    public void FocusRing_ShowsOnKeyboardFocus_OnEveryFocusablePart()
    {
        var picker = new ColorPicker { Color = Blue };
        var view = new ColorView { Color = Blue };
        var other = new Button { Content = "Elsewhere" };
        var (window, _) = Show(new StackPanel { Spacing = 8, Children = { other, picker, view } });
        try
        {
            void Check(InputElement target, Control ringOwner)
            {
                var ring = ringOwner.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == FocusRing.Part);
                target.Focus(NavigationMethod.Pointer);
                Settle(window);
                Assert.False(ring.IsEffectivelyVisible, $"{target}: ring on pointer focus");
                other.Focus(NavigationMethod.Pointer);
                target.Focus(NavigationMethod.Tab);
                Settle(window);
                Assert.True(ring.IsEffectivelyVisible, $"{target}: no ring on keyboard focus");
            }

            var button = Button(picker);
            Check(button, button);
            var spectrum = Part<ColorSpectrum>(view, "ColorSpectrum");
            Check(spectrum, spectrum);
            var slider = Part<ColorSlider>(view, "ColorSpectrumAlphaSlider");
            Check(slider, slider);
            var tab = view.GetVisualDescendants().OfType<TabItem>().First();
            Check(tab, tab);

            view.SelectedIndex = (int)ColorViewTab.Palette;
            Settle(window);
            var item = view.GetVisualDescendants().OfType<ListBoxItem>().First(i => i is not SegmentedControlItem);
            Check(item, item);

            view.SelectedIndex = (int)ColorViewTab.Components;
            Settle(window);
            var hex = Part<TextBox>(view, ColorPickerThemes.HexTextBoxPart);
            Check(hex, hex);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Palette and thumbs ------------------------------------------------------------------

    [AvaloniaFact]
    public void PaletteItem_Selected_ShowsTheAccentRing_HoverTheListFill()
    {
        var (window, view) = Show(new ColorView { Color = Blue, SelectedIndex = (int)ColorViewTab.Palette });
        try
        {
            var items = view.GetVisualDescendants().OfType<ListBoxItem>().ToList();
            items[0].IsSelected = true;
            ((IPseudoClasses)items[1].Classes).Set(":pointerover", true);
            Settle(window);
            PickerRenderTests.StripTransitions(view);
            var selected = Part<SquircleBorder>(items[0], ButtonParts.ChromePart);
            var hovered = Part<SquircleBorder>(items[1], ButtonParts.ChromePart);
            Assert.Equal(Effective(Resolve(window, "AccentFillColor", ThemeVariant.Light)), Effective(selected.BorderBrush));
            Assert.Equal(new Thickness(FocusRing.Width), selected.BorderThickness);
            Assert.Equal(Effective(Resolve(window, "SystemControlHighlightListMediumBrush", ThemeVariant.Light)), Effective(hovered.Background));
            // The chip sits SpacingXS in, its corner concentric with the cell's.
            var chip = items[0].GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name is null);
            Assert.Equal(Spacing.XS, chip.TranslatePoint(default, items[0])!.Value.X, 0.5);
            Assert.Equal(new CornerRadius(4), chip.CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Thumbs_AreWhiteRings_ThatGrowOnHoverAndPress()
    {
        var (window, view) = Show(new ColorView { Color = Blue });
        try
        {
            var slider = Part<ColorSlider>(view, "ColorSpectrumAlphaSlider");
            var thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();
            var ring = Part<Border>(thumb, ColorPartsThemes.RingPart);
            Assert.Equal(Colors.White, Effective(ring.BorderBrush));
            Assert.Equal(Brushes.Transparent, thumb.GetVisualDescendants().OfType<Panel>().First().Background);

            ((IPseudoClasses)slider.Classes).Set(":pointerover", true);
            Settle(window);
            Assert.Equal(1.1, ((Avalonia.Media.Transformation.TransformOperations)thumb.RenderTransform!).Value.M11, 3);
            ((IPseudoClasses)slider.Classes).Set(":pressed", true);
            Settle(window);
            Assert.Equal(1.2, ((Avalonia.Media.Transformation.TransformOperations)thumb.RenderTransform!).Value.M11, 3);

            var spectrum = Part<ColorSpectrum>(view, "ColorSpectrum");
            Assert.Equal(Colors.White, Effective(Part<Border>(spectrum, ColorPartsThemes.RingPart).BorderBrush));

            view.IsEnabled = false;
            Settle(window);
            var disabled = Effective(Resolve(window, "SystemControlBackgroundBaseLowBrush", ThemeVariant.Light));
            Assert.Equal(disabled, Effective(ring.BorderBrush));
            Assert.Equal(disabled, Effective(Part<Border>(spectrum, ColorPartsThemes.RingPart).BorderBrush));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThumbMotion_IsOff_UnderReduceMotion(bool reduceMotion)
    {
        var (window, view) = Show(new ColorView { Color = Blue }, reduceMotion: reduceMotion);
        try
        {
            var thumb = Part<ColorSlider>(view, "ColorSpectrumAlphaSlider").GetVisualDescendants().OfType<Thumb>().Single();
            var selection = Part<Panel>(Part<ColorSpectrum>(view, "ColorSpectrum"), ColorPartsThemes.SelectionPanelPart);
            foreach (var animated in new Animatable[] { thumb, selection })
                Assert.Equal(reduceMotion, animated.Transitions is null || animated.Transitions.Count == 0);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Helpers -------------------------------------------------------------------------

    private static FlyoutPresenter OpenPresenter(Window window) =>
        window.GetVisualDescendants().OfType<FlyoutPresenter>().Single();

    private static object? Resolve(Window window, string key, ThemeVariant variant) =>
        window.TryFindResource(key, variant, out var value) ? value : throw new InvalidOperationException(key);
}
