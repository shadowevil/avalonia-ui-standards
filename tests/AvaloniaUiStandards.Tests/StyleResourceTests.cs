using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

public class StyleResourceTests
{
    public static TheoryData<string, string, string> Palette => new()
    {
        { "SystemRegionColor", "#F5F5F5", "#1E1E1E" },
        { "SystemAltHighColor", "#FFFFFF", "#1E1E1E" },
        { "SystemBaseHighColor", "#1A1A1A", "#F2F2F2" },
        { "SystemBaseMediumHighColor", "#404040", "#D9D9D9" },
        { "SystemBaseMediumColor", "#737373", "#9E9E9E" },
        { "SystemBaseMediumLowColor", "#A6A6A6", "#6B6B6B" },
        { "SystemBaseLowColor", "#D4D4D4", "#454545" },
        { "SystemChromeLowColor", "#E8E8E8", "#2A2A2A" },
        { "SystemChromeMediumLowColor", "#FFFFFF", "#2A2A2A" },
        { "SystemChromeMediumColor", "#E8E8E8", "#333333" },
        { "SystemChromeHighColor", "#D4D4D4", "#525252" },
        { "SystemListLowColor", "#E8E8E8", "#2A2A2A" },
        { "SystemListMediumColor", "#D4D4D4", "#404040" },
        { "SystemAccentColor", "#007CEA", "#007CEA" },
    };

    [AvaloniaTheory]
    [MemberData(nameof(Palette))]
    public void PaletteColours_ResolveInLightAndDark(string key, string light, string dark)
    {
        var theme = NewTheme();
        Assert.Equal(Color.Parse(light), Resolve<Color>(theme, key, ThemeVariant.Light));
        Assert.Equal(Color.Parse(dark), Resolve<Color>(theme, key, ThemeVariant.Dark));
    }

    public static TheoryData<string, object> Values => new()
    {
        { "FontSizeCaption", 12d },
        { "FontSizeBody", 14d },
        { "FontSizeBodyStrong", 14d },
        { "FontWeightBodyStrong", FontWeight.SemiBold },
        { "FontSizeSubtitle", 20d },
        { "FontWeightSubtitle", FontWeight.SemiBold },
        { "FontSizeTitle", 28d },
        { "FontWeightTitle", FontWeight.SemiBold },
        { "FontSizeLargeTitle", 40d },
        { "FontWeightLargeTitle", FontWeight.SemiBold },
        { "FontSizeDisplay", 68d },
        { "FontWeightDisplay", FontWeight.SemiBold },
        { "SpacingXS", 4d },
        { "SpacingS", 8d },
        { "SpacingM", 12d },
        { "SpacingL", 16d },
        { "SpacingXL", 24d },
        { "SpacingXXL", 32d },
        { "SpacingXXXL", 48d },
        { "CornerRadiusControl", new CornerRadius(8) },
        { "CornerRadiusOverlay", new CornerRadius(16) },
        { "ControlCornerRadius", new CornerRadius(8) },
        { "OverlayCornerRadius", new CornerRadius(16) },
        { "CornerSmoothing", 0.6 },
        { "MotionFast", TimeSpan.FromMilliseconds(100) },
        { "MotionNormal", TimeSpan.FromMilliseconds(200) },
        { "MotionSlow", TimeSpan.FromMilliseconds(300) },
        { "GlassBlurSigma", 6d },
        { "GlassRefractionStrength", 28d },
        { "GlassRefractionEdge", 28d },
        { "GlassChromaticDispersion", 0.35 },
        { "GlassTintOpacity", 0.6 },
    };

    [AvaloniaTheory]
    [MemberData(nameof(Values))]
    public void StyleValues_ResolveInLightAndDark(string key, object expected)
    {
        var theme = NewTheme();
        Assert.Equal(expected, Resolve<object>(theme, key, ThemeVariant.Light));
        Assert.Equal(expected, Resolve<object>(theme, key, ThemeVariant.Dark));
    }

    [AvaloniaFact]
    public void GlassTint_DiffersForLightAndDark()
    {
        var theme = NewTheme();
        Assert.Equal(Colors.White, Resolve<Color>(theme, "GlassTintColor", ThemeVariant.Light));
        Assert.Equal(Color.Parse("#1E1E1E"), Resolve<Color>(theme, "GlassTintColor", ThemeVariant.Dark));
    }

    [AvaloniaFact]
    public void MotionEasing_IsCubicEaseOut()
    {
        Assert.IsType<CubicEaseOut>(Resolve<object>(NewTheme(), "MotionEasing", ThemeVariant.Light));
    }

    [AvaloniaTheory]
    [InlineData(false, null, false)]
    [InlineData(true, null, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    public void ReduceMotion_OverridesOrFollowsOs(bool os, bool? setting, bool expectReduced)
    {
        var theme = new StandardsTheme(new OsAccessibility.Settings(os, false)) { ReduceMotion = setting };
        foreach (var (key, ms) in new[] { ("MotionFast", 100), ("MotionNormal", 200), ("MotionSlow", 300) })
            Assert.Equal(expectReduced ? TimeSpan.Zero : TimeSpan.FromMilliseconds(ms), Resolve<TimeSpan>(theme, key, ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void ReduceMotion_BackToNull_FollowsOsAgain()
    {
        var theme = new StandardsTheme(new OsAccessibility.Settings(true, false)) { ReduceMotion = false };
        Assert.Equal(TimeSpan.FromMilliseconds(100), Resolve<TimeSpan>(theme, "MotionFast", ThemeVariant.Light));
        theme.ReduceMotion = null;
        Assert.Equal(TimeSpan.Zero, Resolve<TimeSpan>(theme, "MotionFast", ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void OsSettings_ReadWithoutThrowing()
    {
        var settings = OsAccessibility.Read();
        Assert.Null(new StandardsTheme().ReduceMotion);
        Assert.Equal(settings.ReduceTransparency, new StandardsTheme().IsTransparencyReduced);
    }

    [AvaloniaFact]
    public void Density_DefaultsToNormal()
    {
        Assert.Equal(DensityStyle.Normal, NewTheme().Density);
    }

    [AvaloniaFact]
    public void ContentControlThemeFontFamily_ResolvesToInter()
    {
        var family = Resolve<FontFamily>(NewTheme(), "ContentControlThemeFontFamily", ThemeVariant.Light);
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family), out var glyphTypeface));
        Assert.Equal("Inter", glyphTypeface.FamilyName);

        var text = new TextBlock { Text = "Inter" };
        var window = new Window { Content = text };
        window.Show();
        Assert.Equal(family, text.FontFamily);
    }

    [AvaloniaFact]
    public void AccentOverride_ChangesAccentBrushes()
    {
        var accent = Color.Parse("#E91E63");
        var theme = NewTheme();
        var border = new Border { Width = 10, Height = 10 };
        border.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemControlHighlightAccentBrush"));
        var accentButton = new Border { Width = 10, Height = 10 };
        accentButton.Bind(Border.BackgroundProperty, new DynamicResourceExtension("AccentButtonBackground"));
        var window = new Window { Content = new StackPanel { Children = { border, accentButton } } };
        window.Styles.Add(theme);
        window.Show();

        Assert.Equal(StandardsTheme.DefaultAccent, ((ISolidColorBrush)border.Background!).Color);

        theme.Accent = accent;
        window.UpdateLayout();

        Assert.Equal(accent, Resolve<Color>(theme, "SystemAccentColor", ThemeVariant.Light));
        Assert.Equal(accent, Resolve<Color>(theme, "SystemAccentColor", ThemeVariant.Dark));
        Assert.Equal(accent, ((ISolidColorBrush)border.Background!).Color);
        Assert.Equal(accent, ((ISolidColorBrush)accentButton.Background!).Color);
    }

    private static StandardsTheme NewTheme() => new(OsAccessibility.Settings.NotReduced);

    private static T Resolve<T>(IResourceNode node, string key, ThemeVariant variant)
    {
        Assert.True(node.TryGetResource(key, variant, out var value), $"Resource {key} not found ({variant}).");
        return Assert.IsAssignableFrom<T>(value);
    }
}
