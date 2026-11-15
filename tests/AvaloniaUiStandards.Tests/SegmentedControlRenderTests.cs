using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Catalog.Pages;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the SegmentedControl sheets (2x, labelled).</summary>
public class SegmentedControlRenderTests
{
    internal enum SegmentState { Rest, Hover, Pressed, SegmentDisabled, Disabled, Focused }

    private static readonly string[] StateLabels = ["Rest", "Hover (Week)", "Pressed (Week)", "Segment disabled (Year)", "Disabled", "Focused"];

    [AvaloniaFact]
    public void States_Render()
    {
        using var sheet = new Sheet("SegmentedControl: neutral pill on the Button track, radius 8, 2 px inset, equal-width segments");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, StateLabels,
            [
                ("Text", Enum.GetValues<SegmentState>().Select(s => Cell(sheet, theme, s, Text)).ToList()),
                ("Icons", Enum.GetValues<SegmentState>().Select(s => Cell(sheet, theme, s, Glyphs)).ToList()),
                ("Compact, text", Enum.GetValues<SegmentState>().Select(s => Cell(sheet, theme, s, Text, compact: true)).ToList()),
                ("Compact, icons", Enum.GetValues<SegmentState>().Select(s => Cell(sheet, theme, s, Glyphs, compact: true)).ToList()),
            ]);
        sheet.Save("m3-segmented-control.png");
    }

    /// <summary>The icon-only selection modes (New, Add, Subtract, Intersect) and selection styles (Normal, Fixed ratio, Fixed size), cropped to the controls, in light and dark.</summary>
    [AvaloniaFact]
    public void SelectionMode_RendersInLightAndDark()
    {
        foreach (var (label, variant) in ButtonRenderTests.Themes)
        {
            var control = new StackPanel { Spacing = 12, Children = { SegmentedControlPage.SelectionMode(), SegmentedControlPage.SelectionStyle() } };
            var surface = new Border { Padding = new Thickness(16, 12), Child = control };
            surface.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemRegionColor"));
            var window = new Window { SizeToContent = SizeToContent.WidthAndHeight, RequestedThemeVariant = variant, Content = surface };
            window.SetRenderScaling(Sheet.Scale);
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            // The control with 12 px (logical) of its surface around it.
            using var frame = Frame.Capture(window);
            var origin = control.TranslatePoint(default, window)!.Value * Sheet.Scale;
            var margin = (int)Math.Round(12 * Sheet.Scale);
            var area = new PixelRect(
                (int)origin.X - margin,
                (int)origin.Y - margin,
                (int)Math.Round(control.Bounds.Width * Sheet.Scale) + 2 * margin,
                (int)Math.Round(control.Bounds.Height * Sheet.Scale) + 2 * margin);
            using var crop = Frame.Crop(frame.Bitmap, area);
            var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(dir);
            using (var stream = File.Create(Path.Combine(dir, $"selection-mode-icons-{label.ToLowerInvariant()}.png")))
                crop.Save(stream, new PngBitmapEncoderOptions());
            window.Close();
        }
    }

    // --- Content -------------------------------------------------------------------------

    internal static SegmentedControl Text()
    {
        var control = new SegmentedControl { ItemsSource = new[] { "Day", "Week", "Month", "Year" }, SelectedIndex = 0 };
        return control;
    }

    internal static SegmentedControl Glyphs()
    {
        var control = new SegmentedControl { SelectedIndex = 0 };
        foreach (var (name, data) in new[] { ("List", Icons.ListBullets), ("Grid", Icons.SquaresFour), ("Columns", Icons.SidebarSimple), ("Gallery", Icons.Eye) })
        {
            var item = new SegmentedControlItem { Content = new Icon { Data = data } };
            ToolTip.SetTip(item, name);
            Avalonia.Automation.AutomationProperties.SetName(item, name);
            control.Items.Add(item);
        }
        return control;
    }

    // --- Cells ---------------------------------------------------------------------------

    private static Control Cell(Sheet sheet, ThemeVariant theme, SegmentState state, Func<SegmentedControl> create, bool compact = false)
    {
        var control = create();
        return sheet.Cell(control, theme,
            beforeShow: w =>
            {
                if (compact)
                    w.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { Density = Avalonia.Themes.Fluent.DensityStyle.Compact });
            },
            afterShow: w =>
            {
                w.UpdateLayout();
                Strip(control);
                if (state == SegmentState.Disabled)
                    control.IsEnabled = false;
                if (state == SegmentState.SegmentDisabled)
                    control.ContainerFromIndex(3)!.IsEnabled = false;
                if (state == SegmentState.Hover)
                    ((IPseudoClasses)control.ContainerFromIndex(1)!.Classes).Set(":pointerover", true);
                if (state == SegmentState.Pressed)
                    ((IPseudoClasses)control.ContainerFromIndex(1)!.Classes).Set(":pressed", true);
                if (state == SegmentState.Focused)
                    control.ContainerFromIndex(0)!.Focus(NavigationMethod.Tab);
                w.UpdateLayout();
            });
    }

    private static void Strip(Control root)
    {
        root.Transitions = null;
        foreach (var part in root.GetVisualDescendants().OfType<Animatable>())
            part.Transitions = null;
    }
}
