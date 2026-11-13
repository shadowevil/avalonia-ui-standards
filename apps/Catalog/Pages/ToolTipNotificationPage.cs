using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;

namespace Catalog.Pages;

/// <summary>ToolTip and notification cards: the tooltip card, a card of each type, and live tooltips and notifications.</summary>
internal sealed class ToolTipNotificationPage : UserControl
{
    private static readonly (NotificationType Type, string Title, string Message)[] Samples =
    [
        (NotificationType.Information, "Update available", "Restart to finish installing."),
        (NotificationType.Success, "Saved", "The document was saved."),
        (NotificationType.Warning, "Low disk space", "Less than 1 GB is free on the startup disk."),
        (NotificationType.Error, "Sync failed", "Check your connection and try again."),
    ];

    private WindowNotificationManager? _manager;
    private int _next;

    public ToolTipNotificationPage() => Content = Ui.Page("ToolTip and notifications",
        Ui.Section("ToolTip", new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new WrapPanel { Children = { Ui.Spaced(OpenTip("Save (Ctrl+S)")), Ui.Spaced(OpenTip("Saves the document to its current location.")) } },
                new WrapPanel { Children = { Ui.Spaced(WithTip("Hover for a tooltip", "Save (Ctrl+S)")), Ui.Spaced(WithTip("Disabled, with a tooltip", "Not available while offline", enabled: false)) } },
                Ui.Caption("A solid card in the card colour with a 1 px border, radius 8 and caption text. It appears after 400 ms (100 ms when moving between controls) and fades in over MotionFast."),
            },
        }),
        Ui.Section("Notification cards", new StackPanel
        {
            Spacing = 12,
            Children =
            {
                Ui.SideScroll(Cards()),
                ShowButton(),
                Ui.Caption("Glass cards (radius 16) with an icon in the type's colour and a subtle close button. Shown at the window's top right; they fade in and out over MotionSlow."),
            },
        }));

    private static Control Cards()
    {
        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var (type, title, message) in Samples)
            panel.Children.Add(new NotificationCard { Content = new Notification(title, message, type), NotificationType = type, IsHitTestVisible = false });
        return panel;
    }

    private Control ShowButton()
    {
        var button = new Button { Content = "Show a notification" };
        button.Click += (_, _) =>
        {
            _manager ??= new WindowNotificationManager(TopLevel.GetTopLevel(this)) { Position = NotificationPosition.TopRight, MaxItems = 4 };
            var (type, title, message) = Samples[_next++ % Samples.Length];
            _manager.Show(new Notification(title, message, type));
        };
        return button;
    }

    // A tooltip shown in place, as it looks when open.
    private static Control OpenTip(string text)
    {
        var tip = new ToolTip { Content = text, IsHitTestVisible = false };
        tip.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() => ((IPseudoClasses)tip.Classes).Set(":open", true));
        return tip;
    }

    private static Control WithTip(string label, string tip, bool enabled = true)
    {
        var button = new Button { Content = label, IsEnabled = enabled };
        ToolTip.SetTip(button, tip);
        ToolTip.SetShowOnDisabled(button, true);
        return button;
    }
}
