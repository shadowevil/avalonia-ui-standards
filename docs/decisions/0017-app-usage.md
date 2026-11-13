# 0017 — How apps use the library

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Apps replace `<FluentTheme/>` with `<StandardsTheme/>` and call `.UseUiStandards()` on the `AppBuilder`, which registers Inter ([0012](0012-type.md)).
  - Style tier resource keys use plain names in the style of Avalonia's own keys, for example `FontSizeBody`, `SpacingM`, `CornerRadiusControl`, `MotionFast`.
  - Key families: `FontSize*`, `FontWeight*`, `Spacing*`, `CornerRadius*`, `CornerSmoothing`, `Motion*`, `Glass*`, `AccentFill*`, `AccentText*`, `DangerColor*`, `DangerFill*`, `DangerForeground`.
  - `StandardsTheme` properties: `Accent`, `Density`, `ReduceMotion`, `ReduceTransparency`.
- **Why:** Adopting the library takes one line, and the keys read like Avalonia's.
