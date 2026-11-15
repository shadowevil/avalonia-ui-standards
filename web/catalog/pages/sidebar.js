// Sidebar (0034): the Module's stages side by side (expanded, the icon rail, the overlay on glass),
// the row states, and a live sidebar with sections, a footer and the accent flag. The catalog's own
// sidebar is the Module in the window's slot, so resize the window to see the real stages.
import { icon } from "../../ui.js";
import "../../modules/sidebar.js";
import { el, body, caption, captioned, page, section } from "./ui.js";

const PAGES = [
  ["Colours", "palette"], ["Type", "text-aa"], ["Spacing", "ruler"], ["Shape", "squares-four"], ["Glass", "drop"],
  ["Motion", "play"], ["Button", "cursor-click"], ["ListBox", "list"],
];

function row(name, iconName, attributes = {}) {
  const a = el("a", "ui-list-item ui-squircle ui-focus-ring", icon(iconName), el("span", "ui-sidebar-label", name));
  a.href = "#sidebar";
  for (const [k, v] of Object.entries(attributes)) a.setAttribute(k, v);
  const li = el("li", "", a);
  return li;
}

/** A sidebar: sections of rows (the second row is current), an optional footer, and stage attributes for a fixed sample. */
function sidebar({ label, sections = [{ rows: PAGES }], footer = false, attributes = {}, rowStates = {}, current = 1 }) {
  const list = el("ul");
  list.setAttribute("role", "list");
  let k = 0;
  for (const { header, rows } of sections) {
    if (header) list.append(el("li", "ui-sidebar-header", header));
    for (const [name, i] of rows) {
      list.append(row(name, i, { ...(k === current ? { "aria-current": "page" } : {}), ...(rowStates[k] ?? {}) }));
      k++;
    }
  }
  const sb = el("ui-sidebar", "", el("div", "ui-sidebar-list ui-scroll", list));
  if (footer) {
    const ul = el("ul", "ui-sidebar-footer", row("Settings", "gear"));
    ul.setAttribute("role", "list");
    sb.append(ul);
  }
  sb.setAttribute("aria-label", label);
  for (const [k2, v] of Object.entries(attributes)) sb.setAttribute(k2, v);
  return sb;
}

// The sample box stands in for the shell's slot: the sidebar fills it.
const box = (width, height, sb) => {
  const b = el("div", "", sb);
  Object.assign(b.style, { position: "relative", inlineSize: `${width}px`, blockSize: `${height}px` });
  return b;
};

const row2 = (...children) => {
  const r = el("div", "", ...children);
  Object.assign(r.style, { display: "flex", flexWrap: "wrap", alignItems: "flex-start", gap: "var(--ui-spacing-l) var(--ui-spacing-xl)" });
  return r;
};

function stages() {
  const sections = [{ header: "Foundations", rows: PAGES.slice(0, 5) }, { header: "Controls", rows: PAGES.slice(5) }];
  const options = { sections, footer: true };
  // The overlay over a strip of the panel: glass on glass-free ground, as the window draws it.
  const panel = el("div", "ui-glass ui-squircle");
  Object.assign(panel.style, { position: "absolute", inset: "0 0 0 120px", padding: "var(--ui-spacing-m)", paddingInlineStart: "calc(120px + var(--ui-spacing-m))" });
  panel.append(el("span", "catalog-body", "Content panel"));
  const overlay = box(360, 400, sidebar({ ...options, label: "Overlay", attributes: { "data-overlay": "" } }));
  overlay.prepend(panel);
  overlay.firstElementChild.style.zIndex = "0";
  overlay.lastElementChild.style.zIndex = "1";
  overlay.lastElementChild.style.inlineSize = "240px";
  return row2(
    captioned("Expanded: 240, icon and label", box(240, 400, sidebar({ ...options, label: "Expanded" }))),
    captioned("Compact: the icon rail, 48; labels are tooltips", box(48, 400, sidebar({ ...options, label: "Rail", attributes: { "data-collapsed": "" } }))),
    captioned("Overlay: expanded, on glass over the panel", overlay));
}

function states() {
  const attributes = [
    ["Rest", {}], ["Hover", { "data-state": "hover" }], ["Pressed", { "data-state": "pressed" }],
    ["Selected", { "aria-current": "page" }], ["Selected + focus", { "aria-current": "page", "data-state": "focus" }],
    ["Disabled", { "aria-disabled": "true" }],
  ];
  return row2(...attributes.map(([name, a]) => {
    const selected = "aria-current" in a;
    const sb = sidebar({ label: name, sections: [{ rows: [PAGES[6], PAGES[7]] }], rowStates: { 0: a }, current: selected ? 0 : -1 });
    const sample = captioned(name, box(200, 80, sb));
    sample.inert = true;
    return sample;
  }));
}

function switchContent() {
  const content = el("span", "ui-toggle-switch-content", el("span", "ui-toggle-switch-off", "Off"), el("span", "ui-toggle-switch-on", "On"));
  content.setAttribute("aria-hidden", "true");
  return content;
}

function live() {
  const sections = [{ header: "Foundations", rows: PAGES.slice(0, 5) }, { header: "Controls", rows: PAGES.slice(5) }];
  const sb = sidebar({ label: "Live sidebar", sections, footer: true });
  const flag = el("input", "ui-toggle-switch ui-focus-ring");
  flag.type = "checkbox";
  flag.setAttribute("role", "switch");
  flag.setAttribute("aria-label", "Accent flag");
  flag.addEventListener("change", () => sb.toggleAttribute("accent-flag", flag.checked));
  const switchLabel = el("label", "ui-toggle-switch-label", flag, switchContent());
  const rail = el("input", "ui-toggle-switch ui-focus-ring");
  rail.type = "checkbox";
  rail.setAttribute("role", "switch");
  rail.setAttribute("aria-label", "Icon rail");
  const railBox = box(240, 400, sb);
  rail.addEventListener("change", () => {
    sb.toggleAttribute("data-collapsed", rail.checked);
    railBox.style.inlineSize = rail.checked ? "48px" : "240px";
  });
  const railLabel = el("label", "ui-toggle-switch-label", rail, switchContent());
  return el("div", "catalog-stack",
    el("div", "catalog-list-options", body("Accent flag"), switchLabel, body("Icon rail"), railLabel),
    railBox,
    caption("Up and Down move between rows, over the section headers and on into the footer; Home and End go to the first and last. " +
      "Enter, Space or a click chooses a row. The sidebar is one Tab stop, on the current row."));
}

export default {
  id: "sidebar",
  title: "Sidebar",
  icon: "sidebar-simple",
  render: () => page("Sidebar",
    section("Stages", stages()),
    section("States", states()),
    section("Live", live()),
    caption("The window's sidebar button (before the title) collapses the expanded sidebar to the rail and expands it again, and in the " +
      "compact and hidden stages opens it as an overlay. Resize the window to see the stages: expanded from 1008, the rail from 640, hidden below.")),
};
