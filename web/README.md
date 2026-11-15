# Avalonia UI Standards, web library

The web counterpart of the desktop library: plain HTML, CSS and JavaScript (ES modules), no framework and no build step. See [decision 0045](../docs/decisions/0045-web-library.md).

## Layout

- `style/`, `blocks/`, `modules/`, `screens/`: one folder per tier, each with `index.css` and `index.js`.
- `ui.css`, `ui.js`: entry files that load every tier.
- `catalog/`: the catalog app.
- `tests/`: test pages (`*.test.html`) and the harness.
- `tools/`: render and test scripts (`common.py` holds their shared helpers).
- `out/`: render output (gitignored).

## Use in an app

Load the stylesheet and, for the theme functions, the module:

    <link rel="stylesheet" href="/path/to/web/ui.css">
    <script type="module">
      import { setTheme, setAccent, setDensity, setReduceMotion, setReduceTransparency, setGlassQuality } from "/path/to/web/ui.js";
      setAccent("#2A9D8F");          // #RRGGBB; recomputes the AA fills, text colours and wash for light and dark
      setTheme("dark");              // "system" (default), "light" or "dark"
      setDensity("compact");         // "normal" (default) or "compact"
      setReduceMotion(true);         // "system" (default), true or false
      setReduceTransparency(true);   // "system" (default), true or false; true makes glass solid
      setGlassQuality("minimal");    // "full" (default), "minimal" or "solid"
    </script>

Without script, the page follows the browser's colour scheme and reduced motion preferences, with the default accent `#007CEA`. It follows `prefers-reduced-transparency` where the browser supports it (Chromium today); elsewhere set it with `setReduceTransparency`. The settings are attributes on `<html>` (`data-theme`, `data-density`, `data-reduce-motion`, `data-reduce-transparency`), so they can also be written in the markup, as can `data-glass-quality`. The design values are custom properties named after the desktop keys (`SpacingM` is `--ui-spacing-m`); colours that differ by theme are `light-dark()` values, resolved by `color-scheme`.

Glass ([decision 0015](../docs/decisions/0015-glass.md)) is the class `ui-glass`, with `ui-squircle` for its shape. From CSS alone it draws the blur, tint and rim. Loading `ui.js` (or `style/index.js`) also turns on refraction in Chromium, with nothing to call; Safari and Firefox keep blur, tint and rim. Glass inside glass is minimal (no refraction), and a third level is solid. `setGlassQuality` (attribute `data-glass-quality` on `<html>`) is the performance switch: `"minimal"` drops refraction everywhere, `"solid"` draws every glass as its solid fill, and `"full"` brings refraction back. Reduce transparency makes glass solid whatever the quality.

Accessibility classes: `ui-focus-ring` (the focus ring on the element), `ui-focus-ring-within` (the ring on a wrapper while its content has keyboard focus), `ui-visually-hidden` (hidden on screen, still read by screen readers) and `ui-skip-link` (a skip link shown at the top left only while focused). The ring is `--ui-focus-ring-width`, `--ui-focus-ring-offset` and `--ui-focus-ring-color` (the accent; the system `Highlight` colour in forced colours), for Blocks that draw it on a part.

## Modules

The app shell (`modules/shell.css`, [decision 0022](../docs/decisions/0022-window-layout.md)) is classes on the landmark elements, with slots for the Modules:

- Sidebar ([0034](../docs/decisions/0034-sidebar.md)): `<ui-sidebar>` in the shell's `nav.ui-shell-sidebar`, with the stages (expanded, icon rail, hidden with an overlay); `createSidebarToggle()` makes the title band's sidebar button.
- Menu row ([0044](../docs/decisions/0044-menu-bar.md)): `createMenuRow(menu, { appMenu })` builds the app's menus from a plain definition; its `element` goes in `.ui-shell-menu-row`, below the title band.
- Toolbar band ([0043](../docs/decisions/0043-toolbar-band.md)): `<ui-toolbar-band>` in the toolbar band slot, with zones, groups and a "…" menu for the items that do not fit.
- Status bar ([0047](../docs/decisions/0047-status-bar.md)): `<ui-status-bar>` in `footer.ui-shell-status-bar`, with its zones, rows, fitting and `<ui-status-progress>`.

Each file's header comment shows its markup and attributes.

## Serve

    python3 -m http.server --directory web

Then open `http://localhost:8000/catalog/`.

## Catalog

`catalog/index.html` is the catalog app ([decision 0020](../docs/decisions/0020-catalog-app.md)): the app shell with one Sidebar row per page, the page in the glass panel, the page name and library version in the status bar, a menu row (Edit, View and About), and the theme controls in the toolbar band (theme, accent, density, motion, transparency and glass quality). The choices are saved in `localStorage` half a second after the last change and restored at startup. Each page has a route, `#button`.

- `catalog/pages/`: one module per page; `pages/index.js` lists them in sidebar order. A page module exports `{ id, title, icon, render }`; adding a page is its module and its entry in `pages/index.js`. `pages/ui.js` and `pages/state-grid.js` hold the shared builders (headings, captions, the variants × states grid).
- `catalog/settings.js` saves and restores the choices; `catalog/toolbar.js` builds the toolbar band's controls and `catalog/menu.js` the menus.
- The library version shown is a constant in `catalog/catalog.js`, kept equal to `<Version>` in `src/AvaloniaUiStandards/AvaloniaUiStandards.csproj`, which the static server cannot read.
- `?render` shows one page alone, as tall as its content, for the renders.

## Render and test

Both need a locally installed Chrome (set `CHROME` to use another path) and Python 3.

    python3 web/tools/render.py [page ...]
    python3 web/tools/test.py

`render.py` writes light and dark PNGs to `web/out/renders/`. By default it renders every catalog page whole, at the desktop's whole-page content widths of 1000 and 620 px, as `catalog-page-<id>-<light|dark>[-narrow].png`; `--only button,listbox` limits it to those pages. Paths given relative to `web/` (with any query, such as `tests/shell.html?size=480x320`) are rendered at `--width` and `--height` instead (default 1280x800). Besides the catalog, `tests/shell.html` shows the shell at the desktop window sizes and `tests/scroll.html` the scroll shadows and scrollbar states. `test.py` runs every `web/tests/*.test.html` and exits non-zero on any failure. A test page needs a `<pre id="results">` element and imports `tests/harness.js`.
