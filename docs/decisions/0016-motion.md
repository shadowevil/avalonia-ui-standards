# 0016 — Motion

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Durations: Fast 100 ms (hover, press), Normal 200 ms (toggles, checks), Slow 300 ms (flyouts, dialogs). All use ease-out cubic.
  - Motion is instant when "reduce motion" is on. The library reads it from the OS once, when the theme is created (macOS accessibility settings, Windows animation setting, Linux desktop animation setting). Apps can force it through `StandardsTheme` ([0017](0017-app-usage.md)).
  - Motion is theme-controlled: the library's own templates use these values, and `StandardsTheme` sets the library's transitions, together with `ReduceMotion`. The `Motion*` keys are read-only values that apps can use for their own animations; overriding them does not retune library controls.
  - Colour and brush fades are true cross-fades that take transparency into account (`CrossFadeBrushTransition`), so a fade never passes through a lighter or darker colour than its two ends (for example, no white flash when fading from transparent). This applies to the button family, list items and window buttons, and is tested.
  - Frame rate (planned, M3 step 5b):
    - All of a window's rendering, interactive included, runs at 30 fps by default.
    - The app developer opts in, in code, to the display rate (`StandardsWindow.HighRefreshRate`, default false, or a `StandardsTheme` default for the whole app). There is no end-user setting.
    - Continuous animation (looping or ambient, for example a window `Backdrop` and the glass re-rendering it causes) still stops under reduce motion and pauses when the window is inactive or minimised.
- **Why:** Fluent hardcodes its timings per control; shared values keep motion consistent. The 30 fps default saves power and CPU, and apps that need smooth high-rate motion opt in.
