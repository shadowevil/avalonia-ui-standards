# 0016 — Motion

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Durations: Fast 100 ms (hover, press), Normal 200 ms (toggles, checks), Slow 300 ms (flyouts, dialogs). All use ease-out cubic.
    - Menus and flyouts fade in over Fast; see [0036](0036-menus-and-flyouts.md).
  - Motion is instant when "reduce motion" is on. The library reads it from the OS once, when the theme is created (macOS accessibility settings, Windows animation setting, Linux desktop animation setting). Apps can force it through `StandardsTheme` ([0017](0017-app-usage.md)).
  - Motion is theme-controlled: the library's own templates use these values, and `StandardsTheme` sets the library's transitions, together with `ReduceMotion`. The `Motion*` keys are read-only values that apps can use for their own animations; overriding them does not retune library controls.
  - Colour and brush fades are true cross-fades that take transparency into account (`CrossFadeBrushTransition`), so a fade never passes through a lighter or darker colour than its two ends (for example, no white flash when fading from transparent). This applies to the button family, list items and window buttons, and is tested.
  - Frame rate:
    - Continuous animation (anything animating on its own, for example a window `Backdrop` and the glass re-rendering over it) runs at 30 fps by default, through the `StandardsWindow.FrameTick` event. Its `FrameTickEventArgs` carry `Time` (the frame's time on the window's animation clock) and `Elapsed` (since the previous tick, zero after a start or resume).
    - The app developer opts in, in code, to the display rate (`StandardsWindow.HighRefreshRate`, default false, or a `StandardsTheme` default for the whole app). There is no end-user setting.
    - Interactive rendering (scrolling, hover and press fades) runs at the display rate, because Avalonia drives it and exposes no cap.
    - Continuous animation stops under reduce motion and pauses when the window is inactive or minimised.
- **Why:** Fluent hardcodes its timings per control; shared values keep motion consistent. Avalonia renders only when something changes, so capping continuous animation removes most of the ongoing cost.
