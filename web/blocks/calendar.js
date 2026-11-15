// Calendar (0038, 0046, 0048): <ui-calendar>, a panel for choosing a day, with month, year and
// decade views. No native element shows a month grid (<input type="date"> opens the browser's own
// picker), so it is a custom element. It builds its own content:
//
//   <ui-calendar class="ui-squircle" role="group" aria-label="…" value="2026-11-13">
//     <div class="ui-calendar-header">
//       <button class="ui-button ui-subtle ui-calendar-title …">November 2026</button>
//       <button class="ui-button ui-subtle ui-calendar-previous …" aria-label="Previous month">‹</button>
//       <button class="ui-button ui-subtle ui-calendar-next …" aria-label="Next month">›</button>
//     </div>
//     <table class="ui-calendar-grid" role="grid" aria-labelledby="(title)">
//       <thead><tr><th scope="col" abbr="Sunday">Su</th>…</tr></thead>        month view only
//       <tbody><tr><td class="ui-calendar-cell ui-squircle ui-focus-ring" tabindex="-1"
//         aria-selected="false" aria-label="Friday, November 13, 2026" data-value="2026-11-13">13</td>…
//     </table>
//   </ui-calendar>
//
// The ARIA grid pattern: the grid is one Tab stop (roving tabindex on its cells, the keyboard
// cursor's cell holding tabindex 0); the selected cell has aria-selected="true", today
// aria-current="date", days outside the shown month data-outside, unavailable days (before min or
// after max) aria-disabled="true". Keys are Fluent's (0038): Left and Right move a day (a month or
// a year in the other views; swapped under dir="rtl"), Up and Down a week (four cells in the other
// views), Page Up and Page Down a month (a year, a decade), Home and End the first and last day of
// the month (cell of the view), Ctrl+Up the next view out (month, year, decade), Ctrl+Down and
// Enter or Space the next view in; in the month view Enter or Space selects the day. A click
// selects a day, or opens a month or year. The ring shows on the cursor's cell while focus came from
// the keyboard (:focus-visible), and nowhere after a pointer press.
//
// Attributes: value (YYYY-MM-DD, the initial value as on <input>: once the user or the value
// property changes the value, the attribute no longer applies; a form reset goes back to it), min and max (YYYY-MM-DD),
// display-mode (month, year, decade), first-day-of-week (0 Sunday to 6 Saturday; default from the
// locale), disabled, name, and the strings previous-label and next-label (default "Previous month"
// and "Next month", "Previous year", … by view). The locale is the nearest lang attribute, else the
// browser's; names come from Intl. Properties: value, valueAsDate (a UTC Date or null),
// displayMode, displayDate, disabled; focusCursor() focuses the cursor's cell. It fires `change`
// when the user selects another day, and `ui-calendar-pick` whenever the user picks a day (changed
// or not; the CalendarDatePicker closes on it). It is form-associated (it submits its value with
// `name`).
import { icon } from "./icon.js";
import "./button.js";

// --- Dates, shared with the pickers: UTC Dates at midnight, so no time zone moves a day. ---------

const pad = (n, size = 2) => String(n).padStart(size, "0");

/** The UTC midnight Date for year, month (0-11), day; months and days roll over. */
export function utcDate(year, month, day) {
  const date = new Date(Date.UTC(2000, 0, 1));
  date.setUTCFullYear(year, month, day);
  return date;
}

/** Today at UTC midnight, in the local calendar. */
export function today() {
  const now = new Date();
  return utcDate(now.getFullYear(), now.getMonth(), now.getDate());
}

export const daysInMonth = (year, month) => utcDate(year, month + 1, 0).getUTCDate();

/** A YYYY-MM-DD string as a UTC Date, or null when it is not a valid date. */
export function parseIsoDate(text) {
  const m = /^(\d{4,})-(\d{2})-(\d{2})$/.exec(text ?? "");
  if (!m) return null;
  const [y, mo, d] = [Number(m[1]), Number(m[2]) - 1, Number(m[3])];
  return mo <= 11 && d >= 1 && d <= daysInMonth(y, mo) ? utcDate(y, mo, d) : null;
}

/** A UTC Date as YYYY-MM-DD ("" for null). */
export const isoDate = (date) => (date ? `${pad(date.getUTCFullYear(), 4)}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())}` : "");

/** The locale of an element: its nearest lang attribute when the browser knows it, else the browser's. */
export function localeOf(element) {
  const lang = element.closest?.("[lang]")?.getAttribute("lang");
  try {
    if (lang && Intl.DateTimeFormat.supportedLocalesOf(lang).length) return lang;
  } catch {
    // An invalid tag falls back to the browser's locale.
  }
  return navigator.language || "en-US";
}

/** Formats a UTC Date with Intl options in the UTC time zone. */
export const formatDate = (locale, date, options) => new Intl.DateTimeFormat(locale, { ...options, timeZone: "UTC" }).format(date);

/** The locale's first day of the week, 0 (Sunday) to 6. */
function firstDayOf(locale) {
  try {
    const l = new Intl.Locale(locale);
    const info = l.getWeekInfo?.() ?? l.weekInfo;
    if (info?.firstDay) return info.firstDay % 7;
  } catch {
    // No week data: Sunday, as Fluent's invariant default.
  }
  return 0;
}

/**
 * The weekday headings for a locale from `first`: the short names, cut to two characters when the
 * seven stay distinct (Fluent shows .NET's shortest names: Su, Mo, …), with the full names.
 */
function weekdayNames(locale, first) {
  const days = Array.from({ length: 7 }, (_, i) => utcDate(2023, 0, 1 + ((first + i) % 7))); // 2023-01-01 is a Sunday
  const full = days.map((d) => formatDate(locale, d, { weekday: "long" }));
  const short = days.map((d) => formatDate(locale, d, { weekday: "short" }).replace(/\.$/, ""));
  const shortest = short.map((s) => Array.from(s).slice(0, 2).join(""));
  return { full, short: new Set(shortest).size === 7 ? shortest : short };
}

const sameDay = (a, b) => a && b && a.getTime() === b.getTime();
const MODES = ["month", "year", "decade"];

function setAttr(el, name, value) {
  if (value === null) {
    if (el.hasAttribute(name)) el.removeAttribute(name);
  } else if (el.getAttribute(name) !== value) el.setAttribute(name, value);
}

function button(className, ...content) {
  const b = document.createElement("button");
  b.type = "button";
  b.className = `ui-button ui-subtle ui-squircle ui-focus-ring ${className}`;
  b.append(...content);
  return b;
}

let nextId = 0;

export class UiCalendar extends HTMLElement {
  static formAssociated = true;
  static observedAttributes = ["value", "min", "max", "display-mode", "disabled", "first-day-of-week", "lang", "previous-label", "next-label"];

  #internals = this.attachInternals();
  #value = null; // the selected day
  #dirty = false; // set once the user or the value property changes the value; the attribute then no longer applies
  #cursor = today(); // the keyboard cursor: a day (month view), a month (year view), a year (decade view)
  #mode = "month";
  #formDisabled = false;
  #built = false;
  #title;
  #previous;
  #next;
  #grid;

  connectedCallback() {
    if (!this.#built) this.#build();
    this.#render();
  }

  attributeChangedCallback(name, old, value) {
    if (name === "value" && !this.#dirty) this.#setValue(parseIsoDate(value));
    if (name === "display-mode") this.#mode = MODES.includes(value) ? value : "month";
    if (this.#built) this.#render();
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
    this.#setValue(date ? utcDate(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()) : null);
    if (this.#built) this.#render();
  }

  #setValue(date) {
    this.#value = date;
    if (date) this.#cursor = date;
  }

  get displayMode() {
    return this.#mode;
  }

  set displayMode(mode) {
    this.setAttribute("display-mode", mode);
  }

  /** The day the month view shows, and the keyboard cursor's date. */
  get displayDate() {
    return new Date(this.#cursor);
  }

  set displayDate(date) {
    this.#cursor = utcDate(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate());
    if (this.#built) this.#render();
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

  formResetCallback() {
    this.#dirty = false;
    this.#setValue(parseIsoDate(this.getAttribute("value")));
    if (this.#built) this.#render();
  }

  formDisabledCallback(disabled) {
    this.#formDisabled = disabled;
    if (this.#built) this.#render();
  }

  /** Moves focus to the keyboard cursor's cell. */
  focusCursor(options) {
    this.#grid.querySelector(".ui-calendar-cell[tabindex='0']")?.focus(options);
  }

  #build() {
    this.#built = true;
    this.classList.add("ui-squircle");
    if (!this.hasAttribute("role")) this.setAttribute("role", "group");
    const id = `ui-calendar-${nextId++}`;
    this.#title = button("ui-calendar-title");
    this.#title.id = `${id}-title`;
    this.#title.setAttribute("aria-live", "polite");
    this.#previous = button("ui-calendar-previous ui-calendar-arrow", icon("caret-left"));
    this.#next = button("ui-calendar-next ui-calendar-arrow", icon("caret-right"));
    const header = document.createElement("div");
    header.className = "ui-calendar-header";
    header.append(this.#title, this.#previous, this.#next);
    this.#grid = document.createElement("table");
    this.#grid.className = "ui-calendar-grid";
    this.#grid.setAttribute("role", "grid");
    this.#grid.setAttribute("aria-labelledby", this.#title.id);
    this.replaceChildren(header, this.#grid);

    this.#title.addEventListener("click", () => this.#zoom(1));
    this.#previous.addEventListener("click", () => this.#page(-1));
    this.#next.addEventListener("click", () => this.#page(1));
    this.#grid.addEventListener("click", (e) => this.#onCellClick(e));
    this.#grid.addEventListener("keydown", (e) => this.#onKeyDown(e));
  }

  #locale() {
    return localeOf(this);
  }

  #bounds() {
    return { min: parseIsoDate(this.getAttribute("min")), max: parseIsoDate(this.getAttribute("max")) };
  }

  #available(date) {
    const { min, max } = this.#bounds();
    return !(min && date < min) && !(max && date > max);
  }

  #render() {
    const locale = this.#locale();
    const disabled = this.disabled;
    setAttr(this, "aria-disabled", disabled ? "true" : null);
    this.#internals.setFormValue(this.value);
    const c = this.#cursor;
    const y = c.getUTCFullYear();
    const decade = Math.floor(y / 10) * 10;
    const yearText = (year) => formatDate(locale, utcDate(year, 0, 1), { year: "numeric" });
    this.#title.textContent = this.#mode === "month" ? formatDate(locale, c, { month: "long", year: "numeric" })
      : this.#mode === "year" ? yearText(y)
        : `${yearText(decade)}-${yearText(decade + 9)}`;
    const unit = { month: "month", year: "year", decade: "decade" }[this.#mode];
    this.#previous.setAttribute("aria-label", this.getAttribute("previous-label") ?? `Previous ${unit}`);
    this.#next.setAttribute("aria-label", this.getAttribute("next-label") ?? `Next ${unit}`);
    this.#title.disabled = disabled;
    this.#previous.disabled = disabled;
    this.#next.disabled = disabled;
    this.#grid.dataset.mode = this.#mode;

    const refocus = this.#grid.contains(document.activeElement);
    this.#grid.replaceChildren(...(this.#mode === "month" ? this.#monthView(locale) : this.#cellView(locale)));
    if (refocus) this.focusCursor();
  }

  #cell(text, { label, value, cursor, selected, current, outside, unavailable }) {
    const td = document.createElement("td");
    td.className = "ui-calendar-cell ui-squircle ui-focus-ring";
    td.textContent = text;
    td.dataset.value = value;
    if (label) td.setAttribute("aria-label", label);
    td.setAttribute("aria-selected", selected ? "true" : "false");
    if (current) td.setAttribute("aria-current", "date");
    if (outside) td.dataset.outside = "";
    if (unavailable) td.setAttribute("aria-disabled", "true");
    if (!this.disabled) td.tabIndex = cursor ? 0 : -1;
    return td;
  }

  #monthView(locale) {
    const first = this.hasAttribute("first-day-of-week") ? ((Number(this.getAttribute("first-day-of-week")) % 7) + 7) % 7 : firstDayOf(locale);
    const names = weekdayNames(locale, Number.isFinite(first) ? first : 0);
    const head = document.createElement("thead");
    const headRow = head.insertRow();
    names.short.forEach((name, i) => {
      const th = document.createElement("th");
      th.scope = "col";
      th.abbr = names.full[i];
      th.textContent = name;
      headRow.append(th);
    });
    const c = this.#cursor;
    const start = utcDate(c.getUTCFullYear(), c.getUTCMonth(), 1);
    // Six weeks; a month starting on the first weekday shows the whole week before it, as Fluent.
    const offset = ((start.getUTCDay() - first + 7) % 7) || 7;
    const body = document.createElement("tbody");
    const now = today();
    for (let week = 0; week < 6; week++) {
      const row = body.insertRow();
      for (let day = 0; day < 7; day++) {
        const date = utcDate(start.getUTCFullYear(), start.getUTCMonth(), 1 - offset + week * 7 + day);
        row.append(this.#cell(formatDate(locale, date, { day: "numeric" }), {
          label: formatDate(locale, date, { dateStyle: "full" }),
          value: isoDate(date),
          cursor: sameDay(date, c),
          selected: sameDay(date, this.#value),
          current: sameDay(date, now),
          outside: date.getUTCMonth() !== c.getUTCMonth(),
          unavailable: !this.#available(date),
        }));
      }
    }
    return [head, body];
  }

  // The year view's months, or the decade view's years (the decade with a year either side).
  #cellView(locale) {
    const c = this.#cursor;
    const y = c.getUTCFullYear();
    const body = document.createElement("tbody");
    const sel = this.#value;
    const { min, max } = this.#bounds();
    const decade = Math.floor(y / 10) * 10;
    for (let r = 0; r < 3; r++) {
      const row = body.insertRow();
      for (let k = 0; k < 4; k++) {
        const i = r * 4 + k;
        if (this.#mode === "year") {
          const date = utcDate(y, i, 1);
          row.append(this.#cell(formatDate(locale, date, { month: "short" }), {
            label: formatDate(locale, date, { month: "long", year: "numeric" }),
            value: isoDate(date).slice(0, 7),
            cursor: i === c.getUTCMonth(),
            selected: sel && sel.getUTCFullYear() === y && sel.getUTCMonth() === i,
            unavailable: (min && utcDate(y, i + 1, 0) < min) || (max && date > max),
          }));
        } else {
          const year = decade - 1 + i;
          row.append(this.#cell(formatDate(locale, utcDate(year, 0, 1), { year: "numeric" }), {
            value: pad(year, 4),
            cursor: year === y,
            selected: sel && sel.getUTCFullYear() === year,
            outside: year < decade || year > decade + 9,
            unavailable: (min && year < min.getUTCFullYear()) || (max && year > max.getUTCFullYear()),
          }));
        }
      }
    }
    return [body];
  }

  // Moves the cursor to the day in that month (months roll over; the day is kept within the
  // month) and redraws.
  #moveTo(year, month, day) {
    const first = utcDate(year, month, 1);
    const [y, m] = [first.getUTCFullYear(), first.getUTCMonth()];
    this.#cursor = utcDate(y, m, Math.min(day, daysInMonth(y, m)));
    this.#render();
  }

  #page(step) {
    const c = this.#cursor;
    const [y, m, d] = [c.getUTCFullYear(), c.getUTCMonth(), c.getUTCDate()];
    if (this.#mode === "month") this.#moveTo(y, m + step, d);
    else this.#moveTo(y + step * (this.#mode === "year" ? 1 : 10), m, d);
  }

  // Out (+1) to the year and decade views, or in (-1).
  #zoom(step) {
    const i = MODES.indexOf(this.#mode) + step;
    if (i < 0 || i >= MODES.length) return false;
    this.#mode = MODES[i];
    this.#render();
    return true;
  }

  #select(date) {
    if (!this.#available(date)) return;
    const changed = !sameDay(date, this.#value);
    this.#dirty = true;
    this.#setValue(date);
    this.#render();
    if (changed) this.dispatchEvent(new Event("change", { bubbles: true }));
    this.dispatchEvent(new CustomEvent("ui-calendar-pick", { bubbles: true }));
  }

  #openCell(cell) {
    const c = this.#cursor;
    if (this.#mode === "month") {
      this.#select(parseIsoDate(cell.dataset.value));
    } else if (this.#mode === "year") {
      this.#mode = "month";
      this.#moveTo(c.getUTCFullYear(), Number(cell.dataset.value.slice(5, 7)) - 1, c.getUTCDate());
    } else {
      this.#mode = "year";
      this.#moveTo(Number(cell.dataset.value), c.getUTCMonth(), c.getUTCDate());
    }
  }

  #onCellClick(e) {
    const cell = e.target.closest(".ui-calendar-cell");
    if (!cell || this.disabled || cell.getAttribute("aria-disabled") === "true") return;
    this.#openCell(cell);
    this.focusCursor({ preventScroll: true });
  }

  #onKeyDown(e) {
    const cell = e.target.closest(".ui-calendar-cell");
    if (!cell || this.disabled || e.altKey || e.metaKey) return;
    const rtl = getComputedStyle(this).direction === "rtl";
    const c = this.#cursor;
    const [y, m, d] = [c.getUTCFullYear(), c.getUTCMonth(), c.getUTCDate()];
    const month = this.#mode === "month";
    const year = this.#mode === "year";
    const across = { ArrowLeft: rtl ? 1 : -1, ArrowRight: rtl ? -1 : 1, ArrowUp: month ? -7 : -4, ArrowDown: month ? 7 : 4 };
    if (e.ctrlKey) {
      if (e.key === "ArrowUp") this.#zoom(1);
      else if (e.key === "ArrowDown") {
        if (month) return;
        this.#openCell(cell);
      } else return;
    } else if (e.key in across) {
      const n = across[e.key];
      if (month) {
        this.#cursor = utcDate(y, m, d + n);
        this.#render();
      } else if (year) this.#moveTo(y, m + n, d);
      else this.#moveTo(y + n, m, d);
    } else if (e.key === "PageUp" || e.key === "PageDown") {
      const n = e.key === "PageUp" ? -1 : 1;
      if (month) this.#moveTo(y, m + n, d);
      else this.#moveTo(y + n * (year ? 1 : 10), m, d);
    } else if (e.key === "Home" || e.key === "End") {
      const end = e.key === "End";
      if (month) this.#moveTo(y, m, end ? daysInMonth(y, m) : 1);
      else if (year) this.#moveTo(y, end ? 11 : 0, d);
      else this.#moveTo(Math.floor(y / 10) * 10 + (end ? 9 : 0), m, d);
    } else if (e.key === "Enter" || e.key === " ") {
      if (cell.getAttribute("aria-disabled") !== "true") this.#openCell(cell);
    } else return;
    e.preventDefault();
    this.focusCursor();
  }
}

if (!customElements.get("ui-calendar")) customElements.define("ui-calendar", UiCalendar);
