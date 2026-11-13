# 0019 — Focus ring

- **Status:** Agreed, 2026-11-12
- **Decision:** Keyboard focus shows a 2 px accent ring with a 2 px gap, drawn outside the control's squircle shape ([0014](0014-shape.md)), on every Block. The ring is drawn in the control's template, so the control's clipping ancestors clip it too; containers that clip need about 4 px of padding for the ring to show in full.
- **Why:** It is clear in both themes, tied to the accent, and follows the shape.
