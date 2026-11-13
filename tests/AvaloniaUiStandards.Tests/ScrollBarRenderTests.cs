using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.ListBoxRenderTests;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the ScrollBar sheets (2x, labelled): its states, and Fluent's bar beside it.</summary>
public class ScrollBarRenderTests
{
    private enum BarState { Idle, Scrolling, Near, Hovered, Dragging }

    private static readonly string[] StateLabels = ["Idle", "Scrolling (thin)", "Pointer 12 px away", "Hovered (full)", "Dragging"];

    [AvaloniaFact]
    public void States_Render()
    {
        using var sheet = new Sheet("ScrollBar: overlay pill, thin 3 px while scrolling, widening to 8 px within 24 px of the pointer");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            foreach (var vertical in new[] { true, false })
                sheet.AddSection($"{themeLabel}, {(vertical ? "vertical" : "horizontal")}", StateLabels,
                    new[] { Surface.GlassPanel, Surface.Card }.Select(surface => (surface == Surface.GlassPanel ? "Glass panel" : "Card", (IReadOnlyList<Control>)
                        Enum.GetValues<BarState>().Select(state => Cell(sheet, theme, surface, vertical, state)).ToList())).ToList());
        sheet.Save("m3-scrollbar-states.png");
    }

    [AvaloniaFact]
    public void BeforeAfter_RendersFluentNextToLibrary()
    {
        using var sheet = new Sheet("ScrollBar: Fluent (before) and the library (after), on the glass panel");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, ["At rest", "Pointer on the bar"],
            [
                ("Fluent", [Cell(sheet, theme, Surface.GlassPanel, true, BarState.Idle, fluent: true), Cell(sheet, theme, Surface.GlassPanel, true, BarState.Hovered, fluent: true)]),
                ("Library", [Cell(sheet, theme, Surface.GlassPanel, true, BarState.Idle), Cell(sheet, theme, Surface.GlassPanel, true, BarState.Hovered)]),
            ]);
        sheet.Save("m3-scrollbar-before-after.png");
    }

    [AvaloniaFact]
    public void Insets_Render()
    {
        using var sheet = new Sheet("ScrollBar inset from the glass panel's edge (scroller filling the panel, 16 px corner); ends clear the corner by 8 px");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, ["8 px, scrolling", "8 px, hovered"],
            [
                ("Glass panel",
                [
                    Cell(sheet, theme, Surface.GlassPanel, true, BarState.Scrolling, fullPanel: true),
                    Cell(sheet, theme, Surface.GlassPanel, true, BarState.Hovered, fullPanel: true),
                ]),
            ]);
        sheet.Save("m3-scrollbar-inset.png");
    }

    // The scroller filling a glass panel edge to edge, as in the window's content panel.
    private static Control FullPanel(Control content)
    {
        var wash = new Border { Padding = new Thickness(12), Child = new GlassBorder { Child = content } };
        wash.Bind(Border.BackgroundProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("BackdropWash"));
        return wash;
    }

    // A list scrolled to the middle, with the bar in the given state. Motion is reduced so each
    // state shows its end value.
    private static Control Cell(Sheet sheet, ThemeVariant theme, Surface surface, bool vertical, BarState state, bool fluent = false, bool fullPanel = false)
    {
        var lines = new StackPanel();
        foreach (var i in Enumerable.Range(1, 20))
            lines.Children.Add(new TextBlock { Text = vertical ? $"Line {i}: list content" : $"Line {i}: a long line of list content that runs on", Margin = new Thickness(8, 4) });
        var viewer = new ScrollViewer
        {
            Width = 200, Height = 110, Content = lines,
            HorizontalScrollBarVisibility = vertical ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = vertical ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
        };
        var middle = vertical ? new Vector(0, 120) : new Vector(80, 0);
        return sheet.Cell(fullPanel ? FullPanel(viewer) : OnSurface(viewer, surface), theme,
            beforeShow: w => w.Styles.Add(fluent ? new FluentTheme() : new StandardsTheme(new OsAccessibility.Settings(true, false))),
            afterShow: w =>
            {
                w.UpdateLayout();
                var bar = viewer.GetVisualDescendants().OfType<ScrollBar>().Single(b => b.IsVisible);
                if (state != BarState.Scrolling)
                    viewer.Offset = middle;
                Dispatcher.UIThread.RunJobs();
                w.UpdateLayout();
                if (state == BarState.Scrolling)
                    viewer.Offset = middle;
                if (fluent && state == BarState.Hovered)
                {
                    // Fluent expands its bar after a hover delay; auto-hide off shows that look at once.
                    viewer.AllowAutoHide = false;
                }

                Point OnBar(double across, double along) => bar.TranslatePoint(vertical ? new Point(across, along) : new Point(along, across), w)!.Value;
                var size = vertical ? bar.Bounds.Height : bar.Bounds.Width;
                switch (state)
                {
                    case BarState.Near:
                        w.MouseMove(OnBar(-12, size * 0.6));
                        break;
                    case BarState.Hovered when !fluent:
                        w.MouseMove(OnBar((vertical ? bar.Bounds.Width : bar.Bounds.Height) / 2, size * 0.9));
                        break;
                    case BarState.Dragging:
                        var thumb = bar.GetVisualDescendants().OfType<Thumb>().Single();
                        var point = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), w)!.Value;
                        w.MouseMove(point);
                        w.MouseDown(point, MouseButton.Left);
                        break;
                }
                Dispatcher.UIThread.RunJobs();
                w.UpdateLayout();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
            });
    }
}
