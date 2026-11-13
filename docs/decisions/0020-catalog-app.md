# 0020 — Catalog app

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - A desktop app in `apps/Catalog` (project `Catalog`) for browsing and trying the library live. It is built with the solution and never shipped.
  - A simple sidebar list of pages, with no tier grouping.
  - A toolbar with live theme controls: theme (System, Light, Dark), density, accent colour, reduce motion and reduce transparency.
  - Every control page shows its variants × states side by side (rest, hover, pressed, disabled, focused) next to a live row.
  - Style pages show the actual theme values.
  - It uses the library as apps do ([0017](0017-app-usage.md)).
  - It uses only library controls once they exist. It moves into the window of [0022](0022-window-layout.md) once the window frame, title band, content panel and status bar are done; the sidebar Module follows later.
  - The test project renders every catalog page (full height) to PNG, in light and dark.
  - The catalog has an animated ribbon backdrop, placed in `StandardsWindow`'s `Backdrop` slot ([0022](0022-window-layout.md)). It is simple ribbons undulating in place in a seamless 45 s loop (no sideways scroll), tinted from the accent, with 2–3 thin glowing edge lines per ribbon whose brightness varies smoothly along their length with dark gaps, and twinkling sparkles near the lit edges. It is driven by `StandardsWindow.FrameTick` ([0016](0016-motion.md)), so it is frozen under reduce motion and paused when the window is inactive or minimised. It is catalog-only, a deliberate exception to "uses only library controls". Outside the glass panel, a scrim in the page colour keeps text at AA: strength 0.45 in light (5.48:1 measured) and 0.64 in dark (5.36:1 measured).
  - In light, the ribbons use normal blending: accent bands at opacity 0.5, accent lines deeper than the bands with wider halos, and sparkles with a ringed halo. Additive blending washed out to white on the light backdrop, so light uses normal blending and deeper tones to keep the ribbons visible.
  - A test also drives the catalog's main window through every page and toolbar setting, as a regression guard.
  - Each new Block adds its page as part of its own step.
- **Why:** It is a place to see and try everything live, across themes and settings, while the test renders ([0009](0009-screenshots-from-tests.md)) remain the reproducible record.
