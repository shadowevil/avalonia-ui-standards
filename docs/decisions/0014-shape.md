# 0014 — Shape

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Corners are continuous (squircle) with about 60% smoothing. The radius is 8 for controls and 16 for overlays and panels.
  - The one exception is the window frame on Windows, which uses the native 8 px rounded corners ([0022](0022-window-layout.md)).
  - Smoothing is theme-wide only (`CornerSmoothing`), not set per element.
  - They are drawn by a `SquircleBorder` in the Style tier ([0004](0004-tier-names.md)), which clips its content to the shape.
  - Controls get templates using it as they are restyled, as described in [0003](0003-restyle-fluent.md).
  - Web ([0045](0045-web-library.md)): where the browser supports CSS `corner-shape` (Chromium), the class `ui-squircle` draws `--ui-corner-shape` (`superellipse(1.3)`) with its radius `--ui-squircle-radius` (default `--ui-corner-radius-control`) scaled by `--ui-corner-radius-factor` (1.18), the best fit to smoothing 0.6, within 0.07 px of the desktop outline. Elsewhere (Safari, Firefox) it is plain `border-radius` at the same radius, within 0.2 px. Why no script fallback: an exact clip path needs script per element, cuts off shadows and cannot wrap native fields, for under 0.2 px of difference.
- **Why:** It gives the softer, macOS-like shape. Avalonia has no continuous corners built in, and no maintained package provides them.
