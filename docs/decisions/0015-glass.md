# 0015 — Glass

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - The library's own `GlassBorder` in the Style tier ([0004](0004-tier-names.md)): a Skia backdrop blur of what is behind it in the window, plus a tint and a 1 px light border. Its content is clipped to the shape.
  - It refracts the backdrop like a thick glass slab. Content behind is displaced (lensed) near the rim within an edge band and only blurred in the flat centre. The order is refract, then blur, then tint, then border.
  - The blur and refraction run on a half-resolution copy of the backdrop (relative to the screen scaling), then scale back up, so they are cheaper (about half the cost). A pixel test checks the look against full resolution. Reusing the result while nothing behind the glass changes is not possible in Avalonia 12, which already redraws only changed regions.
  - Rim (planned, M3 step 5c):
    - Every glass surface gets a hairline rim, about 1 px, right on the edge, with no inner band.
    - It takes a light tint of what is directly below each point of the edge (the lightened local backdrop colour, hue kept), and its intensity follows the local brightness, so it is faint over dark areas and never uniform.
    - Corners and the top edge catch slightly more light.
    - The rim replaces the fixed 1 px border on glass. The fixed border colours (see Values) stay only for the solid fallback (reduce transparency or no Skia), which is otherwise unchanged.
    - Exact values come from renders at that step.
  - Values:
    - Blur sigma 6 ("light frost").
    - Refraction strength 28 px over a 28 px edge band.
    - Chromatic dispersion on, at a subtle 0.35.
    - Tint and border:

      | | Light | Dark |
      |---|---|---|
      | Tint colour | `#FFFFFF` | `#1E1E1E` |
      | Tint opacity | 0.6 | 0.6 |
      | 1 px border | `#99FFFFFF` (white 60%) | `#33FFFFFF` (white 20%) |
  - Theme keys: `GlassTintColor`, `GlassTintOpacity`, `GlassBorderBrush`, `GlassBlurSigma`, `GlassRefractionStrength`, `GlassRefractionEdge`, `GlassChromaticDispersion`.
  - It is used on overlays (menus, flyouts, tooltips, dialogs), on the window's content panel ([0022](0022-window-layout.md)), and on cards only where they sit directly on the window backdrop. Cards inside the content panel are solid (card colour), so there is no glass on glass. Sidebars are not glass.
  - Fallbacks:
    - Without Skia runtime effects, the blur stays and refraction and dispersion drop.
    - Without Skia, or when "reduce transparency" is on, it is a solid fill in the neutral palette ([0011](0011-colour.md)).
  - The library reads "reduce transparency" from the OS once, when the theme is created (macOS accessibility settings, Windows transparency setting). Linux has no such setting, so it stays "not reduced" unless the app forces it. Apps can force it through `StandardsTheme` ([0017](0017-app-usage.md)).
- **Why:** The glass is the same on every OS and in headless renders. Avalonia's `ExperimentalAcrylicBorder` renders nothing headless, and OS window effects differ per platform. Refraction makes the surface read as real glass, not a tinted overlay.
