using Avalonia;

namespace AvaloniaUiStandards.Style;

public static class AppBuilderExtensions
{
    /// <summary>Registers what <see cref="StandardsTheme"/> needs: the bundled Inter font.</summary>
    public static AppBuilder UseUiStandards(this AppBuilder builder) => builder.WithInterFont();
}
