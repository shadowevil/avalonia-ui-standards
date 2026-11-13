using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.Colours;

namespace AvaloniaUiStandards.Tests;

/// <summary>SelectionOptions.AccentFlag: the bar on selected items, the square leading corners, inheritance, the ring, density and contrast.</summary>
public class AccentFlagTests
{
    private static readonly CornerRadius Round = new(8);
    private static readonly CornerRadius Flagged = new(0, 8, 8, 0);

    [AvaloniaFact]
    public void Default_IsOff()
    {
        var (window, list) = Show(NewList());
        try
        {
            Assert.False(SelectionOptions.GetAccentFlag(list));
            var item = Item(list, 0);
            item.IsSelected = true;
            Pump(window);
            Assert.Equal(Colors.Transparent, Fill(Bar(item)));
            Assert.Equal(Round, Chrome(item).CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    // Only the selected item shows the bar and square leading corners; hover and press show neither.
    [AvaloniaTheory]
    [InlineData("rest", false)]
    [InlineData(":pointerover", false)]
    [InlineData(":pressed", false)]
    [InlineData("selected", true)]
    [InlineData("selected:pointerover", true)]
    [InlineData("selected:pressed", true)]
    public void On_FlagsOnlySelectedItems(string state, bool flagged)
    {
        var list = NewList();
        SelectionOptions.SetAccentFlag(list, true);
        var (window, _) = Show(list);
        try
        {
            var item = Item(list, 0);
            SetState(item, state);
            Pump(window);
            Assert.Equal(flagged ? BarColour(item) : Colors.Transparent, Fill(Bar(item)));
            Assert.Equal(flagged ? Flagged : Round, Chrome(item).CornerRadius);
            // The other items stay round and unflagged.
            Assert.Equal(Colors.Transparent, Fill(Bar(Item(list, 1))));
            Assert.Equal(Round, Chrome(Item(list, 1)).CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SelectedDisabled_ShowsTheFlagInTheDisabledTextColour()
    {
        var list = NewList();
        SelectionOptions.SetAccentFlag(list, true);
        var (window, _) = Show(list);
        try
        {
            var item = Item(list, 0);
            item.IsSelected = true;
            item.IsEnabled = false;
            Pump(window);
            Assert.Equal(Effective(Resolve(item, "AccentButtonForegroundDisabled")), Fill(Bar(item)));
            Assert.Equal(Flagged, Chrome(item).CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Flag_IsInherited_AndAnItemOverridesIt()
    {
        var list = NewList();
        SelectionOptions.SetAccentFlag(list, true);
        var (window, _) = Show(list);
        try
        {
            var (first, second) = (Item(list, 0), Item(list, 1));
            Assert.True(SelectionOptions.GetAccentFlag(first));
            SelectionOptions.SetAccentFlag(first, false);
            first.IsSelected = true;
            Pump(window);
            Assert.Equal(Colors.Transparent, Fill(Bar(first)));
            Assert.Equal(Round, Chrome(first).CornerRadius);

            // Turned on for one item while the list has it off.
            SelectionOptions.SetAccentFlag(list, false);
            SelectionOptions.SetAccentFlag(second, true);
            list.SelectedIndex = 1;
            Pump(window);
            Assert.Equal(BarColour(second), Fill(Bar(second)));
            Assert.Equal(Flagged, Chrome(second).CornerRadius);

            // Turning it off on a selected item takes the flag away.
            SelectionOptions.SetAccentFlag(second, false);
            Pump(window);
            Assert.Equal(Colors.Transparent, Fill(Bar(second)));
            Assert.Equal(Round, Chrome(second).CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    // The ring is grown around the flagged shape: square leading corners give it a radius of the
    // ring's offset there (the outline 4 px out from a square corner), 8 + 4 at the trailing ones.
    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void FocusRing_FollowsTheFlaggedShape(bool flag, bool selected)
    {
        var list = NewList();
        SelectionOptions.SetAccentFlag(list, flag);
        var (window, _) = Show(list);
        try
        {
            var item = Item(list, 0);
            item.IsSelected = selected;
            Assert.True(item.Focus(NavigationMethod.Tab));
            Pump(window);
            var ring = Part<SquircleBorder>(item, FocusRing.Part);
            Assert.True(ring.IsVisible);
            Assert.Equal(flag && selected ? new CornerRadius(4, 12, 12, 4) : new CornerRadius(12), ring.CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    // The bar is 3 wide, the item's full height, flush on its leading edge, at both densities.
    [AvaloniaTheory]
    [InlineData("Normal", 32)]
    [InlineData("Compact", 24)]
    public void Bar_IsFullHeightOnTheLeadingEdge(string density, double height)
    {
        var list = NewList();
        SelectionOptions.SetAccentFlag(list, true);
        var (window, _) = Show(list, density: density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal);
        try
        {
            var item = Item(list, 0);
            item.IsSelected = true;
            Pump(window);
            var bar = Bar(item);
            Assert.Equal(new Rect(0, 0, 3, height), new Rect(bar.TranslatePoint(default, item)!.Value, bar.Bounds.Size));
        }
        finally
        {
            window.Close();
        }
    }

    // The bar is flush on the edge, so it stands apart from the fill on one side and the surface on
    // the other, in every selected state and both themes (white would match a white surface, and the
    // pressed accent the pressed fill).
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Bar_StandsOutFromTheFillAndTheSurface(string theme)
    {
        const double minimum = 1.75; // white measures 1.0 on the card, the pressed accent 1.0 on the pressed fill
        var list = NewList();
        SelectionOptions.SetAccentFlag(list, true);
        var (window, _) = Show(list, theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark);
        try
        {
            var item = Item(list, 0);
            foreach (var state in new[] { "selected", "selected:pointerover", "selected:pressed" })
            {
                SetState(item, state);
                Pump(window);
                var ratio = AccessibleColors.ContrastRatio(Fill(Bar(item)), Fill(Chrome(item)));
                Assert.True(ratio >= minimum, $"{state}: {ratio:F2}");
                SetState(item, "rest");
            }
            foreach (var surfaceKey in new[] { "SystemRegionColor", "SystemChromeMediumLowColor", "ComboBoxDropDownBackground" })
            {
                var surface = Over(Effective(Resolve(item, surfaceKey)), theme == "Light" ? Colors.White : Colors.Black);
                var ratio = AccessibleColors.ContrastRatio(BarColour(item), surface);
                Assert.True(ratio >= minimum, $"{surfaceKey}: {ratio:F2}");
            }
        }
        finally
        {
            window.Close();
        }
    }

    // The bar fades with the chrome's transitions (the same objects), none under reduce motion.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Bar_FadesWithTheChrome(bool reduceMotion)
    {
        var list = NewList();
        SelectionOptions.SetAccentFlag(list, true);
        var (window, _) = Show(list, reduceMotion: reduceMotion);
        try
        {
            var item = Item(list, 0);
            var transitions = Bar(item).Transitions!;
            Assert.Same(Chrome(item).Transitions, transitions);
            if (reduceMotion)
                Assert.Empty(transitions);
            else
                Assert.Contains(transitions, t => t is CrossFadeBrushTransition { Property: var p } && p == SquircleBorder.BackgroundProperty);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Sidebar_FlagsTheSelectedRow_InEveryStage()
    {
        var sidebar = SidebarTests.NewSidebar();
        SelectionOptions.SetAccentFlag(sidebar, true);
        foreach (var width in new[] { 1200d, 800d })
        {
            var window = new StandardsWindow { Width = width, Height = 520, Sidebar = sidebar, Content = new TextBlock { Text = "Content" } };
            window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
            window.Show();
            try
            {
                Pump(window);
                sidebar.SelectedIndex = 2;
                Pump(window);
                AssertFlagged(sidebar);
                if (width < 1000)
                {
                    // The icon rail's overlay shows the same rows, flagged the same way.
                    sidebar.Toggle();
                    Pump(window);
                    Assert.True(sidebar.IsOverlayOpen);
                    AssertFlagged(sidebar);
                }
            }
            finally
            {
                window.Sidebar = null;
                window.Close();
            }
        }
    }

    // Only the selected row has the bar, full height, with square leading corners.
    private static void AssertFlagged(Sidebar sidebar)
    {
        var rows = sidebar.Items.OfType<SidebarItem>().ToList();
        var selected = rows.Single(r => r.IsSelected);
        Assert.Equal(BarColour(selected), Fill(Bar(selected)));
        Assert.Equal(Flagged, Chrome(selected).CornerRadius);
        Assert.Equal(selected.Bounds.Height, Bar(selected).Bounds.Height);
        Assert.All(rows.Where(r => r != selected), r => Assert.Equal(Colors.Transparent, Fill(Bar(r))));
    }

    [AvaloniaFact]
    public void TreeView_FlagsThePillsLeadingEdge_PerItemOrSubtree()
    {
        var child = new TreeViewItem { Header = "Squircle" };
        var parent = new TreeViewItem { Header = "Shape", IsExpanded = true, Items = { child } };
        var other = new TreeViewItem { Header = "Colour" };
        var tree = new TreeView { Width = 260, Items = { parent, other } };
        SelectionOptions.SetAccentFlag(tree, true);
        var window = new Window { Width = 400, Height = 300, Content = tree };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        window.Show();
        try
        {
            Pump(window);
            child.IsSelected = true;
            Pump(window);
            var chrome = TreePart<SquircleBorder>(child, ButtonParts.ChromePart);
            var bar = TreePart<SquircleBorder>(child, AccentFlag.Part);
            Assert.Equal(BarColour(child), Fill(bar));
            Assert.Equal(Flagged, chrome.CornerRadius);
            Assert.Equal(chrome.TranslatePoint(default, window), bar.TranslatePoint(default, window));
            Assert.Equal(chrome.Bounds.Height, bar.Bounds.Height);
            // The parent is not selected: no flag on it.
            Assert.Equal(Colors.Transparent, Fill(TreePart<SquircleBorder>(parent, AccentFlag.Part)));

            // Off on the parent turns it off for its subtree; the rest of the tree keeps it.
            SelectionOptions.SetAccentFlag(parent, false);
            Pump(window);
            Assert.Equal(Colors.Transparent, Fill(bar));
            Assert.Equal(Round, chrome.CornerRadius);
            other.IsSelected = true;
            Pump(window);
            Assert.Equal(BarColour(other), Fill(TreePart<SquircleBorder>(other, AccentFlag.Part)));
        }
        finally
        {
            window.Close();
        }
    }

    // Set on a ComboBox or an AutoCompleteBox, the flag reaches the drop-down's items.
    [AvaloniaFact]
    public void ComboBox_PassesTheFlagToItsDropDownItems()
    {
        var box = new ComboBox { ItemsSource = new[] { "One", "Two", "Three" }, SelectedIndex = 1 };
        SelectionOptions.SetAccentFlag(box, true);
        var window = new Window { Width = 300, Height = 300, Content = box };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        window.Show();
        try
        {
            box.IsDropDownOpen = true;
            Pump(window);
            var items = box.GetLogicalDescendants().OfType<ComboBoxItem>().ToList();
            Assert.Equal(BarColour(items[1]), Fill(Bar(items[1])));
            Assert.Equal(Flagged, Chrome(items[1]).CornerRadius);
            Assert.Equal(Colors.Transparent, Fill(Bar(items[0])));
        }
        finally
        {
            box.IsDropDownOpen = false;
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AutoCompleteBox_PassesTheFlagToItsSuggestions()
    {
        var box = new AutoCompleteBox { Width = 200, ItemsSource = new[] { "Documents", "Downloads", "Desktop" }, MinimumPrefixLength = 1 };
        SelectionOptions.SetAccentFlag(box, true);
        var window = new Window { Width = 300, Height = 300, Content = box };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        window.Show();
        try
        {
            var text = Part<TextBox>(box, "PART_TextBox");
            text.Focus();
            text.Text = "D";
            Pump(window);
            Assert.True(box.IsDropDownOpen);
            var list = (ListBox)((SquircleBorder)Part<Popup>(box, "PART_Popup").Child!).Child!;
            list.SelectedIndex = 1;
            Pump(window);
            var item = Item(list, 1);
            Assert.True(SelectionOptions.GetAccentFlag(item));
            Assert.Equal(BarColour(item), Fill(Bar(item)));
            Assert.Equal(Flagged, Chrome(item).CornerRadius);
        }
        finally
        {
            box.IsDropDownOpen = false;
            window.Close();
        }
    }

    // --- Helpers -----------------------------------------------------------------------------

    private static ListBox NewList() => new() { Width = 200, ItemsSource = new[] { "Documents", "Downloads", "Pictures" } };

    private static (Window Window, ListBox List) Show(ListBox list, ThemeVariant? variant = null,
        DensityStyle density = DensityStyle.Normal, bool reduceMotion = true)
    {
        var window = new Window { Width = 300, Height = 200, RequestedThemeVariant = variant ?? ThemeVariant.Light, Content = list };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion, Density = density });
        window.Show();
        Pump(window);
        return (window, list);
    }

    private static void SetState(ListBoxItem item, string state)
    {
        item.IsSelected = state.StartsWith("selected");
        ((IPseudoClasses)item.Classes).Set(":pointerover", state.EndsWith(":pointerover"));
        ((IPseudoClasses)item.Classes).Set(":pressed", state.EndsWith(":pressed"));
    }

    private static ListBoxItem Item(ListBox list, int index) => (ListBoxItem)list.ContainerFromIndex(index)!;

    private static SquircleBorder Chrome(Control item) => Part<SquircleBorder>(item, ButtonParts.ChromePart);

    private static SquircleBorder Bar(Control item) => Part<SquircleBorder>(item, AccentFlag.Part);

    private static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    // The item's own template part (its children's parts come after).
    private static T TreePart<T>(TreeViewItem item, string name) where T : Control =>
        item.GetVisualDescendants().OfType<T>().First(c => c.Name == name && c.TemplatedParent == item);

    private static Color Fill(SquircleBorder border) => border.Background is { } brush ? Effective(brush) : Colors.Transparent;

    private static Color BarColour(Control item) => Effective(Resolve(item, AccentFlag.BarKey));

    private static object Resolve(Control control, string key)
    {
        Assert.True(control.TryFindResource(key, control.ActualThemeVariant, out var value), key);
        return value!;
    }

    private static void Pump(Window window)
    {
        for (var i = 0; i < 2; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
        Dispatcher.UIThread.RunJobs();
    }
}
