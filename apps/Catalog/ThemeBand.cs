using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;

namespace Catalog;

/// <summary>
/// The live theme controls, in a <see cref="ToolbarBand"/> for the window's toolbar band slot: each one sets a property
/// of the application's <see cref="StandardsTheme"/>. Theme and accent lead, then density; motion and transparency
/// sit at the trailing end. As the window narrows, transparency moves into the "…" menu first, then motion, density,
/// accent and theme last.
/// </summary>
internal sealed class ThemeBand
{
    internal static readonly (string Name, string Hex)[] Accents =
    [
        ("Default", Hex(StandardsTheme.DefaultAccent)),
        ("Teal", "#00899E"),
        ("Green", "#2E8B3E"),
        ("Orange", "#E0720B"),
        ("Purple", "#7A4FD6"),
        ("Pink", "#E91E63"),
    ];

    private readonly StandardsTheme? _theme = Application.Current?.Styles.OfType<StandardsTheme>().FirstOrDefault();
    private readonly ChipIcon _swatch = new();

    public ThemeBand()
    {
        Theme = Segmented("Theme", ["System", "Light", "Dark"]);
        Density = Segmented("Density", ["Normal", "Compact"]);
        Motion = Segmented("Motion", ["Follow OS", "Reduced", "Full"], Icons.MotionSystem);
        Transparency = Segmented("Transparency", ["Follow OS", "Reduced", "Full"], Icons.Drop);

        // The accent: a drop-down button showing the accent, opening a menu of the presets as check items (each with
        // its chip), then the hex box.
        AccentPresets = Accents.Select(a => Preset(a.Name, a.Hex)).ToList();
        AccentHex = new TextBox { MaxLength = 9, MinWidth = 120 };
        ToolTip.SetTip(AccentHex, "Accent colour as #RRGGBB");
        AutomationProperties.SetName(AccentHex, "Accent colour as #RRGGBB");
        AccentHexItem = new MenuItem { Header = AccentHex, StaysOpenOnClick = true };
        // Its label as a submenu entry in the band's "…" menu, which opens the whole accent menu.
        ToolbarBand.SetLabel(AccentHexItem, "Custom colour…");
        AutomationProperties.SetName(AccentHexItem, "Custom colour");
        AccentMenu = new MenuFlyout();
        foreach (var preset in AccentPresets)
            AccentMenu.Items.Add(preset);
        AccentMenu.Items.Add(new Separator());
        AccentMenu.Items.Add(AccentHexItem);
        Accent = new DropDownButton { Content = _swatch, Flyout = AccentMenu };
        Accent.Classes.Add("subtle");
        ToolTip.SetTip(Accent, "Accent colour");
        AutomationProperties.SetName(Accent, "Accent colour");

        // Below the button, left edges aligned; opened at the band's trailing end (from "…"), right edges aligned.
        AccentMenu.Opening += (_, _) => AccentMenu.Placement =
            AccentMenu.Target == Accent ? PlacementMode.BottomEdgeAlignedLeft : PlacementMode.BottomEdgeAlignedRight;
        // Keyboard: moving onto the hex row puts the caret in the box.
        AccentHexItem.GotFocus += (_, e) =>
        {
            if (e.Source == AccentHexItem)
                AccentHex.Focus(NavigationMethod.Directional);
        };
        // Zones and overflow order: theme and accent stay longest; the accessibility pair at the trailing end goes first.
        ToolbarBand.SetPriority(Accent, 1);
        ToolbarBand.SetPriority(Density, 2);
        ToolbarBand.SetBeginsGroup(Density, true);
        ToolbarBand.SetZone(Motion, ToolbarZone.Trailing);
        ToolbarBand.SetPriority(Motion, 3);
        ToolbarBand.SetZone(Transparency, ToolbarZone.Trailing);
        ToolbarBand.SetPriority(Transparency, 4);
        ToolbarBand.SetBeginsGroup(Transparency, true);
        Band = new ToolbarBand { Items = { Theme, Accent, Density, Motion, Transparency } };
        // Chosen from the "…" menu's submenu (the menu itself is closed then): the whole accent menu opens.
        AccentHexItem.Click += (_, _) =>
        {
            if (!AccentMenu.IsOpen)
                AccentMenu.ShowAt(Band);
        };

        // The controls start at the theme's current values (the catalog's saved settings), before they set them.
        var accent = Hex(_theme?.Accent ?? StandardsTheme.DefaultAccent);
        AccentHex.Text = accent;
        CheckPreset(accent);
        var variant = Application.Current?.RequestedThemeVariant;
        Theme.SelectedIndex = variant == ThemeVariant.Light ? 1 : variant == ThemeVariant.Dark ? 2 : 0;
        Density.SelectedIndex = _theme?.Density == DensityStyle.Compact ? 1 : 0;
        Motion.SelectedIndex = Index(_theme?.ReduceMotion);
        Transparency.SelectedIndex = Index(_theme?.ReduceTransparency);

        Theme.SelectionChanged += (_, _) => Application.Current!.RequestedThemeVariant =
            Theme.SelectedIndex switch { 1 => ThemeVariant.Light, 2 => ThemeVariant.Dark, _ => ThemeVariant.Default };
        Density.SelectionChanged += (_, _) =>
        {
            if (_theme is not null)
                _theme.Density = Density.SelectedIndex == 1 ? DensityStyle.Compact : DensityStyle.Normal;
        };
        Motion.SelectionChanged += (_, _) =>
        {
            ShowMotionIcon();
            if (_theme is not null)
                _theme.ReduceMotion = Tri(Motion.SelectedIndex);
        };
        ShowMotionIcon();
        Transparency.SelectionChanged += (_, _) =>
        {
            if (_theme is not null)
                _theme.ReduceTransparency = Tri(Transparency.SelectedIndex);
        };
        // The text property, not TextChanged: TextChanged is raised after the box renders, so it never fires while the
        // flyout is closed (a preset or hex set from code).
        AccentHex.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
                ApplyHex();
        };

        // The swatch follows the accent however it is set, while the band is shown.
        IDisposable? accentWatch = null;
        Band.AttachedToVisualTree += (_, _) => accentWatch = _theme?.GetObservable(StandardsTheme.AccentProperty)
            .Subscribe(new AnonymousObserver<Color>(c => _swatch.Background = new SolidColorBrush(c)));
        Band.DetachedFromVisualTree += (_, _) => accentWatch?.Dispose();
    }

    /// <summary>The band, for <c>StandardsWindow.ToolbarBand</c>.</summary>
    public ToolbarBand Band { get; }

    public SegmentedControl Theme { get; }

    /// <summary>The accent: a drop-down button showing the accent's chip, opening <see cref="AccentMenu"/>.</summary>
    public DropDownButton Accent { get; }

    public MenuFlyout AccentMenu { get; }

    /// <summary>The presets' radio items, in the order of <see cref="Accents"/>.</summary>
    public IReadOnlyList<MenuItem> AccentPresets { get; }

    /// <summary>The menu's last item, holding <see cref="AccentHex"/>.</summary>
    public MenuItem AccentHexItem { get; }

    public TextBox AccentHex { get; }

    public SegmentedControl Density { get; }

    public SegmentedControl Motion { get; }

    public SegmentedControl Transparency { get; }

    private static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    // Follow OS (null), Reduced (true), Full (false).
    private static bool? Tri(int index) => index switch { 1 => true, 2 => false, _ => null };

    private static int Index(bool? value) => value switch { true => 1, false => 2, null => 0 };

    // The motion group's leading icon shows the chosen value: two curls (Follow OS), one (Reduced) or three gusts (Full).
    private void ShowMotionIcon() => ((LeadingIconSegmentedControl)Motion).Icon.Data = Motion.SelectedIndex switch
    {
        1 => Icons.MotionReduced,
        2 => Icons.MotionFull,
        _ => Icons.MotionSystem,
    };

    private static SegmentedControl Segmented(string label, string[] values, Geometry? icon = null)
    {
        var control = icon is null ? new SegmentedControl() : new LeadingIconSegmentedControl(icon);
        foreach (var value in values)
            control.Items.Add(new SegmentedControlItem { Content = value });
        control.SelectedIndex = 0;
        ToolTip.SetTip(control, label);
        ToolbarBand.SetLabel(control, label);
        AutomationProperties.SetName(control, label);
        return control;
    }

    // A preset: a check item with its chip and name, so the chosen one shows the menu's check mark; the presets are
    // kept one-of-many here. Choosing one sets the hex box, which sets the accent, and checks it again (choosing the
    // checked one unchecks it first).
    private MenuItem Preset(string name, string hex)
    {
        var item = new MenuItem
        {
            Header = name,
            Icon = new ChipIcon { Background = new SolidColorBrush(Color.Parse(hex)) },
            ToggleType = MenuItemToggleType.CheckBox,
        };
        item.Click += (_, _) =>
        {
            AccentHex.Text = hex;
            CheckPreset(hex);
        };
        return item;
    }

    private void ApplyHex()
    {
        var text = AccentHex.Text?.Trim() ?? "";
        var valid = text.StartsWith('#') && text.Length is 7 && Color.TryParse(text, out _);
        if (!valid && Application.Current!.FindResource("DangerColor") is Color danger)
            AccentHex.Foreground = new SolidColorBrush(danger);
        else
            AccentHex.ClearValue(TemplatedControl.ForegroundProperty);
        if (!valid || _theme is null)
            return;

        _theme.Accent = Color.Parse(text);
        CheckPreset(text);
    }

    // Checks the preset with this hex and no other; none when it matches no preset.
    private void CheckPreset(string hex)
    {
        for (var i = 0; i < Accents.Length; i++)
            AccentPresets[i].IsChecked = Accents[i].Hex.Equals(hex, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// A colour chip in an icon's place: a squircle at the Icon size (20, 16 Compact) filled with its <c>Background</c>,
/// with a hairline edge and radius 4, as the Colours page's swatches. As an <see cref="Icon"/>, it sits where icons do:
/// a toolbar band's icon drop-down, a menu item's icon column.
/// </summary>
internal sealed class ChipIcon : Icon
{
    public ChipIcon()
    {
        Template = new FuncControlTemplate<ChipIcon>((_, _) =>
        {
            var chip = new SquircleBorder { CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1) };
            chip.Bind(SquircleBorder.BackgroundProperty, new TemplateBinding(BackgroundProperty));
            chip.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("SystemControlForegroundBaseLowBrush"));
            return chip;
        });
    }

    // The Icon theme, for its density size; the template above replaces its path.
    protected override Type StyleKeyOverride => typeof(Icon);
}

/// <summary>
/// A segmented control with a decorative icon on its leading side, 8 px before the segments. The icon is part of the
/// control, so it moves into the "…" menu with it; it takes no focus and is hidden from automation, where the control
/// keeps its own name. It is drawn as the band's icon buttons draw theirs: at the Icon size (20, 16 Compact) in the
/// subtle button's text colour.
/// </summary>
internal sealed class LeadingIconSegmentedControl : SegmentedControl
{
    private const double Gap = 8;

    public LeadingIconSegmentedControl(Geometry icon)
    {
        Icon = new Icon { Data = icon, Focusable = false, IsHitTestVisible = false };
        Icon.Bind(TemplatedControl.ForegroundProperty, new DynamicResourceExtension("ButtonForeground"));
        AutomationProperties.SetAccessibilityView(Icon, AccessibilityView.Raw);
        LogicalChildren.Add(Icon);
    }

    internal Icon Icon { get; }

    protected override Type StyleKeyOverride => typeof(SegmentedControl);

    // Applying the template replaces the visual children, so the icon joins them afterwards.
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (!VisualChildren.Contains(Icon))
            VisualChildren.Add(Icon);
    }

    private double Lead => Icon.DesiredSize.Width + Gap;

    protected override Size MeasureOverride(Size availableSize)
    {
        Icon.Measure(Size.Infinity);
        var available = new Size(Math.Max(0, availableSize.Width - Lead), availableSize.Height);
        var size = default(Size);
        foreach (var child in Segments())
        {
            child.Measure(available);
            size = new Size(Math.Max(size.Width, child.DesiredSize.Width), Math.Max(size.Height, child.DesiredSize.Height));
        }
        return new Size(size.Width + Lead, Math.Max(size.Height, Icon.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var icon = Icon.DesiredSize;
        Icon.Arrange(new Rect(0, (finalSize.Height - icon.Height) / 2, icon.Width, icon.Height));
        var rest = new Rect(Lead, 0, Math.Max(0, finalSize.Width - Lead), finalSize.Height);
        foreach (var child in Segments())
            child.Arrange(rest);
        return finalSize;
    }

    // The template's root: every visual child but the icon.
    private IEnumerable<Layoutable> Segments() => VisualChildren.OfType<Layoutable>().Where(c => c != Icon);
}
