# 0020 — Catalog app

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - A desktop app in `apps/Catalog` (project `Catalog`) for browsing and trying the library live. It is built with the solution and never shipped.
  - A simple sidebar list of pages, with no tier grouping.
  - A toolbar with live theme controls: theme (System, Light, Dark), density, accent colour, reduce motion and reduce transparency.
  - Every control page shows its variants × states side by side (rest, hover, pressed, disabled, focused) next to a live row.
  - Style pages show the actual theme values.
  - It uses the library as apps do ([0017](0017-app-usage.md)).
  - The test project renders every catalog page to PNG, in light and dark, for reproducible screenshots.
  - Each new Block adds its page as part of its own step.
- **Why:** It is a place to see and try everything live, across themes and settings, while the test renders ([0009](0009-screenshots-from-tests.md)) remain the reproducible record.
