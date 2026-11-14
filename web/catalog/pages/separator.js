// Separator and GridSplitter: the hairline, and the splitter's grip in every state next to live
// splitters.
import { el, body, caption, page, section, sideScroll, spaced } from "./ui.js";

const STATES = [
  ["Rest", null],
  ["Hover", "hover"],
  ["Dragging", "pressed"],
  ["Focused", "focus"],
  ["Disabled", null],
];

function pane(text) {
  const p = el("div", "ui-squircle", body(text));
  p.style.cssText = "background: var(--ui-system-chrome-medium-low-color); padding: var(--ui-spacing-s); min-inline-size: 0; min-block-size: 0";
  return p;
}

// Two panes split by a splitter: side by side (columns) or stacked (rows).
function split(columns, width, height) {
  const splitter = el("div", "ui-grid-splitter");
  splitter.setAttribute("aria-label", columns ? "Resize columns" : "Resize rows");
  splitter.setAttribute("aria-orientation", columns ? "vertical" : "horizontal");
  const grid = el("div", "", pane("One"), splitter, pane("Two"));
  grid.style.cssText = `display: grid; inline-size: ${width}px; block-size: ${height}px; grid-template-${columns ? "columns" : "rows"}: 1fr auto 1fr`;
  return grid;
}

// The splitter states as the desktop's state table: no live column, each sample padded 4, at the
// start of its cell, and inert.
function states() {
  const grid = el("div", "catalog-state-grid");
  grid.style.setProperty("--catalog-state-columns", STATES.length);
  grid.append(el("div"));
  for (const [label] of STATES) grid.append(el("div", "catalog-state-caption", caption(label)));
  for (const [label, columns, width, height] of [["Between columns", true, 140, 90], ["Between rows", false, 110, 110]]) {
    grid.append(el("div", "catalog-state-label", body(label)));
    for (const [state, forced] of STATES) {
      const sample = split(columns, width, height);
      const splitter = sample.querySelector(".ui-grid-splitter");
      if (forced) splitter.dataset.state = forced;
      if (state === "Disabled") splitter.setAttribute("aria-disabled", "true");
      const cell = el("div", "catalog-state-cell", sample);
      cell.style.padding = "var(--ui-spacing-xs)";
      cell.style.justifySelf = "start";
      cell.inert = true;
      grid.append(cell);
    }
  }
  return sideScroll(grid);
}

function separatorSample() {
  const column = el("div", "", body("Above"), el("hr", "ui-separator"), body("Below"));
  column.style.cssText = "display: flex; flex-direction: column; inline-size: 200px";
  const stack = el("div", "", column,
    caption("1 px in the border colour, 4 px above and below. In a menu it is inset 8 px further from the sides."));
  stack.style.cssText = "display: flex; flex-direction: column; gap: var(--ui-spacing-m)";
  return stack;
}

function live() {
  const wrap = el("div", "", spaced(split(true, 200, 140)), spaced(split(false, 160, 180)));
  wrap.style.cssText = "display: flex; flex-wrap: wrap";
  return wrap;
}

export default {
  id: "separator-and-gridsplitter",
  title: "Separator and GridSplitter",
  icon: "arrows-out-line-horizontal",
  render: () => page("Separator and GridSplitter",
    section("Separator", separatorSample()),
    section("GridSplitter states", states()),
    section("Live", live()),
    caption("Invisible at rest; a pill grip shows on hover, darker while dragging. Tab to a splitter and use the arrow keys to resize.")),
};
