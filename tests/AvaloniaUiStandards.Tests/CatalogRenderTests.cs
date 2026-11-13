using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaUiStandards.Style;
using Catalog;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders each catalog page and the toolbar, in light and dark, at 2x (screenshots/catalog-*.png).</summary>
public class CatalogRenderTests
{
    private const double Width = 1200;

    [AvaloniaFact]
    public void Pages_RenderInLightAndDark()
    {
        foreach (var (label, variant) in ButtonRenderTests.Themes)
        {
            foreach (var page in CatalogPages.All)
                // End states are shown, so transitions are made instant; headless animation timing is
                // not dependable. The Motion page shows the real durations.
                Render(page.Create(), variant, $"catalog-{page.Title.ToLowerInvariant().Replace(' ', '-')}-{label.ToLowerInvariant()}.png", reduceMotion: page.Title != "Motion");

            Render(new ThemeToolbar { Margin = new Thickness(16) }, variant, $"catalog-toolbar-{label.ToLowerInvariant()}.png", reduceMotion: true);
        }
    }

    private static void Render(Control content, ThemeVariant variant, string name, bool reduceMotion)
    {
        var root = new Border { Child = content };
        root.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemRegionColor"));
        var window = new Window { Width = Width, Height = 4000, RequestedThemeVariant = variant, Content = root };
        // Each window has its own theme with fixed settings, so the shared test theme is never changed.
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion });
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        Settle(window);

        // Sized to the laid-out content, because SizeToContent is limited to the headless screen size.
        root.Measure(new Size(Width, double.PositiveInfinity));
        window.Height = Math.Ceiling(root.DesiredSize.Height);
        Settle(window);

        using var frame = Frame.Capture(window);
        frame.Save(name);
        window.Close();
    }

    private static void Settle(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
