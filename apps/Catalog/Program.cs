using Avalonia;
using AvaloniaUiStandards;

namespace Catalog;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseUiStandards()
            .StartWithClassicDesktopLifetime(args);
}
