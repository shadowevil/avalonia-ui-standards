using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Hosts a picker or calendar in a window for the theme tests, and resolves theme resources.</summary>
internal static class PickerHost
{
    public static (Window Window, T Control) Show<T>(T control, ThemeVariant? variant = null, StandardsTheme? theme = null, double height = 200) where T : Control
    {
        var window = new Window { Width = 400, Height = height, RequestedThemeVariant = variant ?? ThemeVariant.Light };
        if (theme is not null)
            window.Styles.Add(theme);
        window.Content = new Border { Padding = new Thickness(20), Child = control, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        window.Show();
        Pump(window);
        return (window, control);
    }

    public static object Resolve(Control control, string key)
    {
        Assert.True(control.TryFindResource(key, control.ActualThemeVariant, out var value), key);
        return value!;
    }

    public static void Pump(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
