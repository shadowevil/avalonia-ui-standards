using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
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
using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>The window: frame, title band, slots, window buttons, drag and resize, and contrast.</summary>
public class StandardsWindowTests
{
    // --- Renders -------------------------------------------------------------------------

    public static TheoryData<string, string, bool, WindowButtonStyle> Renders()
    {
        var data = new TheoryData<string, string, bool, WindowButtonStyle>();
        foreach (var theme in new[] { "Light", "Dark" })
            foreach (var state in new[] { "normal", "maximized" })
            {
                data.Add(theme, state, true, WindowButtonStyle.TrafficLights);
                data.Add(theme, state, false, WindowButtonStyle.TrafficLights);
            }
        data.Add("Light", "normal", true, WindowButtonStyle.CaptionButtons);
        data.Add("Dark", "normal", true, WindowButtonStyle.CaptionButtons);
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Renders))]
    public void Window_Renders(string theme, string state, bool slots, WindowButtonStyle buttons)
    {
        var window = SampleWindow(theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, slots, buttons);
        // Headless frames have no alpha, so a desktop-like grey stands in for what is behind the
        // transparent window, to show the shadow (test only; the library keeps it transparent).
        window.Background = new SolidColorBrush(Color.Parse(theme == "Dark" ? "#3A3D42" : "#C9CDD3"));
        try
        {
            window.SetRenderScaling(Sheet.Scale);
            window.Show();
            window.Activate();
            if (state == "maximized")
                window.WindowState = WindowState.Maximized;
            Pump(window);

            using var frame = Frame.Capture(window);
            var style = buttons == WindowButtonStyle.TrafficLights ? "traffic-lights" : "caption-buttons";
            frame.Save($"window-{theme.ToLowerInvariant()}-{state}-{(slots ? "slots" : "bare")}-{style}.png");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void WindowButtons_RenderStates()
    {
        using var sheet = new Sheet("Window buttons: traffic lights (glyphs show on group hover) and caption buttons");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            var rows = new List<(string, IReadOnlyList<Control>)>
            {
                ("Traffic lights", [
                    TrafficLights(sheet, theme, _ => { }),
                    TrafficLights(sheet, theme, g => SetGlyphs(g)),
                    TrafficLights(sheet, theme, g => { SetGlyphs(g); Pseudo(Lights(g)[0], ":pressed"); }),
                    TrafficLights(sheet, theme, g => Lights(g)[0].IsEnabled = false),
                    TrafficLights(sheet, theme, g => Lights(g)[0].Focus(NavigationMethod.Tab)),
                ]),
            };
            foreach (var (label, kind, maximized) in new[]
                     {
                         ("Caption: minimise", WindowButtonKind.Minimize, false),
                         ("Caption: maximise", WindowButtonKind.Maximize, false),
                         ("Caption: restore", WindowButtonKind.Maximize, true),
                         ("Caption: close", WindowButtonKind.Close, false),
                     })
                rows.Add((label, StateCells.All.Select(s => StateCells.Render(sheet, theme,
                    new CaptionButton { Kind = kind, IsMaximized = maximized, Height = 48 }, s)).ToList()));
            sheet.AddSection(themeLabel, StateCells.All.Select(s => s == CellState.Hover ? "Hover (group)" : s.ToString()).ToList(), rows);
        }
        sheet.Save("window-buttons.png");
    }

    [AvaloniaFact]
    public void WindowButtons_RenderInactiveStates()
    {
        using var sheet = new Sheet("Window buttons while the window is inactive (hover brings back the active look)");
        string[] columns = ["Active", "Inactive", "Inactive, hovered"];
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            var rows = new List<(string, IReadOnlyList<Control>)>
            {
                ("Traffic lights", [
                    TrafficLights(sheet, theme, _ => { }),
                    TrafficLights(sheet, theme, g => g.IsActive = false),
                    TrafficLights(sheet, theme, g => { g.IsActive = false; SetGlyphs(g); }),
                ]),
                ("Caption buttons", [
                    Captions(sheet, theme, _ => { }),
                    Captions(sheet, theme, g => g.IsActive = false),
                    Captions(sheet, theme, g => { g.IsActive = false; Pseudo(g.GetVisualDescendants().OfType<CaptionButton>().Last(), ":pointerover"); }),
                ]),
            };
            sheet.AddSection(themeLabel, columns, rows);
        }
        sheet.Save("window-inactive.png");
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void InactiveWindow_Renders(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var window = SampleWindow(variant, slots: true, WindowButtonStyle.TrafficLights);
        window.Background = new SolidColorBrush(Color.Parse(theme == "Dark" ? "#3A3D42" : "#C9CDD3"));
        try
        {
            window.SetRenderScaling(Sheet.Scale);
            window.Show();
            Pump(window);
            // Renders show end states, so the buttons' fades are removed first (headless animation
            // timing is not dependable).
            foreach (var part in Part<WindowButtons>(window, StandardsWindowTheme.WindowButtonsPart).GetVisualDescendants().OfType<Avalonia.Animation.Animatable>())
                part.Transitions = null;
            // Headless windows are not deactivated by the platform; set IsActive as the platform does
            // (after the activation that showing it queues).
            typeof(WindowBase).GetProperty(nameof(WindowBase.IsActive))!.GetSetMethod(nonPublic: true)!.Invoke(window, [false]);
            Pump(window);
            using var frame = Frame.Capture(window);
            frame.Save($"window-{theme.ToLowerInvariant()}-inactive-slots-traffic-lights.png");
        }
        finally
        {
            window.Close();
        }
    }

    // --- Behaviour -----------------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData(WindowButtonStyle.TrafficLights)]
    [InlineData(WindowButtonStyle.CaptionButtons)]
    public void WindowButtons_MaximiseRestoreMinimiseAndClose(WindowButtonStyle style)
    {
        var window = Show(SampleWindow(ThemeVariant.Light, slots: false, style));
        Button Button(WindowButtonKind kind) => window.GetVisualDescendants().OfType<Button>()
            .Single(b => b is TrafficLightButton t ? t.Kind == kind : b is CaptionButton c && c.Kind == kind);

        Click(Button(WindowButtonKind.Maximize));
        Assert.Equal(WindowState.Maximized, window.WindowState);
        if (style == WindowButtonStyle.CaptionButtons)
            Assert.True(((CaptionButton)Button(WindowButtonKind.Maximize)).IsMaximized);
        Click(Button(WindowButtonKind.Maximize));
        Assert.Equal(WindowState.Normal, window.WindowState);
        Click(Button(WindowButtonKind.Minimize));
        Assert.Equal(WindowState.Minimized, window.WindowState);
        window.WindowState = WindowState.Normal;

        var closed = false;
        window.Closed += (_, _) => closed = true;
        Click(Button(WindowButtonKind.Close));
        Assert.True(closed);
    }

    [AvaloniaFact]
    public void TrafficLights_ShowGlyphsWhileTheGroupIsHovered()
    {
        var window = Show(SampleWindow(ThemeVariant.Light, slots: false, WindowButtonStyle.TrafficLights));
        try
        {
            var lights = window.GetVisualDescendants().OfType<TrafficLightButton>().ToList();
            Assert.All(lights, l => Assert.DoesNotContain(":glyph", l.Classes));

            var minimise = lights[1];
            window.MouseMove(minimise.TranslatePoint(new Point(6, 6), window)!.Value);
            Pump(window);
            Assert.All(lights, l => Assert.Contains(":glyph", l.Classes));

            window.MouseMove(new Point(400, 300));
            Pump(window);
            Assert.All(lights, l => Assert.DoesNotContain(":glyph", l.Classes));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void DoubleClickOnTitleBand_MaximisesAndRestores()
    {
        var window = Show(SampleWindow(ThemeVariant.Light, slots: false, WindowButtonStyle.CaptionButtons));
        try
        {
            var band = Part<Control>(window, StandardsWindowTheme.TitleBandPart);
            var point = band.TranslatePoint(new Point(band.Bounds.Width / 2, band.Bounds.Height / 2), window)!.Value;
            DoubleClick(window, point);
            Assert.Equal(WindowState.Maximized, window.WindowState);

            // Clicks soon after count on (a third, fourth click), so wait out the double-click time first.
            var settings = window.GetPlatformSettings()!;
            Thread.Sleep(settings.GetDoubleTapTime(PointerType.Mouse) + TimeSpan.FromMilliseconds(100));
            DoubleClick(window, point);
            Assert.Equal(WindowState.Normal, window.WindowState);
        }
        finally
        {
            window.Close();
        }
    }

    // Points just inside and outside the frame's edges and corners map to the right resize edge;
    // the title band maps to the drag area; content does not resize.
    [AvaloniaFact]
    public void HitAreas_MapToDragAndResizeEdges()
    {
        var window = Show(SampleWindow(ThemeVariant.Light, slots: true, WindowButtonStyle.TrafficLights));
        try
        {
            var frame = Part<Control>(window, StandardsWindowTheme.FramePart).Bounds;
            var (l, t, r, b) = (frame.Left, frame.Top, frame.Right, frame.Bottom);
            var (cx, cy) = (frame.Center.X, frame.Center.Y);
            (Point Point, string Expected)[] cases =
            [
                (new(l + 1, t + 1), StandardsWindowTheme.ResizePart(WindowEdge.NorthWest)),
                (new(l - 2, t - 2), StandardsWindowTheme.ResizePart(WindowEdge.NorthWest)),
                (new(cx, t + 1), StandardsWindowTheme.ResizePart(WindowEdge.North)),
                (new(r - 1, t + 1), StandardsWindowTheme.ResizePart(WindowEdge.NorthEast)),
                (new(l + 1, cy), StandardsWindowTheme.ResizePart(WindowEdge.West)),
                (new(r - 1, cy), StandardsWindowTheme.ResizePart(WindowEdge.East)),
                (new(r + 2, cy), StandardsWindowTheme.ResizePart(WindowEdge.East)),
                (new(l + 1, b - 1), StandardsWindowTheme.ResizePart(WindowEdge.SouthWest)),
                (new(cx, b - 1), StandardsWindowTheme.ResizePart(WindowEdge.South)),
                (new(r - 1, b - 1), StandardsWindowTheme.ResizePart(WindowEdge.SouthEast)),
                (new(cx, t + 24), StandardsWindowTheme.TitleBandPart),
            ];
            foreach (var (point, expected) in cases)
                Assert.Equal(expected, HitPart(window, point));
            Assert.DoesNotContain("PART_Resize", HitPart(window, new Point(cx, cy)) ?? "");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Maximised_SquaresTheFrameAndDropsShadowAndGrips()
    {
        var window = Show(SampleWindow(ThemeVariant.Light, slots: false, WindowButtonStyle.CaptionButtons));
        try
        {
            var frame = Part<SquircleBorder>(window, StandardsWindowTheme.FramePart);
            Assert.Equal(new CornerRadius(16), frame.CornerRadius);
            Assert.Equal(StandardsWindowTheme.ShadowMargin, frame.Margin);
            Assert.True(Part<Control>(window, StandardsWindowTheme.ShadowPart).IsVisible);

            window.WindowState = WindowState.Maximized;
            Pump(window);
            Assert.Equal(new CornerRadius(0), frame.CornerRadius);
            Assert.Equal(new Thickness(0), frame.Margin);
            Assert.False(Part<Control>(window, StandardsWindowTheme.ShadowPart).IsVisible);
            Assert.False(Part<Control>(window, StandardsWindowTheme.ResizeGripsPart).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Slots_ShowOnlyWhenSet_AndContentSitsInTheGlassPanel()
    {
        var bare = Show(SampleWindow(ThemeVariant.Light, slots: false, WindowButtonStyle.TrafficLights));
        var full = Show(SampleWindow(ThemeVariant.Light, slots: true, WindowButtonStyle.TrafficLights));
        try
        {
            foreach (var part in new[] { StandardsWindowTheme.SidebarPart, StandardsWindowTheme.StatusBarPart })
            {
                Assert.False(Part<Control>(bare, part).IsVisible);
                Assert.True(Part<Control>(full, part).IsVisible);
            }
            var panel = Part<GlassBorder>(full, StandardsWindowTheme.ContentPanelPart);
            Assert.Contains(full.Content as Visual, panel.GetVisualDescendants());
            Assert.Equal(new CornerRadius(16), panel.CornerRadius);
            Assert.Equal(new Thickness(8, 0, 8, 8), panel.Margin);
        }
        finally
        {
            bare.Close();
            full.Close();
        }
    }

    // The status bar mirrors the title band: its text is vertically centred in the band.
    [AvaloniaTheory]
    [InlineData("Normal")]
    [InlineData("Compact")]
    public void StatusText_IsVerticallyCentredInTheBand(string density)
    {
        var window = SampleWindow(ThemeVariant.Light, slots: true, WindowButtonStyle.CaptionButtons);
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced)
        {
            Density = density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal,
        });
        Show(window);
        try
        {
            var band = Part<Control>(window, StandardsWindowTheme.StatusBarPart);
            var text = Assert.IsType<TextBlock>(window.StatusBar);
            var bandCentre = band.TranslatePoint(new Point(0, band.Bounds.Height / 2), window)!.Value.Y;
            var textCentre = text.TranslatePoint(new Point(0, text.Bounds.Height / 2), window)!.Value.Y;
            Assert.True(Math.Abs(textCentre - bandCentre) <= 0.5, $"text centre {textCentre:F2} vs band centre {bandCentre:F2}");
        }
        finally
        {
            window.Close();
        }
    }

    // Any status content is centred: in a grid, a smaller text next to a taller one is centred too.
    [AvaloniaTheory]
    [InlineData("Normal")]
    [InlineData("Compact")]
    public void StatusContent_WithSeveralElements_IsVerticallyCentred(string density)
    {
        var window = SampleWindow(ThemeVariant.Light, slots: true, WindowButtonStyle.CaptionButtons);
        var small = new TextBlock { Text = "Ready", FontSize = 11 };
        var large = new TextBlock { Text = "12 items", FontSize = 15 };
        Grid.SetColumn(large, 1);
        window.StatusBar = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { small, large } };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced)
        {
            Density = density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal,
        });
        Show(window);
        try
        {
            var band = Part<Control>(window, StandardsWindowTheme.StatusBarPart);
            var bandCentre = band.TranslatePoint(new Point(0, band.Bounds.Height / 2), window)!.Value.Y;
            foreach (var text in new[] { small, large })
            {
                // The box hugs its line (a stretched TextBlock would draw its text at the top of a
                // taller box), so the box centre is the text's centre, as in the single-text test.
                Assert.True(text.Bounds.Height <= Math.Ceiling(text.TextLayout.Height), $"{text.Text} is stretched: {text.Bounds.Height:F2} for a {text.TextLayout.Height:F2} line");
                var textCentre = text.TranslatePoint(new Point(0, text.Bounds.Height / 2), window)!.Value.Y;
                Assert.True(Math.Abs(textCentre - bandCentre) <= 0.5, $"{text.Text}: text centre {textCentre:F2} vs band centre {bandCentre:F2}");
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TitleBandAndStatusBar_FollowDensity()
    {
        var window = SampleWindow(ThemeVariant.Light, slots: true, WindowButtonStyle.CaptionButtons);
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        window.Styles.Add(theme);
        Show(window);
        try
        {
            Assert.Equal(48, Part<Control>(window, StandardsWindowTheme.TitleBandPart).Bounds.Height);
            Assert.Equal(32, Part<Control>(window, StandardsWindowTheme.StatusBarPart).Bounds.Height);
            theme.Density = DensityStyle.Compact;
            Pump(window);
            Assert.Equal(40, Part<Control>(window, StandardsWindowTheme.TitleBandPart).Bounds.Height);
            Assert.Equal(24, Part<Control>(window, StandardsWindowTheme.StatusBarPart).Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }

    // Window buttons are not focus or Tab stops, as native ones: Tab skips them and they show no ring.
    [AvaloniaTheory]
    [InlineData(WindowButtonStyle.TrafficLights)]
    [InlineData(WindowButtonStyle.CaptionButtons)]
    public void WindowButtons_AreNotTabStops(WindowButtonStyle style)
    {
        var window = Show(SampleWindow(ThemeVariant.Light, slots: false, style));
        try
        {
            var buttons = window.GetVisualDescendants().OfType<Button>().Where(b => b is TrafficLightButton or CaptionButton).ToList();
            Assert.Equal(3, buttons.Count);
            Assert.All(buttons, b => Assert.False(b.Focusable));
            Assert.All(buttons, b => Assert.False(KeyboardNavigation.GetIsTabStop(b)));
            Assert.All(buttons, b => Assert.False(b.Focus(NavigationMethod.Tab)));

            // Tab moves through the toolbar and content only.
            var visited = new HashSet<IInputElement>();
            for (var i = 0; i < 12; i++)
            {
                window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
                if (window.FocusManager?.GetFocusedElement() is { } focused)
                    visited.Add(focused);
            }
            Assert.NotEmpty(visited);
            Assert.DoesNotContain(visited, e => e is TrafficLightButton or CaptionButton);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(true, Key.W, KeyModifiers.Meta, "closed")]
    [InlineData(true, Key.M, KeyModifiers.Meta, "minimized")]
    [InlineData(true, Key.F4, KeyModifiers.Alt, "none")]
    [InlineData(false, Key.F4, KeyModifiers.Alt, "closed")]
    [InlineData(false, Key.W, KeyModifiers.Meta, "none")]
    [InlineData(false, Key.M, KeyModifiers.Control, "none")]
    public void Shortcuts_FollowTheOsStandard(bool macOS, Key key, KeyModifiers modifiers, string expected)
    {
        var window = Show(SampleWindow(ThemeVariant.Light, slots: false, WindowButtonStyle.Auto));
        var closed = false;
        window.Closed += (_, _) => closed = true;
        try
        {
            var handled = window.HandleShortcut(key, modifiers, macOS);
            Assert.Equal(expected != "none", handled);
            Assert.Equal(expected == "closed", closed);
            Assert.Equal(expected == "minimized", window.WindowState == WindowState.Minimized);
        }
        finally
        {
            if (!closed)
                window.Close();
        }
    }

    [AvaloniaFact]
    public void Shortcut_ClosesThroughTheWindowsKeyHandling()
    {
        var window = Show(SampleWindow(ThemeVariant.Light, slots: false, WindowButtonStyle.Auto));
        var closed = false;
        window.Closed += (_, _) => closed = true;
        if (OperatingSystem.IsMacOS())
            window.KeyPress(Key.W, RawInputModifiers.Meta, PhysicalKey.W, "w");
        else
            window.KeyPress(Key.F4, RawInputModifiers.Alt, PhysicalKey.F4, null);
        Pump(window);
        Assert.True(closed);
    }

    // The caption buttons win over the resize grips where they overlap: the close button's top-right
    // corner (inside the frame's squircle), the right edge beside it, and the top edge over the buttons.
    [AvaloniaFact]
    public void CaptionButtons_WinOverTheResizeGrips()
    {
        var window = Show(SampleWindow(ThemeVariant.Light, slots: false, WindowButtonStyle.CaptionButtons));
        try
        {
            var frame = Part<Control>(window, StandardsWindowTheme.FramePart).Bounds;
            var (t, r) = (frame.Top, frame.Right);
            var w = WindowButtonThemes.CaptionButtonWidth;

            Assert.Equal(WindowButtonKind.Close, HitCaption(window, new Point(r - 7, t + 7)));
            Assert.Equal(WindowButtonKind.Close, HitCaption(window, new Point(r - 2, t + 30)));
            Assert.Equal(WindowButtonKind.Close, HitCaption(window, new Point(r - w / 2, t + 2)));
            Assert.Equal(WindowButtonKind.Minimize, HitCaption(window, new Point(r - 2.5 * w, t + 2)));

            // Outside the frame, and in the clipped corner outside the squircle, the grips still resize.
            Assert.Equal(StandardsWindowTheme.ResizePart(WindowEdge.NorthEast), HitPart(window, new Point(r + 2, t - 2)));
            Assert.Equal(StandardsWindowTheme.ResizePart(WindowEdge.NorthEast), HitPart(window, new Point(r - 1, t + 1)));
            Assert.Equal(StandardsWindowTheme.ResizePart(WindowEdge.East), HitPart(window, new Point(r + 2, t + 30)));
            Assert.Equal(StandardsWindowTheme.ResizePart(WindowEdge.North), HitPart(window, new Point(frame.Left + 100, t + 1)));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InactiveWindow_GreysTrafficLights_AndDimsCaptionGlyphs()
    {
        var lights = new WindowButtons { ButtonStyle = WindowButtonStyle.TrafficLights, IsActive = false };
        var captions = new WindowButtons { ButtonStyle = WindowButtonStyle.CaptionButtons, IsActive = false, Height = 48 };
        var window = Show(new Window { Width = 400, Height = 120, Content = new StackPanel { Children = { lights, captions } } });
        try
        {
            var grey = Effective(Resolve(window, "SystemBaseLowColor"));
            foreach (var light in lights.GetVisualDescendants().OfType<TrafficLightButton>())
            {
                Assert.Equal(grey, Effective(light.Background));
                Assert.Equal(0, Part<Avalonia.Controls.Shapes.Path>(light, WindowButtonThemes.GlyphPart).Opacity);
            }
            var disabled = Effective(Resolve(window, "ButtonForegroundDisabled"));
            Assert.All(captions.GetVisualDescendants().OfType<CaptionButton>(), c => Assert.Equal(disabled, Effective(c.Foreground)));

            // Hovering the group brings the colours and glyphs back, as macOS does.
            foreach (var light in lights.GetVisualDescendants().OfType<TrafficLightButton>())
                light.SetGlyphVisible(true);
            window.UpdateLayout();
            Assert.Equal(WindowButtonThemes.CloseColor, Effective(lights.GetVisualDescendants().OfType<TrafficLightButton>().First().Background));

            lights.IsActive = captions.IsActive = true;
            window.UpdateLayout();
            Assert.All(captions.GetVisualDescendants().OfType<CaptionButton>(), c => Assert.NotEqual(disabled, Effective(c.Foreground)));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Window_ForwardsItsActiveStateToTheButtons()
    {
        var window = Show(SampleWindow(ThemeVariant.Light, slots: false, WindowButtonStyle.TrafficLights));
        try
        {
            window.Activate();
            Pump(window);
            var buttons = Part<WindowButtons>(window, StandardsWindowTheme.WindowButtonsPart);
            Assert.True(window.IsActive);
            Assert.True(buttons.IsActive);

            // Headless windows are not deactivated by the platform, so the OS change is simulated
            // through IsActive's non-public setter, as the platform sets it.
            typeof(WindowBase).GetProperty(nameof(WindowBase.IsActive))!.GetSetMethod(nonPublic: true)!.Invoke(window, [false]);
            Pump(window);
            Assert.False(buttons.IsActive);
            Assert.All(buttons.GetVisualDescendants().OfType<TrafficLightButton>(), l => Assert.Contains(":inactive", l.Classes));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MinimumSize_Is480By320ForTheFrame()
    {
        var window = Show(SampleWindow(ThemeVariant.Light, slots: false, WindowButtonStyle.TrafficLights));
        try
        {
            var margin = StandardsWindowTheme.ShadowMargin;
            Assert.Equal(480 + margin.Left + margin.Right, window.MinWidth);
            Assert.Equal(320 + margin.Top + margin.Bottom, window.MinHeight);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SecondaryText_MeetsAa_AndDisabledTextIsUnchanged()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        Assert.Equal(Color.Parse("#686868"), Resource<Color>(theme, "SystemBaseMediumColor", ThemeVariant.Light));
        Assert.Equal(Color.Parse("#9E9E9E"), Resource<Color>(theme, "SystemBaseMediumColor", ThemeVariant.Dark));
        Assert.Equal(Color.Parse("#A6A6A6"), Resource<Color>(theme, "SystemBaseMediumLowColor", ThemeVariant.Light));
        Assert.Equal(Color.Parse("#6B6B6B"), Resource<Color>(theme, "SystemBaseMediumLowColor", ThemeVariant.Dark));
    }

    // --- Contrast ------------------------------------------------------------------------

    // Primary text and AccentTextColor meet 4.5:1 on the wash's extremes and on the glass panel
    // over them; caption close keeps white on the danger fill; traffic light glyphs reach 3:1.
    [AvaloniaTheory]
    [InlineData("#007CEA")]
    [InlineData("#E0720B")]
    [InlineData("#7A00CC")]
    [InlineData("#00B7C3")]
    public void Text_MeetsAaOnTheWashAndTheGlass(string accent)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Accent = Color.Parse(accent) };
        foreach (var (variant, tint) in new[] { (ThemeVariant.Light, GlassBorder.DefaultTintLight), (ThemeVariant.Dark, GlassBorder.DefaultTintDark) })
        {
            var start = Resource<Color>(theme, "BackdropWashStartColor", variant);
            var end = Resource<Color>(theme, "BackdropWashEndColor", variant);
            Color[] backgrounds = [start, end, StandardsTheme.Mix(start, tint, 0.6), StandardsTheme.Mix(end, tint, 0.6)];
            foreach (var key in new[] { "SystemBaseHighColor", "SystemBaseMediumColor", "AccentTextColor", "AccentTextColorPressed" })
            {
                var text = Resource<Color>(theme, key, variant);
                foreach (var background in backgrounds)
                {
                    var ratio = AccessibleColors.ContrastRatio(text, background);
                    Assert.True(ratio >= 4.5, $"{accent} {variant} {key} {text} on {background}: {ratio:F2}");
                }
            }
        }
    }

    [AvaloniaFact]
    public void TrafficLightGlyphs_ReachNonTextContrast()
    {
        foreach (var fill in new[] { WindowButtonThemes.CloseColor, WindowButtonThemes.MinimizeColor, WindowButtonThemes.ZoomColor })
        {
            var glyph = Over(WindowButtonThemes.GlyphColor, fill);
            Assert.True(AccessibleColors.ContrastRatio(glyph, fill) >= 3, $"{fill}");
        }
        Assert.True(AccessibleColors.ContrastRatio(Colors.White, Color.Parse("#E02F26")) >= 4.5);
    }

    // --- Helpers -------------------------------------------------------------------------

    internal static StandardsWindow SampleWindow(ThemeVariant variant, bool slots, WindowButtonStyle buttons)
    {
        var window = new StandardsWindow
        {
            Title = "Documents",
            Width = 880,
            Height = 560,
            RequestedThemeVariant = variant,
            WindowButtonStyle = buttons,
            Toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
                Children = { new Button { Content = "Share", Classes = { "subtle" } }, new Button { Content = "New", Classes = { "accent" } } },
            },
            Content = SampleContent(),
        };
        if (slots)
        {
            window.Sidebar = new StackPanel
            {
                Width = 180,
                Margin = new Thickness(8, 4, 8, 0),
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = "Library", FontWeight = FontWeight.SemiBold, Margin = new Thickness(8, 4) },
                    new TextBlock { Text = "Recent", Margin = new Thickness(8, 4) },
                    new TextBlock { Text = "Shared", Margin = new Thickness(8, 4) },
                    new HyperlinkButton { Content = "Manage folders" },
                },
            };
            window.StatusBar = new TextBlock { Text = "3 items · Synced just now" };
        }
        return window;
    }

    private static Control SampleContent()
    {
        var card = new SquircleBorder
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = "Quarterly report", FontWeight = FontWeight.SemiBold },
                    new TextBlock { Text = "Cards inside the glass panel are solid.", TextWrapping = TextWrapping.Wrap },
                    new HyperlinkButton { Content = "Open" },
                },
            },
        };
        card.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("SystemChromeMediumLowColor"));
        return new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(4),
            Children =
            {
                new TextBlock { Text = "Documents", FontSize = 20, FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = "Text on the glass panel meets WCAG AA over the wash.", TextWrapping = TextWrapping.Wrap },
                card,
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new Button { Content = "Cancel" }, new Button { Content = "Save", Classes = { "accent" } } } },
            },
        };
    }

    private static T Show<T>(T window) where T : Window
    {
        window.Show();
        Pump(window);
        return window;
    }

    private static void Pump(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static object Resolve(Control control, string key)
    {
        Assert.True(control.TryFindResource(key, control.ActualThemeVariant, out var value), key);
        return value!;
    }

    // The kind of the caption button hit at a point, or null.
    private static WindowButtonKind? HitCaption(Window window, Point point)
    {
        using (window.CaptureRenderedFrame()) { }
        for (var v = window.InputHitTest(point) as Visual; v is not null; v = v.GetVisualParent())
            if (v is CaptionButton caption)
                return caption.Kind;
        return null;
    }


    // The name of the first named window part above the hit element.
    private static string? HitPart(Window window, Point point)
    {
        using (window.CaptureRenderedFrame()) { }
        for (var v = window.InputHitTest(point) as Visual; v is not null; v = v.GetVisualParent())
            if (v is Control { Name: { } name } && name.StartsWith("PART_") && name != "PART_ContentPresenter")
                return name;
        return null;
    }

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void DoubleClick(Window window, Point point)
    {
        for (var i = 0; i < 2; i++)
        {
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
        }
        Pump(window);
    }

    private static void Pseudo(Control control, string pseudoClass) => ((IPseudoClasses)control.Classes).Set(pseudoClass, true);

    private static IReadOnlyList<TrafficLightButton> Lights(WindowButtons group) =>
        group.GetVisualDescendants().OfType<TrafficLightButton>().ToList();

    private static void SetGlyphs(WindowButtons group)
    {
        foreach (var light in Lights(group))
            light.SetGlyphVisible(true);
    }

    private static Control TrafficLights(Sheet sheet, ThemeVariant theme, Action<WindowButtons> state)
    {
        var group = new WindowButtons { ButtonStyle = WindowButtonStyle.TrafficLights, Height = 48, Margin = new Thickness(8, 0) };
        return sheet.Cell(group, theme, afterShow: _ =>
        {
            foreach (var part in group.GetVisualDescendants().OfType<Avalonia.Animation.Animatable>())
                part.Transitions = null;
            state(group);
        });
    }

    private static Control Captions(Sheet sheet, ThemeVariant theme, Action<WindowButtons> state)
    {
        var group = new WindowButtons { ButtonStyle = WindowButtonStyle.CaptionButtons, Height = 48 };
        return sheet.Cell(group, theme, afterShow: _ =>
        {
            foreach (var part in group.GetVisualDescendants().OfType<Avalonia.Animation.Animatable>())
                part.Transitions = null;
            state(group);
        });
    }

    private static T Resource<T>(StandardsTheme theme, string key, ThemeVariant variant)
    {
        Assert.True(theme.TryGetResource(key, variant, out var value), key);
        return Assert.IsType<T>(value);
    }
}
