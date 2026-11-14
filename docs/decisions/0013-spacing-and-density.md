# 0013 — Spacing and density

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - A 4 px grid with named steps: XS 4, S 8, M 12, L 16, XL 24, XXL 32, XXXL 48.
  - Fluent density Normal is the default; apps can switch to Compact.
  - Fluent's own off-grid paddings are replaced as each control is restyled ([0007](0007-restyle-scope.md) applies per control).
  - Web ([0045](0045-web-library.md)): the steps are `--ui-spacing-*`. `data-density="compact"` on `<html>` switches the shared control values `--ui-control-min-height`, `--ui-control-padding` and `--ui-icon-size`, the desktop's private per-control values that more than one Block uses; each Block and Module adds its own density values with it.
- **Why:** The rhythm stays consistent, and Fluent itself is close to a 4 px grid.
