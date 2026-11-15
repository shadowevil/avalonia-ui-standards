using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Templates;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The StandardsWindow theme and the shell's values: the frame (wash, border, shadow), the
/// title band, the menu row, the toolbar band slot, the sidebar slot, the glass content panel (a scroll area), the status bar, the
/// backdrop slot and the resize grips.
/// </summary>
internal static class StandardsWindowTheme
{
    public const string FramePart = "PART_Frame";
    public const string ShadowPart = "PART_Shadow";
    public const string TitleBandPart = "PART_TitleBand";
    public const string TitlePart = "PART_Title";
    public const string TitleIconPart = "PART_TitleIcon";

    /// <summary>The title icon's side, in px (the Windows title bar icon size).</summary>
    public const double TitleIconSize = 16;
    public const string ToolbarPart = "PART_Toolbar";
    public const string ToolbarBandPart = "PART_ToolbarBand";
    public const string MenuRowPart = "PART_MenuRow";
    public const string WindowButtonsPart = "PART_WindowButtons";
    public const string SidebarPart = "PART_Sidebar";
    public const string SidebarTogglePart = "PART_SidebarToggle";
    public const string ContentPanelPart = "PART_ContentPanel";
    public const string ContentScrollViewerPart = "PART_ContentScrollViewer";
    public const string BackdropPart = "PART_Backdrop";
    public const string BackdropSamplerPart = "PART_BackdropSampler";
    public const string StatusBarPart = "PART_StatusBar";
    public const string ResizeGripsPart = "PART_ResizeGrips";
    public const string TopPart = "PART_Top";
    public const string RevealSheetPart = "PART_RevealSheet";

    public static string ResizePart(WindowEdge edge) => $"PART_Resize{edge}";

    /// <summary>Room around the frame for the drawn shadow (blur sigma 8, offset 8 down); none on the native macOS frame.</summary>
    public static readonly Thickness ShadowMargin = new(24, 16, 24, 32);

    /// <summary>The smallest frame (the visible window).</summary>
    public static readonly Size MinimumFrame = new(480, 320);

    /// <summary>Resize bands: 8 px along the edges and 12 px at the corners, half outside the frame.</summary>
    public const double ResizeBand = 8;
    public const double ResizeCorner = 12;

    /// <summary>Title band height: 48 (Normal), 40 (Compact).</summary>
    public const double TitleBandNormal = 48;
    public const double TitleBandCompact = 40;

    /// <summary>Status bar height at every density.</summary>
    public const double StatusBarHeight = 24;

    /// <summary>Menu row height: the menu titles' (Button's) height, 32 (Normal) and 24 (Compact).</summary>
    public const double MenuRowNormal = 32;
    public const double MenuRowCompact = 24;

    // Shadow colour per theme, and the density heights, under non-public keys.
    internal static readonly object ShadowColorKey = new();
    internal static readonly object TitleBandHeightKey = new();
    internal static readonly object MenuRowHeightKey = new();
    internal static readonly object RevealTransitionsKey = new();

    /// <summary>
    /// The full screen reveal's transitions: <paramref name="duration"/> is <c>MotionNormal</c>, or zero under reduce
    /// motion (no transition: the rows show and hide at once).
    /// </summary>
    public static Transitions RevealTransitions(TimeSpan duration, Easing easing) => duration == TimeSpan.Zero
        ? []
        : [new DoubleTransition { Property = WindowTop.RevealProperty, Duration = duration, Easing = easing }];

    internal static readonly Color ShadowLight = Color.Parse("#40000000");
    internal static readonly Color ShadowDark = Color.Parse("#80000000");

    public static void AddResources(IResourceDictionary resources)
    {
        ((IResourceDictionary)resources.ThemeDictionaries[ThemeVariant.Light])[ShadowColorKey] = ShadowLight;
        ((IResourceDictionary)resources.ThemeDictionaries[ThemeVariant.Dark])[ShadowColorKey] = ShadowDark;
    }

    public static void ApplyDensity(IResourceDictionary resources, bool compact)
    {
        resources[TitleBandHeightKey] = compact ? TitleBandCompact : TitleBandNormal;
        resources[MenuRowHeightKey] = compact ? MenuRowCompact : MenuRowNormal;
    }

    public static ControlTheme Create(ControlTheme? windowBase) => new(typeof(StandardsWindow))
    {
        BasedOn = windowBase,
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            // The frame's minimum is 480 × 320; the window adds the shadow margin around it.
            new Setter(Layoutable.MinWidthProperty, MinimumFrame.Width + ShadowMargin.Left + ShadowMargin.Right),
            new Setter(Layoutable.MinHeightProperty, MinimumFrame.Height + ShadowMargin.Top + ShadowMargin.Bottom),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<StandardsWindow>(BuildTemplate)),
        },
        Children =
        {
            // Frame: squircle with the wash, a 1 px border, inset by the shadow margin.
            Part<SquircleBorder>(FramePart, null,
                (Layoutable.MarginProperty, ShadowMargin),
                (SquircleBorder.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusOverlay")),
                (SquircleBorder.BorderThicknessProperty, new Thickness(1))),
            Part<WindowShadow>(ShadowPart, null, (Layoutable.MarginProperty, ShadowMargin)),
            Part<Panel>(ResizeGripsPart, null, (Layoutable.MarginProperty, ShadowMargin - new Thickness(ResizeBand / 2))),

            // Maximised, or without OS transparency: the frame fills the window, square, with no shadow.
            Part<SquircleBorder>(FramePart, ":maximized",
                (Layoutable.MarginProperty, new Thickness(0)),
                (SquircleBorder.CornerRadiusProperty, new CornerRadius(0)),
                (SquircleBorder.BorderThicknessProperty, new Thickness(0))),
            Part<WindowShadow>(ShadowPart, ":maximized", (Visual.IsVisibleProperty, false)),
            Part<Panel>(ResizeGripsPart, ":maximized", (Visual.IsVisibleProperty, false)),

            // Full screen: as maximised. The title band and the menu row hide above the top edge (WindowTop).
            Part<SquircleBorder>(FramePart, ":fullscreen",
                (Layoutable.MarginProperty, new Thickness(0)),
                (SquircleBorder.CornerRadiusProperty, new CornerRadius(0)),
                (SquircleBorder.BorderThicknessProperty, new Thickness(0))),
            Part<WindowShadow>(ShadowPart, ":fullscreen", (Visual.IsVisibleProperty, false)),
            Part<Panel>(ResizeGripsPart, ":fullscreen", (Visual.IsVisibleProperty, false)),
            Part<WindowTop>(TopPart, ":fullscreen", (WindowTop.IsFullScreenProperty, true)),
            Part<SquircleBorder>(FramePart, ":opaque",
                (Layoutable.MarginProperty, new Thickness(0)),
                (SquircleBorder.CornerRadiusProperty, new CornerRadius(0))),
            Part<WindowShadow>(ShadowPart, ":opaque", (Visual.IsVisibleProperty, false)),
            Part<Panel>(ResizeGripsPart, ":opaque", (Layoutable.MarginProperty, new Thickness(0))),

            // Native macOS frame: the system draws the shadow and resizes the window, so the
            // window is exactly the frame, with no drawn shadow and no grips.
            Part<SquircleBorder>(FramePart, ":native-frame", (Layoutable.MarginProperty, new Thickness(0))),
            Part<WindowShadow>(ShadowPart, ":native-frame", (Visual.IsVisibleProperty, false)),
            Part<Panel>(ResizeGripsPart, ":native-frame", (Visual.IsVisibleProperty, false)),

            // Without OS transparency, or on the native macOS frame, the window adds no margin
            // around the frame, so the minimum size is the frame's.
            new Avalonia.Styling.Style(x => Selectors.Or(x.Nesting().Class(":opaque"), x.Nesting().Class(":native-frame")))
            {
                Setters =
                {
                    new Setter(Layoutable.MinWidthProperty, MinimumFrame.Width),
                    new Setter(Layoutable.MinHeightProperty, MinimumFrame.Height),
                },
            },

            // Title band: traffic lights on the left (SpacingL in), caption buttons flush right.
            Part<TextBlock>(TitlePart, null, (Layoutable.MarginProperty, new Thickness(Spacing.L, 0, 0, 0))),
            Part<TextBlock>(TitlePart, ":traffic-lights", (Layoutable.MarginProperty, new Thickness(Spacing.M, 0, 0, 0))),
            Part<ContentPresenter>(ToolbarPart, null, (Layoutable.MarginProperty, new Thickness(Spacing.L, 0, Spacing.S, 0))),
            Part<ContentPresenter>(ToolbarPart, ":traffic-lights", (Layoutable.MarginProperty, new Thickness(Spacing.L, 0, Spacing.L, 0))),
            Part<WindowButtons>(WindowButtonsPart, ":traffic-lights", (Layoutable.MarginProperty, new Thickness(Spacing.L, 0, 0, 0))),

            // With a Sidebar Module: its button before the title (SpacingL in, so its icon lines up
            // with the rail's icons below); the sidebar draws its own gap from the window's edge,
            // and its overlay over the glass panel.
            Part<Button>(SidebarTogglePart, null,
                (Layoutable.MarginProperty, new Thickness(Spacing.L, 0, 0, 0)),
                (Visual.IsVisibleProperty, false)),
            Part<Button>(SidebarTogglePart, ":sidebar-module", (Visual.IsVisibleProperty, true)),
            Part<Button>(SidebarTogglePart, ":traffic-lights", (Layoutable.MarginProperty, new Thickness(Spacing.M, 0, 0, 0))),

            // Status content gets SpacingL at the sides; a StatusBar Module keeps its own side padding (its
            // clickable items' fills reach into it).
            Part<ContentPresenter>(StatusBarPart, null, (TemplatedControl.PaddingProperty, new Thickness(Spacing.L, 0))),
            Part<ContentPresenter>(StatusBarPart, ":status-bar-module", (TemplatedControl.PaddingProperty, new Thickness(0))),
            // The slot is 24 high; a StatusBar grows it with its rows (24 per row).
            Part<ContentPresenter>(StatusBarPart, null, (Layoutable.HeightProperty, StatusBarHeight)),
            Part<ContentPresenter>(StatusBarPart, ":status-bar-module", (Layoutable.HeightProperty, double.NaN), (Layoutable.MinHeightProperty, StatusBarHeight)),
            Part<TextBlock>(TitlePart, ":sidebar-module", (Layoutable.MarginProperty, new Thickness(Spacing.S, 0, 0, 0))),

            // The title icon (Windows layout, with Window.Icon): after the sidebar button, or SpacingL
            // in without one (SpacingM beside traffic lights); the title follows it by SpacingS.
            Part<Image>(TitleIconPart, null, (Layoutable.MarginProperty, new Thickness(Spacing.L, 0, 0, 0)), (Visual.IsVisibleProperty, false)),
            Part<Image>(TitleIconPart, ":traffic-lights", (Layoutable.MarginProperty, new Thickness(Spacing.M, 0, 0, 0))),
            Part<Image>(TitleIconPart, ":sidebar-module", (Layoutable.MarginProperty, new Thickness(Spacing.S, 0, 0, 0))),
            Part<Image>(TitleIconPart, ":title-icon", (Visual.IsVisibleProperty, true)),
            Part<TextBlock>(TitlePart, ":title-icon", (Layoutable.MarginProperty, new Thickness(Spacing.S, 0, 0, 0))),
            Part<ContentPresenter>(SidebarPart, null, (Layoutable.MarginProperty, new Thickness(Spacing.S, 0, 0, Spacing.S))),
            Part<GlassBorder>(ContentPanelPart, null, (Layoutable.MarginProperty, new Thickness(Spacing.S, 0, Spacing.S, Spacing.S))),
            Part<ContentPresenter>(SidebarPart, ":sidebar-module",
                (Layoutable.MarginProperty, new Thickness(0, 0, 0, Spacing.S)),
                (Visual.ZIndexProperty, 1)),
            // Full screen without a toolbar band: the glass panel and the sidebar keep SpacingS from the top edge.
            new Avalonia.Styling.Style(x => x.Nesting().Class(":fullscreen").Not(y => y.Class(":toolbar-band")).Template().OfType<GlassBorder>().Name(ContentPanelPart))
            {
                Setters = { new Setter(Layoutable.MarginProperty, new Thickness(Spacing.S)) },
            },
            new Avalonia.Styling.Style(x => x.Nesting().Class(":fullscreen").Not(y => y.Class(":toolbar-band")).Template().OfType<ContentPresenter>().Name(SidebarPart))
            {
                Setters = { new Setter(Layoutable.MarginProperty, new Thickness(0, Spacing.S, 0, Spacing.S)) },
            },
            new Avalonia.Styling.Style(x => x.Nesting().Class(":fullscreen").Not(y => y.Class(":toolbar-band")).Not(y => y.Class(":sidebar-module")).Template().OfType<ContentPresenter>().Name(SidebarPart))
            {
                Setters = { new Setter(Layoutable.MarginProperty, new Thickness(Spacing.S, Spacing.S, 0, Spacing.S)) },
            },
        },
    };

    /// <summary>
    /// The status bar slot centres its content in the band; this style vertically centres every
    /// TextBlock below the status bar slot, including the ones inside controls placed there, so
    /// text in a taller panel row does not sit at its top. A theme-wide style: a control theme
    /// reaches only its template, not the content placed in a slot.
    /// </summary>
    public static IStyle StatusBarTextStyle() =>
        new Avalonia.Styling.Style(x => x.Is<StandardsWindow>().Template().OfType<ContentPresenter>().Name(StatusBarPart).Descendant().OfType<TextBlock>())
        {
            Setters = { new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center) },
        };

    private static Control BuildTemplate(StandardsWindow window, INameScope scope)
    {
        var parts = new List<Control>();
        T Named<T>(T control, string name) where T : Control
        {
            control.Name = name;
            parts.Add(control);
            return control;
        }

        // Title band.
        // The window sets the buttons' style: its WindowButtonStyle, or by layout mode when Auto.
        var buttons = Named(new WindowButtons(), WindowButtonsPart);
        var title = Named(new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis }, TitlePart);
        title.Bind(TextBlock.TextProperty, new TemplateBinding(Window.TitleProperty));
        title.Bind(TextBlock.FontWeightProperty, new DynamicResourceExtension("FontWeightBodyStrong"));
        title.Bind(TextBlock.FontSizeProperty, new DynamicResourceExtension("FontSizeBody"));
        var sidebarToggle = Named(SidebarToggle(), SidebarTogglePart);
        // Decorative: not focusable, not hit-testable (a press falls through to the title band, which
        // drags), and hidden from assistive technology.
        var titleIcon = Named(new Image
        {
            Width = TitleIconSize,
            Height = TitleIconSize,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Focusable = false,
            IsHitTestVisible = false,
        }, TitleIconPart);
        Avalonia.Automation.AutomationProperties.SetAccessibilityView(titleIcon, Avalonia.Automation.AccessibilityView.Raw);
        var heading = new StackPanel { Orientation = Orientation.Horizontal, Children = { sidebarToggle, titleIcon, title } };
        Grid.SetColumn(heading, 1);
        var toolbar = Named(Slot(StandardsWindow.ToolbarProperty), ToolbarPart);
        toolbar.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(toolbar, 2);
        var titleBand = Named(new Grid
        {
            Background = Brushes.Transparent,
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto"),
            Children = { buttons, heading, toolbar },
        }, TitleBandPart);
        WindowDecorationProperties.SetElementRole(titleBand, WindowDecorationsElementRole.TitleBar);
        titleBand.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(TitleBandHeightKey));

        // The menu row below it, in the Windows layout of a window with a menu (the window places its
        // menu bar in it): on the window background, part of the title bar (its empty area drags the
        // window), with the menu titles as interactive elements.
        var menuRow = Named(new Panel { Background = Brushes.Transparent, IsVisible = false }, MenuRowPart);
        WindowDecorationProperties.SetElementRole(menuRow, WindowDecorationsElementRole.TitleBar);
        menuRow.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(MenuRowHeightKey));

        // The toolbar band below it, on the window background; the presenter takes presses on the
        // band's empty area (the window decides whether they drag).
        var toolbarBand = Named(Slot(StandardsWindow.ToolbarBandProperty), ToolbarBandPart);
        toolbarBand.Background = Brushes.Transparent;

        // The top: the title band and menu row above the toolbar band. In full screen the two rows hide above the top
        // edge and slide in over the content on a glass sheet, the toolbar band moving down below them; drawn above the
        // sidebar and the glass panel.
        var sheet = Named(new GlassBorder { CornerRadius = new CornerRadius(0) }, RevealSheetPart);
        var top = Named(new WindowTop(new StackPanel { Children = { titleBand, menuRow } }, toolbarBand, sheet), TopPart);
        top.Bind(Animatable.TransitionsProperty, new DynamicResourceExtension(RevealTransitionsKey));
        top.ZIndex = 2;
        DockPanel.SetDock(top, Dock.Top);

        // Status bar, sidebar and the glass content panel. The status bar is the last row, across
        // the window and under the sidebar; it comes last in the tree, so its items are the last Tab stops.
        var statusBar = Named(Slot(StandardsWindow.StatusBarProperty), StatusBarPart);
        statusBar.VerticalContentAlignment = VerticalAlignment.Center;
        Grid.SetRow(statusBar, 1);

        var sidebar = Named(Slot(StandardsWindow.SidebarProperty), SidebarPart);
        DockPanel.SetDock(sidebar, Dock.Left);

        // The panel is the scroll area: the content scrolls edge to edge under its shape, the
        // padding is inside the scrolled content, the scroll bar and shadows sit at its edge.
        // Not scrollable, the scroller is disabled: the content gets the panel's size inside the padding.
        var content = Named(new ContentPresenter { Padding = new Thickness(Spacing.M) }, "PART_ContentPresenter");
        content.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        content.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        var scroller = Named(new ScrollViewer
        {
            Content = content,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        }, ContentScrollViewerPart);
        scroller.Bind(ScrollViewer.VerticalScrollBarVisibilityProperty, new TemplateBinding(StandardsWindow.IsContentScrollableProperty)
        {
            Converter = new FuncValueConverter<bool, ScrollBarVisibility>(on => on ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled),
        });
        var panel = Named(new GlassBorder { Child = scroller }, ContentPanelPart);

        // Frame (the backdrop behind everything in it), shadow and resize grips. The sampler right after the backdrop
        // takes the shared backdrop sample that first-level glass on the window's layers reads; the top (with the full
        // screen reveal sheet, drawn over the content) takes its own.
        var backdrop = Named(Slot(StandardsWindow.BackdropProperty), BackdropPart);
        var sampler = new GlassBackdropSampler { Name = BackdropSamplerPart };
        sampler.Bind(GlassBackdropSampler.BlurSigmaProperty, new DynamicResourceExtension("GlassBlurSigma"));
        var layers = new DockPanel
        {
            Children =
            {
                top,
                new Grid
                {
                    RowDefinitions = new RowDefinitions("*,Auto"),
                    Children = { new DockPanel { Children = { sidebar, panel } }, statusBar },
                },
            },
        };
        BackdropPlane.SetPlane(layers, sampler.Plane);
        BackdropPlane.SetPlane(top, BackdropPlane.Excluded);
        var frame = Named(new SquircleBorder
        {
            Child = new Panel { Children = { backdrop, sampler, layers } },
        }, FramePart);
        frame.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("BackdropWash"));
        frame.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("SystemControlForegroundBaseLowBrush"));

        var shadow = Named(new WindowShadow(), ShadowPart);
        shadow.Bind(WindowShadow.CornerRadiusProperty, new DynamicResourceExtension("CornerRadiusOverlay"));
        shadow.Bind(WindowShadow.ColorProperty, new DynamicResourceExtension(ShadowColorKey));
        shadow.Bind(WindowShadow.SmoothingProperty, new DynamicResourceExtension("CornerSmoothing"));

        var grips = Named(new Panel(), ResizeGripsPart);
        // A window that cannot be resized has no grips.
        grips.Bind(Visual.IsVisibleProperty, new TemplateBinding(Window.CanResizeProperty));
        foreach (var edge in Enum.GetValues<WindowEdge>())
            grips.Children.Add(Named(Grip(edge), ResizePart(edge)));

        // Behind the frame is the window background, transparent unless an app sets it.
        var surface = new Panel { Children = { shadow, frame, grips } };
        surface.Bind(Panel.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
        var root = Named(new VisualLayerManager { Child = surface }, "PART_VisualLayerManager");
        foreach (var part in parts)
            part.RegisterInNameScope(scope);
        return root;
    }

    // The sidebar's show/hide button: a subtle square button with the sidebar icon, shown while
    // the window's Sidebar is a Sidebar Module. It is an interactive element of the chrome: where
    // the client area extends under the title bar, the OS passes its input through to it instead
    // of treating it as part of the title band's drag area. Pressing it is handled by the button,
    // so the band starts no drag and no double-click action.
    private static Button SidebarToggle()
    {
        var toggle = new Button
        {
            Content = new Icon { Data = Icons.SidebarSimple },
            Padding = default,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        toggle.Classes.Add("subtle");
        toggle.SetValue(Sidebar.IsToggleProperty, true);
        toggle.SetValue(Avalonia.Automation.AutomationProperties.NameProperty, "Toggle sidebar");
        WindowDecorationProperties.SetElementRole(toggle, WindowDecorationsElementRole.DecorationsElement);
        toggle.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(ButtonParts.MinHeightKey));
        toggle.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(ButtonParts.MinHeightKey));
        return toggle;
    }

    // An optional slot: a presenter for the window's property, shown only when it is set.
    private static ContentPresenter Slot(AvaloniaProperty property)
    {
        var presenter = new ContentPresenter();
        presenter.Bind(ContentPresenter.ContentProperty, new TemplateBinding(property));
        presenter.Bind(Visual.IsVisibleProperty, new TemplateBinding(property) { Converter = ObjectConverters.IsNotNull });
        return presenter;
    }

    private static ResizeGrip Grip(WindowEdge edge)
    {
        var (horizontal, vertical, cursor, role) = edge switch
        {
            WindowEdge.NorthWest => (HorizontalAlignment.Left, VerticalAlignment.Top, StandardCursorType.TopLeftCorner, WindowDecorationsElementRole.ResizeNW),
            WindowEdge.North => (HorizontalAlignment.Stretch, VerticalAlignment.Top, StandardCursorType.TopSide, WindowDecorationsElementRole.ResizeN),
            WindowEdge.NorthEast => (HorizontalAlignment.Right, VerticalAlignment.Top, StandardCursorType.TopRightCorner, WindowDecorationsElementRole.ResizeNE),
            WindowEdge.West => (HorizontalAlignment.Left, VerticalAlignment.Stretch, StandardCursorType.LeftSide, WindowDecorationsElementRole.ResizeW),
            WindowEdge.East => (HorizontalAlignment.Right, VerticalAlignment.Stretch, StandardCursorType.RightSide, WindowDecorationsElementRole.ResizeE),
            WindowEdge.SouthWest => (HorizontalAlignment.Left, VerticalAlignment.Bottom, StandardCursorType.BottomLeftCorner, WindowDecorationsElementRole.ResizeSW),
            WindowEdge.South => (HorizontalAlignment.Stretch, VerticalAlignment.Bottom, StandardCursorType.BottomSide, WindowDecorationsElementRole.ResizeS),
            _ => (HorizontalAlignment.Right, VerticalAlignment.Bottom, StandardCursorType.BottomRightCorner, WindowDecorationsElementRole.ResizeSE),
        };
        var corner = horizontal != HorizontalAlignment.Stretch && vertical != VerticalAlignment.Stretch;
        var grip = new ResizeGrip
        {
            Background = Brushes.Transparent,
            Cursor = new Cursor(cursor),
            HorizontalAlignment = horizontal,
            VerticalAlignment = vertical,
            Width = corner ? ResizeCorner : horizontal == HorizontalAlignment.Stretch ? double.NaN : ResizeBand,
            Height = corner ? ResizeCorner : vertical == VerticalAlignment.Stretch ? double.NaN : ResizeBand,
            Margin = horizontal == HorizontalAlignment.Stretch ? new Thickness(ResizeCorner, 0)
                : vertical == VerticalAlignment.Stretch ? new Thickness(0, ResizeCorner) : default,
        };
        WindowDecorationProperties.SetElementRole(grip, role);
        return grip;
    }

    // A style on a named template part, under an optional window pseudo-class.
    private static Avalonia.Styling.Style Part<T>(string name, string? pseudoClass, params (AvaloniaProperty Property, object Value)[] setters)
        where T : Control
    {
        var style = new Avalonia.Styling.Style(x =>
            (pseudoClass is null ? x.Nesting() : x.Nesting().Class(pseudoClass)).Template().OfType<T>().Name(name));
        foreach (var (property, value) in setters)
            style.Setters.Add(new Setter(property, value));
        return style;
    }
}
