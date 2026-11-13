using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;
using Catalog;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// Renders each catalog page and the toolbar, in light and dark, at 2x (screenshots/catalog-*.png), at
/// a normal and a narrow page width, hosted in the same page scroller as the app, and checks that the
/// page wraps to the width and no text is clipped.
/// </summary>
public class CatalogRenderTests
{
    // Page viewport widths: a wide window, and an 800 px window less its 180 px sidebar.
    private static readonly (double Width, string Suffix)[] Widths = [(1000, ""), (620, "-narrow")];

    [AvaloniaFact]
    public void Pages_RenderInLightAndDark()
    {
        var problems = new List<string>();
        foreach (var (label, variant) in ButtonRenderTests.Themes)
        foreach (var (width, suffix) in Widths)
        {
            foreach (var page in CatalogPages.All)
            {
                var name = $"catalog-{page.Title.ToLowerInvariant().Replace(' ', '-')}-{label.ToLowerInvariant()}{suffix}.png";
                // End states are shown, so transitions are made instant; headless animation timing is
                // not dependable. The Motion page shows the real durations.
                problems.AddRange(Render(page.Create(), variant, name, width, reduceMotion: page.Title != "Motion"));
            }

            problems.AddRange(Render(new ThemeToolbar { Margin = new Thickness(16) }, variant,
                $"catalog-toolbar-{label.ToLowerInvariant()}{suffix}.png", width, reduceMotion: true));
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    private static List<string> Render(Control content, ThemeVariant variant, string name, double width, bool reduceMotion)
    {
        var host = new PageHost { Content = content };
        var root = new Border { Child = host };
        root.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemRegionColor"));
        var window = new Window { Width = width, Height = 4000, RequestedThemeVariant = variant, Content = root };
        // Each window has its own theme with fixed settings, so the shared test theme is never changed.
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion });
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        Settle(window);

        // Sized to the page's full height, so the capture shows all of it without scrolling.
        window.Height = Math.Ceiling(host.Extent.Height);
        Settle(window);

        var problems = Check(host).Select(p => $"{name}: {p}").ToList();
        using var frame = Frame.Capture(window);
        frame.Save(name);
        window.Close();
        return problems;
    }

    /// <summary>
    /// The page must wrap to the page scroller's viewport: its extent is no wider, and every visible control
    /// ends inside it. Controls below a nested scroller (a state grid) are exempt, since they scroll; the nested
    /// scroller itself is not. Text must fit its box, and wrapped siblings must not overlap.
    /// </summary>
    private static IEnumerable<string> Check(PageHost host)
    {
        var viewport = host.Viewport.Width;
        if (host.Extent.Width > viewport + 0.5)
            yield return $"page is wider than its viewport (extent {host.Extent.Width:0} > viewport {viewport:0})";

        var content = (Control)host.Content!;
        foreach (var control in content.GetSelfAndVisualDescendants().OfType<Control>())
        {
            if (!control.IsVisible || control.Bounds.Width <= 0 || InNestedScroller(control, host))
                continue;

            var right = control.TranslatePoint(new Point(control.Bounds.Width, 0), host)?.X ?? 0;
            if (right > viewport + 0.5)
                yield return $"{Describe(control)} runs past the viewport ({right:0} > {viewport:0})";

            if (control is TextBlock { Text: { Length: > 0 } text, TextWrapping: TextWrapping.NoWrap } block)
            {
                var natural = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(block.FontFamily, block.FontStyle, block.FontWeight), block.FontSize, null).Width;
                if (natural > block.Bounds.Width + 1)
                    yield return $"\"{text}\" is clipped ({block.Bounds.Width:0} wide, needs {natural:0})";
            }

            if (control is WrapPanel wrap)
            {
                var items = wrap.Children.Where(c => c.IsVisible).ToList();
                for (var i = 0; i < items.Count; i++)
                    for (var j = i + 1; j < items.Count; j++)
                        if (items[i].Bounds.Deflate(0.5).Intersects(items[j].Bounds.Deflate(0.5)))
                            yield return $"wrapped items overlap ({Describe(items[i])} and {Describe(items[j])})";
            }
        }
    }

    // True when the control is below a scroller other than the page scroller.
    private static bool InNestedScroller(Control control, PageHost host) =>
        control.GetVisualAncestors().TakeWhile(a => a != host).OfType<ScrollViewer>().Any();

    private static string Describe(Control c) => c is TextBlock { Text: { } t } ? $"\"{t}\"" : c.GetType().Name;

    private static void Settle(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
