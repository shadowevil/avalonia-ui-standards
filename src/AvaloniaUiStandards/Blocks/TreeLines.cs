using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The node lines of one tree item (<see cref="TreeViewOptions.ShowLinesProperty"/>), drawn under
/// its row and its children's rows, over the item's whole height, beside the content pills. An
/// expanded item with lines on draws a stub down from below its caret to the bottom of its row. An
/// item with lines on whose parent item has them on draws its branch: down its parent's caret column
/// from its top, a rounded elbow into its row's centre, and a level line ending before its caret (or
/// before its pill on a leaf); unless it is its parent's last child, the column carries on to its
/// bottom, past its own children. So the stub and the branches of consecutive children join into one
/// line per expanded parent. Lines are 1 px in the border colour, snapped to device pixels.
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

    private readonly TreeViewItem _item;
    private readonly Control _row;
    private readonly Control _caret;
    private readonly Control _content;
    private TreeViewItem? _parent;
    private TopLevel? _topLevel;
    private bool _watchingParent;

    static TreeLines()
    {
        AffectsRender<TreeLines>(StrokeProperty);
    }

    public TreeLines(TreeViewItem item, Control row, Control caret, Control content)
    {
        (_item, _row, _caret, _content) = (item, row, caret, content);
        Name = Part;
        IsHitTestVisible = false;
        foreach (var part in new[] { row, caret, content })
            part.PropertyChanged += OnPartChanged;
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    private bool IsOn => GetValue(TreeViewOptions.ShowLinesProperty);

    /// <summary>Whether this item's stub shows: lines on, expanded, with children.</summary>
    internal bool HasStub => IsOn && _item.IsExpanded && _item.ItemCount > 0;

    /// <summary>Whether this item's branch shows: lines on for it and for its parent item.</summary>
    internal bool HasBranch => IsOn && _parent is { } parent && parent.GetValue(TreeViewOptions.ShowLinesProperty);

    /// <summary>Whether the parent's column carries on past this item (it is not the last child).</summary>
    internal bool Continues => _parent is { } parent && parent.IndexFromContainer(_item) < parent.ItemCount - 1;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _item.PropertyChanged += OnItemChanged;
        _parent = ItemsControl.ItemsControlFromItemContainer(_item) as TreeViewItem;
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null)
            _topLevel.ScalingChanged += OnScalingChanged;
        WatchParent(IsOn);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _item.PropertyChanged -= OnItemChanged;
        if (_topLevel is not null)
            _topLevel.ScalingChanged -= OnScalingChanged;
        _topLevel = null;
        WatchParent(false);
        _parent = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TreeViewOptions.ShowLinesProperty)
        {
            if (VisualRoot is not null)
                WatchParent(IsOn);
            InvalidateVisual();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        InvalidateVisual();
    }

    // The parent's option and children decide the branch; they are followed only while lines are on here.
    private void WatchParent(bool watch)
    {
        if (watch == _watchingParent || _parent is null)
            return;
        _watchingParent = watch;
        if (watch)
        {
            _parent.PropertyChanged += OnParentChanged;
            _parent.Items.CollectionChanged += OnSiblingsChanged;
        }
        else
        {
            _parent.PropertyChanged -= OnParentChanged;
            _parent.Items.CollectionChanged -= OnSiblingsChanged;
        }
    }

    private void OnItemChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TreeViewItem.IsExpandedProperty || e.Property == ItemsControl.ItemCountProperty
            || e.Property == TreeViewItem.LevelProperty || e.Property == TemplatedControl.PaddingProperty)
            InvalidateVisual();
    }

    private void OnPartChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
            InvalidateVisual();
    }

    private void OnParentChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ItemsControl.ItemCountProperty || e.Property == TreeViewOptions.ShowLinesProperty)
            InvalidateVisual();
    }

    // An add, remove, move or replace can change which child is last.
    private void OnSiblingsChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    private void OnScalingChanged(object? sender, EventArgs e) => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        var (stub, branch) = (HasStub, HasBranch);
        if (!stub && !branch || Stroke is not { } stroke)
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
                var x = Line(centre.X);
                var (top, bottom) = (Edge(centre.Y + size.Height / 2), Edge(rowBottom.Y));
                if (bottom > top)
                {
                    g.BeginFigure(new Point(x, top), false);
                    g.LineTo(new Point(x, bottom));
                    g.EndFigure(false);
                }
            }
            if (branch)
            {
                var x = Line(centre.X - indent);
                var y = Line(centre.Y);
                var caretLeft = centre.X - size.Width / 2;
                var end = Edge(_item.ItemCount > 0
                    ? caretLeft + indent * CaretGlyphInset - Gap
                    : (_content.TranslatePoint(default, this)?.X ?? caretLeft + indent) - Gap);
                if (Continues)
                {
                    g.BeginFigure(new Point(x, 0), false);
                    g.LineTo(new Point(x, Edge(Bounds.Height)));
                    g.EndFigure(false);
                }
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
