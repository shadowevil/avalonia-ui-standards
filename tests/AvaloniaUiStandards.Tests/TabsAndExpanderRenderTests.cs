using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the TabControl, TabStrip and Expander sheets (2x, labelled).</summary>
public class TabsAndExpanderRenderTests
{
    private static readonly string[] States = ["Rest", "Hover (Activity)", "Pressed (Activity)", "Disabled", "Focused (General)"];

    [AvaloniaFact]
    public void Tabs_Render()
    {
        using var sheet = new Sheet("TabControl and TabStrip: text headers, one accent underline sliding along a hairline; content on the surface");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, States,
            [
                ("TabControl", Enumerable.Range(0, States.Length).Select(s => TabCell(sheet, theme, s, Tabs)).ToList()),
                ("With icons", Enumerable.Range(0, States.Length).Select(s => TabCell(sheet, theme, s, () => Tabs(icons: true))).ToList()),
                ("Compact", Enumerable.Range(0, States.Length).Select(s => TabCell(sheet, theme, s, Tabs, compact: true)).ToList()),
                ("TabStrip", Enumerable.Range(0, States.Length).Select(s => TabCell(sheet, theme, s, StripControl)).ToList()),
                ("Strip on the left", Enumerable.Range(0, States.Length).Select(s => TabCell(sheet, theme, s, () => Tabs(placement: Dock.Left))).ToList()),
            ]);
        sheet.Save("m3-tabs.png");
    }

    [AvaloniaFact]
    public void TabsBeside_SegmentedControl_Render()
    {
        string[] columns = ["Glass panel", "Card"];
        using var sheet = new Sheet("Tabs switch content areas; a segmented control sets one value");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, columns,
            [
                ("Tabs", [Plain(sheet, theme, Tabs(), ListBoxRenderTests.Surface.GlassPanel), Plain(sheet, theme, Tabs(), ListBoxRenderTests.Surface.Card)]),
                ("Segmented", [Plain(sheet, theme, SegmentedControlRenderTests.Text(), ListBoxRenderTests.Surface.GlassPanel),
                    Plain(sheet, theme, SegmentedControlRenderTests.Text(), ListBoxRenderTests.Surface.Card)]),
            ]);
        sheet.Save("m3-tabs-and-segmented.png");
    }

    [AvaloniaFact]
    public void Expander_Renders()
    {
        string[] columns = ["Rest", "Hover", "Pressed", "Disabled", "Focused"];
        using var sheet = new Sheet("Expander: squircle card, caret turns towards the content, list-item header fills, hairline over the content");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, columns,
            [
                ("Collapsed", Enumerable.Range(0, 5).Select(s => ExpanderCell(sheet, theme, s, expanded: false)).ToList()),
                ("Expanded", Enumerable.Range(0, 5).Select(s => ExpanderCell(sheet, theme, s, expanded: true)).ToList()),
                ("Compact, expanded", Enumerable.Range(0, 5).Select(s => ExpanderCell(sheet, theme, s, expanded: true, compact: true)).ToList()),
                ("Opens upwards", Enumerable.Range(0, 5).Select(s => ExpanderCell(sheet, theme, s, expanded: true, direction: ExpandDirection.Up)).ToList()),
            ]);
        sheet.Save("m3-expander.png");
    }

    // --- Content -------------------------------------------------------------------------

    internal static TabControl Tabs() => Tabs(false);

    internal static TabControl Tabs(bool icons = false, Dock placement = Dock.Top)
    {
        var tabs = new TabControl { Width = icons ? 380 : placement == Dock.Top ? 300 : 340, TabStripPlacement = placement };
        foreach (var (header, icon) in new[] { ("General", Icons.Gear), ("Activity", Icons.Bell), ("Sharing", Icons.ChatText) })
            tabs.Items.Add(new TabItem
            {
                Header = header,
                Icon = icons ? new Icon { Data = icon } : null,
                Content = new TextBlock { Text = $"{header} settings appear here, on the surface below the tabs.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            });
        tabs.SelectedIndex = 0;
        return tabs;
    }

    private static Control StripControl()
    {
        var strip = new TabStrip();
        foreach (var header in new[] { "General", "Activity", "Sharing" })
            strip.Items.Add(new TabStripItem { Content = header });
        strip.SelectedIndex = 0;
        return strip;
    }

    // --- Cells ---------------------------------------------------------------------------

    private static Control TabCell(Sheet sheet, ThemeVariant theme, int state, Func<Control> create, bool compact = false)
    {
        var control = create();
        return sheet.Cell(control, theme,
            beforeShow: w => Density(w, compact),
            afterShow: w =>
            {
                w.UpdateLayout();
                Strip(control);
                var items = (SelectingItemsControl)control;
                var activity = (Control)items.ContainerFromIndex(1)!;
                switch (state)
                {
                    case 1: ((IPseudoClasses)activity.Classes).Set(":pointerover", true); break;
                    case 2: ((IPseudoClasses)activity.Classes).Set(":pressed", true); break;
                    case 3: control.IsEnabled = false; break;
                    case 4: items.ContainerFromIndex(0)!.Focus(NavigationMethod.Tab); break;
                }
                w.UpdateLayout();
            });
    }

    private static Control ExpanderCell(Sheet sheet, ThemeVariant theme, int state, bool expanded, bool compact = false,
        ExpandDirection direction = ExpandDirection.Down)
    {
        var expander = new Expander
        {
            Width = 280,
            Header = "Advanced options",
            ExpandDirection = direction,
            IsExpanded = expanded,
            Content = new StackPanel
            {
                Spacing = 8,
                Children = { new CheckBox { Content = "Keep a history" }, new TextBlock { Text = "Content sits inside the card." } },
            },
        };
        return sheet.Cell(expander, theme,
            beforeShow: w => Density(w, compact),
            afterShow: w =>
            {
                w.UpdateLayout();
                Strip(expander);
                var header = expander.GetVisualDescendants().OfType<ToggleButton>().First(t => t.Name == ExpanderTheme.HeaderPart);
                switch (state)
                {
                    case 1: ((IPseudoClasses)header.Classes).Set(":pointerover", true); break;
                    case 2: ((IPseudoClasses)header.Classes).Set(":pressed", true); break;
                    case 3: expander.IsEnabled = false; break;
                    case 4: header.Focus(NavigationMethod.Tab); break;
                }
                w.UpdateLayout();
            });
    }

    private static Control Plain(Sheet sheet, ThemeVariant theme, Control content, ListBoxRenderTests.Surface surface) =>
        sheet.Cell(ListBoxRenderTests.OnSurface(content, surface), theme, afterShow: w =>
        {
            w.UpdateLayout();
            Strip(content);
        });

    private static void Density(Window window, bool compact)
    {
        if (compact)
            window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = DensityStyle.Compact });
    }

    private static void Strip(Control root)
    {
        root.Transitions = null;
        foreach (var part in root.GetVisualDescendants().OfType<Animatable>())
            part.Transitions = null;
    }
}
