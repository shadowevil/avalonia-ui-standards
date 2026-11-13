using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// The focus ring draws outside its control but is clipped like the control: by scroll viewers,
/// the glass panel and the window frame.
/// </summary>
public class FocusRingClipTests
{
    // The ring is the accent (#007CEA); nothing else in these scenes is that blue.
    private static bool IsRing((byte R, byte G, byte B) p) => p.B > 150 && p.B - p.R > 90 && p.B - p.G > 40;

    [AvaloniaFact]
    public void Ring_IsClippedByAScrollViewer_PartlyAndFullyOutOfView()
    {
        var button = new Button { Content = "Focus me", HorizontalAlignment = HorizontalAlignment.Center };
        var scroller = new ScrollViewer
        {
            Width = 200,
            Height = 120,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden,
            Content = new StackPanel { Children = { new Border { Height = 40 }, button, new Border { Height = 400 } } },
        };
        var window = Show(new Window
        {
            Width = 300, Height = 220, RequestedThemeVariant = ThemeVariant.Light,
            Content = new Border { Padding = new Thickness(50), Child = scroller },
        });
        try
        {
            button.Focus(NavigationMethod.Tab);
            Pump(window);
            var viewport = Viewport(scroller, window);

            // In view: the ring is there, just outside the button.
            using (var frame = Frame.Capture(window))
            {
                Assert.True(CountRing(frame, r => true) > 0, "No ring while the button is in view.");
                AssertNoRingOutside(frame, viewport);
            }

            // Partly out of view (button half above the viewport): the ring is cut at the top edge.
            scroller.Offset = new Vector(0, 56);
            Pump(window);
            using (var frame = Frame.Capture(window))
            {
                AssertNoRingOutside(frame, viewport);
                Assert.True(CountRing(frame, p => p.Y >= viewport.Top && p.Y < viewport.Top + 6) > 0,
                    "The partly visible ring should reach the viewport's top edge.");
            }

            // Fully out of view: no ring anywhere.
            scroller.Offset = new Vector(0, 120);
            Pump(window);
            using (var frame = Frame.Capture(window))
                Assert.Equal(0, CountRing(frame, p => true));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Ring_IsClippedByTheGlassPanel()
    {
        // A button pushed above the glass panel's top edge, towards the title band.
        var button = new Button { Content = "Focus me" };
        Canvas.SetLeft(button, 40);
        Canvas.SetTop(button, -28);
        var window = new StandardsWindow
        {
            Title = "Clip",
            Width = 520, Height = 380,
            RequestedThemeVariant = ThemeVariant.Light,
            WindowButtonStyle = WindowButtonStyle.CaptionButtons,
            Content = new Canvas { Children = { button } },
        };
        Show(window);
        try
        {
            button.Focus(NavigationMethod.Tab);
            Pump(window);
            var panel = window.GetVisualDescendants().OfType<GlassBorder>().Single(g => g.Name == StandardsWindowTheme.ContentPanelPart);
            var panelTop = panel.TranslatePoint(default, window)!.Value.Y;

            using var frame = Frame.Capture(window);
            Assert.True(CountRing(frame, p => p.Y >= panelTop) > 0, "The ring should show inside the panel.");
            Assert.Equal(0, CountRing(frame, p => p.Y < panelTop - 1));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Ring_DrawsFullyOutsideTheControl_WhenNotClipped()
    {
        var button = new Button { Content = "Focus me", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var window = Show(new Window { Width = 240, Height = 120, RequestedThemeVariant = ThemeVariant.Light, Content = button });
        try
        {
            button.Focus(NavigationMethod.Tab);
            Pump(window);
            var b = new Rect(button.TranslatePoint(default, window)!.Value, button.Bounds.Size);
            using var frame = Frame.Capture(window);

            // The ring's stroke lies 2–4 px outside the button: check the middle of each side, 3 px out.
            Assert.True(IsRing(frame[(int)b.Center.X, (int)b.Top - 3]), "top");
            Assert.True(IsRing(frame[(int)b.Center.X, (int)b.Bottom + 2]), "bottom");
            Assert.True(IsRing(frame[(int)b.Left - 3, (int)b.Center.Y]), "left");
            Assert.True(IsRing(frame[(int)b.Right + 2, (int)b.Center.Y]), "right");
        }
        finally
        {
            window.Close();
        }
    }

    private static Rect Viewport(ScrollViewer scroller, Window window) =>
        new(scroller.TranslatePoint(default, window)!.Value, scroller.Bounds.Size);

    private static void AssertNoRingOutside(Frame frame, Rect viewport)
    {
        var outside = CountRing(frame, p => !viewport.Inflate(0.5).Contains(p));
        Assert.True(outside == 0, $"{outside} ring pixels outside the viewport {viewport}.");
    }

    private static int CountRing(Frame frame, Func<Point, bool> where)
    {
        var count = 0;
        for (var y = 0; y < frame.Height; y++)
            for (var x = 0; x < frame.Width; x++)
                if (IsRing(frame[x, y]) && where(new Point(x + 0.5, y + 0.5)))
                    count++;
        return count;
    }

    private static T Show<T>(T window) where T : Window
    {
        window.Show();
        Pump(window);
        return window;
    }

    private static void Pump(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
