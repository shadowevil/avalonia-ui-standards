using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;
using static AvaloniaUiStandards.Tests.PickerHost;

namespace AvaloniaUiStandards.Tests;

/// <summary>DatePicker, TimePicker and CalendarDatePicker: the field, the flyouts and their clip, and the drop-down.</summary>
public class PickerThemeTests
{
    public static TheoryData<string> Fields => new() { nameof(DatePicker), nameof(TimePicker), nameof(CalendarDatePicker) };

    private static Control CreateField(string name) => name switch
    {
        nameof(DatePicker) => new DatePicker(),
        nameof(TimePicker) => new TimePicker(),
        _ => new CalendarDatePicker { Width = 200 },
    };

    // --- The field -----------------------------------------------------------------------------

    [AvaloniaTheory]
    [MemberData(nameof(Fields))]
    public void Field_IsASquircleFieldWithAHairline(string name)
    {
        var (window, field) = Show(CreateField(name));
        try
        {
            var chrome = field.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == FieldParts.ChromePart && b.TemplatedParent == field);
            var picker = (TemplatedControl)field;
            Assert.Equal(new CornerRadius(8), picker.CornerRadius);
            Assert.Equal(new Thickness(1), picker.BorderThickness);
            Assert.Equal(picker.CornerRadius, chrome.CornerRadius);
            Assert.Equal(picker.Bounds.Size, chrome.Bounds.Size);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(nameof(DatePicker), "Normal", 32)]
    [InlineData(nameof(DatePicker), "Compact", 24)]
    [InlineData(nameof(TimePicker), "Normal", 32)]
    [InlineData(nameof(TimePicker), "Compact", 24)]
    [InlineData(nameof(CalendarDatePicker), "Normal", 32)]
    [InlineData(nameof(CalendarDatePicker), "Compact", 24)]
    public void FieldHeight_FollowsDensity(string name, string density, double height)
    {
        var (window, field) = Show(CreateField(name), theme: density == "Compact" ? PickerRenderTests.Compact() : null);
        try
        {
            Assert.Equal(height, field.Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }

    // Expected background and border per state: Fluent's TextControl keys, as TextInput.
    public static TheoryData<string, string, string, string> FieldStates()
    {
        var data = new TheoryData<string, string, string, string>();
        foreach (var name in new[] { nameof(DatePicker), nameof(TimePicker), nameof(CalendarDatePicker) })
        {
            data.Add(name, "rest", "TextControlBackground", "TextControlBorderBrush");
            data.Add(name, ":pointerover", "TextControlBackgroundPointerOver", "TextControlBorderBrushPointerOver");
            data.Add(name, "focus", "TextControlBackgroundFocused", "TextControlBorderBrushFocused");
            data.Add(name, ":disabled", "TextControlBackgroundDisabled", "TextControlBorderBrushDisabled");
        }
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(FieldStates))]
    public void FieldStates_UseTextControlColours(string name, string state, string backgroundKey, string borderKey)
    {
        var (window, field) = Show(CreateField(name));
        try
        {
            if (state == "focus")
                PickerRenderTests.FocusTarget(field).Focus(NavigationMethod.Pointer);
            else if (state == ":disabled")
                field.IsEnabled = false;
            else if (state != "rest")
                ((IPseudoClasses)field.Classes).Set(state, true);
            Pump(window);

            var picker = (TemplatedControl)field;
            Assert.Equal(Effective(Resolve(window, backgroundKey)), Effective(picker.Background));
            Assert.Equal(Effective(Resolve(window, borderKey)), Effective(picker.BorderBrush));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Fields))]
    public void FocusRing_ShowsAroundTheFieldOnKeyboardFocusOnly(string name)
    {
        var (window, field) = Show(CreateField(name));
        try
        {
            var ring = field.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == FocusRing.Part && b.TemplatedParent == field);
            Assert.False(ring.IsVisible);
            PickerRenderTests.FocusTarget(field).Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.True(ring.IsVisible);
            // The ring surrounds the whole field, 4 px out (2 px gap, 2 px ring).
            Assert.Equal(new Point(-4, -4), ring.TranslatePoint(default, field)!.Value);
            Assert.Equal(field.Bounds.Width + 8, ring.Bounds.Width);
        }
        finally
        {
            window.Close();
        }

        (window, field) = Show(CreateField(name));
        try
        {
            PickerRenderTests.FocusTarget(field).Focus(NavigationMethod.Pointer);
            Pump(window);
            Assert.False(field.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == FocusRing.Part && b.TemplatedParent == field).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CalendarDatePicker_ButtonIsAFieldButtonWithTheCalendarIcon()
    {
        var (window, field) = Show(new CalendarDatePicker { Width = 200 });
        try
        {
            var button = field.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_Button");
            var icon = Assert.IsType<Icon>(button.Content);
            Assert.Same(Icons.CalendarBlank, icon.Data);
            Assert.Equal(24, button.Bounds.Width);
            Assert.Equal(24, button.Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(nameof(DatePicker))]
    [InlineData(nameof(TimePicker))]
    public void Segments_AreCentredInTheField(string name)
    {
        var (window, field) = Show<Control>(name == nameof(DatePicker) ? new DatePicker { SelectedDate = PickerRenderTests.Date } : new TimePicker { SelectedTime = PickerRenderTests.Time });
        try
        {
            foreach (var segment in field.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && t.Name?.EndsWith("TextBlock") == true))
            {
                Assert.Equal(0, segment.Padding.Top);
                Assert.Equal(0, segment.Padding.Bottom);
                var top = segment.TranslatePoint(default, field)!.Value.Y;
                Assert.Equal(field.Bounds.Height / 2, top + segment.Bounds.Height / 2, 0.5);
            }
        }
        finally
        {
            window.Close();
        }
    }

    // --- The flyouts -------------------------------------------------------------------------

    [AvaloniaTheory]
    [InlineData(nameof(DatePicker), "Normal", 32)]
    [InlineData(nameof(DatePicker), "Compact", 24)]
    [InlineData(nameof(TimePicker), "Normal", 32)]
    [InlineData(nameof(TimePicker), "Compact", 24)]
    public void Flyout_IsGlassWithSevenListItemRows(string name, string density, double row)
    {
        Control picker = name == nameof(DatePicker) ? new DatePicker { SelectedDate = PickerRenderTests.Date } : new TimePicker { SelectedTime = PickerRenderTests.Time };
        var (window, _) = Show(picker, theme: density == "Compact" ? PickerRenderTests.Compact() : null, height: 500);
        try
        {
            PickerRenderTests.OpenFlyout(picker);
            Pump(window);
            var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
            Assert.True(popup.IsOpen);
            Assert.True(popup.IsUsingOverlayLayer);
            var presenter = (Control)popup.Child!;

            var glass = Assert.IsType<GlassBorder>(Assert.Single(presenter.GetVisualChildren()));
            Assert.Equal(new CornerRadius(16), glass.CornerRadius);
            Assert.Equal(new Thickness(Spacing.XS), glass.Padding);

            var container = presenter.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "PART_PickerContainer");
            Assert.Equal(PickerThemes.VisibleRows * row, container.Bounds.Height);
            foreach (var panel in presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().Where(p => p.IsEffectivelyVisible))
            {
                Assert.Equal(row, panel.ItemHeight);
                Assert.False(ScrollShadows.GetIsEnabled(panel.FindAncestorOfType<ScrollViewer>()!));
            }

            // The highlight is the list item pill in the accent fill, centred on the selected row.
            var highlight = presenter.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == PickerThemes.HighlightPart);
            Assert.Equal(row, highlight.Bounds.Height);
            Assert.Equal(new CornerRadius(8), highlight.CornerRadius);
            Assert.Equal(Effective(Resolve(window, "AccentFillColor")), Effective(highlight.Background));
            Assert.Equal(container.Bounds.Height / 2, highlight.Bounds.Center.Y, 0.5);

            // The selected rows read white over it.
            var selected = presenter.GetVisualDescendants().OfType<ListBoxItem>().Where(i => i.IsSelected && i.IsEffectivelyVisible).ToList();
            Assert.NotEmpty(selected);
            foreach (var item in selected)
            {
                var text = item.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().First();
                Assert.Equal(Effective(Resolve(window, "AccentButtonForeground")), Effective(text.Foreground));
                Assert.Equal(highlight.TranslatePoint(highlight.Bounds.Center - highlight.Bounds.Position, window)!.Value.Y,
                    item.TranslatePoint(new Point(0, item.Bounds.Height / 2), window)!.Value.Y, 0.5);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(nameof(DatePicker))]
    [InlineData(nameof(TimePicker))]
    public void FlyoutButtons_AreLibraryButtonsWithIcons(string name)
    {
        Control picker = name == nameof(DatePicker) ? new DatePicker() : new TimePicker();
        var (window, _) = Show(picker, height: 500);
        try
        {
            PickerRenderTests.OpenFlyout(picker);
            Pump(window);
            var presenter = (Control)picker.GetVisualDescendants().OfType<Popup>().Single().Child!;
            var accept = presenter.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_AcceptButton");
            var dismiss = presenter.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_DismissButton");
            Assert.Contains("accent", accept.Classes);
            Assert.Same(Icons.Check, Assert.IsType<Icon>(accept.Content).Data);
            Assert.Same(Icons.X, Assert.IsType<Icon>(dismiss.Content).Data);
            foreach (var button in new[] { accept, dismiss })
            {
                Assert.Equal(ButtonParts.ChromePart, button.GetVisualDescendants().OfType<SquircleBorder>().First().Name);
                Assert.Equal(32, button.Bounds.Height);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlyoutButtons_AcceptAndDismissAsInFluent()
    {
        var picker = new DatePicker { SelectedDate = PickerRenderTests.Date };
        var (window, _) = Show(picker, height: 500);
        try
        {
            var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
            PickerRenderTests.OpenFlyout(picker);
            Pump(window);
            var presenter = (DatePickerPresenter)popup.Child!;
            presenter.Date = PickerRenderTests.Date.AddDays(3);
            Click(presenter, "PART_DismissButton");
            Pump(window);
            Assert.False(popup.IsOpen);
            Assert.Equal(PickerRenderTests.Date, picker.SelectedDate!.Value.Date);

            PickerRenderTests.OpenFlyout(picker);
            Pump(window);
            presenter.Date = PickerRenderTests.Date.AddDays(3);
            Click(presenter, "PART_AcceptButton");
            Pump(window);
            Assert.False(popup.IsOpen);
            Assert.Equal(PickerRenderTests.Date.AddDays(3), picker.SelectedDate!.Value.Date);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// The known overrun: Fluent's rows run to the overlay's edges, so they fill its rounded
    /// corners and are cut by the curve. Every row's fill is red and the selectors are scrolled
    /// half a row, so rows cross the top and bottom edges; no red may show outside the squircle
    /// concentric with the overlay (inset SpacingXS, radius 16 less the inset). The Fluent
    /// baseline proves the check finds the overrun.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(nameof(DatePickerPresenter))]
    [InlineData(nameof(TimePickerPresenter))]
    public void FlyoutItems_StayInsideTheRoundedCorners(string name)
    {
        Assert.True(RedOutsideCorners(name, fluent: true) > 0, "Fluent baseline shows the overrun");
        Assert.Equal(0, RedOutsideCorners(name, fluent: false));
    }

    private static int RedOutsideCorners(string name, bool fluent)
    {
        Control presenter = name == nameof(DatePickerPresenter)
            ? new DatePickerPresenter { Date = PickerRenderTests.Date }
            : new TimePickerPresenter { Time = PickerRenderTests.Time };
        var window = new Window
        {
            Width = 340, Height = 460, Background = Brushes.White, RequestedThemeVariant = ThemeVariant.Light,
            Content = new Border { Padding = new Thickness(20), Child = presenter, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top },
        };
        if (fluent)
            window.Styles.Add(new FluentTheme());
        window.SetRenderScaling(1);
        window.Show();
        try
        {
            Pump(window);
            foreach (var item in presenter.GetVisualDescendants().OfType<ListBoxItem>())
            {
                item.Transitions = null;
                foreach (var part in item.GetVisualDescendants().OfType<Animatable>())
                    part.Transitions = null;
                var fill = item.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c is SquircleBorder or Border or Avalonia.Controls.Presenters.ContentPresenter);
                switch (fill)
                {
                    case SquircleBorder s: s.Background = Brushes.Red; break;
                    case Border b: b.Background = Brushes.Red; break;
                    case Avalonia.Controls.Presenters.ContentPresenter p: p.Background = Brushes.Red; break;
                }
            }
            foreach (var scroller in presenter.GetVisualDescendants().OfType<ScrollViewer>().Where(s => s.IsEffectivelyVisible))
                scroller.Offset += new Vector(0, 13);
            Pump(window);

            // The rows must stay inside a squircle concentric with the overlay: inset SpacingXS from
            // its top and sides, its radius the overlay's less the inset, down to the selectors' bottom.
            var overlay = fluent
                ? presenter.GetVisualDescendants().OfType<Border>().First()
                : (Control)presenter.GetVisualDescendants().OfType<GlassBorder>().Single();
            var selectors = presenter.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "PART_PickerContainer");
            var origin = overlay.TranslatePoint(default, window)!.Value;
            var bottom = selectors.TranslatePoint(new Point(0, selectors.Bounds.Height), window)!.Value.Y;
            var rect = new Rect(origin.X + Spacing.XS, origin.Y + Spacing.XS, overlay.Bounds.Width - 2 * Spacing.XS, bottom - origin.Y - Spacing.XS);
            var shape = SquircleGeometry.Create(rect, new CornerRadius(16 - Spacing.XS), SquircleBorder.DefaultSmoothing);

            using var frame = Frame.Capture(window);
            var red = 0;
            const int corner = 20;
            foreach (var (cx, cy, dx, dy) in new[]
                     {
                         (rect.Left, rect.Top, 1, 1), (rect.Right - corner, rect.Top, -1, 1),
                         (rect.Left, rect.Bottom - corner, 1, -1), (rect.Right - corner, rect.Bottom - corner, -1, -1),
                     })
                for (var y = (int)cy; y < cy + corner; y++)
                    for (var x = (int)cx; x < cx + corner; x++)
                    {
                        // Clearly outside: still outside 1.5 px further in, past anti-aliasing.
                        var inward = new Point(x + 0.5 + 1.5 * dx, y + 0.5 + 1.5 * dy);
                        if (shape.FillContains(inward))
                            continue;
                        var (r, g, b) = frame[x, y];
                        if (r > 200 && g < 90 && b < 90)
                            red++;
                    }
            return red;
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CalendarDatePicker_DropDownIsGlassBelowTheField()
    {
        var picker = new CalendarDatePicker { Width = 200, SelectedDate = PickerRenderTests.Date };
        var (window, _) = Show(picker, height: 500);
        try
        {
            picker.IsDropDownOpen = true;
            Pump(window);
            var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
            Assert.True(popup.IsUsingOverlayLayer);
            var glass = Assert.IsType<GlassBorder>(popup.Child);
            var calendar = Assert.IsType<Calendar>(glass.Child);
            Assert.Equal(Colors.Transparent, Effective(calendar.Background));
            Assert.Equal(default, calendar.BorderThickness);
            var top = glass.TranslatePoint(default, window)!.Value.Y;
            var fieldBottom = picker.TranslatePoint(new Point(0, picker.Bounds.Height), window)!.Value.Y;
            Assert.Equal(fieldBottom + Spacing.XS, top, 0.5);

            // Choosing a day closes it and sets the date, as in Fluent.
            var day = calendar.GetVisualDescendants().OfType<CalendarDayButton>().First(d => d.Content?.ToString() == "20" && !d.Classes.Contains(":inactive"));
            var centre = day.TranslatePoint(new Point(day.Bounds.Width / 2, day.Bounds.Height / 2), window)!.Value;
            window.MouseMove(centre);
            window.MouseDown(centre, MouseButton.Left);
            window.MouseUp(centre, MouseButton.Left);
            Pump(window);
            Assert.False(popup.IsOpen);
            Assert.Equal(new DateTime(2026, 3, 20), picker.SelectedDate);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Transitions_AreTheFixedThemeObjects_AndEmptyUnderReduceMotion()
    {
        var reduced = new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true };
        foreach (var (theme, expectEmpty) in new[] { ((StandardsTheme?)null, false), (reduced, true) })
        {
            // The field chrome, a selector row in the open flyout and a calendar cell.
            var picker = new DatePicker { SelectedDate = PickerRenderTests.Date };
            var calendar = new Calendar { DisplayDate = PickerRenderTests.Date };
            var window = new Window { Width = 400, Height = 800, Content = new StackPanel { Children = { picker, calendar } } };
            if (theme is not null)
                window.Styles.Add(theme);
            window.Show();
            Pump(window);
            try
            {
                PickerRenderTests.OpenFlyout(picker);
                Pump(window);
                var presenter = (Control)picker.GetVisualDescendants().OfType<Popup>().Single().Child!;
                var row = presenter.GetVisualDescendants().OfType<ListBoxItem>().First(i => i.IsEffectivelyVisible);
                var day = calendar.GetVisualDescendants().OfType<CalendarDayButton>().First();
                Assert.True(window.TryFindResource(ButtonParts.ChromeTransitionsKey, out var shared));
                foreach (var chrome in new[]
                         {
                             picker.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == FieldParts.ChromePart),
                             row.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart),
                             day.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart),
                         })
                {
                    Assert.Same(shared, chrome.Transitions);
                    Assert.Equal(expectEmpty, chrome.Transitions!.Count == 0);
                }
            }
            finally
            {
                window.Close();
            }
        }
    }

    [AvaloniaTheory]
    [InlineData(nameof(DatePicker))]
    [InlineData(nameof(TimePicker))]
    public void FlyoutSelectors_ShowTheRingOnTheFocusedColumn(string name)
    {
        Control picker = name == nameof(DatePicker) ? new DatePicker { SelectedDate = PickerRenderTests.Date } : new TimePicker { SelectedTime = PickerRenderTests.Time };
        var (window, _) = Show(picker, height: 500);
        try
        {
            PickerRenderTests.OpenFlyout(picker);
            Pump(window);
            var presenter = (Control)picker.GetVisualDescendants().OfType<Popup>().Single().Child!;
            var selectors = presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().Where(p => p.IsEffectivelyVisible)
                .OrderBy(p => p.TranslatePoint(default, presenter)!.Value.X).ToList();
            SquircleBorder Ring(DateTimePickerPanel selector) => ((Panel)selector.FindAncestorOfType<ScrollViewer>()!.Parent!)
                .Children.OfType<SquircleBorder>().Single(b => b.Name == PickerThemes.ColumnFocusRingPart);
            var viewport = presenter.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == PickerThemes.ViewportPart);
            var highlight = presenter.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == PickerThemes.HighlightPart);
            var fieldRing = picker.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == FocusRing.Part);
            Assert.All(selectors, s => Assert.False(Ring(s).IsVisible));
            Assert.All(selectors, s => Assert.Null(s.FocusAdorner));

            // Tab moves from selector to selector; the focused column alone shows the ring. The
            // flyout focuses its first selector when it opens, so focus is moved off it first.
            presenter.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_DismissButton").Focus(NavigationMethod.Pointer);
            selectors[0].Focus(NavigationMethod.Tab);
            Pump(window);
            for (var i = 0; i < selectors.Count; i++)
            {
                Assert.True(selectors[i].IsFocused, $"selector {i} focused");
                for (var j = 0; j < selectors.Count; j++)
                    Assert.Equal(i == j, Ring(selectors[j]).IsVisible);

                // Around the column's part of the highlight (2 px gap and 2 px ring above and
                // below), inside the column and so inside the viewport's curve.
                var ring = Ring(selectors[i]);
                var ringBox = new Rect(ring.TranslatePoint(default, viewport)!.Value, ring.Bounds.Size);
                var rowBox = new Rect(highlight.TranslatePoint(default, viewport)!.Value, highlight.Bounds.Size);
                Assert.Equal(rowBox.Height + 8, ringBox.Height);
                Assert.Equal(rowBox.Center.Y, ringBox.Center.Y, 0.5);
                Assert.True(new Rect(viewport.Bounds.Size).Contains(ringBox), $"{ringBox} inside the viewport");
                Assert.Equal(new CornerRadius(12), ring.CornerRadius);
                Assert.Equal(Effective(Resolve(window, "SystemControlHighlightAccentBrush")), Effective(ring.BorderBrush));
                // The field shows no ring while a column does.
                Assert.False(fieldRing.IsVisible);

                window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
                Pump(window);
            }

            // Pointer focus shows no ring.
            selectors[0].Focus(NavigationMethod.Pointer);
            Pump(window);
            Assert.All(selectors, s => Assert.False(Ring(s).IsVisible));
        }
        finally
        {
            window.Close();
        }
    }

    // Opened from the keyboard, the flyout's first selector has keyboard focus and shows its
    // ring; opened by pointer, it has focus but no ring.
    [AvaloniaTheory]
    [InlineData(nameof(DatePicker), "Enter")]
    [InlineData(nameof(DatePicker), "Space")]
    [InlineData(nameof(DatePicker), "Pointer")]
    [InlineData(nameof(TimePicker), "Enter")]
    [InlineData(nameof(TimePicker), "Space")]
    [InlineData(nameof(TimePicker), "Pointer")]
    public void Flyout_OpenedFromTheKeyboard_ShowsTheRingOnTheFirstSelector(string name, string how)
    {
        Control picker = name == nameof(DatePicker) ? new DatePicker { SelectedDate = PickerRenderTests.Date } : new TimePicker { SelectedTime = PickerRenderTests.Time };
        var (window, _) = Show(picker, height: 500);
        try
        {
            var opener = (Button)PickerRenderTests.FocusTarget(picker);
            if (how == "Pointer")
            {
                var centre = opener.TranslatePoint(new Point(opener.Bounds.Width / 2, opener.Bounds.Height / 2), window)!.Value;
                window.MouseMove(centre);
                window.MouseDown(centre, MouseButton.Left);
                window.MouseUp(centre, MouseButton.Left);
            }
            else
            {
                opener.Focus(NavigationMethod.Tab);
                var (key, physical) = how == "Enter" ? (Key.Enter, PhysicalKey.Enter) : (Key.Space, PhysicalKey.Space);
                window.KeyPress(key, RawInputModifiers.None, physical, null);
                window.KeyRelease(key, RawInputModifiers.None, physical, null);
            }
            Pump(window);

            var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
            Assert.True(popup.IsOpen);
            var presenter = (Control)popup.Child!;
            var first = presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().Where(p => p.IsEffectivelyVisible)
                .OrderBy(p => p.TranslatePoint(default, presenter)!.Value.X).First();
            Assert.True(first.IsFocused);
            var ring = ((Panel)first.FindAncestorOfType<ScrollViewer>()!.Parent!)
                .Children.OfType<SquircleBorder>().Single(b => b.Name == PickerThemes.ColumnFocusRingPart);
            Assert.Equal(how != "Pointer", ring.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Normal", 32)]
    [InlineData("Compact", 24)]
    public void StepButtons_ShowOverASelectorOnHover(string density, double row)
    {
        var picker = new DatePicker { SelectedDate = PickerRenderTests.Date };
        var (window, _) = Show(picker, theme: density == "Compact" ? PickerRenderTests.Compact() : null, height: 500);
        try
        {
            PickerRenderTests.OpenFlyout(picker);
            Pump(window);
            var presenter = (Control)picker.GetVisualDescendants().OfType<Popup>().Single().Child!;
            var host = (Panel)presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().First().FindAncestorOfType<ScrollViewer>()!.Parent!;
            var steps = host.Children.OfType<RepeatButton>().ToList();
            Assert.Equal(2, steps.Count);
            Assert.All(steps, s => Assert.False(s.IsVisible));

            ((IPseudoClasses)host.Classes).Set(":pointerover", true);
            Pump(window);
            foreach (var step in steps)
            {
                Assert.True(step.IsVisible);
                Assert.Equal(row, step.Bounds.Height);
                Assert.False(step.Focusable);
                var chrome = step.GetVisualDescendants().OfType<SquircleBorder>().First(b => b.Name == ButtonParts.ChromePart);
                var fill = chrome.GetBaseValue(SquircleBorder.BackgroundProperty) is { HasValue: true } set ? Effective(set.Value) : Effective(chrome.Background);
                Assert.Equal(Effective(Resolve(window, "GlassTintColor")), fill);
                Assert.Equal(255, fill.A);
            }
            Assert.Same(Icons.CaretUp, Assert.IsType<Icon>(steps.Single(s => s.VerticalAlignment == Avalonia.Layout.VerticalAlignment.Top).Content).Data);
            Assert.Same(Icons.CaretDown, Assert.IsType<Icon>(steps.Single(s => s.VerticalAlignment == Avalonia.Layout.VerticalAlignment.Bottom).Content).Data);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CalendarDatePicker_FieldRingStaysHiddenWhileACalendarCellHasTheRing()
    {
        var picker = new CalendarDatePicker { Width = 200, SelectedDate = PickerRenderTests.Date };
        var (window, _) = Show(picker, height: 500);
        try
        {
            picker.IsDropDownOpen = true;
            Pump(window);
            var calendar = (Calendar)((GlassBorder)picker.GetVisualDescendants().OfType<Popup>().Single().Child!).Child!;
            var day = calendar.GetVisualDescendants().OfType<CalendarDayButton>().First(d => d.Classes.Contains(":selected"));
            day.Focus(NavigationMethod.Tab);
            window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            Pump(window);

            var cursor = calendar.GetVisualDescendants().OfType<CalendarDayButton>().Single(d => d.Classes.Contains(":dayfocused"));
            Assert.True(cursor.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == FocusRing.Part).IsVisible);
            Assert.False(picker.GetVisualDescendants().OfType<SquircleBorder>().Single(b => b.Name == FocusRing.Part && b.TemplatedParent == picker).IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Helpers -----------------------------------------------------------------------------

    private static void Click(Control root, string name) =>
        root.GetVisualDescendants().OfType<Button>().Single(b => b.Name == name).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
}
