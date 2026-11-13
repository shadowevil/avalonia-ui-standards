using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>The TreeView Block: list item rows, indentation, the caret, expand and collapse, keyboard and renders.</summary>
public class TreeViewTests
{
    private enum RowState { Rest, Hover, Pressed, Selected, SelectedHover, Disabled, Focused }

    private static readonly string[] RowStateLabels = ["Rest", "Hover", "Pressed", "Selected", "Selected + hover", "Disabled", "Focused"];

    [AvaloniaFact]
    public void Theme_GivesFullWidthListItemRows()
    {
        var (window, tree) = Show();
        try
        {
            Assert.Equal(new Thickness(4), tree.Padding);
            Assert.Equal(Colors.Transparent, ((ISolidColorBrush)tree.Background!).Color);
            Assert.Null(tree.FocusAdorner);
            var root = Items(tree)[0];
            var child = Items(tree)[1];
            Assert.Equal(1, child.Level);
            // Every row, at any level, is a squircle inset 4 from the tree's edges, the tree's full width.
            foreach (var item in new[] { root, child })
            {
                var chrome = Chrome(item);
                Assert.Equal(new CornerRadius(8), chrome.CornerRadius);
                Assert.Equal(4, chrome.TranslatePoint(default, tree)!.Value.X);
                Assert.Equal(tree.Bounds.Width - 8, chrome.Bounds.Width);
                Assert.Equal(32, chrome.Bounds.Height);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Normal", 20, 32)]
    [InlineData("Compact", 16, 24)]
    public void Indent_IsOneCaretWidthPerLevel(string density, double indent, double height)
    {
        var (window, tree) = Show(density: density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal);
        try
        {
            var items = Items(tree);
            items[1].IsExpanded = true;
            Pump(window);
            var rootCaret = Caret(items[0]);
            var childCaret = Caret(items[1]);
            var grandchildCaret = Caret(items[3]);
            Assert.Equal(indent, rootCaret.Bounds.Width);
            Assert.Equal(height, Chrome(items[1]).Bounds.Height);
            // The layout position (a turned caret's own origin is rotated with it).
            double X(Control v) => ((Visual)v.GetVisualParent()!).TranslatePoint(default, tree)!.Value.X + v.Bounds.X;
            Assert.Equal(indent, X(childCaret) - X(rootCaret), 1);
            Assert.Equal(2 * indent, X(grandchildCaret) - X(rootCaret), 1);
            // A child's caret starts where its parent's caret ends.
            Assert.Equal(X(rootCaret) + rootCaret.Bounds.Width, X(childCaret), 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Caret_TurnsDownWhenExpanded_AndIsBlankOnLeaves()
    {
        var (window, tree) = Show();
        try
        {
            var items = Items(tree);
            var caret = Caret(items[0]);
            Assert.Equal(Icons.CaretRight, caret.GetVisualDescendants().OfType<Icon>().Single().Data);
            Assert.False(caret.Focusable);
            Assert.True(items[0].IsExpanded);
            Assert.Equal(Rounded(Matrix.CreateRotation(Math.PI / 2)), Rounded(caret.RenderTransform!.Value));

            items[0].IsExpanded = false;
            Pump(window);
            Assert.Equal(Matrix.Identity, Rounded(caret.RenderTransform!.Value));
            Assert.False(Children(items[0]).IsVisible);

            // Clicking the caret expands.
            var point = caret.TranslatePoint(new Point(caret.Bounds.Width / 2, caret.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Pump(window);
            Assert.True(items[0].IsExpanded);
            Assert.True(Children(items[0]).IsVisible);

            // A leaf keeps the caret's room, with nothing in it.
            var leaf = items[2];
            Assert.Equal(0, Caret(leaf).Opacity);
            Assert.Equal(Caret(items[1]).TranslatePoint(default, tree)!.Value.X, Caret(leaf).TranslatePoint(default, tree)!.Value.X, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Caret_TurnsOverMotionNormal_InstantUnderReduceMotion()
    {
        var (window, tree) = Show(reduceMotion: false);
        try
        {
            var transition = Assert.IsType<TransformOperationsTransition>(Assert.Single(Caret(Items(tree)[0]).Transitions!));
            Assert.Equal(TimeSpan.FromMilliseconds(200), transition.Duration);
        }
        finally
        {
            window.Close();
        }

        var (reduced, reducedTree) = Show(reduceMotion: true);
        try
        {
            Assert.Empty(Caret(Items(reducedTree)[0]).Transitions!);
        }
        finally
        {
            reduced.Close();
        }
    }

    [AvaloniaFact]
    public void Keyboard_FollowsFluent_ArrowsMoveAndLeftRightCollapseAndExpand()
    {
        var (window, tree) = Show();
        try
        {
            var items = Items(tree);
            var (root, child, sibling, grandchild) = (items[0], items[1], items[2], items[3]);
            tree.SelectedItem = root;
            root.Focus(NavigationMethod.Tab);
            Pump(window);

            Press(window, Key.Down);
            Assert.Same(child, tree.SelectedItem);

            // Right on a collapsed parent expands it; Right again moves to its first child.
            Assert.False(child.IsExpanded);
            Press(window, Key.Right);
            Assert.True(child.IsExpanded);
            Assert.Same(child, tree.SelectedItem);
            Press(window, Key.Right);
            Assert.Same(grandchild, tree.SelectedItem);

            // Left on a leaf moves to the parent; Left on an expanded parent collapses it.
            Press(window, Key.Left);
            Assert.Same(child, tree.SelectedItem);
            Press(window, Key.Left);
            Assert.False(child.IsExpanded);

            Press(window, Key.Down);
            Assert.Same(sibling, tree.SelectedItem);
            Press(window, Key.Up);
            Press(window, Key.Up);
            Assert.Same(root, tree.SelectedItem);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HoverAndPress_AreTheRowsOwn_NotTheParents()
    {
        var (window, tree) = Show();
        try
        {
            var items = Items(tree);
            var child = items[1];
            var point = Chrome(child).TranslatePoint(new Point(60, 16), window)!.Value;
            window.MouseMove(point);
            Pump(window);
            Assert.Contains(":pointerover", Chrome(child).Classes);
            Assert.Equal(Colors.Transparent, Fill(items[0]));
            Assert.NotEqual(Colors.Transparent, Fill(child));

            window.MouseDown(point, MouseButton.Left);
            Pump(window);
            Assert.Contains(":pressed", Chrome(child).Classes);
            Assert.DoesNotContain(":pressed", Chrome(items[0]).Classes);
            window.MouseUp(point, MouseButton.Left);
            Pump(window);
            Assert.DoesNotContain(":pressed", Chrome(child).Classes);
            Assert.Same(child, tree.SelectedItem);
            // Selected: the accent fill and white text, on this row only.
            Assert.Equal(Colors.White, ((ISolidColorBrush)Header(child).Foreground!).Color);
            Assert.Equal(Colors.Transparent, Fill(items[0]));
        }
        finally
        {
            window.Close();
        }
    }

    // --- Renders -----------------------------------------------------------------------------

    [AvaloniaFact]
    public void TreeView_Renders()
    {
        using var sheet = new Sheet("TreeView: list item rows, a caret that turns down, one caret width of indent per level, no lines");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            sheet.AddSection(themeLabel + ": row states", RowStateLabels,
            [
                ("Glass panel", Enum.GetValues<RowState>().Select(s => StateCell(sheet, theme, s)).ToList()),
            ]);
            sheet.AddSection(themeLabel + ": a tree", ["Normal", "Compact density"],
            [
                ("Glass panel", [TreeCell(sheet, theme, DensityStyle.Normal), TreeCell(sheet, theme, DensityStyle.Compact)]),
            ]);
        }
        sheet.Save("m3-treeview.png");
    }

    private static Control StateCell(Sheet sheet, ThemeVariant theme, RowState state)
    {
        var item = new TreeViewItem { Header = "Documents", ItemsSource = new[] { "Notes" } };
        var tree = new TreeView { Width = 170, ItemsSource = new[] { item } };
        return sheet.Cell(ListBoxRenderTests.OnSurface(tree, ListBoxRenderTests.Surface.GlassPanel), theme, afterShow: w =>
        {
            w.UpdateLayout();
            var chrome = Chrome(item);
            if (state is RowState.Selected or RowState.SelectedHover)
                item.IsSelected = true;
            if (state is RowState.Hover or RowState.SelectedHover)
                ((IPseudoClasses)chrome.Classes).Set(":pointerover", true);
            if (state == RowState.Pressed)
                ((IPseudoClasses)chrome.Classes).Set(":pressed", true);
            if (state == RowState.Disabled)
                item.IsEnabled = false;
            if (state == RowState.Focused)
                item.Focus(NavigationMethod.Tab);
        });
    }

    private static Control TreeCell(Sheet sheet, ThemeVariant theme, DensityStyle density)
    {
        var tree = NewTree();
        tree.Width = 260;
        return sheet.Cell(ListBoxRenderTests.OnSurface(tree, ListBoxRenderTests.Surface.GlassPanel), theme,
            beforeShow: w => w.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true, Density = density }),
            afterShow: w =>
            {
                w.UpdateLayout();
                var items = Items(tree);
                items[1].IsExpanded = true;
                w.UpdateLayout();
                tree.SelectedItem = Items(tree).First(i => Equals(i.Header, "Shape"));
                ((IPseudoClasses)Chrome(Items(tree).First(i => Equals(i.Header, "Motion"))).Classes).Set(":pointerover", true);
            });
    }

    // --- Helpers -----------------------------------------------------------------------------

    // Style (expanded) > Colour, Shape > (Squircle, Radii), Motion; Blocks > Button.
    private static TreeView NewTree() => new()
    {
        Items =
        {
            new TreeViewItem
            {
                Header = "Style",
                IsExpanded = true,
                Items =
                {
                    new TreeViewItem { Header = "Shape", Items = { new TreeViewItem { Header = "Squircle" }, new TreeViewItem { Header = "Radii" } } },
                    new TreeViewItem { Header = "Colour" },
                    new TreeViewItem { Header = "Motion" },
                },
            },
            new TreeViewItem { Header = "Blocks", Items = { new TreeViewItem { Header = "Button" } } },
        },
    };

    private static (Window Window, TreeView Tree) Show(DensityStyle density = DensityStyle.Normal, bool reduceMotion = true)
    {
        var tree = NewTree();
        tree.Width = 260;
        var window = new Window { Width = 400, Height = 400, Content = tree };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = reduceMotion, Density = density });
        window.Show();
        Pump(window);
        // The grandchildren need their parent realised: expand and collapse once.
        var shape = (TreeViewItem)((TreeViewItem)tree.Items[0]!).Items[0]!;
        shape.IsExpanded = true;
        Pump(window);
        shape.IsExpanded = false;
        Pump(window);
        return (window, tree);
    }

    // Root, Shape, Colour, Squircle (Shape's first child), Radii, Motion, Blocks, Button: the realised items in an order the tests index.
    private static List<TreeViewItem> Items(TreeView tree)
    {
        var style = (TreeViewItem)tree.Items[0]!;
        var shape = (TreeViewItem)style.Items[0]!;
        return [style, shape, (TreeViewItem)style.Items[1]!, (TreeViewItem)shape.Items[0]!, (TreeViewItem)shape.Items[1]!,
            (TreeViewItem)style.Items[2]!, (TreeViewItem)tree.Items[1]!];
    }

    private static SquircleBorder Chrome(TreeViewItem item) => Part<SquircleBorder>(item, ButtonParts.ChromePart);

    private static ToggleButton Caret(TreeViewItem item) => Part<ToggleButton>(item, TreeViewTheme.CaretPart);

    private static ContentPresenter Header(TreeViewItem item) => Part<ContentPresenter>(item, TreeViewTheme.HeaderPresenterPart);

    private static ItemsPresenter Children(TreeViewItem item) => Part<ItemsPresenter>(item, TreeViewTheme.ItemsPart);

    private static Color Fill(TreeViewItem item) => ((ISolidColorBrush?)Chrome(item).Background)?.Color ?? Colors.Transparent;

    // The item's own template part (the first match is the item's own: its children's parts come after).
    private static T Part<T>(TreeViewItem item, string name) where T : Control =>
        item.GetVisualDescendants().OfType<T>().First(c => c.Name == name && c.TemplatedParent == item);

    private static Matrix Rounded(Matrix m) => new(Math.Round(m.M11, 6), Math.Round(m.M12, 6), Math.Round(m.M21, 6), Math.Round(m.M22, 6), 0, 0);

    private static void Press(Window window, Key key)
    {
        var physical = key switch { Key.Up => PhysicalKey.ArrowUp, Key.Down => PhysicalKey.ArrowDown, Key.Left => PhysicalKey.ArrowLeft, _ => PhysicalKey.ArrowRight };
        window.KeyPress(key, RawInputModifiers.None, physical, null);
        Pump(window);
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
