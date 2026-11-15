using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Catalog;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// Full screen (0049): a macOS feature entered by the green light, Ctrl+Cmd+F and the <see cref="FullScreenMenuItem"/>,
/// leaving for the state before; the :fullscreen and :maximized states; the title band and menu row hiding above the top
/// edge and revealing at it, with the hide delay and while in use; and the window buttons following what the window can
/// do (CanMinimize, CanMaximize, CanResize) on every OS. Renders: screenshots/fullscreen-final-*.png.
/// </summary>
public class FullScreenTests
{
    private static StandardsWindow Show(WindowLayoutMode layout = WindowLayoutMode.MacOS, bool macOS = true, bool band = true,
        Action<StandardsWindow>? before = null, bool reduceMotion = true)
    {
        var window = new StandardsWindow(nativeFrame: false)
        {
            Title = "Documents",
            Width = 880,
            Height = 560,
            LayoutMode = layout,
            Toolbar = new Button { Name = "ToolbarButton", Content = "Share", Classes = { "subtle" }, HorizontalAlignment = HorizontalAlignment.Right },
            ToolbarBand = band ? new ToolbarBand { Items = { new Button { Content = "Bold" } } } : null,
            Content = new TextBlock { Text = "Content" },
            RequestedThemeVariant = ThemeVariant.Light,
        };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = reduceMotion });
        window.RunsOnMacOS = macOS;
        before?.Invoke(window);
        window.Show();
        window.Activate();
        MenuRowTests.Pump(window);
        return window;
    }

    private static TrafficLightButton Light(Window window, WindowButtonKind kind) =>
        window.GetVisualDescendants().OfType<TrafficLightButton>().Single(l => l.Kind == kind);

    private static CaptionButton Caption(Window window, WindowButtonKind kind) =>
        window.GetVisualDescendants().OfType<CaptionButton>().Single(c => c.Kind == kind);

    // A part's top in the frame (the visible window; in full screen the frame fills the window).
    private static double Top(Visual part, Window window) =>
        part.TranslatePoint(default, MenuRowTests.Part<SquircleBorder>(window, StandardsWindowTheme.FramePart))!.Value.Y - Border(window);

    // The frame's border width (none in full screen).
    private static double Border(Window window) => MenuRowTests.Part<SquircleBorder>(window, StandardsWindowTheme.FramePart).BorderThickness.Top;

    // The content presenter inside the glass panel (focusable here, so focus can leave the rows for it).
    private static ContentPresenter PanelContent(Window window)
    {
        var presenter = Panel(window).GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ContentPresenter");
        presenter.Focusable = true;
        return presenter;
    }

    private static Control TitleBand(Window window) => MenuRowTests.Part<Control>(window, StandardsWindowTheme.TitleBandPart);

    private static Control Panel(Window window) => MenuRowTests.Part<Control>(window, StandardsWindowTheme.ContentPanelPart);

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static Point Centre(Control control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    // --- States -------------------------------------------------------------------------------

    [AvaloniaFact]
    public void FullScreen_HasItsOwnPseudoClass_AndTheFrameFillsTheWindow()
    {
        var window = Show();
        try
        {
            window.WindowState = WindowState.Maximized;
            MenuRowTests.Pump(window);
            Assert.Contains(":maximized", window.Classes);
            Assert.DoesNotContain(":fullscreen", window.Classes);

            window.WindowState = WindowState.FullScreen;
            MenuRowTests.Pump(window);
            Assert.DoesNotContain(":maximized", window.Classes);
            Assert.Contains(":fullscreen", window.Classes);
            var frame = MenuRowTests.Part<SquircleBorder>(window, StandardsWindowTheme.FramePart);
            Assert.Equal(new Thickness(0), frame.Margin);
            Assert.Equal(new CornerRadius(0), frame.CornerRadius);
            Assert.Equal(new Thickness(0), frame.BorderThickness);
            Assert.False(MenuRowTests.Part<WindowShadow>(window, StandardsWindowTheme.ShadowPart).IsVisible);
            Assert.False(MenuRowTests.Part<Panel>(window, StandardsWindowTheme.ResizeGripsPart).IsVisible);

            window.WindowState = WindowState.Normal;
            MenuRowTests.Pump(window);
            Assert.DoesNotContain(":maximized", window.Classes);
            Assert.DoesNotContain(":fullscreen", window.Classes);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(WindowState.Normal)]
    [InlineData(WindowState.Maximized)]
    public void ToggleFullScreen_OnMacOS_EntersAndLeavesForTheStateBefore(WindowState before)
    {
        var window = Show();
        try
        {
            window.WindowState = before;
            window.ToggleFullScreen();
            Assert.Equal(WindowState.FullScreen, window.WindowState);
            window.ToggleFullScreen();
            Assert.Equal(before, window.WindowState);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ToggleFullScreen_DoesNothingOffMacOS_OrWhenTheWindowCannotBeMaximised()
    {
        var windows = new[]
        {
            Show(macOS: false),
            Show(before: w => w.CanMaximize = false),
            Show(before: w => w.CanResize = false),
        };
        try
        {
            foreach (var window in windows)
            {
                window.ToggleFullScreen();
                Assert.Equal(WindowState.Normal, window.WindowState);
                Assert.False(window.IsFullScreenAvailable);
            }
        }
        finally
        {
            foreach (var window in windows)
                window.Close();
        }
    }

    // --- Keys ---------------------------------------------------------------------------------

    [AvaloniaFact]
    public void CtrlCmdF_TogglesFullScreenOnMacOSOnly_AndCannotActKeysStayUnhandled()
    {
        var window = Show();
        var fixedSize = Show(before: w => w.CanResize = false);
        try
        {
            const KeyModifiers CtrlCmd = KeyModifiers.Control | KeyModifiers.Meta;
            Assert.True(window.HandleShortcut(Key.F, CtrlCmd, macOS: true));
            Assert.Equal(WindowState.FullScreen, window.WindowState);
            // Cmd+M does nothing in full screen, as on macOS.
            Assert.False(window.HandleShortcut(Key.M, KeyModifiers.Meta, macOS: true));
            Assert.Equal(WindowState.FullScreen, window.WindowState);
            Assert.True(window.HandleShortcut(Key.F, CtrlCmd, macOS: true));
            Assert.Equal(WindowState.Normal, window.WindowState);

            // Off macOS there is no full screen: the key is the app's.
            Assert.False(window.HandleShortcut(Key.F, CtrlCmd, macOS: false));
            Assert.False(window.HandleShortcut(Key.F11, KeyModifiers.None, macOS: false));
            Assert.Equal(WindowState.Normal, window.WindowState);

            // A window that cannot be maximised has no full screen; one that cannot be minimised ignores Cmd+M.
            Assert.False(fixedSize.HandleShortcut(Key.F, CtrlCmd, macOS: true));
            fixedSize.CanMinimize = false;
            Assert.False(fixedSize.HandleShortcut(Key.M, KeyModifiers.Meta, macOS: true));
            Assert.Equal(WindowState.Normal, fixedSize.WindowState);

            // Esc does not leave full screen.
            window.ToggleFullScreen();
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            MenuRowTests.Pump(window);
            Assert.Equal(WindowState.FullScreen, window.WindowState);
        }
        finally
        {
            window.Close();
            fixedSize.Close();
        }
    }

    [AvaloniaFact]
    public void CtrlCmdF_PressedInTheWindow_TogglesFullScreen()
    {
        if (!OperatingSystem.IsMacOS())
            return;
        var window = Show();
        try
        {
            MenuRowTests.Press(window, Key.F, RawInputModifiers.Control | RawInputModifiers.Meta, "f", PhysicalKey.F);
            Assert.Equal(WindowState.FullScreen, window.WindowState);
            MenuRowTests.Press(window, Key.F, RawInputModifiers.Control | RawInputModifiers.Meta, "f", PhysicalKey.F);
            Assert.Equal(WindowState.Normal, window.WindowState);
        }
        finally
        {
            window.Close();
        }
    }

    // --- The green light ----------------------------------------------------------------------

    [AvaloniaFact]
    public void GreenLight_OnMacOS_EntersAndLeavesFullScreen_OptionClickZooms()
    {
        var window = Show();
        try
        {
            var green = Light(window, WindowButtonKind.Maximize);
            Assert.Equal(ZoomGlyph.EnterFullScreen, green.ZoomGlyph);
            Assert.Equal("Enter Full Screen", Avalonia.Automation.AutomationProperties.GetName(green));

            var point = Centre(green, window);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            MenuRowTests.Pump(window);
            Assert.Equal(WindowState.FullScreen, window.WindowState);
            Assert.Equal(ZoomGlyph.ExitFullScreen, green.ZoomGlyph);
            Assert.True(green.IsEnabled);
            // In full screen the minimise light is disabled.
            Assert.False(Light(window, WindowButtonKind.Minimize).IsEnabled);

            Click(green);
            Assert.Equal(WindowState.Normal, window.WindowState);
            Assert.True(Light(window, WindowButtonKind.Minimize).IsEnabled);

            // Option held over the lights: the plus glyph, and a click zooms.
            MenuRowTests.Pump(window);
            point = Centre(green, window);
            window.MouseMove(point, RawInputModifiers.Alt);
            Assert.Equal(ZoomGlyph.Zoom, green.ZoomGlyph);
            window.MouseDown(point, MouseButton.Left, RawInputModifiers.Alt);
            window.MouseUp(point, MouseButton.Left, RawInputModifiers.Alt);
            MenuRowTests.Pump(window);
            Assert.Equal(WindowState.Maximized, window.WindowState);
            window.MouseMove(point);
            Assert.Equal(ZoomGlyph.EnterFullScreen, green.ZoomGlyph);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void GreenLight_OffMacOS_Zooms()
    {
        var window = Show(macOS: false);
        try
        {
            var green = Light(window, WindowButtonKind.Maximize);
            Assert.Equal(ZoomGlyph.Zoom, green.ZoomGlyph);
            Assert.Equal("Zoom", Avalonia.Automation.AutomationProperties.GetName(green));
            Click(green);
            Assert.Equal(WindowState.Maximized, window.WindowState);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Window buttons follow what the window can do --------------------------------------

    [AvaloniaFact]
    public void CaptionButtons_ShowOnlyWhatTheWindowCanDo()
    {
        var windows = new[]
        {
            Show(WindowLayoutMode.Windows, macOS: false),
            Show(WindowLayoutMode.Windows, macOS: false, before: w => w.CanResize = false),
            Show(WindowLayoutMode.Windows, macOS: false, before: w => (w.CanResize, w.CanMinimize) = (false, false)),
            Show(WindowLayoutMode.Windows, macOS: false, before: w => w.CanMaximize = false),
        };
        try
        {
            bool[][] expected = [[true, true], [true, false], [false, false], [true, false]];
            for (var i = 0; i < windows.Length; i++)
            {
                Assert.Equal(expected[i][0], Caption(windows[i], WindowButtonKind.Minimize).IsVisible);
                Assert.Equal(expected[i][1], Caption(windows[i], WindowButtonKind.Maximize).IsVisible);
                Assert.True(Caption(windows[i], WindowButtonKind.Close).IsVisible);
            }

            // It follows the properties live, and the close button stays flush right.
            var window = windows[0];
            window.CanMinimize = false;
            window.CanResize = false;
            MenuRowTests.Pump(window);
            Assert.False(Caption(window, WindowButtonKind.Minimize).IsVisible);
            Assert.False(Caption(window, WindowButtonKind.Maximize).IsVisible);
            var close = Caption(window, WindowButtonKind.Close);
            var frame = MenuRowTests.Part<SquircleBorder>(window, StandardsWindowTheme.FramePart);
            Assert.Equal(frame.Bounds.Width - 1, close.TranslatePoint(new Point(close.Bounds.Width, 0), frame)!.Value.X, 1);
        }
        finally
        {
            foreach (var window in windows)
                window.Close();
        }
    }

    [AvaloniaFact]
    public void TrafficLights_DisableWhatTheWindowCannotDo()
    {
        var window = Show(before: w => (w.CanResize, w.CanMinimize) = (false, false));
        try
        {
            var green = Light(window, WindowButtonKind.Maximize);
            var yellow = Light(window, WindowButtonKind.Minimize);
            Assert.False(green.IsEnabled);
            Assert.False(yellow.IsEnabled);
            Assert.True(Light(window, WindowButtonKind.Close).IsEnabled);
            // Disabled lights are the neutral grey and show no glyph, even while the group is hovered.
            foreach (var light in window.GetVisualDescendants().OfType<TrafficLightButton>())
                light.SetGlyphVisible(true);
            MenuRowTests.Pump(window);
            var grey = (Color)green.FindResource(green.ActualThemeVariant, "SystemBaseLowColor")!;
            Assert.Equal(grey, ((ISolidColorBrush)green.Background!).Color);
            var glyph = green.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().Single(p => p.Name == WindowButtonThemes.GlyphPart);
            Assert.Equal(0, glyph.Opacity);

            window.CanResize = true;
            window.CanMinimize = true;
            MenuRowTests.Pump(window);
            Assert.True(green.IsEnabled);
            Assert.True(yellow.IsEnabled);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FixedSizeWindow_HasNoResizeGrips_AndTheTitleBandDoubleClickDoesNotMaximise()
    {
        var window = Show(WindowLayoutMode.Windows, macOS: false, before: w => (w.CanResize, w.CanMinimize) = (false, false));
        try
        {
            Assert.False(MenuRowTests.Part<Panel>(window, StandardsWindowTheme.ResizeGripsPart).IsVisible);
            window.OnTitleBandDoubleClick(null);
            Assert.Equal(WindowState.Normal, window.WindowState);
            window.OnTitleBandDoubleClick("Minimize");
            Assert.Equal(WindowState.Normal, window.WindowState);
            window.CanResize = true;
            MenuRowTests.Pump(window);
            Assert.True(MenuRowTests.Part<Panel>(window, StandardsWindowTheme.ResizeGripsPart).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    // --- The reveal ---------------------------------------------------------------------------

    private sealed class Delays
    {
        private readonly List<(TimeSpan Delay, Action Action)> _queue = [];

        public IReadOnlyList<(TimeSpan Delay, Action Action)> Queue => _queue;

        public void Add(TimeSpan delay, Action action) => _queue.Add((delay, action));

        public void RunAll()
        {
            var due = _queue.ToList();
            _queue.Clear();
            foreach (var (_, action) in due)
                action();
        }
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void FullScreen_HidesTheTitleBandAboveTheTopEdge_TheToolbarBandStays(bool band)
    {
        var window = Show(band: band);
        try
        {
            var bandSlot = MenuRowTests.Part<ContentPresenter>(window, StandardsWindowTheme.ToolbarBandPart);
            window.WindowState = WindowState.FullScreen;
            MenuRowTests.Pump(window);
            Assert.False(window.IsTopRevealed);
            Assert.Equal(-TitleBand(window).Bounds.Height, Top(TitleBand(window), window), 1);
            if (band)
            {
                Assert.Equal(0, Top(bandSlot, window), 1);
                Assert.Equal(bandSlot.Bounds.Height, Top(Panel(window), window), 1);
            }
            else
                Assert.Equal(Spacing.S, Top(Panel(window), window), 1);

            window.WindowState = WindowState.Normal;
            MenuRowTests.Pump(window);
            Assert.Equal(0, Top(TitleBand(window), window), 1);
            Assert.Equal(TitleBand(window).Bounds.Height + (band ? bandSlot.Bounds.Height : 0), Top(Panel(window), window), 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Reveal_AtTheTopEdge_HidesHalfASecondAfterThePointerLeaves()
    {
        var delays = new Delays();
        var window = Show(before: w => w.RevealDelay = delays.Add);
        try
        {
            window.WindowState = WindowState.FullScreen;
            MenuRowTests.Pump(window);
            var panelTop = Top(Panel(window), window);
            var bandSlot = MenuRowTests.Part<ContentPresenter>(window, StandardsWindowTheme.ToolbarBandPart);

            window.MouseMove(new Point(400, 3));
            MenuRowTests.Pump(window);
            Assert.False(window.IsTopRevealed);
            window.MouseMove(new Point(400, StandardsWindow.RevealEdge));
            MenuRowTests.Pump(window);
            Assert.True(window.IsTopRevealed);
            Assert.True(MenuRowTests.Part<GlassBorder>(window, StandardsWindowTheme.RevealSheetPart).IsVisible);
            Assert.Equal(0, Top(TitleBand(window), window), 1);
            Assert.Equal(TitleBand(window).Bounds.Height, Top(bandSlot, window), 1);
            // The content does not move.
            Assert.Equal(panelTop, Top(Panel(window), window), 1);

            // Over the revealed rows and band: nothing pending. Below them: one hide, after 500 ms.
            window.MouseMove(new Point(400, 60));
            Assert.Empty(delays.Queue);
            window.MouseMove(new Point(400, 300));
            window.MouseMove(new Point(400, 320));
            var (delay, _) = Assert.Single(delays.Queue);
            Assert.Equal(TimeSpan.FromMilliseconds(500), delay);

            // Back over them before it runs: it is cancelled.
            window.MouseMove(new Point(400, 40));
            delays.RunAll();
            Assert.True(window.IsTopRevealed);

            window.MouseMove(new Point(400, 300));
            delays.RunAll();
            MenuRowTests.Pump(window);
            Assert.False(window.IsTopRevealed);
            Assert.False(MenuRowTests.Part<GlassBorder>(window, StandardsWindowTheme.RevealSheetPart).IsVisible);
            Assert.Equal(-TitleBand(window).Bounds.Height, Top(TitleBand(window), window), 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Reveal_KeyboardFocusInTheRows_RevealsAndKeepsThem()
    {
        var delays = new Delays();
        var window = Show(before: w => w.RevealDelay = delays.Add);
        try
        {
            window.WindowState = WindowState.FullScreen;
            MenuRowTests.Pump(window);
            var share = MenuRowTests.Part<Button>(window, "ToolbarButton");
            share.Focus(NavigationMethod.Tab);
            MenuRowTests.Pump(window);
            Assert.True(window.IsTopRevealed);

            // In use: the hide waits and tries again.
            window.MouseMove(new Point(400, 300));
            delays.RunAll();
            Assert.True(window.IsTopRevealed);
            Assert.Single(delays.Queue);

            // Focus leaves: it hides when the delay runs.
            PanelContent(window).Focus();
            delays.RunAll();
            MenuRowTests.Pump(window);
            Assert.False(window.IsTopRevealed);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Reveal_StaysWhileAMenuFromTheRowsIsOpen()
    {
        var delays = new Delays();
        var window = Show(WindowLayoutMode.Windows, before: w =>
        {
            w.RevealDelay = delays.Add;
            NativeMenu.SetMenu(w, new NativeMenu { new NativeMenuItem("_File") { Menu = new NativeMenu { new NativeMenuItem("_New") } } });
        });
        try
        {
            window.WindowState = WindowState.FullScreen;
            MenuRowTests.Pump(window);
            // The menu row hides with the title band.
            var menuRow = MenuRowTests.Part<Panel>(window, StandardsWindowTheme.MenuRowPart);
            Assert.True(Top(menuRow, window) < 0);

            // F10 opens the menu bar: the rows reveal and stay while it is open.
            MenuRowTests.Press(window, Key.F10);
            Assert.True(window.IsTopRevealed);
            window.MenuRow.Titles[0].IsSubMenuOpen = true;
            MenuRowTests.Pump(window);
            window.MouseMove(new Point(400, 400));
            delays.RunAll();
            Assert.True(window.IsTopRevealed);

            window.MenuRow.Close();
            PanelContent(window).Focus();
            MenuRowTests.Pump(window);
            delays.RunAll();
            Assert.False(window.IsTopRevealed);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Reveal_SlidesOverMotionNormal_InstantUnderReduceMotion()
    {
        var moving = Show(reduceMotion: false);
        var reduced = Show();
        try
        {
            var transition = Assert.IsType<Avalonia.Animation.DoubleTransition>(Assert.Single(moving.Top!.Transitions!));
            Assert.Equal(WindowTop.RevealProperty, transition.Property);
            Assert.Equal(TimeSpan.FromMilliseconds(200), transition.Duration);
            Assert.IsType<Avalonia.Animation.Easings.CubicEaseOut>(transition.Easing);
            Assert.Empty(reduced.Top!.Transitions!);
        }
        finally
        {
            moving.Close();
            reduced.Close();
        }
    }

    [AvaloniaFact]
    public void Reveal_AClickInTheRowsOrTheBand_DoesNotKeepThem()
    {
        var delays = new Delays();
        var window = Show(before: w => w.RevealDelay = delays.Add);
        try
        {
            window.WindowState = WindowState.FullScreen;
            MenuRowTests.Pump(window);
            foreach (var target in new[]
                     {
                         MenuRowTests.Part<Button>(window, "ToolbarButton"),
                         ((ToolbarBand)window.ToolbarBand!).Items.OfType<Button>().First(),
                     })
            {
                window.MouseMove(new Point(400, 0));
                MenuRowTests.Pump(window);
                Assert.True(window.IsTopRevealed);
                var point = Centre(target, window);
                window.MouseMove(point);
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);
                MenuRowTests.Pump(window);
                Assert.True(target.IsFocused);

                window.MouseMove(new Point(400, 300));
                delays.RunAll();
                MenuRowTests.Pump(window);
                Assert.False(window.IsTopRevealed);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(WindowState.Normal)]
    [InlineData(WindowState.Maximized)]
    public void CaptionMaximise_InFullScreen_ShowsRestore_AndLeavesForTheStateBefore(WindowState before)
    {
        var window = Show(WindowLayoutMode.Windows);
        try
        {
            window.WindowState = before;
            window.ToggleFullScreen();
            MenuRowTests.Pump(window);
            var maximise = Caption(window, WindowButtonKind.Maximize);
            Assert.True(maximise.IsMaximized);
            Assert.Equal("Restore", Avalonia.Automation.AutomationProperties.GetName(maximise));
            Click(maximise);
            Assert.Equal(before, window.WindowState);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(WindowState.Normal)]
    [InlineData(WindowState.Maximized)]
    public void CaptionRestore_OffMacOS_LeavesFullScreenAnAppSet(WindowState before)
    {
        var window = Show(WindowLayoutMode.Windows, macOS: false);
        try
        {
            window.WindowState = before;
            window.WindowState = WindowState.FullScreen;
            MenuRowTests.Pump(window);
            var maximise = Caption(window, WindowButtonKind.Maximize);
            Assert.True(maximise.IsMaximized);
            Click(maximise);
            Assert.Equal(before, window.WindowState);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void SystemTitleBarStrip_MatchesOnlyThisWindowsStrip()
    {
        IntPtr window = 1, other = 2, screen = 10, otherScreen = 11;
        // With a parent: only this window's.
        Assert.True(MacWindowSettings.IsOwnStrip(window, window, screen, screen, false, false));
        Assert.False(MacWindowSettings.IsOwnStrip(window, other, screen, screen, true, true));
        // Without one: same screen (never a missing one) and both on the active Space.
        Assert.True(MacWindowSettings.IsOwnStrip(window, IntPtr.Zero, screen, screen, true, true));
        Assert.False(MacWindowSettings.IsOwnStrip(window, IntPtr.Zero, screen, otherScreen, true, true));
        Assert.False(MacWindowSettings.IsOwnStrip(window, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, true, true));
        Assert.False(MacWindowSettings.IsOwnStrip(window, IntPtr.Zero, screen, screen, true, false));
        Assert.False(MacWindowSettings.IsOwnStrip(window, IntPtr.Zero, screen, screen, false, true));
    }

    [AvaloniaFact]
    public void SystemTitleBarStrip_IsLeftAloneWithoutANativeWindow_AndFailsSafely()
    {
        // AppKit's full screen strip exists only on a native macOS window; without one nothing is looked up.
        Assert.False(MacWindowSettings.HideFullScreenTitleBarWindow(IntPtr.Zero));
        var window = Show();
        try
        {
            window.WindowState = WindowState.FullScreen;
            window.MouseMove(new Point(400, 0));
            MenuRowTests.Pump(window);
            Assert.True(window.IsTopRevealed);
            // The strip check runs only on the native macOS frame.
            Assert.False(window.IsStripTimerRunning);
        }
        finally
        {
            window.Close();
        }
    }

    // --- The menu item ------------------------------------------------------------------------

    private static (StandardsWindow Window, FullScreenMenuItem Item) WithItem(bool macOS = true, WindowLayoutMode layout = WindowLayoutMode.MacOS,
        Action<StandardsWindow>? before = null)
    {
        var item = new FullScreenMenuItem();
        var window = Show(layout, macOS, before: w =>
        {
            NativeMenu.SetMenu(w, new NativeMenu { new NativeMenuItem("_View") { Menu = new NativeMenu { item } } });
            before?.Invoke(w);
        });
        return (window, item);
    }

    private static void Choose(NativeMenuItem item) => ((INativeMenuItemExporterEventsImplBridge)item).RaiseClicked();

    [AvaloniaFact]
    public void MenuItem_TogglesFullScreen_AndItsHeaderFollows()
    {
        var (window, item) = WithItem();
        try
        {
            Assert.Same(window, item.Window);
            Assert.True(item.IsVisible);
            Assert.True(item.IsEnabled);
            Assert.Equal("Enter Full Screen", item.Header);
            Assert.Equal(new KeyGesture(Key.F, KeyModifiers.Control | KeyModifiers.Meta), item.Gesture);

            Choose(item);
            Assert.Equal(WindowState.FullScreen, window.WindowState);
            Assert.Equal("Exit Full Screen", item.Header);
            Choose(item);
            Assert.Equal(WindowState.Normal, window.WindowState);
            Assert.Equal("Enter Full Screen", item.Header);

            window.CanMaximize = false;
            Assert.False(item.IsEnabled);
        }
        finally
        {
            window.Close();
        }
        Assert.Null(item.Window);
    }

    [AvaloniaFact]
    public void MenuItem_IsHiddenOffMacOS_AndDoesNothing()
    {
        var (window, item) = WithItem(macOS: false, layout: WindowLayoutMode.Windows);
        try
        {
            Assert.False(item.IsVisible);
            Choose(item);
            Assert.Equal(WindowState.Normal, window.WindowState);
            Assert.DoesNotContain(window.MenuRow.Titles[0].Items.OfType<MenuItem>(), m => m.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MenuItem_HeadersComeFromResources_AndShowInTheMenuRowOnMacOS()
    {
        var (window, item) = WithItem(layout: WindowLayoutMode.Windows, before: w =>
        {
            w.Resources[FullScreenMenuItem.EnterHeaderKey] = "Plein écran";
            w.Resources[FullScreenMenuItem.ExitHeaderKey] = "Quitter le plein écran";
        });
        try
        {
            Assert.Equal("Plein écran", item.Header);
            var row = window.MenuRow.Titles[0].Items.OfType<MenuItem>().Single();
            Assert.True(row.IsVisible);
            Assert.Equal("Plein écran", row.Header);
            window.ToggleFullScreen();
            MenuRowTests.Pump(window);
            Assert.Equal("Quitter le plein écran", row.Header);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Renders ------------------------------------------------------------------------------

    private static MainWindow Catalog(WindowLayoutMode layout, ThemeVariant variant, bool fullScreen, Action<MainWindow>? before = null)
    {
        CatalogLayout.Set(Application.Current!, layout);
        var window = new MainWindow
        {
            Width = fullScreen ? 960 : 1008,
            Height = fullScreen ? 600 : 648,
            RequestedThemeVariant = variant,
            Background = new SolidColorBrush(Color.Parse(variant == ThemeVariant.Dark ? "#3A3D42" : "#C9CDD3")),
        };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true });
        window.RunsOnMacOS = true;
        before?.Invoke(window);
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        window.Activate();
        window.SelectPage(1);
        if (fullScreen)
            window.WindowState = WindowState.FullScreen;
        MenuRowTests.Pump(window);
        return window;
    }

    private static Control Shot(Sheet sheet, Window window, PixelRect? crop = null, Action? after = null)
    {
        after?.Invoke();
        MenuRowTests.Pump(window);
        using var frame = Frame.Capture(window);
        var image = sheet.Image(frame.Bitmap, crop);
        window.Close();
        return image;
    }

    private static PixelRect Px(double x, double y, double w, double h) =>
        new((int)(x * Sheet.Scale), (int)(y * Sheet.Scale), (int)(w * Sheet.Scale), (int)(h * Sheet.Scale));

    private static void Reveal(Window window)
    {
        window.MouseMove(new Point(300, 0));
        MenuRowTests.Pump(window);
    }

    [AvaloniaFact]
    public void Renders()
    {
        try
        {
            foreach (var (layout, tag) in new[] { (WindowLayoutMode.MacOS, "macos"), (WindowLayoutMode.Windows, "windows-layout-on-macos") })
            foreach (var (label, variant) in ButtonRenderTests.Themes)
            {
                using var sheet = new Sheet($"Full screen, {tag}, {label.ToLowerInvariant()}");
                MainWindow revealed = Catalog(layout, variant, true);
                sheet.AddSection("", ["Windowed", "Full screen at rest", "Full screen, top revealed"],
                [
                    ("", [Shot(sheet, Catalog(layout, variant, false)), Shot(sheet, Catalog(layout, variant, true)), Shot(sheet, revealed, after: () => Reveal(revealed))]),
                ]);
                sheet.Save($"fullscreen-final-{tag}-{label.ToLowerInvariant()}.png");
            }

            using (var sheet = new Sheet("Window buttons: fixed size (CanResize = false), full screen and the green light's glyphs (crops)"))
            {
                foreach (var (label, variant) in ButtonRenderTests.Themes)
                {
                    var mac = Px(24, 16, 120, 48);
                    var win = Px(24 + 960 - 200, 16, 200, 48);
                    Control Lights(Action<MainWindow> setup, bool hover = true, bool fullScreen = false)
                    {
                        var w = Catalog(WindowLayoutMode.MacOS, variant, fullScreen, setup);
                        if (fullScreen)
                            Reveal(w);
                        return Shot(sheet, w, fullScreen ? Px(0, 0, 120, 48) : mac, () =>
                        {
                            if (hover)
                                foreach (var l in w.GetVisualDescendants().OfType<TrafficLightButton>())
                                    l.SetGlyphVisible(true);
                        });
                    }
                    Control Captions(Action<MainWindow> setup) => Shot(sheet, Catalog(WindowLayoutMode.Windows, variant, false, w =>
                    {
                        w.RunsOnMacOS = false;
                        setup(w);
                    }), win);
                    sheet.AddSection(label, ["Resizable", "CanResize = false", "CanResize, CanMinimize = false"],
                    [
                        ("macOS layout", [Lights(_ => { }, hover: false), Lights(w => w.CanResize = false, hover: false), Lights(w => (w.CanResize, w.CanMinimize) = (false, false), hover: false)]),
                        ("Hovered", [Lights(_ => { }), Lights(w => w.CanResize = false), Lights(w => (w.CanResize, w.CanMinimize) = (false, false))]),
                        ("Windows layout", [Captions(_ => { }), Captions(w => w.CanResize = false), Captions(w => (w.CanResize, w.CanMinimize) = (false, false))]),
                    ]);
                    sheet.AddSection($"{label}: the green light (hovered)", ["Enter full screen", "Option held: zoom", "In full screen: exit"],
                    [
                        ("", [Lights(_ => { }), OptionLights(sheet, variant, mac), Lights(_ => { }, fullScreen: true)]),
                    ]);
                }
                sheet.Save("fullscreen-final-window-buttons.png");
            }

            using (var sheet = new Sheet("The full screen menu item in the View menu: the Windows layout on macOS (menu row); macOS shows it in the system menu bar"))
            {
                foreach (var (label, variant) in ButtonRenderTests.Themes)
                {
                    Control Open(bool fullScreen)
                    {
                        var w = Catalog(WindowLayoutMode.Windows, variant, fullScreen);
                        if (fullScreen)
                            Reveal(w);
                        return Shot(sheet, w, fullScreen ? Px(0, 0, 560, 400) : Px(24, 16, 560, 400), () =>
                        {
                            var title = w.MenuRow.Titles.First(t => Equals(t.Header, "_View"));
                            title.Focus();
                            title.IsSubMenuOpen = true;
                            MenuRowTests.Pump(w);
                        });
                    }
                    sheet.AddSection(label, ["Windowed", "Full screen, top revealed"], [("", [Open(false), Open(true)])]);
                }
                sheet.Save("fullscreen-final-menu-item.png");
            }
        }
        finally
        {
            CatalogLayout.Set(Application.Current!, WindowLayoutMode.Automatic);
        }
    }

    // Option held over the lights: the pointer rests on the green light with Option down.
    private static Control OptionLights(Sheet sheet, ThemeVariant variant, PixelRect crop)
    {
        var w = Catalog(WindowLayoutMode.MacOS, variant, false);
        return Shot(sheet, w, crop, () =>
        {
            var green = Light(w, WindowButtonKind.Maximize);
            w.MouseMove(Centre(green, w), RawInputModifiers.Alt);
            Dispatcher.UIThread.RunJobs();
        });
    }
}
