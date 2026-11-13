using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;
using Path = Avalonia.Controls.Shapes.Path;

namespace AvaloniaUiStandards.Tests;

/// <summary>Icon: the 256 design box at every size, the nonzero fill, the inherited colour, density, and the set sheet.</summary>
public class IconTests
{
    // caret-down's ink spans x 40..216 of the 256 box: its stroke ends are round caps of radius 8 around x 48 and 208.
    private const double CaretDownInkWidth = 176;

    [AvaloniaTheory]
    [InlineData(16)]
    [InlineData(20)]
    [InlineData(24)]
    public void Icon_KeepsTheDesignBox_AtEachSize(double size)
    {
        var icon = new Icon { Data = Icons.CaretDown, Width = size, Height = size };
        var window = Show(icon);
        try
        {
            Assert.Equal(new Size(size, size), icon.Bounds.Size);
            var path = icon.GetVisualDescendants().OfType<Path>().Single();
            Assert.Equal(Stretch.None, path.Stretch);
            Assert.Equal(new Size(256, 256), Assert.IsType<Canvas>(path.GetVisualParent()).Bounds.Size);

            // The ink keeps caret-down's proportion of the box, not stretched to its full width.
            using var frame = Frame.Capture(window);
            var (left, top, right, bottom) = Ink(frame, icon, window);
            var px = size * Sheet.Scale / 256;
            Assert.InRange((right - left) / (size * Sheet.Scale), (CaretDownInkWidth - 12) / 256, (CaretDownInkWidth + 12) / 256);
            Assert.InRange(left, 40 * px - 1.5, 40 * px + 1.5);
            // And its place in the box: the caret sits in the middle band, from y 88 to 184.
            Assert.InRange(top, 88 * px - 1.5, 88 * px + 1.5);
            Assert.InRange(bottom, 184 * px - 1.5, 184 * px + 1.5);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Fill_IsNonzero_AndKeepsTheCounters()
    {
        // Two nested squares drawn in the same direction: the nonzero rule fills the inner one, even-odd leaves it empty.
        var nested = Draw(Icons.Parse("M32,32H224V224H32ZM96,96H160V160H96Z"));
        Assert.True(nested(128, 128), "nonzero fills the inner square");
        Assert.True(nested(64, 64), "outer square");

        var house = Draw(Icons.House);
        Assert.True(house(40, 180), "left wall");
        Assert.True(house(108, 190), "door frame");
        Assert.True(house(128, 32), "roof ridge");
        Assert.False(house(76, 150), "inside the house");
        Assert.False(house(180, 130), "inside the house, right");
        Assert.False(house(128, 190), "the door opening");

        var gear = Draw(Icons.Gear);
        Assert.True(gear(128, 88), "hub ring");
        Assert.False(gear(128, 128), "hub centre");
    }

    // Renders the geometry at 128 px and 2x, where one device pixel is one design unit, and returns whether a design point is inked.
    private static Func<int, int, bool> Draw(Geometry data)
    {
        var icon = new Icon { Data = data, Width = 128, Height = 128 };
        var window = Show(icon);
        try
        {
            using var frame = Frame.Capture(window);
            var origin = icon.TranslatePoint(default, window)!.Value * Sheet.Scale;
            var inked = new bool[256, 256];
            for (var y = 0; y < 256; y++)
                for (var x = 0; x < 256; x++)
                    inked[x, y] = frame.Luma((int)origin.X + x, (int)origin.Y + y) < 128;
            return (x, y) => inked[x, y];
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Colour_FollowsTheInheritedForeground()
    {
        // A square filling the design box, so the centre pixel is the fill colour.
        var icon = new Icon { Data = Geometry.Parse("M0,0H256V256H0Z") };
        var host = new ContentControl { Foreground = new SolidColorBrush(Color.Parse("#C01070")), Content = icon };
        var window = Show(host);
        try
        {
            Assert.Equal(Color.Parse("#C01070"), Effective(icon.Foreground));
            using var frame = Frame.Capture(window);
            var centre = icon.TranslatePoint(new Point(icon.Bounds.Width / 2, icon.Bounds.Height / 2), window)!.Value * Sheet.Scale;
            Assert.Equal(((byte)0xC0, (byte)0x10, (byte)0x70), frame[(int)centre.X, (int)centre.Y]);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void InAButton_TakesItsTextColour_AndDimsWhenDisabled(string theme)
    {
        var variant = theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        var icon = new Icon { Data = Icons.Gear };
        var button = new Button { Content = icon };
        button.Classes.Add("accent");
        var window = Show(button, variant);
        try
        {
            Assert.Equal(Effective(Resolve(window, "AccentButtonForeground", variant)), Effective(icon.Foreground));

            button.IsEnabled = false;
            window.UpdateLayout();
            var disabled = Effective(Resolve(window, "AccentButtonForegroundDisabled", variant));
            Assert.Equal(disabled, Effective(icon.Foreground));
            Assert.NotEqual(Effective(Resolve(window, "AccentButtonForeground", variant)), disabled);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Size_FollowsDensity()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var icon = new Icon { Data = Icons.Gear };
        var window = Show(icon, theme: theme);
        try
        {
            Assert.Equal(new Size(20, 20), icon.Bounds.Size);
            theme.Density = DensityStyle.Compact;
            window.UpdateLayout();
            Assert.Equal(new Size(16, 16), icon.Bounds.Size);
            theme.Density = DensityStyle.Normal;
            window.UpdateLayout();
            Assert.Equal(new Size(20, 20), icon.Bounds.Size);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Set_RendersAt20InLightAndDark()
    {
        using var sheet = new Sheet("Icon: the Phosphor Regular set at 20 px, in the text colour");
        foreach (var (label, variant) in ButtonRenderTests.Themes)
            sheet.AddSection(label, [""], [("", [sheet.Cell(SetGrid(), variant)])]);
        sheet.Save("m3-icon-set.png");
    }

    /// <summary>Every icon in <see cref="Icons"/>, by property name.</summary>
    private static IReadOnlyList<(string Name, Geometry Data)> All() =>
        typeof(Icons).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(Geometry))
            .Select(p => (p.Name, (Geometry)p.GetValue(null)!))
            .ToList();

    private static Control SetGrid()
    {
        const int columns = 6;
        var icons = All();
        var grid = new Grid { ColumnSpacing = 16, RowSpacing = 10 };
        for (var c = 0; c < columns; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        for (var r = 0; r < (icons.Count + columns - 1) / columns; r++)
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var i = 0; i < icons.Count; i++)
        {
            var name = new TextBlock { Text = icons[i].Name, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            var cell = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new Icon { Data = icons[i].Data }, name } };
            Grid.SetRow(cell, i / columns);
            Grid.SetColumn(cell, i % columns);
            grid.Children.Add(cell);
        }
        grid.Bind(TemplatedControl.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseHighBrush"));
        return grid;
    }

    // The control on white with black text, at 2x, in its own window with the library theme.
    private static Window Show(Control content, ThemeVariant? variant = null, StandardsTheme? theme = null)
    {
        var window = new Window
        {
            SizeToContent = SizeToContent.WidthAndHeight,
            RequestedThemeVariant = variant ?? ThemeVariant.Light,
            Background = Brushes.White,
            Foreground = Brushes.Black,
        };
        if (theme is not null)
            window.Styles.Add(theme);
        window.Content = new Border { Padding = new Thickness(8), Child = content, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    // The ink's bounds within the icon, in device pixels from its top left (half-inked pixels count).
    private static (int Left, int Top, int Right, int Bottom) Ink(Frame frame, Control icon, Window window)
    {
        var origin = icon.TranslatePoint(default, window)!.Value * Sheet.Scale;
        var size = (int)Math.Round(icon.Bounds.Width * Sheet.Scale);
        int left = size, top = size, right = 0, bottom = 0;
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                if (frame.Luma((int)origin.X + x, (int)origin.Y + y) < 128)
                {
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x + 1);
                    bottom = Math.Max(bottom, y + 1);
                }
        return (left, top, right, bottom);
    }

    private static object? Resolve(Window window, string key, ThemeVariant variant) =>
        window.TryFindResource(key, variant, out var value) ? value : throw new InvalidOperationException(key);
}
