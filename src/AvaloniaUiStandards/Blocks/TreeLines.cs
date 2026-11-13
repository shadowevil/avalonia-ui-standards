using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The node lines of one tree item (<see cref="TreeViewOptions.ShowLinesProperty"/>), drawn under
/// its row and its children's rows, over the item's whole height, beside the content pills. An
/// expanded item with lines on draws a stub down from below its caret to the bottom of its row,
/// when at least one of its children has lines on. An item with lines on whose parent item has them
/// on draws its elbow: down its parent's caret column from its top, a rounded elbow into its row's
/// centre, and a level line ending before its caret (or before its pill on a leaf). Unless no later
/// sibling has lines on, the parent's column carries on to the item's bottom, past its own
/// children, whenever the parent has lines on. So the stub and the elbows of consecutive children
/// join into one line per expanded parent, ending at the last child that shows its elbow. The
/// parent redraws its children when its own item changes which of them is last. Lines are 1 px in
/// the border colour, snapped to device pixels.
/// </summary>
internal sealed class TreeLines : Control
{
    public const string Part = "PART_Lines";

    /// <summary>The line thickness, as the Separator's hairline.</summary>
    public const double Thickness = SeparatorTheme.Thickness;

    /// <summary>The elbow's radius.</summary>
    public const double Radius = Spacing.XS;

    /// <summary>The space between a level line's end and the caret glyph or pill.</summary>
    public const double Gap = Spacing.XS;

    /// <summary>The caret glyph's left edge in its box, pointing right or turned down (72 of the 256 design box).</summary>
    public const double CaretGlyphInset = 72.0 / 256;

    // The cubic control distance that draws a quarter circle.
    private const double Kappa = 0.5523;

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<TreeLines, IBrush?>(nameof(Stroke));

    // Each item's lines layer, so a parent can redraw the children whose last-child status changes.
    private static readonly ConditionalWeakTable<TreeViewItem, TreeLines> Layers = new();

    private readonly TreeViewItem _item;
    private readonly Control _row;
    private readonly Control _caret;
    private readonly Control _content;
    private TreeViewItem? _parent;
    private TopLevel? _topLevel;

    static TreeLines()
    {
        AffectsRender<TreeLines>(StrokeProperty);
    }

    public TreeLines(TreeViewItem item, Control row, Control caret, Control content)
    {
        (_item, _row, _caret, _content) = (item, row, caret, content);
        Name = Part;
        IsHitTestVisible = false;
        Layers.AddOrUpdate(item, this);
        foreach (var part in new[] { row, caret, content })
            part.PropertyChanged += OnPartChanged;
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>How many times this layer asked to be redrawn (for tests of how far a change spreads).</summary>
    internal int Redraws { get; private set; }

    private bool IsOn => GetValue(TreeViewOptions.ShowLinesProperty);

    private bool ParentIsOn => _parent is { } parent && parent.GetValue(TreeViewOptions.ShowLinesProperty);

    /// <summary>Whether this item's stub shows: lines on, expanded, with at least one child that has lines on.</summary>
    internal bool HasStub => IsOn && _item.IsExpanded && Enumerable.Range(0, _item.ItemCount).Any(i => ChildIsOn(_item, i));

    /// <summary>Whether this item's elbow shows: lines on for it and for its parent item.</summary>
    internal bool HasElbow => IsOn && ParentIsOn;

    /// <summary>Whether the parent's column carries on past this item (a later sibling has lines on).</summary>
    internal bool Continues
    {
        get
        {
            if (_parent is not { } parent)
                return false;
            for (var i = parent.IndexFromContainer(_item) + 1; i < parent.ItemCount; i++)
                if (ChildIsOn(parent, i))
                    return true;
            return false;
        }
    }

    // A child's lines value; an item without a container yet takes its parent's, which it inherits.
    private static bool ChildIsOn(TreeViewItem parent, int index) =>
        (parent.ContainerFromIndex(index) as TreeViewItem ?? parent).GetValue(TreeViewOptions.ShowLinesProperty);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _item.PropertyChanged += OnItemChanged;
        _item.Items.CollectionChanged += OnChildrenChanged;
        _parent = ItemsControl.ItemsControlFromItemContainer(_item) as TreeViewItem;
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null)
            _topLevel.ScalingChanged += OnScalingChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _item.PropertyChanged -= OnItemChanged;
        _item.Items.CollectionChanged -= OnChildrenChanged;
        if (_topLevel is not null)
            _topLevel.ScalingChanged -= OnScalingChanged;
        _topLevel = null;
        _parent = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TreeViewOptions.ShowLinesProperty)
        {
            Redraw();
            // The children's columns and elbows follow this item's value, even where a child overrides its own.
            for (var i = 0; i < _item.ItemCount; i++)
                ChildLayer(i)?.Redraw();
            // The parent's stub and the earlier siblings' columns depend on which children have lines on.
            if (_parent is { } parent && Layers.TryGetValue(parent, out var parentLayer))
            {
                parentLayer.Redraw();
                for (var i = 0; i < parent.ItemCount; i++)
                    parentLayer.ChildLayer(i)?.Redraw();
            }
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        Redraw();
    }

    private void Redraw()
    {
        Redraws++;
        InvalidateVisual();
    }

    private TreeLines? ChildLayer(int index) =>
        index >= 0 && _item.ContainerFromIndex(index) is TreeViewItem child && Layers.TryGetValue(child, out var layer) ? layer : null;

    private void OnItemChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TreeViewItem.IsExpandedProperty || e.Property == ItemsControl.ItemCountProperty
            || e.Property == TreeViewItem.LevelProperty || e.Property == TemplatedControl.PaddingProperty)
            Redraw();
    }

    private void OnPartChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
            Redraw();
    }

    // An add, remove, move or replace can change which child is last: redraw the last two children and the
    // ones at the change, once the containers follow the change (after layout).
    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!IsOn)
            return;
        var (oldAt, newAt) = (e.OldStartingIndex, e.NewStartingIndex);
        Dispatcher.UIThread.Post(() =>
        {
            var count = _item.ItemCount;
            foreach (var index in new[] { count - 1, count - 2, oldAt, newAt }.Distinct())
                ChildLayer(index)?.Redraw();
        }, DispatcherPriority.Loaded);
    }

    private void OnScalingChanged(object? sender, EventArgs e) => Redraw();

    public override void Render(DrawingContext context)
    {
        // The parent's column passes through this item whenever the parent has lines on, even if this item has them off.
        var (stub, elbow) = (HasStub, HasElbow);
        var column = ParentIsOn && Continues;
        if (!stub && !elbow && !column || Stroke is not { } stroke)
            return;

        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        // The line covers whole device pixels: its thickness rounded to them, its centre placed so both edges land on them.
        var devices = Math.Max(1, Math.Round(Thickness * scale));
        double Line(double v) => (Math.Floor(v * scale - devices / 2 + 0.5) + devices / 2) / scale;
        double Edge(double v) => Math.Round(v * scale) / scale;

        // The caret's box: its centre is unmoved by its turn, so its translated centre is the layout centre.
        var size = _caret.Bounds.Size;
        if (_caret.TranslatePoint(new Point(size.Width / 2, size.Height / 2), this) is not { } centre
            || _row.TranslatePoint(new Point(0, _row.Bounds.Height), this) is not { } rowBottom)
            return;
        var indent = size.Width;

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            if (stub)
            {
                var own = Line(centre.X);
                var (top, bottom) = (Edge(centre.Y + size.Height / 2), Edge(rowBottom.Y));
                if (bottom > top)
                {
                    g.BeginFigure(new Point(own, top), false);
                    g.LineTo(new Point(own, bottom));
                    g.EndFigure(false);
                }
            }
            var x = Line(centre.X - indent);
            if (column)
            {
                g.BeginFigure(new Point(x, 0), false);
                g.LineTo(new Point(x, Edge(Bounds.Height)));
                g.EndFigure(false);
            }
            if (elbow)
            {
                var y = Line(centre.Y);
                var caretLeft = centre.X - size.Width / 2;
                var end = Edge(_item.ItemCount > 0
                    ? caretLeft + indent * CaretGlyphInset - Gap
                    : (_content.TranslatePoint(default, this)?.X ?? caretLeft + indent) - Gap);
                g.BeginFigure(new Point(x, 0), false);
                g.LineTo(new Point(x, y - Radius));
                g.CubicBezierTo(new Point(x, y - Radius * (1 - Kappa)), new Point(x + Radius * (1 - Kappa), y), new Point(x + Radius, y));
                g.LineTo(new Point(Math.Max(end, x + Radius), y));
                g.EndFigure(false);
            }
        }
        context.DrawGeometry(null, new Pen(stroke, devices / scale), geometry);
    }
}
