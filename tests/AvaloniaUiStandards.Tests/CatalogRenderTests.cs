using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Catalog;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// Renders the whole catalog window on each page, in light and dark, at its default size and at its minimum, and each
/// page whole at a wide and a narrow width, at 2x (screenshots/catalog-*.png), and checks the page in the window's own
/// content scroller: it wraps to the width, nothing runs past it and no text is clipped; the toolbar stays inside the
/// title band.
/// </summary>
public class CatalogRenderTests
{
    // The window at its default size (sidebar expanded), at a medium width (the sidebar's icon rail) and at its minimum
    // (the 480 x 320 frame plus the shadow margin; the sidebar hidden).
    private static readonly (Size Size, string Suffix)[] WindowSizes =
        [(new Size(1200, 800), ""), (new Size(848, 600), "-medium"), (new Size(528, 368), "-min")];

    [AvaloniaFact]
    public void Window_RendersEveryPageInLightAndDark()
    {
        var problems = new List<string>();
        foreach (var (label, variant) in ButtonRenderTests.Themes)
        foreach (var (size, suffix) in WindowSizes)
            for (var i = 0; i < CatalogPages.All.Count; i++)
            {
                var title = CatalogPages.All[i].Title;
                var name = $"catalog-window-{title.ToLowerInvariant().Replace(' ', '-')}-{label.ToLowerInvariant()}{suffix}.png";
                var window = new MainWindow
                {
                    Width = size.Width,
                    Height = size.Height,
                    RequestedThemeVariant = variant,
                    // Headless frames have no alpha, so a desktop-like grey stands in for what is behind the transparent window.
                    Background = new SolidColorBrush(Color.Parse(variant == ThemeVariant.Dark ? "#3A3D42" : "#C9CDD3")),
                };
                window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = title != "Motion" });
                window.SetRenderScaling(Sheet.Scale);
                window.Show();
                window.Activate();
                window.PageList.SelectedIndex = i;
                Settle(window);

                problems.AddRange(Check(ContentScroller(window)).Concat(CheckToolbar(window)).Select(p => $"{name}: {p}"));
                using var frame = Frame.Capture(window);
                frame.Save(name);
                window.Close();
            }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    // A window is as wide as its content plus the shadow margin and the panel's side margins.
    private static readonly double ShadowAndPanelMargins = StandardsWindowTheme.ShadowMargin.Left + StandardsWindowTheme.ShadowMargin.Right + 2 * Spacing.S;

    // Content widths for the whole-page renders: a wide window, and an 800 px window less its 180 px sidebar.
    private static readonly (double Width, string Suffix)[] ContentWidths = [(1000, ""), (620, "-narrow")];

    [AvaloniaFact]
    public void Pages_RenderWholeInLightAndDark()
    {
        // Each page in a window tall enough for all of it, with the real content scroller, at a wide and a narrow content
        // width; and the toolbar in the title band at the same widths. The window is as wide as its content plus the
        // shadow margin and the panel's side margins.
        var problems = new List<string>();
        foreach (var (label, variant) in ButtonRenderTests.Themes)
        foreach (var (width, suffix) in ContentWidths)
        {
            foreach (var page in CatalogPages.All)
            {
                var name = $"catalog-page-{page.Title.ToLowerInvariant().Replace(' ', '-')}-{label.ToLowerInvariant()}{suffix}.png";
                var window = NewHost(variant, width + ShadowAndPanelMargins, reduceMotion: page.Title != "Motion");
                window.Content = page.Create();
                window.Show();
                Settle(window);

                // Tall enough for the whole page: its extent, plus the title band, the panel's bottom margin and the shadow margin.
                var scroller = ContentScroller(window);
                // The scroller's extent fills its viewport, so the page's own desired height (plus the panel's 12 px padding) is used.
                var chrome = window.Height - scroller.Viewport.Height;
                window.Height = Math.Ceiling(((Control)window.Content!).DesiredSize.Height + 24 + chrome);
                Settle(window);
                for (var attempt = 0; attempt < 2 && scroller.Extent.Height > scroller.Viewport.Height + 0.5; attempt++)
                {
                    window.Height += Math.Ceiling(scroller.Extent.Height - scroller.Viewport.Height) + 1;
                    Settle(window);
                }

                problems.AddRange(Check(scroller).Select(p => $"{name}: {p}"));
                using var frame = Frame.Capture(window);
                frame.Save(name);
                window.Close();
            }

            var toolbarName = $"catalog-toolbar-{label.ToLowerInvariant()}{suffix}.png";
            var toolbarWindow = NewHost(variant, width + ShadowAndPanelMargins, reduceMotion: true);
            toolbarWindow.Height = 200;
            toolbarWindow.Toolbar = new ThemeToolbar();
            toolbarWindow.Content = new TextBlock { Text = "Toolbar" };
            toolbarWindow.Show();
            Settle(toolbarWindow);
            problems.AddRange(CheckToolbar(toolbarWindow).Select(p => $"{toolbarName}: {p}"));
            using (var frame = Frame.Capture(toolbarWindow))
                frame.Save(toolbarName);
            toolbarWindow.Close();
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    // A bare library window for hosting a page: a grey stand-in for what is behind it, its own fixed theme, rendered at 2x.
    private static StandardsWindow NewHost(ThemeVariant variant, double width, bool reduceMotion)
    {
        var window = new StandardsWindow
        {
            Title = "Catalog page",
            Width = width,
            Height = 3000,
            RequestedThemeVariant = variant,
            Background = new SolidColorBrush(Color.Parse(variant == ThemeVariant.Dark ? "#3A3D42" : "#C9CDD3")),
        };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion });
        window.SetRenderScaling(Sheet.Scale);
        return window;
    }

    /// <summary>The toolbar's visible content must sit inside the title band, after the title.</summary>
    private static IEnumerable<string> CheckToolbar(Window window)
    {
        var toolbar = window.GetVisualDescendants().OfType<ThemeToolbar>().Single();
        var band = window.GetVisualDescendants().OfType<Control>().First(c => c.Name == StandardsWindowTheme.TitleBandPart);
        var title = window.GetVisualDescendants().OfType<TextBlock>().First(c => c.Name == StandardsWindowTheme.TitlePart);
        var content = toolbar.GetVisualDescendants().OfType<Control>().First(c => c is StackPanel { Name: "Row" } or Button { Name: "MoreButton" } && c.IsVisible);

        var left = content.TranslatePoint(new Point(0, 0), window)!.Value.X;
        var right = content.TranslatePoint(new Point(content.Bounds.Width, 0), window)!.Value.X;
        var bandRight = band.TranslatePoint(new Point(band.Bounds.Width, 0), window)!.Value.X;
        var titleRight = title.TranslatePoint(new Point(title.Bounds.Width, 0), window)!.Value.X;
        if (right > bandRight + 0.5)
            yield return $"toolbar runs past the title band ({right:0} > {bandRight:0})";
        if (left < titleRight - 0.5)
            yield return $"toolbar overlaps the title ({left:0} < {titleRight:0})";
    }

    /// <summary>The window's content scroller (the glass panel's), where the page is shown.</summary>
    internal static ScrollViewer ContentScroller(Window window) =>
        window.GetVisualDescendants().OfType<ScrollViewer>().First(c => c.Name == StandardsWindowTheme.ContentScrollViewerPart);

    /// <summary>
    /// The page must wrap to the window's content scroller's viewport: its extent is no wider, and every visible control
    /// ends inside it. Controls below a nested scroller (a state grid) are exempt, since they scroll; the nested
    /// scroller itself is not. Text must fit its box, and wrapped siblings must not overlap.
    /// </summary>
    private static IEnumerable<string> Check(ScrollViewer host)
    {
        var viewport = host.Viewport.Width;
        if (host.Extent.Width > viewport + 0.5)
            yield return $"page is wider than its viewport (extent {host.Extent.Width:0} > viewport {viewport:0})";

        // The page and what is in it, not the scroller's own parts (its scroll bars).
        var content = host.GetVisualDescendants().OfType<ScrollContentPresenter>().First();
        foreach (var control in content.GetVisualDescendants().OfType<Control>())
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
    private static bool InNestedScroller(Control control, ScrollViewer host) =>
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
