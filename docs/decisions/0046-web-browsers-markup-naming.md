# 0046 — Web browsers, markup and naming

- **Status:** Agreed, 2026-11-14
- **Decision:**
  - Browsers: the current versions of Chrome and Edge (Chromium), Safari and Firefox. Chromium is the reference: there the look matches the desktop library as closely as CSS allows, using CSS `corner-shape` for the squircle and SVG refraction in `backdrop-filter` for glass. Safari and Firefox get fallbacks (rounded corners at the same radius, see [0014](0014-shape.md); glass without refraction). Each gap is recorded in the decision for that look.
  - Markup: a Block is a styled native element wherever one fits (`<button>`, `<input>`, `<select>`, `<dialog>`, `<progress>`), with classes. A Block or Module is a custom element only where no native element fits (for example the segmented control, the tree, the sidebar).
  - Naming, with the prefix `ui-` and kebab-case:
    - Custom properties take the desktop key's words ([0017](0017-app-usage.md)): `SpacingM` is `--ui-spacing-m`, `AccentFillColor` is `--ui-accent-fill-color`.
    - Classes: `ui-` plus the Block name, with variants as further classes, for example `class="ui-button ui-accent"`.
    - Custom elements: `ui-` plus the name, for example `<ui-segmented-control>`.
    - Theme settings are attributes on `<html>`: `data-theme`, `data-density`, `data-reduce-motion`, `data-reduce-transparency`.
- **Why:** The library works for everyone in the three engines while showing the full desktop look where the engine can draw it. Native elements keep the browser's accessibility, keyboard and form behaviour without rebuilding them in script. Names that follow the desktop keys let a value be found in either library, and the short prefix keeps the library's names apart from a page's own CSS.
