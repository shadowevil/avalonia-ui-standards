# 0016 — Motion

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Durations: Fast 100 ms (hover, press), Normal 200 ms (toggles, checks), Slow 300 ms (flyouts, dialogs). All use ease-out cubic.
  - Motion is instant when "reduce motion" is on. The library reads it from the OS once, when the theme is created (macOS accessibility settings, Windows animation setting, Linux desktop animation setting). Apps can force it through `StandardsTheme` ([0017](0017-app-usage.md)).
  - Motion is theme-controlled: the library's own templates use these values, and `StandardsTheme` sets the library's transitions, together with `ReduceMotion`. The `Motion*` keys are read-only values that apps can use for their own animations; overriding them does not retune library controls.
- **Why:** Fluent hardcodes its timings per control; shared values keep motion consistent.
