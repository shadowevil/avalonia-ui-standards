# Web keyboard

Keyboard rules for the web library. Native elements keep the browser's keyboard behaviour untouched ([semantics.md](semantics.md)). For custom elements the keys are the desktop decisions', linked below; this file adds only what the web needs.

These standards are recorded in [0048](../decisions/0048-web-html-standards.md).

## Tab order

- Tab order is the DOM order, which is also the visual order: `tabindex` is `0` or `-1` only, never positive, and focusable content is not reordered with CSS (`order`, grid placement, `*-reverse` directions).
- A skip link, the class `ui-skip-link` (`<a href="#main">`, the first focusable element, shown on focus), jumps past the title band, toolbar and sidebar to `<main>`.
- Content a user cannot reach is out of the Tab order ([accessibility.md](accessibility.md)).
- The toolbar band's zones are written in visual order (leading, centre, trailing, then "…"), so the DOM order gives the one Tab stop per item of [0043](../decisions/0043-toolbar-band.md).
- Window buttons are not Tab stops, and the sidebar button is ([0022](../decisions/0022-window-layout.md), [0034](../decisions/0034-sidebar.md)).

## Composite custom elements

The keys are those of the decision for each element. A composite is one Tab stop and moves focus inside by roving tabindex: the active item has `tabindex="0"`, the others `-1`, and focus moves by calling `focus()` on the item (no `aria-activedescendant`).

| Element | Keys | Tab lands on |
|---|---|---|
| Segmented control | [0032](../decisions/0032-segmented-control.md); Left and Right swap under `dir="rtl"` | the selected segment |
| Tree | [0035](../decisions/0035-treeview.md); Home and End go to the first and last visible row | the selected item, else the first |
| Sidebar | [0034](../decisions/0034-sidebar.md) | the selected row, in the list or the footer |
| Toolbar band | [0043](../decisions/0043-toolbar-band.md); each item is a Tab stop, with no roving | each item |
| Menu | [0036](../decisions/0036-menus-and-flyouts.md) | the menu button; focus moves into the opened menu |

- A composite always calls `preventDefault()` on the navigation keys it handles, so they never scroll the page around it.
- Disabled items are skipped and take no focus.

## Overlays and no keyboard traps

- Escape closes the topmost popup, menu, flyout or overlay and returns focus to the element that opened it; a `<dialog>` opened with `showModal()` does this natively. An overlay that opens gives focus to its selected or first item ([0034](../decisions/0034-sidebar.md) for the sidebar overlay).
- Focus can always leave an element with Tab and Shift+Tab (WCAG 2.1.2); only a modal `<dialog>` or an overlay holds it, and Escape closes it. A modal's background is `inert`.
- Every hover-only or drag-only function (the splitter, a resize grip) has a keyboard path: arrow keys on the focused handle.
- Native keys are never re-implemented on native controls: Enter and Space on buttons, Space on checkboxes and switches.

## Shortcuts

- A shortcut uses Ctrl or Cmd, or is a function key; a single printable key works only while its own widget has focus (WCAG 2.1.4).
- Shortcuts follow the OS the page runs on (Cmd on macOS, Ctrl elsewhere), as on desktop ([0022](../decisions/0022-window-layout.md)), and never override the browser's own (Ctrl or Cmd with T, W, L, F, R).
- A shortcut is declared with `aria-keyshortcuts` and shown in its menu item or tooltip. `accesskey` is not used: it clashes with browser and screen reader keys.
