# Web semantics

Markup rules for the web library ([0045](../decisions/0045-web-library.md), [0046](../decisions/0046-web-browsers-markup-naming.md)). Accessibility is in [accessibility.md](accessibility.md), keyboard in [keyboard.md](keyboard.md).

These standards are recorded in [0048](../decisions/0048-web-html-standards.md).

## Native elements first

- A Block is a styled native element wherever one fits: `<button>`, `<a href>`, `<input>`, `<select>`, `<textarea>`, `<dialog>`, `<progress>`, `<meter>`, `<details>`, `<table>`. Variants and sizes are classes (`class="ui-button ui-accent"`).
- A Block never replaces a native element's behaviour in script. Styling that a native element cannot take (for example a custom checkbox indicator) is done with `appearance: none` and CSS on the element itself, or with a wrapper whose native control stays in the DOM.
- Use the element for what it is: `<button>` for an action, `<a href>` for navigation, `<input type="checkbox">` for an on/off setting, `<input type="radio">` for one of a group, `<input type="range">` for a slider. Never a `<div>` or `<span>` with a click handler.
- A `<button>` always has `type`: `button` unless it submits or resets a form.
- Lists are `<ul>`, `<ol>` or `<dl>`; tabular data is `<table>` with `<th scope>`; icons that decorate are inline `<svg>` marked `aria-hidden="true"`.

## Custom elements

- A custom element (`<ui-` plus the name) is allowed only where no native element fits, as in [0046](../decisions/0046-web-browsers-markup-naming.md): the segmented control, the tree, the sidebar and similar composites.
- A custom element is autonomous (`extends HTMLElement`), not a customised built-in (Safari does not support `is=`).
- It works from its markup: children written in HTML are the content, and attributes are the configuration. Content that matters is in the light DOM, not a shadow root, so page CSS, find in page and the accessibility tree reach it. A shadow root is used only where encapsulation is needed and then exposes its parts with `part`.
- Its roles, names, states and keys follow [accessibility.md](accessibility.md) and [keyboard.md](keyboard.md); a custom element that cannot meet them is a native element's job instead.
- It is registered once, when its module loads (`customElements.define`), and is usable in markup before and after registration (the unstyled markup stays readable).

## Landmarks and headings

- A page has one `<main>`. Navigation is `<nav>` (named with `aria-label` when there is more than one), page-level header and footer are `<header>` and `<footer>`, a side panel is `<aside>`. Every region of the page sits in a landmark.
- The app shell Screen ([0022](../decisions/0022-window-layout.md)) maps its areas to landmarks: title band `<header>`, sidebar `<nav>`, glass content panel `<main>`, status bar `<footer>`, inspector `<aside>`.
- Headings are real `<h1>`-`<h6>` elements, in order, with one `<h1>` per page and no skipped levels. Their size comes from a class (the type ramp, [0012](../decisions/0012-type.md)), never from the level.
- Section titles that do not name a region (such as the sidebar's section headers) are not headings unless they structure content.

## Forms

- Every field is a native form control, with a `name` where the field is submitted, inside a `<form>` where it is submitted, and has a visible `<label for>` or a wrapping `<label>`. Placeholder text is a hint, never the label.
- Groups of related fields are `<fieldset>` with a `<legend>`; a radio group is always one.
- Help and error text is linked to its field with `aria-describedby`; an invalid field has `aria-invalid="true"` (or native constraint validation and `:user-invalid`).
- Use the right `type`, `autocomplete` and `inputmode`, so the browser's autofill and keyboards work.
- A custom element that acts as a form field is form-associated (`static formAssociated = true` with `ElementInternals`), so it takes part in `<form>` submission, reset, `<label>` and validation like a native control.

## Language and direction

- `<html>` has `lang`. Text in another language has its own `lang`.
- Direction comes from `dir` on `<html>` or an element, never from script. The library's CSS uses logical properties (`margin-inline-start`, `padding-block`, `inset-inline-end`) so right-to-left layouts mirror, and arrow-key direction follows `dir` ([keyboard.md](keyboard.md)).
- The document has a `<title>` and `<meta name="viewport" content="width=device-width, initial-scale=1">` (never `user-scalable=no` or a maximum scale).
