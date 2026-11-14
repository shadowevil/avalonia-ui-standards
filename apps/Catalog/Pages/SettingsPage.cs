using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using AvaloniaUiStandards;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;

namespace Catalog.Pages;

/// <summary>
/// The catalog's own settings: high refresh rate, app-wide through <see cref="StandardsTheme.HighRefreshRate"/>, and the
/// layout mode, applied to the main window's <see cref="StandardsWindow.LayoutMode"/>.
/// </summary>
internal sealed class SettingsPage : UserControl
{
    public SettingsPage()
    {
        var theme = Application.Current?.Styles.OfType<StandardsTheme>().FirstOrDefault();
        HighRefreshRate = new ToggleSwitch { IsChecked = theme?.HighRefreshRate ?? false };
        AutomationProperties.SetName(HighRefreshRate, "High refresh rate");
        HighRefreshRate.IsCheckedChanged += (_, _) =>
        {
            if (theme is not null)
                theme.HighRefreshRate = HighRefreshRate.IsChecked == true;
        };

        var app = Application.Current;
        LayoutMode = new SegmentedControl { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        foreach (var label in LayoutModeLabels)
            LayoutMode.Items.Add(new SegmentedControlItem { Content = label });
        LayoutMode.SelectedIndex = (int)(app is null ? WindowLayoutMode.Automatic : CatalogLayout.Get(app));
        AutomationProperties.SetName(LayoutMode, "Layout mode");
        LayoutMode.SelectionChanged += (_, _) =>
        {
            if (app is not null && LayoutMode.SelectedIndex >= 0)
                CatalogLayout.Set(app, (WindowLayoutMode)LayoutMode.SelectedIndex);
        };

        Content = Ui.Page("Settings",
            Ui.Section("High refresh rate", new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    HighRefreshRate,
                    Ui.Caption("Animate continuous effects at the display's full refresh rate instead of 30 fps. Uses more power."),
                },
            }),
            Ui.Section("Layout mode", new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    LayoutMode,
                    Ui.Caption("The window layout the catalog shows: its window buttons, menu bar and title band. Automatic uses this computer's; macOS and Windows show that system's on any computer."),
                },
            }));
    }

    /// <summary>The layout mode choices, in the order of <see cref="WindowLayoutMode"/>.</summary>
    internal static readonly string[] LayoutModeLabels = ["Automatic", "macOS", "Windows"];

    /// <summary>The layout mode control: Automatic, macOS, Windows.</summary>
    internal SegmentedControl LayoutMode { get; }

    /// <summary>The high refresh rate switch.</summary>
    internal ToggleSwitch HighRefreshRate { get; }
}
