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
    - The rim replaces the fixed 1 px border on glass. The fixed border colours (see Values) stay only for the solid fallback (see Fallbacks), which is otherwise unchanged.
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
    - How it is drawn:
      - By default the rim reads the backdrop along the edge into a strip, one texel per device pixel of the perimeter (rows of up to 4096 texels, a texture size every GPU supports). Three small passes along the strip work out the carried light (the brightest colour within 12 px, faded, against the darkest). One draw over the band then shades each pixel from the squircle's exact distance (an analytic distance in a shader, within about 0.05 px of the outline).
      - It skips the work outside the region Avalonia redraws, and it takes the current opacity. It reuses the strip when that region holds none of the edge's sample points. When it holds some, only those are read again: outside the region the surface holds the last frame, with whatever was drawn above the glass.
      - Limits: where the region is several rectangles, Skia gives only their bounding box, so points inside the box but outside the rectangles are read from the last frame. Every point is read when the rim's visual renders again (a change of size, shape, theme or rim method), since each render creates a new draw operation. Opacity changes and fades do not render it again.
      - Inside an effect or opacity-mask layer (with `SkiaOptions.UseOpacitySaveLayer`, also below full opacity), without a surface to read, or when the strip's textures cannot be allocated, the rim uses Skia backdrop filters instead (a displacement, a dilate, a blur and an erode per strip along the edge), with the same distance shader for its geometry. The surface does not hold a layer's content until the layer ends, so the strip would read the wrong backdrop there.
      - The two methods differ by under 1.5 levels on average over the rim's pixels. The strip carries light along the edge itself and the filters across a square, so a few catches within 20 px of a corner differ more.
      - Why: the strip draws the rim in about a tenth of the GPU time of the filters, with about a twentieth of their CPU time. Its geometry needs no rasterised masks, so a new size (a resize or a sidebar collapse) costs no rebuild.
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
  - It is used on overlays (menus, flyouts, tooltips, dialogs), on the window's content panel ([0022](0022-window-layout.md)), and on cards only where they sit directly on the window backdrop. Cards inside the content panel are solid (card colour). Sidebars are not glass.
    - Tooltips are solid; see [0037](0037-tooltip-and-notifications.md).
    - Glass overlays open in the window's overlay layer, so they stay inside the window; see [0036](0036-menus-and-flyouts.md).
  - Glass nests two levels deep. A glass surface inside one glass surface (the second level) is minimal glass; a third level and deeper is the solid fill. Solid ancestors do not count as levels.
  - Quality:
    - Full glass: refraction, dispersion, blur, tint and the rim.
    - Minimal glass: blur, tint and the rim, with no refraction or dispersion.
    - With `GlassQuality` `Full` (the default; [0017](0017-app-usage.md)), the first level is full glass. The second level, and any glass whose shorter side is under 64 px, is minimal glass. `Minimal` makes every glass level minimal; `Solid` makes every glass surface the solid fill. `GlassQuality` is the app's performance switch, separate from the reduce-transparency accessibility setting, which makes glass solid whatever the quality.
    - The quality does not change under load.
    - Why: refraction is the costliest part of the fill and reads only on the outer level. Under 64 px the 28 px refraction bands leave under 8 px of flat centre, and full and minimal glass render the same there. A fixed quality keeps every frame looking the same.
  - GPU budget: `.UseUiStandards()` sets Skia's GPU resource budget to 256 MB ([0017](0017-app-usage.md)). Why: glass needs about 190 MB of GPU scratch in a full-screen window with several surfaces. Under Avalonia's default budget (about 29 MB), Skia frees and reallocates its textures every frame.
  - Performance:
    - Measured on an Apple A18 Pro (Metal) at 2x, over the catalog's animated ribbons at the display rate (60 Hz). The ribbons alone run at 58 fps with a 9.4 ms render frame.
    - The rim takes 0.5 to 1.1 ms of CPU and GPU time per frame for a full-window panel with four cards; the filter rim takes 7 to 20 ms. The fill (blur, tint and refraction) is the larger cost.
    - Full glass side by side on the backdrop holds 30 fps up to about 16 surfaces in a normal window and 24 surfaces full screen. 30 fps is first lost at 24 and 32 surfaces respectively.
    - Minimal glass at the second level, side by side in one panel, holds 30 fps up to about 33 surfaces. It is lost by 65.
    - Where 30 fps is lost, the GPU is the limit: about 20 ms of GPU time per frame against 8 to 10 ms of CPU, mostly the fills' backdrop blurs (about 0.7 ms each).
    - Not yet done, the next step: one shared backdrop sample per frame (one blur for all glass layers) in place of each fill's own backdrop blur.
  - Fallbacks:
    - Without Skia runtime effects, the blur stays and refraction and dispersion drop.
    - Without Skia, when "reduce transparency" is on, or with `GlassQuality` `Solid`, it is a solid fill in the neutral palette ([0011](0011-colour.md)).
    - Glass in a popup shown as its own OS window is the solid fill; see [0036](0036-menus-and-flyouts.md).
  - The library reads "reduce transparency" from the OS once, when the theme is created (macOS accessibility settings, Windows transparency setting). Linux has no such setting, so it stays "not reduced" unless the app forces it. Apps can force it through `StandardsTheme` ([0017](0017-app-usage.md)).
- **Why:** The glass is the same on every OS and in headless renders. Avalonia's `ExperimentalAcrylicBorder` renders nothing headless, and OS window effects differ per platform. Refraction makes the surface read as real glass, not a tinted overlay.
