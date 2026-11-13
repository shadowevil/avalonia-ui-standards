using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;

using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>The Separator and GridSplitter themes, and their sheet.</summary>
public class SeparatorGridSplitterTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Separator_IsAHairlineInTheBorderColour(string theme)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var separator = new Separator();
        var window = Show(new StackPanel { Width = 200, Children = { separator } }, variant);
        try
        {
            Assert.Equal(1, separator.Bounds.Height);
            Assert.Equal(200, separator.Bounds.Width);
            Assert.Equal(new Thickness(0, 4), separator.Margin);
            Assert.False(separator.Focusable);
            var expected = Effective(window.FindResource(variant, "SystemBaseLowColor"));
            Assert.Equal(expected, Effective(separator.Background));
            var line = Assert.IsType<Border>(Assert.Single(separator.GetVisualChildren()));
            Assert.Equal(expected, Effective(line.Background));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Separator_InAMenu_IsInsetFromTheSides()
    {
        var separator = new Separator();
        var menu = new ContextMenu { ItemsSource = new object[] { new MenuItem { Header = "One" }, separator, new MenuItem { Header = "Two" } } };
        var target = new Border { Width = 100, Height = 40, Background = Brushes.Transparent, ContextMenu = menu };
        var window = Show(target, ThemeVariant.Light, width: 400, height: 300);
        try
        {
            menu.Open(target);
            Pump(window);
            Assert.Equal(SeparatorTheme.MenuMargin, separator.Margin);
            Assert.Equal(new Thickness(8, 4), separator.Margin);
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void GridSplitter_IsInvisibleAtRest_AndShowsAPillGripOnHover()
    {
        var (window, splitter) = ShowSplitter(columns: true, ThemeVariant.Light);
        try
        {
            var grip = Part(splitter, GridSplitterTheme.GripPart);
            Assert.Equal(0, grip.Opacity);
            Assert.Equal(new Size(GridSplitterTheme.GripThickness, GridSplitterTheme.GripLength), grip.Bounds.Size);
            Assert.Equal(new CornerRadius(2), grip.CornerRadius);
            Assert.Equal(8, splitter.Bounds.Width);
            Assert.False(splitter.ClipToBounds);

            // Upright between columns, centred.
            Assert.Equal((splitter.Bounds.Width - grip.Bounds.Width) / 2, grip.Bounds.X);
            Assert.Equal((splitter.Bounds.Height - grip.Bounds.Height) / 2, grip.Bounds.Y);

            ((IPseudoClasses)splitter.Classes).Set(":pointerover", true);
            window.UpdateLayout();
            Assert.Equal(1, grip.GetBaseValue(Visual.OpacityProperty).Value);
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, ScrollBarTheme.ThumbHoverKey)), Effective(grip.GetBaseValue(Border.BackgroundProperty).Value));

            ((IPseudoClasses)splitter.Classes).Set(":pressed", true);
            window.UpdateLayout();
            Assert.Equal(Effective(window.FindResource(ThemeVariant.Light, ScrollBarTheme.ThumbDragKey)), Effective(grip.GetBaseValue(Border.BackgroundProperty).Value));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void GridSplitter_BetweenRows_LaysTheGripLevel()
    {
        var (window, splitter) = ShowSplitter(columns: false, ThemeVariant.Light);
        try
        {
            var grip = Part(splitter, GridSplitterTheme.GripPart);
            Assert.Equal(new Size(GridSplitterTheme.GripLength, GridSplitterTheme.GripThickness), grip.Bounds.Size);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void GridSplitter_KeyboardFocus_ShowsTheGripAndTheRing()
    {
        var (window, splitter) = ShowSplitter(columns: true, ThemeVariant.Light);
        try
        {
            var ring = Part(splitter, GridSplitterTheme.RingPart);
            Assert.False(ring.IsVisible);
            Assert.True(splitter.Focus(NavigationMethod.Tab));
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Assert.True(ring.IsVisible);
            Assert.Equal(1, Part(splitter, GridSplitterTheme.GripPart).GetBaseValue(Visual.OpacityProperty).Value);
            // 2 px gap and 2 px ring outside the grip.
            Assert.Equal(new Size(12, 32), ring.Bounds.Size);
            Assert.Equal(new Thickness(2), ring.BorderThickness);
            Assert.Equal(1, splitter.ZIndex);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void GridSplitter_Grip_FadesUnlessMotionIsReduced(bool reduceMotion)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion };
        var (window, splitter) = ShowSplitter(columns: true, ThemeVariant.Light, theme);
        try
        {
            var transitions = Part(splitter, GridSplitterTheme.GripPart).Transitions;
            if (reduceMotion)
                Assert.Empty(transitions!);
            else
                Assert.Contains(transitions!, t => t is DoubleTransition { Property.Name: "Opacity" } d && d.Duration == TimeSpan.FromMilliseconds(100));
        }
        finally
        {
            window.Close();
        }
    }

    // --- Sheet -----------------------------------------------------------------------------

    private enum SplitterState { Rest, Hover, Dragging, Focused, Disabled }

    [AvaloniaFact]
    public void SeparatorAndGridSplitter_RenderSheet()
    {
        using var sheet = new Sheet("Separator (1 px, border colour) and GridSplitter (invisible at rest, pill grip on hover)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            sheet.AddSection($"{themeLabel}: Separator", ["Glass panel", "Window backdrop", "Card", "In a menu"],
            [
                ("Separator", [
                    sheet.Cell(ListBoxRenderTests.OnSurface(SeparatorSample(), ListBoxRenderTests.Surface.GlassPanel), theme),
                    sheet.Cell(ListBoxRenderTests.OnSurface(SeparatorSample(), ListBoxRenderTests.Surface.Backdrop), theme),
                    sheet.Cell(ListBoxRenderTests.OnSurface(SeparatorSample(), ListBoxRenderTests.Surface.Card), theme),
                    MenuSeparatorCell(sheet, theme),
                ]),
            ]);
            var states = Enum.GetValues<SplitterState>();
            sheet.AddSection($"{themeLabel}: GridSplitter", states.Select(s => s.ToString()).ToList(),
            [
                ("Between columns", states.Select(s => SplitterCell(sheet, theme, s, columns: true)).ToList()),
                ("Between rows", states.Select(s => SplitterCell(sheet, theme, s, columns: false)).ToList()),
            ]);
        }
        sheet.Save("m3-separator-gridsplitter.png");
    }

    private static Control SeparatorSample() => new StackPanel
    {
        Width = 180,
        Children = { new TextBlock { Text = "Above" }, new Separator(), new TextBlock { Text = "Below" } },
    };

    private static Control MenuSeparatorCell(Sheet sheet, ThemeVariant theme)
    {
        var target = new Border { Width = 1, Height = 1 };
        var menu = new ContextMenu { ItemsSource = new object[] { new MenuItem { Header = "Cut" }, new Separator(), new MenuItem { Header = "Paste" } } };
        target.ContextMenu = menu;
        var window = new Window { Width = 220, Height = 140, RequestedThemeVariant = theme };
        var wash = new Border { Child = target, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        wash.Bind(Border.BackgroundProperty, new DynamicResourceExtension("BackdropWash"));
        window.Content = wash;
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        try
        {
            menu.PlacementTarget = target;
            menu.Placement = PlacementMode.Pointer;
            menu.Open(target);
            Pump(window);
            foreach (var part in window.GetVisualDescendants().OfType<Animatable>())
                part.Transitions = null;
            Pump(window);
            using var capture = window.CaptureRenderedFrame()!;
            return sheet.Image(capture);
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    private static Control SplitterCell(Sheet sheet, ThemeVariant theme, SplitterState state, bool columns)
    {
        var (grid, splitter) = SplitGrid(columns);
        if (state == SplitterState.Disabled)
            splitter.IsEnabled = false;
        return sheet.Cell(grid, theme, afterShow: w =>
        {
            w.UpdateLayout();
            foreach (var part in splitter.GetVisualDescendants().OfType<Animatable>())
                part.Transitions = null;
            if (state is SplitterState.Hover or SplitterState.Dragging)
                ((IPseudoClasses)splitter.Classes).Set(":pointerover", true);
            if (state == SplitterState.Dragging)
                ((IPseudoClasses)splitter.Classes).Set(":pressed", true);
            if (state == SplitterState.Focused)
                splitter.Focus(NavigationMethod.Tab);
        });
    }

    // --- Helpers ---------------------------------------------------------------------------

    private static (Grid Grid, GridSplitter Splitter) SplitGrid(bool columns)
    {
        var splitter = new GridSplitter();
        var grid = new Grid { Width = columns ? 160 : 120, Height = columns ? 100 : 120 };
        Border Pane(string text)
        {
            var pane = new SquircleBorder { CornerRadius = new CornerRadius(8), Child = new TextBlock { Text = text, Margin = new Thickness(8) } };
            pane.Bind(SquircleBorder.BackgroundProperty, new DynamicResourceExtension("SystemChromeMediumLowColor"));
            return new Border { Child = pane };
        }
        var first = Pane("One");
        var second = Pane("Two");
        if (columns)
        {
            grid.ColumnDefinitions = new ColumnDefinitions("*,Auto,*");
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(second, 2);
        }
        else
        {
            grid.RowDefinitions = new RowDefinitions("*,Auto,*");
            Grid.SetRow(splitter, 1);
            Grid.SetRow(second, 2);
        }
        grid.Children.Add(first);
        grid.Children.Add(splitter);
        grid.Children.Add(second);
        return (grid, splitter);
    }

    private static (Window Window, GridSplitter Splitter) ShowSplitter(bool columns, ThemeVariant variant, StandardsTheme? theme = null)
    {
        var (grid, splitter) = SplitGrid(columns);
        var window = Show(grid, variant, theme: theme);
        return (window, splitter);
    }

    private static Window Show(Control content, ThemeVariant variant, double width = 300, double height = 300, StandardsTheme? theme = null)
    {
        var window = new Window { Width = width, Height = height, RequestedThemeVariant = variant };
        if (theme is not null)
            window.Styles.Add(theme);
        window.Content = content;
        window.Show();
        Pump(window);
        return window;
    }

    private static PillBorder Part(GridSplitter splitter, string name) =>
        splitter.GetVisualDescendants().OfType<PillBorder>().Single(p => p.Name == name);

    internal static void Pump(TopLevel top)
    {
        Dispatcher.UIThread.RunJobs();
        top.UpdateLayout();
        Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
