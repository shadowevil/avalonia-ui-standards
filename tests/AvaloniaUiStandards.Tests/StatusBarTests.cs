using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Catalog;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// The status bar: its height, zones and gaps, the clickable items' inset fill and full-height hit target, shortening
/// then hiding by priority, text and icon styles, the live message and automation, Tab order in the window, and the
/// renders (screenshots/statusbar-*.png).
/// </summary>
public class StatusBarTests
{
    // --- Size and layout --------------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData(DensityStyle.Normal)]
    [InlineData(DensityStyle.Compact)]
    public void Bar_Is24High_AtEveryDensity_InTheWindowAndOnItsOwn(DensityStyle density)
    {
        var bar = new StatusBar { Message = "Ready", Items = { Clickable("100 %") } };
        var window = Window(bar, 800, density);
        var alone = new StatusBar { Message = "Ready" };
        var host = Host(alone, 400, density);
        try
        {
            Assert.Equal(24, Part<ContentPresenter>(window, StandardsWindowTheme.StatusBarPart).Bounds.Height);
            Assert.Equal(24, bar.Bounds.Height);
            Assert.Equal(24, bar.Items[0].Bounds.Height);
            Assert.Equal(24, alone.Bounds.Height);
            // Any other status content keeps the slot's 16 px side padding; the Module pads itself.
            Assert.Equal(new Thickness(0), Part<ContentPresenter>(window, StandardsWindowTheme.StatusBarPart).Padding);
            window.StatusBar = new TextBlock { Text = "Ready" };
            Pump(window);
            Assert.Equal(new Thickness(16, 0), Part<ContentPresenter>(window, StandardsWindowTheme.StatusBarPart).Padding);
            Assert.Equal(24, Part<ContentPresenter>(window, StandardsWindowTheme.StatusBarPart).Bounds.Height);
        }
        finally
        {
            window.Close();
            host.Close();
        }
    }

    [AvaloniaFact]
    public void Zones_PlaceItems_WithTheGapsAndPadding()
    {
        var (a, b) = (Text("Alpha"), Text("Beta"));
        var progress = new StatusProgress { Value = 40, Text = "40 %" };
        StatusBar.SetZone(progress, StatusZone.Center);
        var (c, d, e) = (Text("Gamma"), Clickable("Delta"), Clickable("Epsilon"));
        foreach (var item in new Control[] { c, d, e })
            StatusBar.SetZone(item, StatusZone.Trailing);
        var bar = new StatusBar { Message = "Ready", Items = { a, b, progress, c, d, e } };
        var window = Host(bar, 900);
        try
        {
            var message = bar.MessageBlock;
            Assert.Equal(16, X(bar, message));
            // Text items 16 apart.
            Assert.Equal(16 + message.Bounds.Width + 16, X(bar, a));
            Assert.Equal(X(bar, a) + a.Bounds.Width + 16, X(bar, b));
            // Clickable items: the last one's text ends 16 px from the edge (its fill 8 px), fills 4 apart, and the text
            // before them 8 px from the fill, so their texts are 16 apart.
            Assert.Equal(900 - 8, X(bar, e) + e.Bounds.Width);
            Assert.Equal(X(bar, e) - 4, X(bar, d) + d.Bounds.Width);
            Assert.Equal(X(bar, d) - 8, X(bar, c) + c.Bounds.Width);
            // The centre zone is centred.
            Assert.True(Math.Abs((900 - progress.Bounds.Width) / 2 - X(bar, progress)) <= 0.5);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LeadingClickableItem_HasItsTextAt16_AndItsFill8PxIn()
    {
        var first = Clickable("Image size");
        var bar = new StatusBar { Items = { first, Text("Ready") } };
        var window = Host(bar, 600);
        try
        {
            Assert.Equal(8, X(bar, first));
            var presenter = Part<ContentPresenter>(first, ButtonParts.ContentPart);
            // The text is 8 px into the fill (the 1 px border and 7 px padding).
            Assert.Equal(16, presenter.TranslatePoint(new Point(presenter.Padding.Left, 0), bar)!.Value.X);
            Assert.Equal(X(bar, first) + first.Bounds.Width + 8, X(bar, bar.Items[1]));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ClickableItem_Fill_Is20High_Centred_WhileTheHitTargetSpansTheBar()
    {
        var zoom = new DropDownButton { Content = "100 %", Flyout = new MenuFlyout { Items = { new MenuItem { Header = "200 %" } } } };
        zoom.Classes.Add("subtle");
        StatusBar.SetZone(zoom, StatusZone.Trailing);
        var bar = new StatusBar { Message = "Ready", Items = { zoom } };
        var window = Host(bar, 600);
        try
        {
            Assert.Equal(24, zoom.Bounds.Height);
            var chrome = Part<SquircleBorder>(zoom, ButtonParts.ChromePart);
            Assert.Equal(20, chrome.Bounds.Height);
            Assert.Equal(2, chrome.TranslatePoint(default, zoom)!.Value.Y);
            Assert.Equal(new CornerRadius(8), chrome.CornerRadius);
            // The focus ring follows the fill, 4 px outside it.
            Assert.Equal(new Thickness(-4, -2), Part<SquircleBorder>(zoom, FocusRing.Part).Margin);
            // The 2 px above and below the fill still hit the button.
            foreach (var y in new[] { 0.5, 23.5 })
            {
                var point = zoom.TranslatePoint(new Point(zoom.Bounds.Width / 2, y), window)!.Value;
                var hit = window.InputHitTest(point) as Visual;
                Assert.True(hit == zoom || hit?.FindAncestorOfType<DropDownButton>() == zoom, $"y {y} hits {hit}");
            }
            // The chevron is 4 px after the text and 8 px from the fill's end.
            var glyph = Part<PathIcon>(zoom, DropDownButtonTheme.GlyphPart);
            Assert.Equal(zoom.Bounds.Width - 8, glyph.TranslatePoint(new Point(glyph.Bounds.Width, 0), zoom)!.Value.X);
            // The padding is inside the 1 px border.
            Assert.Equal(new Thickness(7, 0, 4, 0), zoom.Padding);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Shortening and hiding ---------------------------------------------------------------

    [AvaloniaFact]
    public void NarrowBar_ShortensEveryItemFirst_ThenHides_HighestPriorityFirst_TrailingEndFirstOnTies()
    {
        var size = Clickable("4000 × 3000 px · 300 ppi");
        StatusBar.SetShortTexts(size, ["4000 × 3000 px"]);
        StatusBar.SetPriority(size, 2);
        var frames = Text("30 fps · High refresh rate: off");
        StatusBar.SetShortTexts(frames, ["30 fps · High refresh: off", "30 fps"]);
        StatusBar.SetPriority(frames, 3);
        var mode = Text("RGB/8 · sRGB");
        StatusBar.SetShortTexts(mode, ["RGB/8"]);
        StatusBar.SetPriority(mode, 1);
        var zoom = Clickable("100 %");
        foreach (var item in new Control[] { frames, mode, zoom })
            StatusBar.SetZone(item, StatusZone.Trailing);
        var bar = new StatusBar { Message = "Ready", Items = { size, frames, mode, zoom } };
        var window = Host(bar, 1000);
        try
        {
            string Shown() => string.Join(" | ", new Control[] { size, frames, mode, zoom }
                .Where(bar.IsShown).Select(i => i is TextBlock t ? t.Text : (string?)((Button)i).Content));

            Assert.Equal("4000 × 3000 px · 300 ppi | 30 fps · High refresh rate: off | RGB/8 · sRGB | 100 %", Shown());
            Assert.Equal(bar.Bounds.Width, bar.Width);

            // Narrowing step by step: every item takes its shorter forms before any hides, highest priority first.
            var states = new List<string> { Shown() };
            for (var width = 1000.0; width >= 60; width -= 4)
            {
                bar.Width = width;
                Pump(window);
                AssertFits(bar);
                if (Shown() != states[^1])
                    states.Add(Shown());
            }
            Assert.Equal(
            [
                "4000 × 3000 px · 300 ppi | 30 fps · High refresh rate: off | RGB/8 · sRGB | 100 %",
                "4000 × 3000 px · 300 ppi | 30 fps · High refresh: off | RGB/8 · sRGB | 100 %",
                "4000 × 3000 px · 300 ppi | 30 fps | RGB/8 · sRGB | 100 %",
                "4000 × 3000 px | 30 fps | RGB/8 · sRGB | 100 %",
                "4000 × 3000 px | 30 fps | RGB/8 | 100 %",
                // The readout hides, and the colour mode takes its full text again in the room it left.
                "4000 × 3000 px | RGB/8 · sRGB | 100 %",
                "4000 × 3000 px | RGB/8 | 100 %",
                "RGB/8 · sRGB | 100 %",
                "RGB/8 | 100 %",
                "100 %",
                "",
            ], states);
            // The message never hides; with nothing left to hide it is trimmed.
            Assert.True(bar.MessageBlock.IsEffectivelyVisible);
            bar.Width = 40;
            Pump(window);
            Assert.True(bar.MessageBlock.Bounds.Width <= 40 - 16);

            // Wide again, every item shows its full text.
            bar.Width = 1000;
            Pump(window);
            Assert.Equal("4000 × 3000 px · 300 ppi | 30 fps · High refresh rate: off | RGB/8 · sRGB | 100 %", Shown());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ShortenedItem_ShowsTheNewShortForm_WhenItsTextsChange()
    {
        var frames = Text("30 fps · High refresh rate: off");
        StatusBar.SetShortTexts(frames, ["30 fps"]);
        StatusBar.SetZone(frames, StatusZone.Trailing);
        var bar = new StatusBar { Message = "A long status message for a narrow bar", Items = { frames } };
        var window = Host(bar, 340);
        try
        {
            Assert.Equal("30 fps", frames.Text);
            StatusBar.SetShortTexts(frames, ["60 fps"]);
            frames.Text = "60 fps · High refresh rate: on";
            Pump(window);
            Assert.Equal("60 fps", frames.Text);
            bar.Width = 1000;
            Pump(window);
            Assert.Equal("60 fps · High refresh rate: on", frames.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HidingAnItem_GivesItsRoomBackToAShortenedItem()
    {
        // The size shortens first (it has a shorter form); when the readout (higher priority, no shorter form) still has
        // to hide, the size takes its full text again in the room the readout left.
        var size = Text("4000 × 3000 px · 300 ppi");
        StatusBar.SetShortTexts(size, ["4000 × 3000 px"]);
        StatusBar.SetPriority(size, 1);
        var frames = Text("30 fps · High refresh rate: off");
        StatusBar.SetPriority(frames, 2);
        StatusBar.SetZone(frames, StatusZone.Trailing);
        var bar = new StatusBar { Message = "Ready", Items = { size, frames } };
        var window = Host(bar, 1000);
        try
        {
            // Room for the message and the full size, but not for the short size beside the readout.
            var full = size.Bounds.Width;
            bar.Width = 16 + bar.MessageBlock.Bounds.Width + 16 + full + 16 + 4;
            Pump(window);
            Assert.False(bar.IsShown(frames));
            Assert.True(bar.IsShown(size));
            Assert.Equal("4000 × 3000 px · 300 ppi", size.Text);
            AssertFits(bar);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ShortenedItem_TakesANewTextOfItsOwn_WithoutNewShortTexts()
    {
        // The app sets a short text under the shorter form, leaving ShortTexts as they were: the bar measures again
        // and shows the new text, which fits.
        var frames = Text("30 fps · High refresh rate: off");
        StatusBar.SetShortTexts(frames, ["30 fps"]);
        StatusBar.SetZone(frames, StatusZone.Trailing);
        var bar = new StatusBar { Message = "A long status message for a narrow bar", Items = { frames } };
        var window = Host(bar, 340);
        try
        {
            Assert.Equal("30 fps", frames.Text);
            frames.Text = "Paused";
            Pump(window);
            Assert.Equal("Paused", frames.Text);
            Assert.True(bar.IsShown(frames));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void NewTextUnderAShortForm_ShowsTheShortForm_BeforeTheNextLayout()
    {
        // As the catalog's readout updates: new shorter forms, then the new full text. The full text never shows in
        // the short slot, not even until the bar lays out again.
        var frames = Text("30 fps · High refresh rate: off");
        StatusBar.SetShortTexts(frames, ["30 fps"]);
        StatusBar.SetZone(frames, StatusZone.Trailing);
        var bar = new StatusBar { Message = "A long status message for a narrow bar", Items = { frames } };
        var window = Host(bar, 340);
        try
        {
            Assert.Equal("30 fps", frames.Text);
            StatusBar.SetShortTexts(frames, ["60 fps"]);
            frames.Text = "60 fps · High refresh rate: on";
            Assert.Equal("60 fps", frames.Text);
            Pump(window);
            Assert.Equal("60 fps", frames.Text);
            bar.Width = 1000;
            Pump(window);
            Assert.Equal("60 fps · High refresh rate: on", frames.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BoundText_KeepsItsBinding_ThroughShorteningAndRestoring()
    {
        // The app's text comes from a binding to its own object.
        var source = new TextBlock { Text = "30 fps · High refresh rate: off" };
        var frames = new TextBlock();
        frames.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding(nameof(TextBlock.Text)) { Source = source });
        StatusBar.SetShortTexts(frames, ["30 fps"]);
        StatusBar.SetZone(frames, StatusZone.Trailing);
        var bar = new StatusBar { Message = "A long status message for a narrow bar", Items = { frames } };
        var window = Host(bar, 340);
        try
        {
            Assert.Equal("30 fps", frames.Text);
            // A value from the binding while shortened is the new full text.
            source.Text = "60 fps · High refresh rate: on";
            Assert.Equal("30 fps", frames.Text);
            bar.Width = 1000;
            Pump(window);
            Assert.Equal("60 fps · High refresh rate: on", frames.Text);
            // Restored, the binding still drives the text.
            source.Text = "90 fps · High refresh rate: on";
            Pump(window);
            Assert.Equal("90 fps · High refresh rate: on", frames.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void NewTextUnderAShortForm_RightAfterAnItemIsAdded_LeavesTheSharedEmptyStateEmpty()
    {
        // Adding an item resets what the bar has applied to the shared empty state; a new text on a shortened item in
        // the same handler, before layout, records its form in a new state, not in the shared one.
        var frames = Text("30 fps · High refresh rate: off");
        StatusBar.SetShortTexts(frames, ["30 fps"]);
        StatusBar.SetZone(frames, StatusZone.Trailing);
        var bar = new StatusBar { Message = "A long status message for a narrow bar", Items = { frames } };
        var window = Host(bar, 340);
        try
        {
            Assert.Equal("30 fps", frames.Text);
            bar.Items.Add(Text("Layer"));
            frames.Text = "60 fps · High refresh rate: on";
            Assert.Empty(StatusBar.State.Empty.Texts);
            Assert.Empty(StatusBar.State.Empty.Hidden);
            Pump(window);
            Assert.Empty(StatusBar.State.Empty.Texts);
            Assert.Empty(StatusBar.State.Empty.Hidden);
            Assert.Equal("30 fps", frames.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HiddenItems_TakeNoFocus()
    {
        var (a, b) = (Clickable("Alpha"), Clickable("Beta"));
        StatusBar.SetPriority(b, 1);
        var bar = new StatusBar { Message = "Ready", Items = { a, b } };
        var window = Host(bar, 160);
        try
        {
            Assert.False(bar.IsShown(b));
            Assert.False(b.IsEffectivelyVisible);
            a.Focus(NavigationMethod.Tab);
            Press(window, Key.Tab);
            Assert.NotSame(b, Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    // --- Styles ------------------------------------------------------------------------------

    [AvaloniaFact]
    public void Text_IsCaptionInSecondaryText_Icons16_AndMenusKeepTheirOwnLook()
    {
        var text = Text("Ready");
        var label = new TextBlock { Text = "RGB/8" };
        var icon = new Icon { Data = Icons.Palette };
        var menuItem = new MenuItem { Header = "RGB/16", Icon = new Icon { Data = Icons.Palette } };
        var mode = new DropDownButton
        {
            Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { icon, label } },
            Flyout = new MenuFlyout { Items = { menuItem } },
        };
        mode.Classes.Add("subtle");
        var disabled = Clickable("Off");
        disabled.IsEnabled = false;
        var bar = new StatusBar { Message = "Ready", Items = { text, mode, disabled } };
        var window = Host(bar, 700);
        try
        {
            var secondary = ((ISolidColorBrush)window.FindResource("SystemControlForegroundBaseMediumBrush")!).Color;
            foreach (var block in new[] { bar.MessageBlock, text, label })
            {
                Assert.Equal(12, block.FontSize);
                Assert.Equal(secondary, ((ISolidColorBrush)block.Foreground!).Color);
            }
            Assert.Equal(new Size(16, 16), icon.Bounds.Size);
            // A string content: Caption size, secondary text, and the disabled colour when disabled.
            var presenter = Part<ContentPresenter>(disabled, ButtonParts.ContentPart);
            Assert.Equal(12, disabled.FontSize);
            Assert.NotEqual(secondary, ((ISolidColorBrush)presenter.Foreground!).Color);

            mode.Flyout!.ShowAt(mode);
            Pump(window);
            Assert.True(mode.Flyout.IsOpen);
            var header = menuItem.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "RGB/16");
            Assert.Equal(14, header.FontSize);
            mode.Flyout.Hide();
        }
        finally
        {
            window.Close();
        }
    }

    // --- Automation and keyboard ------------------------------------------------------------

    [AvaloniaFact]
    public void Automation_ReportsAStatusBar_NamedStatus_WithAPoliteLiveMessage()
    {
        var bar = new StatusBar { Message = "Ready" };
        var window = Host(bar, 400);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(bar);
            Assert.Equal(AutomationControlType.StatusBar, peer.GetAutomationControlType());
            Assert.Equal("Status", peer.GetName());

            var message = ControlAutomationPeer.CreatePeerForElement(bar.MessageBlock);
            Assert.Equal(AutomationLiveSetting.Polite, message.GetLiveSetting());
            Assert.Equal("Ready", message.GetName());
            bar.Message = "Saved";
            Pump(window);
            Assert.Equal("Saved", message.GetName());
            Assert.Contains(message, peer.GetChildren().SelectMany(c => c.GetChildren()));
            Assert.False(bar.MessageBlock.Focusable);

            // The resource sets the default name; the app's own name wins.
            window.Resources["StatusBarText"] = "Statut";
            Pump(window);
            Assert.Equal("Statut", peer.GetName());
            AutomationProperties.SetName(bar, "Editor status");
            Assert.Equal("Editor status", peer.GetName());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Tab_ReachesClickableItemsLast_InVisualOrder_SkippingText()
    {
        // Declared out of zone order: the trailing item first.
        var zoom = Clickable("100 %");
        StatusBar.SetZone(zoom, StatusZone.Trailing);
        var progress = new StatusProgress { Value = 40, Text = "40 %", IsCancelable = true };
        StatusBar.SetZone(progress, StatusZone.Center);
        var size = Clickable("4000 × 3000 px");
        var bar = new StatusBar { Message = "Ready", Items = { zoom, progress, Text("Layer"), size } };
        var content = new Button { Content = "In the panel" };
        var window = Window(bar, 900, content: content);
        try
        {
            content.Focus(NavigationMethod.Tab);
            var visited = new List<object?>();
            for (var i = 0; i < 3; i++)
            {
                Press(window, Key.Tab);
                visited.Add(Focused(window));
            }
            Assert.Equal([size, progress.CancelButton, zoom], visited);
            // Shift+Tab from the first status item goes back into the content.
            Press(window, Key.Tab, RawInputModifiers.Shift);
            Press(window, Key.Tab, RawInputModifiers.Shift);
            Press(window, Key.Tab, RawInputModifiers.Shift);
            Assert.Same(content, Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Progress_ShowsABarOrARing_WithItsText_AndCancels()
    {
        var progress = new StatusProgress { Value = 42, Text = "42 %", IsCancelable = true };
        StatusBar.SetZone(progress, StatusZone.Center);
        var bar = new StatusBar { Message = "Exporting", Items = { progress } };
        var window = Host(bar, 700);
        try
        {
            Assert.Equal(160, progress.BarPart.Bounds.Width);
            Assert.Equal(42, progress.BarPart.Value);
            Assert.False(progress.RingPart.IsVisible);
            Assert.Equal(progress.BarPart.Bounds.Right + 8, progress.TextPart.TranslatePoint(default, progress)!.Value.X);
            Assert.Equal(new Size(20, 24), progress.CancelButton.Bounds.Size);
            Assert.Equal("Cancel", AutomationProperties.GetName(progress.CancelButton));
            var cancelled = 0;
            progress.CancelRequested += (_, _) => cancelled++;
            progress.CancelButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(1, cancelled);

            progress.Kind = StatusProgressKind.Ring;
            progress.IsIndeterminate = true;
            progress.IsCancelable = false;
            Pump(window);
            Assert.Equal(new Size(16, 16), progress.RingPart.Bounds.Size);
            Assert.True(progress.RingPart.IsIndeterminate);
            Assert.False(progress.BarPart.IsVisible);
            Assert.False(progress.CancelButton.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Rows ------------------------------------------------------------------------------------

    private static Control? OwnerOf(AutomationPeer peer) => (peer as ControlAutomationPeer)?.Owner;

    private static StatusBarRow Row(params Control[] items)
    {
        var row = new StatusBarRow();
        foreach (var item in items)
            row.Items.Add(item);
        return row;
    }

    [AvaloniaTheory]
    [InlineData(DensityStyle.Normal)]
    [InlineData(DensityStyle.Compact)]
    public void Rows_Are24High_StackedAboveTheMainRow_InOrder_WithNothingBetween(DensityStyle density)
    {
        var (top, middle) = (Row(Text("Top"), Clickable("Top item")), Row(Text("Middle")));
        var main = Clickable("Main item");
        var bar = new StatusBar { Message = "Ready", Items = { main }, Rows = { top, middle } };
        var window = Window(bar, 800, density);
        try
        {
            Assert.Equal(72, bar.Bounds.Height);
            Assert.Equal(0, top.Bounds.Y);
            Assert.Equal(24, top.Bounds.Height);
            Assert.Equal(24, middle.Bounds.Y);
            Assert.Equal(24, middle.Bounds.Height);
            Assert.Equal(24, ((Control)top.Items[1]).Bounds.Height);
            // The main row is the bottom 24 px.
            Assert.Equal(48, main.TranslatePoint(default, bar)!.Value.Y);
            Assert.InRange(bar.MessageBlock.TranslatePoint(default, bar)!.Value.Y + bar.MessageBlock.Bounds.Height / 2, 59, 61);

            // Collection order is top to bottom.
            bar.Rows.Move(1, 0);
            Pump(window);
            Assert.Equal(0, middle.Bounds.Y);
            Assert.Equal(24, top.Bounds.Y);
            // No row has a divider or space around it: the same text edge, 16 px in.
            Assert.Equal(16, X(bar, (Control)middle.Items[0]));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheWindowsStatusSlot_GrowsWithTheBar_AndShrinksWhenARowHides()
    {
        var stats = Row(Text("Cursor 12, 40"));
        var bar = new StatusBar { Message = "Ready", Rows = { stats } };
        var window = Window(bar, 800);
        try
        {
            var slot = Part<ContentPresenter>(window, StandardsWindowTheme.StatusBarPart);
            Assert.Equal(48, slot.Bounds.Height);
            Assert.Equal(new Thickness(0), slot.Padding);
            stats.IsVisible = false;
            Pump(window);
            Assert.Equal(24, bar.Bounds.Height);
            Assert.Equal(24, slot.Bounds.Height);
            stats.IsVisible = true;
            Pump(window);
            Assert.Equal(48, slot.Bounds.Height);
            // The glass panel gives the room back: the panel's bottom sits above the whole bar.
            var panel = Part<Control>(window, StandardsWindowTheme.ContentPanelPart);
            Assert.True(panel.TranslatePoint(new Point(0, panel.Bounds.Height), window)!.Value.Y <= window.Bounds.Height - 48);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EachRow_ShortensAndHidesItsOwnItems()
    {
        // The stats row is crowded and the main row is not: only the stats row loses items.
        var cursor = Text("Cursor 1280, 640 px");
        StatusBar.SetShortTexts(cursor, ["1280, 640"]);
        StatusBar.SetPriority(cursor, 1);
        var memory = Text("Memory 412 MB");
        StatusBar.SetZone(memory, StatusZone.Trailing);
        StatusBar.SetPriority(memory, 2);
        var selection = Text("Selection 1200 × 800 px");
        var stats = Row(cursor, selection, memory);
        var zoom = Clickable("100 %");
        StatusBar.SetZone(zoom, StatusZone.Trailing);
        var bar = new StatusBar { Message = "Ready", Items = { zoom }, Rows = { stats } };
        var window = Host(bar, 560);
        try
        {
            Assert.Equal("Cursor 1280, 640 px", cursor.Text);
            Assert.True(stats.IsShown(memory));
            bar.Width = 330;
            Pump(window);
            Assert.False(stats.IsShown(memory));
            Assert.True(stats.IsShown(cursor));
            // The main row is untouched: its item and message show in full.
            Assert.True(bar.IsShown(zoom));
            Assert.Equal("Ready", bar.MessageBlock.Text);
            Assert.True(bar.MessageBlock.IsVisible);
            // Widening restores the stats row without a change to the main row.
            bar.Width = 560;
            Pump(window);
            Assert.Equal("Cursor 1280, 640 px", cursor.Text);
            Assert.True(stats.IsShown(memory));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ARowsIsVisible_ShowsAndHidesIt_AndTakesItsItemsOutOfTheTabOrder()
    {
        var statsButton = Clickable("Units");
        var stats = Row(Text("Cursor"), statsButton);
        var main = Clickable("Zoom");
        var bar = new StatusBar { Message = "Ready", Items = { main }, Rows = { stats } };
        var content = new Button { Content = "In the panel" };
        var window = Window(bar, 800, content: content);
        try
        {
            content.Focus(NavigationMethod.Tab);
            Press(window, Key.Tab);
            Assert.Same(statsButton, Focused(window));
            content.Focus(NavigationMethod.Tab);
            stats.IsVisible = false;
            Pump(window);
            Assert.Equal(24, bar.Bounds.Height);
            Press(window, Key.Tab);
            Assert.Same(main, Focused(window));
            Assert.Equal(0, main.TranslatePoint(default, bar)!.Value.Y);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheBar_HasOneStatusPeer_AndOneLiveRegion_OnTheMainRow()
    {
        var stats = Row(Text("Cursor"), Clickable("Units"));
        var bar = new StatusBar { Message = "Ready", Items = { Clickable("Zoom") }, Rows = { stats, Row(Text("More")) } };
        var window = Window(bar, 800);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(bar);
            var all = new List<AutomationPeer>();
            void Walk(AutomationPeer p)
            {
                all.Add(p);
                foreach (var child in p.GetChildren())
                    Walk(child);
            }
            Walk(peer);
            Assert.Single(all, p => p.GetAutomationControlType() == AutomationControlType.StatusBar);
            Assert.Single(all, p => p.GetLiveSetting() == AutomationLiveSetting.Polite);
            Assert.Equal(bar.MessageBlock, OwnerOf(all.Single(p => p.GetLiveSetting() == AutomationLiveSetting.Polite)));
            Assert.Equal("Status", peer.GetName());
            // The text and clickable items of the rows are still there to read and use.
            Assert.Contains(all, p => OwnerOf(p) == stats.Items[0]);
            Assert.Contains(all, p => OwnerOf(p) == stats.Items[1]);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Tab_RunsThroughTheRowsInOrder_ThenTheMainRow()
    {
        var (a, b, c) = (Clickable("A"), Clickable("B"), Clickable("C"));
        StatusBar.SetZone(c, StatusZone.Trailing);
        var (d, e) = (Clickable("D"), Clickable("E"));
        var main = Clickable("Main");
        var bar = new StatusBar { Message = "Ready", Items = { main }, Rows = { Row(c, a), Row(b), Row(d, e) } };
        var content = new Button { Content = "In the panel" };
        var window = Window(bar, 900, content: content);
        try
        {
            content.Focus(NavigationMethod.Tab);
            var visited = new List<object?>();
            for (var i = 0; i < 6; i++)
            {
                Press(window, Key.Tab);
                visited.Add(Focused(window));
            }
            // Within a row: leading, then trailing (c is declared first but trails).
            Assert.Equal([a, c, b, d, e, main], visited);

            // A row added later takes its place at the top.
            var extra = Clickable("Extra");
            bar.Rows.Insert(0, Row(extra));
            Pump(window);
            content.Focus(NavigationMethod.Tab);
            Press(window, Key.Tab);
            Assert.Same(extra, Focused(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OnlyTheBottomRow_HasTheCornerInset()
    {
        // The bottom row's last fill ends 8 px from the bar's side and 2 px above the window's bottom edge, clear of the
        // 16 px corner. The rows above keep the same side insets but never reach the bottom edge.
        var top = Clickable("Units");
        StatusBar.SetZone(top, StatusZone.Trailing);
        var zoom = new DropDownButton { Content = "100 %", Flyout = new MenuFlyout() };
        zoom.Classes.Add("subtle");
        StatusBar.SetZone(zoom, StatusZone.Trailing);
        var bar = new StatusBar { Message = "Ready", Items = { zoom }, Rows = { Row(top) } };
        var window = Window(bar, 900);
        try
        {
                        var bottom = Part<SquircleBorder>(zoom, ButtonParts.ChromePart);
            var corner = bottom.TranslatePoint(new Point(bottom.Bounds.Width, bottom.Bounds.Height), bar)!.Value;
            Assert.Equal(bar.Bounds.Width - 8, corner.X);
            Assert.Equal(bar.Bounds.Height - 2, corner.Y);
            var upper = Part<SquircleBorder>(top, ButtonParts.ChromePart);
            var upperEnd = upper.TranslatePoint(new Point(upper.Bounds.Width, upper.Bounds.Height), bar)!.Value;
            Assert.Equal(bar.Bounds.Width - 8, upperEnd.X);
            Assert.True(upperEnd.Y <= bar.Bounds.Height - 24 - 2);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Renders -------------------------------------------------------------------------------

    /// <summary>
    /// The catalog window's bottom area with a realistic status bar, in light and dark at both densities, narrow, and with a
    /// running progress item; the light Normal render hovers a clickable item at each end, and its bottom-right corner is
    /// cropped as well.
    /// </summary>
    [AvaloniaFact]
    public void Renders()
    {
        foreach (var (label, variant) in ButtonRenderTests.Themes)
        foreach (var density in new[] { DensityStyle.Normal, DensityStyle.Compact })
        {
            var name = $"statusbar-{label.ToLowerInvariant()}-{(density == DensityStyle.Compact ? "compact" : "normal")}";
            var hoverEnds = variant == ThemeVariant.Light && density == DensityStyle.Normal;
            using var bitmap = Render(variant, density, 1200, progress: false, hoverEnds);
            Save(bitmap, $"{name}.png", 120);
            if (hoverEnds)
                Save(bitmap, "statusbar-corner-light.png", 64, fromRight: 160);
        }
        using (var narrow = Render(ThemeVariant.Light, DensityStyle.Normal, 528, progress: false, hoverEnds: false))
            Save(narrow, "statusbar-narrow-light.png", 120);
        using (var running = Render(ThemeVariant.Light, DensityStyle.Normal, 1200, progress: true, hoverEnds: false))
            Save(running, "statusbar-progress-light.png", 120);
    }

    /// <summary>
    /// The same window with a stats row above the main row (statusbar-rows-*.png): light and dark at both densities,
    /// narrow, the bottom-right corner, and the catalog's status bar page with both rows.
    /// </summary>
    [AvaloniaFact]
    public void RendersRows()
    {
        foreach (var (label, variant) in ButtonRenderTests.Themes)
        foreach (var density in new[] { DensityStyle.Normal, DensityStyle.Compact })
        {
            var name = $"statusbar-rows-{label.ToLowerInvariant()}-{(density == DensityStyle.Compact ? "compact" : "normal")}";
            using var bitmap = Render(variant, density, 1200, progress: false, hoverEnds: false, rows: true);
            Save(bitmap, $"{name}.png", 120);
            if (variant == ThemeVariant.Light && density == DensityStyle.Normal)
                Save(bitmap, "statusbar-rows-corner-light.png", 64, fromRight: 200);
        }
        using (var narrow = Render(ThemeVariant.Light, DensityStyle.Normal, 400, progress: false, hoverEnds: false, rows: true))
            Save(narrow, "statusbar-rows-narrow-light.png", 120);

        foreach (var (label, variant) in ButtonRenderTests.Themes)
        {
            var window = new MainWindow
            {
                Width = 1200,
                Height = 800,
                RequestedThemeVariant = variant,
                Background = new SolidColorBrush(Color.Parse(variant == ThemeVariant.Dark ? "#3A3D42" : "#C9CDD3")),
            };
            window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true });
            window.SetRenderScaling(Sheet.Scale);
            window.Show();
            window.Activate();
            window.SelectPage(CatalogPages.All.ToList().FindIndex(p => p.Title == "Status bar"));
            Pump(window);
            using var frame = Frame.Capture(window);
            frame.Save($"statusbar-rows-catalog-page-{label.ToLowerInvariant()}.png");
            window.Close();
        }
    }

    private static Frame Render(ThemeVariant variant, DensityStyle density, double width, bool progress, bool hoverEnds, bool rows = false)
    {
        var window = new MainWindow
        {
            Width = width,
            Height = 600,
            RequestedThemeVariant = variant,
            Background = new SolidColorBrush(Color.Parse(variant == ThemeVariant.Dark ? "#3A3D42" : "#C9CDD3")),
        };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true, Density = density });
        window.SetRenderScaling(Sheet.Scale);

        var size = Clickable("4000 × 3000 px · 300 ppi");
        StatusBar.SetShortTexts(size, ["4000 × 3000 px"]);
        StatusBar.SetPriority(size, 2);
        var frames = Text(FrameStatus.Texts(30, false, null)[0]);
        StatusBar.SetShortTexts(frames, FrameStatus.Texts(30, false, null)[1..]);
        StatusBar.SetPriority(frames, 3);
        var mode = new DropDownButton
        {
            Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { new Icon { Data = Icons.Palette }, new TextBlock { Text = "RGB/8 · sRGB" } } },
            Flyout = new MenuFlyout(),
        };
        mode.Classes.Add("subtle");
        StatusBar.SetShortTexts(mode, ["RGB/8"]);
        StatusBar.SetPriority(mode, 1);
        var zoom = new DropDownButton { Content = "100 %", Flyout = new MenuFlyout() };
        zoom.Classes.Add("subtle");
        foreach (var item in new Control[] { frames, mode, zoom })
            StatusBar.SetZone(item, StatusZone.Trailing);
        var bar = new StatusBar { Message = hoverEnds ? null : progress ? "Exporting poster.png…" : "Ready", Items = { size, frames, mode, zoom } };
        if (progress)
        {
            var running = new StatusProgress { Value = 42, Text = "42 %", IsCancelable = true };
            StatusBar.SetZone(running, StatusZone.Center);
            bar.Items.Insert(1, running);
        }
        if (rows)
        {
            var cursor = Text("Cursor 1280, 640 px");
            StatusBar.SetShortTexts(cursor, ["1280, 640"]);
            StatusBar.SetPriority(cursor, 2);
            var selection = Text("Selection 1200 × 800 px");
            StatusBar.SetShortTexts(selection, ["1200 × 800"]);
            StatusBar.SetPriority(selection, 3);
            var memory = Text("Memory 412 MB");
            StatusBar.SetShortTexts(memory, ["412 MB"]);
            StatusBar.SetPriority(memory, 1);
            var fps = Text("60 fps");
            var units = new DropDownButton { Content = "px", Flyout = new MenuFlyout() };
            units.Classes.Add("subtle");
            foreach (var item in new Control[] { memory, fps, units })
                StatusBar.SetZone(item, StatusZone.Trailing);
            bar.Rows.Add(Row(cursor, selection, memory, fps, units));
        }
        window.StatusBar = bar;
        window.Show();
        window.Activate();
        Pump(window);
        if (hoverEnds)
        {
            ((IPseudoClasses)size.Classes).Add(":pointerover");
            ((IPseudoClasses)zoom.Classes).Add(":pointerover");
            Pump(window);
        }
        var frame = Frame.Capture(window);
        window.Close();
        return frame;
    }

    // The bottom rows of a render (in logical px), whole width or the right-hand part.
    private static void Save(Frame frame, string name, int height, int fromRight = 0)
    {
        var rows = (int)(Sheet.Scale * height);
        var x = fromRight == 0 ? 0 : frame.Width - (int)(Sheet.Scale * fromRight);
        using var crop = Frame.Crop(frame.Bitmap, new PixelRect(x, frame.Height - rows, frame.Width - x, rows));
        var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(dir);
        using var stream = File.Create(Path.Combine(dir, name));
        crop.Save(stream, new PngBitmapEncoderOptions());
    }

    // --- Helpers -------------------------------------------------------------------------------

    private static TextBlock Text(string text) => new() { Text = text };

    private static Button Clickable(string text)
    {
        var button = new Button { Content = text };
        button.Classes.Add("subtle");
        return button;
    }

    private static void AssertFits(StatusBar bar)
    {
        var shown = bar.Items.Where(bar.IsShown).Prepend(bar.MessageBlock).OrderBy(i => X(bar, i)).ToList();
        for (var i = 1; i < shown.Count; i++)
            Assert.True(X(bar, shown[i - 1]) + shown[i - 1].Bounds.Width <= X(bar, shown[i]) + 0.5, $"{shown[i - 1]} runs into {shown[i]}");
        if (shown.Count > 1)
            Assert.True(X(bar, shown[^1]) + shown[^1].Bounds.Width <= bar.Bounds.Width + 0.5, "the last item runs past the bar");
    }

    private static double X(Visual bar, Visual item) => item.TranslatePoint(default, bar)!.Value.X;

    private static Window Host(StatusBar bar, double width, DensityStyle density = DensityStyle.Normal)
    {
        bar.Width = width;
        bar.HorizontalAlignment = HorizontalAlignment.Left;
        bar.VerticalAlignment = VerticalAlignment.Top;
        var window = new Window { Width = 1200, Height = 400, Content = bar };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true, Density = density });
        window.Show();
        Pump(window);
        return window;
    }

    private static StandardsWindow Window(StatusBar bar, double width, DensityStyle density = DensityStyle.Normal, Control? content = null)
    {
        var window = new StandardsWindow { Width = width, Height = 500, StatusBar = bar, Content = content ?? new TextBlock { Text = "Content" } };
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { ReduceMotion = true, Density = density });
        window.Show();
        window.Activate();
        Pump(window);
        return window;
    }

    private static IInputElement? Focused(Window window) => window.FocusManager!.GetFocusedElement();

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, null);
        window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Pump(window);
    }

    private static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static void Pump(Window window)
    {
        for (var i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
        Dispatcher.UIThread.RunJobs();
    }
}
