using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// The Sidebar's footer rows: pinned below the list out of its scrolling, the list's row design and stages, one
/// selection across list and footer, keyboard and overlay, and renders.
/// </summary>
public class SidebarFooterTests
{
    private const double Wide = 1200, Medium = 800, Narrow = 560;

    [AvaloniaFact]
    public void Footer_IsPinnedAtTheBottom_AndDoesNotScroll()
    {
        // More rows than fit, so the list scrolls.
        var (window, sidebar) = Show(Wide, height: 360, extraRows: 12);
        try
        {
            var scroller = Part<ScrollViewer>(sidebar, "PART_ScrollViewer");
            var footerRow = FooterRow(sidebar);
            var bottomBefore = Bottom(footerRow, window);
            Assert.True(scroller.Extent.Height > scroller.Viewport.Height, "the list does not scroll");
            Assert.False(scroller.IsVisualAncestorOf(footerRow), "the footer row is inside the list's scroller");
            // At the bottom of the sidebar, inside the sidebar's padding.
            Assert.Equal(Bottom(sidebar, window) - Spacing.XS, bottomBefore, 1);
            // Below the list's viewport.
            Assert.True(footerRow.TranslatePoint(default, window)!.Value.Y >= Bottom(scroller, window) - 0.5);

            scroller.Offset = new Vector(0, 200);
            Pump(window);
            Assert.Equal(bottomBefore, Bottom(footerRow, window), 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Selection_IsOneAcrossListAndFooter_AndRaisedOnce()
    {
        var (window, sidebar) = Show(Wide);
        try
        {
            var raised = 0;
            sidebar.SelectionChanged += (_, _) => raised++;
            sidebar.SelectedIndex = 1;
            Pump(window);
            Assert.Equal(1, raised);

            // A click on the footer row selects it and clears the list.
            Click(window, FooterRow(sidebar));
            Assert.Equal(0, sidebar.SelectedFooterIndex);
            Assert.Equal(-1, sidebar.SelectedIndex);
            Assert.True(FooterRow(sidebar).IsSelected);
            Assert.Equal(2, raised);

            // A list row clears the footer.
            Click(window, Rows(sidebar)[2]);
            Assert.Equal(-1, sidebar.SelectedFooterIndex);
            Assert.Same(Rows(sidebar)[2], sidebar.SelectedItem);
            Assert.False(FooterRow(sidebar).IsSelected);
            Assert.Equal(3, raised);

            // In code, as with SelectedIndex.
            sidebar.SelectedFooterIndex = 0;
            Pump(window);
            Assert.Equal(-1, sidebar.SelectedIndex);
            Assert.Equal(4, raised);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FooterRows_UseTheRowDesign_AndFollowTheStages()
    {
        var (window, sidebar) = Show(Wide);
        try
        {
            var row = FooterRow(sidebar);
            var listRow = Rows(sidebar)[0];
            Assert.Equal(listRow.Bounds.Height, row.Bounds.Height);
            Assert.Equal(listRow.TranslatePoint(default, window)!.Value.X, row.TranslatePoint(default, window)!.Value.X, 1);
            Assert.Equal(1, Label(row).Opacity);
            sidebar.SelectedFooterIndex = 0;
            Pump(window);
            Assert.Equal(Colors.White, ((ISolidColorBrush)Label(row).Foreground!).Color);

            // The icon rail: label hidden, the label as the tooltip, the icon where the list's icons are.
            window.Width = Medium;
            Pump(window);
            Assert.Equal(SidebarStage.Compact, sidebar.Stage);
            Assert.Contains(":collapsed", row.Classes);
            Assert.Equal(0, Label(row).Opacity);
            Assert.Equal(row.Content, ToolTip.GetTip(row));
            Assert.Equal(IconX(listRow, window), IconX(row, window), 1);

            // Hidden: not shown and takes no focus; the overlay shows it expanded and focuses it when it is selected.
            window.Width = Narrow;
            Pump(window);
            Assert.False(sidebar.Footer.IsEffectivelyVisible);
            sidebar.Toggle();
            Pump(window);
            Assert.True(row.IsEffectivelyVisible);
            Assert.Equal(1, Label(row).Opacity);
            Assert.Same(row, window.FocusManager!.GetFocusedElement());

            // Choosing a footer row closes the overlay, as a list row does.
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            Pump(window);
            Assert.False(sidebar.IsOverlayOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Keyboard_ArrowsRunOnBetweenListAndFooter()
    {
        var (window, sidebar) = Show(Wide);
        try
        {
            var rows = Rows(sidebar);
            sidebar.SelectedItem = rows[^1];
            rows[^1].Focus(NavigationMethod.Tab);
            Pump(window);

            // Down from the list's last row: the footer row, selected and focused, and the list cleared.
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Pump(window);
            Assert.Same(FooterRow(sidebar), window.FocusManager!.GetFocusedElement());
            Assert.Equal(0, sidebar.SelectedFooterIndex);
            Assert.Equal(-1, sidebar.SelectedIndex);

            // Down again stays; Up goes back to the list's last row.
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Pump(window);
            Assert.Equal(0, sidebar.SelectedFooterIndex);
            Assert.Equal(-1, sidebar.SelectedIndex);
            window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            Pump(window);
            Assert.Same(rows[^1], window.FocusManager!.GetFocusedElement());
            Assert.Same(rows[^1], sidebar.SelectedItem);
            Assert.Equal(-1, sidebar.SelectedFooterIndex);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Keyboard_SidebarIsOneTabStop_WhoseTargetIsTheSelectedRow()
    {
        var (window, sidebar) = Show(Wide, content: new Button { Content = "Content button" });
        try
        {
            // From a list row, Tab leaves the sidebar (it does not stop at the footer).
            sidebar.SelectedIndex = 1;
            Rows(sidebar)[0].Focus(NavigationMethod.Tab);
            Pump(window);
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Pump(window);
            var focused = window.FocusManager!.GetFocusedElement() as Visual;
            Assert.False(focused is null || focused == sidebar || sidebar.IsVisualAncestorOf(focused), $"Tab stayed in the sidebar, on {focused}");

            // With the footer row selected, Tab from the control before the sidebar (its button) lands on that row.
            sidebar.SelectedFooterIndex = 0;
            var button = Part<Button>(window, StandardsWindowTheme.SidebarTogglePart);
            button.Focus(NavigationMethod.Tab);
            Pump(window);
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Pump(window);
            Assert.Same(FooterRow(sidebar), window.FocusManager!.GetFocusedElement());

            // And back: with a list row selected in code, Tab from the button lands on that row.
            sidebar.SelectedIndex = 2;
            button.Focus(NavigationMethod.Tab);
            Pump(window);
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Pump(window);
            Assert.Same(sidebar.ContainerFromIndex(2), window.FocusManager!.GetFocusedElement());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SelectionChanged_ReportsTheRowRemovedOnTheOtherSide()
    {
        var (window, sidebar) = Show(Wide);
        try
        {
            var events = new List<SelectionChangedEventArgs>();
            sidebar.SelectedIndex = 1;
            Pump(window);
            sidebar.SelectionChanged += (_, e) => events.Add(e);

            sidebar.SelectedFooterIndex = 0;
            Pump(window);
            var toFooter = Assert.Single(events);
            Assert.Contains(Rows(sidebar)[0], toFooter.RemovedItems.Cast<object>());
            Assert.Contains(FooterRow(sidebar), toFooter.AddedItems.Cast<object>());

            events.Clear();
            sidebar.SelectedIndex = 2;
            Pump(window);
            var toList = Assert.Single(events);
            Assert.Contains(FooterRow(sidebar), toList.RemovedItems.Cast<object>());
            Assert.Contains(sidebar.SelectedItem!, toList.AddedItems.Cast<object>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Footer_IsANamedListForAssistiveTechnology()
    {
        var (window, sidebar) = Show(Wide);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(sidebar.Footer);
            Assert.Equal(SidebarTheme.FooterText, peer.GetName());
            Assert.Equal(AutomationControlType.List, peer.GetAutomationControlType());
            var row = ControlAutomationPeer.CreatePeerForElement(FooterRow(sidebar));
            Assert.Equal("Settings", row.GetName());

            // An app renames it through the string resource.
            window.Resources[SidebarTheme.FooterTextKey] = "More";
            Pump(window);
            Assert.Equal("More", peer.GetName());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Footer_MovesWithTheSidebarToAnotherWindow()
    {
        var (window, sidebar) = Show(Wide);
        window.Sidebar = null;
        window.Close();
        var second = new StandardsWindow { Width = Wide, Height = 520, Sidebar = sidebar, Content = new TextBlock { Text = "Content" } };
        second.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        try
        {
            second.Show();
            Pump(second);
            Assert.True(FooterRow(sidebar).IsEffectivelyVisible);
            Assert.True(second.IsVisualAncestorOf(sidebar.Footer));
        }
        finally
        {
            second.Sidebar = null;
            second.Close();
        }
    }

    [AvaloniaFact]
    public void NoFooterItems_ShowsNoFooter()
    {
        var (window, sidebar) = Show(Wide, footer: false);
        try
        {
            Assert.False(sidebar.Footer.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Stages_RenderWithTheFooter()
    {
        string[] columns = ["Expanded", "Compact (icon rail)", "Compact + overlay", "Hidden", "Hidden + overlay"];
        using var sheet = new Sheet("Sidebar footer: Settings pinned below the list in every stage; list row selected, then the footer row");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, columns,
            [
                ("List row selected", [
                    Cell(sheet, theme, Wide, false, footerSelected: false),
                    Cell(sheet, theme, Medium, false, footerSelected: false),
                    Cell(sheet, theme, Medium, true, footerSelected: false),
                    Cell(sheet, theme, Narrow, false, footerSelected: false),
                    Cell(sheet, theme, Narrow, true, footerSelected: false),
                ]),
                ("Footer row selected", [
                    Cell(sheet, theme, Wide, false, footerSelected: true),
                    Cell(sheet, theme, Medium, false, footerSelected: true),
                    Cell(sheet, theme, Medium, true, footerSelected: true),
                    Cell(sheet, theme, Narrow, false, footerSelected: true),
                    Cell(sheet, theme, Narrow, true, footerSelected: true),
                ]),
            ]);
        sheet.Save("sidebar-footer.png");
    }

    private static Control Cell(Sheet sheet, ThemeVariant theme, double width, bool overlay, bool footerSelected)
    {
        var (window, sidebar) = Show(width, theme, height: 420);
        try
        {
            window.SetRenderScaling(Sheet.Scale);
            if (footerSelected)
                sidebar.SelectedFooterIndex = 0;
            else
                sidebar.SelectedIndex = 2;
            if (overlay)
                sidebar.Toggle();
            Pump(window);
            var frame = Part<Control>(window, StandardsWindowTheme.FramePart);
            var origin = frame.TranslatePoint(default, window)!.Value;
            using var capture = window.CaptureRenderedFrame()!;
            var crop = new PixelRect((int)(origin.X * Sheet.Scale), (int)(origin.Y * Sheet.Scale),
                (int)(Math.Min(400, frame.Bounds.Width) * Sheet.Scale), (int)(frame.Bounds.Height * Sheet.Scale));
            return sheet.Image(capture, crop);
        }
        finally
        {
            window.Close();
        }
    }

    private static (StandardsWindow Window, Sidebar Sidebar) Show(double width, ThemeVariant? theme = null, double height = 520,
        int extraRows = 0, bool footer = true, Control? content = null)
    {
        var sidebar = SidebarTests.NewSidebar();
        for (var i = 0; i < extraRows; i++)
            sidebar.Items.Add(new SidebarItem { Icon = Icons.Plus, Content = $"Row {i + 1}" });
        if (footer)
            sidebar.FooterItems.Add(new SidebarItem { Icon = Icons.Gear, Content = "Settings" });
        var window = new StandardsWindow
        {
            Title = "Sidebar",
            Width = width,
            Height = height,
            Sidebar = sidebar,
            Content = content ?? new TextBlock { Text = "Content" },
            WindowButtonStyle = WindowButtonStyle.CaptionButtons,
            RequestedThemeVariant = theme ?? ThemeVariant.Light,
            Background = new SolidColorBrush(Color.Parse(theme == ThemeVariant.Dark ? "#3A3D42" : "#C9CDD3")),
        };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        window.Show();
        window.Activate();
        Pump(window);
        return (window, sidebar);
    }

    private static SidebarItem FooterRow(Sidebar sidebar) => (SidebarItem)sidebar.FooterItems[0];

    private static List<SidebarItem> Rows(Sidebar sidebar) => sidebar.Items.OfType<SidebarItem>().ToList();

    private static ContentPresenter Label(SidebarItem row) => Part<ContentPresenter>(row, ButtonParts.ContentPart);

    private static double IconX(SidebarItem row, Window window) => Part<Control>(row, SidebarTheme.IconPart).TranslatePoint(default, window)!.Value.X;

    private static double Bottom(Control control, Window window) => control.TranslatePoint(new Point(0, control.Bounds.Height), window)!.Value.Y;

    private static void Click(Window window, Control control)
    {
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Pump(window);
    }

    private static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

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
