using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaUiStandards.Style;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// The node lines of one tree item (<see cref="TreeViewOptions.ShowLinesProperty"/>), drawn under
/// its row and its children's rows, over the item's whole height. An expanded item with lines on
/// draws a stub down from below its caret to the bottom of its row. An item whose parent item has
/// lines on draws its branch: down its parent's caret column from its top, a rounded elbow into its
/// row's centre, and a level line ending before its caret (or before its header on a leaf); unless
/// it is its parent's last child, the column carries on to its bottom, past its own children. So
/// the stub and the branches of consecutive children join into one line per expanded parent.
/// Lines are 1 px in the border colour, snapped to device pixels.
/// </summary>
internal sealed class TreeLines : Control
{
    public const string Part = "PART_Lines";

    /// <summary>The line thickness, as the Separator's hairline.</summary>
    public const double Thickness = SeparatorTheme.Thickness;

    /// <summary>The elbow's radius.</summary>
    public const double Radius = Spacing.XS;

    /// <summary>The space between a level line's end and the caret glyph or header.</summary>
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
    private readonly Control _header;
    private TreeViewItem? _parent;

    static TreeLines()
    {
        AffectsRender<TreeLines>(StrokeProperty, TreeViewOptions.ShowLinesProperty);
    }

    public TreeLines(TreeViewItem item, Control row, Control caret, Control header)
    {
        (_item, _row, _caret, _header) = (item, row, caret, header);
        Name = Part;
        IsHitTestVisible = false;
        foreach (var part in new[] { row, caret, header })
            part.PropertyChanged += OnPartChanged;
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>Whether this item's stub shows: lines on, expanded, with children.</summary>
    internal bool HasStub => GetValue(TreeViewOptions.ShowLinesProperty) && _item.IsExpanded && _item.ItemCount > 0;

    /// <summary>Whether this item's branch shows: its parent item has lines on.</summary>
    internal bool HasBranch => _parent is { } parent && parent.GetValue(TreeViewOptions.ShowLinesProperty);

    /// <summary>Whether the parent's column carries on past this item (it is not the last child).</summary>
    internal bool Continues => _parent is { } parent && parent.IndexFromContainer(_item) < parent.ItemCount - 1;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _item.PropertyChanged += OnItemChanged;
        _parent = ItemsControl.ItemsControlFromItemContainer(_item) as TreeViewItem;
        if (_parent is not null)
            _parent.PropertyChanged += OnParentChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _item.PropertyChanged -= OnItemChanged;
        if (_parent is not null)
            _parent.PropertyChanged -= OnParentChanged;
        _parent = null;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        InvalidateVisual();
    }

    private void OnItemChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TreeViewItem.IsExpandedProperty || e.Property == ItemsControl.ItemCountProperty
            || e.Property == TreeViewItem.LevelProperty || e.Property == Avalonia.Controls.Primitives.TemplatedControl.PaddingProperty)
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
                    : (_header.TranslatePoint(default, this)?.X ?? caretLeft + indent) - Gap);
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
