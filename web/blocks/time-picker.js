// TimePicker (0038, 0046, 0048): <ui-time-picker>, a field showing the time in segments (hour,
// minute and, on a 12-hour clock, the period, in the locale's order) that opens the glass flyout
// of looping selectors. It is DatePicker's field and flyout (date-picker.js) with time columns;
// the parts, keys and focus behaviour are DatePicker's.
//
// Native first (0046): <input type="time"> was set aside for the reasons given for <input
// type="date"> in date-picker.js; the element keeps its contract: value is HH:MM (24-hour), and it
// fires input and change and takes part in forms.
//
//   <ui-time-picker role="group" aria-label="Alarm" value="09:41" clock="12" name="alarm">
//
// Attributes: value (HH:MM; the initial value, as on <input>, see date-picker.js), clock ("12"
// or "24"; default the locale's clock), disabled, name, and the strings hour-placeholder and minute-placeholder (the empty segments and
// the selectors' names; "hour", "minute"), period-label (the period selector's name, "AM/PM"),
// flyout-label ("Choose a time"), accept-label and dismiss-label. An empty field shows the
// placeholders and the period of the current time, as Fluent. The locale is the nearest lang
// attribute, else the browser's (Intl numerals, period names and order).
import { PickerElement, PickerPresenter } from "./date-picker.js";
import { localeOf } from "./calendar.js";

const pad = (n) => String(n).padStart(2, "0");

/** An HH:MM string as minutes after midnight, or null. */
function parseTime(text) {
  const m = /^(\d{2}):(\d{2})$/.exec(text ?? "");
  if (!m || Number(m[1]) > 23 || Number(m[2]) > 59) return null;
  return Number(m[1]) * 60 + Number(m[2]);
}

const timeText = (minutes) => (minutes === null ? "" : `${pad(Math.floor(minutes / 60))}:${pad(minutes % 60)}`);

// The locale's clock (12 or 24 hours).
function localeClock(locale) {
  const cycle = new Intl.DateTimeFormat(locale, { hour: "numeric" }).resolvedOptions().hourCycle;
  return cycle === "h11" || cycle === "h12" ? 12 : 24;
}

// The locale's period names (for 09:00 and 21:00) and the order of the parts on a clock.
function timeParts(locale, clock) {
  const format = new Intl.DateTimeFormat(locale, { hour: "numeric", minute: "2-digit", hourCycle: clock === 12 ? "h12" : "h23", timeZone: "UTC" });
  const at = (h) => format.formatToParts(new Date(Date.UTC(2000, 0, 1, h)));
  const period = (h) => at(h).find((p) => p.type === "dayPeriod")?.value ?? (h < 12 ? "AM" : "PM");
  const order = at(9).map((p) => (p.type === "dayPeriod" ? "period" : p.type)).filter((t) => t === "hour" || t === "minute" || t === "period");
  const wanted = clock === 12 ? ["hour", "minute", "period"] : ["hour", "minute"];
  return {
    periods: [period(9), period(21)],
    order: order.length === wanted.length && wanted.every((t) => order.includes(t)) ? order : wanted,
  };
}

function timeModel(state, locale, clock, labels = {}) {
  const number = new Intl.NumberFormat(locale, { useGrouping: false });
  const twoDigits = new Intl.NumberFormat(locale, { minimumIntegerDigits: 2, useGrouping: false });
  const { periods, order } = timeParts(locale, clock);
  const columnOf = {
    // A 12-hour clock lists 1 to 12; a 24-hour clock 0 to 23.
    hour: () => (clock === 12
      ? { count: 12, index: ((state.h + 11) % 12), text: (i) => number.format(i + 1), valueNow: (i) => i + 1, valueMin: 1, valueMax: 12 }
      : { count: 24, index: state.h, text: (i) => number.format(i), valueNow: (i) => i, valueMin: 0, valueMax: 23 }),
    minute: () => ({ count: 60, index: state.m, text: (i) => twoDigits.format(i), valueNow: (i) => i, valueMin: 0, valueMax: 59 }),
    period: () => ({ count: 2, index: state.h < 12 ? 0 : 1, loop: false, text: (i) => periods[i], valueNow: (i) => i, valueMin: 0, valueMax: 1 }),
  };
  return {
    columns: () => order.map((part) => ({ loop: true, ...columnOf[part](), part, label: labels[part] ?? part, fr: 1 })),
    set(part, i) {
      if (part === "minute") state.m = i;
      if (part === "period") state.h = (state.h % 12) + (i ? 12 : 0);
      if (part === "hour") state.h = clock === 12 ? ((i + 1) % 12) + (state.h >= 12 ? 12 : 0) : i;
    },
  };
}

/**
 * A TimePicker flyout shown on its own, for previews (the catalog's "shown inline"): the flyout
 * element, not a popover, showing `value` (HH:MM) on `clock` in `locale`.
 */
export function createTimePickerPresenter({ value, clock, locale = navigator.language } = {}) {
  const minutes = parseTime(value) ?? 0;
  const state = { h: Math.floor(minutes / 60), m: minutes % 60 };
  const model = timeModel(state, locale, clock ?? localeClock(locale), { period: "AM/PM" });
  return new PickerPresenter(model, { label: "Choose a time" }).element;
}

export class UiTimePicker extends PickerElement {
  static observedAttributes = ["value", "clock", "disabled", "lang", "data-state", "hour-placeholder", "minute-placeholder",
    "period-label", "flyout-label", "accept-label", "dismiss-label"];

  #value = null; // minutes after midnight

  setValueFromAttribute(text) {
    this.#value = parseTime(text);
  }

  get value() {
    return timeText(this.#value);
  }

  set value(text) {
    this.dirty = true;
    this.#value = parseTime(String(text ?? ""));
    if (this.field) this.renderField();
  }

  /** The clock in use: 12 or 24. */
  get clock() {
    const attr = this.getAttribute("clock");
    return attr === "12" ? 12 : attr === "24" ? 24 : localeClock(localeOf(this));
  }

  #labels() {
    return {
      hour: this.string("hour-placeholder", "hour"),
      minute: this.string("minute-placeholder", "minute"),
      period: this.string("period-label", "AM/PM"),
    };
  }

  segments() {
    const locale = localeOf(this);
    const empty = this.#value === null;
    const now = new Date();
    const state = empty ? { h: now.getHours(), m: 0 } : { h: Math.floor(this.#value / 60), m: this.#value % 60 };
    const columns = timeModel(state, locale, this.clock).columns();
    const labels = this.#labels();
    return columns.map((c) => ({
      part: c.part, fr: 1,
      text: empty && c.part !== "period" ? labels[c.part] : c.text(c.index),
      placeholder: empty,
    }));
  }

  flyoutLabel() {
    return this.string("flyout-label", "Choose a time");
  }

  initialState() {
    if (this.#value !== null) return { h: Math.floor(this.#value / 60), m: this.#value % 60 };
    const now = new Date();
    return { h: now.getHours(), m: now.getMinutes() };
  }

  model() {
    return timeModel(this.state ?? this.initialState(), localeOf(this), this.clock, this.#labels());
  }

  commit({ h, m }) {
    this.value = timeText(h * 60 + m);
  }
}

if (!customElements.get("ui-time-picker")) customElements.define("ui-time-picker", UiTimePicker);
