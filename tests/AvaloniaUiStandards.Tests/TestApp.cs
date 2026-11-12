using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace AvaloniaUiStandards.Tests;

public class TestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
