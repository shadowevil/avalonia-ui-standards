namespace AvaloniaUiStandards;

/// <summary>How glass surfaces draw, app-wide (<see cref="StandardsTheme.GlassQuality"/>).</summary>
public enum GlassQuality
{
    /// <summary>
    /// Full glass at the first level (refraction, dispersion, blur, tint and the rim); minimal glass
    /// at the second level and on small surfaces; solid from the third level.
    /// </summary>
    Full,

    /// <summary>Every glass surface minimal: a blurred and tinted backdrop with the rim, no refraction or dispersion.</summary>
    Minimal,

    /// <summary>Every glass surface the solid fill with its fixed border, as with reduce transparency.</summary>
    Solid,
}
