# 0015 — Glass

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - The library's own `GlassBorder` in the Style tier ([0004](0004-tier-names.md)): a Skia backdrop blur of what is behind it in the window, plus a tint and a rim (below). Its content is clipped to the shape.
  - It refracts the backdrop like a thick glass slab. Content behind is displaced (lensed) near the rim within an edge band and only blurred in the flat centre. The order is refract, then blur, then tint, then the rim.
  - The blur and refraction run on a half-resolution copy of the backdrop (relative to the screen scaling), then scale back up, so they are cheaper (about half the cost). A pixel test checks the look against full resolution. Reusing the result while nothing behind the glass changes is not possible in Avalonia 12, which already redraws only changed regions.
  - Rim:
    - Every glass surface gets a hairline rim right on the edge, with a soft glow falling inward from it.
    - Its width follows the local brightness: a hairline over dim areas, swelling smoothly, inward only, where the backdrop under that part of the edge is bright.
    - It takes a light tint of what is directly outside each point of the edge (the lightened local backdrop colour, hue kept), and its intensity follows the local brightness, so it is faint over dark areas and never uniform.
    - Corners and the top edge catch slightly more light.
    - Where a bright feature, such as a thin line, touches the edge, the rim takes on its tint and brightness at that point, and the light runs along the edge in both directions before fading. The crossing blends into that run with no odd pixel.
    - The rim replaces the fixed 1 px border on glass. The fixed border colours (see Values) stay only for the solid fallback (reduce transparency or no Skia), which is otherwise unchanged.
    - Rim values:
      - Width 1 px, swelling to 1.75 px over the brightest backdrop.
      - Inner glow 4 px beyond the rim's width, starting at half the rim's strength and fading to nothing.
      - Sampled 1.5 px outside the edge.
      - Lightened towards white by 0.3 in light and 0.6 in dark.
      - Top edge bias 1.0, sides and bottom 0.7.
      - Strength 1: the rim's opacity follows the local brightness at full scale.
      - Carried light runs 12 px along the edge at full strength in each direction, then fades over about 8 px. Its excess over the surrounding brightness counts 1.7 times.
      - White mix 0.5 in both themes: catches are half the feature's colour, half white.
      - Bright stretches and catches are amplified by up to 100% (boost 1), ramping smoothly with the backdrop's luminance from 0.4 to 0.9. The same ramp widens the rim inward by up to 0.25 px, within the 1.75 px maximum, so the boost shows even where the rim is already near white.
    - Why these values: they were tuned live in the catalog against its ribbon backdrop. The gain was lowered to 1.7 and the run along the edge doubled to 12 px, for a longer, softer run of light. The 4 px inner glow makes the edge read as lit glass rather than a drawn line. A white mix of 0.5 in both themes keeps catches partly the feature's colour and partly white, and a boost of 1 doubles the bright catches so they stand out.
  - Values:
    - Blur sigma 6 ("light frost").
    - Refraction strength 28 px over a 28 px edge band.
    - Chromatic dispersion on, at a subtle 0.35.
    - Tint and border:

      | | Light | Dark |
      |---|---|---|
      | Tint colour | `#FFFFFF` | `#1E1E1E` |
      | Tint opacity | 0.68 | 0.68 |
      | 1 px border | `#99FFFFFF` (white 60%) | `#33FFFFFF` (white 20%) |
  - Theme keys: `GlassTintColor`, `GlassTintOpacity`, `GlassBorderBrush`, `GlassBlurSigma`, `GlassRefractionStrength`, `GlassRefractionEdge`, `GlassChromaticDispersion`.
  - It is used on overlays (menus, flyouts, tooltips, dialogs), on the window's content panel ([0022](0022-window-layout.md)), and on cards only where they sit directly on the window backdrop. Cards inside the content panel are solid (card colour), so there is no glass on glass. Sidebars are not glass.
    - Tooltips are solid; see [0037](0037-tooltip-and-notifications.md).
  - Fallbacks:
    - Without Skia runtime effects, the blur stays and refraction and dispersion drop.
    - Without Skia, or when "reduce transparency" is on, it is a solid fill in the neutral palette ([0011](0011-colour.md)).
    - Glass in a popup shown as its own OS window is the solid fill; see [0036](0036-menus-and-flyouts.md).
  - The library reads "reduce transparency" from the OS once, when the theme is created (macOS accessibility settings, Windows transparency setting). Linux has no such setting, so it stays "not reduced" unless the app forces it. Apps can force it through `StandardsTheme` ([0017](0017-app-usage.md)).
- **Why:** The glass is the same on every OS and in headless renders. Avalonia's `ExperimentalAcrylicBorder` renders nothing headless, and OS window effects differ per platform. Refraction makes the surface read as real glass, not a tinted overlay.
