# Avalonia UI Standards, web library

The web counterpart of the desktop library: plain HTML, CSS and JavaScript (ES modules), no framework and no build step. See [decision 0045](../docs/decisions/0045-web-library.md).

## Layout

- `style/`, `blocks/`, `modules/`, `screens/`: one folder per tier, each with `index.css` and `index.js`.
- `ui.css`, `ui.js`: entry files that load every tier.
- `catalog/`: the catalog pages.
- `tests/`: test pages (`*.test.html`) and the harness.
- `tools/`: render and test scripts (`common.py` holds their shared helpers).
- `out/`: render output (gitignored).

## Use in an app

Load the stylesheet and, for the theme functions, the module:

    <link rel="stylesheet" href="/path/to/web/ui.css">
    <script type="module">
      import { setTheme, setAccent, setDensity, setReduceMotion, setReduceTransparency } from "/path/to/web/ui.js";
      setAccent("#2A9D8F");   // #RRGGBB; recomputes the AA fills, text colours and wash for light and dark
      setTheme("dark");       // "system" (default), "light" or "dark"
      setDensity("compact");  // "normal" (default) or "compact"
      setReduceMotion(true);  // "system" (default), true or false
    </script>

Without script, the page follows the browser's colour scheme and reduced motion preferences, with the default accent `#007CEA`. It follows `prefers-reduced-transparency` where the browser supports it (Chromium today); elsewhere set it with `setReduceTransparency`. The settings are attributes on `<html>` (`data-theme`, `data-density`, `data-reduce-motion`, `data-reduce-transparency`), so they can also be written in the markup. The design values are custom properties named after the desktop keys (`SpacingM` is `--ui-spacing-m`); colours that differ by theme are `light-dark()` values, resolved by `color-scheme`.

## Serve

    python3 -m http.server --directory web

Then open `http://localhost:8000/catalog/`.

## Render and test

Both need a locally installed Chrome (set `CHROME` to use another path) and Python 3.

    python3 web/tools/render.py [page ...]
    python3 web/tools/test.py

`render.py` writes light and dark PNGs of each page (default: every `catalog/*.html`) to `web/out/renders/`; options `--width` and `--height` (default 1280x800). `test.py` runs every `web/tests/*.test.html` and exits non-zero on any failure. A test page needs a `<pre id="results">` element and imports `tests/harness.js`.
