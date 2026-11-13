using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the accent flag sheet: ListBox, Sidebar and TreeView, off and on, states, themes and densities (2x).</summary>
public class AccentFlagRenderTests
{
    private enum State { Rest, Hover, Pressed, Selected, SelectedHover, SelectedPressed, SelectedFocus, SelectedDisabled }

    private static readonly string[] StateLabels =
        ["Rest", "Hover", "Pressed", "Selected", "Selected + hover", "Selected + pressed", "Selected + focus", "Selected, disabled"];

    /// <summary>A row of the ListBox and TreeView sections: the flag off or on, at a density.</summary>
    private sealed record Look(string Label, bool Flag, DensityStyle Density = DensityStyle.Normal);

    private static readonly Look[] Looks =
    [
        new("Off (default)", false),
        new("On", true),
        new("On, Compact", true, DensityStyle.Compact),
    ];

    [AvaloniaFact]
    public void AccentFlag_Renders()
    {
        using var sheet = new Sheet("SelectionOptions.AccentFlag: a 3 px lighter-accent bar on the selected item's leading edge, whose leading corners turn square");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            sheet.AddSection($"ListBox, {themeLabel}", StateLabels,
                Looks.Select(look => (look.Label, (IReadOnlyList<Control>)Enum.GetValues<State>().Select(s => ListCell(sheet, theme, s, look)).ToList())));

            sheet.AddSection($"Sidebar, {themeLabel}", ["Expanded, off", "Expanded, on", "Icon rail, off", "Icon rail, on", "Icon rail, on + focus", "Overlay, on"],
                new[] { ("Normal", DensityStyle.Normal), ("Compact", DensityStyle.Compact) }.Select(d => (d.Item1, (IReadOnlyList<Control>)
                [
                    SidebarCell(sheet, theme, d.Item2, 1200, flag: false),
                    SidebarCell(sheet, theme, d.Item2, 1200, flag: true),
                    SidebarCell(sheet, theme, d.Item2, 800, flag: false),
                    SidebarCell(sheet, theme, d.Item2, 800, flag: true),
                    SidebarCell(sheet, theme, d.Item2, 800, flag: true, focus: true),
                    SidebarCell(sheet, theme, d.Item2, 800, flag: true, overlay: true),
                ])));

            sheet.AddSection($"TreeView, {themeLabel}", StateLabels,
                Looks.Select(look => (look.Label, (IReadOnlyList<Control>)Enum.GetValues<State>().Select(s => TreeCell(sheet, theme, s, look)).ToList())));
        }
        sheet.Save("m3-accent-flag.png");
    }

    // --- Cells ---------------------------------------------------------------------------

    private static Control ListCell(Sheet sheet, ThemeVariant theme, State state, Look look)
    {
        var list = new ListBox { Width = 180, ItemsSource = new[] { "Documents" } };
        SelectionOptions.SetAccentFlag(list, look.Flag);
        return sheet.Cell(ListBoxRenderTests.OnSurface(list, ListBoxRenderTests.Surface.GlassPanel), theme,
            beforeShow: w => w.Styles.Add(Theme(look.Density)),
            afterShow: w =>
            {
                w.UpdateLayout();
                var item = (ListBoxItem)list.ContainerFromIndex(0)!;
                Apply(item, item, state);
            });
    }

    private static Control TreeCell(Sheet sheet, ThemeVariant theme, State state, Look look)
    {
        var item = new TreeViewItem { Header = "Documents", IsExpanded = true, Items = { new TreeViewItem { Header = "Notes" } } };
        var tree = new TreeView { Width = 180, Items = { item } };
        SelectionOptions.SetAccentFlag(tree, look.Flag);
        return sheet.Cell(ListBoxRenderTests.OnSurface(tree, ListBoxRenderTests.Surface.GlassPanel), theme,
            beforeShow: w => w.Styles.Add(Theme(look.Density)),
            afterShow: w =>
            {
                w.UpdateLayout();
                var row = item.GetVisualDescendants().OfType<Control>().First(c => c.Name == TreeViewTheme.RowPart && c.TemplatedParent == item);
                Apply(item, row, state);
            });
    }

    private static Control SidebarCell(Sheet sheet, ThemeVariant theme, DensityStyle density, double width, bool flag,
        bool focus = false, bool overlay = false)
    {
        var sidebar = SidebarTests.NewSidebar();
        SelectionOptions.SetAccentFlag(sidebar, flag);
        var window = new StandardsWindow
        {
            Title = "Sidebar",
            Width = width,
            Height = 360,
            Sidebar = sidebar,
            Content = new TextBlock { Text = "Content" },
            RequestedThemeVariant = theme,
            Background = new SolidColorBrush(Color.Parse(theme == ThemeVariant.Dark ? "#3A3D42" : "#C9CDD3")),
        };
        window.Styles.Add(Theme(density));
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        window.Activate();
        try
        {
            Pump(window);
            sidebar.SelectedIndex = 2;
            var rows = sidebar.Items.OfType<SidebarItem>().ToList();
            // A hovered row beside the selection: hover shows no flag.
            ((IPseudoClasses)rows[3].Classes).Set(":pointerover", true);
            if (overlay)
                sidebar.Toggle();
            Pump(window);
            if (focus)
                rows[1].Focus(NavigationMethod.Tab);
            Pump(window);
            var frame = window.GetVisualDescendants().OfType<Control>().First(c => c.Name == StandardsWindowTheme.FramePart);
            var origin = frame.TranslatePoint(default, window)!.Value;
            using var capture = window.CaptureRenderedFrame()!;
            var crop = new PixelRect((int)(origin.X * Sheet.Scale), (int)(origin.Y * Sheet.Scale),
                (int)((overlay || width > 1000 ? 280 : 120) * Sheet.Scale), (int)(250 * Sheet.Scale));
            return sheet.Image(capture, crop);
        }
        finally
        {
            window.Sidebar = null;
            window.Close();
        }
    }

    // --- Helpers -----------------------------------------------------------------------------

    private static StandardsTheme Theme(DensityStyle density) =>
        new(OsAccessibility.Settings.NotReduced) { ReduceMotion = true, Density = density };

    // The state on the item; hover and press are on the pointer target (the item, or a tree row).
    private static void Apply(Control item, Control target, State state)
    {
        if (state >= State.Selected && item is ListBoxItem listItem)
            listItem.IsSelected = true;
        if (state >= State.Selected && item is TreeViewItem treeItem)
            treeItem.IsSelected = true;
        if (state is State.Hover or State.SelectedHover)
            ((IPseudoClasses)target.Classes).Set(":pointerover", true);
        if (state is State.Pressed or State.SelectedPressed)
            ((IPseudoClasses)target.Classes).Set(":pressed", true);
        if (state == State.SelectedDisabled)
            item.IsEnabled = false;
        if (state == State.SelectedFocus)
            ((InputElement)item).Focus(NavigationMethod.Tab);
    }

    private static void Pump(Window window)
    {
        for (var i = 0; i < 2; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
        Dispatcher.UIThread.RunJobs();
    }
}
