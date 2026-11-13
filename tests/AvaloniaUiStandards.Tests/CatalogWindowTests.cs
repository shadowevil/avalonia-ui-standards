using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
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
/// toolbar setting (theme, density, accent, reduce motion, reduce transparency), with the
/// application-wide theme as the app has it.
/// </summary>
public class CatalogWindowTests
{
    [AvaloniaFact]
    public void MainWindow_SurvivesPagesAndToolbarChanges()
    {
        var app = Application.Current!;
        var theme = app.Styles.OfType<StandardsTheme>().Single();
        var variant = app.RequestedThemeVariant;
        var window = new MainWindow();
        try
        {
            window.Show();
            Pump();
            var toolbar = window.GetVisualDescendants().OfType<ThemeToolbar>().Single();

            VisitAllPages(window);
            foreach (var themeIndex in new[] { 2, 1, 0, 2 })
            {
                toolbar.ThemeBox.SelectedIndex = themeIndex;
                Pump();
                VisitAllPages(window);
            }

            foreach (var (box, indices) in new (ComboBox, int[])[]
                     {
                         (toolbar.DensityBox, [1, 0, 1]),
                         (toolbar.AccentBox, Enumerable.Range(0, toolbar.AccentBox.ItemCount).Concat([0]).ToArray()),
                         (toolbar.MotionBox, [1, 2, 0]),
                         (toolbar.TransparencyBox, [1, 2, 0]),
                     })
                foreach (var index in indices)
                {
                    box.SelectedIndex = index;
                    Pump();
                    VisitAllPages(window);
                }

            // The intent: after all of it, the shown page's buttons are alive and still animate.
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
            window.PageList.SelectedIndex = i;
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
