using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
    public void Theme_GivesFullWidthRows_WithAPillFromTheContentToTheRightEdge()
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
            // Every row, at any level, is inset 4 from the tree's edges, the tree's full width. Its pill
            // is a squircle from the end of the caret to the row's right edge, so the right edges align.
            foreach (var item in new[] { root, child })
            {
                var row = Row(item);
                Assert.Equal(4, row.TranslatePoint(default, tree)!.Value.X);
                Assert.Equal(tree.Bounds.Width - 8, row.Bounds.Width);
                var chrome = Chrome(item);
                var caret = Caret(item);
                var caretRight = ((Visual)caret.GetVisualParent()!).TranslatePoint(default, tree)!.Value.X + caret.Bounds.Right;
                Assert.Equal(new CornerRadius(8), chrome.CornerRadius);
                Assert.Equal(caretRight, chrome.TranslatePoint(default, tree)!.Value.X, 1);
                Assert.Equal(tree.Bounds.Width - 4, chrome.TranslatePoint(default, tree)!.Value.X + chrome.Bounds.Width, 1);
                Assert.Equal(32, chrome.Bounds.Height);
                Assert.Equal(32, row.Bounds.Height);
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
    public void EnterAndDoubleClick_ToggleAnItem()
    {
        var (window, tree) = Show();
        try
        {
            var shape = Items(tree)[1];
            tree.SelectedItem = shape;
            shape.Focus(NavigationMethod.Tab);
            Pump(window);
            Assert.False(shape.IsExpanded);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            Pump(window);
            Assert.True(shape.IsExpanded);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            Pump(window);
            Assert.False(shape.IsExpanded);

            // A double-click on the header (not the caret) toggles too.
            var header = Header(shape);
            var point = header.TranslatePoint(new Point(10, header.Bounds.Height / 2), window)!.Value;
            for (var i = 0; i < 2; i++)
            {
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);
            }
            Pump(window);
            Assert.True(shape.IsExpanded);
        }
        finally
        {
            window.Close();
        }
    }

    public static TheoryData<TreeViewClickAction, TreeViewClickAction> ClickModes()
    {
        TreeViewClickAction[] all = [TreeViewClickAction.None, TreeViewClickAction.SingleClick, TreeViewClickAction.DoubleClick, Both];
        var data = new TheoryData<TreeViewClickAction, TreeViewClickAction>();
        foreach (var expand in all)
            foreach (var collapse in all)
                data.Add(expand, collapse);
        return data;
    }

    private const TreeViewClickAction Both = TreeViewClickAction.SingleClick | TreeViewClickAction.DoubleClick;

    [AvaloniaFact]
    public void ClickModes_DefaultToDoubleClick()
    {
        var tree = new TreeView();
        Assert.Equal(TreeViewClickAction.DoubleClick, TreeViewOptions.GetExpandOn(tree));
        Assert.Equal(TreeViewClickAction.DoubleClick, TreeViewOptions.GetCollapseOn(tree));
    }

    [AvaloniaTheory]
    [MemberData(nameof(ClickModes))]
    public void RowClicks_ExpandAndCollapse_AsSet_ToggleOncePerGesture(TreeViewClickAction expandOn, TreeViewClickAction collapseOn)
    {
        var (window, tree) = Show(configure: t =>
        {
            TreeViewOptions.SetExpandOn(t, expandOn);
            TreeViewOptions.SetCollapseOn(t, collapseOn);
        });
        try
        {
            var shape = Items(tree)[1];
            // Points on the indent outside the pill, on the header text and on the empty pill beyond it, apart
            // from each other, so a click never continues the click sequence of the one before.
            var points = new[] { RowPoint(window, shape, -10), RowPoint(window, shape, 32), RowPoint(window, shape, 44), RowPoint(window, shape, 150), RowPoint(window, shape, 180) };
            var next = 0;
            Point Fresh() => points[next++ % points.Length];

            foreach (var startExpanded in new[] { false, true, false, true })
            {
                var actions = startExpanded ? collapseOn : expandOn;

                // A single click.
                shape.IsExpanded = startExpanded;
                Pump(window);
                Click(window, Fresh(), 1);
                Assert.Equal(startExpanded ^ actions.HasFlag(TreeViewClickAction.SingleClick), shape.IsExpanded);
                Assert.Same(shape, tree.SelectedItem);

                // A double click toggles once, whether its first click or the double click does it.
                shape.IsExpanded = startExpanded;
                Pump(window);
                Click(window, Fresh(), 2);
                Assert.Equal(startExpanded ^ (actions != TreeViewClickAction.None), shape.IsExpanded);
                Assert.Same(shape, tree.SelectedItem);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RowClicks_SingleExpandDoubleCollapse_ADoubleClickOnACollapsedRowLeavesItExpanded()
    {
        var (window, tree) = Show(configure: t => TreeViewOptions.SetExpandOn(t, TreeViewClickAction.SingleClick));
        try
        {
            var shape = Items(tree)[1];
            Click(window, RowPoint(window, shape, 30), 2);
            Assert.True(shape.IsExpanded);
            Click(window, RowPoint(window, shape, 120), 2);
            Assert.False(shape.IsExpanded);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(TreeViewClickAction.None)]
    [InlineData(TreeViewClickAction.DoubleClick)]
    [InlineData(Both)]
    public void Caret_TogglesOnEveryClick_WhateverTheSetting(TreeViewClickAction mode)
    {
        var (window, tree) = Show(configure: t =>
        {
            TreeViewOptions.SetExpandOn(t, mode);
            TreeViewOptions.SetCollapseOn(t, mode);
        });
        try
        {
            var shape = Items(tree)[1];
            var caret = Caret(shape);
            var point = caret.TranslatePoint(new Point(caret.Bounds.Width / 2, caret.Bounds.Height / 2), window)!.Value;
            Click(window, point, 1);
            Assert.True(shape.IsExpanded);
            // A click on another row ends the click sequence on the caret.
            Click(window, RowPoint(window, Items(tree)[2], 30), 1);
            // A double click on the caret is two clicks: it toggles twice and the row's double click does nothing.
            Click(window, point, 2);
            Assert.True(shape.IsExpanded);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RowClicks_IgnoreLeaves_AndLeaveTheirDoubleClickToTheApp()
    {
        var (window, tree) = Show(configure: t =>
        {
            TreeViewOptions.SetExpandOn(t, Both);
            TreeViewOptions.SetCollapseOn(t, Both);
        });
        try
        {
            var leaf = Items(tree)[2];
            var seen = 0;
            tree.DoubleTapped += (_, _) => seen++;
            Click(window, RowPoint(window, leaf, 30), 2);
            Assert.False(leaf.IsExpanded);
            Assert.Same(leaf, tree.SelectedItem);
            Assert.Equal(1, seen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RowClicks_IgnoreClicksTheContentHandles()
    {
        var button = new Button { Content = "Go" };
        var item = new TreeViewItem { Header = button, ItemsSource = new[] { "Child" } };
        var tree = new TreeView { Width = 260, ItemsSource = new[] { item } };
        TreeViewOptions.SetExpandOn(tree, Both);
        TreeViewOptions.SetCollapseOn(tree, Both);
        var window = new Window { Width = 400, Height = 400, Content = tree };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        window.Show();
        Pump(window);
        try
        {
            var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            Click(window, point, 1);
            Assert.False(item.IsExpanded);
            Click(window, point, 2);
            Assert.False(item.IsExpanded);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RowClicks_IgnoreClicksExtendingASelection()
    {
        var (window, tree) = Show(configure: t =>
        {
            t.SelectionMode = SelectionMode.Multiple;
            TreeViewOptions.SetExpandOn(t, TreeViewClickAction.SingleClick);
        });
        try
        {
            var shape = Items(tree)[1];
            var point = RowPoint(window, shape, 30);
            window.MouseDown(point, MouseButton.Left, RawInputModifiers.Control);
            window.MouseUp(point, MouseButton.Left, RawInputModifiers.Control);
            Pump(window);
            Assert.False(shape.IsExpanded);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ClickModes_AreInherited_AndAnItemOverridesThemForItsSubtree()
    {
        var (window, tree) = Show(configure: t => TreeViewOptions.SetExpandOn(t, TreeViewClickAction.SingleClick));
        try
        {
            var items = Items(tree);
            var (shape, blocks) = (items[1], items[6]);
            Assert.Equal(TreeViewClickAction.SingleClick, TreeViewOptions.GetExpandOn(shape));
            TreeViewOptions.SetExpandOn(blocks, TreeViewClickAction.None);
            Click(window, RowPoint(window, blocks, 30), 1);
            Assert.False(blocks.IsExpanded);
            Click(window, RowPoint(window, shape, 30), 1);
            Assert.True(shape.IsExpanded);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Keyboard_IsUnchanged_WithRowClicksOff()
    {
        var (window, tree) = Show(configure: t =>
        {
            TreeViewOptions.SetExpandOn(t, TreeViewClickAction.None);
            TreeViewOptions.SetCollapseOn(t, TreeViewClickAction.None);
        });
        try
        {
            var shape = Items(tree)[1];
            tree.SelectedItem = shape;
            shape.Focus(NavigationMethod.Tab);
            Pump(window);
            Press(window, Key.Right);
            Assert.True(shape.IsExpanded);
            Press(window, Key.Left);
            Assert.False(shape.IsExpanded);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            Pump(window);
            Assert.True(shape.IsExpanded);
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
            // Hovering the row outside the pill (its indent) lights the pill.
            window.MouseMove(Row(child).TranslatePoint(new Point(6, 16), window)!.Value);
            Pump(window);
            Assert.Contains(":pointerover", Row(child).Classes);
            Assert.NotEqual(Colors.Transparent, Fill(child));

            var point = Chrome(child).TranslatePoint(new Point(60, 16), window)!.Value;
            window.MouseMove(point);
            Pump(window);
            Assert.Contains(":pointerover", Row(child).Classes);
            Assert.Equal(Colors.Transparent, Fill(items[0]));
            Assert.NotEqual(Colors.Transparent, Fill(child));

            window.MouseDown(point, MouseButton.Left);
            Pump(window);
            Assert.Contains(":pressed", Row(child).Classes);
            Assert.DoesNotContain(":pressed", Row(items[0]).Classes);
            window.MouseUp(point, MouseButton.Left);
            Pump(window);
            Assert.DoesNotContain(":pressed", Row(child).Classes);
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

    // --- Node lines --------------------------------------------------------------------------

    [AvaloniaFact]
    public void Lines_AreOnByDefault_AndDrawnOnlyWhenOn()
    {
        Assert.True(TreeViewOptions.GetShowLines(new TreeView()));
        foreach (var on in new[] { false, true })
        {
            var (window, tree) = ShowLines(1, on);
            try
            {
                var items = Items(tree);
                Assert.Equal(on, Lines(items[0]).HasStub);
                Assert.Equal(on, Lines(items[2]).HasBranch);
                using var frame = Frame.Capture(window);
                // Style's column through the Colour row.
                Assert.Equal(on ? 1 : 0, LinePixels(frame, Column(window, items[0]), RowY(window, items[2], 3), 1).Count);
            }
            finally
            {
                window.Close();
            }
        }
    }

    [AvaloniaFact]
    public void Lines_FollowExpandAndCollapse()
    {
        var (window, tree) = ShowLines(1, true);
        try
        {
            var items = Items(tree);
            var (style, shape, squircle) = (items[0], items[1], items[3]);
            // Shape is expanded: its stub shows, below its caret.
            Assert.True(Lines(shape).HasStub);
            using (var frame = Frame.Capture(window))
                Assert.Single(LinePixels(frame, Column(window, shape), RowY(window, shape, -3), 1));

            shape.IsExpanded = false;
            Pump(window);
            Assert.False(Lines(shape).HasStub);
            Assert.False(squircle.IsEffectivelyVisible);
            using (var frame = Frame.Capture(window))
                Assert.Empty(LinePixels(frame, Column(window, shape), RowY(window, shape, -3), 1));

            // A collapsed root has no lines below it.
            style.IsExpanded = false;
            Pump(window);
            using (var frame = Frame.Capture(window))
                Assert.Empty(LinePixels(frame, Column(window, style), RowY(window, style, -3), 1));

            style.IsExpanded = true;
            shape.IsExpanded = true;
            Pump(window);
            Assert.True(Lines(shape).HasStub);
            using (var frame = Frame.Capture(window))
                Assert.Single(LinePixels(frame, Column(window, shape), RowY(window, shape, -3), 1));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Lines_LastChildEndsWithItsElbow_OthersContinue()
    {
        var (window, tree) = ShowLines(1, true);
        try
        {
            var items = Items(tree);
            var (style, shape, colour, squircle, radii, motion) = (items[0], items[1], items[2], items[3], items[4], items[5]);
            Assert.True(Lines(colour).Continues);
            Assert.False(Lines(motion).Continues);
            Assert.False(Lines(radii).Continues);
            using var frame = Frame.Capture(window);
            // Below the elbow, Style's column carries on through Colour (not last), not through Motion (last).
            Assert.Single(LinePixels(frame, Column(window, style), RowY(window, colour, -3), 1));
            Assert.Empty(LinePixels(frame, Column(window, style), RowY(window, motion, -3), 1));
            // Above the elbow the last child still joins the column.
            Assert.Single(LinePixels(frame, Column(window, style), RowY(window, motion, 3), 1));
            // Shape's column ends at Radii (its last child); Style's passes through Radii's row (Shape is not last).
            Assert.Single(LinePixels(frame, Column(window, shape), RowY(window, squircle, -3), 1));
            Assert.Empty(LinePixels(frame, Column(window, shape), RowY(window, radii, -3), 1));
            Assert.Single(LinePixels(frame, Column(window, style), RowY(window, radii, -3), 1));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(1, "Normal")]
    [InlineData(2, "Normal")]
    [InlineData(1, "Compact")]
    [InlineData(2, "Compact")]
    public void Lines_AreCrispHairlines_OnTheCaretColumn(double scale, string density)
    {
        var (window, tree) = ShowLines(scale, true, density == "Compact" ? DensityStyle.Compact : DensityStyle.Normal);
        try
        {
            var items = Items(tree);
            using var frame = Frame.Capture(window);
            foreach (var (parent, child) in new[] { (items[0], items[2]), (items[1], items[3]) })
            {
                var column = Column(window, parent);
                var y = RowY(window, child, 3);
                var pixels = LinePixels(frame, column, y, scale);
                // One logical pixel, whole device pixels, all of one colour (no anti-aliased fringe).
                Assert.Equal((int)scale, pixels.Count);
                Assert.Single(pixels.Select(p => frame[p, (int)(y * scale)]).Distinct());
                // Within half a logical pixel of the caret's centre.
                Assert.InRange((pixels.Average() + 0.5) / scale, column - 0.5, column + 0.5);
                // The elbow runs level at the child row's centre, one logical pixel thick.
                var centre = (int)Math.Round((Chrome(child).TranslatePoint(default, window)!.Value.Y + Chrome(child).Bounds.Height / 2) * scale);
                var x = (int)Math.Round((column + 1.5 * TreeLines.Radius) * scale);
                var rows = Enumerable.Range(centre - 4, 8).Where(r => frame[x, r] != frame[x, centre - 8]).ToList();
                Assert.Equal((int)scale, rows.Count);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Lines_StayVisibleBesideSelectedAndHoveredRows()
    {
        var (window, tree) = ShowLines(1, true);
        try
        {
            var items = Items(tree);
            tree.SelectedItem = items[2];
            ((IPseudoClasses)Row(items[3]).Classes).Set(":pointerover", true);
            Pump(window);
            using var frame = Frame.Capture(window);
            Assert.Single(LinePixels(frame, Column(window, items[0]), RowY(window, items[2], 3), 1));
            Assert.Single(LinePixels(frame, Column(window, items[1]), RowY(window, items[3], 3), 1));
            // The elbows end before the pills.
            Assert.True(Chrome(items[2]).TranslatePoint(default, window)!.Value.X > Column(window, items[0]) + TreeLines.Radius + TreeLines.Gap);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Lines_ParentsColumnPassesThroughAnItemWithLinesOff()
    {
        var (window, tree) = ShowLines(1, true);
        try
        {
            var items = Items(tree);
            TreeViewOptions.SetShowLines(items[1], false);
            Pump(window);
            Assert.False(Lines(items[1]).HasBranch);
            Assert.False(Lines(items[1]).HasStub);
            using var frame = Frame.Capture(window);
            // Style's column inside Squircle's row (Shape's child), and below Shape's elbow.
            Assert.Single(LinePixels(frame, Column(window, items[0]), RowY(window, items[3], 3), 1));
            Assert.Single(LinePixels(frame, Column(window, items[0]), RowY(window, items[1], -3), 1));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Lines_AddingChildren_RedrawsOnlyTheChangedSiblings()
    {
        var children = new System.Collections.ObjectModel.ObservableCollection<string> { "0" };
        var parent = new TreeViewItem { Header = "Parent", IsExpanded = true, ItemsSource = children };
        var tree = new TreeView { Width = 260, ItemsSource = new[] { parent } };
        var window = new Window { Width = 400, Height = 400, Content = tree };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        window.Show();
        Pump(window);
        try
        {
            TreeLines Layer(int i) => Lines((TreeViewItem)parent.ContainerFromIndex(i)!);
            var first = Layer(0).Redraws;
            const int added = 200;
            for (var i = 1; i <= added; i++)
            {
                children.Add(i.ToString());
                Pump(window);
            }
            // The first child is redrawn only when it stops being last; every child a bounded number of times.
            Assert.InRange(Layer(0).Redraws - first, 1, 3);
            var total = Enumerable.Range(0, added + 1).Sum(i => Layer(i).Redraws);
            Assert.InRange(total, added, 12 * added);
            Assert.True(Lines((TreeViewItem)parent.ContainerFromIndex(added - 1)!).Continues);
            Assert.False(Layer(added).Continues);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Lines_FollowAddedAndRemovedItems()
    {
        var children = new System.Collections.ObjectModel.ObservableCollection<string> { "One", "Two" };
        var parent = new TreeViewItem { Header = "Parent", IsExpanded = true, ItemsSource = children };
        var tree = new TreeView { Width = 260, ItemsSource = new[] { parent } };
        var window = new Window { Width = 400, Height = 400, Content = tree, Background = Brushes.White, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true });
        window.Show();
        Pump(window);
        try
        {
            TreeViewItem Child(int i) => (TreeViewItem)parent.ContainerFromIndex(i)!;
            bool Below(int i)
            {
                using var frame = Frame.Capture(window);
                return LinePixels(frame, Column(window, parent), RowY(window, Child(i), -3), 1).Count == 1;
            }
            Assert.True(Lines(Child(0)).Continues);
            Assert.False(Lines(Child(1)).Continues);
            Assert.True(Below(0));
            Assert.False(Below(1));

            children.Add("Three");
            Pump(window);
            Assert.True(Below(1));
            Assert.False(Below(2));

            children.RemoveAt(2);
            Pump(window);
            Assert.False(Below(1));

            // A move changes which child is last without changing the count.
            children.Move(1, 0);
            Pump(window);
            Assert.Equal("One", Child(1).Header);
            Assert.True(Below(0));
            Assert.False(Below(1));
            children[1] = "Replaced";
            Pump(window);
            Assert.False(Below(1));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Lines_SetOnAnItem_DrawThatItemsConnectionsToItsChildren()
    {
        var (window, tree) = ShowLines(1, false);
        try
        {
            var items = Items(tree);
            TreeViewOptions.SetShowLines(items[1], true);
            Pump(window);
            Assert.False(Lines(items[0]).HasStub);
            Assert.False(Lines(items[1]).HasBranch);
            Assert.True(Lines(items[1]).HasStub);
            Assert.True(Lines(items[3]).HasBranch);
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
        using var sheet = new Sheet("TreeView: content pills, a caret that turns down, one caret width of indent per level, node lines");
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

    [AvaloniaFact]
    public void TreeView_Lines_Render()
    {
        using var sheet = new Sheet("TreeView node lines, off and on: Colour selected, Squircle hovered, Blocks collapsed, Motion and Radii last children");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            sheet.AddSection(themeLabel, ["Lines off", "Lines on"],
            [
                ("Normal", [LinesCell(sheet, theme, DensityStyle.Normal, false), LinesCell(sheet, theme, DensityStyle.Normal, true)]),
                ("Compact", [LinesCell(sheet, theme, DensityStyle.Compact, false), LinesCell(sheet, theme, DensityStyle.Compact, true)]),
            ]);
        }
        sheet.Save("m3-treeview-lines.png");
    }

    [AvaloniaFact]
    public void TreeView_Pill_Render()
    {
        using var sheet = new Sheet("TreeView pill, lines on: Colour selected (level 1), Squircle hovered (level 2); the pill to the row's right edge (built) or hugging the label");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            sheet.AddSection(themeLabel, ["To the right edge (built)", "Hugging the label", "Right edge, keyboard focus", "Hugging, keyboard focus"],
            [.. new[] { ("Normal", DensityStyle.Normal), ("Compact", DensityStyle.Compact) }.Select(d => (d.Item1, (IReadOnlyList<Control>)
            [
                LinesCell(sheet, theme, d.Item2, true),
                LinesCell(sheet, theme, d.Item2, true, hug: true),
                LinesCell(sheet, theme, d.Item2, true, focus: true),
                LinesCell(sheet, theme, d.Item2, true, hug: true, focus: true),
            ]))]);
        }
        sheet.Save("m3-treeview-pill.png");
    }

    // The test tree on the glass panel at 2x, Shape expanded, Colour selected and Squircle hovered (or Colour focused).
    // hug: each pill as wide as its content and padding, instead of reaching the row's right edge.
    private static Control LinesCell(Sheet sheet, ThemeVariant theme, DensityStyle density, bool lines, bool hug = false, bool focus = false)
    {
        using var capture = CaptureLines(theme, density, lines, hug, focus);
        return sheet.Image(capture);
    }

    private static WriteableBitmap CaptureLines(ThemeVariant theme, DensityStyle density, bool lines, bool hug, bool focus)
    {
        var tree = NewTree();
        tree.Width = 220;
        TreeViewOptions.SetShowLines(tree, lines);
        var surface = new Border { Padding = new Thickness(16, 12), Child = ListBoxRenderTests.OnSurface(tree, ListBoxRenderTests.Surface.GlassPanel) };
        surface.Bind(Border.BackgroundProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("SystemRegionColor"));
        var window = new Window { SizeToContent = SizeToContent.WidthAndHeight, RequestedThemeVariant = theme };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true, Density = density });
        window.Content = surface;
        window.SetRenderScaling(Sheet.Scale);
        window.Show();
        window.UpdateLayout();
        var items = Items(tree);
        items[1].IsExpanded = true;
        window.UpdateLayout();
        if (hug)
            foreach (var item in items)
                ((Control)Chrome(item).GetVisualParent()!).HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        tree.SelectedItem = items[2];
        if (focus)
            items[2].Focus(NavigationMethod.Tab);
        else
            ((IPseudoClasses)Row(items[3]).Classes).Set(":pointerover", true);
        Pump(window);
        var capture = window.CaptureRenderedFrame()!;
        window.Close();
        return capture;
    }

    private static Control StateCell(Sheet sheet, ThemeVariant theme, RowState state)
    {
        var item = new TreeViewItem { Header = "Documents", ItemsSource = new[] { "Notes" } };
        var tree = new TreeView { Width = 170, ItemsSource = new[] { item } };
        return sheet.Cell(ListBoxRenderTests.OnSurface(tree, ListBoxRenderTests.Surface.GlassPanel), theme, afterShow: w =>
        {
            w.UpdateLayout();
            var row = Row(item);
            if (state is RowState.Selected or RowState.SelectedHover)
                item.IsSelected = true;
            if (state is RowState.Hover or RowState.SelectedHover)
                ((IPseudoClasses)row.Classes).Set(":pointerover", true);
            if (state == RowState.Pressed)
                ((IPseudoClasses)row.Classes).Set(":pressed", true);
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
                ((IPseudoClasses)Row(Items(tree).First(i => Equals(i.Header, "Motion"))).Classes).Set(":pointerover", true);
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

    private static (Window Window, TreeView Tree) Show(DensityStyle density = DensityStyle.Normal, bool reduceMotion = true, Action<TreeView>? configure = null)
    {
        var tree = NewTree();
        tree.Width = 260;
        configure?.Invoke(tree);
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

    private static Border Row(TreeViewItem item) => Part<Border>(item, TreeViewTheme.RowPart);

    private static ToggleButton Caret(TreeViewItem item) => Part<ToggleButton>(item, TreeViewTheme.CaretPart);

    private static ContentPresenter Header(TreeViewItem item) => Part<ContentPresenter>(item, TreeViewTheme.HeaderContentPart);

    private static ItemsPresenter Children(TreeViewItem item) => Part<ItemsPresenter>(item, TreeViewTheme.ItemsPart);

    private static Color Fill(TreeViewItem item) => ((ISolidColorBrush?)Chrome(item).Background)?.Color ?? Colors.Transparent;

    // The item's own template part (the first match is the item's own: its children's parts come after).
    private static T Part<T>(TreeViewItem item, string name) where T : Control =>
        item.GetVisualDescendants().OfType<T>().First(c => c.Name == name && c.TemplatedParent == item);

    private static Matrix Rounded(Matrix m) => new(Math.Round(m.M11, 6), Math.Round(m.M12, 6), Math.Round(m.M21, 6), Math.Round(m.M22, 6), 0, 0);

    // A tree on a white window at the scale, Style and Shape expanded, lines on or off.
    private static (Window Window, TreeView Tree) ShowLines(double scale, bool lines, DensityStyle density = DensityStyle.Normal)
    {
        var tree = NewTree();
        tree.Width = 260;
        TreeViewOptions.SetShowLines(tree, lines);
        var window = new Window { Width = 400, Height = 400, Content = tree, Background = Brushes.White, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new StandardsTheme(OsAccessibility.Settings.NotReduced) { ReduceMotion = true, Density = density });
        window.SetRenderScaling(scale);
        window.Show();
        Pump(window);
        Items(tree)[1].IsExpanded = true;
        Pump(window);
        return (window, tree);
    }

    private static TreeLines Lines(TreeViewItem item) => Part<TreeLines>(item, TreeLines.Part);

    // The x of an item's caret centre (its children's column), in the window's logical pixels.
    private static double Column(Window window, TreeViewItem item)
    {
        var caret = Caret(item);
        return caret.TranslatePoint(new Point(caret.Bounds.Width / 2, caret.Bounds.Height / 2), window)!.Value.X;
    }

    // A y in an item's row: dy below its top, or -dy above its bottom when negative.
    private static double RowY(Window window, TreeViewItem item, double dy)
    {
        var chrome = Chrome(item);
        var top = chrome.TranslatePoint(default, window)!.Value.Y;
        return dy >= 0 ? top + dy : top + chrome.Bounds.Height + dy;
    }

    // The device pixels within 4 logical pixels of x, on the pixel row at y, that differ from the background at the left end.
    private static List<int> LinePixels(Frame frame, double x, double y, double scale)
    {
        var row = (int)(y * scale);
        var from = (int)((x - 4) * scale);
        var background = frame[from, row];
        return [.. Enumerable.Range(from, (int)(8 * scale)).Where(px => frame[px, row] != background)];
    }

    // A point on the row, x in from where its caret starts (the pill from 20 on, the indent before 0).
    private static Point RowPoint(Window window, TreeViewItem item, double x) =>
        Row(item).TranslatePoint(new Point(x + Row(item).Padding.Left + item.Level * TreeViewTheme.Normal.Indent, Row(item).Bounds.Height / 2), window)!.Value;

    // Clicks count times at point.
    private static void Click(Window window, Point point, int count)
    {
        window.MouseMove(point);
        for (var i = 0; i < count; i++)
        {
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
        }
        Pump(window);
    }

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
