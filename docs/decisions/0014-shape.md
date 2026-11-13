# 0014 — Shape

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Corners are continuous (squircle) with about 60% smoothing. The radius is 8 for controls and 16 for overlays and panels.
  - The one exception is the window frame on Windows, which uses the native 8 px rounded corners ([0022](0022-window-layout.md)).
  - Smoothing is theme-wide only (`CornerSmoothing`), not set per element.
  - They are drawn by a `SquircleBorder` in the Style tier ([0004](0004-tier-names.md)), which clips its content to the shape.
  - Controls get templates using it as they are restyled, as described in [0003](0003-restyle-fluent.md).
- **Why:** It gives the softer, macOS-like shape. Avalonia has no continuous corners built in, and no maintained package provides them.
