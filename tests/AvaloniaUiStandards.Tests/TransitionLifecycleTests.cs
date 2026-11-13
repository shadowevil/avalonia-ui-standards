using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// Library buttons with live transitions survive leaving and re-entering the tree, and theme
/// and density changes, the way the catalog swaps its pages.
/// </summary>
public class TransitionLifecycleTests
{
    // Every Button variant and sibling, in a few states, as a page of controls.
    private static Control Page()
    {
        var panel = new WrapPanel();
        foreach (var cls in new string?[] { null, "accent", "subtle", "destructive" })
        {
            var button = new Button { Content = cls ?? "Standard" };
            if (cls is not null)
                button.Classes.Add(cls);
            panel.Children.Add(button);
        }
        panel.Children.Add(new RepeatButton { Content = "Repeat" });
        panel.Children.Add(new ToggleButton { Content = "Toggle" });
        panel.Children.Add(new ToggleButton { Content = "Checked", IsChecked = true });
        panel.Children.Add(new ToggleButton { Content = "Indeterminate", IsThreeState = true, IsChecked = null });
        panel.Children.Add(new HyperlinkButton { Content = "Link" });
        panel.Children.Add(new DropDownButton { Content = "Drop down" });
        panel.Children.Add(new SplitButton { Content = "Split" });
        panel.Children.Add(new ToggleSplitButton { Content = "Toggle split", IsChecked = true });
        panel.Children.Add(new Button { Content = "Disabled", IsEnabled = false });
        return panel;
    }

    private static (Window Window, ContentControl Host, StandardsTheme Theme) Show(ThemeVariant variant, bool scrollViewer)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        ContentControl host = scrollViewer ? new ScrollViewer() : new ContentControl();
        host.Content = Page();
        var window = new Window { Width = 800, Height = 600, RequestedThemeVariant = variant, Content = host };
        window.Styles.Add(theme);
        window.Show();
        Pump();
        return (window, host, theme);
    }

    private static void Pump()
    {
        Dispatcher.UIThread.RunJobs();
        Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    // The intent of each test: the page's buttons are alive and still animate (all their
    // chromes have the theme's transitions: background, border brush and pressed scale).
    private static void AssertChromesHaveTransitions(Control page)
    {
        var chromes = page.GetVisualDescendants().OfType<SquircleBorder>().Where(c => c.Name == ButtonParts.ChromePart).ToList();
        Assert.NotEmpty(chromes);
        Assert.All(chromes, c => Assert.Equal(3, c.Transitions?.Count));
    }

    // (a) The catalog's page swap: the old page's controls leave the tree (and its inherited theme
    // variant), then it is shown again.
    [AvaloniaTheory]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Dark", true)]
    public void SwappingContent_DetachesButtonsSafely(string variant, bool scrollViewer)
    {
        var (window, host, _) = Show(variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, scrollViewer);
        try
        {
            var first = (Control)host.Content!;
            host.Content = Page();
            Pump();
            host.Content = new TextBlock { Text = "Another page" };
            Pump();
            host.Content = first;
            Pump();

            AssertChromesHaveTransitions(first);
        }
        finally
        {
            window.Close();
        }
    }

    // (b) Theme variant changes while the page is detached, then it is attached again.
    [AvaloniaFact]
    public void VariantChangeWhileDetached_ThenReattached()
    {
        var (window, host, _) = Show(ThemeVariant.Dark, scrollViewer: true);
        try
        {
            var page = (Control)host.Content!;
            host.Content = new TextBlock { Text = "Another page" };
            Pump();
            window.RequestedThemeVariant = ThemeVariant.Light;
            Pump();
            host.Content = page;
            Pump();
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Pump();

            AssertChromesHaveTransitions(page);
        }
        finally
        {
            window.Close();
        }
    }

    // (c) Theme variant switches while attached.
    [AvaloniaFact]
    public void VariantSwitchWhileAttached()
    {
        var (window, host, _) = Show(ThemeVariant.Light, scrollViewer: true);
        try
        {
            foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light, ThemeVariant.Default, ThemeVariant.Dark })
            {
                window.RequestedThemeVariant = variant;
                Pump();
            }

            AssertChromesHaveTransitions((Control)host.Content!);
        }
        finally
        {
            window.Close();
        }
    }

    // (d) Density, accent and reduce motion switch while attached.
    [AvaloniaFact]
    public void DensityAccentAndMotionSwitchWhileAttached()
    {
        var (window, host, theme) = Show(ThemeVariant.Dark, scrollViewer: true);
        try
        {
            theme.Density = DensityStyle.Compact;
            Pump();
            theme.Accent = Avalonia.Media.Color.Parse("#E0720B");
            Pump();
            theme.ReduceMotion = true;
            Pump();
            theme.Density = DensityStyle.Normal;
            theme.ReduceMotion = false;
            Pump();

            AssertChromesHaveTransitions((Control)host.Content!);
        }
        finally
        {
            window.Close();
        }
    }
}
