# 0048 — Web HTML standards

- **Status:** Agreed, 2026-11-14
- **Decision:**
  - The files in `docs/web/` (semantics, accessibility, keyboard, theming, composition) are the web library's HTML standards. Every web Block, Module and Screen follows them, and reviews check against them. The target is WCAG 2.2 AA.
  - The choices they make that no other decision covers:
    - Roles: the segmented control is a `radiogroup` of `radio`s, the tree a `tree` of `treeitem`s, the sidebar a `nav` with a list whose selected row has `aria-current="page"`.
    - The toolbar band is a labelled `role="group"`, not `toolbar`: each item is its own Tab stop ([0043](0043-toolbar-band.md)), and `toolbar` would announce one Tab stop with arrow keys between items.
    - App shell landmarks: title band `<header>`, sidebar `<nav>`, content panel `<main>`, status bar `<footer>`, and the inspector, once it has a slot, `<aside>`.
    - Custom elements are autonomous (never `is=`), keep their content in the light DOM (a shadow root only where needed, exposed through `part`), and fields are form-associated.
    - Composite custom elements move focus with a roving `tabindex`, not `aria-activedescendant`. The tree also takes Home and End.
    - Layout uses logical CSS properties, so right-to-left pages mirror.
    - One Block per file in `web/blocks/`; a Block's CSS and JS use the Style tier ([0004](0004-tier-names.md)), and a composite Block (one serving one purpose with several parts, as 0004 allows) may also use Blocks built before it in the web roadmap's order, never later ones, so Blocks never depend on each other in a cycle. Strings a Block shows or speaks can be overridden.
    - Shortcuts use Cmd on macOS and Ctrl elsewhere and are declared with `aria-keyshortcuts`; `accesskey` is not used.
    - Every Block and Module has a catalog page, a render and a test page.
    - The Style tier provides `ui-focus-ring-within` (the ring on a wrapper whose descendant has keyboard focus), `ui-visually-hidden`, `ui-skip-link`, and a focus ring in the system `Highlight` colour under forced colours.
    - Render and test pages force a state with `data-state="hover"`, `"pressed"` or `"focus"` on a Block; Blocks match it beside the real pseudo-class.
    - Under forced colours, selection, the scrollbar thumb and progress use system colours too.
    - Size tokens are in px, as on desktop: browser zoom scales everything, and the browser's font-size setting is not followed.
- **Why:** Native semantics and the ARIA Authoring Practices give users of assistive technology the behaviour they expect, and the keyboard matches the desktop decisions. Roles follow what the control actually does, so the toolbar band is not announced as a toolbar it does not behave like. px keeps the web pixel-identical to the desktop at 100%, while zoom still enlarges everything.
