// Status bar (0047): <ui-status-bar> and its progress item <ui-status-progress>. Markup and look
// are in status-bar.css.
//
// <ui-status-bar>
// - A labelled role="group" (aria-label "Status" unless the page names it): ARIA has no status bar
//   role, and role="status" on the whole bar would announce every readout. The message alone is
//   the polite live region: a .ui-status-bar-message span with role="status", made first in the
//   bar if the markup has none, so it exists before its text changes. The message attribute (or
//   the message property) sets its text.
// - Rows: .ui-status-bar-row children, shown above the main row in markup order; `hidden` hides
//   one. The bar's other children are the main row's items, a row's children its items. Each row
//   fits on its own.
// - Items: data-zone ("leading", the default, "center" or "trailing"), data-priority (an integer,
//   default 0) and data-short-texts (a JSON array of shorter texts, longest first). A <button> or
//   .ui-button item is clickable. An item's text is its first non-blank text node, looked for in
//   the item and its children (not in icons).
// - Fitting: a row never wraps and has no overflow menu. When its items do not fit, every item
//   takes its shorter texts, then items hide, both highest priority first and ties from the
//   trailing end; once an item hides, the shortened items still shown lengthen again in the room
//   it left, the last shortened first, while the row fits. The message never hides; it is trimmed.
//   A text the page sets while a shorter form shows becomes the item's full text, and the form at
//   the same level shows again before the next paint.
// - Items keep the markup's Tab order; text items take no focus; hidden items and rows take none.
//
// <ui-status-progress>: a <progress> Block (kind="bar", 160 px, or kind="ring", 16 px) with value
// (0 to 100; indeterminate without one, or with the indeterminate attribute), its text (text) and,
// with cancelable, a cancel button named and tooltipped by cancel-label (default "Cancel") that
// fires a "cancel" event on the element. The progress is named by its text.
import { icon } from "../blocks/icon.js";
import { tooltip, tooltipOf } from "../blocks/tooltip.js";
import "../blocks/progress.js";

/** The bar's name unless the page sets one, and the cancel button's default name. */
export const STATUS_TEXT = "Status";
export const CANCEL_TEXT = "Cancel";

// Desktop sizes (0047): the side padding (SpacingL), the gap between text items (SpacingL), between
// clickable fills (SpacingXS), and a clickable item's text inset in its fill (SpacingS).
const PADDING = 16;
const TEXT_GAP = 16;
const CLICKABLE_GAP = 4;
const CLICKABLE_INSET = 8;
const ZONES = { leading: 0, center: 1, trailing: 2 };
const EPSILON = 0.01;

const isItem = (el) => !el.matches(".ui-status-bar-row, .ui-status-bar-message, [popover], .ui-tooltip, script, template");
const isClickable = (el) => el.matches("button, .ui-button");
const zoneOf = (el) => ZONES[el.dataset.zone] ?? 0;
const priorityOf = (el) => Number.parseInt(el.dataset.priority ?? "0", 10) || 0;

function shortTextsOf(el) {
  if (!el.dataset.shortTexts) return [];
  try {
    const forms = JSON.parse(el.dataset.shortTexts);
    return Array.isArray(forms) ? forms.map(String) : [];
  } catch {
    return [];
  }
}

// The item's text: its first non-blank text node, in the item or its children (not in icons,
// tooltips or menus).
function textNodeOf(item) {
  for (const node of item.childNodes) if (node.nodeType === 3 && node.data.trim()) return node;
  for (const child of item.children) {
    if (child.matches("svg, .ui-tooltip, [popover]")) continue;
    for (const node of child.childNodes) if (node.nodeType === 3 && node.data.trim()) return node;
  }
  return null;
}

const canvas = document.createElement("canvas").getContext("2d");

function textWidth(node, text) {
  if (!text) return 0;
  const cs = getComputedStyle(node.parentElement);
  canvas.font = `${cs.fontStyle} ${cs.fontWeight} ${cs.fontSize} ${cs.fontFamily}`;
  canvas.letterSpacing = cs.letterSpacing === "normal" ? "0px" : cs.letterSpacing;
  return Math.ceil(canvas.measureText(text).width);
}

// The item's laid-out width, untouched by a pressed button's scale.
function boxWidth(el) {
  const cs = getComputedStyle(el);
  const width = parseFloat(cs.width) || 0;
  if (cs.boxSizing === "border-box") return width;
  return width + ["paddingLeft", "paddingRight", "borderLeftWidth", "borderRightWidth"].reduce((sum, p) => sum + (parseFloat(cs[p]) || 0), 0);
}

// --- Fitting (the desktop StatusBarRow.Fit) ---------------------------------------------------

const gap = (a, b) => (a.inset > 0 && b.inset > 0 ? CLICKABLE_GAP : TEXT_GAP - a.inset - b.inset);
const widthIn = (entry, plan) => entry.widths[plan.levels.get(entry.item) ?? 0];

/** The width a row needs for its shown entries: the padding (less the end items' insets), the items and the gaps. */
export function required(entries, plan) {
  const shown = entries.filter((e) => !plan.hidden.has(e.item));
  if (!shown.length) return 0;
  let total = 2 * PADDING - shown[0].inset - shown.at(-1).inset;
  shown.forEach((e, i) => {
    total += widthIn(e, plan) + (i > 0 ? gap(shown[i - 1], e) : 0);
  });
  return total;
}

/** Which entries hide and which shorter form each shows, for a row `width` wide. */
export function plan(entries, width) {
  const result = { hidden: new Set(), levels: new Map() };
  const fits = () => required(entries, result) <= width + EPSILON;
  const candidates = entries.map((e, i) => ({ e, i })).filter((x) => !x.e.message)
    .sort((a, b) => b.e.priority - a.e.priority || b.i - a.i).map((x) => x.e);
  for (const entry of candidates) {
    while (!fits() && (result.levels.get(entry.item) ?? 0) < entry.widths.length - 1) {
      result.levels.set(entry.item, (result.levels.get(entry.item) ?? 0) + 1);
    }
  }
  for (const entry of candidates) if (!fits()) result.hidden.add(entry.item);
  if (!result.hidden.size) return result;
  for (const entry of [...candidates].reverse().filter((e) => !result.hidden.has(e.item))) {
    let level;
    while ((level = result.levels.get(entry.item) ?? 0) > 0) {
      result.levels.set(entry.item, level - 1);
      if (fits()) continue;
      result.levels.set(entry.item, level);
      break;
    }
  }
  return result;
}

/** Each shown entry's inline offset and width (the message's trimmed), for a row `width` wide. */
export function place(entries, plan, width) {
  const shown = entries.filter((e) => !plan.hidden.has(e.item));
  const placed = [];
  if (!shown.length) return placed;
  const lead = shown.filter((e) => e.zone === 0);
  const centre = shown.filter((e) => e.zone === 1);
  const trail = shown.filter((e) => e.zone === 2);
  const excess = Math.max(0, required(entries, plan) - width);
  const w = (e) => (e.message ? Math.max(0, widthIn(e, plan) - excess) : widthIn(e, plan));
  const span = (zone) => zone.reduce((sum, e, i) => sum + w(e) + (i > 0 ? gap(zone[i - 1], e) : 0), 0);

  let x = lead.length ? PADDING - lead[0].inset : 0;
  lead.forEach((e, i) => {
    if (i > 0) x += gap(lead[i - 1], e);
    placed.push({ entry: e, x, width: w(e) });
    x += w(e);
  });
  const leadEnd = x;

  const trailStart = trail.length ? width - PADDING + trail.at(-1).inset - span(trail) : width;
  let right = trailStart;
  trail.forEach((e, i) => {
    placed.push({ entry: e, x: right, width: w(e) });
    right += w(e) + (i + 1 < trail.length ? gap(e, trail[i + 1]) : 0);
  });

  if (centre.length) {
    const s = span(centre);
    const lo = lead.length ? leadEnd + gap(lead.at(-1), centre[0]) : PADDING - centre[0].inset;
    const hi = (trail.length ? trailStart - gap(centre.at(-1), trail[0]) : width - PADDING + centre.at(-1).inset) - s;
    let cx = Math.min(Math.max((width - s) / 2, lo), Math.max(lo, hi));
    centre.forEach((e, i) => {
      if (i > 0) cx += gap(centre[i - 1], e);
      placed.push({ entry: e, x: cx, width: w(e) });
      cx += w(e);
    });
  }
  return placed;
}

// --- The bar ----------------------------------------------------------------------------------

export class UiStatusBar extends HTMLElement {
  static observedAttributes = ["message"];

  #message = null;
  #shortened = new Map(); // item -> { full, form, level }
  #mutations = new MutationObserver((records) => this.#mutated(records));
  #resize = new ResizeObserver(() => this.fit());
  #observed = new Set();
  #writing = false;

  connectedCallback() {
    if (!this.hasAttribute("role")) this.setAttribute("role", "group");
    if (!this.hasAttribute("aria-label") && !this.hasAttribute("aria-labelledby")) this.setAttribute("aria-label", STATUS_TEXT);
    this.#message = this.querySelector(":scope > .ui-status-bar-message");
    if (!this.#message) {
      this.#message = document.createElement("span");
      this.#message.className = "ui-status-bar-message";
      this.prepend(this.#message);
    }
    this.#message.setAttribute("role", "status");
    if (this.hasAttribute("message")) this.#message.textContent = this.getAttribute("message");
    this.#mutations.observe(this, {
      subtree: true, childList: true, characterData: true, attributes: true,
      attributeFilter: ["data-zone", "data-priority", "data-short-texts", "hidden", "class", "disabled"],
    });
    this.#resize.observe(this);
    document.fonts?.ready.then(() => this.fit());
    this.fit();
  }

  disconnectedCallback() {
    this.#mutations.disconnect();
    this.#resize.disconnect();
    this.#observed.clear();
  }

  attributeChangedCallback(name, old, value) {
    if (name === "message" && this.#message) this.#message.textContent = value ?? "";
  }

  /** The status message: the main row's leading text, announced politely when it changes. */
  get message() {
    return this.#message?.textContent ?? this.getAttribute("message") ?? "";
  }

  set message(value) {
    this.setAttribute("message", value ?? "");
  }

  /** The rows above the main row, top to bottom. */
  get rows() {
    return [...this.children].filter((el) => el.matches(".ui-status-bar-row"));
  }

  /** The items of the main row (no `row`) or of `row`, in markup order. */
  items(row = this) {
    return [...row.children].filter(isItem);
  }

  // A page change: a text set where a shorter form shows becomes the full text, and the form at
  // the same level shows again at once; then the rows fit again.
  #mutated(records) {
    if (this.#writing) return;
    const touched = new Set(records.map((r) => r.target.nodeType === 3 ? r.target.parentElement : r.target));
    for (const [item, s] of this.#shortened) {
      if (![...touched].some((node) => node && item.contains(node))) continue;
      const node = textNodeOf(item);
      const text = node?.data ?? null;
      if (text === s.form && !records.some((r) => r.target === item && r.attributeName === "data-short-texts")) continue;
      const forms = shortTextsOf(item);
      if (text !== s.form) s.full = text;
      if (forms.length && node) {
        s.level = Math.min(Math.max(s.level, 1), forms.length);
        s.form = forms[s.level - 1];
        this.#write(() => { node.data = s.form; });
      } else {
        this.#shortened.delete(item);
      }
    }
    this.fit();
  }

  #write(fn) {
    this.#writing = true;
    try {
      fn();
    } finally {
      this.#mutations.takeRecords();
      this.#writing = false;
    }
  }

  // Shows a shorter form on the item, or its full text when `form` is null.
  #show(item, form) {
    const s = this.#shortened.get(item);
    if ((s?.form ?? null) === form) return;
    const node = textNodeOf(item);
    if (!node) return;
    if (form === null) {
      this.#shortened.delete(item);
      this.#write(() => { node.data = s.full ?? ""; });
      return;
    }
    const state = s ?? { full: node.data, form: null, level: 0 };
    state.form = form;
    state.level = shortTextsOf(item).indexOf(form) + 1;
    this.#shortened.set(item, state);
    this.#write(() => { node.data = form; });
  }

  // The item's width with its full text and with each shorter form.
  #widths(item) {
    const desired = boxWidth(item);
    const forms = shortTextsOf(item);
    const node = forms.length ? textNodeOf(item) : null;
    if (!node) return [desired];
    const base = desired - textWidth(node, node.data);
    const full = this.#shortened.get(item)?.full ?? node.data;
    return [base + textWidth(node, full), ...forms.map((f) => base + textWidth(node, f))];
  }

  /** Lays out every row: places the items and applies the shorter texts and hidden items. */
  fit() {
    if (!this.isConnected) return;
    const rows = this.rows.filter((row) => !row.hidden);
    this.style.setProperty("--_main-row", String(rows.length + 1));
    for (const row of rows) this.#fitRow(row, null);
    this.#fitRow(this, this.#message);
  }

  #fitRow(row, message) {
    const items = this.items(row).filter((item) => !item.hidden && getComputedStyle(item).display !== "none");
    for (const item of items) {
      if (!this.#observed.has(item)) {
        this.#observed.add(item);
        this.#resize.observe(item);
      }
      const clickable = isClickable(item);
      if (item.classList.contains("ui-status-bar-clickable") !== clickable) {
        this.#write(() => item.classList.toggle("ui-status-bar-clickable", clickable));
      }
    }
    const width = row.clientWidth;
    const entries = [];
    if (message) {
      message.style.removeProperty("--_w");
      if (message.textContent) entries.push({ item: message, zone: 0, widths: [boxWidth(message)], priority: -Infinity, inset: 0, message: true });
    }
    items.map((item, i) => ({ item, i })).sort((a, b) => zoneOf(a.item) - zoneOf(b.item) || a.i - b.i).forEach(({ item }) => {
      entries.push({ item, zone: zoneOf(item), widths: this.#widths(item), priority: priorityOf(item), inset: isClickable(item) ? CLICKABLE_INSET : 0, message: false });
    });
    const result = plan(entries, width);
    const placed = place(entries, result, width);
    this.#write(() => {
      for (const entry of entries) {
        if (entry.message) continue;
        const forms = shortTextsOf(entry.item);
        const level = result.levels.get(entry.item) ?? 0;
        this.#show(entry.item, level > 0 && level <= forms.length ? forms[level - 1] : null);
        entry.item.toggleAttribute("data-status-bar-hidden", result.hidden.has(entry.item));
      }
      for (const { entry, x, width: w } of placed) {
        entry.item.style.setProperty("--_x", `${x}px`);
        if (entry.message && w < entry.widths[0] - EPSILON) entry.item.style.setProperty("--_w", `${w}px`);
      }
    });
  }
}

// --- The progress item ------------------------------------------------------------------------

let nextProgress = 0;

export class UiStatusProgress extends HTMLElement {
  static observedAttributes = ["kind", "value", "indeterminate", "text", "cancelable", "cancel-label"];

  #bar;
  #ring;
  #text;
  #cancel;
  #tip;

  connectedCallback() {
    if (!this.#bar) this.#build();
    this.#sync();
  }

  attributeChangedCallback() {
    if (this.#bar) this.#sync();
  }

  #build() {
    const id = `ui-status-progress-${nextProgress++}`;
    this.#bar = document.createElement("progress");
    this.#bar.className = "ui-progress-bar ui-status-progress-bar";
    this.#ring = document.createElement("progress");
    this.#ring.className = "ui-progress-ring ui-status-progress-ring";
    this.#text = document.createElement("span");
    this.#text.className = "ui-status-progress-text";
    this.#text.id = `${id}-text`;
    for (const p of [this.#bar, this.#ring]) {
      p.max = 100;
      p.setAttribute("aria-labelledby", this.#text.id);
    }
    this.#cancel = document.createElement("button");
    this.#cancel.type = "button";
    this.#cancel.className = "ui-button ui-subtle ui-squircle ui-focus-ring ui-status-bar-clickable ui-status-progress-cancel";
    this.#cancel.append(icon("x"));
    this.#cancel.addEventListener("click", () => this.dispatchEvent(new Event("cancel", { bubbles: true })));
    this.append(this.#bar, this.#ring, this.#text, this.#cancel);
    this.#tip = tooltip(this.#cancel, CANCEL_TEXT);
  }

  #sync() {
    const ring = this.getAttribute("kind") === "ring";
    this.#bar.hidden = ring;
    this.#ring.hidden = !ring;
    const value = Number.parseFloat(this.getAttribute("value"));
    for (const p of [this.#bar, this.#ring]) {
      if (this.hasAttribute("indeterminate") || !Number.isFinite(value)) p.removeAttribute("value");
      else p.value = Math.min(100, Math.max(0, value));
    }
    this.#text.textContent = this.getAttribute("text") ?? "";
    const label = this.getAttribute("cancel-label") ?? CANCEL_TEXT;
    this.#cancel.setAttribute("aria-label", label);
    if (tooltipOf(this.#cancel)) this.#tip.textContent = label;
    this.#cancel.hidden = !this.hasAttribute("cancelable");
  }

  /** The progress, from 0 to 100. */
  get value() { return Number.parseFloat(this.getAttribute("value")) || 0; }
  set value(v) { this.setAttribute("value", String(v)); }

  get indeterminate() { return this.hasAttribute("indeterminate"); }
  set indeterminate(on) { this.toggleAttribute("indeterminate", !!on); }

  /** The text after the progress. */
  get text() { return this.getAttribute("text") ?? ""; }
  set text(v) { this.setAttribute("text", v ?? ""); }

  /** "bar" (the default) or "ring". */
  get kind() { return this.getAttribute("kind") === "ring" ? "ring" : "bar"; }
  set kind(v) { this.setAttribute("kind", v); }

  get cancelable() { return this.hasAttribute("cancelable"); }
  set cancelable(on) { this.toggleAttribute("cancelable", !!on); }
}

if (!customElements.get("ui-status-progress")) customElements.define("ui-status-progress", UiStatusProgress);
if (!customElements.get("ui-status-bar")) customElements.define("ui-status-bar", UiStatusBar);
