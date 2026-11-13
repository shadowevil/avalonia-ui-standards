using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>ListBox, ListBoxItem and ComboBoxItem: squircle items, states, sizes, contrast, focus.</summary>
public class ListBoxThemeTests
{
    [AvaloniaFact]
    public void Theme_AppliesToListBoxAndItems()
    {
        var (window, list) = Show(Items(3));
        try
        {
            Assert.IsType<SquircleBorder>(Assert.Single(list.GetVisualChildren()));
            Assert.Equal(new Thickness(4), list.Padding);
            Assert.Equal(new CornerRadius(8), list.CornerRadius);
            Assert.Equal(Colors.Transparent, Effective(list.Background));
            Assert.Null(list.FocusAdorner);
            var item = Item(list, 0);
            Assert.Equal(ButtonParts.ChromePart, Chrome(item).Name);
            Assert.Equal(new CornerRadius(8), item.CornerRadius);

            // Items are inset from the list's edges by SpacingXS.
            var origin = item.TranslatePoint(default, list)!.Value;
            Assert.Equal(new Point(4, 4), origin);
            Assert.Equal(list.Bounds.Width - 8, item.Bounds.Width);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Normal", 32, 12, 4)]
    [InlineData("Compact", 24, 8, 2)]
    public void ItemSize_FollowsDensity(string density, double height, double side, double vertical)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal };
        var (window, list) = Show(Items(2), theme: theme);
        try
        {
            Assert.Equal(height, Item(list, 0).Bounds.Height);
            Assert.Equal(new Thickness(side, vertical), Item(list, 0).Padding);
        }
        finally
        {
            window.Close();
        }
    }

    // Expected fill and text per state (null fill = transparent).
    public static TheoryData<string, string, string?, string> States()
    {
        var data = new TheoryData<string, string, string?, string>();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            data.Add(theme, "rest", null, "SystemControlForegroundBaseHighBrush");
            data.Add(theme, ":pointerover", "SystemControlHighlightListMediumBrush", "SystemControlHighlightAltBaseHighBrush");
            data.Add(theme, ":pressed", "SystemControlHighlightBaseMediumLowBrush", "SystemControlHighlightAltBaseHighBrush");
            data.Add(theme, "selected", "AccentFillColor", "AccentButtonForeground");
            data.Add(theme, "selected:pointerover", "AccentFillColorPointerOver", "AccentButtonForeground");
            data.Add(theme, "selected:pressed", "AccentFillColorPressed", "AccentButtonForeground");
        }
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(States))]
    public void ItemStates_Resolve(string theme, string state, string? fillKey, string textKey)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, list) = Show(Items(2), variant);
        try
        {
            var (fill, text) = ItemColours(window, Item(list, 0), state);
            Assert.Equal(fillKey is null ? Colors.Transparent : Effective(Resolve(window, fillKey)), fill);
            Assert.Equal(Effective(Resolve(window, textKey)), text);
        }
        finally
        {
            window.Close();
        }
    }

    // Every enabled state's text meets 4.5:1 over its fill, on the card and the page; selected text is white.
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void ItemText_MeetsAaContrast(string theme)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        foreach (var surfaceKey in new[] { "SystemRegionColor", "SystemChromeMediumLowColor" })
            foreach (var state in new[] { "rest", ":pointerover", ":pressed", "selected", "selected:pointerover", "selected:pressed" })
            {
                var (window, list) = Show(Items(2), variant);
                try
                {
                    var surface = Effective(Resolve(window, surfaceKey));
                    var (fill, text) = ItemColours(window, Item(list, 0), state);
                    var under = Over(fill, surface);
                    var ratio = AccessibleColors.ContrastRatio(Over(text, under), under);
                    Assert.True(ratio >= 4.5, $"{state} on {surfaceKey}: {ratio:F2}");
                    if (state.StartsWith("selected"))
                        Assert.Equal(Colors.White, text);
                }
                finally
                {
                    window.Close();
                }
            }
    }

    // Hover and press stand out from every surface a list sits on, and from each other:
    // at least 1.3:1 (the luminance contrast ratio), measured over each surface.
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void HoverAndPressedFills_StandOutFromTheirSurfaces(string theme)
    {
        const double minimum = 1.3;
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        foreach (var surfaceKey in new[] { "SystemRegionColor", "SystemChromeMediumLowColor", "ComboBoxDropDownBackground" })
        {
            var (window, list) = Show(Items(2), variant);
            try
            {
                var surface = Effective(Resolve(window, surfaceKey));
                var (hover, _) = ItemColours(window, Item(list, 0), ":pointerover");
                var (pressed, _) = ItemColours(window, Item(list, 1), ":pressed");
                var hoverOn = Over(hover, surface);
                var pressedOn = Over(pressed, surface);
                foreach (var (name, a, b) in new[] { ("hover", hoverOn, surface), ("pressed", pressedOn, surface), ("pressed vs hover", pressedOn, hoverOn) })
                {
                    var ratio = AccessibleColors.ContrastRatio(a, b);
                    Assert.True(ratio >= minimum, $"{name} on {surfaceKey}: {ratio:F2}");
                }
            }
            finally
            {
                window.Close();
            }
        }
    }

    [AvaloniaFact]
    public void InnerScroller_TakesTheListsScrollSettings()
    {
        var list = Items(3);
        ScrollViewer.SetBringIntoViewOnFocusChange(list, false);
        ScrollViewer.SetHorizontalSnapPointsType(list, Avalonia.Controls.Primitives.SnapPointsType.Mandatory);
        ScrollViewer.SetVerticalSnapPointsType(list, Avalonia.Controls.Primitives.SnapPointsType.MandatorySingle);
        ScrollViewer.SetHorizontalSnapPointsAlignment(list, Avalonia.Controls.Primitives.SnapPointsAlignment.Far);
        ScrollViewer.SetVerticalSnapPointsAlignment(list, Avalonia.Controls.Primitives.SnapPointsAlignment.Center);
        var (window, _) = Show(list);
        try
        {
            var scroller = list.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "PART_ScrollViewer");
            Assert.False(ScrollViewer.GetBringIntoViewOnFocusChange(scroller));
            Assert.Equal(Avalonia.Controls.Primitives.SnapPointsType.Mandatory, ScrollViewer.GetHorizontalSnapPointsType(scroller));
            Assert.Equal(Avalonia.Controls.Primitives.SnapPointsType.MandatorySingle, ScrollViewer.GetVerticalSnapPointsType(scroller));
            Assert.Equal(Avalonia.Controls.Primitives.SnapPointsAlignment.Far, ScrollViewer.GetHorizontalSnapPointsAlignment(scroller));
            Assert.Equal(Avalonia.Controls.Primitives.SnapPointsAlignment.Center, ScrollViewer.GetVerticalSnapPointsAlignment(scroller));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FocusedItem_ShowsTheRing_AndRaisesItself()
    {
        var (window, list) = Show(Items(3));
        try
        {
            var item = Item(list, 1);
            Assert.True(item.Focus(NavigationMethod.Tab));
            Dispatcher.UIThread.RunJobs();

            var ring = item.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == FocusRing.Part);
            Assert.True(ring.IsVisible);
            Assert.Equal(new CornerRadius(12), ring.CornerRadius);
            Assert.Equal(1, item.ZIndex);
            Assert.Equal(0, Item(list, 0).ZIndex);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComboBoxItem_UsesTheListItemDesign()
    {
        var box = new ComboBox { ItemsSource = new[] { "One", "Two", "Three" }, SelectedIndex = 1 };
        var window = new Window { Width = 300, Height = 300, Content = box };
        window.Show();
        try
        {
            box.IsDropDownOpen = true;
            Pump(window);
            var item = box.GetLogicalDescendants().OfType<ComboBoxItem>().First();
            Assert.Equal(ButtonParts.ChromePart, Chrome(item).Name);
            Assert.Equal(new CornerRadius(8), item.CornerRadius);
            Assert.Equal(32, item.Bounds.Height);
        }
        finally
        {
            box.IsDropDownOpen = false;
            window.Close();
        }
    }

    // Every frame of a fill fade, composited over the surface, stays between the two states'
    // composited colours in every channel: no frame is lighter or darker than both ends, so a
    // fade from transparent never flashes. Covers ListBoxItem and ComboBoxItem.
    public static TheoryData<string, string, string, string> FillTransitions()
    {
        var data = new TheoryData<string, string, string, string>();
        foreach (var theme in new[] { "Light", "Dark" })
            foreach (var surfaceKey in new[] { "SystemRegionColor", "SystemChromeMediumLowColor" })
                foreach (var (from, to) in new[] { ("rest", ":pointerover"), (":pointerover", "rest"), ("rest", "selected"), ("selected", "rest"), (":pointerover", ":pressed") })
                    data.Add(theme, surfaceKey, from, to);
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(FillTransitions))]
    public void FillTransitions_NeverOvershootTheirEnds(string theme, string surfaceKey, string from, string to)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        foreach (var comboItem in new[] { false, true })
        {
            var (window, item) = comboItem ? ShowComboItem(variant) : ShowListItem(variant);
            try
            {
                var name = $"{(comboItem ? "ComboBoxItem" : "ListBoxItem")} {from} -> {to}";
                var surface = Effective(Resolve(window, surfaceKey));
                var chrome = Chrome(item);
                var fades = chrome.Transitions!.OfType<TransitionBase>()
                    .Where(t => t.Property == SquircleBorder.BackgroundProperty || t.Property == SquircleBorder.BorderBrushProperty).ToList();
                Assert.Equal(2, fades.Count);
                Assert.All(fades, t => Assert.IsType<CrossFadeBrushTransition>(t));

                SetState(item, from, true);
                Pump(window);
                var fromBrush = TargetFill(chrome);
                SetState(item, from, false);
                SetState(item, to, true);
                Pump(window);
                var toBrush = TargetFill(chrome);

                var start = Over(Effective(fromBrush), surface);
                var end = Over(Effective(toBrush), surface);
                Assert.NotEqual(start, end);
                for (var step = 0; step <= 50; step++)
                {
                    var frame = Over(Effective(CrossFadeBrushTransition.At(step / 50.0, fromBrush, toBrush)), surface);
                    foreach (var (f, a, b, channel) in new[] { (frame.R, start.R, end.R, "R"), (frame.G, start.G, end.G, "G"), (frame.B, start.B, end.B, "B") })
                        Assert.True(f >= Math.Min(a, b) - 1 && f <= Math.Max(a, b) + 1,
                            $"{name}: frame {step}/50 is {frame}, outside {start}..{end} in {channel}");
                }
            }
            finally
            {
                window.Close();
            }
        }
    }

    [Fact]
    public void CrossFade_FoldsBrushOpacity_AndTreatsNullAsTransparent()
    {
        var translucent = new SolidColorBrush(Colors.White, 0.1);
        Assert.Equal(Color.FromArgb(13, 255, 255, 255), Effective(CrossFadeBrushTransition.At(0.5, null, translucent)));
        Assert.Equal(Color.FromArgb(128, 255, 255, 255), Effective(CrossFadeBrushTransition.At(0.5, Brushes.Transparent, Brushes.White)));
        Assert.Same(translucent, CrossFadeBrushTransition.At(1, Brushes.Black, translucent));
        Assert.Same(Brushes.Black, CrossFadeBrushTransition.At(0, Brushes.Black, translucent));
    }

    // --- Helpers -------------------------------------------------------------------------

    private static (Window Window, ContentControl Item) ShowListItem(ThemeVariant variant)
    {
        var (window, list) = Show(Items(2), variant);
        return (window, Item(list, 0));
    }

    private static (Window Window, ContentControl Item) ShowComboItem(ThemeVariant variant)
    {
        var box = new ComboBox { ItemsSource = new[] { "One", "Two" } };
        var window = new Window { Width = 300, Height = 300, RequestedThemeVariant = variant, Content = box };
        window.Show();
        box.IsDropDownOpen = true;
        Pump(window);
        return (window, box.GetLogicalDescendants().OfType<ComboBoxItem>().First());
    }

    private static void SetState(ContentControl item, string state, bool on)
    {
        if (state == "selected")
            ((ISelectable)item).IsSelected = on;
        else if (state != "rest")
            ((IPseudoClasses)item.Classes).Set(state, on);
    }

    // The fill the chrome's state sets, which its transition runs towards.
    private static IBrush? TargetFill(SquircleBorder chrome) => (IBrush?)chrome.GetBaseValue(SquircleBorder.BackgroundProperty).Value;

    private static ListBox Items(int count) => new() { ItemsSource = Enumerable.Range(1, count).Select(i => $"Item {i}").ToList(), Width = 200 };

    private static (Window Window, ListBox List) Show(ListBox list, ThemeVariant? variant = null, StandardsTheme? theme = null)
    {
        var window = new Window { Width = 300, Height = 200, RequestedThemeVariant = variant ?? ThemeVariant.Light, Content = list };
        if (theme is not null)
            window.Styles.Add(theme);
        window.Show();
        Pump(window);
        return (window, list);
    }

    private static (Color Fill, Color Text) ItemColours(Window window, ListBoxItem item, string state)
    {
        if (state.StartsWith("selected"))
            item.IsSelected = true;
        var pseudo = state.Replace("selected", "");
        if (pseudo.StartsWith(':'))
            ((IPseudoClasses)item.Classes).Set(pseudo, true);
        Pump(window);

        var chrome = Chrome(item);
        var fill = chrome.GetBaseValue(SquircleBorder.BackgroundProperty) is { HasValue: true } set ? Effective(set.Value) : Effective(chrome.Background);
        var presenter = (ContentPresenter)chrome.Child!;
        return (fill, Effective(presenter.Foreground));
    }

    private static ListBoxItem Item(ListBox list, int index) => (ListBoxItem)list.ContainerFromIndex(index)!;

    private static SquircleBorder Chrome(Control item) =>
        item.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart);

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
    }
}
