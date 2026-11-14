# Web accessibility

Accessibility rules for the web library. Native elements ([semantics.md](semantics.md)) give most of this for free; this file covers what the library must add and never remove. The target is WCAG 2.2 level AA.

These standards are recorded in [0048](../decisions/0048-web-html-standards.md).

## Names, roles and states

- Every interactive element has an accessible name. A native control gets it from its content or `<label>`; an icon-only control has `aria-label` (and a tooltip for pointer users, as the desktop segmented control does, [0032](../decisions/0032-segmented-control.md)); a custom element takes `aria-label` or `aria-labelledby` from its markup.
- The visible label is part of the accessible name (WCAG "label in name"): `aria-label` extends visible text, never replaces it with different words.
- ARIA is used only where no native semantics exist: a role on a custom element, or a state a native element cannot express. Never an ARIA role that repeats or contradicts the native one (`<button role="button">`, `<a role="button">` for an action).
- A custom element sets its role and states on itself, kept in step with its state in script:

  | Custom element | Role and states |
  |---|---|
  | Segmented control | `radiogroup` of `radio` segments, `aria-checked`, `aria-disabled` |
  | Tree | `tree` of `treeitem`, `aria-expanded`, `aria-selected`, `aria-level` (children in `group`) |
  | Toolbar band | `role="group"` with an accessible name (`aria-label`, "Toolbar" unless the app sets one); why not `toolbar` is in [0048](../decisions/0048-web-html-standards.md) |
  | Sidebar | a `<nav>` containing a list, the selected row marked `aria-current="page"`; the footer list is named ([0034](../decisions/0034-sidebar.md)) |

- State that CSS draws (selected, checked, expanded, pressed, current) is read from the attribute that exposes it (`[aria-checked="true"]`, `[aria-expanded]`, `[aria-current]`, `:checked`), so the visible state and the announced state cannot differ.
- Disabled native controls use `disabled`; a disabled custom element uses `aria-disabled="true"` and takes no focus.
- Strings that the library shows or speaks (names, tooltips) are overridable by the app through attributes or slots, with English defaults ([0017](../decisions/0017-app-usage.md) does this for desktop string resources).

## Contrast

- Text meets WCAG AA contrast, 4.5:1 for normal text and 3:1 for large text ([0011](../decisions/0011-colour.md)). The Style tier's accent fills, accent text, status text and secondary text values already meet it in light and dark, recomputed for any accent by `setAccent`.
- A Block uses the matching text token on its background token (`--ui-accent-fill-color` with `--ui-danger-foreground`, `--ui-accent-text-color` for links) and never mixes them, so the guarantee holds.
- Meaningful non-text parts (a field's border, a checkbox indicator, a focus ring) reach 3:1 against their surroundings (WCAG 1.4.11). Disabled controls and decoration are exempt, as disabled text is on desktop ([0011](../decisions/0011-colour.md)).
- Colour is never the only cue: errors and status also carry an icon or text, and selection also changes shape or weight where it matters.
- Text the app places over its own `Backdrop` is the app's to keep at AA ([0022](../decisions/0022-window-layout.md)).

## Focus visibility

- Every focusable element shows the focus ring of [0019](../decisions/0019-focus-ring.md) on `:focus-visible` only: keyboard focus shows it, a mouse click does not. How a Block takes it is in [composition.md](composition.md).
- The ring is never removed with `outline: none` unless the Block draws its own visible replacement (menu items show focus as their highlight, [0036](../decisions/0036-menus-and-flyouts.md); a segmented control rings the whole control, [0032](../decisions/0032-segmented-control.md)).
- A focused element is never fully hidden by sticky or overlaid content (WCAG 2.4.11): containers that clip need about 4 px of padding for the ring, as on desktop, and scroll containers use `scroll-padding`.
- Elements removed from view (a hidden sidebar, a closed popup) take no focus and are out of the Tab order: use `hidden`, `display: none` or `inert`, not only `opacity` or an off-screen transform.

## Zoom, reflow and sizes

- Layout works at 200 % browser zoom and at 320 CSS px wide without two-dimensional scrolling (WCAG 1.4.10), except for content that needs it (data tables, the Backdrop).
- Every size token is in `px`, as decided, matching the desktop's pixel sizes ([0012](../decisions/0012-type.md), [0013](../decisions/0013-spacing-and-density.md)). Browser zoom scales everything, so 200 % zoom and reflow hold; the browser's font-size setting is not followed. There is no fixed `height` on text containers.
- Targets are at least 24 × 24 CSS px (WCAG 2.5.8); Compact density keeps this ([0013](../decisions/0013-spacing-and-density.md)).
- Content is not locked to one orientation, and text spacing changes by the user do not clip it.

## User preferences

- `prefers-color-scheme`, `prefers-reduced-motion`, `prefers-reduced-transparency` and `forced-colors` are respected; the first three through the theme attributes ([theming.md](theming.md)).
- Under `forced-colors: active` the library does not override system colours: borders that carry meaning stay (a transparent `border` or `outline` is kept so it turns visible), and the focus ring uses the `Highlight` system colour.
- Animation never blocks content: nothing flashes more than three times a second, and anything that moves on its own for over five seconds can be paused, stopped or hidden, unless it is essential (a progress indicator). Reduce motion also stops it ([0016](../decisions/0016-motion.md), [theming.md](theming.md)).

## Hidden text

- Text for assistive technology only uses a `ui-visually-hidden` class (clipped, one pixel, not `display: none`). It is for names and context that the visual design leaves out.
- `display: none`, `hidden` and `visibility: hidden` hide from everyone, so they are for content that is truly not there.
- `aria-hidden="true"` hides from assistive technology only, so it is for decoration (icons beside a text label, the sliding pill) and never on a focusable element or its ancestor.
- An icon with meaning is not `aria-hidden`: it takes `role="img"` and a name, or the control that holds it is named.

## Status and live regions

- Status messages that appear without focus moving are announced from a live region that exists in the page before its text changes: `role="status"` (polite) for progress and results, `role="alert"` (assertive) only for errors that need action now.
- Notification cards ([0037](../decisions/0037-tooltip-and-notifications.md)) use `role="status"`, or `role="alert"` for errors, and do not take focus.
- A progress element (`<progress>`, the ProgressRing) has a name and a value; an indeterminate one has no `value`. Its percentage is announced by the browser, not repeated in a live region on every tick.
- Tooltips are supplementary: the control keeps its own name, and the tooltip is linked with `aria-describedby`, shown on keyboard focus as well as hover, stays visible while the pointer is over it (hoverable), and is dismissed with Escape (WCAG 1.4.13).
