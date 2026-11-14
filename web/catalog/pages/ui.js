// Small builders shared by the catalog pages, as the desktop catalog's Ui class. Values shown on
// a page are read from the live theme (computed styles), never copied.

/** An element with a class and children (nodes or strings). */
export function el(tag, className = "", ...children) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  node.append(...children);
  return node;
}

/** Body text. */
export const body = (text) => el("span", "catalog-body", text);

/** Secondary text in the caption size. */
export const caption = (text = "") => el("span", "catalog-caption", text);

/** A page: its title (the page's h1) and its sections, stacked 24 apart. */
export function page(title, ...sections) {
  return el("div", "catalog-page", el("h1", "catalog-title", title), ...sections);
}

/** A section: its heading (h2) above its content, 12 apart. */
export function section(heading, ...content) {
  return el("section", "catalog-section", el("h2", "catalog-subtitle", heading), ...content);
}

/** A caption above a control, 4 apart. */
export function captioned(text, control) {
  return el("div", "catalog-captioned", caption(text), control);
}

/** Lets content scroll sideways when it cannot fit; the page never does. */
export const sideScroll = (content) => el("div", "catalog-side-scroll", content);

// The theme watchers: one observer and one set of media listeners for every page, holding a set
// of {node, fn, shown}; a watcher whose node was shown and has left the document is dropped on the
// next run or the next watchTheme call.
const watchers = new Set();
let watchTimer = 0;

function pruneWatchers() {
  for (const watcher of watchers) if (watcher.shown && !watcher.node.isConnected) watchers.delete(watcher);
}

function runWatchers() {
  watchTimer = 0;
  pruneWatchers();
  for (const { fn } of watchers) fn();
}

function startWatching() {
  const schedule = () => {
    watchTimer ||= setTimeout(runWatchers);
  };
  new MutationObserver(schedule).observe(document.documentElement, { attributes: true });
  for (const q of ["(prefers-color-scheme: dark)", "(prefers-reduced-motion: reduce)", "(prefers-reduced-transparency: reduce)"]) {
    matchMedia(q).addEventListener("change", schedule);
  }
}
let watching = false;

/**
 * Calls `fn` now and whenever the theme changes (a setting attribute or the accent on <html>, or
 * the browser's colour scheme, reduced motion or reduced transparency), while `node` stays in the
 * document.
 */
export function watchTheme(node, fn) {
  if (!watching) {
    watching = true;
    startWatching();
  }
  pruneWatchers();
  const watcher = { node, fn, shown: false };
  watchers.add(watcher);
  // First call once the node is in the page, where its tokens resolve.
  queueMicrotask(() => {
    if (!node.isConnected) return watchers.delete(watcher);
    watcher.shown = true;
    fn();
  });
}

/** Test hook: the number of live theme watchers. */
export function watcherCount() {
  pruneWatchers();
  return watchers.size;
}

/** A computed colour ("rgb(…)" or "rgba(…)") as #RRGGBB, or #AARRGGBB when not opaque, as desktop. */
export function hex(colour) {
  const m = colour.match(/rgba?\(\s*([\d.]+)[,\s]+([\d.]+)[,\s]+([\d.]+)(?:\s*[,/]\s*([\d.]+%?))?\s*\)/);
  if (!m) return colour;
  const byte = (v) => Math.round(Number(v)).toString(16).padStart(2, "0").toUpperCase();
  const alpha = m[4] === undefined ? 1 : m[4].endsWith("%") ? parseFloat(m[4]) / 100 : Number(m[4]);
  const rgb = byte(m[1]) + byte(m[2]) + byte(m[3]);
  return alpha >= 1 ? `#${rgb}` : `#${byte(alpha * 255)}${rgb}`;
}

/** The kebab-case custom property for a desktop key: SpacingM is --ui-spacing-m. */
export const cssName = (key) => "--ui-" + key.replace(/([a-z0-9])([A-Z])/g, "$1-$2").replace(/([A-Z])([A-Z][a-z])/g, "$1-$2").toLowerCase();

/** The computed value of a custom property on `node`. */
export const token = (node, key) => getComputedStyle(node).getPropertyValue(cssName(key)).trim();
