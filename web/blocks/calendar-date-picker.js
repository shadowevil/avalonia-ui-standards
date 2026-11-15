// CalendarDatePicker (0038, 0046, 0048): <ui-calendar-date-picker>, a text field for a date with
// a calendar button that opens the Calendar (calendar.js) in a glass drop-down. It builds its
// parts from TextInput's field (text-input.js), a field button and a flyout (flyout.js):
//
//   <ui-calendar-date-picker aria-label="Due date" value="2026-11-13" name="due">
//     <span class="ui-field ui-squircle ui-focus-ring-within">
//       <input class="ui-text-input" role="combobox" aria-haspopup="dialog" aria-expanded="false"
//           aria-autocomplete="none" placeholder="<M/d/yyyy>" aria-label="Due date">
//       <span class="ui-field-trailing">
//         <button type="button" class="ui-button ui-subtle ui-field-button ui-squircle" tabindex="-1"
//             aria-label="Show calendar" aria-expanded="false">(calendar-blank icon)</button>
//       </span>
//     </span>
//     <div class="ui-calendar-date-picker-drop-down ui-flyout ui-glass ui-squircle" popover
//         role="dialog" aria-label="Choose a date"><ui-calendar></ui-calendar></div>
//   </ui-calendar-date-picker>
//
// Native first (0046): <input type="date"> has no free text in the locale's short format, no
// field button and opens the browser's own picker, so the element wraps a native text input
// instead (the APG date picker combobox): typing, selection, autofill and the text input's keys
// stay native, and value is YYYY-MM-DD as on <input type="date">.
//
// Behaviour (Fluent's): the text is the date in the locale's short format; typed text is read on
// Enter or when the field loses focus, an invalid date putting the last valid text back and an
// empty text clearing the value. The calendar button (never a Tab stop, as every field button)
// or Alt+Down in the text opens the drop-down SpacingXS below the field, focus moving to the
// Calendar's selected day (else today); Alt+Up or Escape closes it. Picking a day sets the date,
// closes the drop-down and returns focus to the text. Focus leaving the drop-down closes it. It fires input and change when the value
// changes, and is form-associated.
//
// The control is named once, on the text: the element's aria-label moves to the text (the element
// takes no role), and a <label for> the element names the text.
//
// Attributes: value (YYYY-MM-DD; the initial value, as on <input>: once the user or the value
// property changes the value, the attribute no longer applies; a form reset goes back to it), min and max (passed to the Calendar),
// disabled, name, placeholder (default the locale's short date pattern in angle brackets, as
// Fluent: <M/d/yyyy>), and the strings button-label ("Show calendar") and flyout-label ("Choose a
// date"). The locale is the nearest lang attribute, else the browser's. Forced states for renders
// and tests: data-state="hover" or "focus" on the element is passed to the field.
import { icon } from "./icon.js";
import "./button.js";
import { syncField } from "./text-input.js";
import { openFlyout, closeFlyout } from "./flyout.js";
import { el } from "./date-picker.js";
import { utcDate, today, daysInMonth, parseIsoDate, isoDate, localeOf, formatDate } from "./calendar.js";

const SHORT = { year: "numeric", month: "numeric", day: "numeric" };

/** The locale's short date pattern (M/d/yyyy for en-US), from Intl's parts. */
function shortPattern(locale) {
  return new Intl.DateTimeFormat(locale, { ...SHORT, timeZone: "UTC" }).formatToParts(utcDate(2026, 0, 2)).map((p) => {
    if (p.type === "year") return "yyyy";
    if (p.type === "month") return p.value.length > 1 ? "MM" : "M";
    if (p.type === "day") return p.value.length > 1 ? "dd" : "d";
    return p.value;
  }).join("");
}

/** Reads a date typed in the locale's short format (its order of day, month and year), or null. */
function parseShort(text, locale) {
  const order = new Intl.DateTimeFormat(locale, { ...SHORT, timeZone: "UTC" }).formatToParts(utcDate(2026, 0, 2))
    .map((p) => p.type).filter((t) => t === "year" || t === "month" || t === "day");
  const digits = new Intl.NumberFormat(locale).formatToParts(1234567890).filter((p) => p.type === "integer").map((p) => p.value).join("");
  // Native digits (Arabic, Devanagari, …) read as Latin ones.
  const latin = text.replace(/\p{Nd}/gu, (ch) => {
    const i = digits.indexOf(ch);
    return i >= 0 ? "1234567890"[i] : ch;
  });
  const groups = latin.match(/\d+/g);
  if (!groups || groups.length !== 3 || order.length !== 3) return null;
  const v = Object.fromEntries(order.map((t, i) => [t, Number(groups[i])]));
  // A two-digit year, as Fluent's culture: 00-49 in the 2000s, 50-99 in the 1900s (limit 2049).
  if (groups[order.indexOf("year")].length <= 2) v.year += v.year <= 49 ? 2000 : 1900;
  if (v.month < 1 || v.month > 12 || v.day < 1 || v.day > daysInMonth(v.year, v.month - 1)) return null;
  return utcDate(v.year, v.month - 1, v.day);
}

let nextId = 0;

export class UiCalendarDatePicker extends HTMLElement {
  static formAssociated = true;
  static observedAttributes = ["value", "min", "max", "disabled", "lang", "placeholder", "aria-label", "button-label", "flyout-label", "data-state"];

  #internals = this.attachInternals();
  #value = null;
  #dirty = false; // set once the user or the value property changes the value; the attribute then no longer applies
  #label = null; // the name, moved from the element to the text
  #movingLabel = false;
  #formDisabled = false;
  #field;
  #input;
  #button;
  #dropDown;
  #calendar;

  connectedCallback() {
    if (!this.#field) this.#build();
    this.#render();
  }

  attributeChangedCallback(name, old, value) {
    if (name === "value" && !this.#dirty) this.#value = parseIsoDate(value);
    if (name === "aria-label" && this.#movingLabel) return;
    if (this.#field) this.#render();
  }

  get value() {
    return isoDate(this.#value);
  }

  set value(text) {
    this.valueAsDate = parseIsoDate(String(text ?? ""));
  }

  get valueAsDate() {
    return this.#value ? new Date(this.#value) : null;
  }

  set valueAsDate(date) {
    this.#dirty = true;
    this.#value = date ? utcDate(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()) : null;
    if (this.#field) this.#render();
  }

  get disabled() {
    return this.hasAttribute("disabled") || this.#formDisabled;
  }

  set disabled(value) {
    this.toggleAttribute("disabled", Boolean(value));
  }

  get name() {
    return this.getAttribute("name");
  }

  get form() {
    return this.#internals.form;
  }

  /** The text input, the calendar button, the drop-down and its Calendar. */
  get parts() {
    return { field: this.#field, input: this.#input, button: this.#button, dropDown: this.#dropDown, calendar: this.#calendar };
  }

  formResetCallback() {
    this.#dirty = false;
    this.#value = parseIsoDate(this.getAttribute("value"));
    if (this.#field) this.#render();
  }

  formDisabledCallback(disabled) {
    this.#formDisabled = disabled;
    if (this.#field) this.#render();
  }

  get isOpen() {
    return this.#dropDown?.matches(":popover-open") ?? false;
  }

  /** Opens the drop-down; focus moves to the Calendar's cursor day. */
  open() {
    if (this.disabled || this.isOpen) return;
    const c = this.#calendar;
    c.valueAsDate = this.#value;
    c.displayMode = "month";
    c.displayDate = this.#value ?? today();
    openFlyout(this.#dropDown, { anchor: this.#field, opener: this.#input, placement: "block-end-start", focus: false });
    c.focusCursor({ preventScroll: true });
  }

  close() {
    if (this.isOpen) closeFlyout(this.#dropDown);
  }

  #build() {
    const id = `ui-calendar-date-picker-${nextId++}`;
    this.#input = el("input", "ui-text-input");
    this.#input.type = "text";
    this.#input.autocomplete = "off";
    this.#input.setAttribute("role", "combobox");
    this.#input.setAttribute("aria-haspopup", "dialog");
    this.#input.setAttribute("aria-expanded", "false");
    this.#input.setAttribute("aria-autocomplete", "none");
    this.#input.setAttribute("aria-controls", `${id}-drop-down`);
    this.#button = el("button", "ui-button ui-subtle ui-field-button ui-squircle", icon("calendar-blank"));
    this.#button.type = "button";
    this.#button.tabIndex = -1;
    this.#button.setAttribute("aria-expanded", "false");
    this.#button.setAttribute("aria-controls", `${id}-drop-down`);
    this.#field = el("span", "ui-field ui-squircle ui-focus-ring-within", this.#input, el("span", "ui-field-trailing", this.#button));
    this.#calendar = el("ui-calendar", "");
    this.#dropDown = el("div", "ui-calendar-date-picker-drop-down ui-flyout ui-glass ui-squircle", this.#calendar);
    this.#dropDown.id = `${id}-drop-down`;
    this.#dropDown.popover = "auto";
    this.#dropDown.setAttribute("role", "dialog");
    this.replaceChildren(this.#field, this.#dropDown);

    this.#button.addEventListener("click", () => (this.isOpen ? this.close() : this.open()));
    // A press on the button while open would light-dismiss first and then reopen: it closes.
    this.#button.addEventListener("pointerdown", () => { this.#wasOpen = this.isOpen; });
    this.#button.addEventListener("click", (e) => {
      if (this.#wasOpen) {
        this.#wasOpen = false;
        e.stopImmediatePropagation();
        this.close();
        this.#input.focus();
      }
    }, { capture: true });
    this.#input.addEventListener("keydown", (e) => {
      if (e.altKey && e.key === "ArrowDown") this.open();
      else if (e.altKey && e.key === "ArrowUp") this.close();
      else if (e.key === "Enter") this.#readText();
      else return;
      e.preventDefault();
    });
    this.#input.addEventListener("change", (e) => {
      e.stopPropagation();
      this.#readText();
    });
    this.#calendar.addEventListener("change", (e) => e.stopPropagation());
    this.#calendar.addEventListener("ui-calendar-pick", (e) => {
      e.stopPropagation();
      this.#setValue(this.#calendar.valueAsDate);
      closeFlyout(this.#dropDown, { restoreFocus: true });
    });
    // Focus leaving the drop-down (Tab, Shift+Tab) closes it, as menus; a cell redrawn under focus
    // (no related target) does not.
    this.#dropDown.addEventListener("focusout", (e) => {
      if (e.relatedTarget && !this.#dropDown.contains(e.relatedTarget)) this.close();
    });
    this.#dropDown.addEventListener("keydown", (e) => {
      if (!e.altKey || e.key !== "ArrowUp") return;
      e.preventDefault();
      e.stopPropagation();
      closeFlyout(this.#dropDown, { restoreFocus: true });
    });
    this.#dropDown.addEventListener("toggle", (e) => this.#button.setAttribute("aria-expanded", String(e.newState === "open")));
    this.addEventListener("click", (e) => {
      if (e.target === this) this.#input.focus();
    });
  }

  #wasOpen = false;

  #locale() {
    return localeOf(this);
  }

  #text(date) {
    return date ? formatDate(this.#locale(), date, SHORT) : "";
  }

  #setValue(date) {
    const before = this.value;
    this.valueAsDate = date;
    if (this.value !== before) {
      this.dispatchEvent(new Event("input", { bubbles: true }));
      this.dispatchEvent(new Event("change", { bubbles: true }));
    }
  }

  // Reads the typed text: a valid date sets the value, empty clears it, anything else puts the
  // last valid text back.
  #readText() {
    const text = this.#input.value.trim();
    const date = text ? parseShort(text, this.#locale()) : null;
    const { min, max } = { min: parseIsoDate(this.getAttribute("min")), max: parseIsoDate(this.getAttribute("max")) };
    if (text && (!date || (min && date < min) || (max && date > max))) this.#render();
    else this.#setValue(date);
  }

  #render() {
    const locale = this.#locale();
    this.#internals.setFormValue(this.value);
    this.#input.value = this.#text(this.#value);
    this.#input.placeholder = this.getAttribute("placeholder") ?? `<${shortPattern(locale)}>`;
    this.#input.disabled = this.disabled;
    // The name is the text's alone: aria-label moves from the element to the text, and a <label
    // for> the element names the text too.
    if (this.hasAttribute("aria-label")) {
      this.#label = this.getAttribute("aria-label");
      this.#movingLabel = true;
      this.removeAttribute("aria-label");
      this.#movingLabel = false;
    }
    const label = this.#label ?? ([...(this.#internals.labels ?? [])].map((l) => l.textContent.trim()).join(" ") || null);
    if (label) this.#input.setAttribute("aria-label", label);
    else this.#input.removeAttribute("aria-label");
    this.#button.setAttribute("aria-label", this.getAttribute("button-label") ?? "Show calendar");
    this.#dropDown.setAttribute("aria-label", this.getAttribute("flyout-label") ?? "Choose a date");
    for (const name of ["min", "max", "lang"]) {
      const v = this.getAttribute(name);
      if (v === null) this.#calendar.removeAttribute(name);
      else this.#calendar.setAttribute(name, v);
    }
    // A forced state on the element (renders and tests) is the field's.
    const state = this.getAttribute("data-state");
    if (state === null) this.#field.removeAttribute("data-state");
    else this.#field.dataset.state = state;
    syncField(this.#input);
  }
}

if (!customElements.get("ui-calendar-date-picker")) customElements.define("ui-calendar-date-picker", UiCalendarDatePicker);
