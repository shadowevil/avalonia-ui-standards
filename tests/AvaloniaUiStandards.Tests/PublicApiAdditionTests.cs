using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>The Thickness and GridLength spacing resources, and pinning accessibility settings the way an app's tests do.</summary>
public class PublicApiAdditionTests
{
    private static readonly (string Step, double Size)[] Steps =
        [("XS", 4), ("S", 8), ("M", 12), ("L", 16), ("XL", 24), ("XXL", 32), ("XXXL", 48)];

    private static T Resolve<T>(StandardsTheme theme, string key)
    {
        Assert.True(theme.TryGetResource(key, ThemeVariant.Light, out var value), key);
        return Assert.IsType<T>(value);
    }

    [AvaloniaTheory]
    [InlineData(DensityStyle.Normal)]
    [InlineData(DensityStyle.Compact)]
    public void SpacingResources_AllFormsHoldTheSameSteps_UnderEveryDensity(DensityStyle density)
    {
        var theme = StandardsTheme.CreateForTests(AccessibilitySettings.NotReduced);
        theme.Density = density;
        foreach (var (step, size) in Steps)
        {
            Assert.Equal(size, Resolve<double>(theme, "Spacing" + step));
            Assert.Equal(new Thickness(size), Resolve<Thickness>(theme, $"Spacing{step}Thickness"));
            Assert.Equal(new GridLength(size), Resolve<GridLength>(theme, $"Spacing{step}GridLength"));
        }
    }

    [AvaloniaFact]
    public void SpacingResources_WorkAsDynamicResources_InMarginsAndGridSizes()
    {
        var theme = StandardsTheme.CreateForTests(AccessibilitySettings.NotReduced);
        var border = new Border();
        border.Bind(Layoutable.MarginProperty, border.GetResourceObservable("SpacingMThickness"));
        var grid = new Grid { ColumnDefinitions = [new ColumnDefinition(), new ColumnDefinition()], Children = { border } };
        grid.ColumnDefinitions[1].Bind(ColumnDefinition.WidthProperty, grid.GetResourceObservable("SpacingLGridLength"));
        var window = new Window { Content = grid };
        window.Styles.Add(theme);
        window.Show();

        Assert.Equal(new Thickness(12), border.Margin);
        Assert.Equal(new GridLength(16), grid.ColumnDefinitions[1].Width);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void CreateForTests_PinsTheSettingsTheAppFollows(bool reduceMotion, bool reduceTransparency)
    {
        var theme = StandardsTheme.CreateForTests(new AccessibilitySettings(reduceMotion, reduceTransparency));
        Assert.Equal(reduceMotion ? TimeSpan.Zero : TimeSpan.FromMilliseconds(100), Resolve<TimeSpan>(theme, "MotionFast"));
        Assert.Equal(reduceTransparency, theme.IsTransparencyReduced);

        // An explicit app setting still overrides the pinned one.
        theme.ReduceMotion = !reduceMotion;
        Assert.Equal(reduceMotion ? TimeSpan.FromMilliseconds(100) : TimeSpan.Zero, Resolve<TimeSpan>(theme, "MotionFast"));
    }
}
