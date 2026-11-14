using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Catalog;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// Drives the catalog's main window as a user would: every sidebar page in turn, and every
/// toolbar band setting (theme, density, accent, reduce motion, reduce transparency), with the
/// application-wide theme as the app has it.
/// </summary>
public class CatalogWindowTests
{
    [AvaloniaFact]
    public void MainWindow_SurvivesPagesAndToolbarBandChanges()
    {
        var app = Application.Current!;
        var theme = app.Styles.OfType<StandardsTheme>().Single();
        var variant = app.RequestedThemeVariant;
        var window = new MainWindow();
        try
        {
            window.Show();
            Pump();
            var band = window.ThemeControls;

            VisitAllPages(window);
            foreach (var themeIndex in new[] { 2, 1, 0, 2 })
            {
                band.Theme.SelectedIndex = themeIndex;
                Pump();
                VisitAllPages(window);
            }

            foreach (var (box, indices) in new (SelectingItemsControl, int[])[]
                     {
                         (band.Density, [1, 0, 1]),
                         (band.Motion, [1, 2, 0]),
                         (band.Transparency, [1, 2, 0]),
                     })
                foreach (var index in indices)
                {
                    box.SelectedIndex = index;
                    Pump();
                    VisitAllPages(window);
                }

            // Every accent preset, then back to the default, as choosing them in the accent menu does.
            foreach (var preset in band.AccentPresets.Append(band.AccentPresets[0]))
            {
                preset.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Pump();
                VisitAllPages(window);
            }

            // The intent: after all of it, the Button page's buttons are alive and still animate.
            window.SelectPage(CatalogPages.All.ToList().FindIndex(p => p.Title == "Button"));
            Pump();
            var chromes = CatalogRenderTests.ContentScroller(window).GetVisualDescendants().OfType<SquircleBorder>().Where(c => c.Name == ButtonParts.ChromePart).ToList();
            Assert.NotEmpty(chromes);
            Assert.All(chromes, c => Assert.Equal(3, c.Transitions?.Count));
        }
        finally
        {
            window.Close();
            app.RequestedThemeVariant = variant;
            theme.Density = DensityStyle.Normal;
            theme.Accent = StandardsTheme.DefaultAccent;
            theme.ReduceMotion = null;
            theme.ReduceTransparency = null;
        }
    }

    private static void VisitAllPages(MainWindow window)
    {
        for (var i = 0; i < CatalogPages.All.Count; i++)
        {
            window.SelectPage(i);
            Pump();
        }
    }

    private static void Pump()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
