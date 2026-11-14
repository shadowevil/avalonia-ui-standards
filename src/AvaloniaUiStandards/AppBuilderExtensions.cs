using Avalonia;
using Avalonia.Skia;

namespace AvaloniaUiStandards;

public static class AppBuilderExtensions
{
    /// <summary>
    /// Skia's GPU resource budget that <see cref="UseUiStandards"/> sets: 256 MB. Glass needs about
    /// 190 MB of GPU scratch in a full-screen window with several surfaces; under Avalonia's
    /// default budget (about 29 MB) Skia frees and reallocates its textures every frame.
    /// </summary>
    public const long GpuResourceBudget = 256L * 1024 * 1024;

    /// <summary>
    /// Registers what <see cref="StandardsTheme"/> needs: the bundled Inter font, and Skia's GPU
    /// resource budget of <see cref="GpuResourceBudget"/>. An app sets its own budget, or other
    /// Skia options, with <c>.With(new SkiaOptions { ... })</c> after this call. That replaces the
    /// whole <see cref="SkiaOptions"/>, so to keep the budget it includes
    /// <c>MaxGpuResourceSizeBytes = AppBuilderExtensions.GpuResourceBudget</c>.
    /// </summary>
    public static AppBuilder UseUiStandards(this AppBuilder builder) =>
        builder.WithInterFont().With(new SkiaOptions { MaxGpuResourceSizeBytes = GpuResourceBudget });
}
