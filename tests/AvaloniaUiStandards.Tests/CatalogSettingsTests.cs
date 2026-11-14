using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Catalog;
using Catalog.Pages;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>The catalog's Settings page: its high refresh rate switch sets the app-wide default, and the readout follows.</summary>
public class CatalogSettingsTests
{
    [AvaloniaFact]
    public void HighRefreshRateSwitch_SetsTheAppDefault_AndTheReadoutFollows()
    {
        var theme = Application.Current!.Styles.OfType<StandardsTheme>().Single();
        var window = new MainWindow { Width = 1200, Height = 800 };
        try
        {
            theme.ReduceMotion = false;
            window.Show();
            window.Activate();
            window.SelectPage(CatalogPages.All.ToList().FindIndex(p => p.Title == "Settings"));
            Settle(window);
            // Settings is the sidebar's footer row.
            Assert.Equal(0, window.PageList.SelectedFooterIndex);
            Assert.Equal(-1, window.PageList.SelectedIndex);
            var page = Assert.IsType<SettingsPage>(window.Content);
            Assert.False(page.HighRefreshRate.IsChecked);
            Assert.EndsWith("High refresh rate: off", window.StatusFrames.Text);

            page.HighRefreshRate.IsChecked = true;
            Settle(window);
            Assert.True(theme.HighRefreshRate);
            Assert.True(window.HighRefreshRate);
            Assert.EndsWith("High refresh rate: on", window.StatusFrames.Text);

            page.HighRefreshRate.IsChecked = false;
            Settle(window);
            Assert.False(theme.HighRefreshRate);
            Assert.EndsWith("High refresh rate: off", window.StatusFrames.Text);
        }
        finally
        {
            window.Close();
            theme.HighRefreshRate = false;
            theme.ReduceMotion = null;
        }
    }

    private static void Settle(MainWindow window)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }
}
