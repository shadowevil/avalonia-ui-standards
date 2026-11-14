using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

    // The accent presets' chip in the list, and the swatch's corner radius.
    private const double ListChipSize = 16;
    private static readonly CornerRadius ChipRadius = new(4);

    private readonly StandardsTheme? _theme = Application.Current?.Styles.OfType<StandardsTheme>().FirstOrDefault();
    private readonly SquircleBorder _swatchChip;
    private bool _syncing;

    public ThemeBand()
    {
        Theme = Segmented("Theme", ["System", "Light", "Dark"]);
        Density = Segmented("Density", ["Normal", "Compact"]);
        Motion = Segmented("Motion", ["Follow OS", "Reduced", "Full"], Icons.Play);
        Transparency = Segmented("Transparency", ["Follow OS", "Reduced", "Full"], Icons.Drop);

        _swatchChip = Chip();
        AccentList = new ListBox
        {
            Width = 200,
            ItemsSource = Accents.Select(a => AccentRow(a.Name, a.Hex)).ToList(),
        };
        SelectionOptions.SetAccentFlag(AccentList, true);
        AutomationProperties.SetName(AccentList, "Accent presets");
        AccentHex = new TextBox { MaxLength = 9 };
        ToolTip.SetTip(AccentHex, "Accent colour as #RRGGBB");
        AutomationProperties.SetName(AccentHex, "Accent colour as #RRGGBB");
        AccentFlyout = new Flyout
        {
            Content = new StackPanel { Spacing = 8, Children = { AccentList, AccentHex } },
        };
        Accent = Swatch();
        // Below the swatch, left edges aligned; opened from "…" at the trailing end, right edges aligned, as the "…" menu.
        AccentFlyout.Opening += (_, _) => AccentFlyout.Placement =
            AccentFlyout.Target == Accent ? PlacementMode.BottomEdgeAlignedLeft : PlacementMode.BottomEdgeAlignedRight;

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

        // The controls start at the theme's current values (the catalog's saved settings), before they set them.
        var accent = Hex(_theme?.Accent ?? StandardsTheme.DefaultAccent);
        AccentList.SelectedIndex = Array.FindIndex(Accents, a => a.Hex == accent);
        AccentHex.Text = accent;
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
            if (_theme is not null)
                _theme.ReduceMotion = Tri(Motion.SelectedIndex);
        };
        Transparency.SelectionChanged += (_, _) =>
        {
            if (_theme is not null)
                _theme.ReduceTransparency = Tri(Transparency.SelectedIndex);
        };
        AccentList.SelectionChanged += (_, _) =>
        {
            if (!_syncing && AccentList.SelectedIndex >= 0)
                AccentHex.Text = Accents[AccentList.SelectedIndex].Hex;
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
            .Subscribe(new AnonymousObserver<Color>(c => _swatchChip.Background = new SolidColorBrush(c)));
        Band.DetachedFromVisualTree += (_, _) => accentWatch?.Dispose();
    }

    /// <summary>The band, for <c>StandardsWindow.ToolbarBand</c>.</summary>
    public ToolbarBand Band { get; }

    public SegmentedControl Theme { get; }

    /// <summary>The accent swatch: a button showing the accent, opening <see cref="AccentFlyout"/>.</summary>
    public Button Accent { get; }

    public Flyout AccentFlyout { get; }

    public ListBox AccentList { get; }

    public TextBox AccentHex { get; }

    public SegmentedControl Density { get; }

    public SegmentedControl Motion { get; }

    public SegmentedControl Transparency { get; }

    private static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    // Follow OS (null), Reduced (true), Full (false).
    private static bool? Tri(int index) => index switch { 1 => true, 2 => false, _ => null };

    private static int Index(bool? value) => value switch { true => 1, false => 2, null => 0 };

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

    // A colour chip: a squircle with a hairline edge, as the Colours page's swatches.
    private static SquircleBorder Chip()
    {
        var chip = new SquircleBorder { CornerRadius = ChipRadius, BorderThickness = new Thickness(1) };
        chip.Bind(SquircleBorder.BorderBrushProperty, new DynamicResourceExtension("SystemControlForegroundBaseLowBrush"));
        return chip;
    }

    /// <summary>
    /// A subtle button, square at the button height as the band's icon buttons, holding the accent chip at the icon
    /// size (20, 16 Compact): the chip covers an invisible <see cref="Icon"/>, which takes the size from the theme.
    /// </summary>
    private Button Swatch()
    {
        var button = new Button
        {
            Content = new Panel { Children = { new Icon { Opacity = 0 }, _swatchChip } },
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Flyout = AccentFlyout,
        };
        button.Classes.Add("subtle");
        button.Bind(Layoutable.MinWidthProperty, button.GetObservable(Layoutable.MinHeightProperty));
        ToolTip.SetTip(button, "Accent colour");
        AutomationProperties.SetName(button, "Accent colour");
        return button;
    }

    private static Control AccentRow(string name, string hex)
    {
        var chip = Chip();
        chip.Width = chip.Height = ListChipSize;
        chip.Background = new SolidColorBrush(Color.Parse(hex));
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { chip, new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center } },
        };
        AutomationProperties.SetName(row, name);
        return row;
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
        _syncing = true;
        AccentList.SelectedIndex = Array.FindIndex(Accents, a => a.Hex.Equals(text, StringComparison.OrdinalIgnoreCase));
        _syncing = false;
    }
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
