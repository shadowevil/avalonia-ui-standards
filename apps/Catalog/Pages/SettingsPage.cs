using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using AvaloniaUiStandards;

namespace Catalog.Pages;

/// <summary>The catalog's own settings: high refresh rate, app-wide through <see cref="StandardsTheme.HighRefreshRate"/>.</summary>
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

        Content = Ui.Page("Settings",
            Ui.Section("High refresh rate", new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    HighRefreshRate,
                    Ui.Caption("Animate continuous effects at the display's full refresh rate instead of 30 fps. Uses more power."),
                },
            }));
    }

    /// <summary>The high refresh rate switch.</summary>
    internal ToggleSwitch HighRefreshRate { get; }
}
