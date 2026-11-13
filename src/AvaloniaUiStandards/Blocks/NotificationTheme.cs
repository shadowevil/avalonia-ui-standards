using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Style;
using static AvaloniaUiStandards.Blocks.ButtonParts;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The notification themes: WindowNotificationManager (Fluent's, inset <c>SpacingS</c>) and
/// NotificationCard, a glass card with the overlay radius, <c>SpacingL</c> padding, an icon per
/// type in the type's text colour, the title in body strong over the message, and a subtle
/// square close button with Icons.X. Cards fade in and out over <c>MotionSlow</c>.
/// </summary>
internal static class NotificationTheme
{
    public const string TypeIconPart = "PART_TypeIcon";
    public const string CloseButtonPart = "PART_CloseButton";

    /// <summary>The card's width.</summary>
    public const double Width = 360;

    /// <summary>The card's outer margin and the manager's inset: <c>SpacingS</c>, so cards sit 16 px from the window edge and from each other.</summary>
    public static readonly Thickness Margin = new(Spacing.S);

    /// <summary>The card's padding: <c>SpacingL</c>.</summary>
    public static readonly Thickness Padding = new(Spacing.L);

    /// <summary>The gap between the icon, the text and the close button: <c>SpacingM</c>.</summary>
    public const double Gap = Spacing.M;

    // The close button's margin, set by BlocksThemes.ApplyDensity under this non-public key.
    internal static readonly object CloseMarginKey = new();

    /// <summary>
    /// The close button's margin for a button and icon size: pulled up into the card's padding by
    /// half their difference, so the button's centre lines up with the type icon's, and right into
    /// the padding by <c>SpacingS</c>, with <c>SpacingS</c> after the text.
    /// </summary>
    public static Thickness CloseMargin(double buttonSize, double iconSize) =>
        new(Gap - Spacing.XS, -(buttonSize - iconSize) / 2, -Spacing.S, 0);

    /// <summary>The icon and colour key per type, by the card's pseudo-class.</summary>
    public static readonly (string PseudoClass, Func<Geometry> Icon, string ColourKey)[] Types =
    [
        (":information", () => Icons.Info, "AccentTextColor"),
        (":success", () => Icons.CheckCircle, "SuccessTextColor"),
        (":warning", () => Icons.Warning, "WarningTextColor"),
        (":error", () => Icons.WarningCircle, "DangerTextColor"),
    ];

    static NotificationTheme() =>
        NotificationCard.IsClosingProperty.Changed.AddClassHandler<NotificationCard>((card, _) => OnClosing(card));

    public static ControlTheme Manager(ControlTheme? fluentTheme) => new(typeof(WindowNotificationManager))
    {
        BasedOn = fluentTheme,
        Setters = { new Setter(Layoutable.MarginProperty, Margin) },
    };

    public static ControlTheme Card()
    {
        var theme = new ControlTheme(typeof(NotificationCard))
        {
            Setters =
            {
                new Setter(Layoutable.WidthProperty, Width),
                new Setter(Layoutable.MarginProperty, Margin),
                new Setter(TemplatedControl.PaddingProperty, Padding),
                new Setter(TemplatedControl.CornerRadiusProperty, Resource("CornerRadiusOverlay")),
                new Setter(TemplatedControl.ForegroundProperty, Resource("SystemControlForegroundBaseHighBrush")),
                new Setter(ContentControl.ContentTemplateProperty, new FuncDataTemplate<INotification>((notification, _) => Text(notification), supportsRecycling: false)),
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<NotificationCard>(Build)),
            },
        };
        foreach (var (pseudoClass, icon, colourKey) in Types)
            theme.Children.Add(new Avalonia.Styling.Style(x => x.Nesting().Class(pseudoClass).Template().OfType<Icon>().Name(TypeIconPart))
            {
                Setters =
                {
                    new Setter(PathIcon.DataProperty, icon()),
                    new Setter(TemplatedControl.ForegroundProperty, Resource(colourKey)),
                },
            });
        return theme;
    }

    // The title in body strong over the message, both wrapping.
    private static Control Text(INotification? notification)
    {
        var title = new TextBlock { Text = notification?.Title, TextWrapping = TextWrapping.Wrap, IsVisible = !string.IsNullOrEmpty(notification?.Title) };
        title.Bind(TextBlock.FontWeightProperty, new DynamicResourceExtension("FontWeightBodyStrong"));
        var message = new TextBlock { Text = notification?.Message, TextWrapping = TextWrapping.Wrap, IsVisible = !string.IsNullOrEmpty(notification?.Message) };
        return new StackPanel { Spacing = Spacing.XS, Children = { title, message } };
    }

    private static Control Build(NotificationCard card, INameScope scope)
    {
        var icon = new Icon { Name = TypeIconPart, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 0, Gap, 0) };

        var content = new ContentPresenter { Name = ContentPart, VerticalAlignment = VerticalAlignment.Center };
        content.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
        content.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
        Grid.SetColumn(content, 1);

        var close = new Button
        {
            Name = CloseButtonPart,
            Content = new Icon { Data = Icons.X },
            Padding = default,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
        };
        close.Classes.Add("subtle");
        close.Bind(Layoutable.MarginProperty, new DynamicResourceExtension(CloseMarginKey));
        close.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(MinHeightKey));
        close.Bind(Layoutable.HeightProperty, new DynamicResourceExtension(MinHeightKey));
        NotificationCard.SetCloseOnClick(close, true);
        AutomationProperties.SetName(close, "Close");
        Grid.SetColumn(close, 2);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Children = { icon, content, close },
        };

        var surface = new GlassBorder { Name = OverlayParts.SurfacePart, Child = grid }.FadesIn(OverlayParts.FadeInSlowKey);
        surface.Bind(Decorator.PaddingProperty, new TemplateBinding(TemplatedControl.PaddingProperty));
        surface.Bind(GlassBorder.CornerRadiusProperty, new TemplateBinding(TemplatedControl.CornerRadiusProperty));

        foreach (var part in new StyledElement[] { surface, icon, content, close })
            part.RegisterInNameScope(scope);
        return surface;
    }

    // A card with this theme fades out when it starts closing, then marks itself closed, which
    // removes it. Under reduce motion it is closed at once. Cards with another theme are left to it.
    private static async void OnClosing(NotificationCard card)
    {
        if (!card.IsClosing || card.IsClosed || !HasThisTheme(card))
            return;
        if (card.TryFindResource(OverlayParts.FadeOutSlowKey, out var value) && value is Animation fade)
            await fade.RunAsync(card);
        card.IsClosed = true;
    }

    private static bool HasThisTheme(NotificationCard card) =>
        card.GetVisualChildren().FirstOrDefault() is GlassBorder { Name: OverlayParts.SurfacePart } surface && surface.TemplatedParent == card;
}
