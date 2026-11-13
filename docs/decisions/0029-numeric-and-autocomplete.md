# 0029 — NumericUpDown, ButtonSpinner and AutoCompleteBox

- **Status:** Agreed, 2026-11-13
- **Decision:**
  - NumericUpDown and ButtonSpinner are TextInput fields ([0027](0027-text-input.md)): the `SquircleBorder` chrome `PART_BorderElement`, the density height and padding, a 1 px border, Fluent's TextControl colours, `DangerColor` on error, and the field buttons. Both get the library's own templates with Fluent's part names (`PART_Spinner`, `PART_TextBox`, `PART_SpinnerPanel`, `PART_IncreaseButton`, `PART_DecreaseButton`, `PART_ContentPresenter`).
    - **Why:** Every field lines up and looks the same; behaviour stays Fluent's ([0003](0003-restyle-fluent.md)).
  - Focus inside the field (the NumericUpDown's TextBox, or a ButtonSpinner and its content) turns the border accent with TextInput's focused fill; keyboard focus inside shows the ring around the whole field ([0019](0019-focus-ring.md), tracked as in [0028](0028-combobox.md)). The NumericUpDown's TextBox is the borderless inner TextBox of [0027](0027-text-input.md).
    - **Why:** It reads as one text field with buttons, not a text box inside a frame.
  - The spin buttons are `Icons.CaretUp` (increase) and `Icons.CaretDown` (decrease), side by side at the trailing end in Fluent's order, as field buttons: 24 px at Normal and 20 px at Compact, inset 4 or 2 from the field's edge, 16 px icons, the Subtle button colours, and the disabled colour when the value is at its limit. With `ButtonSpinnerLocation.Left` they sit at the leading end.
    - **Why:** Carets rather than plus and minus: a ButtonSpinner spins any content (a list of sizes, not only numbers), and the carets match the Up and Down keys, which also spin. Side by side rather than stacked: two stacked buttons in a 32 px field would be 14 px tall targets (10 px at Compact). The field-button size and inset are shared with the clear and reveal buttons.
  - The spin buttons are not tab stops, but they take focus when clicked, as in Fluent.
    - **Why:** The arrow keys spin from the field, so tab stops on the buttons would only add steps. Taking focus on click makes a NumericUpDown commit typed text before it spins, as Fluent does.
  - A NumericUpDown holds the field states (hover, focus within, error, disabled) and passes its colours to its ButtonSpinner, whose template it shares with a standalone ButtonSpinner.
    - **Why:** Hovering a spin button still counts as hovering the field, and errors and disabled states set on the NumericUpDown reach the chrome.
  - AutoCompleteBox keeps Fluent's template, with TextInput's density height and padding; its TextBox is a TextInput, so its colours, focus and ring are TextInput's. The suggestions use the ComboBox drop-down of [0024](0024-lists-and-scrolling.md): a `SquircleBorder` (`CornerRadiusOverlay`, Fluent's drop-down background and border) that clips the library ListBox, whose 4 px padding insets the item squircles.
    - **Why:** It is TextInput with suggestions; the suggestion list matches the ComboBox list.
  - Fades are `MotionFast` cross-fades ([0016](0016-motion.md)), instant under reduce motion.
- **Why:** The remaining text-entry fields join the field family with the fewest new parts: TextInput's chrome and field buttons, and the ComboBox drop-down.
