using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Reactive;
using AvaloniaUiStandards.Modules;

namespace Catalog;

/// <summary>
/// Flowing, translucent ribbons for behind the catalog window's content, tinted from the theme's accent. Each
/// ribbon is a faint translucent band drawn with glowing lines (a bright core and a soft halo) along its edges
/// and, on the wider ones, along its middle. The ribbons stay in place and undulate: their waves sway, rise and
/// breathe, out of step with each other, and loop seamlessly every <see cref="LoopSeconds"/>. The light on the
/// lines varies smoothly along them, with dark gaps, and travels; small soft dots twinkle beside the lit edges.
/// </summary>
/// <remarks>
/// Each frame rebuilds the curves (about a hundred points per ribbon) and
/// draws them in one pass: the bands first, then the lines and dots. Every motion is a sine of a whole number
/// of cycles per loop, so position 1 looks exactly like position 0. A line's light is a gradient shader along
/// its length whose stops are set from the light's exact value at each point, so it is continuous in space and
/// in time. The loop advances on the hosting window's frame ticks (which pause under reduce motion and while the
/// window is inactive or minimised); with reduce motion the dots are also hidden. Outside <see cref="PanelBounds"/>, where text on the
/// backdrop sits, one scrim shape in the page colour tones everything down so the text stays legible; its inner
/// edge is under the panel's border.
/// </remarks>
internal sealed class RibbonBackdrop : Control
{
    /// <summary>Seconds for one seamless loop.</summary>
    public const double LoopSeconds = 45;

    internal const int RibbonCount = 4;

    private sealed record Ribbon(
        double Wavelength, double Centre, double Amplitude, int Waves, double Shift, int Waves2, double Shift2,
        double Thickness, double Swell, int Waves3, double Strength, int Tint, bool Crest, int Dots, double Slot);

    // Back to front. Centre, Amplitude and Thickness are fractions of the height; Wavelength is in px.
    private static readonly Ribbon[] Ribbons =
    [
        new(1800, 0.30, 0.10, 1, 0.4, 2, 1.7, 0.24, 0.07, 2, 0.8, 0, false, 28, 0.0),
        new(1400, 0.62, 0.09, 2, 2.1, 3, 0.3, 0.20, 0.06, 1, 0.9, 1, true, 36, 1.3),
        new(2200, 0.48, 0.12, 1, 4.0, 3, 2.6, 0.17, 0.06, 2, 1.0, 2, true, 40, 2.6),
        new(1600, 0.78, 0.07, 2, 1.1, 1, 5.2, 0.20, 0.07, 3, 0.8, 0, false, 24, 3.9),
    ];

    // Dots come in four sizes (radius, px) and a few brightness steps.
    private static readonly double[] DotSizes = [1.3, 2.3, 3.6, 5.0];
    private const int Levels = 16;

    // The light on a line is set at a stop every so many px along it.
    private const double StopSpacing = 20;
    private const double SampleStep = 10;

    // How much of the page colour covers the ribbons outside the panel. Measured over every pixel of the text areas at
    // 64 points of the loop, with every dot and line at full brightness. Light, whose bands and lines are deep enough to
    // show through the glass, reaches 5:1 at about 0.41 (without a scrim it is 1.78:1); it is set a little above that,
    // to 0.45 (5.48:1 measured). Dark reaches 5:1 at about 0.62 (5.06:1 measured); it is set to 0.64 (5.36:1 measured).
    private const double LightScrim = 0.45;
    private const double DarkScrim = 0.64;

    // The opacity of a ribbon band in light.
    private const double LightBandOpacity = 0.5;

    private sealed record Dot(double X, double Y, int Size, double Brightness, int Rate, double Offset, int Line, bool Flash, double Drift);

    private Color _accent = AvaloniaUiStandards.StandardsTheme.DefaultAccent;
    private Color _region = Colors.White;
    private Color _text = Colors.Black;
    private double _phase;
    private bool _reduced;
    private Size _built;
    private double[] _x = [];
    private double[][]? _centre;
    private double[][]? _half;
    private StreamGeometry[]? _band;
    private IBrush[]? _bandBrush;
    private StreamGeometry[][]? _lines;
    private double[][]? _across;
    private LinearGradientBrush[][]? _coreBrush;
    private LinearGradientBrush[][]? _haloBrush;
    private Pen[][]? _corePen;
    private Pen[][]? _haloPen;
    private Color _coreColour;
    private Color _haloColour;
    private bool _dark;
    private Dot[][]? _dots;
    private IBrush[][]? _dotBrushes;
    private Rect? _panel;
    private Geometry? _scrim;
    private IBrush? _scrimBrush;
    private StandardsWindow? _host;
    private readonly List<IDisposable> _watches = [];

    public RibbonBackdrop()
    {
        ClipToBounds = true;
        IsHitTestVisible = false;
    }

    /// <summary>Position in the loop, 0 to 1. Drawing is periodic in it, so 1 looks exactly like 0.</summary>
    public double Phase
    {
        get => _phase;
        set
        {
            _phase = value;
            InvalidateVisual();
        }
    }

    public static readonly StyledProperty<Rect> PanelBoundsProperty =
        AvaloniaProperty.Register<RibbonBackdrop, Rect>(nameof(PanelBounds));

    /// <summary>
    /// Where the window's content panel is, in this control's coordinates (<c>StandardsWindow.ContentPanelBounds</c>).
    /// Everything outside it is toned down by a scrim, so text on the backdrop stays legible; empty means none.
    /// </summary>
    public Rect PanelBounds
    {
        get => GetValue(PanelBoundsProperty);
        set => SetValue(PanelBoundsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PanelBoundsProperty)
        {
            // Shrunk a pixel, so the scrim overlaps under the panel's border.
            _panel = PanelBounds.Width > 0 && PanelBounds.Height > 0 ? PanelBounds.Inflate(-1) : null;
            _scrim = null;
            InvalidateVisual();
        }
    }

    /// <summary>True when the dots show and twinkle (they are hidden under reduce motion).</summary>
    public bool Twinkling => !_reduced;

    /// <summary>When false, animation frames do not move the loop; only <see cref="Phase"/> does (for tests).</summary>
    internal bool AutoAdvance { get; set; } = true;

    /// <summary>Draws every dot and line at full brightness (for contrast tests).</summary>
    internal bool FullGlints { get; set; }

    /// <summary>Leaves the dots out (for tests of the smoothness of the lines and bands, which the quick flashes would mask).</summary>
    internal bool HideDots { get; set; }

    /// <summary>Moves the loop forward by <paramref name="elapsed"/>.</summary>
    public void Advance(TimeSpan elapsed) => Phase = (Phase + elapsed.TotalSeconds / LoopSeconds) % 1;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _watches.Add(this.GetResourceObservable("SystemAccentColor").Subscribe(new AnonymousObserver<object?>(v => SetColour(v, c => _accent = c))));
        _watches.Add(this.GetResourceObservable("SystemRegionColor").Subscribe(new AnonymousObserver<object?>(v => SetColour(v, c => _region = c))));
        _watches.Add(this.GetResourceObservable("SystemBaseHighColor").Subscribe(new AnonymousObserver<object?>(v => SetColour(v, c => _text = c))));
        // Reduce motion makes every motion duration zero.
        _watches.Add(this.GetResourceObservable("MotionNormal").Subscribe(new AnonymousObserver<object?>(v =>
        {
            _reduced = v is TimeSpan t && t == TimeSpan.Zero;
            InvalidateVisual();
        })));

        // The window's frame ticks drive the loop: they run only while motion is not reduced and the window is
        // active and not minimised, so nothing here repeats those checks. Outside a StandardsWindow nothing moves it.
        _host = TopLevel.GetTopLevel(this) as StandardsWindow;
        if (_host is not null)
            _host.FrameTick += OnFrameTick;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        foreach (var watch in _watches)
            watch.Dispose();
        _watches.Clear();
        if (_host is not null)
            _host.FrameTick -= OnFrameTick;
        _host = null;
    }

    // One step per tick, by the time the window says has passed (zero on the first tick after a start or resume).
    private void OnFrameTick(object? sender, FrameTickEventArgs e)
    {
        // A long gap (a stall) must not jump the ribbons.
        if (AutoAdvance)
            Advance(TimeSpan.FromSeconds(Math.Min(e.Elapsed.TotalSeconds, 0.1)));
    }

    private void SetColour(object? value, Action<Color> set)
    {
        if (value is not Color colour)
            return;
        set(colour);
        _band = null;
        _scrimBrush = null;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
            return;
        if (_band is null || _built != Bounds.Size)
            Build(Bounds.Size);
        Update();

        // The bands, back to front.
        for (var r = 0; r < Ribbons.Length; r++)
            context.DrawGeometry(_bandBrush![r], null, _band![r]);

        // The lines, then the dots, ribbon by ribbon.
        for (var r = 0; r < Ribbons.Length; r++)
        {
            for (var l = 0; l < _lines![r].Length; l++)
            {
                // Only the top edge has the soft halo; every line has the bright core.
                if (_across![r][l] == -1)
                    context.DrawGeometry(null, _haloPen![r][l], _lines[r][l]);
                context.DrawGeometry(null, _corePen![r][l], _lines[r][l]);
            }

            if (!_reduced && !HideDots)
                DrawDots(context, r);
        }

        // A scrim in the page colour over everything outside the panel, where the text on the backdrop sits. Its
        // inner edge lies under the panel's own border, so no edge of it shows.
        if (_panel is { } panel && _scrimBrush is not null)
        {
            _scrim ??= BuildScrim(new Rect(Bounds.Size), panel);
            context.DrawGeometry(_scrimBrush, null, _scrim);
        }
    }

    // The whole backdrop less the panel's rounded rectangle: two figures filled with the even-odd rule.
    private static Geometry BuildScrim(Rect outer, Rect hole)
    {
        const double radius = 16;
        var geometry = new StreamGeometry();
        using var g = geometry.Open();
        g.SetFillRule(FillRule.EvenOdd);
        g.BeginFigure(outer.TopLeft, true);
        g.LineTo(outer.TopRight);
        g.LineTo(outer.BottomRight);
        g.LineTo(outer.BottomLeft);
        g.EndFigure(true);

        var size = new Size(radius, radius);
        g.BeginFigure(new Point(hole.Left + radius, hole.Top), true);
        g.LineTo(new Point(hole.Right - radius, hole.Top));
        g.ArcTo(new Point(hole.Right, hole.Top + radius), size, 0, false, SweepDirection.Clockwise);
        g.LineTo(new Point(hole.Right, hole.Bottom - radius));
        g.ArcTo(new Point(hole.Right - radius, hole.Bottom), size, 0, false, SweepDirection.Clockwise);
        g.LineTo(new Point(hole.Left + radius, hole.Bottom));
        g.ArcTo(new Point(hole.Left, hole.Bottom - radius), size, 0, false, SweepDirection.Clockwise);
        g.LineTo(new Point(hole.Left, hole.Top + radius));
        g.ArcTo(new Point(hole.Left + radius, hole.Top), size, 0, false, SweepDirection.Clockwise);
        g.EndFigure(true);
        return geometry;
    }

    // The ribbons' shape at the current position in the loop. Each wave sways back and forth by a few tenths of a
    // radian and breathes in height, out of step with the others; the ribbon's width swells and narrows too. All
    // are sines of whole cycles per loop, and none travels, so the ribbons undulate in place.
    private double Centre(Ribbon rb, double x, double height)
    {
        var u = 2 * Math.PI * x / rb.Wavelength;
        var t = 2 * Math.PI * Phase;
        var first = Math.Sin(rb.Waves * u + rb.Shift + 0.9 * Math.Sin(t + rb.Slot)) * (1 + 0.3 * Math.Sin(t + rb.Slot * 1.7));
        var second = Math.Sin(rb.Waves2 * u + rb.Shift2 + 1.1 * Math.Sin(2 * t + rb.Slot * 0.6)) * (1 + 0.25 * Math.Sin(2 * t + rb.Slot * 2.3));
        return height * (rb.Centre + rb.Amplitude * (first + 0.5 * second) / 1.5);
    }

    // The shape at x for ribbon r, for the tests of its periodicity.
    internal double CentreAt(int ribbon, double x, double height) => Centre(Ribbons[ribbon], x, height);

    internal double HalfWidthAt(int ribbon, double x, double height) => HalfWidth(Ribbons[ribbon], x, height);

    private double HalfWidth(Ribbon rb, double x, double height)
    {
        var u = 2 * Math.PI * x / rb.Wavelength;
        var t = 2 * Math.PI * Phase;
        var swell = Math.Sin(rb.Waves3 * u + rb.Shift + 1.0 * Math.Sin(3 * t + rb.Slot));
        return height * (rb.Thickness + rb.Swell * swell) * (1 + 0.12 * Math.Sin(2 * t + rb.Slot * 1.1)) / 2;
    }

    // How far across a ribbon a line runs: -1 is its top edge, 1 its bottom edge, 0 its middle (which weaves a
    // little).
    private double LineY(Ribbon rb, double across, double x, double centre, double half)
    {
        var weave = across == 0 ? 0.12 * Math.Sin(2 * Math.PI * 3 * x / rb.Wavelength + rb.Shift + 2 * Math.PI * Phase) : 0;
        return centre + half * (across + weave);
    }

    // How lit a line is at x, 0 to 1: three waves along it that also move with the loop, a whole number of times
    // per loop. Only the crests are lit: below the threshold (about 45% of the length at any time) a line is
    // dark, so the lit stretches read as separate catches of light with dark gaps between them, which travel.
    internal double Light(int ribbon, int line, double x)
    {
        var rb = Ribbons[ribbon];
        var u = 2 * Math.PI * x / rb.Wavelength;
        var a = Math.Sin(2 * u + line * 1.7 + ribbon + 2 * Math.PI * Phase);
        var b = Math.Sin(3 * u + line * 2.3 + ribbon * 0.9 - 4 * Math.PI * Phase);
        var c = Math.Sin(5 * u + line * 0.8 + ribbon * 1.9 + 6 * Math.PI * Phase);
        var m = 0.5 + 0.5 * (0.5 * a + 0.3 * b + 0.2 * c);
        m = Math.Clamp((m - 0.46) / 0.4, 0, 1);
        return m * m * (3 - 2 * m) * rb.Strength;
    }

    // Recomputes the curves for the current position, into the geometries and gradient stops built for this size.
    private void Update()
    {
        var height = Bounds.Height;
        for (var r = 0; r < Ribbons.Length; r++)
        {
            var rb = Ribbons[r];
            for (var i = 0; i < _x.Length; i++)
            {
                _centre![r][i] = Centre(rb, _x[i], height);
                _half![r][i] = HalfWidth(rb, _x[i], height);
            }

            _band![r] = new StreamGeometry();
            using (var g = _band[r].Open())
            {
                g.BeginFigure(new Point(_x[0], LineY(rb, -1, _x[0], _centre![r][0], _half![r][0])), true);
                for (var i = 1; i < _x.Length; i++)
                    g.LineTo(new Point(_x[i], LineY(rb, -1, _x[i], _centre[r][i], _half[r][i])));
                for (var i = _x.Length - 1; i >= 0; i--)
                    g.LineTo(new Point(_x[i], LineY(rb, 1, _x[i], _centre[r][i], _half[r][i])));
                g.EndFigure(true);
            }

            for (var l = 0; l < _lines![r].Length; l++)
            {
                var across = _across![r][l];
                _lines[r][l] = new StreamGeometry();
                using (var g = _lines[r][l].Open())
                {
                    g.BeginFigure(new Point(_x[0], LineY(rb, across, _x[0], _centre[r][0], _half[r][0])), false);
                    for (var i = 1; i < _x.Length; i++)
                        g.LineTo(new Point(_x[i], LineY(rb, across, _x[i], _centre[r][i], _half[r][i])));
                    g.EndFigure(false);
                }

                // The light along the line: a stop at every few px, each with the exact value there, so it is smooth.
                var core = _coreBrush![r][l];
                var halo = _haloBrush![r][l];
                for (var s = 0; s < core.GradientStops.Count; s++)
                {
                    var light = FullGlints ? 1 : Light(r, l, core.GradientStops[s].Offset * Bounds.Width);
                    core.GradientStops[s].Color = Color.FromArgb((byte)Math.Round(255 * light * (_dark ? 0.7 : 1.0)), _coreColour.R, _coreColour.G, _coreColour.B);
                    halo.GradientStops[s].Color = Color.FromArgb((byte)Math.Round(255 * light * (_dark ? 0.18 : 0.65)), _haloColour.R, _haloColour.G, _haloColour.B);
                }
            }
        }
    }

    private void DrawDots(DrawingContext context, int ribbon)
    {
        var height = Bounds.Height;
        foreach (var dot in _dots![ribbon])
        {
            var strength = DotStrength(ribbon, dot);
            if (strength < 0.05)
                continue;

            var (x, y) = DotPosition(ribbon, dot, height);
            var level = Math.Clamp((int)(strength * Levels), 0, Levels - 1);
            context.DrawEllipse(_dotBrushes![dot.Size][level], null, new Point(x, y), DotSizes[dot.Size], DotSizes[dot.Size]);
        }
    }

    // Twinkling, in alpha only, from nearly nothing to full: a soft swell for most dots and a short flash for some, each
    // at its own pace (about 1 to 3 s a cycle, a whole number of cycles per loop), and brighter where the edge beside it
    // is lit.
    private double DotStrength(int ribbon, Dot dot)
    {
        var cycle = dot.Rate * Phase + dot.Offset;
        var twinkle = FullGlints ? 1 : dot.Flash ? Flash(cycle - Math.Floor(cycle)) : Swell(cycle);
        var lit = FullGlints ? 1 : 0.3 + 0.7 * Light(ribbon, dot.Line, dot.X);
        return dot.Brightness * twinkle * lit;
    }

    // Where a dot is: it drifts a little around its place, and follows its edge as the ribbon undulates.
    private (double X, double Y) DotPosition(int ribbon, Dot dot, double height)
    {
        var rb = Ribbons[ribbon];
        var x = dot.X + dot.Drift * Math.Sin(2 * Math.PI * Phase + dot.Offset * 6.28);
        var side = dot.Line == 0 ? -1.0 : 1.0;
        return (x, Centre(rb, x, height) + side * HalfWidth(rb, x, height) + dot.Y);
    }

    /// <summary>The strength and position of every dot of a ribbon at the current position in the loop (for tests).</summary>
    internal (double Strength, double X, double Y)[] DotStates(int ribbon, double height) =>
        _dots![ribbon].Select(dot => (DotStrength(ribbon, dot), DotPosition(ribbon, dot, height).X, DotPosition(ribbon, dot, height).Y)).ToArray();

    // A soft swell: dark most of the time, rising to full and falling back once per cycle.
    private static double Swell(double cycle)
    {
        var s = 0.5 + 0.5 * Math.Sin(2 * Math.PI * cycle);
        return s * s * s;
    }

    // A quick flash at the start of each cycle, dark for the rest.
    private static double Flash(double fraction)
    {
        if (fraction >= 0.12)
            return 0;
        var s = Math.Sin(Math.PI * fraction / 0.12);
        return s * s;
    }

    // Sets up everything that depends on the size and the colours: the sample positions, the reusable geometries,
    // the brushes with their stops, and the dots.
    private void Build(Size size)
    {
        _built = size;
        var tints = Tints();
        var dark = Luma(_region) < 0.5;
        _dark = dark;
        // Dark: light lines glowing on the dark page. Light: lines a little deeper than the bands, in the accent, with a halo of
        // the accent, so they read as colour on the pale page (white would vanish into it).
        _coreColour = dark ? Mix(_accent, Colors.White, 0.85) : Mix(_accent, Colors.Black, 0.45);
        _haloColour = dark ? Mix(_accent, Colors.White, 0.4) : Mix(_accent, Colors.Black, 0.2);

        // Sample positions from just off the left edge to just off the right.
        var count = (int)Math.Ceiling((size.Width + 40) / SampleStep) + 1;
        _x = new double[count];
        for (var i = 0; i < count; i++)
            _x[i] = -20 + i * SampleStep;

        _centre = new double[Ribbons.Length][];
        _half = new double[Ribbons.Length][];
        _band = new StreamGeometry[Ribbons.Length];
        _bandBrush = new IBrush[Ribbons.Length];
        _lines = new StreamGeometry[Ribbons.Length][];
        _across = new double[Ribbons.Length][];
        _coreBrush = new LinearGradientBrush[Ribbons.Length][];
        _haloBrush = new LinearGradientBrush[Ribbons.Length][];
        _corePen = new Pen[Ribbons.Length][];
        _haloPen = new Pen[Ribbons.Length][];
        _dots = new Dot[Ribbons.Length][];
        var stops = (int)Math.Ceiling(size.Width / StopSpacing) + 1;
        for (var r = 0; r < Ribbons.Length; r++)
        {
            var rb = Ribbons[r];
            _centre[r] = new double[count];
            _half[r] = new double[count];
            _band[r] = new StreamGeometry();
            _bandBrush[r] = dark
                ? Solid(Mix(tints[rb.Tint].A, tints[rb.Tint].B, 0.5), 0.16 * rb.Strength)
                : Solid(LightBand(rb.Tint), LightBandOpacity * rb.Strength);

            // The lines: the top and bottom edges, and the weaving middle on the wider ribbons.
            var across = rb.Crest ? new[] { -1.0, 1.0, 0.0 } : new[] { -1.0, 1.0 };
            _across[r] = across;
            _lines[r] = new StreamGeometry[across.Length];
            _coreBrush[r] = new LinearGradientBrush[across.Length];
            _haloBrush[r] = new LinearGradientBrush[across.Length];
            _corePen[r] = new Pen[across.Length];
            _haloPen[r] = new Pen[across.Length];
            for (var l = 0; l < across.Length; l++)
            {
                _lines[r][l] = new StreamGeometry();
                _coreBrush[r][l] = LightBrush(size.Width, stops);
                _haloBrush[r][l] = LightBrush(size.Width, stops);
                var main = across[l] == -1;
                _haloPen[r][l] = new Pen(_haloBrush[r][l], main ? (dark ? 8 : 14) : (dark ? 5 : 9), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
                _corePen[r][l] = new Pen(_coreBrush[r][l], main ? 1.6 : 1.0, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
            }

            _dots[r] = MakeDots(r, rb, size);
        }

        BuildDotBrushes(dark);
        var strength = dark ? DarkScrim : LightScrim;
        _scrimBrush = strength > 0 ? Solid(_region, strength) : null;
    }

    // A horizontal gradient across the width with a stop every StopSpacing px; the colours are set each frame.
    private static LinearGradientBrush LightBrush(double width, int stops)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Absolute),
            EndPoint = new RelativePoint(width, 0, RelativeUnit.Absolute),
        };
        for (var i = 0; i < stops; i++)
            brush.GradientStops.Add(new GradientStop(Colors.Transparent, Math.Min(1, i * StopSpacing / width)));
        return brush;
    }

    // Dots spread across the width, close beside the ribbon's edges and thinning out with distance, in four sizes (the
    // larger ones brighter): never on a grid. Each belongs to the edge it sits by, and brightens with its light.
    private static Dot[] MakeDots(int ribbon, Ribbon rb, Size size)
    {
        var random = new Random(17 + ribbon * 31);
        var dots = new Dot[(int)Math.Round(rb.Dots * size.Width / rb.Wavelength)];
        for (var i = 0; i < dots.Length; i++)
        {
            var x = (i + random.NextDouble()) / dots.Length * size.Width;
            var side = random.Next(2) == 0 ? -1.0 : 1.0;
            var distance = -Math.Log(1 - random.NextDouble()) * size.Height * 0.03;
            var pick = random.NextDouble();
            var sizeIndex = pick < 0.45 ? 0 : pick < 0.8 ? 1 : pick < 0.95 ? 2 : 3;
            var brightness = (0.45 + random.NextDouble() * 0.55) * (sizeIndex >= 2 ? 1 : 0.8);
            // A cycle of 0.8 to 3 s, as whole cycles per loop; a fifth of the dots flash instead of swelling.
            var rate = (int)Math.Round(LoopSeconds / (0.8 + random.NextDouble() * 2.2));
            var flash = random.NextDouble() < 0.2;
            dots[i] = new Dot(x, side * distance, sizeIndex, flash ? Math.Min(1, brightness * 1.3) : brightness, rate, random.NextDouble(),
                side < 0 ? 0 : 1, flash, 4 + random.NextDouble() * 8);
        }
        return dots;
    }

    // Soft dots: for each size, a few brightness steps of a round glow tinted by the accent.
    private void BuildDotBrushes(bool dark)
    {
        var tint = Mix(_accent, Colors.White, dark ? 0.8 : 1);
        // In light, a faint deeper accent ring around the white core, so a glint reads on the pale page.
        var halo = Mix(_accent, Colors.Black, 0.1);
        _dotBrushes = new IBrush[DotSizes.Length][];
        for (var s = 0; s < DotSizes.Length; s++)
        {
            _dotBrushes[s] = new IBrush[Levels];
            for (var i = 0; i < Levels; i++)
            {
                var share = (i + 1.0) / Levels;
                Color With(Color c, double alpha) => Color.FromArgb((byte)Math.Round(255 * alpha), c.R, c.G, c.B);
                _dotBrushes[s][i] = new RadialGradientBrush
                {
                    Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    RadiusX = RelativeScalar.Parse("50%"),
                    RadiusY = RelativeScalar.Parse("50%"),
                    GradientStops = dark
                        ? [new GradientStop(With(tint, share * 0.9), 0), new GradientStop(With(tint, share * 0.45), 0.4), new GradientStop(With(tint, 0), 1)]
                        : [new GradientStop(With(tint, share * 0.95), 0), new GradientStop(With(tint, share * 0.7), 0.3), new GradientStop(With(halo, share * 0.4), 0.65), new GradientStop(With(halo, 0), 1)],
                }.ToImmutable();
            }
        }
    }

    // The ribbon colour pairs: the accent blended with neutrals (toward white in light, toward the text colour in dark).
    // In light, the bands are the accent, a little deeper from ribbon to ribbon, drawn with normal
    // alpha over the page, so they read as soft colour and overlaps deepen (a light tint added over a light page
    // would only converge on white).
    private Color LightBand(int tint) => tint switch
    {
        0 => Mix(_accent, Colors.Black, 0.12),
        1 => Mix(_accent, Colors.Black, 0.25),
        _ => Mix(_accent, Colors.Black, 0.38),
    };

    private (Color A, Color B)[] Tints()
    {
        var dark = Luma(_region) < 0.5;
        var highlight = dark ? Mix(_accent, _text, 0.35) : Mix(_accent, Colors.White, 0.55);
        return
        [
            (Mix(_accent, _region, 0.25), highlight),
            (highlight, Mix(_accent, dark ? Colors.Black : _region, 0.35)),
            (Mix(_accent, Colors.White, dark ? 0.1 : 0.3), _accent),
        ];
    }

    private static IBrush Solid(Color colour, double alpha) =>
        new SolidColorBrush(Color.FromArgb((byte)Math.Round(255 * alpha), colour.R, colour.G, colour.B)).ToImmutable();

    private static double Luma(Color c) => (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255;

    private static Color Mix(Color from, Color to, double amount) => Color.FromRgb(
        (byte)Math.Round(from.R + (to.R - from.R) * amount),
        (byte)Math.Round(from.G + (to.G - from.G) * amount),
        (byte)Math.Round(from.B + (to.B - from.B) * amount));
}
