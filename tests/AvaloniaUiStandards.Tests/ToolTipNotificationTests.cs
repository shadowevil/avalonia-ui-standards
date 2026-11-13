using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;

using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>The ToolTip and notification card themes, the status colours, and their sheets.</summary>
public class ToolTipNotificationTests
{
    // --- ToolTip ---------------------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void ToolTip_IsASmallSolidCard_WithCaptionText(string theme)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, target, tip) = ShowToolTip(variant);
        try
        {
            Assert.Equal(new CornerRadius(8), tip.CornerRadius);
            Assert.Equal(new Thickness(8, 4), tip.Padding);
            Assert.Equal(12, tip.FontSize);
            var card = tip.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == OverlayParts.SurfacePart);
            Assert.Equal(Effective(window.FindResource(variant, "SystemChromeMediumLowColor")), Effective(card.Background));
            Assert.Equal(Effective(window.FindResource(variant, "SystemBaseLowColor")), Effective(card.BorderBrush));
            Assert.Equal(new Thickness(1), card.BorderThickness);
            Assert.Empty(tip.GetVisualDescendants().OfType<GlassBorder>());
            // Caption text with its padding: 12 px Inter in a box 8 px taller (plus the border).
            Assert.InRange(tip.Bounds.Height, 23, 26);
        }
        finally
        {
            ToolTip.SetIsOpen(target, false);
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToolTip_Fade_FollowsReduceMotion(bool reduceMotion)
    {
        var (window, target, tip) = ShowToolTip(ThemeVariant.Light, reduceMotion);
        try
        {
            var transitions = tip.Transitions!;
            if (reduceMotion)
                Assert.Empty(transitions);
            else
                Assert.Contains(transitions, t => t is DoubleTransition { Property.Name: "Opacity" } d && d.Duration == TimeSpan.FromMilliseconds(100));
        }
        finally
        {
            ToolTip.SetIsOpen(target, false);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ToolTip_KeepsAvaloniasDelays()
    {
        var target = new Button();
        Assert.Equal(400, ToolTip.GetShowDelay(target));
        Assert.Equal(100, ToolTip.GetBetweenShowDelay(target));
    }

    // --- Notification card -----------------------------------------------------------------

    [AvaloniaFact]
    public void Card_IsAGlassCard_WithTitleMessageAndCloseButton()
    {
        var (window, manager) = ShowManager(ThemeVariant.Light);
        try
        {
            manager.Show(new Notification("Saved", "The document was saved.", NotificationType.Success, TimeSpan.Zero));
            Overlays.Pump(window);
            var card = Cards(window).Single();
            Assert.Equal(NotificationTheme.Width, card.Bounds.Width);
            Assert.Equal(new Thickness(8), card.Margin);
            Assert.Equal(new Thickness(8), manager.Margin);
            var surface = card.GetVisualDescendants().OfType<GlassBorder>().Single(g => g.Name == OverlayParts.SurfacePart);
            Assert.Equal(new CornerRadius(16), surface.CornerRadius);
            Assert.Equal(new Thickness(16), surface.Padding);

            var texts = card.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsVisible).ToList();
            Assert.Equal(["Saved", "The document was saved."], texts.Select(t => t.Text));
            Assert.Equal(FontWeight.SemiBold, texts[0].FontWeight);

            var close = card.GetVisualDescendants().OfType<Button>().Single(b => b.Name == NotificationTheme.CloseButtonPart);
            Assert.Contains("subtle", close.Classes);
            Assert.Same(Icons.X, Assert.IsType<Icon>(close.Content).Data);
            Assert.Equal(32, close.Bounds.Width);
            Assert.Equal(32, close.Bounds.Height);
            Assert.True(NotificationCard.GetCloseOnClick(close));
            Assert.Equal("Close", AutomationProperties.GetName(close));
        }
        finally
        {
            window.Close();
        }
    }

    public static TheoryData<string, NotificationType> TypeData()
    {
        var data = new TheoryData<string, NotificationType>();
        foreach (var theme in new[] { "Light", "Dark" })
            foreach (var type in Enum.GetValues<NotificationType>())
                data.Add(theme, type);
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(TypeData))]
    public void Card_ShowsTheTypeIconInTheTypeColour(string theme, NotificationType type)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var (window, manager) = ShowManager(variant);
        try
        {
            manager.Show(new Notification("Title", "Message", type, TimeSpan.Zero));
            Overlays.Pump(window);
            var icon = Cards(window).Single().GetVisualDescendants().OfType<Icon>().Single(i => i.Name == NotificationTheme.TypeIconPart);
            var (geometry, key) = type switch
            {
                NotificationType.Information => (Icons.Info, "AccentTextColor"),
                NotificationType.Success => (Icons.CheckCircle, "SuccessTextColor"),
                NotificationType.Warning => (Icons.Warning, "WarningTextColor"),
                _ => (Icons.WarningCircle, "DangerTextColor"),
            };
            Assert.Same(geometry, icon.Data);
            Assert.Equal(Effective(window.FindResource(variant, key)), Effective(icon.Foreground));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void StatusColours_MeetAAOnThePageCardAndGlass(string theme)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var window = new Window { RequestedThemeVariant = variant };
        window.Show();
        try
        {
            var backgrounds = new[] { "SystemRegionColor", "SystemChromeMediumLowColor", "BackdropWashStartColor", "BackdropWashEndColor" }
                .Select(k => Effective(window.FindResource(variant, k))).ToList();
            var tint = Effective(window.FindResource(variant, "GlassTintColor"));
            backgrounds.Add(StandardsTheme.Mix(backgrounds[2], tint, GlassBorder.DefaultTintOpacity));
            backgrounds.Add(StandardsTheme.Mix(backgrounds[3], tint, GlassBorder.DefaultTintOpacity));
            foreach (var key in new[] { "SuccessTextColor", "WarningTextColor", "DangerTextColor" })
            {
                var colour = Effective(window.FindResource(variant, key));
                Assert.All(backgrounds, b => Assert.True(AccessibleColors.ContrastRatio(colour, b) >= 4.5, $"{key} {colour} on {b}"));
            }
            Assert.Equal(Color.Parse("#22A134"), Effective(window.FindResource(variant, "SuccessColor")));
            Assert.Equal(Color.Parse("#CE7900"), Effective(window.FindResource(variant, "WarningColor")));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Card_FadesOutThenCloses_InstantlyUnderReduceMotion(bool reduceMotion)
    {
        var (window, manager) = ShowManager(ThemeVariant.Light, reduceMotion);
        try
        {
            manager.Show(new Notification("Title", "Message", NotificationType.Information, TimeSpan.Zero));
            Overlays.Pump(window);
            var card = Cards(window).Single();
            var fade = card.FindResource(OverlayParts.FadeOutSlowKey);
            card.Close();
            Dispatcher.UIThread.RunJobs();
            if (reduceMotion)
            {
                Assert.Null(fade);
                Assert.True(card.IsClosed);
            }
            else
            {
                Assert.Equal(TimeSpan.FromMilliseconds(300), Assert.IsType<Animation>(fade).Duration);
                Assert.True(card.IsClosing);
            }
        }
        finally
        {
            window.Close();
        }
    }

    // --- Sheets ----------------------------------------------------------------------------

    [AvaloniaFact]
    public void ToolTip_RendersSheet()
    {
        using var sheet = new Sheet("ToolTip: a small solid card (radius 8, caption text, 1 px border), over busy content");
        foreach (var (label, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(label, ["Short", "Long (wraps at 320)", "Compact density"],
            [
                ("Open", [
                    ToolTipCell(sheet, theme, "Save (Ctrl+S)"),
                    ToolTipCell(sheet, theme, "Saves the document to its current location. Use Save As to choose another file or format."),
                    ToolTipCell(sheet, theme, "Save (Ctrl+S)", compact: true),
                ]),
            ]);
        sheet.Save("m3-tooltip.png");
    }

    [AvaloniaFact]
    public void Notification_RendersSheet()
    {
        using var sheet = new Sheet("Notification cards: glass (radius 16), type icon in the type colour, subtle close button");
        foreach (var (label, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(label, ["Normal", "Compact"],
            [
                ("Stack of four types", [NotificationCell(sheet, theme, compact: false), NotificationCell(sheet, theme, compact: true)]),
                ("Close button hovered", [NotificationCell(sheet, theme, compact: false, hoverClose: true), NotificationCell(sheet, theme, compact: true, hoverClose: true)]),
            ]);
        sheet.Save("m3-notification.png");
    }

    private static Control ToolTipCell(Sheet sheet, ThemeVariant theme, string text, bool compact = false)
    {
        var (window, target, _) = ShowToolTip(theme, text: text, width: 380, compact: compact);
        try
        {
            return Overlays.Capture(sheet, window);
        }
        finally
        {
            ToolTip.SetIsOpen(target, false);
            window.Close();
        }
    }

    private static Control NotificationCell(Sheet sheet, ThemeVariant theme, bool compact, bool hoverClose = false)
    {
        var (window, manager) = ShowManager(theme, height: hoverClose ? 130 : 440);
        try
        {
            if (compact)
                window.Styles.OfType<StandardsTheme>().Single().Density = Avalonia.Themes.Fluent.DensityStyle.Compact;
            if (hoverClose)
                manager.Show(new Notification("Update available", "Restart to finish installing.", NotificationType.Information, TimeSpan.Zero));
            else
            {
                manager.Show(new Notification("Update available", "Restart to finish installing.", NotificationType.Information, TimeSpan.Zero));
                manager.Show(new Notification("Saved", "The document was saved.", NotificationType.Success, TimeSpan.Zero));
                manager.Show(new Notification("Low disk space", "Less than 1 GB is free on the startup disk.", NotificationType.Warning, TimeSpan.Zero));
                manager.Show(new Notification("Sync failed", "Check your connection and try again.", NotificationType.Error, TimeSpan.Zero));
            }
            Overlays.Pump(window);
            if (hoverClose)
            {
                var close = Cards(window).Single().GetVisualDescendants().OfType<Button>().Single(b => b.Name == NotificationTheme.CloseButtonPart);
                ((IPseudoClasses)close.Classes).Set(":pointerover", true);
            }
            return Overlays.Capture(sheet, window);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Helpers ---------------------------------------------------------------------------

    private static (Window Window, Control Target, ToolTip Tip) ShowToolTip(ThemeVariant variant, bool reduceMotion = true,
        string text = "Save (Ctrl+S)", double width = 300, bool compact = false)
    {
        var target = new Button { Content = "Save", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(16) };
        ToolTip.SetTip(target, text);
        ToolTip.SetPlacement(target, PlacementMode.Bottom);
        var window = Overlays.Host(variant, target, width, 150, reduceMotion);
        if (compact)
            window.Styles.OfType<StandardsTheme>().Single().Density = Avalonia.Themes.Fluent.DensityStyle.Compact;
        ToolTip.SetIsOpen(target, true);
        Overlays.Pump(window);
        var tip = window.GetVisualDescendants().OfType<ToolTip>().Single();
        return (window, target, tip);
    }

    private static (Window Window, WindowNotificationManager Manager) ShowManager(ThemeVariant variant, bool reduceMotion = true, double height = 200)
    {
        var window = Overlays.Host(variant, new Panel(), 400, height, reduceMotion);
        var manager = new WindowNotificationManager(window) { Position = NotificationPosition.TopRight, MaxItems = 4 };
        Overlays.Pump(window);
        return (window, manager);
    }

    private static List<NotificationCard> Cards(Window window) => window.GetVisualDescendants().OfType<NotificationCard>().ToList();
}
