using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>The PanelStack Module (0051): solid cards SpacingS apart, each with the Expander's title strip that collapses and expands it.</summary>
public class PanelStackTests
{
    private static (StandardsWindow Window, PanelStack Stack) Show(ThemeVariant? theme = null)
    {
        var stack = SplitPaneTests.NewPanelStack();
        var window = SplitPaneTests.Show(bottom: false, theme: theme, inspectorContent: stack);
        return (window, stack);
    }

    private static List<PanelStackItem> Items(PanelStack stack) => stack.Items.OfType<PanelStackItem>().ToList();

    private static Color Colour(object? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    // The item's content presenter (the strip has its own PART_ContentPresenter).
    private static ContentPresenter Content(PanelStackItem item) =>
        item.GetVisualDescendants().OfType<Reveal>().First(r => r.Name == ExpanderTheme.RevealPart)
            .GetVisualDescendants().OfType<ContentPresenter>().First(c => c.Name == "PART_ContentPresenter");

    private static ToggleButton Strip(PanelStackItem item) =>
        item.GetVisualDescendants().OfType<ToggleButton>().First(t => t.Name == ExpanderTheme.HeaderPart);

    [AvaloniaFact]
    public void Items_AreCardsInTheExpanderLook_StackedSpacingSApart()
    {
        var (window, stack) = Show();
        try
        {
            var items = Items(stack);
            var expander = (ControlTheme)window.FindResource(typeof(Expander))!;
            Assert.Same(expander, ((ControlTheme)window.FindResource(typeof(PanelStackItem))!).BasedOn);
            Assert.All(items, item => Assert.IsAssignableFrom<Expander>(item));
            for (var i = 1; i < items.Count; i++)
            {
                var above = items[i - 1].TranslatePoint(new Point(0, items[i - 1].Bounds.Height), stack)!.Value.Y;
                var top = items[i].TranslatePoint(default, stack)!.Value.Y;
                Assert.Equal(above + Style.Spacing.S, top, 0.01);
            }
            // Each is a solid card (the Expander's card colour), as wide as the stack.
            var card = items[0].GetVisualDescendants().OfType<Style.SquircleBorder>().First(b => b.Name == ExpanderTheme.CardPart);
            var plain = new Expander();
            ((Panel)window.GetVisualDescendants().OfType<Panel>().First()).Children.Add(plain);
            SplitPaneTests.Pump(window);
            Assert.Equal(Colour(plain.Background), Colour(card.Background));
            Assert.Equal(255, Colour(card.Background).A);
            Assert.Equal(stack.Bounds.Width, items[0].Bounds.Width, 0.01);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheTitleStrip_ShowsTheCaretIconAndHeader()
    {
        var (window, stack) = Show();
        try
        {
            var item = Items(stack)[0];
            var strip = Strip(item);
            var caret = strip.GetVisualDescendants().OfType<Icon>().First(i => i.Name == ExpanderTheme.CaretPart);
            var icon = strip.GetVisualDescendants().OfType<Icon>().First(i => i.Name == PanelStackTheme.IconPart);
            var header = strip.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == PanelStackTheme.HeaderPart);
            Assert.True(icon.IsVisible);
            Assert.Same(Icons.SquaresFour, icon.Data);
            Assert.Equal("Layers", header.Content);
            var caretRight = caret.TranslatePoint(new Point(caret.Bounds.Width, 0), strip)!.Value.X;
            var iconLeft = icon.TranslatePoint(default, strip)!.Value.X;
            var iconRight = icon.TranslatePoint(new Point(icon.Bounds.Width, 0), strip)!.Value.X;
            var headerLeft = header.TranslatePoint(default, strip)!.Value.X;
            Assert.Equal(caretRight + Style.Spacing.S, iconLeft, 0.01);
            Assert.Equal(iconRight + Style.Spacing.S, headerLeft, 0.01);

            // Without an icon, the header follows the caret.
            item.Icon = null;
            SplitPaneTests.Pump(window);
            Assert.False(icon.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ChoosingTheStrip_CollapsesAndExpandsTheCard()
    {
        var (window, stack) = Show();
        try
        {
            var items = Items(stack);
            var colour = items[2];
            Assert.False(colour.IsExpanded);
            Assert.False(Content(colour).IsEffectivelyVisible);
            var tall = items[1].Bounds.Height;

            var strip = Strip(items[1]);
            var point = strip.TranslatePoint(new Point(strip.Bounds.Width / 2, strip.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            SplitPaneTests.Pump(window);
            Assert.False(items[1].IsExpanded);
            Assert.True(items[1].Bounds.Height < tall);
            Assert.Equal(strip.Bounds.Height + 2, items[1].Bounds.Height, 1);

            items[1].IsExpanded = true;
            colour.IsExpanded = true;
            SplitPaneTests.Pump(window);
            Assert.Equal(tall, items[1].Bounds.Height, 0.01);
            Assert.True(Content(colour).IsEffectivelyVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ItemsThatAreNotPanelStackItems_AreWrappedInOne()
    {
        var stack = new PanelStack { Items = { new TextBlock { Text = "Loose" } } };
        var window = SplitPaneTests.Show(bottom: false, inspectorContent: stack);
        try
        {
            Assert.IsType<PanelStackItem>(stack.ContainerFromIndex(0));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheStack_ScrollsWhenTallerThanItsPanel()
    {
        var stack = new PanelStack();
        for (var i = 0; i < 12; i++)
            stack.Items.Add(new PanelStackItem { Header = $"Panel {i}", IsExpanded = true, Content = new Border { Height = 120 } });
        var window = SplitPaneTests.Show(frameHeight: 600, bottom: false, inspectorContent: stack);
        try
        {
            var scroller = stack.GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Name == PanelStackTheme.ScrollViewerPart);
            Assert.True(scroller.Extent.Height > scroller.Viewport.Height);
            Assert.Equal(ScrollBarVisibility.Disabled, scroller.HorizontalScrollBarVisibility);
        }
        finally
        {
            window.Close();
        }
    }
}
