// ColorPicker (0041, 0046, 0048): <ui-color-picker>, a field showing a colour that opens the
// ColorView (color-view.js) in a glass flyout (flyout.js) below it. It builds its parts:
//
//   <ui-color-picker value="#007cea" name="accent" aria-label="Accent colour">
//     <button type="button" class="ui-field ui-combo-box-field ui-color-picker-field ui-squircle ui-focus-ring"
//         aria-haspopup="dialog" aria-expanded="false" aria-controls="…">
//       <span class="ui-visually-hidden">Accent colour</span>                  the name, observed
//       <span class="ui-color-chip ui-squircle ui-color-picker-chip" role="img" aria-label="Steel Blue, #007CEA"></span>
//       <span class="ui-combo-box-caret" aria-hidden="true">(caret-down icon)</span>
//     </button>
//     <div class="ui-flyout ui-glass ui-squircle ui-color-picker-flyout" popover role="dialog"
//         aria-label="Choose a colour"><div class="ui-flyout-content"><ui-color-view></ui-color-view></div></div>
//   </ui-color-picker>
//
// Native first (0046): <input type="color"> opens the browser's own colour dialog (the system
// panel on macOS, a small popup in Chromium), which cannot be the library's glass flyout with the
// ColorView; it has no alpha in Safari and Firefox and its swatch cannot take the field look. So
// the element wraps a native <button> and keeps the native colour input's contract: value is
// "#rrggbb", or "#rrggbbaa" when not opaque; the value attribute is the initial value (a form reset
// goes back to it; once the user or the value property changes the value it no longer applies);
// input fires on every change made in the open view, and change when the flyout closes with a
// different colour than it opened with, as the native input does when its dialog closes. It is
// form-associated, with name and disabled; the default colour is white, as Fluent's.
//
// The field (0041) is the closed ComboBox field (the combobox.css classes ui-combo-box-field and
// ui-combo-box-caret: chrome, density height, border, colours, no pressed look, the square caret
// zone with caret-down) with the colour chip filling the rest, inset and rounded as a field button.
// Children written inside the element replace the chip, as an app's Content does in Fluent.
// The button opens and closes the flyout (Enter, Space, a click); focus moves into it, on the
// selected tab; Escape closes it and returns focus to the button; focus leaving it closes it.
//
// Name: the element's aria-label (moved to the button) or a <label for> the element, kept in step
// as either changes, plus the colour's display name and hex value, so the button reads "Accent
// colour, Steel Blue, #007CEA". Attributes: value, name, disabled, flyout-label (the flyout's
// name, "Choose a colour"), and every string of the ColorView (passed to it). Properties: value,
// color, colorName, isOpen, open(), close(), parts. Forced states for renders and tests: data-state="hover",
// "pressed" (the hover look) or "focus" on the element is passed to the field. colorName (a
// function from { r, g, b, a } to a name, English displayName by default) names the colour in the
// button and is passed to the view, as on the ColorView.
import { icon } from "./icon.js";
import { openFlyout, closeFlyout } from "./flyout.js";
import { el } from "./date-picker.js";
import { STRINGS, parseColor, formatColor, cssColor, displayName } from "./color-view.js";

const WHITE = "#ffffff";
let nextId = 0;


export class UiColorPicker extends HTMLElement {
  static formAssociated = true;
  static observedAttributes = ["value", "disabled", "aria-label", "flyout-label", "data-state", ...Object.keys(STRINGS)];

  #internals = this.attachInternals();
  #value = WHITE;
  #dirty = false;
  #opened = null; // the value when the flyout opened
  #label = null; // the name, moved from the element to the button
  #movingLabel = false;
  #formDisabled = false;
  #labelObserver = new MutationObserver(() => this.#render());
  #wasOpen = false;
  #p = null;
  #colorName = displayName;

  connectedCallback() {
    if (!this.#p) this.#build();
    this.#observeLabels();
    this.#render();
  }

  disconnectedCallback() {
    this.#labelObserver.disconnect();
  }

  attributeChangedCallback(name, old, value) {
    if (name === "aria-label" && this.#movingLabel) return;
    if (name === "value" && !this.#dirty) this.#value = formatColor(parseColor(value) ?? parseColor(WHITE));
    if (this.#p) this.#render();
  }

  get value() {
    return this.#value;
  }

  set value(text) {
    const color = parseColor(text);
    if (!color) return;
    this.#dirty = true;
    this.#value = formatColor(color);
    if (this.#p) this.#render();
  }

  /** The colour as { r, g, b, a } (0-255). */
  get color() {
    return parseColor(this.#value);
  }

  set color(color) {
    this.value = formatColor({ a: 255, ...color });
  }

  get disabled() {
    return this.hasAttribute("disabled") || this.#formDisabled;
  }

  set disabled(on) {
    this.toggleAttribute("disabled", Boolean(on));
  }

  get name() {
    return this.getAttribute("name");
  }

  get form() {
    return this.#internals.form;
  }

  get isOpen() {
    return this.#p?.flyout.matches(":popover-open") ?? false;
  }

  /** The function naming the colour in the button and the view. */
  get colorName() {
    return this.#colorName;
  }

  set colorName(fn) {
    this.#colorName = typeof fn === "function" ? fn : displayName;
    if (this.#p) this.#render();
  }

  /** The field button, the chip (null when replaced), the flyout and its ColorView. */
  get parts() {
    const { field, chip, flyout, view } = this.#p ?? {};
    return { field, chip, flyout, view };
  }

  /** Opens the flyout below the field; focus moves into the view. */
  open() {
    if (this.disabled || this.isOpen || !this.#p) return;
    const { field, flyout, view } = this.#p;
    view.value = this.#value;
    this.#opened = this.#value;
    openFlyout(flyout, { anchor: field, placement: "block-end-start" });
  }

  close() {
    if (this.isOpen) closeFlyout(this.#p.flyout);
  }

  formResetCallback() {
    this.#dirty = false;
    this.#value = formatColor(parseColor(this.getAttribute("value")) ?? parseColor(WHITE));
    if (this.#p) this.#render();
  }

  formDisabledCallback(disabled) {
    this.#formDisabled = disabled;
    if (this.#p) this.#render();
  }

  #build() {
    const id = `ui-color-picker-${nextId++}`;
    // Children written inside the element are the field's content in place of the chip.
    const content = [...this.childNodes].filter((n) => n.nodeType === Node.ELEMENT_NODE || n.textContent.trim());
    const name = el("span", "ui-visually-hidden");
    const chip = content.length ? null : el("span", "ui-color-chip ui-squircle ui-color-picker-chip");
    chip?.setAttribute("role", "img");
    const custom = content.length ? el("span", "ui-color-picker-content", ...content) : null;
    const caret = el("span", "ui-combo-box-caret", icon("caret-down"));
    caret.setAttribute("aria-hidden", "true");
    const field = el("button", "ui-field ui-combo-box-field ui-color-picker-field ui-squircle ui-focus-ring", name, chip ?? custom, caret);
    field.type = "button";
    field.setAttribute("aria-haspopup", "dialog");
    field.setAttribute("aria-expanded", "false");
    field.setAttribute("aria-controls", `${id}-flyout`);
    const view = el("ui-color-view");
    const flyout = el("div", "ui-flyout ui-glass ui-squircle ui-color-picker-flyout", el("div", "ui-flyout-content", view));
    flyout.id = `${id}-flyout`;
    flyout.popover = "auto";
    flyout.setAttribute("role", "dialog");
    this.replaceChildren(field, flyout);
    this.#p = { field, name, chip, flyout, view };

    // A press on the field while open light-dismisses first; its click must not open it again.
    field.addEventListener("pointerdown", () => { this.#wasOpen = this.isOpen; }, true);
    field.addEventListener("click", () => {
      if (this.#wasOpen) this.#wasOpen = false;
      else if (this.isOpen) this.close();
      else this.open();
    });
    view.addEventListener("input", (e) => {
      e.stopPropagation();
      this.#dirty = true;
      this.#value = view.value;
      this.#render();
      this.dispatchEvent(new Event("input", { bubbles: true }));
    });
    view.addEventListener("change", (e) => e.stopPropagation());
    flyout.addEventListener("toggle", (e) => {
      if (e.newState !== "closed") return;
      const changed = this.#opened !== null && this.#opened !== this.#value;
      this.#opened = null;
      if (changed) this.dispatchEvent(new Event("change", { bubbles: true }));
    });
    // Focus leaving the flyout (Tab, Shift+Tab) closes it; focus returns nowhere, it has moved on.
    flyout.addEventListener("focusout", (e) => {
      if (e.relatedTarget && !flyout.contains(e.relatedTarget)) closeFlyout(flyout, { restoreFocus: false });
    });
  }

  // Follows the labels' text: a <label for> is read again whenever its content changes.
  #observeLabels() {
    this.#labelObserver.disconnect();
    for (const label of this.#internals.labels ?? []) {
      this.#labelObserver.observe(label, { childList: true, characterData: true, subtree: true });
    }
  }

  #render() {
    const { field, name, chip, flyout, view } = this.#p;
    const color = parseColor(this.#value);
    this.#internals.setFormValue(this.#value);
    field.disabled = this.disabled;
    if (this.disabled) this.close();

    // The name is the button's: aria-label moves from the element; a <label for> names it too.
    if (this.hasAttribute("aria-label")) {
      this.#label = this.getAttribute("aria-label");
      this.#movingLabel = true;
      this.removeAttribute("aria-label");
      this.#movingLabel = false;
    }
    this.#observeLabels();
    const label = this.#label ?? ([...(this.#internals.labels ?? [])].map((l) => l.textContent.trim()).join(" ") || "");
    name.textContent = label ? `${label}, ` : "";
    if (chip) {
      chip.style.setProperty("--ui-color-chip-color", cssColor(color));
      chip.setAttribute("aria-label", `${this.#colorName(color)}, #${this.#value.slice(1).toUpperCase()}`);
    }
    flyout.setAttribute("aria-label", this.getAttribute("flyout-label") ?? "Choose a colour");
    for (const key of Object.keys(STRINGS)) {
      const v = this.getAttribute(key);
      if (v === null) view.removeAttribute(key);
      else view.setAttribute(key, v);
    }
    if (view.colorName !== this.#colorName) view.colorName = this.#colorName;
    if (!this.isOpen) view.value = this.#value;

    // A forced state on the element (renders and tests) is the field's; pressed has the hover look.
    const state = this.getAttribute("data-state");
    if (state === null) field.removeAttribute("data-state");
    else field.dataset.state = state.replace("pressed", "hover");
  }
}

if (!customElements.get("ui-color-picker")) customElements.define("ui-color-picker", UiColorPicker);
