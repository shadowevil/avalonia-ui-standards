using System.Text.Json;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using AvaloniaUiStandards;

namespace Catalog;

/// <summary>
/// The catalog's saved settings: the theme toolbar's choices and the Settings page's, so the catalog reopens as it
/// was left. Kept as JSON in the per-user application data folder (<see cref="DefaultPath"/>).
/// </summary>
internal sealed record CatalogSettings
{
    /// <summary>System, Light or Dark.</summary>
    public string Theme { get; init; } = "System";

    /// <summary>Normal or Compact.</summary>
    public string Density { get; init; } = nameof(DensityStyle.Normal);

    /// <summary>The accent colour, as #RRGGBB.</summary>
    public string Accent { get; init; } = Hex(StandardsTheme.DefaultAccent);

    /// <summary>Reduce motion: null follows the OS.</summary>
    public bool? ReduceMotion { get; init; }

    /// <summary>Reduce transparency: null follows the OS.</summary>
    public bool? ReduceTransparency { get; init; }

    public bool HighRefreshRate { get; init; }

    /// <summary>
    /// The settings file: <c>AvaloniaUiStandards.Catalog/settings.json</c> in the per-user application data folder,
    /// which is ~/Library/Application Support on macOS, %APPDATA% on Windows and ~/.config (or $XDG_CONFIG_HOME) on Linux.
    /// </summary>
    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AvaloniaUiStandards.Catalog", "settings.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
    };

    /// <summary>The settings in <paramref name="path"/>, or the defaults when it is missing or cannot be read.</summary>
    public static CatalogSettings Load(string path)
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<CatalogSettings>(File.ReadAllText(path), Json) ?? new() : new();
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new();
        }
    }

    /// <summary>Writes the settings to <paramref name="path"/> atomically, creating its folder; a failure to write is ignored.</summary>
    public void Save(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            // Written beside it, then moved over it, so a reader never sees a half-written file.
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, Json));
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>The current choices of <paramref name="app"/> and its <paramref name="theme"/>.</summary>
    public static CatalogSettings From(Application app, StandardsTheme theme) => new()
    {
        Theme = app.RequestedThemeVariant == ThemeVariant.Light ? "Light" : app.RequestedThemeVariant == ThemeVariant.Dark ? "Dark" : "System",
        Density = theme.Density.ToString(),
        Accent = Hex(theme.Accent),
        ReduceMotion = theme.ReduceMotion,
        ReduceTransparency = theme.ReduceTransparency,
        HighRefreshRate = theme.HighRefreshRate,
    };

    /// <summary>Sets these choices on <paramref name="app"/> and its <paramref name="theme"/>; unknown values keep the defaults.</summary>
    public void ApplyTo(Application app, StandardsTheme theme)
    {
        app.RequestedThemeVariant = Theme switch { "Light" => ThemeVariant.Light, "Dark" => ThemeVariant.Dark, _ => ThemeVariant.Default };
        theme.Density = Enum.TryParse<DensityStyle>(Density, out var density) && Enum.IsDefined(density) ? density : DensityStyle.Normal;
        theme.Accent = Accent is { Length: 7 } && Accent.StartsWith('#') && Color.TryParse(Accent, out var accent) ? accent : StandardsTheme.DefaultAccent;
        theme.ReduceMotion = ReduceMotion;
        theme.ReduceTransparency = ReduceTransparency;
        theme.HighRefreshRate = HighRefreshRate;
    }

    private static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}

/// <summary>
/// Loads the catalog's settings at startup and saves them whenever a choice changes, at most once per
/// <see cref="Delay"/> after the last change.
/// </summary>
internal sealed class CatalogSettingsStore : IDisposable
{
    /// <summary>How long after the last change the settings are written.</summary>
    public static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(500);

    private readonly Application _app;
    private readonly StandardsTheme _theme;
    private readonly DispatcherTimer _timer;

    private CatalogSettingsStore(Application app, StandardsTheme theme, string path)
    {
        _app = app;
        _theme = theme;
        Path = path;
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = Delay };
        _timer.Tick += (_, _) => Flush();
        _theme.PropertyChanged += OnChanged;
        _app.PropertyChanged += OnAppChanged;
    }

    /// <summary>The settings file.</summary>
    public string Path { get; }

    /// <summary>Applies the settings saved in <paramref name="path"/> and saves later changes there.</summary>
    public static CatalogSettingsStore Start(Application app, StandardsTheme theme, string path)
    {
        CatalogSettings.Load(path).ApplyTo(app, theme);
        return new CatalogSettingsStore(app, theme, path);
    }

    /// <summary>Whether a change is waiting to be written.</summary>
    public bool IsPending => _timer.IsEnabled;

    /// <summary>Writes the current settings now.</summary>
    public void Flush()
    {
        _timer.Stop();
        CatalogSettings.From(_app, _theme).Save(Path);
    }

    private void OnChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == StandardsTheme.DensityProperty || e.Property == StandardsTheme.AccentProperty
            || e.Property == StandardsTheme.ReduceMotionProperty || e.Property == StandardsTheme.ReduceTransparencyProperty
            || e.Property == StandardsTheme.HighRefreshRateProperty)
            Schedule();
    }

    private void OnAppChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Application.RequestedThemeVariantProperty)
            Schedule();
    }

    private void Schedule()
    {
        _timer.Stop();
        _timer.Start();
    }

    public void Dispose()
    {
        if (IsPending)
            Flush();
        _theme.PropertyChanged -= OnChanged;
        _app.PropertyChanged -= OnAppChanged;
    }
}
