using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
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

    [AvaloniaFact]
    public void PillVariants_Render()
    {
        string[] columns = ["Text", "Icons", "Glass panel", "Several side by side"];
        using var sheet = new Sheet("SegmentedControl pill: neutral raised (the library) and accent with white text (the alternative)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, columns,
                new[] { false, true }.Select(accent => (accent ? "Accent pill" : "Neutral pill", (IReadOnlyList<Control>)
                [
                    VariantCell(sheet, theme, accent, Text()),
                    VariantCell(sheet, theme, accent, Glyphs()),
                    VariantCell(sheet, theme, accent, Text(), ListBoxRenderTests.Surface.GlassPanel),
                    VariantCell(sheet, theme, accent, Toolbar()),
                ])).ToList());
        sheet.Save("m3-segmented-control-pill.png");
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

    private static Control Toolbar() => new StackPanel
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal,
        Spacing = 8,
        Children =
        {
            new SegmentedControl { ItemsSource = new[] { "System", "Light", "Dark" }, SelectedIndex = 1 },
            new SegmentedControl { ItemsSource = new[] { "Normal", "Compact" }, SelectedIndex = 0 },
            new Button { Content = "Share" },
        },
    };

    // --- Cells ---------------------------------------------------------------------------

    private static Control Cell(Sheet sheet, ThemeVariant theme, SegmentState state, Func<SegmentedControl> create, bool compact = false)
    {
        var control = create();
        return sheet.Cell(control, theme,
            beforeShow: w =>
            {
                if (compact)
                    w.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = Avalonia.Themes.Fluent.DensityStyle.Compact });
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

    private static Control VariantCell(Sheet sheet, ThemeVariant theme, bool accent, Control content,
        ListBoxRenderTests.Surface? surface = null) =>
        sheet.Cell(surface is { } s ? ListBoxRenderTests.OnSurface(content, s) : content, theme,
            beforeShow: w =>
            {
                if (accent)
                    UseAccentPill(w);
            },
            afterShow: w =>
            {
                w.UpdateLayout();
                foreach (var control in content.GetVisualDescendants().OfType<SegmentedControl>().Append(content as SegmentedControl).OfType<SegmentedControl>())
                    Strip(control);
            });

    /// <summary>The alternative pill: the AA accent fill with white text, as selected list items and checked toggles.</summary>
    internal static void UseAccentPill(Window window)
    {
        Assert.True(window.TryFindResource("AccentFillColor", out var fill));
        window.Resources[SegmentedControlTheme.PillKey] = new SolidColorBrush((Color)fill!);
        window.Resources[SegmentedControlTheme.SelectedTextKey] = Brushes.White;
    }

    private static void Strip(Control root)
    {
        root.Transitions = null;
        foreach (var part in root.GetVisualDescendants().OfType<Animatable>())
            part.Transitions = null;
    }
}
