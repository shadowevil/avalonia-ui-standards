# 0035 — TreeView

- **Status:** Proposed, 2026-11-13
- **Decision:**
  - TreeView and TreeViewItem are restyled Fluent Blocks ([0003](0003-restyle-fluent.md), [0004](0004-tier-names.md)). Selection and keyboard stay Fluent's: Up and Down move between visible rows; Right expands a collapsed item, then moves to its first child; Left collapses an expanded item, then moves to its parent; Enter toggles; double-clicking a row toggles it.
    - **Why:** Fluent's tree keyboard model is the Windows and macOS outline model; only the look changes.
  - The TreeView is transparent with padding 4 (`SpacingXS`) and no focus visual of its own, as the ListBox ([0024](0024-lists-and-scrolling.md)).
  - Each item's row is the list item design of [0024](0024-lists-and-scrolling.md): a squircle (`CornerRadiusControl`) inset 4 from the tree's edges, 32 high at Normal and 24 at Compact, list-medium hover, base-medium-low press, the accent fill with white text when selected, the focus ring ([0019](0019-focus-ring.md)). Rows are the tree's full width at every level; only their content is indented.
    - **Why:** One row design for lists, drop-downs, sidebars and trees, and a selection that reads as one aligned column of pills, as in macOS outline views and Fluent's TreeView.
  - Hover and press are the row's own, not the item's: an item's bounds include its children's rows, so hovering or pressing a child does not light its parent.
  - Row content: the indent, the caret, then the header `SpacingXS` after it. Padding 4 left and 12 right at Normal, 2 and 8 at Compact; the caret's own box already insets its glyph, so the glyph lands near the list items' 12 px text inset.
  - The indent is one caret width per level: 20 px at Normal, 16 at Compact (the Icon sizes; [0026](0026-icon.md)). A child's caret starts where its parent's caret ends.
    - **Why:** On the 4 px grid, proportional to the row at each density, and each level steps by exactly one caret, so the hierarchy reads without lines.
  - The caret is `Icons.CaretRight` in an `Icon` at the density size, in the row's text colour (white on the selected row). It turns to point down when expanded, over `MotionNormal` with the motion easing, instantly under reduce motion ([0016](0016-motion.md)). It is not a Tab stop. Items without children keep the caret's room, empty, so sibling headers line up.
    - **Why:** The turn shows the change of state without moving anything; `MotionNormal` is the duration for toggles ([0016](0016-motion.md)).
  - No connecting lines between levels.
    - **Why:** The clean list design: indentation and the caret carry the hierarchy, as in macOS and Fluent.
  - Children appear and disappear at once on expand and collapse; only the caret animates.
- **Why:** The tree reuses the list's row, shape, colours and motion, so it fits beside every other list in the library.
