using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the ListBox, scroll shadow and ComboBox drop-down sheets (2x, labelled).</summary>
public class ListBoxRenderTests
{
    private enum ItemState { Rest, Hover, Pressed, Selected, SelectedHover, Disabled, Focused }

    private static readonly string[] ItemStateLabels = ["Rest", "Hover", "Pressed", "Selected", "Selected + hover", "Disabled", "Focused"];

    internal enum Surface { GlassPanel, Backdrop, Card }

    [AvaloniaFact]
    public void Selection_Renders()
    {
        using var sheet = new Sheet("ListBox item: accent selection (list background transparent, items inset 4 px)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, ItemStateLabels,
            [
                ("Glass panel", Enum.GetValues<ItemState>().Select(s => ItemCell(sheet, theme, s, Surface.GlassPanel)).ToList()),
                ("Window backdrop", Enum.GetValues<ItemState>().Select(s => ItemCell(sheet, theme, s, Surface.Backdrop)).ToList()),
            ]);
        sheet.Save("m3-listbox-selection.png");
    }

    [AvaloniaFact]
    public void BeforeAfter_RendersFluentNextToLibrary()
    {
        using var sheet = new Sheet("ListBox: Fluent default (before) and the library (after)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, ItemStateLabels,
            [
                ("Fluent", Enum.GetValues<ItemState>().Select(s => ItemCell(sheet, theme, s, Surface.Card, fluent: true)).ToList()),
                ("Library", Enum.GetValues<ItemState>().Select(s => ItemCell(sheet, theme, s, Surface.Card)).ToList()),
            ]);
        sheet.Save("m3-listbox-before-after.png");
    }

    [AvaloniaFact]
    public void ScrollShadows_Render()
    {
        string[] columns = ["Top (bottom shadow)", "Middle (both)", "End (top shadow)", "Horizontal, middle (left and right)"];

        using var sheet = new Sheet("Scroll shadows: 12 px, the accent darkened to lightness 0.35 at 8 % (light) / 0.12 at 18 % (dark)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, columns,
                new[] { Surface.GlassPanel, Surface.Card }.Select(surface => ((string)(surface == Surface.GlassPanel ? "Glass panel" : "Card"), (IReadOnlyList<Control>)
                [
                    ShadowCell(sheet, theme, surface, vertical: true, offset: 0),
                    ShadowCell(sheet, theme, surface, vertical: true, offset: 120),
                    ShadowCell(sheet, theme, surface, vertical: true, offset: 10_000),
                    ShadowCell(sheet, theme, surface, vertical: false, offset: 150),
                ])).ToList());
        sheet.Save("m3-scroll-shadows.png");
    }

    [AvaloniaFact]
    public void ComboBoxDropDown_Renders()
    {
        using var sheet = new Sheet("ComboBox drop-down open: hover on the first item, the third selected");
        string[] columns = ["Fluent", "Library"];
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, columns,
            [
                ("Drop-down", [
                    DropDownCell(sheet, theme, fluent: true),
                    DropDownCell(sheet, theme),
                ]),
            ]);
        sheet.Save("m3-combobox-dropdown.png");
    }

    // --- Cells ---------------------------------------------------------------------------

    private static Control ItemCell(Sheet sheet, ThemeVariant theme, ItemState state, Surface surface, bool fluent = false)
    {
        var list = new ListBox { Width = 200, ItemsSource = new[] { "Documents" } };
        if (!fluent && surface != Surface.Card)
            list.Background = Brushes.Transparent;
        return sheet.Cell(OnSurface(list, surface), theme,
            beforeShow: w =>
            {
                if (fluent)
                    w.Styles.Add(new FluentTheme());
            },
            afterShow: w =>
            {
                w.UpdateLayout();
                var item = (ListBoxItem)list.ContainerFromIndex(0)!;
                StripTransitions(item);
                if (state is ItemState.Selected or ItemState.SelectedHover)
                    item.IsSelected = true;
                if (state is ItemState.Hover or ItemState.SelectedHover)
                    ((IPseudoClasses)item.Classes).Set(":pointerover", true);
                if (state == ItemState.Pressed)
                    ((IPseudoClasses)item.Classes).Set(":pressed", true);
                if (state == ItemState.Disabled)
                    item.IsEnabled = false;
                if (state == ItemState.Focused)
                    item.Focus(NavigationMethod.Tab);
            });
    }

    private static Control ShadowCell(Sheet sheet, ThemeVariant theme, Surface surface, bool vertical, double offset)
    {
        Control content = vertical
            ? new StackPanel { Children = { } }
            : new UniformGrid { Rows = 3, Columns = 12 };
        if (content is StackPanel lines)
            foreach (var i in Enumerable.Range(1, 20))
                lines.Children.Add(new TextBlock { Text = $"Line {i}: list content", Margin = new Thickness(8, 4) });
        else
            foreach (var i in Enumerable.Range(1, 36))
                ((UniformGrid)content).Children.Add(new Border
                {
                    Width = 48, Height = 30, Margin = new Thickness(3), CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Color.FromRgb((byte)(40 + i * 5), (byte)(120 + i * 2), (byte)(200 - i * 3))),
                    Child = new TextBlock { Text = i.ToString(), Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                });

        var viewer = new ScrollViewer
        {
            Width = 200, Height = 110, Content = content,
            HorizontalScrollBarVisibility = vertical ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = vertical ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled,
        };
        return sheet.Cell(OnSurface(viewer, surface), theme,
            afterShow: w =>
            {
                w.UpdateLayout();
                StripTransitions(viewer);
                viewer.Offset = vertical ? new Vector(0, offset) : new Vector(offset, 0);
                w.UpdateLayout();
            });
    }

    private static Control DropDownCell(Sheet sheet, ThemeVariant theme, bool fluent = false)
    {
        var box = new ComboBox { Width = 220, ItemsSource = new[] { "Documents", "Downloads", "Pictures", "Music" }, SelectedIndex = 2 };
        var window = new Window { Width = 252, Height = 230, RequestedThemeVariant = theme };
        if (fluent)
            window.Styles.Add(new FluentTheme());
        window.Content = new Border { Padding = new Thickness(16), Child = box };
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        try
        {
            box.IsDropDownOpen = true;
            Pump(window);
            // Headless popups render in the window's overlay layer; the window is sized to hold the open drop-down.
            var root = TopLevel.GetTopLevel(box.GetVisualDescendants().OfType<Popup>().Single().Child)!;
            foreach (var item in box.GetLogicalDescendants().OfType<ComboBoxItem>())
                StripTransitions(item);
            ((IPseudoClasses)box.GetLogicalDescendants().OfType<ComboBoxItem>().First().Classes).Set(":pointerover", true);
            Pump(window);
            Pump(root);
            using var capture = root.CaptureRenderedFrame()!;
            return sheet.Image(capture);
        }
        finally
        {
            box.IsDropDownOpen = false;
            window.Close();
        }
    }

    internal static Control OnSurface(Control content, Surface surface)
    {
        switch (surface)
        {
            case Surface.GlassPanel:
            {
                var wash = new Border { Padding = new Thickness(12), Child = new GlassBorder { Padding = new Thickness(8), Child = content } };
                wash.Bind(Border.BackgroundProperty, new DynamicResourceExtension("BackdropWash"));
                return wash;
            }
            case Surface.Backdrop:
            {
                var wash = new Border { Padding = new Thickness(12), Child = content };
                wash.Bind(Border.BackgroundProperty, new DynamicResourceExtension("BackdropWash"));
                return wash;
            }
            default:
            {
                var card = new SquircleBorder { Padding = new Thickness(8), CornerRadius = new CornerRadius(8), Child = content };
                card.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("SystemChromeMediumLowColor"));
                return card;
            }
        }
    }

    private static void StripTransitions(Control root)
    {
        root.Transitions = null;
        foreach (var part in root.GetVisualDescendants().OfType<Animatable>())
            part.Transitions = null;
    }

    private static void Pump(TopLevel top)
    {
        Dispatcher.UIThread.RunJobs();
        top.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
