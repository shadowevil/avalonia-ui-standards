# 0007 — Restyle the target, not its surroundings

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - A restyle or new template changes only its target control (a Block or Module), using the values the Style tier already sets.
  - If it would drift from the set look (the Style tier's values and the existing restyles), stop before applying it. Examples: a value the Style tier doesn't have, or an existing value changed.
  - The drift is reviewed across the whole library (every place the value or look is used) and recorded as a decision. That decision is one of three: change the Style tier, keep the restyle within the set values, or a one-off exception for that control.
- **Why:** It keeps restyles small and the look consistent. A departure from the set look affects everything that shares it, so it is a whole-picture decision, not a local one.
