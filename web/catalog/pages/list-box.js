// ListBox: the item states forced, and live single and multiple selection lists that scroll.
import { icon } from "../../ui.js";
import { el, body, caption, captioned, page, section } from "./ui.js";

const STATES = [
  ["Rest", {}],
  ["Hover", { "data-state": "hover" }],
  ["Pressed", { "data-state": "pressed" }],
  ["Selected", { "aria-selected": "true" }],
  ["Selected + hover", { "aria-selected": "true", "data-state": "hover" }],
  ["Selected + focus", { "aria-selected": "true", "data-state": "focus" }],
  ["Disabled", { "aria-disabled": "true" }],
];

const PLACES = [
  ["Home", "house"], ["Calendar", "calendar"], ["Recent", "clock"], ["Notifications", "bell"],
  ["Messages", "chat-text"], ["Tables", "table"], ["Palette", "palette"], ["Appearance", "eye"],
  ["Layout", "squares-four"], ["Tabs", "tabs"], ["Search", "magnifying-glass"], ["Settings", "gear"],
];

const NAMES = ["Aurora", "Basalt", "Cedar", "Dune", "Ember", "Fjord", "Glacier", "Harbor", "Indigo", "Juniper", "Kelp", "Lagoon", "Meadow", "Nimbus", "Orchid", "Pebble"];

const item = (selected, ...content) => {
  const i = el("div", "ui-list-item ui-squircle ui-focus-ring", ...content);
  if (selected) i.setAttribute("aria-selected", "true");
  return i;
};

function list(label, items, { multiple = false, live = false } = {}) {
  const l = el("ui-list-box", live ? "ui-squircle ui-scroll" : "ui-squircle", ...items);
  l.setAttribute("aria-label", label);
  if (multiple) l.setAttribute("multiple", "");
  return l;
}

function states() {
  return el("div", "catalog-list-states", ...STATES.map(([label, attributes]) => {
    const first = item(false, icon("house"), el("span", "", "Documents"));
    for (const [k, v] of Object.entries(attributes)) first.setAttribute(k, v);
    const state = captioned(label, list(label, [first, item(false, "Downloads")]));
    state.inert = true;
    return state;
  }));
}

function live() {
  const lists = [
    list("Single, text and icon", PLACES.map(([n, k], i) => item(i === 1, icon(k), el("span", "", n))), { live: true }),
    list("Single, text", NAMES.map((n, i) => item(i === 2, n)), { live: true }),
    list("Multiple", NAMES.map((n, i) => item(i === 1 || i === 3, n)), { multiple: true, live: true }),
  ];
  // The accent flag option, on all three lists (a checkbox until the ToggleSwitch Block exists).
  const flag = el("input");
  flag.type = "checkbox";
  flag.addEventListener("change", () => lists.forEach((l) => l.toggleAttribute("accent-flag", flag.checked)));
  return el("div", "catalog-stack",
    el("label", "catalog-list-options", body("Accent flag"), flag),
    el("div", "catalog-list-live", ...lists.map((l) => captioned(l.getAttribute("aria-label"), l))),
    caption("Up and Down move between items; Space or Enter selects. In the multiple list a click toggles an item, Shift extends the range. " +
      "The lists scroll: the scrollbar appears over the content, and a soft shadow marks each edge with items out of view."));
}

export default {
  id: "listbox",
  title: "ListBox",
  icon: "list",
  render: () => page("ListBox", section("States", states()), section("Live", live())),
};
