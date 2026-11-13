# 0030 — CheckBox, ToggleSwitch and RadioButton

- **Status:** Agreed, 2026-11-13
- **Decision:**
  - The three toggles are restyled Fluent ([0003](0003-restyle-fluent.md)) with templates of their own. Fluent's behaviour, pseudo-classes and named parts that its code uses (`PART_ContentPresenter`; on ToggleSwitch also `PART_SwitchKnob`, `PART_MovingKnobs`, `PART_OnContentPresenter`, `PART_OffContentPresenter`) are kept.
    - **Why:** Keyboard, click, drag and three-state behaviour stay Fluent's; only the look changes.
  - One pattern for all three: an indicator, then the label 8 px (`SpacingS`) after it. The indicator is an unchecked layer (a 1 px outline) under a checked layer (the accent fill) and its mark. Without a label the indicator stands alone, with no gap.
    - **Why:** The three read as one family, and the layering lets checking fade in while hover and press fade colours on their own timing.
  - Sizes by density ([0013](0013-spacing-and-density.md)):

    | | Normal | Compact |
    |---|---|---|
    | Row height (CheckBox, RadioButton, ToggleSwitch's switch row) | 32 | 24 |
    | CheckBox box (`SquircleBorder`), corner radius | 20, 5 | 16, 4 |
    | RadioButton circle, dot | 20, 8 | 16, 6 |
    | Check and dash stroke | 2 | 1.5 |
    | ToggleSwitch track (full pill) | 40 × 20 | 32 × 16 |
    | Knob off / on; pressed width off / on | 12 / 14; 16 / 18 | 10 / 12; 14 / 16 |

    - **Why:** The rows match the button heights. The box's radius is a quarter of its side, the proportion of `CornerRadiusControl` (8) on a 32 px button; 8 on a 20 px box would read as nearly round. This is a one-off exception for the box under [0007](0007-restyle-scope.md). The track keeps Fluent's 40 × 20 at Normal.
  - The check and dash are stroked lines with round caps and joins, drawn in the box (the check on Phosphor's `check` centre line, 0.6 of the box wide; the dash half the box wide), not the Phosphor Regular `Icons.Check` / `Icons.Minus` fills.
    - **Why:** Phosphor Regular's stroke is 16/256 of the icon, under 1 px at the 12–14 px a mark needs, and rendered too thin on the fill. A drawn stroke keeps Phosphor's shape and round ends at a weight that reads at both densities.
  - Colours ([0011](0011-colour.md)):
    - Unchecked: Fluent's own keys (`CheckBoxCheckBackgroundStroke/FillUnchecked*`, `RadioButtonOuterEllipseStroke/Fill*`, `ToggleSwitchStroke/FillOff*`). The off knob takes the outline's colour (`ToggleSwitchStrokeOff*`).
    - Checked and indeterminate: the AA accent fills (`AccentFillColor`, `…PointerOver`, `…Pressed`) with Fluent's white marks (`CheckBoxCheckGlyphForeground*`, `RadioButtonCheckGlyphFill*`, `ToggleSwitchKnobFillOn*`). The outline takes the fill's colour, so no grey edge shows around the fill.
    - Disabled and checked: the disabled accent button's colours (`AccentButtonBackgroundDisabled` fill, `AccentButtonForegroundDisabled` mark).
    - Labels: Fluent's `CheckBoxForeground*`, `RadioButtonForeground*`; ToggleSwitch's header and On/Off text use `ToggleSwitchContentForeground` and `ToggleSwitchHeaderForegroundDisabled`.
    - Contrast: the white mark meets 4.5:1 on every enabled fill. The unchecked outline in every state and the checked fill at rest meet the 3:1 non-text contrast on the page and on cards. In dark, the hover and pressed fills (shared with the accent buttons) fall below 3:1 on cards; the mark carries the state. This is tested.
    - **Why:** The checked look matches the checked ToggleButton and the selected list item ([0021](0021-button-siblings.md), [0024](0024-lists-and-scrolling.md)). Fluent's white mark on its pale disabled fill is nearly invisible (about 1.5:1); the disabled accent button's pair keeps a disabled checked toggle readable as checked.
  - ToggleSwitch:
    - Fluent's layout: the header (`Content`) above, `SpacingXS` apart, then the track and the On/Off content 8 px after it. The On and Off texts are kept.
    - The knob is 2 px larger when on, and stretches 4 px wide while pressed.
    - **Why:** The larger white knob makes the on state stronger; the stretch shows the knob is held and can be dragged.
  - Focus ring ([0019](0019-focus-ring.md)): around the indicator (box, circle or track), following its shape, not around the label.
    - **Why:** The ring follows the control's shape; the label has none.
  - Motion ([0016](0016-motion.md)): hover and press colours cross-fade (`CrossFadeBrushTransition`) and the knob's size changes over `MotionFast`; the checked layer and the mark fade over `MotionNormal`; the knob slides over `MotionNormal` with ease-out. All are fixed transition objects, empty under reduce motion.
    - **Why:** 0016 gives toggles and checks the Normal duration and hover and press the Fast one.
- **Why:** Native-feeling toggles that share the button family's shape, accent, focus ring and motion, at both densities.
