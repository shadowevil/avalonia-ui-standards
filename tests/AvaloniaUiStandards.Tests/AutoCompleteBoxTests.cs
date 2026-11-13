using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;
using static AvaloniaUiStandards.Tests.FieldHost;

namespace AvaloniaUiStandards.Tests;

/// <summary>AutoCompleteBox: a TextInput field with the ComboBox drop-down for its suggestions; theme tests and sheets.</summary>
public class AutoCompleteBoxTests
{
    private static readonly string[] Cities = ["Amsterdam", "Athens", "Auckland", "Austin", "Berlin", "Boston"];

    private static AutoCompleteBox New() => new() { Width = 220, ItemsSource = Cities, PlaceholderText = "City", MinimumPrefixLength = 1 };

    private static TextBox Text(AutoCompleteBox box) => Part<TextBox>(box, "PART_TextBox");

    [AvaloniaTheory]
    [InlineData("Normal", 32, 12, 4)]
    [InlineData("Compact", 24, 8, 2)]
    public void Field_IsATextInput(string density, double height, double side, double vertical)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal };
        var box = New();
        var window = Show(new StackPanel { Children = { box } }, theme: theme);
        try
        {
            Assert.Equal(height, box.Bounds.Height);
            Assert.Equal(new Thickness(side, vertical), Text(box).Padding);
            Assert.Equal(new CornerRadius(8), Chrome(Text(box)).CornerRadius);
            Assert.Equal(Colour(box, "TextControlPlaceholderForeground"), Effective(Text(box).PlaceholderForeground));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Focus_ShowsTextInputsAccentBorder_AndTheRingOnKeyboardFocus()
    {
        var box = New();
        var other = new Button { Content = "Other" };
        var window = Show(new StackPanel { Children = { box, other } });
        try
        {
            var text = Text(box);
            text.Focus(NavigationMethod.Pointer);
            Pump(window);
            Assert.Equal(Colour(box, "TextControlBorderBrushFocused"), Effective(text.BorderBrush));
            Assert.False(Ring(text).IsVisible);
            other.Focus();
            text.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.True(Ring(text).IsVisible);
            // Nothing between the ring and the window clips it.
            Assert.DoesNotContain(text.GetVisualAncestors().TakeWhile(a => a != window), a => a.ClipToBounds);
        }
        finally
        {
            window.Close();
        }
    }

    // Validation errors set on the AutoCompleteBox reach its TextBox, which shows the error border.
    [AvaloniaFact]
    public void ValidationErrors_ReachTheTextBoxsErrorLook()
    {
        var box = New();
        var window = Show(new StackPanel { Children = { box } });
        try
        {
            DataValidationErrors.SetErrors(box, ["Unknown city"]);
            Pump(window);
            var text = Text(box);
            Assert.Contains(":error", text.Classes);
            Assert.Equal(Colour(box, "DangerColor"), Effective(text.BorderBrush));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Suggestions_UseTheComboBoxDropDown(string theme)
    {
        var box = New();
        var window = Show(new StackPanel { Children = { box } }, Variant(theme));
        try
        {
            Text(box).Focus();
            Text(box).Text = "A";
            Pump(window);
            Assert.True(box.IsDropDownOpen);
            var popup = Part<Popup>(box, "PART_Popup");
            var chrome = Assert.IsType<SquircleBorder>(popup.Child);
            Assert.Equal(AutoCompleteBoxTheme.SuggestionsPart, chrome.Name);
            Assert.Equal(new CornerRadius(16), chrome.CornerRadius);
            Assert.Equal(Colour(box, "ComboBoxDropDownBackground"), Effective(chrome.Background));
            var list = Assert.IsType<ListBox>(chrome.Child);
            Assert.Equal(new Thickness(4), list.Padding);
            Assert.Equal(default, list.Margin);
            // The library list items: squircles inset 4 inside the drop-down's border, as in a ComboBox.
            var item = (ListBoxItem)list.ContainerFromIndex(0)!;
            Assert.Equal(new CornerRadius(8), item.CornerRadius);
            var inset = chrome.BorderThickness.Left + 4;
            Assert.Equal(new Point(inset, inset), item.TranslatePoint(default, chrome)!.Value);
            Assert.Equal(4, list.ItemCount);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Sheets ---------------------------------------------------------------------------

    private static readonly FieldState[] States =
        [FieldState.Rest, FieldState.Hover, FieldState.Focused, FieldState.KeyboardFocused, FieldState.Error, FieldState.Disabled];

    private static InputElement FocusTarget(Control control) => Text((AutoCompleteBox)control);

    [AvaloniaFact]
    public void States_RenderSheet()
    {
        using var sheet = new Sheet("AutoCompleteBox: the TextInput field (states are the TextBox's), and its suggestions in the ComboBox drop-down");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            foreach (var (densityLabel, density) in FieldCells.Densities)
                sheet.AddSection($"{themeLabel}, {densityLabel}", States.Select(FieldCells.Label).ToList(),
                [
                    ("Placeholder", States.Select(s => Cell(sheet, theme, density, s, null)).ToList()),
                    ("Text", States.Select(s => Cell(sheet, theme, density, s, "Austin")).ToList()),
                ]);
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection($"{themeLabel}, suggestions open", ["Typed \"A\", second suggestion hovered"],
            [
                ("Drop-down", [Open(sheet, theme)]),
            ]);
        sheet.Save("m3-autocompletebox.png");
    }

    // The TextBox holds the states: hover, error and disabled are set on it, focus goes to it.
    private static Control Cell(Sheet sheet, ThemeVariant theme, DensityStyle density, FieldState state, string? text)
    {
        var box = New();
        box.Text = text;
        return FieldCells.Render(sheet, theme, density, box, FieldState.Rest, afterShow: _ =>
        {
            var inner = Text(box);
            if (state == FieldState.Hover)
                ((IPseudoClasses)inner.Classes).Set(":pointerover", true);
            if (state == FieldState.Error)
                ((IPseudoClasses)inner.Classes).Set(":error", true);
            if (state == FieldState.Disabled)
                box.IsEnabled = false;
            if (state is FieldState.Focused or FieldState.KeyboardFocused)
                inner.Focus(state == FieldState.Focused ? NavigationMethod.Pointer : NavigationMethod.Tab);
        });
    }

    private static Control Open(Sheet sheet, ThemeVariant theme)
    {
        var box = New();
        var window = new Window { Width = 252, Height = 230, RequestedThemeVariant = theme };
        window.Content = new Border { Padding = new Thickness(16), Child = box };
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        try
        {
            Pump(window);
            Text(box).Focus();
            Text(box).Text = "A";
            Pump(window);
            var popup = Part<Popup>(box, "PART_Popup");
            var root = TopLevel.GetTopLevel(popup.Child)!;
            var list = (ListBox)((SquircleBorder)popup.Child!).Child!;
            var items = Enumerable.Range(0, list.ItemCount).Select(i => (ListBoxItem)list.ContainerFromIndex(i)!).ToList();
            foreach (var part in items.SelectMany(i => i.GetVisualDescendants().OfType<Avalonia.Animation.Animatable>()))
                part.Transitions = null;
            ((IPseudoClasses)items[1].Classes).Set(":pointerover", true);
            Pump(window);
            Pump(root);
            using var capture = root.CaptureRenderedFrame()!;
            return sheet.Image(capture);
        }
        finally
        {
            window.Close();
        }
    }
}
