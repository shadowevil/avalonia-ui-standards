// Icon: the whole set with names, the sizes, and an icon in a button.
import { icon, iconNames } from "../../ui.js";
import { el, body, caption, page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

// Names shown as on desktop: the Icons property name.
const pascal = (name) => name.replace(/(^|-)([a-z0-9])/g, (_, __, c) => c.toUpperCase());

function sizes() {
  return el("div", "catalog-icon-sizes", ...[16, 20, 24].map((size) =>
    el("div", "catalog-icon-size",
      el("div", "catalog-icon-row", ...["gear", "caret-down", "house"].map((n) => icon(n, { size }))),
      caption(`${size} px`))));
}

function accentButton() {
  const b = el("button", "ui-button ui-accent ui-squircle ui-focus-ring", icon("plus"), "Add");
  b.type = "button";
  return b;
}

export default {
  id: "icon",
  title: "Icon",
  icon: "bell",
  render: () => page("Icon",
    section("Set (Phosphor Regular)", el("ul", "catalog-icon-set", ...iconNames.map((n) => el("li", "", icon(n), body(pascal(n)))))),
    section("Sizes", el("div", "catalog-stack", sizes(),
      caption("20 px at Normal density and 16 px at Compact, as in the set above; 24 px for larger use."))),
    section("In a button", stateGrid([["Accent, with icon", accentButton]]))),
};
