// Pickers: DatePicker, TimePicker and CalendarDatePicker in every state, their flyouts shown
// inline, and the Calendar's month view, year view and disabled look. The samples show today, so
// the Calendar shows today's look; the page changes with the date it is shown on.
import "../../blocks/calendar.js";
import { createDatePickerPresenter } from "../../blocks/date-picker.js";
import { createTimePickerPresenter } from "../../blocks/time-picker.js";
import "../../blocks/calendar-date-picker.js";
import { el, caption, page, section, sideScroll } from "./ui.js";
import { stateGrid } from "./state-grid.js";

const pad = (n) => String(n).padStart(2, "0");
const iso = (d) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

const now = new Date();
const SAMPLE = iso(now);
// The month sample selects the day before today (after it on the 1st), so today shows in accent text.
const BESIDE = iso(new Date(now.getFullYear(), now.getMonth(), now.getDate() + (now.getDate() > 1 ? -1 : 1)));

function picker(tag, label, attributes = {}) {
  const node = document.createElement(tag);
  node.setAttribute("aria-label", label);
  for (const [k, v] of Object.entries(attributes)) node.setAttribute(k, v);
  return node;
}

// Forced states go on the element, which passes them to its field; Pressed has no look of its own.
function force(sample, state) {
  if (state === "Hover") sample.dataset.state = "hover";
  if (state === "Pressed") sample.dataset.state = "pressed";
  if (state === "Focused") sample.dataset.state = "focus";
  if (state === "Disabled") sample.disabled = true;
}

// A caption above a sample, 8 apart, the caption wrapping at 280; only the sample scrolls sideways.
function labelled(text, control) {
  const c = caption(text);
  c.style.maxInlineSize = "280px";
  const stack = el("div", "catalog-stack", c, sideScroll(control));
  stack.style.gap = "var(--ui-spacing-s)";
  return stack;
}

function row(...items) {
  const r = el("div", "", ...items);
  Object.assign(r.style, { display: "flex", flexWrap: "wrap", gap: "var(--ui-spacing-xl)", alignItems: "flex-start" });
  return r;
}

function calendar(attributes) {
  return picker("ui-calendar", "Calendar", attributes);
}

function inline(presenter, width) {
  presenter.style.inlineSize = `${width}px`;
  return presenter;
}

export default {
  id: "pickers",
  title: "Pickers",
  icon: "calendar",
  render: () => page("Pickers",
    section("DatePicker", stateGrid([
      ["Empty", () => picker("ui-date-picker", "Date")],
      ["With a date", () => picker("ui-date-picker", "Date", { value: SAMPLE })],
    ], { force })),
    section("TimePicker", stateGrid([
      ["12-hour", () => picker("ui-time-picker", "Time", { value: "09:41", clock: "12" })],
      ["24-hour", () => picker("ui-time-picker", "Time", { value: "21:41", clock: "24" })],
    ], { force })),
    section("CalendarDatePicker", stateGrid([
      ["Empty", () => picker("ui-calendar-date-picker", "Date", { style: "inline-size: 200px" })],
      ["With a date", () => picker("ui-calendar-date-picker", "Date", { value: SAMPLE, style: "inline-size: 200px" })],
    ], { force })),
    section("Flyouts (shown inline)", row(
      labelled("DatePicker", inline(createDatePickerPresenter({ value: SAMPLE, locale: document.documentElement.lang || undefined }), 296)),
      labelled("TimePicker", inline(createTimePickerPresenter({ value: "09:41", clock: 12, locale: document.documentElement.lang || undefined }), 242)))),
    section("Calendar", row(
      labelled("Month: today in accent text, the selected day filled", calendar({ value: BESIDE })),
      labelled("Year view", calendar({ value: SAMPLE, "display-mode": "year" })),
      labelled("Disabled", calendar({ value: SAMPLE, disabled: "" }))))),
};
