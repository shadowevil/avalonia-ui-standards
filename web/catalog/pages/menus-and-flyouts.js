// Menus and flyouts: menu items and menu bar items in every state, and a live menu bar, context
// menu, menu flyout and flyout.
import { createMenu, createMenuBar } from "../../blocks/menu.js";
import "../../blocks/flyout.js";
import { el, body, caption, page, section, sideScroll, spaced } from "./ui.js";

const STATES = [
  ["Rest", {}],
  ["Highlight", { "data-state": "hover" }],
  ["Pressed", { "data-state": "hover pressed" }],
  ["Disabled", { "aria-disabled": "true" }],
];

// The desktop MenuPage's 150 px label column, a column per state, 8 apart, 4 between rows.
function stateTable(rows) {
  const grid = el("div", "catalog-state-grid");
  grid.style.setProperty("--catalog-state-columns", STATES.length);
  grid.append(el("div"), ...STATES.map(([s]) => el("div", "catalog-state-caption", caption(s))));
  for (const [label, create] of rows) {
    grid.append(el("div", "catalog-state-label", body(label)));
    for (const [, attributes] of STATES) {
      const cell = el("div", "catalog-state-cell", create(attributes));
      cell.style.padding = "0";
      cell.inert = true;
      grid.append(cell);
    }
  }
  return sideScroll(grid);
}

// A menu item on a menu surface shown in place, 220 wide in a 4 px margin, as in an open menu.
function inMenu(entry) {
  return (attributes) => {
    const menu = createMenu([entry], { label: entry.label });
    menu.removeAttribute("popover");
    menu.style.inlineSize = "220px";
    const item = menu.querySelector(".ui-menu-item");
    for (const [k, v] of Object.entries(attributes)) item.setAttribute(k, v);
    const wrap = el("div", "", menu);
    wrap.style.padding = "4px";
    return wrap;
  };
}

function barItem(attributes) {
  const bar = createMenuBar([{ label: "File", items: [] }], { label: "Sample" });
  const item = bar.querySelector(".ui-menu-bar-item");
  for (const [k, v] of Object.entries(attributes)) item.setAttribute(k, v);
  const wrap = el("div", "", bar);
  wrap.style.padding = "8px";
  return wrap;
}

function live() {
  const bar = createMenuBar([
    {
      label: "File",
      items: [
        { label: "New", icon: "plus", shortcut: "Mod+N" },
        { label: "Open…", icon: "magnifying-glass", shortcut: "Mod+O" },
        { label: "Recent", items: [{ label: "Report.txt" }, { label: "Notes.md" }] },
        "-",
        { label: "Print…", shortcut: "Mod+P", disabled: true },
      ],
    },
    {
      label: "View",
      items: [
        { label: "Show sidebar", type: "checkbox", checked: true },
        { label: "Show status bar", type: "checkbox" },
        "-",
        { label: "Large icons", type: "radio", group: "catalog-size" },
        { label: "Small icons", type: "radio", group: "catalog-size", checked: true },
      ],
    },
  ], { label: "Catalog menu" });

  const contextMenu = createMenu([
    { label: "Cut", shortcut: "Mod+X" },
    { label: "Copy", shortcut: "Mod+C" },
    { label: "Paste", shortcut: "Mod+V" },
  ], { label: "Edit" });
  const area = el("div", "ui-squircle ui-focus-ring", caption("Right-click here"), contextMenu);
  area.tabIndex = 0;
  area.setAttribute("role", "group");
  area.setAttribute("aria-label", "Context menu area");
  area.dataset.contextMenu = contextMenu.id;
  Object.assign(area.style, {
    display: "grid", placeItems: "center", inlineSize: "200px", blockSize: "64px",
    backgroundColor: "var(--ui-system-chrome-medium-low-color)",
  });

  const menuFlyout = createMenu([{ label: "Rename" }, { label: "Duplicate" }], { label: "Menu flyout" });
  const menuButton = el("button", "ui-button ui-squircle ui-focus-ring", "Menu flyout");
  menuButton.type = "button";
  menuButton.setAttribute("aria-haspopup", "menu");
  menuButton.setAttribute("aria-expanded", "false");
  menuButton.setAttribute("aria-controls", menuFlyout.id);

  const flyout = el("div", "ui-flyout ui-glass ui-squircle");
  flyout.id = "catalog-flyout";
  flyout.popover = "auto";
  flyout.setAttribute("role", "dialog");
  flyout.setAttribute("aria-label", "Empty bin");
  const message = el("span", "catalog-body", "All items will be deleted.");
  message.style.maxInlineSize = "200px";
  const confirm = el("button", "ui-button ui-destructive ui-squircle ui-focus-ring", "Empty bin");
  confirm.type = "button";
  confirm.addEventListener("click", () => flyout.hidePopover());
  const content = el("div", "ui-flyout-content ui-scroll", el("div", "catalog-stack", message, confirm));
  flyout.append(content);
  const flyoutButton = el("button", "ui-button ui-squircle ui-focus-ring", "Flyout");
  flyoutButton.type = "button";
  flyoutButton.setAttribute("popovertarget", flyout.id);

  const row = el("div", "", spaced(area), spaced(el("div", "", menuButton, menuFlyout)), spaced(el("div", "", flyoutButton, flyout)));
  Object.assign(row.style, { display: "flex", flexWrap: "wrap", alignItems: "center" });
  return el("div", "catalog-stack", bar, row);
}

export default {
  id: "menus-and-flyouts",
  title: "Menus and flyouts",
  icon: "dots-three",
  render: () => page("Menus and flyouts",
    section("Menu items", stateTable([
      ["Icon and shortcut", inMenu({ label: "Open…", icon: "magnifying-glass", shortcut: "Mod+O" })],
      ["Submenu", inMenu({ label: "Recent", items: [{ label: "Report.txt" }] })],
      ["Checked", inMenu({ label: "Show sidebar", type: "checkbox", checked: true })],
      ["Radio, checked", inMenu({ label: "Small icons", type: "radio", checked: true })],
    ])),
    section("Menu bar items", stateTable([["File", barItem]])),
    section("Live", live()),
    caption("Menus and flyouts are glass overlays (radius 16, 4 px inset). Items highlight in the list item's neutral fill; the menu bar's items are subtle buttons. Overlays fade in over MotionFast.")),
};
