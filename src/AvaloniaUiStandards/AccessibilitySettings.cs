namespace AvaloniaUiStandards;

/// <summary>
/// The OS accessibility settings the theme starts from, as given to
/// <see cref="StandardsTheme.CreateForTests"/>.
/// </summary>
/// <param name="ReduceMotion">The OS "reduce motion" setting.</param>
/// <param name="ReduceTransparency">The OS "reduce transparency" setting.</param>
public readonly record struct AccessibilitySettings(bool ReduceMotion, bool ReduceTransparency)
{
    /// <summary>Neither setting is on.</summary>
    public static AccessibilitySettings NotReduced => default;
}
