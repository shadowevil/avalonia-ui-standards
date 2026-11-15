# Web composition

How the web library's pieces are laid out and fit together ([0045](../decisions/0045-web-library.md), [0004](../decisions/0004-tier-names.md), [0046](../decisions/0046-web-browsers-markup-naming.md)).

These standards are recorded in [0048](../decisions/0048-web-html-standards.md).

## Tiers and dependencies

- Tiers are the desktop's: Style, Blocks, Modules, Screens ([0004](../decisions/0004-tier-names.md)). A tier uses only the tiers below it: Blocks use Style; Modules use Blocks and Style; Screens use any lower tier. A composite Block may also use Blocks earlier in this dependency order, never later ones ([0048](../decisions/0048-web-html-standards.md)): icon, button, scroll, list-box, progress, text-input, toggles, segmented-control, slider, tabs, expander, flyout, menu, tooltip, notification, separator, grid-splitter, combobox, button-spinner, numeric-up-down, auto-complete-box, tree-view, calendar, date-picker, time-picker, calendar-date-picker. A new Block is added to the order after the Blocks it uses.
- The rule holds in CSS (a Block's file references `--ui-*` Style tokens and Style classes such as `ui-squircle`, and another Block's classes only where it is a composite using an earlier Block) and in JavaScript (a Block's module imports Style modules and, for a composite, earlier Blocks' modules; a Module imports Blocks and Style). A composite sets the parts it uses through their custom properties and classes, as a Module does, and does not copy their CSS.
- The web library depends on nothing outside itself: no framework, no runtime dependency, no build step ([0045](../decisions/0045-web-library.md)). Every file is standard CSS or an ES module that a static file server delivers.
- Features outside the browsers' current versions ([0046](../decisions/0046-web-browsers-markup-naming.md)) are used only with a fallback that keeps the Block working (`@supports` for `corner-shape`, [0014](../decisions/0014-shape.md)).

## File layout

- One folder per tier under `web/`: `style/`, `blocks/`, `modules/`, `screens/`, each with `index.css` and `index.js`; `ui.css` and `ui.js` load every tier ([web/README.md](../../web/README.md)).
- A Block or Module has files named for it in its tier folder, in kebab-case: `blocks/button.css`, and `blocks/segmented-control.js` where it needs script. A native-element Block with no behaviour has CSS only.
- `index.css` imports each file of the tier; `index.js` imports each module of the tier and re-exports what apps call. Adding a Block means adding its files and one line in each index.
- Catalog pages are in `web/catalog/`, test pages in `web/tests/`, render and test scripts in `web/tools/`.
- A file holds one Block. CSS custom properties that more than one Block uses belong to Style ([0013](../decisions/0013-spacing-and-density.md) states which for density).

## Naming

- Custom properties `--ui-` plus the desktop key's words; classes `ui-` plus the Block name with variants as further classes; custom elements `ui-` plus the name; theme attributes `data-*` on `<html>` ([0046](../decisions/0046-web-browsers-markup-naming.md)).
- Parts and states of a Block are classes or attributes scoped by the Block: `ui-button`, `ui-button-icon`; a state uses the native or ARIA attribute where one exists ([accessibility.md](accessibility.md)), else a `data-` attribute.
- Block CSS selectors are flat (a class, with state and pseudo-class additions) and low in specificity, wrapped in `:where()` where a page needs to override them.
- The library defines custom elements and CSS only under the `ui-` prefix; it never styles bare element selectors other than the page defaults in `base.css`.

## Shape and focus in a Block

- A Block that has a shaped box takes the `ui-squircle` class ([0014](../decisions/0014-shape.md)): in markup (`class="ui-button ui-squircle"`). Its radius is `--ui-squircle-radius`, which the Block sets to its decision's radius (a Style token such as `--ui-corner-radius-control`).
- A Block that takes keyboard focus uses the class `ui-focus-ring` in markup, on the element that takes focus ([0019](../decisions/0019-focus-ring.md)). The ring is an `outline` on `:focus-visible`, so it follows the squircle and the Block's own `overflow` does not clip it.
- A composite Block that rings the whole control (where its decision says so) puts `ui-focus-ring-within` on its wrapper instead; it draws the same outline when a descendant matches `:has(:focus-visible)`. The class is part of the Style tier.

## Composing Blocks in Modules

- A Module's markup contains Block elements and their classes; it does not copy a Block's CSS. To change a Block inside a Module, it sets the Block's own custom properties on the Module's class, or adds a descendant class.
- A Module's custom element holds Blocks in its light DOM as the author wrote them, adding classes and attributes in script only to wire behaviour (roving tabindex, ARIA state).
- A Module defines layout and behaviour; its colours, spacing and radii still come from Style tokens ([theming.md](theming.md)).
- Modules are used by Screens, which arrange them without adding styles to a Block's internals.

## Tests and renders

- Every Block and Module has, before it is done:
  - A catalog page in `web/catalog/` with its variants and states ([0020](../decisions/0020-catalog-app.md)), rendered to PNG in light and dark by `web/tools/render.py` ([0009](../decisions/0009-screenshots-from-tests.md)).
  - A test page `web/tests/<name>.test.html` run by `web/tools/test.py`, covering behaviour: keyboard keys and focus movement ([keyboard.md](keyboard.md)), roles and states ([accessibility.md](accessibility.md)), theme and density switches, and contrast of its text and fill token pairs.
- The tests and renders run from the static server, with no build step ([web/README.md](../../web/README.md)).
