using System.Reflection;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Skia;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>What <c>UseUiStandards</c> registers.</summary>
public class AppBuilderTests
{
    private const BindingFlags Any = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [AvaloniaFact]
    public void UseUiStandards_SetsTheGpuBudget()
    {
        // The test app is built with UseUiStandards.
        var options = ReadSkiaOptions();
        Assert.NotNull(options);
        Assert.Equal(256L * 1024 * 1024, options!.MaxGpuResourceSizeBytes);
        Assert.Equal(AppBuilderExtensions.GpuResourceBudget, options.MaxGpuResourceSizeBytes);
    }

    // SkiaOptions passed with a later .With replace the budget: an app's own options win.
    [AvaloniaFact]
    public void ALaterWith_Wins()
    {
        var own = new SkiaOptions { MaxGpuResourceSizeBytes = 64L * 1024 * 1024, UseOpacitySaveLayer = true };
        var builder = AppBuilder.Configure<Application>().UseUiStandards().With(own);
        // The builder's options, applied as setup applies them, in a locator scope of their own.
        var initializers = (Action?)typeof(AppBuilder).GetField("_optionsInitializers", Any)!.GetValue(builder);
        using ((IDisposable)typeof(AvaloniaLocator).GetMethod("EnterScope", Any)!.Invoke(null, null)!)
        {
            initializers!();
            Assert.Same(own, ReadSkiaOptions());
        }
        Assert.Equal(AppBuilderExtensions.GpuResourceBudget, ReadSkiaOptions()!.MaxGpuResourceSizeBytes);
    }

    // Avalonia's service locator is left out of its reference assemblies.
    private static SkiaOptions? ReadSkiaOptions()
    {
        var locator = typeof(AvaloniaLocator).GetProperty("Current", Any)!.GetValue(null)!;
        return (SkiaOptions?)locator.GetType().GetMethod("GetService", [typeof(Type)])!.Invoke(locator, [typeof(SkiaOptions)]);
    }
}
