using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the ColorPicker (the field and its open flyout) and ColorView (each tab) sheets, 2x, labelled.</summary>
public class ColorPickerRenderTests
{
    internal static readonly Color Blue = Color.FromRgb(0x00, 0x7C, 0xEA);
    internal static readonly Color Translucent = Color.FromArgb(0x80, 0xE1, 0x33, 0x2A);

    /// <summary>A colour in the second row of the Fluent palette, so the palette shows its selection.</summary>
    internal static readonly Color PaletteColour = new FluentColorPalette().GetColor(2, 1);

    private enum ButtonState { Rest, Hover, KeyboardFocus, Disabled }

    private static readonly ButtonState[] ButtonStates = Enum.GetValues<ButtonState>();
    private static readonly string[] ButtonColumns = ["Rest", "Hover", "Keyboard focus", "Disabled"];

    [AvaloniaFact]
    public void ColorPicker_Renders()
    {
        using var sheet = new Sheet("ColorPicker: a field (32 / 24, radius 8, ComboBox colours) with a colour chip and a caret, opening the ColorView in a glass flyout (radius 16)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            sheet.AddSection($"{themeLabel}: the field on the glass panel", ButtonColumns,
            [
                ("Opaque", ButtonStates.Select(s => PickerCell(sheet, theme, Blue, s)).ToList()),
                ("Translucent (checkerboard)", ButtonStates.Select(s => PickerCell(sheet, theme, Translucent, s)).ToList()),
                ("White", ButtonStates.Select(s => PickerCell(sheet, theme, Colors.White, s)).ToList()),
                ("Compact", ButtonStates.Select(s => PickerCell(sheet, theme, Blue, s, DensityStyle.Compact)).ToList()),
            ]);
            sheet.AddSection($"{themeLabel}: flyout open", ["Normal", "Compact"],
            [
                ("Below the field", [
                    OpenCell(sheet, theme, DensityStyle.Normal, new Size(420, 470)),
                    OpenCell(sheet, theme, DensityStyle.Compact, new Size(420, 440)),
                ]),
            ]);
        }
        sheet.Save("m3-colorpicker.png");
    }

    [AvaloniaFact]
    public void ColorView_Renders()
    {
        string[] columns = ["Spectrum", "Palette", "Components"];
        using var sheet = new Sheet("ColorView: icon tabs over pages of one size; squircle spectrum, colour sliders and chips with the white ring thumb; the previewer below");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            foreach (var (densityLabel, density) in FieldCells.Densities)
                sheet.AddSection($"{themeLabel}, {densityLabel}", columns,
                [
                    ("On the glass panel", [
                        ViewCell(sheet, theme, density, 0),
                        ViewCell(sheet, theme, density, 1),
                        ViewCell(sheet, theme, density, 2),
                    ]),
                ]);
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            sheet.AddSection($"{themeLabel}: focus and selection", ["Keyboard focus on the spectrum", "Keyboard focus on a slider",
                    "Palette: selected (row 2), hover (row 1), keyboard focus (row 3)", "Palette: selected with keyboard focus"],
            [
                ("On the glass panel", [
                    ViewCell(sheet, theme, DensityStyle.Normal, 0, v => Part<ColorSpectrum>(v, "ColorSpectrum").Focus(NavigationMethod.Tab)),
                    ViewCell(sheet, theme, DensityStyle.Normal, 2, v => Part<ColorSlider>(v, "Component2Slider").Focus(NavigationMethod.Tab)),
                    ViewCell(sheet, theme, DensityStyle.Normal, 1, v =>
                    {
                        var items = v.GetVisualDescendants().OfType<ListBoxItem>().ToList();
                        ((IPseudoClasses)items[3].Classes).Set(":pointerover", true);
                        items[15].Focus(NavigationMethod.Tab);
                    }, PaletteColour),
                    ViewCell(sheet, theme, DensityStyle.Normal, 1, v =>
                        v.GetVisualDescendants().OfType<ListBoxItem>().Single(i => i.IsSelected).Focus(NavigationMethod.Tab), PaletteColour),
                ]),
            ]);
            sheet.AddSection($"{themeLabel}: disabled (thumbs at reduced opacity, colours in full)", ["Spectrum", "Palette", "Components"],
            [
                ("On the glass panel", [
                    ViewCell(sheet, theme, DensityStyle.Normal, 0, v => v.IsEnabled = false),
                    ViewCell(sheet, theme, DensityStyle.Normal, 1, v => v.IsEnabled = false, PaletteColour),
                    ViewCell(sheet, theme, DensityStyle.Normal, 2, v => v.IsEnabled = false),
                ]),
            ]);
        }
        sheet.Save("m3-colorview.png");
    }

    // --- Cells ---------------------------------------------------------------------------

    private static Control PickerCell(Sheet sheet, ThemeVariant theme, Color colour, ButtonState state, DensityStyle density = DensityStyle.Normal)
    {
        var picker = new ColorPicker { Color = colour, IsEnabled = state != ButtonState.Disabled };
        return sheet.Cell(ListBoxRenderTests.OnSurface(picker, ListBoxRenderTests.Surface.GlassPanel), theme,
            beforeShow: w =>
            {
                if (density != DensityStyle.Normal)
                    w.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = density });
            },
            afterShow: w =>
            {
                w.UpdateLayout();
                PickerRenderTests.StripTransitions(picker);
                var button = Button(picker);
                if (state == ButtonState.Hover)
                    ((IPseudoClasses)button.Classes).Set(":pointerover", true);
                if (state == ButtonState.KeyboardFocus)
                    button.Focus(NavigationMethod.Tab);
            });
    }

    private static Control OpenCell(Sheet sheet, ThemeVariant theme, DensityStyle density, Size size)
    {
        var picker = new ColorPicker { Color = Blue };
        var window = Host(theme, density, picker, size);
        try
        {
            Open(picker);
            Settle(window);
            var root = TopLevel.GetTopLevel(picker)!;
            Settle(root);
            using var capture = root.CaptureRenderedFrame()!;
            return sheet.Image(capture);
        }
        finally
        {
            window.Close();
        }
    }

    private static Control ViewCell(Sheet sheet, ThemeVariant theme, DensityStyle density, int tab, Action<ColorView>? state = null, Color? colour = null)
    {
        var view = new ColorView { Color = colour ?? Blue, SelectedIndex = tab };
        return sheet.Cell(ListBoxRenderTests.OnSurface(view, ListBoxRenderTests.Surface.GlassPanel), theme,
            beforeShow: w =>
            {
                if (density != DensityStyle.Normal)
                    w.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = density });
            },
            afterShow: w =>
            {
                Settle(w);
                PickerRenderTests.StripTransitions(view);
                state?.Invoke(view);
                PickerRenderTests.StripTransitions(view);
                Settle(w);
            });
    }

    // --- Helpers -------------------------------------------------------------------------

    /// <summary>A window with the picker at its top left on the glass panel, the theme at <paramref name="density"/> with reduce motion on.</summary>
    internal static Window Host(ThemeVariant theme, DensityStyle density, Control content, Size size, bool reduceMotion = true)
    {
        var window = new Window { Width = size.Width, Height = size.Height, RequestedThemeVariant = theme };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion, Density = density });
        var surface = ListBoxRenderTests.OnSurface(content, ListBoxRenderTests.Surface.GlassPanel);
        surface.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        surface.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        var region = new Border { Padding = new Thickness(16, 12), Child = surface };
        region.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemRegionColor"));
        window.Content = region;
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        Settle(window);
        return window;
    }

    internal static DropDownButton Button(ColorPicker picker) => Part<DropDownButton>(picker, "DropDownButton");

    internal static void Open(ColorPicker picker)
    {
        var button = Button(picker);
        button.Flyout!.ShowAt(button);
    }

    internal static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    /// <summary>
    /// Lays out and renders until the colour bitmaps are in: the sliders and the spectrum draw
    /// them on a background task and apply them on the UI thread.
    /// </summary>
    internal static void Settle(TopLevel top)
    {
        for (var i = 0; i < 200; i++)
        {
            Dispatcher.UIThread.RunJobs();
            top.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var roots = new List<Visual> { top };
            roots.AddRange(top.GetVisualDescendants().OfType<Popup>().Where(p => p.IsOpen && p.Child is not null).Select(p => (Visual)p.Child!));
            var visuals = roots.SelectMany(r => r.GetVisualDescendants()).Where(v => v.IsEffectivelyVisible).ToList();
            var ready = visuals.OfType<ColorSlider>().All(s => s.Bounds.Width == 0 || s.Background is ImageBrush)
                && visuals.OfType<Rectangle>().Where(r => r.Name == "PART_SpectrumRectangle").All(r => r.Bounds.Width == 0 || r.Fill is not null);
            if (ready && i > 1)
                return;
            Thread.Sleep(5);
        }
    }
}
