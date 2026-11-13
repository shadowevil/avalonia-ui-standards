# 0039 — Separator and GridSplitter

- **Status:** Agreed, 2026-11-13
- **Decision:**
  - Separator is a Block with its own template ([0003](0003-restyle-fluent.md)): a 1 px hairline in the border colour (`BaseLow`, [0011](0011-colour.md)), stretched across its container, with `SpacingXS` above and below and no side margin. It is not focusable.
    - **Why:** `BaseLow` is the palette's colour for borders and lines, so a separator matches the window frame and other borders. Fluent draws it in the darker `BaseMediumLow` with a 12 px side inset meant for menus; outside a menu the inset belongs to the layout, not the line.
  - A Separator placed directly in a menu, context menu, menu flyout or submenu is inset `SpacingS` from the sides, so with the menu's `SpacingXS` padding ([0036](0036-menus-and-flyouts.md)) the line starts 12 px in from the menu's edge.
    - **Why:** It stops short of the overlay's rounded corners and sits within the column of the item pills, as menu separators do on macOS and Windows 11.
  - GridSplitter is a Block with its own template. The handle is transparent and invisible at rest, at least `SpacingS` (8 px) thick. On hover a pill grip, 4 × 24 px, shows centred along the handle: upright between columns, level between rows.
    - **Why:** The split between two panes is already visible from the panes, so a permanent bar only adds noise. The grip appears the way the overlay scrollbar's thumb does ([0025](0025-overlay-scrollbar.md)), so a draggable edge reads the same everywhere. 8 px is easy to grab while staying narrow in layout.
  - Grip colours are the scrollbar thumb's: the hover colour on hover (black or white 52%) and the drag colour while dragging (64%). The grip fades in and out with the scrollbar's fixed `MotionFast` transitions, instant under reduce motion ([0016](0016-motion.md)).
    - **Why:** No new values; the grip and the scrollbar thumb are the same kind of handle.
  - Keyboard focus shows the grip with the focus ring ([0019](0019-focus-ring.md)) around the grip, 2 px out with a 2 px ring, and raises the handle above the panes beside it. Keyboard resizing is Fluent's (arrow keys).
    - **Why:** The ring follows the visible shape, as on every Block; around the invisible handle it would outline nothing.
  - Disabled: the grip never shows and the handle takes no input.
  - While dragging with `ShowsPreview`, the preview is the grip in its drag colour.
- **Why:** Separators and splitters stay quiet structural lines that use only the existing border and scrollbar values.
