using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>TabControl, TabStrip and Expander: shape, sizes, the sliding underline, the reveal, keyboard, focus and motion.</summary>
public class TabsAndExpanderThemeTests
{
    // --- Tabs ----------------------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData("Normal", 32, 12)]
    [InlineData("Compact", 24, 8)]
    public void Tabs_HeadersAreBodyTextAtTheDensityHeight(string density, double height, double side)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal };
        var (window, tabs) = Show(TabsAndExpanderRenderTests.Tabs(), theme: theme);
        try
        {
            var tab = Tab(tabs, 0);
            Assert.Equal(14, tab.FontSize);
            Assert.Equal(FontWeight.Normal, tab.FontWeight);
            Assert.Equal(height, tab.Bounds.Height);
            Assert.Equal(new Thickness(side, tab.Padding.Top, side, tab.Padding.Bottom), tab.Padding);
            Assert.Equal(new CornerRadius(8), Part<SquircleBorder>(tab, ButtonParts.ChromePart).CornerRadius);

            // The content lines up with the header text.
            var text = Part<ContentPresenter>(tab, ButtonParts.ContentPart).TranslatePoint(default, tabs)!.Value.X;
            var content = Part<ContentPresenter>(tabs, "PART_SelectedContentHost").TranslatePoint(default, tabs)!.Value.X;
            Assert.Equal(text, content);
            Assert.NotNull(Part<ContentPresenter>(tabs, "PART_SelectedContentHost2"));
        }
        finally
        {
            window.Close();
        }
    }

    public static TheoryData<string, string, string> TabStates()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            data.Add(theme, "", "TabItemHeaderForegroundUnselected");
            data.Add(theme, ":pointerover", "TabItemHeaderForegroundUnselectedPointerOver");
            data.Add(theme, ":pressed", "TabItemHeaderForegroundUnselectedPressed");
            data.Add(theme, ":selected", "TabItemHeaderForegroundSelected");
        }
        return data;
    }

    // Text colours are Fluent's tab keys; tabs have no fill in any state, and the text meets AA on the page.
    [AvaloniaTheory]
    [MemberData(nameof(TabStates))]
    public void Tabs_TextColoursAndNoFill(string theme, string state, string key)
    {
        var (window, tabs) = Show(TabsAndExpanderRenderTests.Tabs(), theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        try
        {
            var tab = Tab(tabs, state == ":selected" ? 0 : 1);
            if (state is ":pointerover" or ":pressed")
                ((IPseudoClasses)tab.Classes).Set(state, true);
            Pump(window);
            var text = Effective(Part<ContentPresenter>(tab, ButtonParts.ContentPart).Foreground);
            Assert.Equal(Effective(Resolve(window, key)), text);
            Assert.Equal(0, Effective(Part<SquircleBorder>(tab, ButtonParts.ChromePart).Background).A);
            var ratio = AccessibleColors.ContrastRatio(text, Effective(Resolve(window, "SystemRegionColor")));
            Assert.True(ratio >= 4.5, $"{state}: {ratio:F2}");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Tabs_UnderlineSitsUnderTheSelectedHeaderText_OnTheHairline()
    {
        var (window, tabs) = Show(TabsAndExpanderRenderTests.Tabs());
        try
        {
            AssertUnderline(tabs, 0);
            var line = Part<Border>(tabs, TabThemes.StripLinePart);
            Assert.Equal(1, line.Bounds.Height);
            var indicator = Part<SquircleBorder>(tabs, TabThemes.IndicatorPart);
            Assert.Equal(line.TranslatePoint(new Point(0, 1), tabs)!.Value.Y, indicator.TranslatePoint(new Point(0, 3), tabs)!.Value.Y);
            Assert.Equal(Effective(Resolve(window, "TabItemHeaderSelectedPipeFill")), Effective(indicator.Background));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Tabs_UnderlineSlides_AndLandsUnderReduceMotion()
    {
        var (window, tabs) = Show(TabsAndExpanderRenderTests.Tabs());
        try
        {
            var follower = Follower(tabs);
            var slides = follower.Transitions!.OfType<TransitionBase>().ToList();
            Assert.Equal(2, slides.Count);
            Assert.All(slides, t => Assert.Equal(TimeSpan.FromMilliseconds(200), t.Duration));
            tabs.SelectedIndex = 2;
            Pump(window);
            Assert.Equal(Expected(tabs, 2), follower.Target);
            Assert.NotEqual(Expected(tabs, 2).Position, follower.Offset);
        }
        finally
        {
            window.Close();
        }

        var reduced = new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true };
        (window, tabs) = Show(TabsAndExpanderRenderTests.Tabs(), theme: reduced);
        try
        {
            foreach (var index in new[] { 2, 1, 0 })
            {
                tabs.SelectedIndex = index;
                Pump(window);
                AssertUnderline(tabs, index);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Tabs_UnderlineFollowsPlacementAndResizes()
    {
        var (window, tabs) = Show(TabsAndExpanderRenderTests.Tabs(placement: Dock.Left));
        try
        {
            AssertUnderline(tabs, 0);
            var indicator = Part<SquircleBorder>(tabs, TabThemes.IndicatorPart);
            Assert.Equal(3, indicator.Bounds.Width);
            Assert.Equal(1, Part<Border>(tabs, TabThemes.StripLinePart).Bounds.Width);

            tabs.TabStripPlacement = Dock.Top;
            Pump(window);
            AssertUnderline(tabs, 0);
            ((TabItem)tabs.Items[0]!).Header = "General settings";
            Pump(window);
            AssertUnderline(tabs, 0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TabStrip_HasTheSameUnderline()
    {
        var strip = new TabStrip();
        foreach (var header in new[] { "One", "Two", "Three" })
            strip.Items.Add(new TabStripItem { Content = header });
        strip.SelectedIndex = 1;
        var (window, _) = Show(strip);
        try
        {
            AssertUnderline(strip, 1);
            Assert.Equal(32, Tab(strip, 0).Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Tabs_KeyboardAndFocusRing()
    {
        var (window, tabs) = Show(TabsAndExpanderRenderTests.Tabs());
        try
        {
            Tab(tabs, 0).Focus(NavigationMethod.Tab);
            Pump(window);
            var ring = Part<SquircleBorder>(Tab(tabs, 0), FocusRing.Part);
            Assert.False(tabs.ClipToBounds); // the ring lies partly outside the strip
            Assert.True(ring.IsVisible);
            Press(window, Key.Right);
            Assert.Equal(1, tabs.SelectedIndex);
            Press(window, Key.End);
            Assert.Equal(2, tabs.SelectedIndex);
            Press(window, Key.Home);
            Assert.Equal(0, tabs.SelectedIndex);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Tabs_ShowTheTabIcon()
    {
        var (window, tabs) = Show(TabsAndExpanderRenderTests.Tabs(icons: true));
        try
        {
            var icon = Part<ContentPresenter>(Tab(tabs, 0), "PART_IconPresenter");
            Assert.True(icon.IsVisible);
            Assert.IsType<Icon>(icon.Content);
            var (plainWindow, plain) = Show(TabsAndExpanderRenderTests.Tabs());
            Assert.False(Part<ContentPresenter>(Tab(plain, 0), "PART_IconPresenter").IsVisible);
            plainWindow.Close();
        }
        finally
        {
            window.Close();
        }
    }

    // --- Expander ------------------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData("Normal", 32)]
    [InlineData("Compact", 24)]
    public void Expander_CardAndHeaderSize(string density, double height)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal };
        var (window, expander) = Show(NewExpander(expanded: false), theme: theme);
        try
        {
            var card = Part<SquircleBorder>(expander, ExpanderTheme.CardPart);
            Assert.Equal(new CornerRadius(8), card.CornerRadius);
            Assert.Equal(new Thickness(1), card.BorderThickness);
            Assert.Equal(height, Header(expander).Bounds.Height);
            Assert.Equal(height + 2, expander.Bounds.Height);
            Assert.False(Content(expander).IsEffectivelyVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Expander_RevealsUnderReduceMotion_AndTheCaretTurns()
    {
        var reduced = new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true };
        var (window, expander) = Show(NewExpander(expanded: false), theme: reduced);
        try
        {
            Assert.Equal(-90, Angle(expander));
            Assert.Empty(Part<Reveal>(expander, ExpanderTheme.RevealPart).Transitions!);
            var collapsed = expander.Bounds.Height;

            Header(expander).Focus(NavigationMethod.Tab);
            Press(window, Key.Space);
            Assert.True(expander.IsExpanded);
            Assert.Equal(0, Angle(expander));
            Assert.Equal(1, Part<Reveal>(expander, ExpanderTheme.RevealPart).Progress);
            Assert.True(Content(expander).IsEffectivelyVisible);
            Assert.True(expander.Bounds.Height > collapsed + 20);

            expander.ExpandDirection = ExpandDirection.Up;
            Pump(window);
            Assert.Equal(-180, Angle(expander));
            // Opening upwards, the header is at the bottom, under the content.
            Assert.True(Header(expander).TranslatePoint(default, expander)!.Value.Y > Content(expander).TranslatePoint(default, expander)!.Value.Y);

            expander.IsExpanded = false;
            Pump(window);
            Assert.Equal(collapsed, expander.Bounds.Height);
            Assert.False(Content(expander).IsEffectivelyVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Expander_MotionIsMotionNormal_AndAnOpenExpanderShowsOpenAtOnce()
    {
        var (window, expander) = Show(NewExpander(expanded: true));
        try
        {
            var reveal = Part<Reveal>(expander, ExpanderTheme.RevealPart);
            Assert.Equal(1, reveal.Progress);
            var opening = Assert.IsType<DoubleTransition>(Assert.Single(reveal.Transitions!));
            Assert.Equal(TimeSpan.FromMilliseconds(200), opening.Duration);
            var caret = Part<Icon>(expander, ExpanderTheme.CaretPart);
            var turn = Assert.IsType<TransformOperationsTransition>(Assert.Single(caret.Transitions!));
            Assert.Equal(TimeSpan.FromMilliseconds(200), turn.Duration);

            // Closing starts the reveal running back, not a jump.
            expander.IsExpanded = false;
            Pump(window);
            Assert.True(reveal.Progress > 0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Expander_HeaderFillsAndFocusRing()
    {
        var (window, expander) = Show(NewExpander(expanded: false));
        try
        {
            var header = Header(expander);
            var chrome = Part<SquircleBorder>(header, ButtonParts.ChromePart);
            Strip(header);
            ((IPseudoClasses)header.Classes).Set(":pointerover", true);
            Pump(window);
            Assert.Equal(Effective(Resolve(window, "SystemControlHighlightListMediumBrush")), Effective(chrome.Background));

            var ring = Part<SquircleBorder>(expander, FocusRing.Part);
            Assert.False(ring.IsVisible);
            header.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.True(ring.IsVisible);
            Assert.Equal(new CornerRadius(12), ring.CornerRadius);
            var other = new Button { Content = "Other" };
            ((StackPanel)window.Content!).Children.Add(other);
            Pump(window);
            other.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.False(ring.IsVisible);
            header.Focus(NavigationMethod.Pointer);
            Pump(window);
            Assert.False(ring.IsVisible);

            expander.IsEnabled = false;
            Pump(window);
            Assert.Equal(Effective(Resolve(window, "SystemControlDisabledBaseMediumLowBrush")),
                Effective(Part<ContentPresenter>(header, ButtonParts.ContentPart).Foreground));
        }
        finally
        {
            window.Close();
        }
    }

    // --- Helpers -------------------------------------------------------------------------

    private static Expander NewExpander(bool expanded) => new()
    {
        Header = "Advanced",
        IsExpanded = expanded,
        Content = new StackPanel { Children = { new TextBlock { Text = "Line one" }, new TextBlock { Text = "Line two" }, new Button { Content = "Inside" } } },
    };

    private static (Window Window, T Control) Show<T>(T control, ThemeVariant? variant = null, StandardsTheme? theme = null) where T : Control
    {
        var window = new Window
        {
            Width = 600, Height = 400, RequestedThemeVariant = variant ?? ThemeVariant.Light,
            Content = new StackPanel { Margin = new Thickness(8), Children = { control } },
        };
        if (theme is not null)
            window.Styles.Add(theme);
        window.Show();
        Pump(window);
        return (window, control);
    }

    private static TemplatedControl Tab(SelectingItemsControl tabs, int index) => (TemplatedControl)tabs.ContainerFromIndex(index)!;

    private static ToggleButton Header(Expander expander) => Part<ToggleButton>(expander, ExpanderTheme.HeaderPart);

    // The expander's content presenter (the header has its own PART_ContentPresenter).
    private static ContentPresenter Content(Expander expander) =>
        Part<Reveal>(expander, ExpanderTheme.RevealPart).GetVisualDescendants().OfType<ContentPresenter>().First(c => c.Name == "PART_ContentPresenter");

    private static SelectionIndicator Follower(Control tabs) => tabs.GetVisualDescendants().OfType<SelectionIndicator>().Single();

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    // The underline under the header's content (inside its padding), on the edge facing the content.
    private static Rect Expected(SelectingItemsControl tabs, int index)
    {
        var tab = Tab(tabs, index);
        var follower = Follower(tabs);
        var origin = tab.TranslatePoint(default, follower)!.Value;
        var r = new Rect(origin, tab.Bounds.Size);
        var p = tab.Padding;
        return (tabs as TabControl)?.TabStripPlacement switch
        {
            Dock.Left => new Rect(follower.Bounds.Width - 3, r.Y + p.Top, 3, r.Height - p.Top - p.Bottom),
            _ => new Rect(r.X + p.Left, r.Bottom - 3, r.Width - p.Left - p.Right, 3),
        };
    }

    private static void AssertUnderline(SelectingItemsControl tabs, int index)
    {
        var expected = Expected(tabs, index);
        Assert.Equal(expected, Follower(tabs).Target);
        Assert.Equal(expected, Part<SquircleBorder>(tabs, TabThemes.IndicatorPart).Bounds);
    }

    private static double Angle(Expander expander)
    {
        var caret = Part<Icon>(expander, ExpanderTheme.CaretPart);
        var matrix = ((TransformOperations)caret.RenderTransform!).Value;
        return Math.Round(Math.Atan2(matrix.M12, matrix.M11) * 180 / Math.PI) is var a && a == 180 ? -180 : a;
    }

    private static void Strip(Control root)
    {
        root.Transitions = null;
        foreach (var part in root.GetVisualDescendants().OfType<Animatable>())
            part.Transitions = null;
    }

    private static void Press(Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Pump(window);
    }

    private static object Resolve(Control control, string key)
    {
        Assert.True(control.TryFindResource(key, control.ActualThemeVariant, out var value), key);
        return value!;
    }

    private static void Pump(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
