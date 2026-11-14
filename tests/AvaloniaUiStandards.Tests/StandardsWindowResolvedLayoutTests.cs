using Avalonia.Headless.XUnit;
using AvaloniaUiStandards.Modules;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>StandardsWindow.ResolvedLayoutMode: the public, read-only layout in use.</summary>
public class StandardsWindowResolvedLayoutTests
{
    private static WindowLayoutMode ThisOs => OperatingSystem.IsMacOS() ? WindowLayoutMode.MacOS : WindowLayoutMode.Windows;

    [AvaloniaFact]
    public void Automatic_ResolvesToThisOsLayout_NeverAutomatic()
    {
        var window = new StandardsWindow(nativeFrame: false);
        Assert.Equal(WindowLayoutMode.Automatic, window.LayoutMode);
        Assert.Equal(ThisOs, window.ResolvedLayoutMode);
    }

    [AvaloniaFact]
    public void FollowsLiveLayoutModeChanges_AndRaisesThePropertyChange()
    {
        var window = new StandardsWindow(nativeFrame: false);
        var changes = new List<(WindowLayoutMode Old, WindowLayoutMode New)>();
        window.PropertyChanged += (_, e) =>
        {
            if (e.Property == StandardsWindow.ResolvedLayoutModeProperty)
                changes.Add(((WindowLayoutMode)e.OldValue!, (WindowLayoutMode)e.NewValue!));
        };

        var other = ThisOs == WindowLayoutMode.MacOS ? WindowLayoutMode.Windows : WindowLayoutMode.MacOS;
        window.LayoutMode = other;
        Assert.Equal(other, window.ResolvedLayoutMode);
        Assert.Equal([(ThisOs, other)], changes);

        window.LayoutMode = WindowLayoutMode.Automatic;
        Assert.Equal(ThisOs, window.ResolvedLayoutMode);
        Assert.Equal([(ThisOs, other), (other, ThisOs)], changes);

        // Automatic to the layout it already resolves to changes nothing.
        window.LayoutMode = ThisOs;
        Assert.Equal(2, changes.Count);
    }

    [AvaloniaFact]
    public void IsReadOnly()
    {
        Assert.True(StandardsWindow.ResolvedLayoutModeProperty.IsReadOnly);
    }
}
