using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Skia;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>What <c>UseUiStandards</c> registers.</summary>
public class AppBuilderTests
{
    [AvaloniaFact]
    public void UseUiStandards_SetsTheGpuBudget()
    {
        // The test app is built with UseUiStandards; Avalonia's service locator is not public.
        var locator = typeof(AvaloniaLocator).GetProperty("Current", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
        var options = (SkiaOptions?)locator.GetType().GetMethod("GetService", [typeof(Type)])!.Invoke(locator, [typeof(SkiaOptions)]);
        Assert.NotNull(options);
        Assert.Equal(256L * 1024 * 1024, options!.MaxGpuResourceSizeBytes);
        Assert.Equal(AppBuilderExtensions.GpuResourceBudget, options.MaxGpuResourceSizeBytes);
    }
}
