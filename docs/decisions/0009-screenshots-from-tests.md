# 0009 — Screenshots rendered by tests

- **Status:** Agreed, 2026-11-12
- **Decision:** Controls and Style values are rendered headlessly in tests (Avalonia headless) and saved as PNG under the test output, which is gitignored ([0006](0006-tooling-ignores.md)). There is no separate gallery or sample app. Tests use xUnit v3 with Avalonia.Headless.XUnit (`[AvaloniaFact]`), rendering through Avalonia.Skia.
- **Why:** Every rendered look comes from the same code the tests check, and no extra app has to be maintained. xUnit is the framework Avalonia's headless docs lead with.
