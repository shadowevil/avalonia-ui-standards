# Avalonia UI Standards, web library

The web counterpart of the desktop library: plain HTML, CSS and JavaScript (ES modules), no framework and no build step. See [decision 0045](../docs/decisions/0045-web-library.md).

## Layout

- `style/`, `blocks/`, `modules/`, `screens/`: one folder per tier, each with `index.css` and `index.js`.
- `ui.css`, `ui.js`: entry files that load every tier.
- `catalog/`: the catalog pages.
- `tests/`: test pages (`*.test.html`) and the harness.
- `tools/`: render and test scripts (`common.py` holds their shared helpers).
- `out/`: render output (gitignored).

## Serve

    python3 -m http.server --directory web

Then open `http://localhost:8000/catalog/`.

## Render and test

Both need a locally installed Chrome (set `CHROME` to use another path) and Python 3.

    python3 web/tools/render.py [page ...]
    python3 web/tools/test.py

`render.py` writes light and dark PNGs of each page (default: every `catalog/*.html`) to `web/out/renders/`; options `--width` and `--height` (default 1280x800). `test.py` runs every `web/tests/*.test.html` and exits non-zero on any failure. A test page needs a `<pre id="results">` element and imports `tests/harness.js`.
