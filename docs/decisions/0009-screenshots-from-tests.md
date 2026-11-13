# 0009 — Screenshots rendered by tests

- **Status:** Agreed, 2026-11-12
- **Decision:** Controls and Style values are rendered headlessly in tests (Avalonia headless) and saved as PNG under the test output, which is gitignored ([0006](0006-tooling-ignores.md)). Review screenshots stay reproducible because they come from tests; the catalog app for live browsing is in [0020](0020-catalog-app.md). Tests use xUnit v3 with Avalonia.Headless.XUnit (`[AvaloniaFact]`), rendering through Avalonia.Skia.
  - Tests run on Microsoft.Testing.Platform, set in `global.json`, which holds only the runner setting.
- **Why:** Every rendered look comes from the same code the tests check, and a render can be reproduced by anyone who runs the tests. xUnit is the framework Avalonia's headless docs lead with.
