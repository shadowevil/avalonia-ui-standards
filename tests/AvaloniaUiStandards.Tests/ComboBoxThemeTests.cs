using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;
using static AvaloniaUiStandards.Tests.FieldHost;

namespace AvaloniaUiStandards.Tests;

/// <summary>ComboBox field: TextInput chrome and sizes, caret zone, colours, editable text field, focus ring, motion.</summary>
public class ComboBoxThemeTests
{
    private static ComboBox New(bool editable = false) => new()
    {
        Width = 200, ItemsSource = ComboBoxRenderTests.Items, IsEditable = editable,
        SelectedIndex = editable ? -1 : 0, PlaceholderText = "Choose",
    };

    [AvaloniaTheory]
    [InlineData("Normal", 32, 12, 4)]
    [InlineData("Compact", 24, 8, 2)]
    public void Field_MatchesTextInput_InHeightRadiusAndPadding(string density, double height, double side, double vertical)
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced) { Density = density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal };
        var box = New();
        var text = new TextBox { Width = 200 };
        var window = Show(new StackPanel { Children = { box, text } }, theme: theme);
        try
        {
            Assert.Equal(height, box.Bounds.Height);
            Assert.Equal(text.Bounds.Height, box.Bounds.Height);
            Assert.Equal(new CornerRadius(8), Chrome(box).CornerRadius);
            Assert.Equal(new Thickness(1), Chrome(box).BorderThickness);
            Assert.Equal(new Thickness(side, vertical, 0, vertical), box.Padding);

            // The caret zone is square to the field and holds the 16 px caret-down icon, centred.
            var zone = Part<Border>(box, ComboBoxTheme.GlyphZonePart);
            Assert.Equal(height, zone.Bounds.Width);
            Assert.Equal(box.Bounds.Width, zone.TranslatePoint(new Point(zone.Bounds.Width, 0), box)!.Value.X);
            var glyph = Part<Icon>(box, ComboBoxTheme.GlyphPart);
            Assert.Equal(Icons.CaretDown, glyph.Data);
            Assert.Equal(new Size(16, 16), glyph.Bounds.Size);
            var centre = glyph.TranslatePoint(new Point(8, 8), zone)!.Value;
            Assert.Equal(new Point(height / 2, height / 2), centre);
        }
        finally
        {
            window.Close();
        }
    }

    public static TheoryData<string, bool, string, string, string> States()
    {
        var data = new TheoryData<string, bool, string, string, string>();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            foreach (var editable in new[] { false, true })
            {
                data.Add(theme, editable, "rest", "ComboBoxBackground", "ComboBoxBorderBrush");
                data.Add(theme, editable, ":pointerover", "ComboBoxBackgroundPointerOver", "ComboBoxBorderBrushPointerOver");
                // No pressed look of its own: the list opening is the feedback.
                data.Add(theme, editable, ":pointerover:pressed", "ComboBoxBackgroundPointerOver", "ComboBoxBorderBrushPointerOver");
                data.Add(theme, editable, ":error", "ComboBoxBackground", "DangerColor");
                data.Add(theme, editable, ":disabled", "ComboBoxBackgroundDisabled", "ComboBoxBorderBrushDisabled");
            }
            // Focus within marks only an editable ComboBox, as a text field.
            data.Add(theme, false, ":focus-within", "ComboBoxBackground", "ComboBoxBorderBrush");
            data.Add(theme, true, ":focus-within", "TextControlBackgroundFocused", "TextControlBorderBrushFocused");
        }
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(States))]
    public void States_Resolve(string theme, bool editable, string state, string backgroundKey, string borderKey)
    {
        var box = New(editable);
        var window = Show(box, Variant(theme));
        try
        {
            foreach (var pseudo in state.Split(':', StringSplitOptions.RemoveEmptyEntries))
                if (pseudo != "rest")
                    ((IPseudoClasses)box.Classes).Set(":" + pseudo, true);
            Pump(window);
            Assert.Equal(Colour(box, backgroundKey), Effective(box.Background));
            Assert.Equal(Colour(box, borderKey), Effective(box.BorderBrush));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void PlaceholderAndText_MeetAaContrast(string theme)
    {
        foreach (var state in new[] { "rest", ":pointerover", ":pressed" })
        {
            var box = New();
            box.SelectedIndex = -1;
            var window = Show(box, Variant(theme));
            try
            {
                if (state != "rest")
                    ((IPseudoClasses)box.Classes).Set(state, true);
                ((IPseudoClasses)box.Classes).Set(":pointerover", state != "rest");
                Pump(window);
                var placeholder = Part<TextBlock>(box, ComboBoxTheme.PlaceholderPart);
                Assert.True(placeholder.IsVisible);
                foreach (var (surface, colour) in Surfaces(box))
                {
                    var under = Over(Effective(box.Background), colour);
                    foreach (var (what, brush) in new[] { ("placeholder", placeholder.Foreground), ("text", box.Foreground) })
                    {
                        var ratio = AccessibleColors.ContrastRatio(Over(Effective(brush), under), under);
                        Assert.True(ratio >= 4.5, $"{what} {state} on {surface}: {ratio:F2}");
                    }
                    var border = AccessibleColors.ContrastRatio(Over(Effective(box.BorderBrush), colour), colour);
                    Assert.True(border >= NonTextContrast, $"border {state} on {surface}: {border:F2}");
                }
            }
            finally
            {
                window.Close();
            }
        }
    }

    [AvaloniaFact]
    public void Placeholder_ShowsOnlyWithoutSelection_AndNotWhenEditable()
    {
        var plain = New();
        var editable = New(editable: true);
        var window = Show(new StackPanel { Children = { plain, editable } });
        try
        {
            Assert.False(Part<TextBlock>(plain, ComboBoxTheme.PlaceholderPart).IsVisible);
            plain.SelectedIndex = -1;
            Pump(window);
            Assert.True(Part<TextBlock>(plain, ComboBoxTheme.PlaceholderPart).IsVisible);
            Assert.False(Part<TextBlock>(editable, ComboBoxTheme.PlaceholderPart).IsVisible);
            // The editable TextBox shows the placeholder itself.
            var text = Part<TextBox>(editable, ComboBoxTheme.EditableTextBoxPart);
            Assert.True(text.IsVisible);
            Assert.Equal("Choose", text.PlaceholderText);
            Assert.False(Part<ContentControl>(editable, ComboBoxTheme.ContentPart).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Editable_InnerTextBoxHasNoChromeOrRing_AndTheFieldShowsFocus()
    {
        var box = New(editable: true);
        var window = Show(box);
        try
        {
            var text = Part<TextBox>(box, ComboBoxTheme.EditableTextBoxPart);
            Assert.Equal(0, Effective(text.Background).A);
            Assert.Equal(new Thickness(0), text.BorderThickness);
            Assert.False(box.IsTabStop);
            // The text lines up with a non-editable ComboBox's: the same padding.
            Assert.Equal(box.Padding, text.Padding);

            text.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.Equal(0, Effective(text.Background).A);
            Assert.False(Ring(text).IsVisible);
            Assert.True(Ring(box).IsVisible);
            Assert.Equal(Colour(box, "TextControlBorderBrushFocused"), Effective(box.BorderBrush));
            Assert.Equal(new Rect(-4, -4, box.Bounds.Width + 8, box.Bounds.Height + 8), Ring(box).Bounds);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Ring_ShowsOnKeyboardFocusOnly_AndLeavesWithFocus()
    {
        var plain = New();
        var editable = New(editable: true);
        var other = new Button { Content = "Other" };
        var window = Show(new StackPanel { Children = { plain, editable, other } });
        try
        {
            plain.Focus(NavigationMethod.Pointer);
            Pump(window);
            Assert.False(Ring(plain).IsVisible);
            // Pointer focus on a non-editable ComboBox leaves the border as it is (it is button-like).
            Assert.Equal(Colour(plain, "ComboBoxBorderBrush"), Effective(plain.BorderBrush));

            other.Focus();
            plain.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.True(Ring(plain).IsVisible);

            var text = Part<TextBox>(editable, ComboBoxTheme.EditableTextBoxPart);
            text.Focus(NavigationMethod.Pointer);
            Pump(window);
            Assert.False(Ring(plain).IsVisible);
            Assert.False(Ring(editable).IsVisible);

            other.Focus();
            text.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.True(Ring(editable).IsVisible);

            other.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.False(Ring(editable).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CaretZone_TakesClicksOnlyWhenEditable()
    {
        var plain = New();
        var editable = New(editable: true);
        var window = Show(new StackPanel { Children = { plain, editable } });
        try
        {
            Assert.False(Part<Icon>(plain, ComboBoxTheme.GlyphPart).IsHitTestVisible);
            var zone = Part<Border>(editable, ComboBoxTheme.GlyphZonePart);
            Assert.NotNull(zone.Background);
            // Over the caret zone, the hit is the zone, not the inner TextBox (whose clicks do not open the list).
            var point = zone.TranslatePoint(new Point(zone.Bounds.Width / 2, zone.Bounds.Height / 2), window)!.Value;
            var hit = window.InputHitTest(point) as Visual;
            Assert.NotNull(hit);
            Assert.False(hit!.GetSelfAndVisualAncestors().OfType<TextBox>().Any());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Glyph_UsesFluentGlyphColours(string theme)
    {
        var box = New();
        var window = Show(box, Variant(theme));
        try
        {
            var glyph = Part<Icon>(box, ComboBoxTheme.GlyphPart);
            Assert.Equal(Colour(box, "ComboBoxDropDownGlyphForeground"), Effective(glyph.Foreground));
            box.IsEnabled = false;
            Pump(window);
            Assert.Equal(Colour(box, "ComboBoxDropDownGlyphForegroundDisabled"), Effective(glyph.Foreground));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void DropDown_OpensFromTheField_AsWideAsIt()
    {
        var box = New();
        var window = Show(box);
        try
        {
            box.IsDropDownOpen = true;
            Pump(window);
            var popup = Part<Popup>(box, ComboBoxTheme.PopupPart);
            Assert.True(popup.IsOpen);
            Assert.Same(Chrome(box), popup.PlacementTarget);
            Assert.Equal(box.Bounds.Width, popup.MinWidth);
            Assert.Equal(4, box.GetLogicalDescendants().OfType<ComboBoxItem>().Count());
            box.SelectedIndex = 2;
            Pump(window);
            Assert.Equal("Pictures", Part<ContentControl>(box, ComboBoxTheme.ContentPart).Content);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Transitions_UseMotionFast_AndBecomeInstantUnderReduceMotion()
    {
        var theme = new StandardsTheme(OsAccessibility.Settings.NotReduced);
        var box = New();
        var window = Show(box, theme: theme);
        try
        {
            var fades = Chrome(box).Transitions!.Cast<TransitionBase>().ToList();
            Assert.Contains(fades, t => t.Property == SquircleBorder.BorderBrushProperty);
            Assert.All(fades, t => Assert.Equal(TimeSpan.FromMilliseconds(100), t.Duration));
            theme.ReduceMotion = true;
            Pump(window);
            Assert.Empty(Chrome(box).Transitions!);
        }
        finally
        {
            window.Close();
        }
    }
}
