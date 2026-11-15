// TreeView: the row states forced, and a live tree with its options (click modes, lines, accent flag).
import { el, body, caption, captioned, page, section } from "./ui.js";

const STATES = [
  ["Rest", () => {}],
  ["Hover", (item) => { row(item).dataset.state = "hover"; }],
  ["Pressed", (item) => { row(item).dataset.state = "pressed"; }],
  ["Selected", (item) => item.setAttribute("aria-selected", "true")],
  ["Selected + hover", (item) => { item.setAttribute("aria-selected", "true"); row(item).dataset.state = "hover"; }],
  ["Disabled", (item) => item.setAttribute("aria-disabled", "true")],
];

const CLICKS = [["Single", "single"], ["Double", "double"], ["Both", "single double"]];

const SAMPLE = [
  ["Style", true, ["Colour", "Type", ["Shape", false, ["Squircle", "Radii"]], "Motion"]],
  ["Blocks", true, [["Button family", true, ["Button", "ToggleButton", "SplitButton"]], "ListBox", "TreeView"]],
  ["Modules", false, ["StandardsWindow", "Sidebar"]],
];

const row = (item) => item.querySelector(":scope > .ui-tree-view-row");

/** An item: a label, or [label, expanded, children]. */
function item(node) {
  const [label, expanded, children] = typeof node === "string" ? [node, false, null] : node;
  const li = el("li", "ui-tree-view-item",
    el("div", "ui-tree-view-row", el("span", "ui-tree-view-content ui-squircle", label)));
  if (children) {
    li.append(el("ul", "ui-tree-view-group", ...children.map(item)));
    li.setAttribute("aria-expanded", String(expanded));
  }
  return li;
}

function tree(label, nodes, width) {
  const t = el("ui-tree-view", "ui-squircle", el("ul", "", ...nodes.map(item)));
  t.setAttribute("aria-label", label);
  t.style.inlineSize = `${width}px`;
  return t;
}

function states() {
  const wrap = el("div", "");
  wrap.style.cssText = "display: flex; flex-wrap: wrap;";
  for (const [label, apply] of STATES) {
    const t = tree(label, [["Documents", false, ["Notes"]]], 160);
    apply(t.querySelector(".ui-tree-view-item"));
    const sample = captioned(label, t);
    sample.inert = true;
    sample.style.margin = "0 var(--ui-spacing-m) var(--ui-spacing-m) 0";
    wrap.append(sample);
  }
  return wrap;
}

// Single, Double or Both for one click option, set on the tree.
function clickChoice(label, t, attribute) {
  const c = el("ui-segmented-control", "ui-squircle ui-focus-ring-within",
    ...CLICKS.map(([text, value]) => {
      const s = el("span", "ui-segmented-control-item ui-squircle", text);
      s.dataset.value = value;
      if (value === "double") s.setAttribute("aria-checked", "true");
      return s;
    }));
  c.setAttribute("aria-label", label);
  c.addEventListener("change", () => {
    const chosen = c.querySelector("[aria-checked='true']");
    if (chosen) t.setAttribute(attribute, chosen.dataset.value);
  });
  return c;
}

function toggleSwitch(label, on, onChange) {
  const input = el("input", "ui-toggle-switch ui-focus-ring");
  input.type = "checkbox";
  input.setAttribute("role", "switch");
  input.setAttribute("aria-label", label);
  input.checked = on;
  input.addEventListener("change", () => onChange(input.checked));
  return el("label", "ui-toggle-switch-label", input,
    el("span", "ui-toggle-switch-content", el("span", "ui-toggle-switch-off", "Off"), el("span", "ui-toggle-switch-on", "On")));
}

function live() {
  const t = tree("Library", SAMPLE, 280);
  const options = el("div", "");
  options.style.cssText = "display: grid; grid-template-columns: auto auto; justify-content: start; align-items: center; column-gap: var(--ui-spacing-m); row-gap: var(--ui-spacing-s);";
  for (const [label, control] of [
    ["Expand on", clickChoice("Expand on", t, "expand-on")],
    ["Collapse on", clickChoice("Collapse on", t, "collapse-on")],
    ["Lines", toggleSwitch("Lines", true, (on) => (on ? t.removeAttribute("show-lines") : t.setAttribute("show-lines", "false")))],
    ["Accent flag", toggleSwitch("Accent flag", false, (on) => t.toggleAttribute("accent-flag", on))],
  ]) {
    const cell = el("div", "", control);
    cell.style.justifySelf = "start";
    options.append(body(label), cell);
  }
  return el("div", "catalog-stack", options, t,
    caption("Up and Down move between rows; Right expands, then moves to the first child; Left collapses, then moves to the parent; Enter toggles. " +
      "The caret toggles with one click whatever the click options; a double click toggles a row once even when single clicks toggle it too. The caret turns over MotionNormal."));
}

export default {
  id: "treeview",
  title: "TreeView",
  icon: "tree-structure",
  render: () => page("TreeView", section("States", states()), section("Live", live())),
};
