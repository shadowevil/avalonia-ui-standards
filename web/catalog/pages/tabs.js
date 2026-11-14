// Tabs: TabControl and TabStrip headers in every state (hover, press and focus on "Activity"),
// with icons, and live tabs.
import { icon } from "../../ui.js";
import { el, caption, page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

const HEADERS = [["General", "gear"], ["Activity", "bell"], ["Sharing", "chat-text"]];

function tab(header, iconName, selected) {
  const t = el("button", "ui-tab ui-squircle ui-focus-ring");
  t.type = "button";
  if (iconName) t.append(icon(iconName));
  t.append(header);
  t.setAttribute("aria-selected", String(selected));
  return t;
}

function strip(label, icons) {
  const s = el("ui-tab-strip", "", ...HEADERS.map(([header, name], i) => tab(header, icons ? name : null, i === 0)));
  s.setAttribute("aria-label", label);
  return s;
}

// As the desktop's wrap panel in a sideways scroller: one row, as wide as the headers need.
function oneRow(node) {
  node.style.inlineSize = "max-content";
  return node;
}

// At least 300 wide, and wider when the headers need it, so they stay on one row.
function tabs(icons) {
  return (label) => {
    const control = el("ui-tabs", "", strip(label, icons),
      ...HEADERS.map(([header]) => el("div", "ui-tab-panel", `${header} settings.`)));
    control.style.minInlineSize = "300px";
    return oneRow(control);
  };
}

// The states are forced on "Activity"; disabled disables every tab.
function force(sample, state) {
  const all = [...sample.querySelectorAll(".ui-tab")];
  const activity = all[1];
  if (state === "Hover") activity.dataset.state = "hover";
  if (state === "Pressed") activity.dataset.state = "pressed";
  if (state === "Focused") activity.dataset.state = "focus";
  if (state === "Disabled") for (const t of all) t.disabled = true;
}

export default {
  id: "tabs",
  title: "Tabs",
  icon: "tabs",
  render: () => page("Tabs",
    section("TabControl", stateGrid([
      ["Text", tabs(false)],
      ["With icons", tabs(true)],
    ], { force })),
    section("TabStrip", stateGrid([["TabStrip", (label) => oneRow(strip(label, false))]], { force })),
    section("Use", caption(
      "Tabs switch between content areas; a segmented control chooses a value for one setting. The arrow keys, Home and End move between tabs."))),
};
