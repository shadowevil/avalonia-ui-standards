using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;

namespace Catalog.Pages;

/// <summary>ColorPicker and ColorView: a live picker bound to a swatch, the field in every state, and a standalone view.</summary>
internal sealed class ColorPickerPage : UserControl
{
    private static readonly Color Sample = Color.FromRgb(0x00, 0x7C, 0xEA);

    public ColorPickerPage() => Content = Ui.Page("ColorPicker",
        Ui.Section("Live", Live()),
        Ui.Section("States", StateGrid.Build(
        [
            ("Opaque", () => new ColorPicker { Color = Sample }),
            ("Translucent", () => new ColorPicker { Color = Color.FromArgb(0x80, 0xE1, 0x33, 0x2A) }),
        ], control => control.GetVisualDescendants().OfType<DropDownButton>().FirstOrDefault() ?? control)),
        Ui.Section("ColorView", Ui.SideScroll(new ColorView { Color = Sample, HorizontalAlignment = HorizontalAlignment.Left })),
        Ui.Caption("The field opens the ColorView below it in a glass flyout. Tabs switch between the spectrum, the palette and the components; " +
            "RGB and HSV are a segmented control. The spectrum, the sliders and the palette take the arrow keys and show the focus ring."));

    /// <summary>A picker and a swatch that shows its colour and hex value.</summary>
    private static Control Live()
    {
        var picker = new ColorPicker { Color = Sample };
        var swatch = new SquircleBorder { Width = 64, Height = 32 };
        var hex = Ui.Body("");
        void Show(Color colour)
        {
            swatch.Background = new SolidColorBrush(colour);
            hex.Text = Ui.Hex(colour);
        }
        Show(picker.Color);
        picker.ColorChanged += (_, e) => Show(e.NewColor);
        swatch.Bind(SquircleBorder.CornerRadiusProperty, swatch.GetResourceObservable("CornerRadiusControl").ToBinding());
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children = { picker, swatch, hex },
        };
    }
}
