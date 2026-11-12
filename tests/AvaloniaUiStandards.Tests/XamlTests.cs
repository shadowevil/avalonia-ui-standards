using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards.Style;
using Xunit;

namespace AvaloniaUiStandards.Tests;

public class XamlTests
{
    [AvaloniaFact]
    public void CompiledXaml_UsesStyleTierWithoutPrefix()
    {
        var sample = new XamlSample();

        var theme = Assert.IsType<StandardsTheme>(Assert.Single(sample.Styles));
        Assert.Equal(Color.Parse("#E91E63"), theme.Accent);
        Assert.Equal(DensityStyle.Compact, theme.Density);
        Assert.False(theme.ReduceTransparency);

        Assert.Equal(new Avalonia.CornerRadius(8), sample.Squircle.CornerRadius);
        Assert.Equal(6, sample.Glass.BlurSigma);
    }
}
