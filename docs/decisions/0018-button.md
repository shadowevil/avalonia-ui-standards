# 0018 — Button

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Variants:
    - Standard: no class.
    - Accent: class `accent`.
    - Subtle: class `subtle`, no fill until hover.
    - Destructive: class `destructive`, danger colour with white text.
  - Size by density ([0013](0013-spacing-and-density.md)):
    - Normal: height 32 (`MinHeight`), padding 12,4,12,4 (`SpacingM` on the sides, `SpacingXS` top and bottom), replacing Fluent's 8,5,8,6.
    - Compact: height 24, padding 8,2,8,2 (`SpacingS` on the sides).
    - The label is vertically centred.
  - The chrome is a `SquircleBorder` (part `PART_Chrome`) with `CornerRadiusControl` 8 ([0014](0014-shape.md)). Fluent's `PART_ContentPresenter` is kept for the content.
  - Standard state colours come from Fluent's own Button keys, and Subtle reuses Standard's hover and pressed colours. Accent uses `AccentFillColor`, `AccentFillColorPointerOver` and `AccentFillColorPressed`. Destructive uses `DangerFillColor`, `DangerFillColorPointerOver` and `DangerFillColorPressed`, with `DangerForeground` (white) for its text ([0011](0011-colour.md)). Accent and Destructive borders and the disabled state use Fluent's AccentButton keys, and Accent's text is Fluent's AccentButton foreground.
  - Contrast: filled variants (Accent, Destructive) meet WCAG AA ([0011](0011-colour.md)); Standard and Subtle text also meets 4.5:1.
  - Background and border brushes fade over `MotionFast`, as does Fluent's 0.98 pressed scale (kept), with `MotionEasing` ([0016](0016-motion.md)).
- **Accepted consequence:** App styles that target `PART_ContentPresenter`'s background no longer apply, because the chrome is `PART_Chrome`.
- **Why:** It keeps Fluent's behaviour and state model, adds the agreed shape, grid and motion, and covers neutral, primary, low-emphasis and dangerous actions.
