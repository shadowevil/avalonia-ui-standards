using Avalonia;
using Avalonia.Headless;
using AvaloniaUiStandards.Style;
using AvaloniaUiStandards.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace AvaloniaUiStandards.Tests;

public class TestApp : Application
{
    public override void Initialize()
    {
        // Fixed "not reduced" settings, so renders do not depend on the machine running the tests.
        Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced));
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseUiStandards()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
