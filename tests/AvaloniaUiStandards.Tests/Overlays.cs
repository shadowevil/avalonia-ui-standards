using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Tests;

/// <summary>Helpers for showing overlays (menus, flyouts, tooltips, notifications) in headless windows and capturing them.</summary>
internal static class Overlays
{
    /// <summary>
    /// A window over the backdrop wash, with lines of text behind where overlays open so the glass
    /// has something to blur. Its own theme has reduce motion on, so fades do not affect captures.
    /// </summary>
    public static Window Host(ThemeVariant variant, Control content, double width, double height, bool reduceMotion = true)
    {
        var lines = new StackPanel { Margin = new Thickness(12, 8) };
        for (var i = 0; i < 30; i++)
            lines.Children.Add(new TextBlock { Text = "Behind the overlay: text, lines and shapes in the window", Opacity = 0.6 });
        var stripes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18, Margin = new Thickness(40, 0), HorizontalAlignment = HorizontalAlignment.Left };
        for (var i = 0; i < 12; i++)
            stripes.Children.Add(new Border { Width = 8, Background = new SolidColorBrush(Color.FromArgb(0x60, 0x00, 0x7C, 0xEA)) });

        var root = new Panel { Children = { lines, stripes, content } };
        var wash = new Border { Child = root };
        wash.Bind(Border.BackgroundProperty, new DynamicResourceExtension("BackdropWash"));

        var window = new Window { Width = width, Height = height, RequestedThemeVariant = variant };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion });
        window.Content = wash;
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        Pump(window);
        return window;
    }

    public static void Pump(TopLevel top)
    {
        for (var i = 0; i < 2; i++)
        {
            Dispatcher.UIThread.RunJobs();
            top.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>The window as rendered, popups included (headless popups live in its overlay layer).</summary>
    public static Control Capture(Sheet sheet, Window window)
    {
        Pump(window);
        using var capture = window.CaptureRenderedFrame()!;
        return sheet.Image(capture);
    }
}
