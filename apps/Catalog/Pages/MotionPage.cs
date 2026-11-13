using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Catalog.Pages;

/// <summary>The motion durations: a marker slides along a track using the theme's duration and easing.</summary>
internal sealed class MotionPage : UserControl
{
    private const double Track = 320;
    private const double Dot = 24;

    private static readonly string[] Steps = ["Fast", "Normal", "Slow"];

    public MotionPage()
    {
        var list = new StackPanel { Spacing = 16 };
        foreach (var step in Steps)
            list.Children.Add(Row("Motion" + step));
        Content = Ui.Page("Motion", Ui.Section("Durations", list));
    }

    private static Control Row(string key)
    {
        var dot = new Border { Width = Dot, Height = Dot, CornerRadius = new CornerRadius(Dot / 2), HorizontalAlignment = HorizontalAlignment.Left };
        dot.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemControlHighlightAccentBrush"));
        var track = new Border { Width = Track, Height = Dot, Child = dot };

        var atEnd = false;
        var play = new Button { Content = "Play" };
        play.Click += (_, _) =>
        {
            // Duration and easing are read at the moment of the click, so toolbar changes apply.
            var duration = play.FindResource(key) is TimeSpan d ? d : TimeSpan.Zero;
            var easing = play.FindResource("MotionEasing") as Easing ?? new LinearEasing();
            dot.Transitions = [new ThicknessTransition { Property = Layoutable.MarginProperty, Duration = duration, Easing = easing }];
            atEnd = !atEnd;
            dot.Margin = new Thickness(atEnd ? Track - Dot : 0, 0, 0, 0);
        };

        var label = Ui.Body(key);
        label.Width = 140;
        var value = Ui.CaptionOf(key, v => v is TimeSpan t ? $"{t.TotalMilliseconds:0} ms" : "");
        value.Width = 60;
        foreach (var part in new Control[] { label, value, track, play })
            part.Margin = new Thickness(0, 0, 12, 4);
        return new WrapPanel { Children = { label, value, track, play } };
    }
}
