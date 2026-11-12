using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

public class BorderOverlayTests
{
    public static TheoryData<string> Decorators => new() { nameof(SquircleBorder), nameof(GlassBorder) };

    [AvaloniaTheory]
    [MemberData(nameof(Decorators))]
    public void ChildSetAfterShow_OverlayStaysLastVisualChild(string decorator)
    {
        var (window, host) = Show(decorator, child: null);

        var child = new Border { Background = Brushes.Red };
        host.Child = child;
        window.UpdateLayout();

        var visuals = host.GetVisualChildren().ToList();
        Assert.Equal(2, visuals.Count);
        Assert.Same(child, visuals[0]);
        Assert.IsType<BorderOverlay>(visuals[1]);

        host.Child = new Border();
        Assert.IsType<BorderOverlay>(host.GetVisualChildren().Last());
    }

    [AvaloniaTheory]
    [MemberData(nameof(Decorators))]
    public void HitTestAtCentre_ReturnsTheChild(string decorator)
    {
        var child = new Border { Background = Brushes.Red };
        var (window, host) = Show(decorator, child);

        var centre = host.TranslatePoint(new Point(host.Bounds.Width / 2, host.Bounds.Height / 2), window)!.Value;
        // Hit testing reads the rendered scene, so render one frame first.
        using (window.CaptureRenderedFrame()) { }
        Assert.Same(child, window.InputHitTest(centre));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Decorators))]
    public void AutomationChildren_ExcludeTheOverlay(string decorator)
    {
        var child = new Border { Background = Brushes.Red };
        var (_, host) = Show(decorator, child);

        var children = ControlAutomationPeer.CreatePeerForElement(host).GetChildren();
        var peer = Assert.IsAssignableFrom<ControlAutomationPeer>(Assert.Single(children));
        Assert.Same(child, peer.Owner);
    }

    private static (Window Window, Decorator Host) Show(string decorator, Control? child)
    {
        Decorator host = decorator == nameof(GlassBorder)
            ? new GlassBorder { Width = 120, Height = 80 }
            : new SquircleBorder { Width = 120, Height = 80, BorderBrush = Brushes.Blue, BorderThickness = new Thickness(1) };
        host.Child = child;
        var window = new Window { Width = 200, Height = 120, Content = host };
        window.Show();
        return (window, host);
    }
}
