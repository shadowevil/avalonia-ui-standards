using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
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

/// <summary>The Sidebar Module: stages by window width, the user's toggle, the overlay, rows, keyboard and renders.</summary>
public class SidebarTests
{
    // Window widths inside each band, whatever the frame's shadow margin: expanded, compact, hidden.
    private const double Wide = 1200, Medium = 800, Narrow = 560;

    // --- Stages --------------------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData(Wide, SidebarStage.Expanded, 248)]
    [InlineData(Medium, SidebarStage.Compact, 56)]
    [InlineData(Narrow, SidebarStage.Hidden, 0)]
    public void Stage_FollowsWindowWidth(double width, SidebarStage stage, double layoutWidth)
    {
        var (window, sidebar) = Show(width);
        try
        {
            Assert.Equal(stage, sidebar.Stage);
            Assert.Equal(layoutWidth, sidebar.Bounds.Width);
            Assert.False(sidebar.IsOverlayOpen);
            // The glass panel starts SpacingS after the sidebar (at SpacingS from the frame when hidden).
            var panel = Part<Control>(window, StandardsWindowTheme.ContentPanelPart);
            var layers = Part<SquircleBorder>(window, StandardsWindowTheme.FramePart).Child!;
            Assert.Equal(layoutWidth + Spacing.S, panel.TranslatePoint(default, layers)!.Value.X, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Stage_ChangesAsTheWindowResizes_AtTheThresholds()
    {
        var (window, sidebar) = Show(Wide);
        try
        {
            var margin = Wide - sidebar.WindowWidth;
            foreach (var (frameWidth, stage) in new[]
                     {
                         (Sidebar.DefaultExpandedThreshold, SidebarStage.Expanded),
                         (Sidebar.DefaultExpandedThreshold - 1, SidebarStage.Compact),
                         (Sidebar.DefaultCompactThreshold, SidebarStage.Compact),
                         (Sidebar.DefaultCompactThreshold - 1, SidebarStage.Hidden),
                         (Sidebar.DefaultExpandedThreshold + 100, SidebarStage.Expanded),
                     })
            {
                window.Width = frameWidth + margin;
                Pump(window);
                Assert.Equal(frameWidth, sidebar.WindowWidth);
                Assert.Equal(stage, sidebar.Stage);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Thresholds_AreProperties()
    {
        var (window, sidebar) = Show(Medium);
        try
        {
            Assert.Equal(SidebarStage.Compact, sidebar.Stage);
            sidebar.ExpandedThreshold = 700;
            Pump(window);
            Assert.Equal(SidebarStage.Expanded, sidebar.Stage);
            sidebar.CompactThreshold = 900;
            sidebar.ExpandedThreshold = 1000;
            Pump(window);
            Assert.Equal(SidebarStage.Hidden, sidebar.Stage);
        }
        finally
        {
            window.Close();
        }
    }

    // --- The user's toggle -----------------------------------------------------------------

    [AvaloniaFact]
    public void Toggle_CollapsesAndExpands_UntilAThresholdIsCrossed()
    {
        var (window, sidebar) = Show(Wide);
        try
        {
            sidebar.Toggle();
            Pump(window);
            Assert.Equal(SidebarStage.Compact, sidebar.Stage);
            Assert.False(sidebar.IsOverlayOpen);
            Assert.Equal(56, sidebar.Bounds.Width);

            sidebar.Toggle();
            Pump(window);
            Assert.Equal(SidebarStage.Expanded, sidebar.Stage);

            // The user's choice holds while the window stays in the expanded band...
            sidebar.Toggle();
            window.Width = Wide + 100;
            Pump(window);
            Assert.Equal(SidebarStage.Compact, sidebar.Stage);

            // ...and ends when it crosses a threshold: back in the expanded band, the stage is automatic again.
            window.Width = Medium;
            Pump(window);
            Assert.Equal(SidebarStage.Compact, sidebar.Stage);
            window.Width = Wide;
            Pump(window);
            Assert.Equal(SidebarStage.Expanded, sidebar.Stage);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(Medium, SidebarStage.Compact, 56)]
    [InlineData(Narrow, SidebarStage.Hidden, 0)]
    public void Toggle_OpensTheOverlay_OverTheContent(double width, SidebarStage stage, double layoutWidth)
    {
        var (window, sidebar) = Show(width);
        try
        {
            sidebar.Toggle();
            Pump(window);
            Assert.True(sidebar.IsOverlayOpen);
            Assert.Equal(stage, sidebar.Stage);
            // The layout keeps the stage's width; the sidebar is drawn expanded over the panel, on glass.
            Assert.Equal(layoutWidth, sidebar.Bounds.Width);
            var surface = Part<GlassBorder>(sidebar, SidebarTheme.OverlaySurfacePart);
            Assert.True(surface.IsVisible);
            Assert.Equal(SidebarTheme.ExpandedWidth, surface.Bounds.Width);
            Assert.All(Rows(sidebar), row => Assert.Equal(1, Label(row).Opacity));

            // The row's label sits over the glass panel, and a click there lands on the row.
            var row = Rows(sidebar)[2];
            var point = row.TranslatePoint(new Point(row.Bounds.Width - 20, row.Bounds.Height / 2), window)!.Value;
            var panel = Part<Control>(window, StandardsWindowTheme.ContentPanelPart);
            Assert.True(point.X > panel.TranslatePoint(default, window)!.Value.X);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Pump(window);
            Assert.Same(row, sidebar.SelectedItem);
            Assert.False(sidebar.IsOverlayOpen);
            Assert.False(surface.IsVisible);
            Assert.Equal(stage, sidebar.Stage);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Overlay_ClosesOnAClickOutside_WithoutPassingItOn()
    {
        var clicks = 0;
        var button = new Button { Content = "Content button", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        button.Click += (_, _) => clicks++;
        var (window, sidebar) = Show(Narrow, content: button);
        try
        {
            sidebar.Toggle();
            Pump(window);
            // Inside the button's right end, clear of its 0.98 pressed scale.
            var point = button.TranslatePoint(new Point(button.Bounds.Width - 40, button.Bounds.Height / 2), window)!.Value;
            // The button sits under the overlay's left part; its right end is outside the overlay.
            Assert.True(point.X > SidebarTheme.EdgeGap + SidebarTheme.ExpandedWidth, $"button end at {point.X}");
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Pump(window);
            Assert.False(sidebar.IsOverlayOpen);
            Assert.Equal(0, clicks);

            // Closed, the content takes clicks again.
            window.MouseMove(point);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Pump(window);
            Assert.Equal(1, clicks);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Overlay_ClosesOnEscape_AndOnEnter()
    {
        var (window, sidebar) = Show(Narrow);
        try
        {
            sidebar.Toggle();
            Pump(window);
            Assert.True(sidebar.IsOverlayOpen);
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Pump(window);
            Assert.False(sidebar.IsOverlayOpen, "Escape did not close the overlay.");

            // Opened, the chosen row has the keyboard; arrows move the selection and keep the overlay open, Enter closes it.
            sidebar.SelectedIndex = 1;
            sidebar.Toggle();
            Pump(window);
            Assert.Same(Rows(sidebar)[0], TopLevel.GetTopLevel(sidebar)!.FocusManager!.GetFocusedElement());
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Pump(window);
            Assert.Equal(2, sidebar.SelectedIndex);
            Assert.True(sidebar.IsOverlayOpen);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            Pump(window);
            Assert.False(sidebar.IsOverlayOpen, "Enter did not close the overlay.");
            Assert.Equal(2, sidebar.SelectedIndex);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Overlay_ClosesWhenTheWindowCrossesAThreshold()
    {
        var (window, sidebar) = Show(Medium);
        try
        {
            sidebar.Toggle();
            Pump(window);
            Assert.True(sidebar.IsOverlayOpen);
            window.Width = Wide;
            Pump(window);
            Assert.False(sidebar.IsOverlayOpen);
            Assert.Equal(SidebarStage.Expanded, sidebar.Stage);
        }
        finally
        {
            window.Close();
        }
    }

    // --- The window's sidebar button ---------------------------------------------------------

    [AvaloniaFact]
    public void ToggleButton_ShowsOnlyWithASidebarModule_AndToggles()
    {
        var (window, sidebar) = Show(Medium);
        try
        {
            var toggle = Part<Button>(window, StandardsWindowTheme.SidebarTogglePart);
            Assert.True(toggle.IsEffectivelyVisible);
            Assert.Contains("subtle", toggle.Classes);
            Assert.Equal(Icons.SidebarSimple, Assert.IsType<Icon>(toggle.Content).Data);
            Assert.Equal(new Size(32, 32), toggle.Bounds.Size);

            // Its icon lines up with the rail's icons below it.
            var toggleIcon = (Icon)toggle.Content!;
            var rowIcon = Part<Icon>(Rows(sidebar)[0], SidebarTheme.IconPart);
            Assert.Equal(rowIcon.TranslatePoint(default, window)!.Value.X, toggleIcon.TranslatePoint(default, window)!.Value.X, 1);

            var centre = toggle.TranslatePoint(new Point(16, 16), window)!.Value;
            window.MouseDown(centre, MouseButton.Left);
            window.MouseUp(centre, MouseButton.Left);
            Pump(window);
            Assert.True(sidebar.IsOverlayOpen);

            // Pressing it with the overlay open closes the overlay (and does not reopen it).
            window.MouseDown(centre, MouseButton.Left);
            window.MouseUp(centre, MouseButton.Left);
            Pump(window);
            Assert.False(sidebar.IsOverlayOpen);

            window.Sidebar = new ListBox();
            Pump(window);
            Assert.False(toggle.IsEffectivelyVisible);
            // Plain sidebar content keeps the slot's own 8 px gap.
            var slot = Part<ContentPresenter>(window, StandardsWindowTheme.SidebarPart);
            Assert.Equal(new Thickness(Spacing.S, 0, 0, Spacing.S), slot.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToggleButton_StartsNoDragOrDoubleClick_AndIsAnInteractiveChromeElement(bool nativeFrame)
    {
        var (window, sidebar) = Show(Medium, nativeFrame: nativeFrame);
        try
        {
            var toggle = Part<Button>(window, StandardsWindowTheme.SidebarTogglePart);
            var band = Part<Control>(window, StandardsWindowTheme.TitleBandPart);
            // The window's band handler (drag, double-click action) takes only unhandled presses.
            // Presses on the button bubble to the band from the button, so if they arrive handled
            // that handler never runs. Every press reaching the band is recorded with its state.
            var bandPresses = new List<(bool FromToggle, bool Handled)>();
            band.AddHandler(InputElement.PointerPressedEvent, (_, e) => bandPresses.Add(
                (e.Source is Visual s && (s == toggle || toggle.IsVisualAncestorOf(s)), e.Handled)), handledEventsToo: true);
            // Not the title band's drag role: the OS passes input on it through to the button.
            Assert.Equal(WindowDecorationsElementRole.DecorationsElement, WindowDecorationProperties.GetElementRole(toggle));
            var centre = toggle.TranslatePoint(new Point(toggle.Bounds.Width / 2, toggle.Bounds.Height / 2), window)!.Value;
            // The nearest role above the hit element is the button's, not the band's.
            Assert.Equal(WindowDecorationsElementRole.DecorationsElement, RoleAt(window, centre));
            var bandPoint = band.TranslatePoint(new Point(band.Bounds.Width / 2, band.Bounds.Height / 2), window)!.Value;
            Assert.Equal(WindowDecorationsElementRole.TitleBar, RoleAt(window, bandPoint));

            // A double-click on it: both presses reach the band already handled by the button, so
            // no drag and no double-click action; it toggles twice and leaves the window as it was.
            for (var i = 0; i < 2; i++)
            {
                window.MouseDown(centre, MouseButton.Left);
                window.MouseUp(centre, MouseButton.Left);
            }
            Pump(window);
            Assert.Equal([(true, true), (true, true)], bandPresses);
            Assert.Equal(WindowState.Normal, window.WindowState);
            Assert.False(sidebar.IsOverlayOpen);

            // A double-click on the band itself still maximises (the recorder sees those presses too).
            Thread.Sleep(window.GetPlatformSettings()!.GetDoubleTapTime(PointerType.Mouse) + TimeSpan.FromMilliseconds(100));
            bandPresses.Clear();
            for (var i = 0; i < 2; i++)
            {
                window.MouseDown(bandPoint, MouseButton.Left);
                window.MouseUp(bandPoint, MouseButton.Left);
            }
            Pump(window);
            Assert.Equal(2, bandPresses.Count(p => !p.FromToggle));
            if (!nativeFrame)
                Assert.Equal(WindowState.Maximized, window.WindowState);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Rows ------------------------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData("Normal", 32, 20, 48)]
    [InlineData("Compact", 24, 16, 40)]
    public void Rows_FollowDensity_AndKeepTheIconInPlace(string density, double height, double iconSize, double rail)
    {
        var compact = density == "Compact";
        var (window, sidebar) = Show(Wide, density: compact ? DensityStyle.Compact : DensityStyle.Normal);
        try
        {
            var row = Rows(sidebar)[0];
            Assert.Equal(height, row.Bounds.Height);
            var icon = Part<Icon>(row, SidebarTheme.IconPart);
            Assert.Equal(iconSize, icon.Bounds.Width);
            var expandedX = icon.TranslatePoint(default, window)!.Value.X;

            sidebar.Toggle();
            Pump(window);
            Assert.Equal(SidebarTheme.EdgeGap + rail, sidebar.Bounds.Width);
            // The icon is centred in the rail's row, and is where it was when expanded.
            var railRow = row.TranslatePoint(default, window)!.Value.X;
            var iconX = icon.TranslatePoint(default, window)!.Value.X;
            Assert.Equal(railRow + (row.Bounds.Width - iconSize) / 2, iconX, 1);
            Assert.Equal(expandedX, iconX, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Rail_HidesLabels_AndShowsThemAsTooltips()
    {
        var (window, sidebar) = Show(Medium);
        try
        {
            foreach (var row in Rows(sidebar))
            {
                Assert.Contains(":collapsed", row.Classes);
                Assert.Equal(0, Label(row).Opacity);
                Assert.Equal(row.Content, ToolTip.GetTip(row));
            }
            var header = sidebar.GetVisualDescendants().OfType<SidebarHeader>().First();
            Assert.Equal(0, Part<ContentPresenter>(header, "PART_ContentPresenter").Opacity);
            Assert.True(header.Bounds.Height > 0, "The header keeps its height in the rail.");

            window.Width = Wide;
            Pump(window);
            foreach (var row in Rows(sidebar))
            {
                Assert.Equal(1, Label(row).Opacity);
                Assert.Null(ToolTip.GetTip(row));
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Rows_UseTheListItemDesign_WithTheIconInTheLabelColour()
    {
        var (window, sidebar) = Show(Wide);
        try
        {
            var row = Rows(sidebar)[0];
            Assert.Equal(ButtonParts.ChromePart, Part<SquircleBorder>(row, ButtonParts.ChromePart).Name);
            Assert.Equal(new CornerRadius(8), row.CornerRadius);
            var icon = Part<Icon>(row, SidebarTheme.IconPart);
            Assert.Equal(Color(Label(row).Foreground), Color(icon.Foreground));

            sidebar.SelectedItem = row;
            Pump(window);
            Assert.Equal(Colors.White, Color(Label(row).Foreground));
            Assert.Equal(Colors.White, Color(icon.Foreground));
        }
        finally
        {
            window.Close();
        }
    }

    // --- Selection and keyboard --------------------------------------------------------------

    [AvaloniaFact]
    public void Keyboard_MovesThroughRows_SkippingHeaders()
    {
        var (window, sidebar) = Show(Wide);
        try
        {
            var rows = Rows(sidebar);
            sidebar.SelectedItem = rows[0];
            rows[0].Focus(NavigationMethod.Tab);
            Pump(window);

            window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            Pump(window);
            Assert.Same(rows[0], sidebar.SelectedItem);

            foreach (var expected in rows.Skip(1))
            {
                window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
                Pump(window);
                Assert.Same(expected, sidebar.SelectedItem);
            }

            window.KeyPress(Key.Home, RawInputModifiers.None, PhysicalKey.Home, null);
            Pump(window);
            Assert.Same(rows[0], sidebar.SelectedItem);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Headers_AreNotSelectable()
    {
        var (window, sidebar) = Show(Wide);
        try
        {
            sidebar.SelectedItem = Rows(sidebar)[1];
            var header = sidebar.GetVisualDescendants().OfType<SidebarHeader>().Last();
            Assert.False(header.Focusable);
            var point = header.TranslatePoint(new Point(40, header.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Pump(window);
            Assert.Same(Rows(sidebar)[1], sidebar.SelectedItem);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HiddenSidebar_TakesNoFocus()
    {
        var (window, sidebar) = Show(Narrow);
        try
        {
            Assert.False(Part<ScrollViewer>(sidebar, "PART_ScrollViewer").IsVisible);
            // Tab from the content goes nowhere near the sidebar.
            var button = new Button { Content = "Content" };
            window.Content = button;
            Pump(window);
            button.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Pump(window);
            var focused = TopLevel.GetTopLevel(sidebar)!.FocusManager!.GetFocusedElement() as Visual;
            Assert.False(focused is not null && (focused == sidebar || sidebar.IsVisualAncestorOf(focused)), $"Focus went to {focused}.");
        }
        finally
        {
            window.Close();
        }
    }

    // --- Motion ------------------------------------------------------------------------------

    [AvaloniaFact]
    public void Toggle_AnimatesTheWidth_ResizingDoesNot_AndReduceMotionIsInstant()
    {
        var (window, sidebar) = Show(Wide, reduceMotion: false);
        try
        {
            Assert.Equal(2, sidebar.Transitions?.Count);
            Assert.All(sidebar.Transitions!.OfType<DoubleTransition>(), t => Assert.Equal(TimeSpan.FromMilliseconds(200), t.Duration));
            Assert.Equal(1, Label(Rows(sidebar)[0]).Transitions?.Count);

            // A toggle starts an animation from the current width.
            sidebar.Toggle();
            Assert.Equal(248, sidebar.GetValue(Sidebar.LayoutWidthProperty));
            Assert.Equal(SidebarStage.Compact, sidebar.Stage);

            // Past MotionNormal it has reached the rail, and the labels have faded out.
            Elapse(window, TimeSpan.FromMilliseconds(400));
            Assert.Equal(56, sidebar.GetValue(Sidebar.LayoutWidthProperty));
            Assert.Equal(56, sidebar.Bounds.Width);
            Assert.All(Rows(sidebar), row => Assert.Equal(0, Label(row).Opacity));

            // Crossing a threshold applies at once, with the transitions kept for the next toggle.
            window.Width = Narrow;
            Pump(window);
            Assert.Equal(0, sidebar.GetValue(Sidebar.LayoutWidthProperty));
            Assert.Equal(2, sidebar.Transitions?.Count);
        }
        finally
        {
            window.Close();
        }

        var (reduced, reducedSidebar) = Show(Wide, reduceMotion: true);
        try
        {
            Assert.Empty(reducedSidebar.Transitions!);
            reducedSidebar.Toggle();
            Assert.Equal(56, reducedSidebar.GetValue(Sidebar.LayoutWidthProperty));
        }
        finally
        {
            reduced.Close();
        }
    }

    // --- Focus -------------------------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstOverlay_InAFreshHiddenSidebar_FocusesTheSelectedRow(bool reduceMotion)
    {
        var (window, sidebar) = Show(Narrow, reduceMotion: reduceMotion);
        try
        {
            sidebar.SelectedIndex = 3;
            sidebar.Toggle();
            Assert.Same(sidebar.Items[3], Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Overlay_WithNoSelection_FocusesTheFirstRow_PastAHeader()
    {
        var (window, sidebar) = Show(Narrow, reduceMotion: false);
        try
        {
            Assert.Equal(-1, sidebar.SelectedIndex);
            Assert.IsType<SidebarHeader>(sidebar.Items[0]);
            sidebar.Toggle();
            Assert.Same(Rows(sidebar)[0], Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    public static TheoryData<string> CloseWays() => new() { "Escape", "Outside click", "Button" };

    [AvaloniaTheory]
    [MemberData(nameof(CloseWays))]
    public void ClosingTheOverlay_GivesFocusBackToTheButton(string way)
    {
        var (window, sidebar) = Show(Narrow, reduceMotion: false);
        try
        {
            var toggle = Part<Button>(window, StandardsWindowTheme.SidebarTogglePart);
            Assert.True(toggle.Focusable, "The sidebar button is a Tab stop.");
            toggle.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Pump(window);
            Assert.True(sidebar.IsOverlayOpen);
            Assert.IsType<SidebarItem>(Focused(window));

            switch (way)
            {
                case "Escape":
                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                    break;
                case "Outside click":
                    var outside = new Point(window.Bounds.Width - 60, window.Bounds.Height / 2);
                    window.MouseDown(outside, MouseButton.Left);
                    window.MouseUp(outside, MouseButton.Left);
                    break;
                default:
                    var centre = toggle.TranslatePoint(new Point(toggle.Bounds.Width / 2, toggle.Bounds.Height / 2), window)!.Value;
                    window.MouseDown(centre, MouseButton.Left);
                    window.MouseUp(centre, MouseButton.Left);
                    break;
            }
            Pump(window);
            Assert.False(sidebar.IsOverlayOpen);
            Assert.Same(toggle, Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(Narrow, true)]
    [InlineData(Medium, false)]
    public void ChoosingARow_KeepsFocusOnTheRailRow_OrGivesItBackWhenHidden(double width, bool backToButton)
    {
        var (window, sidebar) = Show(width);
        try
        {
            var toggle = Part<Button>(window, StandardsWindowTheme.SidebarTogglePart);
            toggle.Focus(NavigationMethod.Tab);
            sidebar.Toggle();
            Pump(window);
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            Pump(window);
            Assert.False(sidebar.IsOverlayOpen);
            Assert.Same(backToButton ? toggle : sidebar.SelectedItem, Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    private static object? Focused(Window window) => window.FocusManager!.GetFocusedElement();

    // Lets transitions run for about the given time: the headless render timer ticks, with real pauses.
    private static void Elapse(Window window, TimeSpan time)
    {
        for (var elapsed = TimeSpan.Zero; elapsed < time; elapsed += TimeSpan.FromMilliseconds(40))
        {
            Thread.Sleep(40);
            Pump(window);
        }
    }

    // --- Renders -----------------------------------------------------------------------------

    [AvaloniaFact]
    public void Stages_Render()
    {
        string[] columns = ["Expanded", "Compact (icon rail)", "Compact + overlay", "Hidden", "Hidden + overlay"];
        using var sheet = new Sheet("Sidebar: expanded at 1008 px and wider, the icon rail from 640 px, hidden below; the button opens the overlay");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, columns,
            [
                ("Normal", [
                    StageCell(sheet, theme, Wide, overlay: false),
                    StageCell(sheet, theme, Medium, overlay: false),
                    StageCell(sheet, theme, Medium, overlay: true),
                    StageCell(sheet, theme, Narrow, overlay: false),
                    StageCell(sheet, theme, Narrow, overlay: true),
                ]),
                ("Compact density", [
                    StageCell(sheet, theme, Wide, overlay: false, DensityStyle.Compact),
                    StageCell(sheet, theme, Medium, overlay: false, DensityStyle.Compact),
                    StageCell(sheet, theme, Medium, overlay: true, DensityStyle.Compact),
                    StageCell(sheet, theme, Narrow, overlay: false, DensityStyle.Compact),
                    StageCell(sheet, theme, Narrow, overlay: true, DensityStyle.Compact),
                ]),
            ]);
        sheet.Save("m3-sidebar.png");
    }

    [AvaloniaFact]
    public void TrafficLights_Render()
    {
        // The button follows the traffic lights on macOS, before the title.
        using var sheet = new Sheet("Sidebar button in the title band: after the traffic lights, and first with caption buttons; drawn and native macOS frame");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, ["Traffic lights", "Caption buttons"],
            [
                ("Drawn frame", [
                    StageCell(sheet, theme, Wide, overlay: false, buttons: WindowButtonStyle.TrafficLights),
                    StageCell(sheet, theme, Wide, overlay: false, buttons: WindowButtonStyle.CaptionButtons),
                ]),
                ("Native frame", [
                    StageCell(sheet, theme, Wide, overlay: false, buttons: WindowButtonStyle.TrafficLights, nativeFrame: true),
                    StageCell(sheet, theme, Wide, overlay: false, buttons: WindowButtonStyle.CaptionButtons, nativeFrame: true),
                ]),
            ]);
        sheet.Save("m3-sidebar-title-band.png");
    }

    private static Control StageCell(Sheet sheet, ThemeVariant theme, double width, bool overlay,
        DensityStyle density = DensityStyle.Normal, WindowButtonStyle buttons = WindowButtonStyle.CaptionButtons, bool nativeFrame = false)
    {
        var content = new StackPanel { Spacing = 8 };
        var heading = new TextBlock { Text = "Type", FontSize = 28, FontWeight = FontWeight.SemiBold };
        content.Children.Add(heading);
        foreach (var line in new[] { "Inter, on the type ramp:", "Caption 12, Body 14, Subtitle 20,", "Title 28, Large title 40, Display 68." })
            content.Children.Add(new TextBlock { Text = line });
        var (window, sidebar) = Show(width, theme, density: density, content: content, buttons: buttons, height: 420, nativeFrame: nativeFrame);
        try
        {
            window.SetRenderScaling(Sheet.Scale);
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

    // --- Helpers -----------------------------------------------------------------------------

    internal static Sidebar NewSidebar() => new()
    {
        Items =
        {
            new SidebarHeader { Content = "Style" },
            new SidebarItem { Icon = Icons.Palette, Content = "Colours" },
            new SidebarItem { Icon = Icons.TextAa, Content = "Type" },
            new SidebarItem { Icon = Icons.Ruler, Content = "Spacing" },
            new SidebarItem { Icon = Icons.SquaresFour, Content = "Shape" },
            new SidebarHeader { Content = "Blocks" },
            new SidebarItem { Icon = Icons.CursorClick, Content = "Button" },
            new SidebarItem { Icon = Icons.Bell, Content = "Icon" },
            new SidebarItem { Icon = Icons.TreeStructure, Content = "TreeView" },
        },
    };

    private static (StandardsWindow Window, Sidebar Sidebar) Show(double width, ThemeVariant? theme = null, bool reduceMotion = true,
        DensityStyle density = DensityStyle.Normal, Control? content = null, WindowButtonStyle buttons = WindowButtonStyle.CaptionButtons,
        double height = 520, bool nativeFrame = false, Action<StandardsWindow>? beforeShow = null)
    {
        var sidebar = NewSidebar();
        var window = new StandardsWindow(nativeFrame)
        {
            Title = "Sidebar",
            Width = width,
            Height = height,
            Sidebar = sidebar,
            Content = content ?? new TextBlock { Text = "Content" },
            WindowButtonStyle = buttons,
            RequestedThemeVariant = theme ?? ThemeVariant.Light,
            Background = new SolidColorBrush(Avalonia.Media.Color.Parse(theme == ThemeVariant.Dark ? "#3A3D42" : "#C9CDD3")),
        };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion, Density = density });
        beforeShow?.Invoke(window);
        window.Show();
        window.Activate();
        Pump(window);
        return (window, sidebar);
    }

    // The first chrome role (other than None) on the hit element or above it.
    private static WindowDecorationsElementRole RoleAt(Window window, Point point)
    {
        using (window.CaptureRenderedFrame()) { }
        for (var v = window.InputHitTest(point) as Visual; v is not null; v = v.GetVisualParent())
            if (v is Control c && WindowDecorationProperties.GetElementRole(c) is var role and not WindowDecorationsElementRole.None)
                return role;
        return WindowDecorationsElementRole.None;
    }

    // The rows are their own containers.
    private static List<SidebarItem> Rows(Sidebar sidebar) => sidebar.Items.OfType<SidebarItem>().ToList();

    private static ContentPresenter Label(SidebarItem row) => Part<ContentPresenter>(row, ButtonParts.ContentPart);

    private static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static Color Color(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

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
