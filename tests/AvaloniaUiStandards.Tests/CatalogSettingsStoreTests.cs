using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using AvaloniaUiStandards.Style;
using Catalog;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// The catalog's saved settings: a JSON file read at startup and written after changes, with a missing or corrupt
/// file falling back to the defaults. Each test uses its own file in the test output, never the user's folder.
/// </summary>
public class CatalogSettingsStoreTests
{
    // A fresh folder in the test output for one test's settings file; the test deletes it.
    private static string NewPath() =>
        Path.Combine(AppContext.BaseDirectory, "settings-tests", Guid.NewGuid().ToString("N"), "settings.json");

    private static void DeleteFolder(string path)
    {
        var folder = Path.GetDirectoryName(path)!;
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    [AvaloniaFact]
    public void DefaultPath_IsThePerUserAppDataFolder()
    {
        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AvaloniaUiStandards.Catalog", "settings.json"),
            CatalogSettings.DefaultPath);
        if (OperatingSystem.IsMacOS())
            Assert.Contains("/Library/Application Support/", CatalogSettings.DefaultPath);
    }

    [AvaloniaFact]
    public void Settings_RoundTripThroughTheFile()
    {
        var path = NewPath();
        var saved = new CatalogSettings
        {
            Theme = "Dark",
            Density = "Compact",
            Accent = "#7A4FD6",
            ReduceMotion = true,
            ReduceTransparency = false,
            HighRefreshRate = true,
        };
        try
        {
            saved.Save(path);
            Assert.Equal(saved, CatalogSettings.Load(path));
            Assert.False(File.Exists(path + ".tmp"), "the temporary file was left behind");
        }
        finally
        {
            DeleteFolder(path);
        }
    }

    [AvaloniaFact]
    public void MissingOrCorruptFile_GivesTheDefaults()
    {
        var path = NewPath();
        try
        {
            Assert.Equal(new CatalogSettings(), CatalogSettings.Load(path));
            foreach (var text in new[] { "{ not json", "", "null", "[1, 2]", "{\"Theme\": 5}" })
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, text);
                Assert.Equal(new CatalogSettings(), CatalogSettings.Load(path));
            }
        }
        finally
        {
            DeleteFolder(path);
        }

        // Values it cannot use keep the defaults when applied.
        var app = Application.Current!;
        var variant = app.RequestedThemeVariant;
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        try
        {
            new CatalogSettings { Theme = "Purple", Density = "Huge", Accent = "blue" }.ApplyTo(app, theme);
            Assert.Equal(ThemeVariant.Default, app.RequestedThemeVariant);
            Assert.Equal(DensityStyle.Normal, theme.Density);
            Assert.Equal(StandardsTheme.DefaultAccent, theme.Accent);

            // A number parses as an enum value even where no density has it.
            theme.Density = DensityStyle.Compact;
            new CatalogSettings { Density = "5" }.ApplyTo(app, theme);
            Assert.Equal(DensityStyle.Normal, theme.Density);
        }
        finally
        {
            app.RequestedThemeVariant = variant;
        }
    }

    [AvaloniaFact]
    public void Store_AppliesTheFileAtStart_AndSavesChangesAfterADelay()
    {
        var path = NewPath();
        new CatalogSettings { Theme = "Light", Density = "Compact", Accent = "#00899E", ReduceMotion = false, HighRefreshRate = true }.Save(path);
        var app = Application.Current!;
        var variant = app.RequestedThemeVariant;
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var store = CatalogSettingsStore.Start(app, theme, path);
        try
        {
            Assert.Equal(ThemeVariant.Light, app.RequestedThemeVariant);
            Assert.Equal(DensityStyle.Compact, theme.Density);
            Assert.Equal(Color.Parse("#00899E"), theme.Accent);
            Assert.False(theme.ReduceMotion);
            Assert.Null(theme.ReduceTransparency);
            Assert.True(theme.HighRefreshRate);
            Assert.False(store.IsPending);

            // A change is written once the delay has passed, not at once.
            theme.HighRefreshRate = false;
            theme.ReduceTransparency = true;
            app.RequestedThemeVariant = ThemeVariant.Dark;
            Assert.True(store.IsPending);
            Assert.True(CatalogSettings.Load(path).HighRefreshRate);
            // The dispatcher runs its timers while a frame is pushed (RunJobs alone does not).
            var frame = new DispatcherFrame();
            DispatcherTimer.RunOnce(() => frame.Continue = false, CatalogSettingsStore.Delay * 3);
            Dispatcher.UIThread.PushFrame(frame);
            Assert.False(store.IsPending);
            var written = CatalogSettings.Load(path);
            Assert.False(written.HighRefreshRate);
            Assert.True(written.ReduceTransparency);
            Assert.Equal("Dark", written.Theme);
            Assert.Equal("Compact", written.Density);
        }
        finally
        {
            store.Dispose();
            app.RequestedThemeVariant = variant;
            DeleteFolder(path);
        }
    }
}
