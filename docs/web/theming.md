# Web theming

How the theme works in the web library and how Blocks consume it ([0017](../decisions/0017-app-usage.md) Web line, [0046](../decisions/0046-web-browsers-markup-naming.md)). Code: `web/style/theme.js`, `colour.js` and the `web/style/*.css` files.

These standards are recorded in [0048](../decisions/0048-web-html-standards.md).

## Settings

Each setting is an attribute on `<html>`, written by a setter in `web/ui.js` or directly in the markup. Without it the page follows the browser.

| Setter | Attribute | Values | Default |
|---|---|---|---|
| `setTheme` | `data-theme` | `system`, `light`, `dark` | `system` (the attribute is removed) |
| `setDensity` | `data-density` | `normal`, `compact` | `normal` |
| `setReduceMotion` | `data-reduce-motion` | argument `"system"`, `true` or `false`; attribute `"true"` or `"false"` (absent for system) | system |
| `setReduceTransparency` | `data-reduce-transparency` | argument `"system"`, `true` or `false`; attribute `"true"` or `"false"` (absent for system) | system |
| `setGlassQuality` | `data-glass-quality` | `full`, `minimal`, `solid` ([0015](../decisions/0015-glass.md)) | `full` (the attribute is removed) |
| `setAccent` | custom properties on `<html>` | `#RRGGBB` | `#007CEA` ([0011](../decisions/0011-colour.md)) |

- A setter given a value outside its list throws, including the strings `"true"` and `"false"` for the two booleans; the attribute values are separate and are what markup writes.
- Setting is synchronous and takes effect without reload; a setting can change while the page is open.
- The page's own CSS and scripts read the attributes, never the setters' internals.

## Light and dark

- `color-scheme` on `:root` is the browser's preference by default and follows `data-theme` when it is `light` or `dark`. It also makes native controls, scrollbars and form fields match.
- Every colour token that differs by theme is a `light-dark()` value ([0011](../decisions/0011-colour.md)). A Block uses the token and never branches on the theme, so it needs no `@media (prefers-color-scheme)` and no `[data-theme]` selector of its own.

## Accent

- `setAccent` recomputes in JavaScript, for light and dark, the AA fill, hover and pressed values, the accent text values, the secondary text and the backdrop wash ([0011](../decisions/0011-colour.md)), and writes them as `light-dark()` custom properties. Inputs that are not `#RRGGBB` throw.
- Without script the CSS carries the values for the default accent.
- Blocks use the derived tokens (`--ui-accent-fill-color*`, `--ui-accent-text-color*`) and never the raw brand colour for text or fills that carry text.

## Density

- `data-density="compact"` switches the shared `--ui-control-min-height`, `--ui-control-padding` and `--ui-icon-size`; each Block and Module adds its own density values on the same selector ([0013](../decisions/0013-spacing-and-density.md)).
- Blocks size from these tokens, never from literal `px` heights or paddings. The tokens are in `px` ([accessibility.md](accessibility.md)).

## Reduce motion

- `--ui-motion-fast`, `--ui-motion-normal` and `--ui-motion-slow` are 0 ms when `data-reduce-motion="true"`, and when the attribute is not `"false"` and `prefers-reduced-motion: reduce` matches ([0016](../decisions/0016-motion.md)).
- Every transition and animation takes its duration from these tokens and its easing from `--ui-motion-easing`. An animation that loops (indeterminate progress) also stops, or becomes a static state, when the durations are 0.
- Script that animates reads the same tokens, or checks `data-reduce-motion` and `matchMedia("(prefers-reduced-motion: reduce)")`; it never hard-codes a duration.

## Reduce transparency

- `--ui-glass-solid` is 1 when `data-reduce-transparency="true"`, and when the attribute is not `"false"` and `prefers-reduced-transparency: reduce` matches; glass then draws its solid fill ([0015](../decisions/0015-glass.md)).
- Browser gap ([0015](../decisions/0015-glass.md)): only Chromium supports `prefers-reduced-transparency`. In Safari and Firefox it stays "not reduced" unless the app calls `setReduceTransparency(true)`. An app that must honour the OS setting there reads it itself and calls the setter.
- A Block never reads the media query: it uses glass through the shared tokens and classes.

## How Blocks consume tokens

- Colours, type, spacing, radii, motion, glass and sizes come from `--ui-*` custom properties ([0046](../decisions/0046-web-browsers-markup-naming.md)). A Block has no hard-coded colour, size ([0013](../decisions/0013-spacing-and-density.md)), font family, duration or radius in its CSS; the single exceptions are values that are part of the Block's own design in its decision (a 2 px indicator border), written once as a custom property at the top of the Block's file.
- A Block's own values are custom properties named for the Block (`--ui-button-padding`), set on the Block's class so a page or Module can override them for a subtree. Its state colours reference Style tokens, not literals.
- Block CSS does not select on theme attributes, except the density selector above. It never sets a theme attribute or a Style token.
- The page's own `body` styling comes from the library ([0017](../decisions/0017-app-usage.md)); Blocks do not set `body`.
