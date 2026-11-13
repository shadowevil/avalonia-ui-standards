# 0017 — How apps use the library

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Apps replace `<FluentTheme/>` with `<StandardsTheme/>` and call `.UseUiStandards()` on the `AppBuilder`, which registers Inter ([0012](0012-type.md)).
  - Style tier resource keys use plain names in the style of Avalonia's own keys, for example `FontSizeBody`, `SpacingM`, `CornerRadiusControl`, `MotionFast`.
  - Key families: `FontSize*`, `FontWeight*`, `Spacing*`, `CornerRadius*`, `CornerSmoothing`, `Motion*`, `Glass*`, `BackdropWash*`, `AccentFill*`, `AccentText*`, `DangerColor*`, `DangerFill*`, `DangerForeground`.
  - App windows subclass `StandardsWindow` ([0022](0022-window-layout.md)), which has a public `Backdrop` content slot (empty by default).
  - The attached property `ScrollShadows.IsEnabled` turns the scroll-edge shadow off per scroller ([0024](0024-lists-and-scrolling.md)).
  - Planned, M3 step 5b: `StandardsWindow.HighRefreshRate` (default false) opts a window in to the display rate; `StandardsTheme` has a default for it app-wide ([0016](0016-motion.md)).
  - `StandardsWindow.ContentPanelBounds` (read-only) is the glass panel's rectangle in the `Backdrop` slot's coordinates ([0022](0022-window-layout.md)).
  - `StandardsWindow.IsContentScrollable` (default true) makes the glass panel scroll its content ([0022](0022-window-layout.md)).
  - `StandardsTheme` properties: `Accent`, `Density`, `ReduceMotion`, `ReduceTransparency`.
- **Why:** Adopting the library takes one line, and the keys read like Avalonia's.
