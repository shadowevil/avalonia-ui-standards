# 0025 — Overlay scrollbar

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - ScrollBar is a Block that overlays content and takes no layout space, even with `AllowAutoHide` off (then the thumb stays visible). There are no line buttons and no corner square.
  - Invisible at rest. While scrolling, a 3 px pill thumb shows, hiding 1 s after the last scroll.
  - Within 24 px of the bar, the thumb fades in and widens with proximity: fully shown from 12 px, 8 px wide on the bar, with a pill track of the same width behind it. The grab area is the bar's full thickness (see Sizes). Clicking the track pages.
  - The thumb and track are always full pills (radius = half the width).
  - Colours:

    | | Light | Dark |
    |---|---|---|
    | Thumb at rest | black 38% | white 38% |
    | Thumb on hover | black 52% | white 52% |
    | Thumb while dragging | black 64% | white 64% |
    | Track | black 6% | white 8% |

  - Sizes:
    - The visible pills sit 8 px (`SpacingS`) in from the scroller's edge.
    - The bar's grab area is 18 px thick (8 gap + 8 full pill + 2 inner gap), so it still reaches the edge.
    - The track and thumb stop short of the bar's ends by max(half of `CornerRadiusOverlay`, the edge gap), currently 8 px, so they clear the panel's rounded corners. Clicks in those end zones do not page.
    - Minimum thumb length 24 px.
  - Motion: fixed `MotionFast` transitions with cross-fade colours ([0016](0016-motion.md)); instant under reduce motion. Keyboard and accessibility behave as in Fluent.
- **Why:** Content gets the full width, the bar stays out of the way until needed, and it is easy to grab when it is.
