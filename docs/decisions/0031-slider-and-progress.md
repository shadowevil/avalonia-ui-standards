# 0031 — Slider, ProgressBar and ProgressRing

- **Status:** Proposed, 2026-11-13
- **Decision:**
  - Slider and ProgressBar are restyled Fluent ([0003](0003-restyle-fluent.md)) with templates of their own. Fluent's behaviour and the named parts its code uses are kept (Slider: `PART_Track`, `PART_DecreaseButton`, `PART_IncreaseButton`; ProgressBar: `PART_Indicator`).
  - Avalonia 12 has no ProgressRing, so the library adds a `ProgressRing` Block (a `RangeBase` with `IsIndeterminate`, as ProgressBar has).
    - **Why:** A bar does not fit small and inline places (inside a button, beside a label); every platform pairs the two ([Roadmap](../plan/roadmap.md) lists "ProgressBar (with ProgressRing)").
  - Shapes: the slider track and the progress bar are 4 px full pills; the ring is a circle. The track parts and the progress fill run under the thumb or are clipped by the track, so no square or flat end shows.
    - **Why:** The pill matches the overlay scrollbar ([0025](0025-overlay-scrollbar.md)) and the toggle track ([0030](0030-checkbox-toggleswitch-radiobutton.md)); 4 px is Fluent's progress height and stays a line next to text.
  - Sizes by density ([0013](0013-spacing-and-density.md)):

    | | Normal | Compact |
    |---|---|---|
    | Slider row (height, or width when vertical) | 32 | 24 |
    | Slider thumb | 20 | 16 |
    | ProgressRing (default size) | 20 | 16 |
    | Track thickness (slider, progress bar) | 4 | 4 |

    - The ring's stroke is a tenth of its size rounded to whole pixels (halves round up), at least 2 px. A ring can be any size through `Width` and `Height`.
    - **Why:** The slider row matches the button heights. The ring follows the Icon sizes ([0026](0026-icon.md)), since it sits where icons sit. The tracks are lines, not targets, so they keep one thickness.
  - Slider:
    - Value part: the AA accent fills (`AccentFillColor`, `…PointerOver`, `…Pressed`). Rest of the track: Fluent's `SliderTrackFill*`. Ticks: Fluent's `SliderTickBarFill*`, shown by `TickPlacement` as in Fluent, 4 px long and 4 px from the thumb's edge. Ticks on one side fit within the 32 / 24 px row; ticks on both sides make the slider 36 px tall at Normal and 32 px at Compact (wide, when vertical). Ticks inside the row would sit under the thumb or touch it at Compact, so they stay clear of it and the row grows instead.
    - Thumb: a white circle in both themes with a 1 px black 12 % edge and a soft shadow (0 1 3, black 25 %). It grows 10 % on hover and 20 % while pressed (`MotionFast`).
    - Disabled: value part `SystemControlForegroundBaseMediumLowBrush`, rest `SliderTrackFillDisabled`, thumb `SystemControlBackgroundBaseLowBrush`. Fluent's error state (an error-coloured thumb) is kept.
    - It stretches along its track and is centred across it. Vertical fills from the bottom; `IsDirectionReversed` is honoured.
    - The focus ring ([0019](0019-focus-ring.md)) goes around the thumb.
    - **Why:** The accent value matches the toggles; a white thumb with an edge and shadow reads as something to grab on both the light page and dark surfaces (the knob of macOS and iOS sliders, and of the on toggle). Growing on hover and press shows it is live; Fluent's disabled pair for value and rest is the same grey, so the value would vanish.
  - ProgressBar:
    - Track: Fluent's `SystemControlBackgroundBaseLowBrush`. Fill: `AccentFillColor`. Disabled fill: `SystemControlForegroundBaseMediumLowBrush`.
    - The progress text (`ShowProgressText`) sits 8 px after the bar (below a vertical bar), in Caption size and secondary text colour (`BaseMedium`).
    - Indeterminate: a segment 40 % of the track sweeps across it every 2 s with a gentle ease (half linear, half sine in-out), clipped by the track.
    - **Why:** The fill matches every other accent. A 4 px bar cannot hold text over it, as Fluent's taller template does. The gentle ease slows at the ends without the segment lingering out of view.
  - ProgressRing: track circle in ProgressBar's track colour; the accent arc with round ends runs clockwise from the top. Indeterminate: a quarter arc turns once a second. Disabled arc: `SystemControlForegroundBaseMediumLowBrush`.
  - Continuous animation ([0016](0016-motion.md)): the indeterminate bar and ring are driven by the window's frame ticks (`StandardsWindow.FrameTick`, 30 per second by default), so they stop under reduce motion and pause while the window is inactive or minimised. Blocks reach the ticks through an internal Style tier interface (`IFrameTickSource`) that `StandardsWindow` implements, so the Blocks tier does not depend on the Modules tier ([0004](0004-tier-names.md)). In a popup, the popup's parent window's ticks are used.
    - **Why:** 0016 paces all continuous animation through the window's ticks; Fluent's keyframe animations would run at the display rate and ignore reduce motion.
  - Still frame: with no ticks (reduce motion, or outside a `StandardsWindow`), the indeterminate bar shows its segment centred, and the ring its quarter arc centred on the top.
    - **Why:** A determinate bar or ring always starts at its origin; a centred segment cannot be read as an amount.
  - Motion: the slider's colours cross-fade (`CrossFadeBrushTransition`) and the thumb scales over `MotionFast`; fixed transition objects, empty under reduce motion.
  - ProgressBar and ProgressRing are not focusable (as in Fluent), so they have no focus ring.
- **Why:** Range controls that share the toggles' accent, pill shapes and motion, with progress animation that follows the library's frame rate and reduce-motion rules.
