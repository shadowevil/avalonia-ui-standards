# 0045 — Web library in plain HTML, CSS and JavaScript

- **Status:** Agreed, 2026-11-14. Settles the web question of [0002](0002-desktop-first.md).
- **Decision:**
  - The web side is a second library, separate from the desktop one, in plain HTML, CSS and JavaScript: standard ES modules and CSS, no framework, no runtime dependencies and no build step. It runs from a static file server.
  - It lives in `web/` in this repository. It shares `docs/`: the design decisions in `docs/decisions/` apply to both libraries, and a decision that needs a web-specific choice states it in the same file. The web milestones are in their own [web roadmap](../plan/web-roadmap.md).
  - It uses the same tiers and dependency rule ([0004](0004-tier-names.md)): the Style tier as CSS custom properties named after the desktop keys, then Blocks, Modules and Screens.
  - It looks the same as the desktop library: the same colours, type, spacing, squircle shapes, focus ring, glass and window layout, as a web app shell. Where CSS cannot match a desktop look, the decision for that look says where and how it differs.
  - It has HTML standards of its own for semantics, accessibility, keyboard, theming (light and dark, density, reduce motion and reduce transparency) and how the pieces compose, written in `docs/web/` at W2 of the web roadmap.
- **Why:** A web page built from native HTML elements keeps what browsers give for free: accessibility, text selection, find in page, zoom, autofill and fast loading. Avalonia's WASM target draws everything on a canvas and needs a large download. With no framework and no build step, the library has nothing to upgrade but the browsers, and any web stack can use it. Shared decisions keep the two libraries visually one system.
