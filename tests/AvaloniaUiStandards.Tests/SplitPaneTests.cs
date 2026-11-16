using Avalonia;
using Avalonia.Controls;
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
/// The window's inspector and bottom panel (0051): layout A, sizes and clamps, collapsing by drag, the inspector's
/// threshold and overlay, the toggles and menu items, glass sample and region participation, and renders
/// (screenshots/split-panes-*.png).
/// </summary>
public class SplitPaneTests
{
    private static readonly Thickness Margin = StandardsWindowTheme.ShadowMargin;

    // A window whose frame (the visible window) is frameWidth x frameHeight.
    internal static StandardsWindow Show(double frameWidth = 1440, double frameHeight = 900, bool inspector = true, bool bottom = true,
        bool sidebar = false, ThemeVariant? theme = null, Action<StandardsWindow>? before = null, object? inspectorContent = null,
        object? bottomContent = null, object? content = null, bool reduceMotion = true)
    {
        var window = new StandardsWindow(nativeFrame: false)
        {
            Title = "Panes",
            Width = frameWidth + Margin.Left + Margin.Right,
            Height = frameHeight + Margin.Top + Margin.Bottom,
            LayoutMode = WindowLayoutMode.MacOS,
            Sidebar = sidebar ? SidebarTests.NewSidebar() : null,
            Content = content ?? new TextBlock { Text = "Content" },
            Inspector = inspector ? inspectorContent ?? new TextBlock { Text = "Inspector" } : null,
            BottomPanel = bottom ? bottomContent ?? new TextBlock { Text = "Bottom panel" } : null,
            RequestedThemeVariant = theme ?? ThemeVariant.Light,
            Background = new SolidColorBrush(Color.Parse(theme == ThemeVariant.Dark ? "#3A3D42" : "#C9CDD3")),
        };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = reduceMotion });
        before?.Invoke(window);
        window.Show();
        window.Activate();
        Pump(window);
        return window;
    }

    private static Rect InLayers(StandardsWindow window, Visual part) =>
        new(part.TranslatePoint(default, Layers(window))!.Value, part.Bounds.Size);

    private static Visual Layers(StandardsWindow window) => Part<SquircleBorder>(window, StandardsWindowTheme.FramePart).Child!;

    private static GlassBorder ContentPanel(Window w) => Part<GlassBorder>(w, StandardsWindowTheme.ContentPanelPart);

    private static GlassBorder Inspector(Window w) => Part<GlassBorder>(w, StandardsWindowTheme.InspectorPart);

    private static GlassBorder Bottom(Window w) => Part<GlassBorder>(w, StandardsWindowTheme.BottomPanelPart);

    private static PaneSplitter InspectorSplitter(Window w) => Part<PaneSplitter>(w, StandardsWindowTheme.InspectorSplitterPart);

    private static PaneSplitter BottomSplitter(Window w) => Part<PaneSplitter>(w, StandardsWindowTheme.BottomPanelSplitterPart);

    // --- Layout ---------------------------------------------------------------------------------

    [AvaloniaFact]
    public void WithoutSlots_TheLayoutIsTheContentPanelAlone()
    {
        var bare = Show(inspector: false, bottom: false);
        var empty = Show(inspector: false, bottom: false, before: w => { w.IsInspectorOpen = true; w.IsBottomPanelOpen = true; });
        try
        {
            foreach (var window in new[] { bare, empty })
            {
                Assert.False(Inspector(window).IsVisible);
                Assert.False(Bottom(window).IsVisible);
                Assert.False(InspectorSplitter(window).IsVisible);
                Assert.False(BottomSplitter(window).IsVisible);
                var panel = InLayers(window, ContentPanel(window));
                var panes = InLayers(window, Part<Control>(window, StandardsWindowTheme.PanesPart));
                Assert.Equal(panes, panel);
                Assert.Equal(new Thickness(Spacing.S, 0, Spacing.S, Spacing.S), Part<Control>(window, StandardsWindowTheme.PanesPart).Margin);
                Assert.Equal(new Rect(panel.Position, panel.Size), window.ContentPanelBounds);
            }
        }
        finally
        {
            bare.Close();
            empty.Close();
        }
    }

    [AvaloniaFact]
    public void Inspector_IsAGlassPanelRightOfTheContent_FullHeight_WithTheHandleInTheGap()
    {
        var window = Show(bottom: false);
        try
        {
            var panel = InLayers(window, ContentPanel(window));
            var inspector = InLayers(window, Inspector(window));
            var splitter = InLayers(window, InspectorSplitter(window));
            var panes = InLayers(window, Part<Control>(window, StandardsWindowTheme.PanesPart));
            Assert.Equal(new CornerRadius(16), Inspector(window).CornerRadius);
            Assert.Equal(StandardsWindow.InspectorDefaultWidth, inspector.Width);
            Assert.Equal(panel.Right + Spacing.S, inspector.X);
            Assert.Equal(panes.Right, inspector.Right);
            Assert.Equal(panes.Top, inspector.Top);
            Assert.Equal(panes.Bottom, inspector.Bottom);
            Assert.Equal(panel.Height, inspector.Height);
            Assert.Equal(new Rect(panel.Right, panes.Top, Spacing.S, panes.Height), splitter);
            Assert.True(InspectorSplitter(window).IsVisible);
            Assert.IsAssignableFrom<GridSplitter>(InspectorSplitter(window));
            Assert.Equal(GridResizeDirection.Columns, InspectorSplitter(window).ResizeDirection);
            // The content sits in the panel, padded by SpacingM.
            var presenter = Part<Control>(window, StandardsWindowTheme.InspectorPresenterPart);
            Assert.Contains(window.Inspector as Visual, presenter.GetVisualDescendants());
            Assert.Equal(new Thickness(Spacing.M), ((Avalonia.Controls.Presenters.ContentPresenter)presenter).Padding);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BottomPanel_IsDirectlyUnderTheContentPanelOnly_AndTheInspectorStaysFullHeight()
    {
        var window = Show();
        try
        {
            var panel = InLayers(window, ContentPanel(window));
            var bottom = InLayers(window, Bottom(window));
            var inspector = InLayers(window, Inspector(window));
            var splitter = InLayers(window, BottomSplitter(window));
            var panes = InLayers(window, Part<Control>(window, StandardsWindowTheme.PanesPart));
            Assert.Equal(new CornerRadius(16), Bottom(window).CornerRadius);
            Assert.Equal(StandardsWindow.BottomPanelDefaultHeight, bottom.Height);
            Assert.Equal(panel.X, bottom.X);
            Assert.Equal(panel.Width, bottom.Width);
            Assert.Equal(panel.Bottom + Spacing.S, bottom.Y);
            Assert.Equal(panes.Bottom, bottom.Bottom);
            Assert.Equal(new Rect(panel.X, panel.Bottom, panel.Width, Spacing.S), splitter);
            Assert.Equal(GridResizeDirection.Rows, BottomSplitter(window).ResizeDirection);
            Assert.Equal(panes.Top, inspector.Top);
            Assert.Equal(panes.Bottom, inspector.Bottom);
            Assert.Equal(bottom.Right + Spacing.S, inspector.X);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ClosingAPanel_GivesItsRoomBackToTheContentPanel()
    {
        var window = Show();
        try
        {
            window.IsInspectorOpen = false;
            window.IsBottomPanelOpen = false;
            Pump(window);
            Assert.False(Inspector(window).IsVisible);
            Assert.False(Bottom(window).IsVisible);
            Assert.Equal(InLayers(window, Part<Control>(window, StandardsWindowTheme.PanesPart)), InLayers(window, ContentPanel(window)));
        }
        finally
        {
            window.Close();
        }
    }

    // --- Sizes ----------------------------------------------------------------------------------

    [AvaloniaFact]
    public void Sizes_AreKeptWithinTheirLimits()
    {
        var window = Show();
        try
        {
            window.InspectorWidth = 1000;
            Assert.Equal(StandardsWindow.InspectorMaxWidth, window.InspectorWidth);
            window.InspectorWidth = 50;
            Assert.Equal(StandardsWindow.InspectorMinWidth, window.InspectorWidth);
            window.InspectorWidth = 320;
            window.BottomPanelHeight = 10;
            Assert.Equal(StandardsWindow.BottomPanelMinHeight, window.BottomPanelHeight);
            Pump(window);
            Assert.Equal(320, Inspector(window).Bounds.Width);
            Assert.Equal(StandardsWindow.BottomPanelMinHeight, Bottom(window).Bounds.Height);

            // The bottom panel shows at most half the content area's height; the asked height is kept for a taller window.
            window.BottomPanelHeight = 2000;
            Pump(window);
            var area = Part<Control>(window, StandardsWindowTheme.PanesPart).Bounds.Height;
            Assert.Equal(2000, window.BottomPanelHeight);
            Assert.Equal((area - PaneLayout.Gap) / 2, Bottom(window).Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Properties_BindTwoWay_SoAppsCanKeepThem()
    {
        foreach (var property in new AvaloniaProperty[]
                 {
                     StandardsWindow.IsInspectorOpenProperty, StandardsWindow.InspectorWidthProperty,
                     StandardsWindow.IsBottomPanelOpenProperty, StandardsWindow.BottomPanelHeightProperty,
                 })
            Assert.Equal(Avalonia.Data.BindingMode.TwoWay, property.GetMetadata(typeof(StandardsWindow)).DefaultBindingMode);
        var defaults = new StandardsWindow();
        Assert.True(defaults.IsInspectorOpen);
        Assert.True(defaults.IsBottomPanelOpen);
        Assert.Equal(240, defaults.InspectorWidth);
        Assert.Equal(200, defaults.BottomPanelHeight);
        Assert.Equal(1248, StandardsWindow.InspectorThreshold);
    }

    // --- Dragging -------------------------------------------------------------------------------

    private static Point Centre(Visual part, Window window) =>
        part.TranslatePoint(new Point(part.Bounds.Width / 2, part.Bounds.Height / 2), window)!.Value;

    [AvaloniaFact]
    public void DraggingTheInspectorHandle_Resizes_UpToTheMaximum_AndBelowTheMinimumCollapses()
    {
        var window = Show(bottom: false);
        try
        {
            var start = Centre(InspectorSplitter(window), window);
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(start - new Point(60, 0));
            Pump(window);
            Assert.Equal(300, window.InspectorWidth);
            Assert.Equal(300, Inspector(window).Bounds.Width);
            window.MouseMove(start - new Point(400, 0));
            Pump(window);
            Assert.Equal(StandardsWindow.InspectorMaxWidth, window.InspectorWidth);
            // At the minimum it stays open; below it, it collapses.
            window.MouseMove(start + new Point(40, 0));
            Pump(window);
            Assert.Equal(StandardsWindow.InspectorMinWidth, window.InspectorWidth);
            Assert.True(window.IsInspectorOpen);
            window.MouseMove(start + new Point(41, 0));
            Pump(window);
            Assert.False(window.IsInspectorOpen);
            Assert.False(Inspector(window).IsVisible);
            Assert.False(InspectorSplitter(window).IsDragging);
            window.MouseUp(start + new Point(41, 0), MouseButton.Left);
            Pump(window);

            // Reopened, it comes back at the last width it had.
            window.ToggleInspector();
            Pump(window);
            Assert.Equal(StandardsWindow.InspectorMinWidth, Inspector(window).Bounds.Width);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void DraggingTheBottomHandle_Resizes_UpToHalfTheArea_AndBelowTheMinimumCollapses()
    {
        var window = Show();
        try
        {
            var area = Part<Control>(window, StandardsWindowTheme.PanesPart).Bounds.Height;
            var start = Centre(BottomSplitter(window), window);
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(start - new Point(0, 50));
            Pump(window);
            Assert.Equal(250, window.BottomPanelHeight);
            window.MouseMove(start - new Point(0, 2000));
            Pump(window);
            Assert.Equal((area - PaneLayout.Gap) / 2, window.BottomPanelHeight);
            window.MouseMove(start + new Point(0, 200 - StandardsWindow.BottomPanelMinHeight + 1));
            Pump(window);
            Assert.False(window.IsBottomPanelOpen);
            window.MouseUp(start, MouseButton.Left);
            Pump(window);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheArrowKeys_StepTheSize_WithoutCollapsing_AndEscapeCancelsADrag()
    {
        var window = Show();
        try
        {
            var splitter = InspectorSplitter(window);
            splitter.Focus(NavigationMethod.Tab);
            MenuRowTests.Press(window, Key.Left);
            Assert.Equal(240 + splitter.KeyboardIncrement, window.InspectorWidth);
            window.InspectorWidth = StandardsWindow.InspectorMinWidth;
            MenuRowTests.Press(window, Key.Right);
            Assert.Equal(StandardsWindow.InspectorMinWidth, window.InspectorWidth);
            Assert.True(window.IsInspectorOpen);

            var bottom = BottomSplitter(window);
            bottom.Focus(NavigationMethod.Tab);
            MenuRowTests.Press(window, Key.Up);
            Assert.Equal(200 + bottom.KeyboardIncrement, window.BottomPanelHeight);

            var start = Centre(splitter, window);
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(start - new Point(80, 0));
            Pump(window);
            Assert.Equal(280, window.InspectorWidth);
            splitter.Focus();
            MenuRowTests.Press(window, Key.Escape);
            Assert.Equal(200, window.InspectorWidth);
            window.MouseUp(start, MouseButton.Left);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Threshold and overlay ------------------------------------------------------------------

    [AvaloniaFact]
    public void TheInspectorDocksFrom1248_AndHidesFromTheLayoutBelow()
    {
        var window = Show(frameWidth: 1248, bottom: false);
        try
        {
            Assert.Equal(InspectorPlacement.Docked, window.InspectorPlacement);
            window.Width = 1247 + Margin.Left + Margin.Right;
            Pump(window);
            Assert.True(window.IsInspectorNarrow);
            Assert.Equal(InspectorPlacement.None, window.InspectorPlacement);
            Assert.False(Inspector(window).IsVisible);
            Assert.True(window.IsInspectorOpen);
            Assert.Equal(InLayers(window, Part<Control>(window, StandardsWindowTheme.PanesPart)), InLayers(window, ContentPanel(window)));
            window.Width = 1248 + Margin.Left + Margin.Right;
            Pump(window);
            Assert.Equal(InspectorPlacement.Docked, window.InspectorPlacement);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InANarrowWindow_TheToggleOpensTheOverlayAtTheRightEdge_OverTheContent()
    {
        var window = Show(frameWidth: 900);
        try
        {
            var panes = InLayers(window, Part<Control>(window, StandardsWindowTheme.PanesPart));
            var before = InLayers(window, ContentPanel(window));
            window.ToggleInspector();
            Pump(window);
            Assert.True(window.IsInspectorOverlayOpen);
            Assert.Equal(InspectorPlacement.Overlay, window.InspectorPlacement);
            var inspector = InLayers(window, Inspector(window));
            Assert.Equal(new Rect(panes.Right - 240, panes.Top, 240, panes.Height), inspector);
            Assert.Equal(1, Inspector(window).ZIndex);
            Assert.False(InspectorSplitter(window).IsVisible);
            // The content does not reflow under it.
            Assert.Equal(before, InLayers(window, ContentPanel(window)));
            Assert.True(window.IsInspectorOpen);

            window.ToggleInspector();
            Pump(window);
            Assert.False(window.IsInspectorOverlayOpen);
            Assert.False(Inspector(window).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheOverlay_ClosesOnEscape_AndOnAClickOutside_WithoutPassingItOn()
    {
        var clicks = 0;
        var button = new Button { Content = "Content button" };
        button.Click += (_, _) => clicks++;
        var inside = new Button { Content = "Inside" };
        var window = Show(frameWidth: 900, bottom: false, content: button, inspectorContent: inside);
        try
        {
            window.ToggleInspector();
            Pump(window);
            inside.Focus(NavigationMethod.Tab);
            MenuRowTests.Press(window, Key.Escape);
            Assert.False(window.IsInspectorOverlayOpen);

            window.ToggleInspector();
            Pump(window);
            // A click inside keeps it.
            var inPoint = Centre(inside, window);
            window.MouseDown(inPoint, MouseButton.Left);
            window.MouseUp(inPoint, MouseButton.Left);
            Pump(window);
            Assert.True(window.IsInspectorOverlayOpen);

            var point = Centre(button, window);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Pump(window);
            Assert.False(window.IsInspectorOverlayOpen);
            Assert.Equal(0, clicks);
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
    public void TheOverlay_ClosesWhenTheWindowCrossesTheThreshold_AndFocusInItGoesBack()
    {
        var before = new Button { Content = "Before" };
        var inside = new Button { Content = "Inside" };
        var window = Show(frameWidth: 900, bottom: false, content: before, inspectorContent: inside);
        try
        {
            before.Focus(NavigationMethod.Tab);
            window.ToggleInspector();
            Pump(window);
            inside.Focus(NavigationMethod.Tab);
            window.Width = 1300 + Margin.Left + Margin.Right;
            Pump(window);
            Assert.False(window.IsInspectorOverlayOpen);
            Assert.Equal(InspectorPlacement.Docked, window.InspectorPlacement);
            Assert.True(before.IsFocused);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Toggles and menu items -----------------------------------------------------------------

    [AvaloniaFact]
    public void Toggles_FlipTheOpenFlags_AndDoNothingWithoutContent()
    {
        var window = Show();
        var empty = Show(inspector: false, bottom: false);
        try
        {
            window.ToggleInspector();
            window.ToggleBottomPanel();
            Assert.False(window.IsInspectorOpen);
            Assert.False(window.IsBottomPanelOpen);
            window.ToggleInspector();
            window.ToggleBottomPanel();
            Assert.True(window.IsInspectorOpen);
            Assert.True(window.IsBottomPanelOpen);

            empty.ToggleInspector();
            empty.ToggleBottomPanel();
            Assert.True(empty.IsInspectorOpen);
            Assert.True(empty.IsBottomPanelOpen);
        }
        finally
        {
            window.Close();
            empty.Close();
        }
    }

    private static (InspectorMenuItem Inspector, BottomPanelMenuItem Bottom) AddItems(StandardsWindow window)
    {
        var inspector = new InspectorMenuItem();
        var bottom = new BottomPanelMenuItem();
        // At any depth: in a submenu of View.
        NativeMenu.SetMenu(window, new NativeMenu
        {
            new NativeMenuItem("_View") { Menu = new NativeMenu { new NativeMenuItem("_Panels") { Menu = new NativeMenu { inspector, bottom } } } },
        });
        return (inspector, bottom);
    }

    private static void Choose(NativeMenuItem item) => ((INativeMenuItemExporterEventsImplBridge)item).RaiseClicked();

    [AvaloniaFact]
    public void MenuItems_ToggleThePanels_AndTheirHeadersFollow()
    {
        InspectorMenuItem inspector = null!;
        BottomPanelMenuItem bottom = null!;
        var window = Show(before: w => (inspector, bottom) = AddItems(w));
        try
        {
            Assert.Same(window, inspector.Window);
            Assert.Same(window, bottom.Window);
            Assert.True(inspector.IsEnabled);
            Assert.True(bottom.IsEnabled);
            Assert.Equal("Hide Inspector", inspector.Header);
            Assert.Equal("Hide Bottom Panel", bottom.Header);
            Choose(inspector);
            Choose(bottom);
            Assert.False(window.IsInspectorOpen);
            Assert.False(window.IsBottomPanelOpen);
            Assert.Equal("Show Inspector", inspector.Header);
            Assert.Equal("Show Bottom Panel", bottom.Header);

            // Without content, disabled.
            window.Inspector = null;
            window.BottomPanel = null;
            Assert.False(inspector.IsEnabled);
            Assert.False(bottom.IsEnabled);
        }
        finally
        {
            window.Close();
        }
        Assert.Null(inspector.Window);
    }

    [AvaloniaFact]
    public void TheInspectorItem_FollowsTheOverlay_InANarrowWindow()
    {
        InspectorMenuItem inspector = null!;
        var window = Show(frameWidth: 900, before: w => inspector = AddItems(w).Inspector);
        try
        {
            Assert.Equal("Show Inspector", inspector.Header);
            Choose(inspector);
            Pump(window);
            Assert.True(window.IsInspectorOverlayOpen);
            Assert.Equal("Hide Inspector", inspector.Header);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MenuItems_AreDisabledWithoutAWindow()
    {
        Assert.False(new InspectorMenuItem().IsEnabled);
        Assert.False(new BottomPanelMenuItem().IsEnabled);
        Assert.Equal("Show Inspector", new InspectorMenuItem().Header);
    }

    [AvaloniaFact]
    public void MenuItems_HaveTheGesturesOfTheLayout()
    {
        InspectorMenuItem inspector = null!;
        BottomPanelMenuItem bottom = null!;
        var window = Show(before: w => (inspector, bottom) = AddItems(w));
        try
        {
            Assert.Equal(new KeyGesture(Key.I, KeyModifiers.Alt | KeyModifiers.Meta), inspector.Gesture);
            Assert.Equal(new KeyGesture(Key.J, KeyModifiers.Alt | KeyModifiers.Meta), bottom.Gesture);
            window.LayoutMode = WindowLayoutMode.Windows;
            Pump(window);
            Assert.Equal(new KeyGesture(Key.I, KeyModifiers.Control | KeyModifiers.Alt), inspector.Gesture);
            Assert.Equal(new KeyGesture(Key.J, KeyModifiers.Control), bottom.Gesture);
            Assert.Contains(new KeyGesture(Key.J, KeyModifiers.Control), window.MenuShortcuts.Gestures);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(WindowLayoutMode.Windows)]
    [InlineData(WindowLayoutMode.MacOS)]
    public void TheShortcuts_WorkThroughTheMenu(WindowLayoutMode layout)
    {
        var window = Show(before: w =>
        {
            AddItems(w);
            w.LayoutMode = layout;
        });
        try
        {
            if (layout == WindowLayoutMode.Windows)
            {
                MenuRowTests.Press(window, Key.J, RawInputModifiers.Control, "j", PhysicalKey.J);
                Assert.False(window.IsBottomPanelOpen);
                MenuRowTests.Press(window, Key.I, RawInputModifiers.Control | RawInputModifiers.Alt, "i", PhysicalKey.I);
                Assert.False(window.IsInspectorOpen);
                // The menu row shows the items with their gestures.
                Assert.True(window.MenuRow.IsVisible);
            }
            else
            {
                MenuRowTests.Press(window, Key.J, RawInputModifiers.Alt | RawInputModifiers.Meta, "j", PhysicalKey.J);
                Assert.False(window.IsBottomPanelOpen);
                MenuRowTests.Press(window, Key.I, RawInputModifiers.Alt | RawInputModifiers.Meta, "i", PhysicalKey.I);
                Assert.False(window.IsInspectorOpen);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MenuItemHeaders_ComeFromResources()
    {
        InspectorMenuItem inspector = null!;
        BottomPanelMenuItem bottom = null!;
        var window = Show(before: w =>
        {
            w.Resources[InspectorMenuItem.ShowHeaderKey] = "Afficher l'inspecteur";
            w.Resources[InspectorMenuItem.HideHeaderKey] = "Masquer l'inspecteur";
            w.Resources[BottomPanelMenuItem.ShowHeaderKey] = "Afficher le panneau";
            w.Resources[BottomPanelMenuItem.HideHeaderKey] = "Masquer le panneau";
            (inspector, bottom) = AddItems(w);
        });
        try
        {
            Assert.Equal("Masquer l'inspecteur", inspector.Header);
            Assert.Equal("Masquer le panneau", bottom.Header);
            window.ToggleInspector();
            window.ToggleBottomPanel();
            Assert.Equal("Afficher l'inspecteur", inspector.Header);
            Assert.Equal("Afficher le panneau", bottom.Header);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheDefaultHeaders_AreThemeResources()
    {
        var window = Show();
        try
        {
            Assert.Equal("Show Inspector", window.FindResource(InspectorMenuItem.ShowHeaderKey));
            Assert.Equal("Hide Inspector", window.FindResource(InspectorMenuItem.HideHeaderKey));
            Assert.Equal("Show Bottom Panel", window.FindResource(BottomPanelMenuItem.ShowHeaderKey));
            Assert.Equal("Hide Bottom Panel", window.FindResource(BottomPanelMenuItem.HideHeaderKey));
        }
        finally
        {
            window.Close();
        }
    }

    // --- Glass ----------------------------------------------------------------------------------

    [AvaloniaFact]
    public void ThePanels_ReadTheSharedSample_LikeTheContentPanel_AndHiddenOnesDoNot()
    {
        var window = Show();
        try
        {
            var sampler = GlassSharedBackdropTests.Sampler(window);
            foreach (var glass in new[] { ContentPanel(window), Inspector(window), Bottom(window) })
            {
                Assert.True(glass.ReadsSharedSample);
                Assert.Same(sampler, glass.Sampler);
                Assert.Contains(sampler.Consumers, c => c.Glass == glass);
            }

            window.IsInspectorOpen = false;
            window.IsBottomPanelOpen = false;
            Pump(window);
            Assert.DoesNotContain(sampler.Consumers, c => c.Glass == Inspector(window));
            Assert.DoesNotContain(sampler.Consumers, c => c.Glass == Bottom(window));
            Assert.Contains(sampler.Consumers, c => c.Glass == ContentPanel(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ThePanels_TakePartInTheVisibleRegion()
    {
        var doc = new Border { Width = 160, Height = 120, Background = Brushes.White };
        OpaqueRegion.SetIsOpaque(doc, true);
        var window = Show(inspectorContent: doc);
        var solid = Show(before: w => w.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { GlassQuality = GlassQuality.Solid, ReduceMotion = true }));
        try
        {
            // Opaque content in the inspector is cut out of the backdrop, and skipped by the inspector's fill.
            var rect = InLayers(window, doc);
            Assert.False(window.BackdropVisibleRegion.Contains(rect.Center));
            Assert.NotEmpty(Inspector(window).Skip);

            // Solid glass panels are opaque: the backdrop under them is cut out.
            foreach (var glass in new[] { Inspector(solid), Bottom(solid) })
                Assert.False(solid.BackdropVisibleRegion.Contains(InLayers(solid, glass).Center));
            Assert.True(solid.BackdropVisibleRegion.Contains(new Point(4, 4)));
        }
        finally
        {
            window.Close();
            solid.Close();
        }
    }

    // --- Short windows --------------------------------------------------------------------------

    // A toolbar band and a status bar, so the content area can be shorter than 200 above the minimum window size.
    private static void ShortChrome(StandardsWindow window)
    {
        window.ToolbarBand = new ToolbarBand { Items = { new Button { Content = "Bold" } } };
        window.StatusBar = new TextBlock { Text = "Ready" };
    }

    // Resizes the window so the content area is exactly areaHeight tall.
    private static void SetAreaHeight(StandardsWindow window, double areaHeight)
    {
        var area = Part<Control>(window, StandardsWindowTheme.PanesPart);
        window.Height += areaHeight - area.Bounds.Height;
        Pump(window);
        Assert.Equal(areaHeight, area.Bounds.Height);
    }

    [AvaloniaFact]
    public void TheBottomPanel_ShowsAtExactlyTwiceItsMinimumPlusTheGap_WithTheContentPanelAsTall()
    {
        var window = Show(bottom: true, inspector: false, before: ShortChrome);
        try
        {
            SetAreaHeight(window, PaneLayout.BottomRoom);
            Assert.True(Bottom(window).IsVisible);
            Assert.Equal(StandardsWindow.BottomPanelMinHeight, Bottom(window).Bounds.Height);
            Assert.Equal(StandardsWindow.BottomPanelMinHeight, ContentPanel(window).Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AShortWindow_HidesTheBottomPanelFromTheLayout_AndItReturnsWhenTheWindowGrows()
    {
        BottomPanelMenuItem item = null!;
        var window = Show(inspector: false, before: w =>
        {
            ShortChrome(w);
            item = AddItems(w).Bottom;
        });
        try
        {
            SetAreaHeight(window, PaneLayout.BottomRoom - 1);
            Assert.False(Bottom(window).IsVisible);
            Assert.False(BottomSplitter(window).IsVisible);
            Assert.True(window.IsBottomPanelOpen);
            Assert.False(window.IsBottomPanelShown);
            Assert.Equal("Show Bottom Panel", item.Header);
            Assert.Equal(InLayers(window, Part<Control>(window, StandardsWindowTheme.PanesPart)), InLayers(window, ContentPanel(window)));
            Assert.DoesNotContain(GlassSharedBackdropTests.Sampler(window).Consumers, c => c.Glass == Bottom(window));

            SetAreaHeight(window, 500);
            Assert.True(Bottom(window).IsVisible);
            Assert.Equal(StandardsWindow.BottomPanelDefaultHeight, Bottom(window).Bounds.Height);
            Assert.Equal("Hide Bottom Panel", item.Header);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(WindowLayoutMode.MacOS)]
    [InlineData(WindowLayoutMode.Windows)]
    public void InAShortWindow_TheToggleAndItsItemAndGesture_DoNothing_SoThePanelReturns(WindowLayoutMode layout)
    {
        BottomPanelMenuItem item = null!;
        var window = Show(inspector: false, before: w =>
        {
            ShortChrome(w);
            item = AddItems(w).Bottom;
            w.LayoutMode = layout;
        });
        try
        {
            SetAreaHeight(window, PaneLayout.BottomRoom - 1);
            Assert.False(item.IsEnabled);
            Assert.Equal("Show Bottom Panel", item.Header);
            window.ToggleBottomPanel();
            Choose(item);
            if (layout == WindowLayoutMode.Windows)
                MenuRowTests.Press(window, Key.J, RawInputModifiers.Control, "j", PhysicalKey.J);
            else
                MenuRowTests.Press(window, Key.J, RawInputModifiers.Alt | RawInputModifiers.Meta, "j", PhysicalKey.J);
            Assert.True(window.IsBottomPanelOpen);

            SetAreaHeight(window, 500);
            Assert.True(item.IsEnabled);
            Assert.True(Bottom(window).IsVisible);
            Assert.Equal("Hide Bottom Panel", item.Header);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheContentPanel_IsAlwaysAtLeastAsTallAsTheBottomPanel()
    {
        var window = Show(inspector: false, before: w =>
        {
            ShortChrome(w);
            w.BottomPanelHeight = 5000;
        });
        try
        {
            foreach (var area in new[] { PaneLayout.BottomRoom, 201.0, 333, 700 })
            {
                SetAreaHeight(window, area);
                Assert.True(ContentPanel(window).Bounds.Height >= Bottom(window).Bounds.Height, $"area {area}");
                // Within layout rounding (an odd area splits on a half pixel).
                Assert.Equal((area - PaneLayout.Gap) / 2, Bottom(window).Bounds.Height, 0.5);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EscapeDuringABottomDrag_PutsBackTheAskedHeight()
    {
        var window = Show(inspector: false, before: w => w.BottomPanelHeight = 5000);
        try
        {
            var shown = Bottom(window).Bounds.Height;
            Assert.True(shown < 5000);
            var splitter = BottomSplitter(window);
            var start = Centre(splitter, window);
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(start + new Point(0, 40));
            Pump(window);
            Assert.Equal(shown - 40, window.BottomPanelHeight);
            splitter.Focus();
            MenuRowTests.Press(window, Key.Escape);
            Assert.Equal(5000, window.BottomPanelHeight);
            window.MouseUp(start, MouseButton.Left);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Overlay focus and popups ---------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void OpeningTheOverlay_FocusesTheFirstFocusableControlInIt(bool reduceMotion)
    {
        var before = new Button { Content = "Before" };
        var first = new Button { Content = "First" };
        var inspector = new StackPanel { Children = { new TextBlock { Text = "Label" }, first, new Button { Content = "Second" } } };
        var window = Show(frameWidth: 900, bottom: false, content: before, inspectorContent: inspector, reduceMotion: reduceMotion);
        try
        {
            before.Focus(NavigationMethod.Tab);
            window.ToggleInspector();
            Pump(window);
            Assert.True(first.IsFocused);
            MenuRowTests.Press(window, Key.Escape);
            Assert.True(before.IsFocused);
        }
        finally
        {
            window.Close();
        }
    }

    // A fresh PanelStack given to a narrow window has never been laid out (the hidden inspector is not measured): focus
    // follows once layout has created the cards. The window's first layout pass, before it knows its width, docks the
    // inspector, so the stack is set once the window is open.
    [AvaloniaFact]
    public void OpeningTheOverlay_InAFreshNarrowWindow_FocusesTheFirstCardsHeader()
    {
        var stack = NewPanelStack();
        var window = Show(frameWidth: 900, bottom: false);
        try
        {
            window.Inspector = stack;
            Pump(window);
            // The cards have no template yet: there is nothing to focus when the overlay opens.
            var first = Assert.IsType<PanelStackItem>(stack.Items[0]);
            Assert.Empty(first.GetVisualDescendants());
            window.ToggleInspector();
            Assert.Empty(first.GetVisualDescendants());
            Pump(window);
            var header = first.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.ToggleButton>().First(t => t.Name == ExpanderTheme.HeaderPart);
            Assert.True(header.IsFocused);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Escape_ClosesAnOpenDropDownInTheOverlayFirst()
    {
        var combo = new ComboBox { Items = { "Normal", "Multiply", "Screen" }, Width = 180 };
        var window = Show(frameWidth: 900, bottom: false, inspectorContent: combo);
        try
        {
            window.ToggleInspector();
            Pump(window);
            Assert.True(combo.IsFocused);
            combo.IsDropDownOpen = true;
            Pump(window);
            Assert.Equal(-1, combo.SelectedIndex);
            MenuRowTests.Press(window, Key.Escape);
            Assert.False(combo.IsDropDownOpen);
            Assert.True(window.IsInspectorOverlayOpen);
            MenuRowTests.Press(window, Key.Escape);
            Assert.False(window.IsInspectorOverlayOpen);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Motion ---------------------------------------------------------------------------------

    private static void Elapse(Window window, TimeSpan time)
    {
        for (var elapsed = TimeSpan.Zero; elapsed < time; elapsed += TimeSpan.FromMilliseconds(40))
        {
            Thread.Sleep(40);
            Pump(window);
        }
    }

    private static double Reveal(StandardsWindow w, StyledProperty<double> property) =>
        Part<PaneLayout>(w, StandardsWindowTheme.PanesPart).GetValue(property);

    private static (Rect Content, Rect Inspector, Rect Bottom) Layout(StandardsWindow w) =>
        (InLayers(w, ContentPanel(w)), Inspector(w).IsVisible ? InLayers(w, Inspector(w)) : default, Bottom(w).IsVisible ? InLayers(w, Bottom(w)) : default);

    [AvaloniaFact]
    public void ShowingAndHiding_Slides_ResizingDoesNot_AndEndsWhereReduceMotionDoes()
    {
        var moving = Show(reduceMotion: false);
        var reduced = Show(reduceMotion: true);
        try
        {
            var panes = Part<PaneLayout>(moving, StandardsWindowTheme.PanesPart);
            Assert.Equal(2, panes.Transitions?.Count);
            Assert.All(panes.Transitions!.OfType<Avalonia.Animation.DoubleTransition>(), t => Assert.Equal(TimeSpan.FromMilliseconds(200), t.Duration));
            Assert.Empty(Part<PaneLayout>(reduced, StandardsWindowTheme.PanesPart).Transitions!);

            foreach (var w in new[] { moving, reduced })
            {
                w.IsInspectorOpen = false;
                w.IsBottomPanelOpen = false;
            }
            // Reduced: at once. With motion: the slide starts from where the panels were, and they stay shown meanwhile.
            Assert.Equal(0, Reveal(reduced, PaneLayout.InspectorRevealProperty));
            Assert.Equal(0, Reveal(reduced, PaneLayout.BottomRevealProperty));
            Assert.False(Inspector(reduced).IsVisible);
            Assert.Equal(1, Reveal(moving, PaneLayout.InspectorRevealProperty));
            Assert.Equal(1, Reveal(moving, PaneLayout.BottomRevealProperty));
            Assert.True(Inspector(moving).IsVisible);
            Assert.True(Bottom(moving).IsVisible);
            Pump(reduced);
            Elapse(moving, TimeSpan.FromMilliseconds(400));
            Assert.Equal(Layout(reduced), Layout(moving));
            Assert.False(Inspector(moving).IsVisible);
            Assert.False(Bottom(moving).IsVisible);

            foreach (var w in new[] { moving, reduced })
            {
                w.IsInspectorOpen = true;
                w.IsBottomPanelOpen = true;
            }
            Assert.Equal(1, Reveal(reduced, PaneLayout.InspectorRevealProperty));
            Assert.Equal(0, Reveal(moving, PaneLayout.InspectorRevealProperty));
            Assert.Equal(0, Reveal(moving, PaneLayout.BottomRevealProperty));
            Assert.True(Inspector(moving).IsVisible);
            Pump(reduced);
            Elapse(moving, TimeSpan.FromMilliseconds(400));
            Assert.Equal(Layout(reduced), Layout(moving));

            // Crossing the threshold hides the docked inspector at once.
            moving.Width = 1000 + Margin.Left + Margin.Right;
            Pump(moving);
            Assert.False(Inspector(moving).IsVisible);
        }
        finally
        {
            moving.Close();
            reduced.Close();
        }
    }

    [AvaloniaFact]
    public void TheOverlay_SlidesInFromTheRightEdge_LikeTheSidebarOverlay()
    {
        var window = Show(frameWidth: 900, bottom: false, reduceMotion: false);
        try
        {
            var panes = InLayers(window, Part<Control>(window, StandardsWindowTheme.PanesPart));
            window.ToggleInspector();
            // It starts at the right edge, outside the area.
            Assert.True(Inspector(window).IsVisible);
            Assert.Equal(0, Reveal(window, PaneLayout.InspectorRevealProperty));
            var start = Part<PaneLayout>(window, StandardsWindowTheme.PanesPart).Arrangement(Part<PaneLayout>(window, StandardsWindowTheme.PanesPart).Bounds.Size).Inspector;
            Assert.Equal(panes.Width, start.X);
            Elapse(window, TimeSpan.FromMilliseconds(400));
            Assert.Equal(new Rect(panes.Right - 240, panes.Top, 240, panes.Height), InLayers(window, Inspector(window)));
            window.ToggleInspector();
            Assert.Equal(1, Reveal(window, PaneLayout.InspectorRevealProperty));
            Assert.True(Inspector(window).IsVisible);
            Elapse(window, TimeSpan.FromMilliseconds(400));
            Assert.False(Inspector(window).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Catalog --------------------------------------------------------------------------------

    [AvaloniaFact]
    public void Catalog_AtTheMinimumWindowSize_TheSplitPanesPageHidesTheBottomPanel()
    {
        // The 480 x 320 frame plus the shadow margin.
        var window = new Catalog.MainWindow { Width = 528, Height = 368 };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true });
        window.Show();
        try
        {
            window.SelectPage(Catalog.CatalogPages.All.ToList().FindIndex(p => p.Title == "Split panes"));
            Pump(window);
            Assert.True(Part<Control>(window, StandardsWindowTheme.PanesPart).Bounds.Height < PaneLayout.BottomRoom);
            Assert.True(window.IsBottomPanelOpen);
            Assert.False(Bottom(window).IsVisible);
            Assert.Equal(Part<Control>(window, StandardsWindowTheme.PanesPart).Bounds.Height, ContentPanel(window).Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Catalog_SplitPanesPage_FillsThePanels_AndItsViewMenuItemsFollow()
    {
        var window = new Catalog.MainWindow { Width = 1400, Height = 860 };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true });
        window.Show();
        try
        {
            Pump(window);
            Assert.False(window.Menus.Inspector.IsEnabled);
            Assert.False(window.Menus.BottomPanel.IsEnabled);
            var index = Catalog.CatalogPages.All.ToList().FindIndex(p => p.Title == "Split panes");
            window.SelectPage(index);
            Pump(window);
            var stack = Assert.IsType<PanelStack>(window.Inspector);
            Assert.Equal(new object?[] { "Layers", "Properties", "Colour" }, stack.Items.OfType<PanelStackItem>().Select(i => i.Header));
            Assert.Equal(new[] { true, true, false }, stack.Items.OfType<PanelStackItem>().Select(i => i.IsExpanded));
            Assert.IsType<TabControl>(window.BottomPanel);
            Assert.True(window.Menus.Inspector.IsEnabled);
            Assert.Equal("Hide Inspector", window.Menus.Inspector.Header);
            Assert.Equal("Hide Bottom Panel", window.Menus.BottomPanel.Header);

            window.SelectPage(0);
            Pump(window);
            Assert.Null(window.Inspector);
            Assert.Null(window.BottomPanel);
            Assert.False(window.Menus.Inspector.IsEnabled);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Renders --------------------------------------------------------------------------------

    internal static PanelStack NewPanelStack() => new()
    {
        Items =
        {
            new PanelStackItem
            {
                Header = "Layers",
                Icon = Icons.SquaresFour,
                IsExpanded = true,
                Content = new StackPanel
                {
                    Spacing = 4,
                    Children = { new TextBlock { Text = "Title" }, new TextBlock { Text = "Photo" }, new TextBlock { Text = "Background" } },
                },
            },
            new PanelStackItem
            {
                Header = "Properties",
                Icon = Icons.SlidersHorizontal,
                IsExpanded = true,
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Opacity" }, new Slider { Value = 80, Maximum = 100 },
                        new TextBlock { Text = "Blur" }, new Slider { Value = 20, Maximum = 100 },
                    },
                },
            },
            new PanelStackItem { Header = "Colour", Icon = Icons.Palette, Content = new TextBlock { Text = "Colour" } },
        },
    };

    internal static TabControl NewTabs() => new()
    {
        Items =
        {
            new TabItem { Header = "Output", Content = new TextBlock { Text = "Build succeeded." } },
            new TabItem { Header = "Problems", Content = new TextBlock { Text = "No problems." } },
            new TabItem { Header = "Terminal", Content = new TextBlock { Text = "$" } },
        },
    };

    [AvaloniaFact]
    public void Panes_Render()
    {
        foreach (var (label, variant) in ButtonRenderTests.Themes)
        {
            var name = label.ToLowerInvariant();
            var window = Show(sidebar: true, theme: variant, inspectorContent: NewPanelStack(), bottomContent: NewTabs(),
                content: new TextBlock { Text = "Document", FontSize = 28 });
            try
            {
                window.SetRenderScaling(Sheet.Scale);
                Pump(window);
                using var frame = Frame.Capture(window);
                frame.Save($"split-panes-{name}.png");
            }
            finally
            {
                window.Close();
            }

            var narrow = Show(frameWidth: 900, frameHeight: 600, sidebar: true, theme: variant, inspectorContent: NewPanelStack(),
                bottomContent: NewTabs(), content: new TextBlock { Text = "Document", FontSize = 28 });
            try
            {
                narrow.SetRenderScaling(Sheet.Scale);
                narrow.ToggleInspector();
                Pump(narrow);
                using var frame = Frame.Capture(narrow);
                frame.Save($"split-panes-narrow-overlay-{name}.png");
            }
            finally
            {
                narrow.Close();
            }
        }
    }

    internal static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    internal static void Pump(Window window)
    {
        for (var i = 0; i < 4; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
        Dispatcher.UIThread.RunJobs();
    }
}
