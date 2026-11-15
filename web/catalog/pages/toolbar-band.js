// Toolbar band: a live band on the window background in a card, narrowed with a slider so its
// items fold into the "…" menu (the desktop catalog's ToolbarBandPage).
import { icon, tooltip, createMenu } from "../../ui.js";
import "../../modules/toolbar-band.js";
import "../../blocks/color-picker.js";
import { el, body, caption, page, section } from "./ui.js";

function subtle(tag, name, iconName) {
  const node = el(tag, "ui-button ui-subtle ui-squircle ui-focus-ring", icon(iconName));
  node.type = "button";
  node.setAttribute("aria-label", name);
  return node;
}

function segmented(label, segments) {
  const control = el("ui-segmented-control", "ui-squircle ui-focus-ring-within", ...segments);
  control.setAttribute("aria-label", label);
  segments[0].setAttribute("aria-checked", "true");
  return control;
}

const segment = (...content) => el("span", "ui-segmented-control-item ui-squircle", ...content);

function demo(last) {
  const band = document.createElement("ui-toolbar-band");
  const command = (name, iconName) => {
    const button = subtle("button", name, iconName);
    button.addEventListener("click", () => {
      last.textContent = `Last command: ${name}`;
    });
    return button;
  };
  const sidebar = command("Show sidebar", "sidebar-simple");
  sidebar.dataset.priority = "-1";
  const add = command("New", "plus");
  add.dataset.beginsGroup = "";
  const search = command("Search", "magnifying-glass");

  const range = segmented("Range", ["Day", "Week", "Month"].map((t) => segment(t)));
  Object.assign(range.dataset, { zone: "center", priority: "1" });
  const layout = segmented("Layout", [["List", "list-bullets"], ["Grid", "squares-four"]].map(([name, key]) => {
    const s = segment(icon(key));
    s.setAttribute("aria-label", name);
    s.title = name;
    return s;
  }));
  Object.assign(layout.dataset, { zone: "center", priority: "2", beginsGroup: "" });

  const sort = (name) => ({ label: name, onSelect: () => { last.textContent = `Last command: ${name}`; } });
  const viewMenu = createMenu([sort("Sort by name"), sort("Sort by date")], { label: "View" });
  const view = subtle("button", "View", "sliders-horizontal");
  view.classList.add("ui-drop-down-button");
  view.setAttribute("aria-haspopup", "menu");
  view.setAttribute("aria-expanded", "false");
  view.setAttribute("aria-controls", viewMenu.id);
  view.dataset.zone = "trailing";

  const preview = subtle("button", "Preview", "eye");
  preview.setAttribute("aria-pressed", "false");
  preview.dataset.zone = "trailing";

  const separator = el("hr", "ui-separator");
  separator.dataset.zone = "trailing";

  const colour = document.createElement("ui-color-picker");
  colour.setAttribute("value", "#E0720B");
  colour.setAttribute("aria-label", "Highlight colour");
  Object.assign(colour.dataset, { zone: "trailing", canOverflow: "false" });

  band.append(sidebar, add, search, range, layout, view, viewMenu, preview, separator, colour);
  for (const [node, name] of [[sidebar, "Show sidebar"], [add, "New"], [search, "Search"], [view, "View"], [preview, "Preview"]]) tooltip(node, name);
  tooltip(colour, "Highlight colour");
  return band;
}

function live() {
  const last = caption("Last command: none");
  const band = demo(last);
  // The card: the window background (the backdrop wash), a hairline edge, the overlay radius.
  const card = el("div", "ui-squircle", band);
  card.style.cssText = "--ui-squircle-radius: var(--ui-corner-radius-overlay); background: var(--ui-backdrop-wash);"
    + " border: 1px solid var(--ui-system-base-low-color); overflow: clip";
  band.style.inlineSize = "100%";

  const width = el("input", "ui-slider");
  width.type = "range";
  width.min = "30";
  width.max = "100";
  width.value = "100";
  width.setAttribute("aria-label", "Band width");
  width.style.inlineSize = "240px";
  width.addEventListener("input", () => {
    band.style.inlineSize = `${width.value}%`;
  });
  const row = el("div", "", body("Width"), width);
  row.style.cssText = "display: flex; align-items: center; gap: var(--ui-spacing-m)";

  const stack = el("div", "", card, row, last);
  stack.style.cssText = "display: flex; flex-direction: column; gap: var(--ui-spacing-m)";
  return stack;
}

export default {
  id: "toolbar-band",
  title: "Toolbar band",
  icon: "app-window",
  render: () => page("Toolbar band",
    section("Live", live()),
    section("Use", caption(
      "Directly below the title band, on the window background, 48 high (40 Compact). Leading, centre and trailing zones; "
      + "icon items with tooltips. Items that do not fit move into the trailing \"…\" menu, highest priority first, then from the "
      + "trailing end; a segmented control becomes a group of radio items. Each item is one Tab stop; \"…\" opens with Enter, "
      + "Space or Down."))),
};
