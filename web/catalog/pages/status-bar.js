// Status bar: a live bar on the window background in a card, with a stats row above the main row
// (a check box shows or hides it), every item kind, an export that runs a cancelable progress
// item, and a slider that narrows the bar so its items shorten and hide; and the two progress
// kinds. As the desktop catalog's StatusBarPage.
import { el, body, caption, page, section } from "./ui.js";
import { icon } from "../../blocks/icon.js";
import { createMenu } from "../../blocks/menu.js";
import { tooltip } from "../../blocks/tooltip.js";
import "../../modules/status-bar.js";

function item(text, { zone, priority, shortTexts } = {}) {
  const span = el("span", "", text);
  if (zone) span.dataset.zone = zone;
  if (priority) span.dataset.priority = String(priority);
  if (shortTexts) span.dataset.shortTexts = JSON.stringify(shortTexts);
  return span;
}

// A subtle item with a tooltip and an accessible name.
function subtle(button, name) {
  button.type = "button";
  button.classList.add("ui-button", "ui-subtle", "ui-squircle", "ui-focus-ring");
  button.setAttribute("aria-label", name);
  tooltip(button, name);
  return button;
}

// A drop-down item opening a menu of radio items with the current one checked; choosing one calls
// back with its text.
function dropDown(content, name, values, current, chosen, menus) {
  const button = subtle(el("button", "ui-drop-down-button", ...content), name);
  const menu = createMenu(values.map((value) => ({
    label: value, type: "radio", group: name, checked: value === current, onSelect: () => chosen(value),
  })), { label: name });
  button.setAttribute("aria-haspopup", "menu");
  button.setAttribute("aria-expanded", "false");
  button.setAttribute("aria-controls", menu.id);
  menus.push(menu);
  return button;
}

// The bar on the window background, in a card with the window's corner radius.
function card(bar) {
  const c = el("div", "ui-squircle", bar);
  c.style.cssText = "--ui-squircle-radius: var(--ui-corner-radius-overlay); box-sizing: border-box; border: 1px solid var(--ui-system-base-low-color);"
    + " background: var(--ui-backdrop-wash); overflow: clip; display: flex; align-self: stretch";
  return c;
}

function bar(message, ...items) {
  const b = document.createElement("ui-status-bar");
  b.setAttribute("message", message);
  b.append(...items);
  return b;
}

function live() {
  const last = caption("Last command: none");
  const said = (text) => { last.textContent = `Last command: ${text}`; };
  const menus = [];

  const stats = el("div", "ui-status-bar-row",
    item("Cursor 1280, 640 px", { priority: 2, shortTexts: ["1280, 640"] }),
    item("Selection 1200 × 800 px", { priority: 3, shortTexts: ["1200 × 800"] }),
    item("Memory 412 MB", { zone: "trailing", priority: 1, shortTexts: ["412 MB"] }),
    item("60 fps", { zone: "trailing" }));

  const size = subtle(el("button", "", "4000 × 3000 px · 300 ppi"), "Image size");
  size.dataset.priority = "2";
  size.dataset.shortTexts = JSON.stringify(["4000 × 3000 px"]);
  size.addEventListener("click", () => said("Image size"));

  const exportItem = document.createElement("ui-status-progress");
  Object.assign(exportItem.dataset, { zone: "center" });
  exportItem.setAttribute("text", "Exporting 0 %");
  exportItem.setAttribute("cancelable", "");
  exportItem.hidden = true;

  const modeText = el("span", "", "RGB/8 · sRGB");
  const mode = dropDown([icon("palette"), modeText], "Colour mode", ["RGB/8", "RGB/16", "CMYK/8", "Grayscale/8"], "RGB/8", (v) => {
    mode.dataset.shortTexts = JSON.stringify([v]);
    modeText.textContent = `${v} · sRGB`;
    said(v);
  }, menus);
  Object.assign(mode.dataset, { zone: "trailing", priority: "1", shortTexts: JSON.stringify(["RGB/8"]) });

  const zoom = dropDown(["100 %"], "Zoom", ["25 %", "50 %", "100 %", "200 %", "400 %"], "100 %", (v) => {
    zoom.firstChild.data = v;
    said(v);
  }, menus);
  zoom.dataset.zone = "trailing";

  const b = bar("Ready", stats, item("Layer: Background", { priority: 3 }), size, exportItem, mode, zoom);
  const c = card(b);

  const width = el("input", "ui-slider");
  Object.assign(width, { type: "range", min: 30, max: 100, value: 100 });
  width.setAttribute("aria-label", "Bar width");
  width.style.inlineSize = "240px";
  const resize = () => { b.style.inlineSize = `${Math.max(0, (c.clientWidth) * width.value / 100)}px`; b.style.flex = "none"; };
  width.addEventListener("input", resize);
  new ResizeObserver(resize).observe(c);

  let timer = 0;
  const stop = (message) => {
    clearInterval(timer);
    timer = 0;
    exportItem.hidden = true;
    b.message = message;
  };
  exportItem.addEventListener("cancel", () => stop("Export cancelled"));
  const run = el("button", "ui-button ui-squircle ui-focus-ring", "Run export");
  run.type = "button";
  run.addEventListener("click", () => {
    clearInterval(timer);
    let value = 0;
    exportItem.value = 0;
    exportItem.text = "Exporting 0 %";
    exportItem.hidden = false;
    b.message = "Exporting poster.png…";
    timer = setInterval(() => {
      if (!b.isConnected) return clearInterval(timer);
      value = Math.min(100, value + 2);
      exportItem.value = value;
      exportItem.text = `Exporting ${value} %`;
      if (value >= 100) stop("Export finished");
    }, 100);
  });

  const show = el("input", "ui-check-box ui-squircle ui-focus-ring");
  show.type = "checkbox";
  show.checked = true;
  show.addEventListener("change", () => { stats.hidden = !show.checked; });

  const controls = el("div", "", body("Width"), width, run);
  controls.style.cssText = "display: flex; align-items: center; gap: var(--ui-spacing-m)";
  return el("div", "catalog-stack", c, controls, el("label", "ui-toggle", show, "Show stats row"), last, ...menus);
}

function progress() {
  const ring = document.createElement("ui-status-progress");
  ring.dataset.zone = "center";
  for (const [k, v] of [["kind", "ring"], ["indeterminate", ""], ["text", "Syncing 3 files"]]) ring.setAttribute(k, v);
  const barItem = document.createElement("ui-status-progress");
  barItem.dataset.zone = "center";
  for (const [k, v] of [["value", "42"], ["text", "42 %"], ["cancelable", ""]]) barItem.setAttribute(k, v);
  return el("div", "catalog-stack", card(bar("Syncing…", ring)), card(bar("Exporting poster.png…", barItem)));
}

export default {
  id: "status-bar",
  title: "Status bar",
  icon: "check-circle",
  render: () => page("Status bar",
    section("Live", live()),
    section("Progress", progress()),
    section("Use", caption(
      "The bottom of the window, on the window background: the main row, 24 high at every density, and any rows above it "
      + "(the bar's rows, each 24 high, shown or hidden by the app). The message leads the main row and is announced "
      + "politely; leading, centre and trailing zones in every row. Text in Caption size and secondary text; clickable items are "
      + "subtle buttons and drop-downs with a 20 px hover fill. When a row is too narrow, its items take their shorter texts, then "
      + "hide, highest priority first, each row on its own. Clickable items are the window's last Tab stops, the rows from the "
      + "top and then the main row; text takes no focus."))),
};
