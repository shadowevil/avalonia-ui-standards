# Web roadmap

Milestones for the web library of [0045](../decisions/0045-web-library.md), built tier by tier under [0004](../decisions/0004-tier-names.md). Each milestone's contents are agreed before work starts.

1. **W1 Skeleton:**
   1. The `web/` layout: one folder per tier (`style/`, `blocks/`, `modules/`, `screens/`), the catalog in `web/catalog/`, and one entry stylesheet and one entry module that load every tier.
   2. Serving: every page runs from a static file server (for example `python3 -m http.server`), with no build step.
   3. Renders: a script renders pages headlessly to PNG with a locally installed Chromium browser, in light and dark, the web counterpart of [0009](../decisions/0009-screenshots-from-tests.md). Render output is gitignored, added to the ignore rules of [0006](../decisions/0006-tooling-ignores.md).
   4. Tests: plain JavaScript test pages for behaviour (keyboard, theme switches, accent derivation), run headlessly by the same script.
   5. Browsers, markup and naming follow [0046](../decisions/0046-web-browsers-markup-naming.md).
2. **W2 Style tier and HTML standards:**
   1. The design values as CSS custom properties, one for each desktop key ([0017](../decisions/0017-app-usage.md)) named by [0046](../decisions/0046-web-browsers-markup-naming.md): colour with the light and dark variants and the AA-derived accent, danger, text and wash values recomputed in JavaScript for any app accent ([0011](../decisions/0011-colour.md)), type ([0012](../decisions/0012-type.md)), spacing and density ([0013](../decisions/0013-spacing-and-density.md)), shape ([0014](../decisions/0014-shape.md)) and motion ([0016](../decisions/0016-motion.md)).
   2. The squircle ([0014](../decisions/0014-shape.md)) and the focus ring outside it ([0019](../decisions/0019-focus-ring.md)), and the glass material ([0015](../decisions/0015-glass.md)) with its quality tiers and solid fallback. How close CSS gets to each, and where it falls short, is shown in renders beside the desktop ones and recorded.
   3. The theme entry: theme (system, light, dark), accent, density, reduce motion and reduce transparency, each following the browser's media query by default and settable by the app.
   4. The typeface: how Inter reaches the browser.
   5. HTML standards: semantics, accessibility (roles, names, contrast), keyboard, theming, and how Blocks, Modules and Screens compose. One topic per file in `docs/web/`.
3. **W3 Shell and the Blocks the roadmap page needs:**
   1. The window as a web app shell ([0022](../decisions/0022-window-layout.md)): title band, backdrop wash, `Backdrop` slot, glass content panel, status bar and sidebar slot, with the narrow-window stages. Window buttons are left out by default, since the browser owns the window; how and whether they appear is agreed at this step.
   2. Button and its siblings ([0018](../decisions/0018-button.md), [0021](../decisions/0021-button-siblings.md)).
   3. Icon, from the desktop library's Phosphor path data ([0026](../decisions/0026-icon.md)).
   4. ListBox items and the scroll-edge shadow ([0024](../decisions/0024-lists-and-scrolling.md)), and the overlay scrollbar as far as CSS allows ([0025](../decisions/0025-overlay-scrollbar.md)).
   5. ProgressBar and ProgressRing ([0031](../decisions/0031-slider-and-progress.md)).
4. **W4 Roadmap page:** a page that shows a roadmap's milestones, steps and statuses from a JSON file, built with the library as its first real use.
5. **W5 Catalog** ([0020](../decisions/0020-catalog-app.md)): `web/catalog/`, with one page per Style area and Block in the desktop catalog's structure (variants × states beside a live row, Style pages showing the actual values). It starts with the Style pages and the W3 Blocks; each later Block adds its page. Its theme controls move into the toolbar band once the toolbar band and segmented control exist.
6. **W6 Blocks**, in the desktop order (M3 in the [roadmap](roadmap.md)): TextInput, ComboBox, CheckBox, ToggleSwitch and RadioButton, segmented control, TreeView, Slider, NumericUpDown and ButtonSpinner, AutoCompleteBox, tabs and Expander, menus and flyouts, ToolTip and notification cards, the pickers and Calendar, Separator and GridSplitter, DataGrid, ColorPicker, and the accent flag. Each gets a catalog page.
7. **W7 Modules:** the Sidebar with its collapse stages ([0034](../decisions/0034-sidebar.md)), the toolbar band ([0043](../decisions/0043-toolbar-band.md)) and the menu bar (placement agreed at that step), then the desktop M4 Modules as each is agreed.
8. **W8 First Screen:** the app shell ([0022](../decisions/0022-window-layout.md)).

## Open questions

- How Inter is shipped: a font file in the repo, or the system font stack when Inter is absent (W2).
- How the web library is versioned and distributed beside the desktop version ([0023](../decisions/0023-versioning.md)).
