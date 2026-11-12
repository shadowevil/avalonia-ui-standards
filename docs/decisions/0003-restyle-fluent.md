# 0003 — Restyle Fluent, not a theme from scratch

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - A control Fluent provides is restyled, not rewritten. New templates are written for controls Fluent lacks (for example Dialog). The look comes from the values in the Style tier.
  - A control's template may be replaced with the library's own to apply the shape ([0014](0014-shape.md)) and the material ([0015](0015-glass.md)). Behaviour stays Fluent's, and Fluent's look is still the base.
- **Why:** It keeps Fluent's accessibility and keyboard behaviour while the Style tier defines the look.
