using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Reactive;
using AvaloniaUiStandards;
using AvaloniaUiStandards.Modules;

namespace Catalog;

public partial class MainWindow : StandardsWindow
{
    private readonly FrameRateMeter _meter;
    private bool _motionReduced;

    public MainWindow()
    {
        InitializeComponent();
        // The theme controls, in the toolbar band below the title band.
        ThemeControls = new ThemeBand();
        ToolbarBand = ThemeControls.Band;
        StatusVersion.Text = AppInfo.LibraryText;

        // The menus: in the system menu bar on macOS, in the window's menu row in the Windows layout. The layout
        // follows the Settings page's layout mode.
        Menus = new CatalogMenu(this);
        NativeMenu.SetMenu(this, Menus.Menu);
        if (Application.Current is { } app)
            this.Bind(LayoutModeProperty, app.GetObservable(CatalogLayout.LayoutModeProperty));

        PageList.ItemsSource = CatalogPages.List.Select(p => new SidebarItem { Icon = p.Icon, Content = p.Title }).ToList();
        PageList.FooterItems.AddRange(CatalogPages.Footer.Select(p => new SidebarItem { Icon = p.Icon, Content = p.Title }));
        PageList.SelectionChanged += (_, _) =>
        {
            var page = PageList.SelectedIndex >= 0 ? CatalogPages.List[PageList.SelectedIndex]
                : PageList.SelectedFooterIndex >= 0 ? CatalogPages.Footer[PageList.SelectedFooterIndex]
                : null;
            if (page is null)
                return;
            Content = page.Create();
            StatusPage.Text = page.Title;
            UpdateFrameStatus();
        };
        PageList.SelectedIndex = 0;

        // The ribbons tone themselves down outside the glass panel, where text sits on the backdrop.
        Ribbons.Bind(RibbonBackdrop.PanelBoundsProperty, this.GetObservable(ContentPanelBoundsProperty));

        // The frame readout: rendered frames per second, counted without causing any, and the frame tick state.
        _meter = new FrameRateMeter(this, _ => UpdateFrameStatus());
        Opened += (_, _) => _meter.Attach();
        Closed += (_, _) => _meter.Dispose();
        SizeChanged += (_, _) => _meter.Resize();
        StatusRow.SizeChanged += (_, _) => UpdateFrameStatus();
        // Reduce motion, as the theme applies it: the motion durations are zero.
        this.GetResourceObservable("MotionNormal").Subscribe(new AnonymousObserver<object?>(v =>
        {
            _motionReduced = v is TimeSpan t && t == TimeSpan.Zero;
            UpdateFrameStatus();
        }));
        UpdateFrameStatus();
    }

    internal FrameRateMeter FrameRateMeter => _meter;

    internal ThemeBand ThemeControls { get; }

    internal CatalogMenu Menus { get; }

    /// <summary>The index in <see cref="CatalogPages.All"/> of the page shown.</summary>
    internal int SelectedPage => PageList.SelectedIndex >= 0 ? PageList.SelectedIndex
        : PageList.SelectedFooterIndex >= 0 ? CatalogPages.List.Count + PageList.SelectedFooterIndex : -1;

    /// <summary>Shows the page at <paramref name="index"/> in <see cref="CatalogPages.All"/>, by selecting its sidebar row (list or footer).</summary>
    internal void SelectPage(int index)
    {
        if (index < CatalogPages.List.Count)
            PageList.SelectedIndex = index;
        else
            PageList.SelectedFooterIndex = index - CatalogPages.List.Count;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HighRefreshRateProperty || change.Property == IsActiveProperty || change.Property == WindowStateProperty)
            UpdateFrameStatus();
    }

    // The longest readout that fits beside the page name and the version; the shortest when none does.
    private void UpdateFrameStatus()
    {
        // Called from base constructors' property changes before the fields exist.
        if (_meter is null || StatusFrames is null)
            return;
        var texts = FrameStatus.Texts(_meter.FramesPerSecond, HighRefreshRate, FrameStatus.PauseReason(_motionReduced, IsActive, WindowState));
        var room = StatusRow.Bounds.Width - 2 * StatusRow.ColumnSpacing - TextWidth(StatusPage, StatusPage.Text) - TextWidth(StatusVersion, StatusVersion.Text);
        StatusFrames.Text = texts.FirstOrDefault(t => TextWidth(StatusFrames, t) <= room) ?? texts[^1];
    }

    private static double TextWidth(TextBlock block, string? text) =>
        string.IsNullOrEmpty(text) ? 0 : new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(block.FontFamily, block.FontStyle, block.FontWeight), block.FontSize, null).Width;
}
