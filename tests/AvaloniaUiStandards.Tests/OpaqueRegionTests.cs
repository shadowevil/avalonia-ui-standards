using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using AvaloniaUiStandards.Modules;
using AvaloniaUiStandards.Style;
using Xunit;
using static AvaloniaUiStandards.Tests.GlassSharedBackdropTests;

namespace AvaloniaUiStandards.Tests;

/// <summary>
/// Opaque regions follow their content; glass skips the opaque regions of its content; StandardsWindow clips its
/// backdrop to the rest of the window and exposes that region with change notification.
/// </summary>
public class OpaqueRegionTests
{
    // The glass read reach with the default values: three blur sigmas plus the furthest refraction displacement.
    private static readonly double Reach = 3 * GlassBorder.DefaultBlurSigma
        + GlassBorder.DefaultRefractionStrength * (1 + GlassBorder.DefaultChromaticDispersion);

    // A window whose panel holds a canvas with an opaque document at (left, top).
    private static (StandardsWindow Window, Border Doc) DocumentWindow(double left = 40, double top = 30, double width = 400, double height = 260)
    {
        var doc = new Border { Background = Brushes.White, Width = width, Height = height };
        OpaqueRegion.SetIsOpaque(doc, true);
        Canvas.SetLeft(doc, left);
        Canvas.SetTop(doc, top);
        var window = Show(NewWindow(content: new Canvas { Children = { doc } }, scrollable: false));
        return (window, doc);
    }

    private static Visual Layers(StandardsWindow window) => Part<SquircleBorder>(window, StandardsWindowTheme.FramePart).Child!;

    private static Rect InLayers(StandardsWindow window, Control c) =>
        new(c.TranslatePoint(default, Layers(window))!.Value, c.Bounds.Size);

    private static Point Centre(Rect r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);

    private static Rect SkipBounds(GlassBorder glass) => glass.Skip.Aggregate((a, b) => a.Union(b));

    [AvaloniaFact]
    public void Document_IsCutOutOfTheVisibleRegion_ShrunkByTheReach()
    {
        var (window, doc) = DocumentWindow();
        try
        {
            var region = window.BackdropVisibleRegion;
            var docRect = InLayers(window, doc);
            Assert.False(region.Contains(Centre(docRect)));
            // Within the reach of the document's edge the backdrop stays: glass beside it reads it.
            Assert.True(region.Contains(new Point(docRect.X + Reach - 2, docRect.Center.Y)));
            Assert.False(region.Contains(new Point(docRect.X + Reach + 2, docRect.Center.Y)));
            Assert.True(region.Contains(new Point(2, 2)));
            Assert.Equal(new Rect(Layers(window).Bounds.Size), region.Bounds);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void WithoutOpaqueContent_TheRegionIsTheWholeFrame_AndTheBackdropIsNotClipped()
    {
        var window = Show(NewWindow());
        try
        {
            var region = window.BackdropVisibleRegion;
            Assert.Single(region.Rects);
            Assert.Equal(new Rect(Layers(window).Bounds.Size), region.Rects[0]);
            Assert.Null(Part<Control>(window, StandardsWindowTheme.BackdropPart).Clip);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BackdropClip_LeavesOutTheShrunkDocument()
    {
        var (window, doc) = DocumentWindow();
        try
        {
            var clip = Part<Control>(window, StandardsWindowTheme.BackdropPart).Clip;
            Assert.NotNull(clip);
            var docRect = InLayers(window, doc);
            Assert.False(clip!.FillContains(Centre(docRect)));
            Assert.True(clip.FillContains(new Point(docRect.X + Reach - 2, docRect.Center.Y)));
            Assert.True(clip.FillContains(new Point(4, 4)));

            // Unmarked, the document no longer clips the backdrop.
            OpaqueRegion.SetIsOpaque(doc, false);
            Pump(window);
            Assert.Null(Part<Control>(window, StandardsWindowTheme.BackdropPart).Clip);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Panel_SkipsTheDocument_InItsOwnCoordinates()
    {
        var (window, doc) = DocumentWindow();
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            var expected = new Rect(doc.TranslatePoint(default, panel)!.Value, doc.Bounds.Size);
            Assert.Equal([expected], panel.Skip);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Region_FollowsMovesAndResizes()
    {
        var (window, doc) = DocumentWindow();
        try
        {
            var before = InLayers(window, doc);
            Canvas.SetLeft(doc, 140);
            Pump(window);
            var moved = InLayers(window, doc);
            Assert.Equal(before.X + 100, moved.X);
            Assert.True(window.BackdropVisibleRegion.Contains(new Point(before.X + Reach + 10, before.Center.Y)));
            Assert.False(window.BackdropVisibleRegion.Contains(Centre(moved)));
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            Assert.Equal(new Rect(doc.TranslatePoint(default, panel)!.Value, doc.Bounds.Size), SkipBounds(panel));

            doc.Width = 200;
            Pump(window);
            var resized = InLayers(window, doc);
            Assert.True(window.BackdropVisibleRegion.Contains(new Point(resized.Right + 20, resized.Center.Y)));
            Assert.Equal(200, SkipBounds(panel).Width);
        }
        finally
        {
            window.Close();
        }
    }

    // In a scrolled panel the document is cut by the viewport: only its visible part counts.
    [AvaloniaFact]
    public void Region_IsClippedByAScrollingAncestor()
    {
        var doc = new Border { Background = Brushes.White, Height = 400 };
        OpaqueRegion.SetIsOpaque(doc, true);
        var content = new StackPanel { Children = { new Border { Height = 200 }, doc, new Border { Height = 600 } } };
        var window = Show(NewWindow(content: content));
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            var viewport = Part<ScrollViewer>(window, StandardsWindowTheme.ContentScrollViewerPart);
            var skip = SkipBounds(panel);
            var viewportRect = new Rect(viewport.TranslatePoint(default, panel)!.Value, viewport.Bounds.Size);
            Assert.True(viewportRect.Contains(skip), $"{skip} is outside the viewport {viewportRect}");
            var top = skip.Top;

            viewport.Offset = new Vector(0, 150);
            Pump(window);
            skip = SkipBounds(panel);
            Assert.Equal(top - 150, skip.Top, 1);
            Assert.True(viewportRect.Contains(skip));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HiddenOrFadedContent_IsNotOpaque()
    {
        var (window, doc) = DocumentWindow();
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            var centre = Centre(InLayers(window, doc));

            doc.IsVisible = false;
            Pump(window);
            Assert.Empty(panel.Skip);
            Assert.True(window.BackdropVisibleRegion.Contains(centre));

            doc.IsVisible = true;
            doc.Opacity = 0.5;
            Pump(window);
            Assert.Empty(panel.Skip);
            Assert.True(window.BackdropVisibleRegion.Contains(centre));

            doc.Opacity = 1;
            Pump(window);
            Assert.NotEmpty(panel.Skip);
            Assert.False(window.BackdropVisibleRegion.Contains(centre));

            // A rotation makes it no longer axis-aligned.
            doc.RenderTransform = new RotateTransform(10);
            Pump(window);
            Assert.Empty(panel.Skip);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RoundedCorners_AreLeftOut()
    {
        var (window, doc) = DocumentWindow();
        doc.CornerRadius = new CornerRadius(12);
        Pump(window);
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            var docRect = new Rect(doc.TranslatePoint(default, panel)!.Value, doc.Bounds.Size);
            Assert.Equal(2, panel.Skip.Length);
            Assert.DoesNotContain(panel.Skip, r => r.Contains(docRect.TopLeft + new Point(1, 1)));
            Assert.Contains(panel.Skip, r => r.Contains(docRect.Center));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void VisibleRegion_RaisesAChange_OnlyWhenItChanges()
    {
        var (window, doc) = DocumentWindow();
        try
        {
            var changes = 0;
            window.PropertyChanged += (_, e) =>
            {
                if (e.Property == StandardsWindow.BackdropVisibleRegionProperty)
                    changes++;
            };
            window.Title = "Another title";
            Pump(window);
            Assert.Equal(0, changes);

            Canvas.SetTop(doc, 60);
            Pump(window);
            Assert.Equal(1, changes);

            window.Width = 700;
            Pump(window);
            Assert.Equal(2, changes);
        }
        finally
        {
            window.Close();
        }
    }

    // Solid glass with an opaque tint counts as opaque: in Solid quality the panel is cut out of the backdrop, exactly,
    // since no glass reads the backdrop.
    [AvaloniaFact]
    public void SolidGlass_CountsAsOpaque()
    {
        var window = NewWindow();
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { GlassQuality = GlassQuality.Solid });
        Show(window);
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            Assert.True(panel.IsOpaqueFill);
            var panelRect = InLayers(window, panel);
            Assert.False(window.BackdropVisibleRegion.Contains(Centre(panelRect)));
            Assert.False(window.BackdropVisibleRegion.Contains(new Point(panelRect.X + 2, panelRect.Center.Y)));
            Assert.True(window.BackdropVisibleRegion.Contains(new Point(panelRect.X - 2, panelRect.Center.Y)));
            Assert.NotNull(Part<Control>(window, StandardsWindowTheme.BackdropPart).Clip);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SolidGlass_WithATranslucentTint_IsNotOpaque()
    {
        var window = NewWindow();
        window.Styles.Add(new StandardsTheme(AccessibilitySettings.NotReduced) { GlassQuality = GlassQuality.Solid });
        Show(window);
        try
        {
            var panel = Part<GlassBorder>(window, StandardsWindowTheme.ContentPanelPart);
            panel.TintColor = Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF);
            Pump(window);
            Assert.False(panel.IsOpaqueFill);
            Assert.True(window.BackdropVisibleRegion.Contains(Centre(InLayers(window, panel))));
        }
        finally
        {
            window.Close();
        }
    }

    // Opaque content inside the backdrop slot is part of the backdrop, not above it.
    [AvaloniaFact]
    public void OpaqueContentInTheBackdrop_DoesNotCutIt()
    {
        var inBackdrop = new Border { Background = Brushes.Red, Width = 300, Height = 300 };
        OpaqueRegion.SetIsOpaque(inBackdrop, true);
        var window = NewWindow();
        window.Backdrop = new Panel { Children = { inBackdrop } };
        Show(window);
        try
        {
            Assert.Single(window.BackdropVisibleRegion.Rects);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Region_Arithmetic()
    {
        var region = BackdropRegion.Create(new Rect(0, 0, 100, 100), [new Rect(25, 25, 50, 50)]);
        Assert.Equal(4, region.Rects.Count);
        Assert.Equal(10000 - 2500, region.Rects.Sum(r => r.Width * r.Height));
        Assert.False(region.Contains(new Point(50, 50)));
        Assert.True(region.Contains(new Point(10, 50)));
        Assert.True(region.Intersects(new Rect(20, 20, 10, 10)));
        Assert.False(region.Intersects(new Rect(30, 30, 10, 10)));
        Assert.Equal(region, BackdropRegion.Create(new Rect(0, 0, 100, 100), [new Rect(25, 25, 50, 50)]));
        Assert.True(BackdropRegion.Create(new Rect(0, 0, 100, 100), [new Rect(-10, -10, 200, 200)]).IsEmpty);
    }

    // UseUiStandards pins one dirty rectangle per frame (Avalonia's default): the kept sample and the rim strip read
    // the redrawn region as one rectangle.
    [AvaloniaFact]
    public void UseUiStandards_TurnsOffRegionDirtyRectClipping()
    {
        const BindingFlags any = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var locator = typeof(AvaloniaLocator).GetProperty("Current", any)!.GetValue(null)!;
        var options = (CompositionOptions?)locator.GetType().GetMethod("GetService", [typeof(Type)])!.Invoke(locator, [typeof(CompositionOptions)]);
        Assert.NotNull(options);
        Assert.False(options!.UseRegionDirtyRectClipping);
    }
}
