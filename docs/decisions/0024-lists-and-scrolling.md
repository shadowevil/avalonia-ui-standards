# 0024 — ListBox, ScrollViewer and ComboBox drop-down

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - ListBox: padding 4 (`SpacingXS`), transparent background.
  - ListBoxItem and ComboBoxItem share one item design:
    - Inset 4 from the list edges.
    - `SquircleBorder` with radius 8 (`CornerRadiusControl`).
    - Height 32 Normal, 24 Compact.
    - Hover uses `ListMedium` and pressed is one step stronger (`BaseMediumLow`), so hover and pressed always differ visibly from the list, card and drop-down surfaces in both themes. This is tested.
    - Selected uses the accent fill (`AccentFill*`) with white text, meeting AA in every selected state.
    - The focus ring ([0019](0019-focus-ring.md)); a focused item raises itself so its ring is not covered.
  - ComboBox drop-down: a squircle popup (`CornerRadiusOverlay`) that clips its content, with padding 4 and Fluent's drop-down background and border. The closed field is designed in [0028](0028-combobox.md).
  - ScrollViewer:
    - Every ScrollViewer shows a 12 px soft shadow (a gradient from a dark tint of the accent to transparent: the accent's hue and saturation at lightness 0.35 in light or 0.12 in dark, at 8% opacity in light or 18% in dark, recomputed when the accent changes) on each edge with content more than 0.5 px out of view.
    - It fades with the fixed transitions ([0016](0016-motion.md)) and is clipped to the viewport.
    - It is turned off per scroller with the public attached property `ScrollShadows.IsEnabled` (default true; a ListBox passes it to its inner scroller).
  - Reach: this includes the scrollers inside Fluent controls (for example TextBox) and the AutoCompleteBox drop-down, which uses the library items.
  - Known for later steps: the DatePicker and TimePicker flyout items still overrun their rounded corners.
- **Why:** One item design for lists and drop-downs, selection matching the accent buttons, and clear cues for hidden content.
