// DatePicker (0038, 0046, 0048): <ui-date-picker>, a field showing the date in segments (month,
// day, year in the locale's order) that opens a glass flyout of looping selectors. This module
// also holds the picker parts TimePicker builds on (time-picker.js): the field and the flyout.
//
// Native first (0046): <input type="date"> was weighed and set aside. It draws its own segments
// (editable in place, with no dividers or month names), opens the browser's own picker (a month
// grid in Chromium, a wheel on iOS), and neither can take the field and flyout of 0038. The
// element keeps the native element's contract instead: value is YYYY-MM-DD, and it fires input
// and change and takes part in forms like <input type="date">.
//
//   <ui-date-picker role="group" aria-label="Start date" value="2026-11-13" name="start">
//     <button type="button" class="ui-date-picker-field ui-field ui-squircle ui-focus-ring"
//         aria-haspopup="dialog" aria-expanded="false">
//       <span class="ui-date-picker-segment" data-part="month">November</span>
//       <span class="ui-date-picker-divider"></span> …
//     </button>
//     <div class="ui-date-picker-flyout ui-flyout ui-glass ui-squircle" popover role="dialog"
//         aria-label="Choose a date">
//       <div class="ui-flyout-content">
//         <div class="ui-date-picker-viewport ui-squircle">
//           <span class="ui-date-picker-highlight ui-squircle"></span>
//           <div class="ui-date-picker-selector" role="spinbutton" tabindex="0" aria-label="month"
//               aria-valuenow="11" aria-valuetext="November" …>
//             <span class="ui-date-picker-row ui-squircle">…</span> x 7, the selected in the middle
//             <button class="ui-date-picker-step ui-button ui-repeat-button …">(caret)</button> x 2
//           </div> …
//         </div>
//         <div class="ui-date-picker-actions">(Accept and Dismiss ui-buttons)</div>
//       </div>
//     </div>
//   </ui-date-picker>
//
// The field is one button (Fluent's flyout button): a click, Enter or Space opens the flyout over
// the field, its selected row level with the field and its width the field's. The flyout's
// selectors are spinbuttons, each a Tab stop; Tab and Shift+Tab cycle through the selectors and
// the two buttons, as in Fluent. Keys (Fluent's): Up and Down select the previous and next row
// (the row above or below, looping where the column loops), Home and End the first and last;
// Enter accepts, Escape (or a click outside) dismisses; the wheel steps a selector; a click on a
// row selects it; the step buttons over the top and bottom row (shown while the pointer is over
// a selector) step it and never take focus. Opened from the keyboard, the first selector shows
// its column ring at once; opened by pointer, none. Accept sets the value, fires input and change
// and returns focus to the field.
//
// Attributes: value (YYYY-MM-DD; the initial value, as on <input>: once the user or the value
// property changes the value, the attribute no longer applies; a form reset goes back to it), min and max (YYYY-MM-DD; the
// year column runs between their years, by default 100 years either side of this year, and an
// accepted date is kept between them), disabled, name, and the strings month-placeholder,
// day-placeholder, year-placeholder (the empty segments and the selectors' names; "month", "day",
// "year"), flyout-label ("Choose a date"), accept-label ("Accept") and dismiss-label ("Dismiss").
// The locale is the nearest lang attribute, else the browser's (Intl names and segment order).
// Forced states for renders and tests: data-state="hover" or "focus" on the element is passed to
// the field.
import { icon } from "./icon.js";
import "./button.js";
import { lastPressWasPointer } from "./text-input.js";
import { openFlyout, closeFlyout } from "./flyout.js";
import { utcDate, today, daysInMonth, parseIsoDate, isoDate, localeOf, formatDate } from "./calendar.js";

/** The rows a selector shows; the middle one is the selected row (0038). */
export const VISIBLE_ROWS = 7;
const MIDDLE = (VISIBLE_ROWS - 1) / 2;

/** An element with a class and children (nodes or strings); shared with calendar-date-picker.js. */
export function el(tag, className, ...children) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  node.append(...children);
  return node;
}

function setAttr(node, name, value) {
  if (value === null || value === undefined) {
    if (node.hasAttribute(name)) node.removeAttribute(name);
  } else if (node.getAttribute(name) !== String(value)) node.setAttribute(name, String(value));
}

/**
 * The flyout of a DatePicker or TimePicker: looping selectors in a squircle viewport inset from
 * the glass, one accent highlight behind the selected row, and Accept and Dismiss.
 *
 * model: { columns(): [{ part, label, count, index, loop, text(i), valueNow(i), valueMin,
 *          valueMax, fr, align }], set(part, index) }; the columns are read again after each set.
 * options: { label, acceptLabel, dismissLabel, onAccept(), onDismiss() }
 */
export class PickerPresenter {
  constructor(model, { label, acceptLabel = "Accept", dismissLabel = "Dismiss", onAccept, onDismiss } = {}) {
    this.model = model;
    this.onAccept = onAccept;
    this.onDismiss = onDismiss;
    this.accepted = false;
    this.viewport = el("div", "ui-date-picker-viewport ui-squircle");
    this.highlight = el("span", "ui-date-picker-highlight ui-squircle");
    this.highlight.setAttribute("aria-hidden", "true");
    this.accept = el("button", "ui-button ui-accent ui-squircle ui-focus-ring ui-date-picker-accept", icon("check"));
    this.dismiss = el("button", "ui-button ui-squircle ui-focus-ring ui-date-picker-dismiss", icon("x"));
    this.accept.type = this.dismiss.type = "button";
    this.accept.setAttribute("aria-label", acceptLabel);
    this.dismiss.setAttribute("aria-label", dismissLabel);
    this.element = el("div", "ui-date-picker-flyout ui-flyout ui-glass ui-squircle",
      el("div", "ui-flyout-content", this.viewport, el("div", "ui-date-picker-actions", this.accept, this.dismiss)));
    this.element.setAttribute("role", "dialog");
    this.element.setAttribute("aria-label", label);
    this.selectors = new Map(); // part -> selector element
    this.render();

    this.accept.addEventListener("click", () => this.onAccept?.());
    this.dismiss.addEventListener("click", () => this.onDismiss?.());
    this.element.addEventListener("keydown", (e) => this.#onKeyDown(e));
    this.element.addEventListener("click", (e) => this.#onClick(e));
    this.element.addEventListener("wheel", (e) => this.#onWheel(e), { passive: false });
    // The step buttons never take focus.
    this.element.addEventListener("mousedown", (e) => {
      if (e.target.closest(".ui-date-picker-step")) e.preventDefault();
    });
  }

  /** The selector elements, in column order. */
  get selectorElements() {
    return [...this.selectors.values()];
  }

  /** Draws the columns from the model, keeping the selector elements (and so focus). */
  render() {
    const columns = this.model.columns();
    const parts = columns.map((c) => c.part);
    for (const [part, node] of this.selectors) {
      if (!parts.includes(part)) {
        node.remove();
        this.selectors.delete(part);
      }
    }
    const template = [];
    const children = [this.highlight];
    columns.forEach((column, i) => {
      if (i) {
        template.push("1px");
        children.push(el("span", "ui-date-picker-spacer"));
      }
      template.push(`${column.fr}fr`);
      let selector = this.selectors.get(column.part);
      if (!selector) {
        selector = this.#createSelector(column);
        this.selectors.set(column.part, selector);
      }
      this.#renderSelector(selector, column);
      children.push(selector);
    });
    this.viewport.style.gridTemplateColumns = template.join(" ");
    // Reorder without detaching a focused selector.
    const focused = document.activeElement;
    children.forEach((child, i) => {
      if (this.viewport.children[i] !== child) this.viewport.insertBefore(child, this.viewport.children[i] ?? null);
    });
    while (this.viewport.children.length > children.length) this.viewport.lastElementChild.remove();
    if (focused && this.element.contains(focused) && document.activeElement !== focused) focused.focus({ preventScroll: true });
  }

  #createSelector(column) {
    const selector = el("div", "ui-date-picker-selector");
    selector.dataset.part = column.part;
    selector.setAttribute("role", "spinbutton");
    selector.tabIndex = 0;
    for (let k = -MIDDLE; k <= MIDDLE; k++) {
      const row = el("span", "ui-date-picker-row ui-squircle");
      row.dataset.offset = String(k);
      if (k === 0) row.dataset.selected = "";
      row.setAttribute("aria-hidden", "true");
      selector.append(row);
    }
    for (const [step, name] of [[-1, "caret-up"], [1, "caret-down"]]) {
      const b = el("button", "ui-button ui-repeat-button ui-squircle ui-date-picker-step", icon(name));
      b.type = "button";
      b.tabIndex = -1;
      b.dataset.step = String(step);
      b.setAttribute("aria-hidden", "true");
      selector.append(b);
    }
    return selector;
  }

  #renderSelector(selector, column) {
    selector.dataset.align = column.align ?? "center";
    setAttr(selector, "aria-label", column.label);
    setAttr(selector, "aria-valuenow", column.valueNow(column.index));
    setAttr(selector, "aria-valuetext", column.text(column.index));
    setAttr(selector, "aria-valuemin", column.valueMin);
    setAttr(selector, "aria-valuemax", column.valueMax);
    for (const row of selector.querySelectorAll(".ui-date-picker-row")) {
      const i = this.#rowIndex(column, Number(row.dataset.offset));
      row.textContent = i === null ? "" : column.text(i);
      row.toggleAttribute("data-empty", i === null);
    }
  }

  // The index a row shows, or null past the end of a column that does not loop.
  #rowIndex(column, offset) {
    const i = column.index + offset;
    if (column.loop) return ((i % column.count) + column.count) % column.count;
    return i >= 0 && i < column.count ? i : null;
  }

  #column(part) {
    return this.model.columns().find((c) => c.part === part);
  }

  /** Selects index `i` (wrapped or clamped) of column `part` and redraws. */
  select(part, i) {
    const column = this.#column(part);
    if (!column) return;
    const index = column.loop ? ((i % column.count) + column.count) % column.count : Math.max(0, Math.min(column.count - 1, i));
    if (index === column.index) return;
    this.model.set(part, index);
    this.render();
  }

  step(part, n) {
    const column = this.#column(part);
    if (column) this.select(part, column.index + n);
  }

  #onClick(e) {
    const selector = e.target.closest(".ui-date-picker-selector");
    if (!selector) return;
    const step = e.target.closest(".ui-date-picker-step");
    if (step) return this.step(selector.dataset.part, Number(step.dataset.step));
    const row = e.target.closest(".ui-date-picker-row");
    if (!row || row.hasAttribute("data-empty")) return;
    const offset = Number(row.dataset.offset);
    if (offset) this.step(selector.dataset.part, offset);
    selector.focus({ preventScroll: true });
  }

  #wheel = 0;

  #onWheel(e) {
    const selector = e.target.closest(".ui-date-picker-selector");
    if (!selector) return;
    e.preventDefault();
    // A wheel notch (or 40 px of a trackpad) moves one row.
    this.#wheel += e.deltaMode === 0 ? e.deltaY : e.deltaY * 40;
    while (Math.abs(this.#wheel) >= 40) {
      const n = Math.sign(this.#wheel);
      this.#wheel -= n * 40;
      this.step(selector.dataset.part, n);
    }
  }

  #onKeyDown(e) {
    if (e.altKey || e.ctrlKey || e.metaKey) return;
    if (e.key === "Tab") {
      // Focus cycles inside the flyout, as Fluent's presenter does.
      const stops = [...this.selectorElements, this.accept, this.dismiss];
      const at = stops.indexOf(document.activeElement);
      const next = stops[(at + (e.shiftKey ? -1 : 1) + stops.length) % stops.length];
      e.preventDefault();
      next.focus();
      return;
    }
    const selector = e.target.closest?.(".ui-date-picker-selector");
    if (e.key === "Enter" && !e.target.closest(".ui-button")) {
      e.preventDefault();
      this.onAccept?.();
      return;
    }
    if (!selector) return;
    const part = selector.dataset.part;
    const column = this.#column(part);
    if (e.key === "ArrowUp") this.step(part, -1);
    else if (e.key === "ArrowDown") this.step(part, 1);
    else if (e.key === "Home") this.select(part, 0);
    else if (e.key === "End") this.select(part, column.count - 1);
    else return;
    e.preventDefault();
  }
}

/**
 * The shared field and flyout behaviour of DatePicker and TimePicker. A subclass provides the
 * segments (segments()), the flyout model (model()), and the value handling (initialState(),
 * commit(state), and setValueFromAttribute(text), which sets the value without marking it dirty).
 * The value attribute is the initial value, as on <input>: once the user or the value property
 * changes the value (dirty), the attribute no longer applies; a form reset goes back to it.
 */
export class PickerElement extends HTMLElement {
  static formAssociated = true;

  internals = this.attachInternals();
  field = null;
  presenter = null;
  formDisabled = false;
  state = null; // the flyout's state while open
  dirty = false;

  connectedCallback() {
    if (!this.field) this.#build();
    this.renderField();
  }

  attributeChangedCallback(name, old, value) {
    if (name === "value" && !this.dirty) this.setValueFromAttribute(value);
    if (this.field) this.renderField();
  }

  get disabled() {
    return this.hasAttribute("disabled") || this.formDisabled;
  }

  set disabled(value) {
    this.toggleAttribute("disabled", Boolean(value));
  }

  get name() {
    return this.getAttribute("name");
  }

  get form() {
    return this.internals.form;
  }

  formResetCallback() {
    this.dirty = false;
    this.setValueFromAttribute(this.getAttribute("value"));
    if (this.field) this.renderField();
  }

  formDisabledCallback(disabled) {
    this.formDisabled = disabled;
    if (this.field) this.renderField();
  }

  string(name, fallback) {
    return this.getAttribute(name) ?? fallback;
  }

  #build() {
    if (!this.hasAttribute("role")) this.setAttribute("role", "group");
    this.field = el("button", "ui-date-picker-field ui-field ui-squircle ui-focus-ring");
    this.field.type = "button";
    this.field.setAttribute("aria-haspopup", "dialog");
    this.field.setAttribute("aria-expanded", "false");
    this.field.addEventListener("click", () => this.open({ keyboard: !lastPressWasPointer() }));
    // A click on a <label for> (sent to the element) moves focus to the field.
    this.addEventListener("click", (e) => {
      if (e.target === this) this.field.focus();
    });
    this.replaceChildren(this.field);
  }

  /** Draws the field's segments and the form value. */
  renderField() {
    this.internals.setFormValue(this.value);
    this.field.disabled = this.disabled;
    // A forced state on the element (renders and tests) is the field's.
    setAttr(this.field, "data-state", this.getAttribute("data-state"));
    const segments = this.segments();
    const template = [];
    const children = [];
    segments.forEach((s, i) => {
      if (i) {
        template.push("1px");
        children.push(el("span", "ui-date-picker-divider"));
      }
      template.push(`${s.fr}fr`);
      const span = el("span", "ui-date-picker-segment", s.text);
      span.dataset.part = s.part;
      span.dataset.align = s.align ?? "center";
      span.toggleAttribute("data-placeholder", Boolean(s.placeholder));
      children.push(span);
    });
    this.field.style.gridTemplateColumns = template.join(" ");
    this.field.replaceChildren(...children);
    this.#renderPresenter();
  }

  // The flyout's strings, and its selectors' names while it is open.
  #renderPresenter() {
    const p = this.presenter;
    if (!p) return;
    setAttr(p.element, "aria-label", this.flyoutLabel());
    setAttr(p.accept, "aria-label", this.string("accept-label", "Accept"));
    setAttr(p.dismiss, "aria-label", this.string("dismiss-label", "Dismiss"));
    if (p.element.matches(":popover-open") && this.state) {
      p.model = this.model();
      p.render();
    }
  }

  /** Opens the flyout over the field; `keyboard` marks the first selector's focus as keyboard focus. */
  open({ keyboard = false } = {}) {
    if (this.disabled) return;
    this.state = this.initialState();
    const flyout = this.#presenter();
    this.presenter.model = this.model();
    this.presenter.render();
    flyout.style.inlineSize = `${this.field.offsetWidth}px`;
    const first = this.presenter.selectorElements[0];
    const rect = this.field.getBoundingClientRect();
    const rtl = getComputedStyle(this).direction === "rtl";
    const point = { x: rtl ? rect.right : rect.left, y: rect.top };
    openFlyout(flyout, { point, opener: this.field, focus: false });
    // Over the field: the selected row level with it, kept inside the viewport.
    const highlight = this.presenter.highlight.getBoundingClientRect();
    const h = flyout.getBoundingClientRect().height;
    const y = point.y + (rect.top + rect.height / 2) - (highlight.top + highlight.height / 2);
    point.y = Math.max(0, Math.min(y, document.documentElement.clientHeight - h));
    openFlyout(flyout, { point, opener: this.field, focus: false });
    first?.focus({ preventScroll: true, focusVisible: keyboard });
  }

  /** Closes the flyout without a change. */
  close() {
    if (this.presenter) closeFlyout(this.presenter.element);
  }

  #presenter() {
    if (!this.presenter) {
      this.presenter = new PickerPresenter(this.model(), {
        label: this.flyoutLabel(),
        acceptLabel: this.string("accept-label", "Accept"),
        dismissLabel: this.string("dismiss-label", "Dismiss"),
        onAccept: () => {
          this.commit(this.state);
          this.close();
          this.dispatchEvent(new Event("input", { bubbles: true }));
          this.dispatchEvent(new Event("change", { bubbles: true }));
        },
        onDismiss: () => this.close(),
      });
      this.presenter.element.popover = "auto";
      this.append(this.presenter.element);
    }
    return this.presenter.element;
  }
}

// --- DatePicker ------------------------------------------------------------------------------

// The locale's order of month, day and year.
function dateOrder(locale) {
  const parts = new Intl.DateTimeFormat(locale, { year: "numeric", month: "long", day: "numeric", timeZone: "UTC" })
    .formatToParts(utcDate(2026, 10, 13)).map((p) => p.type);
  const order = parts.filter((t) => t === "month" || t === "day" || t === "year");
  return order.length === 3 ? order : ["month", "day", "year"];
}

// Fluent's column widths: month 138, day 78, year 78 (in proportion).
const DATE_FR = { month: 138, day: 78, year: 78 };

/**
 * A DatePicker flyout shown on its own, for previews (the catalog's "shown inline"): the flyout
 * element, not a popover, showing `value` (a YYYY-MM-DD string) in `locale`.
 */
export function createDatePickerPresenter({ value, locale = navigator.language } = {}) {
  const date = parseIsoDate(value) ?? today();
  const state = { y: date.getUTCFullYear(), m: date.getUTCMonth(), d: date.getUTCDate() };
  const presenter = new PickerPresenter(dateModel(state, locale, {}), { label: "Choose a date" });
  return presenter.element;
}

function dateModel(state, locale, { minYear, maxYear, labels = {} }) {
  const now = today().getUTCFullYear();
  const lo = minYear ?? now - 100;
  const hi = maxYear ?? now + 100;
  const order = dateOrder(locale);
  const columnOf = {
    month: () => ({
      part: "month", label: labels.month ?? "month", count: 12, index: state.m, loop: true, align: "start",
      text: (i) => formatDate(locale, utcDate(2000, i, 1), { month: "long" }),
      valueNow: (i) => i + 1, valueMin: 1, valueMax: 12,
    }),
    day: () => {
      const count = daysInMonth(state.y, state.m);
      return {
        part: "day", label: labels.day ?? "day", count, index: state.d - 1, loop: true,
        text: (i) => formatDate(locale, utcDate(2000, 0, i + 1), { day: "numeric" }),
        valueNow: (i) => i + 1, valueMin: 1, valueMax: count,
      };
    },
    year: () => ({
      part: "year", label: labels.year ?? "year", count: hi - lo + 1, index: state.y - lo, loop: false,
      text: (i) => formatDate(locale, utcDate(lo + i, 0, 1), { year: "numeric" }),
      valueNow: (i) => lo + i, valueMin: lo, valueMax: hi,
    }),
  };
  return {
    columns: () => order.map((part) => ({ ...columnOf[part](), fr: DATE_FR[part] })),
    set(part, i) {
      if (part === "month") state.m = i;
      if (part === "year") state.y = lo + i;
      if (part === "day") state.d = i + 1;
      state.d = Math.min(state.d, daysInMonth(state.y, state.m));
    },
  };
}

export class UiDatePicker extends PickerElement {
  static observedAttributes = ["value", "min", "max", "disabled", "lang", "data-state", "month-placeholder",
    "day-placeholder", "year-placeholder", "flyout-label", "accept-label", "dismiss-label"];

  #value = null;

  setValueFromAttribute(text) {
    this.#value = parseIsoDate(text);
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
    this.dirty = true;
    this.#value = date ? utcDate(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()) : null;
    if (this.field) this.renderField();
  }

  #labels() {
    return {
      month: this.string("month-placeholder", "month"),
      day: this.string("day-placeholder", "day"),
      year: this.string("year-placeholder", "year"),
    };
  }

  segments() {
    const locale = localeOf(this);
    const labels = this.#labels();
    const d = this.#value;
    const text = {
      month: d && formatDate(locale, d, { month: "long" }),
      day: d && formatDate(locale, d, { day: "numeric" }),
      year: d && formatDate(locale, d, { year: "numeric" }),
    };
    return dateOrder(locale).map((part) => ({
      part, fr: DATE_FR[part], align: part === "month" ? "start" : "center",
      text: text[part] ?? labels[part], placeholder: !d,
    }));
  }

  flyoutLabel() {
    return this.string("flyout-label", "Choose a date");
  }

  initialState() {
    const d = this.#value ?? today();
    return { y: d.getUTCFullYear(), m: d.getUTCMonth(), d: d.getUTCDate() };
  }

  model() {
    const min = parseIsoDate(this.getAttribute("min"));
    const max = parseIsoDate(this.getAttribute("max"));
    return dateModel(this.state ?? this.initialState(), localeOf(this), {
      minYear: min?.getUTCFullYear(), maxYear: max?.getUTCFullYear(), labels: this.#labels(),
    });
  }

  commit({ y, m, d }) {
    let date = utcDate(y, m, d);
    const min = parseIsoDate(this.getAttribute("min"));
    const max = parseIsoDate(this.getAttribute("max"));
    if (min && date < min) date = min;
    if (max && date > max) date = max;
    this.valueAsDate = date;
  }
}

if (!customElements.get("ui-date-picker")) customElements.define("ui-date-picker", UiDatePicker);
