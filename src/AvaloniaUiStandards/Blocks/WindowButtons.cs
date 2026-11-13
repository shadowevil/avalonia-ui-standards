using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaUiStandards.Blocks;

/// <summary>Which window buttons to show.</summary>
public enum WindowButtonStyle
{
    /// <summary>Traffic lights on macOS, caption buttons elsewhere.</summary>
    Auto,

    /// <summary>macOS traffic lights (close, minimise, zoom), placed on the left.</summary>
    TrafficLights,

    /// <summary>Windows and Linux caption buttons (minimise, maximise or restore, close), placed on the right.</summary>
    CaptionButtons,
}

/// <summary>What a window button does.</summary>
public enum WindowButtonKind
{
    Close,
    Minimize,

    /// <summary>Maximise or restore (zoom on macOS).</summary>
    Maximize,
}

/// <summary>A macOS traffic light: a coloured dot whose glyph shows while the group is hovered, grey while its window is inactive (unless hovered).</summary>
public class TrafficLightButton : Button
{
    public static readonly StyledProperty<WindowButtonKind> KindProperty =
        AvaloniaProperty.Register<TrafficLightButton, WindowButtonKind>(nameof(Kind));

    static TrafficLightButton() =>
        KindProperty.Changed.AddClassHandler<TrafficLightButton>((b, _) => b.UpdateKind());

    public TrafficLightButton()
    {
        WindowButtonParts.MakeNonFocusable(this);
        UpdateKind();
    }

    public WindowButtonKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>Shows or hides the glyph; set by <see cref="WindowButtons"/> for the whole group.</summary>
    internal void SetGlyphVisible(bool visible) => PseudoClasses.Set(":glyph", visible);

    /// <summary>Greys the light while its window is inactive; set by <see cref="WindowButtons"/>.</summary>
    internal void SetInactive(bool inactive) => PseudoClasses.Set(":inactive", inactive);

    private void UpdateKind()
    {
        WindowButtonParts.SetRole(this, Kind);
        AutomationProperties.SetName(this, Kind switch
        {
            WindowButtonKind.Close => "Close",
            WindowButtonKind.Minimize => "Minimize",
            _ => "Zoom",
        });
    }
}

/// <summary>A Windows or Linux caption button. Close turns to the danger fill on hover.</summary>
public class CaptionButton : Button
{
    public static readonly StyledProperty<WindowButtonKind> KindProperty =
        AvaloniaProperty.Register<CaptionButton, WindowButtonKind>(nameof(Kind));

    /// <summary>Whether the window is maximised: the maximise button then shows the restore glyph.</summary>
    public static readonly StyledProperty<bool> IsMaximizedProperty =
        AvaloniaProperty.Register<CaptionButton, bool>(nameof(IsMaximized));

    internal static readonly DirectProperty<CaptionButton, Geometry?> GlyphProperty =
        AvaloniaProperty.RegisterDirect<CaptionButton, Geometry?>(nameof(Glyph), b => b.Glyph);

    private Geometry? _glyph;

    static CaptionButton()
    {
        KindProperty.Changed.AddClassHandler<CaptionButton>((b, _) => b.Update());
        IsMaximizedProperty.Changed.AddClassHandler<CaptionButton>((b, _) => b.Update());
    }

    public CaptionButton()
    {
        WindowButtonParts.MakeNonFocusable(this);
        Update();
    }

    /// <summary>Dims the glyph while its window is inactive; set by <see cref="WindowButtons"/>.</summary>
    internal void SetInactive(bool inactive) => PseudoClasses.Set(":inactive", inactive);

    public WindowButtonKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public bool IsMaximized
    {
        get => GetValue(IsMaximizedProperty);
        set => SetValue(IsMaximizedProperty, value);
    }

    internal Geometry? Glyph => _glyph;

    private void Update()
    {
        SetAndRaise(GlyphProperty, ref _glyph, WindowButtonGlyphs.Caption(Kind, IsMaximized));
        WindowButtonParts.SetRole(this, Kind);
        PseudoClasses.Set(":close", Kind == WindowButtonKind.Close);
        AutomationProperties.SetName(this, Kind switch
        {
            WindowButtonKind.Close => "Close",
            WindowButtonKind.Minimize => "Minimize",
            _ => IsMaximized ? "Restore" : "Maximize",
        });
    }
}

/// <summary>
/// The window buttons of a window: traffic lights or caption buttons, chosen by
/// <see cref="ButtonStyle"/> (by OS when <see cref="WindowButtonStyle.Auto"/>). Raises
/// <see cref="Clicked"/> with the button's kind; the window acts on it.
/// </summary>
public class WindowButtons : Decorator
{
    public static readonly StyledProperty<WindowButtonStyle> ButtonStyleProperty =
        AvaloniaProperty.Register<WindowButtons, WindowButtonStyle>(nameof(ButtonStyle));

    public static readonly StyledProperty<bool> IsMaximizedProperty =
        AvaloniaProperty.Register<WindowButtons, bool>(nameof(IsMaximized));

    /// <summary>Whether the window is active; inactive, traffic lights go grey and caption glyphs dim.</summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<WindowButtons, bool>(nameof(IsActive), true);

    static WindowButtons()
    {
        ButtonStyleProperty.Changed.AddClassHandler<WindowButtons>((w, _) => w.Build());
        IsMaximizedProperty.Changed.AddClassHandler<WindowButtons>((w, _) => w.UpdateMaximized());
        IsActiveProperty.Changed.AddClassHandler<WindowButtons>((w, _) => w.UpdateActive());
    }

    public WindowButtons() => Build();

    public WindowButtonStyle ButtonStyle
    {
        get => GetValue(ButtonStyleProperty);
        set => SetValue(ButtonStyleProperty, value);
    }

    public bool IsMaximized
    {
        get => GetValue(IsMaximizedProperty);
        set => SetValue(IsMaximizedProperty, value);
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>The style in use: <see cref="ButtonStyle"/>, with Auto resolved for this OS.</summary>
    public WindowButtonStyle ResolvedStyle => Resolve(ButtonStyle);

    /// <summary>Raised when a window button is clicked.</summary>
    public event EventHandler<WindowButtonKind>? Clicked;

    internal static WindowButtonStyle Resolve(WindowButtonStyle style) => style != WindowButtonStyle.Auto
        ? style
        : OperatingSystem.IsMacOS() ? WindowButtonStyle.TrafficLights : WindowButtonStyle.CaptionButtons;

    private void Build()
    {
        var trafficLights = ResolvedStyle == WindowButtonStyle.TrafficLights;
        WindowButtonKind[] order = trafficLights
            ? [WindowButtonKind.Close, WindowButtonKind.Minimize, WindowButtonKind.Maximize]
            : [WindowButtonKind.Minimize, WindowButtonKind.Maximize, WindowButtonKind.Close];

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = trafficLights ? WindowButtonThemes.TrafficLightGap : 0,
            VerticalAlignment = trafficLights ? VerticalAlignment.Center : VerticalAlignment.Stretch,
        };
        foreach (var kind in order)
        {
            Button button = trafficLights
                ? new TrafficLightButton { Kind = kind }
                : new CaptionButton { Kind = kind, IsMaximized = IsMaximized };
            button.Click += (_, e) =>
            {
                e.Handled = true;
                Clicked?.Invoke(this, kind);
            };
            panel.Children.Add(button);
        }
        Child = panel;
        UpdateGlyphs();
        UpdateActive();
    }

    private void UpdateActive()
    {
        if (Child is not Panel panel)
            return;
        foreach (var light in panel.Children.OfType<TrafficLightButton>())
            light.SetInactive(!IsActive);
        foreach (var caption in panel.Children.OfType<CaptionButton>())
            caption.SetInactive(!IsActive);
    }

    private void UpdateMaximized()
    {
        if (Child is Panel panel)
            foreach (var button in panel.Children.OfType<CaptionButton>())
                button.IsMaximized = IsMaximized;
    }

    // As macOS does: all three glyphs show while the pointer is over the group.
    private void UpdateGlyphs()
    {
        var visible = IsPointerOver;
        if (Child is Panel panel)
            foreach (var light in panel.Children.OfType<TrafficLightButton>())
                light.SetGlyphVisible(visible);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsPointerOverProperty)
            UpdateGlyphs();
    }
}

internal static class WindowButtonParts
{
    /// <summary>
    /// Tags the button with its chrome role (close, minimise, maximise), so a window with an
    /// extended client area lets the OS treat it as that window button.
    /// </summary>
    public static void SetRole(Button button, WindowButtonKind kind) => WindowDecorationProperties.SetElementRole(button, kind switch
    {
        WindowButtonKind.Close => WindowDecorationsElementRole.CloseButton,
        WindowButtonKind.Minimize => WindowDecorationsElementRole.MinimizeButton,
        _ => WindowDecorationsElementRole.MaximizeButton,
    });

    /// <summary>Window buttons are not focus or Tab stops, as native ones; the window has the keyboard shortcuts.</summary>
    public static void MakeNonFocusable(Button button)
    {
        button.Focusable = false;
        KeyboardNavigation.SetIsTabStop(button, false);
    }
}

/// <summary>The glyphs the window buttons draw themselves (no icon set).</summary>
internal static class WindowButtonGlyphs
{
    /// <summary>Caption glyph, in a 10 × 10 box, drawn with a 1 px stroke.</summary>
    public static Geometry Caption(WindowButtonKind kind, bool maximized) => Geometry.Parse(kind switch
    {
        WindowButtonKind.Minimize => "M0,5.5 L10,5.5",
        WindowButtonKind.Maximize when maximized => "M0.5,2.5 H7.5 V9.5 H0.5 Z M2.5,2.5 V0.5 H9.5 V7.5 H7.5",
        WindowButtonKind.Maximize => "M0.5,0.5 H9.5 V9.5 H0.5 Z",
        _ => "M0,0 L10,10 M10,0 L0,10",
    });

    /// <summary>Traffic light glyph, in a 6 × 6 box, drawn with a 1 px round-capped stroke.</summary>
    public static Geometry TrafficLight(WindowButtonKind kind) => Geometry.Parse(kind switch
    {
        WindowButtonKind.Minimize => "M0,3 L6,3",
        WindowButtonKind.Maximize => "M3,0 L3,6 M0,3 L6,3",
        _ => "M0,0 L6,6 M6,0 L0,6",
    });
}
