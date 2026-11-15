// DataGrid: a live grid with text, number, check and combo columns, sorting, a frozen column and
// editing, and its states (read-only, disabled, all grid lines), as the desktop page.
import { el, body, caption, page, section, spaced } from "./ui.js";

const CATEGORIES = ["Style", "Blocks", "Modules", "Screens"];

const SAMPLE = [
  ["Accent", 12, true, "Style", "The brand colour, AA fills derived"],
  ["Swatch", 3, false, "Blocks", "A colour chip"],
  ["Button", 48, true, "Blocks", "Five variants, two densities"],
  ["Sidebar", 7, true, "Modules", "Collapses to an icon rail"],
  ["Radius", 2, false, "Style", "8 for controls, 16 for overlays"],
  ["App shell", 1, false, "Screens", "Frame, title band and panel"],
  ["TextInput", 21, true, "Blocks", "Clear and reveal buttons"],
  ["Glass", 5, true, "Style", "Blur, tint and refraction"],
  ["ComboBox", 9, true, "Blocks", "Editable or not"],
  ["Settings page", 0, false, "Screens", "Planned"],
  ["Spacing", 7, true, "Style", "A 4 px grid"],
  ["TreeView", 4, true, "Blocks", "Node lines and click modes"],
];

function header(text, attributes) {
  const th = el("th", "", text);
  for (const [k, v] of Object.entries(attributes)) th.setAttribute(k, v);
  return th;
}

/** A grid of `rows` sample parts; the full set of columns unless `compact` (Name, Count, In stock). */
function grid(label, rows, height, { compact = false, attributes = {} } = {}) {
  const heads = [
    header("Name", { "data-width": "120" }),
    header("Count", { "data-width": "90", "data-type": "number" }),
    header("In stock", { "data-width": "100", "data-type": "check" }),
  ];
  if (!compact) {
    heads.push(
      header("Category", { "data-width": "130", "data-type": "combo", "data-options": CATEGORIES.join("|") }),
      header("Notes", { "data-width": "*", "data-min-width": "160" }));
  }
  const tbody = el("tbody", "", ...rows.map(([name, count, inStock, category, notes]) => {
    const box = el("input", "ui-check-box ui-squircle");
    box.type = "checkbox";
    box.checked = inStock;
    const cells = [el("td", "", name), el("td", "", String(count)), el("td", "", box)];
    if (!compact) cells.push(el("td", "", category), el("td", "", notes));
    return el("tr", "", ...cells);
  }));
  const table = el("table", "", el("thead", "", el("tr", "", ...heads)), tbody);
  table.setAttribute("aria-label", label);
  const g = el("ui-data-grid", "ui-squircle", table);
  for (const [k, v] of Object.entries(attributes)) g.setAttribute(k, v);
  g.style.blockSize = `${height}px`;
  return g;
}

function state(label, configure) {
  const g = grid(label, SAMPLE.slice(0, 4), 176, { compact: true });
  configure(g);
  const sample = el("div", "", body(label), g);
  sample.style.cssText = "display: flex; flex-direction: column; gap: var(--ui-spacing-xs); inline-size: 320px;";
  return spaced(sample);
}

function states() {
  const wrap = el("div", "",
    state("Read-only", (g) => g.setAttribute("readonly", "")),
    state("Disabled", (g) => {
      g.setAttribute("disabled", "");
      g.querySelectorAll("tbody > tr")[1].setAttribute("aria-selected", "true");
    }),
    state("All grid lines", (g) => g.setAttribute("grid-lines", "all")));
  wrap.style.cssText = "display: flex; flex-wrap: wrap;";
  return wrap;
}

function live() {
  const g = grid("Parts", SAMPLE, 300, { attributes: { "frozen-columns": "1", "can-resize-columns": "" } });
  g.style.alignSelf = "stretch";
  return el("div", "catalog-stack", g,
    caption("Click a header to sort; drag a header edge to resize (the grip shows near a resizable edge). Name stays frozen while the grid scrolls sideways. " +
      "Double click or press F2 to edit a cell: text becomes a field, Category a combo box, In stock toggles. Tab in and use the arrow keys to see the focus ring on the current cell."));
}

export default {
  id: "datagrid",
  title: "DataGrid",
  icon: "table",
  render: () => page("DataGrid",
    section("Live", live()),
    section("States", states()),
    caption("Rows are the list item height (32, or 24 at Compact) with the list's hover and accent selection; horizontal hairlines in the border colour. " +
      "Vertical lines are an option.")),
};
