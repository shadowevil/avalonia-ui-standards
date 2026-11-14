// Menus (0036, keyboard.md): menus, submenus, context menus, menu flyouts and the menu bar, on the
// flyout (flyout.js). Markup and look are in menu.css; createMenu and createMenuBar build them.
//
// No native element is a menu (a <select> cannot hold icons, gestures or submenus, and <menu> is a
// plain list), so a menu is a .ui-menu popover with role="menu", holding <button role="menuitem">
// items (menuitemcheckbox and menuitemradio with aria-checked), separators and submenus. The menu
// bar is the custom element <ui-menu-bar> (role="menubar"). Everything else is delegated from the
// document, so menus added at any time work.
//
// Opening:
// - Menu flyout: a button with aria-haspopup="menu" and aria-controls="<menu id>" opens the menu
//   below it, centred (Avalonia's Bottom); a click toggles it, Down and Up open it on the first and
//   last item.
// - Context menu: data-context-menu="<menu id>" on an element opens the menu at the pointer on a
//   right click, and at the element's bottom start corner with Shift+F10 or the context menu key.
// - Submenu: an item with aria-haspopup="menu" and aria-controls="<submenu id>" (the submenu
//   placed inside the parent menu) opens it at its end edge on click, Right (Left right to left),
//   Enter or Space, or after resting the pointer on it for SUBMENU_DELAY.
// - Menu bar: its items (aria-haspopup="menu", aria-controls) open their menu 4 px below on click,
//   Down, Up, Enter or Space; while one is open, pointing at another item opens that one.
// A menu opened with the keyboard focuses its first item (Up: the last); one opened with the
// pointer focuses the menu itself, so no item is highlighted until the pointer or a key picks one.
//
// Keys in a menu (APG menu): Down and Up move through the enabled items, wrapping; Home and End go
// to the first and last; typing jumps to the next item whose header starts with the typed text;
// Right opens a submenu, or in a menu bar's menu moves to the next bar menu; Left closes a submenu,
// or in a menu bar's menu moves to the previous one (both swap right to left); Escape closes the
// menu and returns focus to its opener (the parent item, bar item, button or element); Tab closes
// every menu and moves on from the opener. Disabled items (aria-disabled="true") are skipped,
// take no focus and do nothing when clicked.
//
// Choosing an item: a menuitemcheckbox flips aria-checked, a menuitemradio checks itself and
// unchecks the other radios of its data-group in the same menu, before the page's click handlers
// run; then every menu closes and focus returns to the first opener. The item's own click event
// is the page's signal.

import { icon } from "./icon.js";
import { openFlyout, closeFlyout, flyoutOpener } from "./flyout.js";

/** ms the pointer rests on an item before its submenu opens or another closes (Avalonia's MenuShowDelay). */
export const SUBMENU_DELAY = 400;
const TYPE_AHEAD_RESET = 500;
const ITEM = ".ui-menu-item";
const isMac = /Mac|iPhone|iPad/.test(navigator.platform);

let nextId = 0;
let hoverTimer = 0;
let typed = "";
let typedAt = 0;
const wasOpen = new WeakMap(); // opener -> its menu was open at pointerdown

const isDisabled = (el) => el.getAttribute("aria-disabled") === "true";
const menuOf = (el) => el.closest(".ui-menu");
const parentMenu = (menu) => menu.parentElement?.closest(".ui-menu") ?? null;
const rootMenu = (menu) => {
  let m = menu;
  while (parentMenu(m)) m = parentMenu(m);
  return m;
};
const rtl = (el) => getComputedStyle(el).direction === "rtl";

/** The items of `menu` itself (not of its submenus), in order. */
export function menuItems(menu) {
  return [...menu.querySelectorAll(ITEM)].filter((item) => menuOf(item) === menu);
}

const enabledItems = (menu) => menuItems(menu).filter((i) => !isDisabled(i));
const controlled = (el) => {
  const id = el.getAttribute("aria-controls");
  return id ? document.getElementById(id) : null;
};
const submenuOf = (item) => (item.getAttribute("aria-haspopup") === "menu" ? controlled(item) : null);
const barOf = (menu) => flyoutOpener(rootMenu(menu))?.closest("ui-menu-bar") ?? null;

// --- Building --------------------------------------------------------------------------------

/** The text and aria-keyshortcuts value of a shortcut such as "Mod+O" (Mod is Cmd on macOS, else Ctrl). */
export function formatShortcut(shortcut) {
  const parts = shortcut.split("+");
  const key = parts.pop();
  const aria = parts.map((p) => ({ Mod: isMac ? "Meta" : "Control", Ctrl: "Control", Cmd: "Meta" }[p] ?? p));
  const text = isMac
    ? parts.map((p) => ({ Mod: "⌘", Cmd: "⌘", Ctrl: "⌃", Alt: "⌥", Shift: "⇧" }[p] ?? p)).join("") + key
    : [...parts.map((p) => (p === "Mod" ? "Ctrl" : p)), key].join("+");
  return { text, aria: [...aria, key].join("+") };
}

function span(className, ...children) {
  const s = document.createElement("span");
  s.className = className;
  s.append(...children);
  return s;
}

function buildItem(entry, menuId) {
  const item = document.createElement("button");
  item.type = "button";
  item.className = "ui-menu-item ui-list-item ui-squircle";
  item.tabIndex = -1;
  const role = entry.type === "checkbox" ? "menuitemcheckbox" : entry.type === "radio" ? "menuitemradio" : "menuitem";
  item.setAttribute("role", role);
  if (role !== "menuitem") {
    item.setAttribute("aria-checked", String(!!entry.checked));
    const mark = role === "menuitemcheckbox" ? icon("check") : span("ui-menu-item-dot");
    item.append(span("ui-menu-item-mark", mark));
    if (entry.group) item.dataset.group = entry.group;
  }
  if (entry.icon) {
    const i = icon(entry.icon);
    i.classList.add("ui-menu-item-icon");
    item.append(i);
  }
  item.append(span("ui-menu-item-header", entry.label));
  if (entry.shortcut) {
    const { text, aria } = formatShortcut(entry.shortcut);
    const gesture = span("ui-menu-item-gesture", text);
    gesture.setAttribute("aria-hidden", "true");
    item.append(gesture);
    item.setAttribute("aria-keyshortcuts", aria);
  }
  if (entry.disabled) item.setAttribute("aria-disabled", "true");
  if (entry.onSelect) item.addEventListener("click", entry.onSelect);
  if (!entry.items) return [item];
  const caret = icon("caret-right");
  caret.classList.add("ui-menu-item-caret");
  item.append(caret);
  const sub = createMenu(entry.items, { label: entry.label, id: `${menuId}-${nextId++}` });
  item.setAttribute("aria-haspopup", "menu");
  item.setAttribute("aria-expanded", "false");
  item.setAttribute("aria-controls", sub.id);
  return [item, sub];
}

/**
 * A menu (a .ui-menu popover) from entries: "-" for a separator, or { label, icon (an icon name),
 * shortcut ("Mod+O"), type ("checkbox" or "radio"), checked, group (radios), disabled, items (a
 * submenu's entries), onSelect (a click listener) }. Options: label (the menu's accessible name),
 * id, placement (data-placement, flyout.css). Place it in the page next to its opener.
 */
export function createMenu(entries, { label, id = `ui-menu-${nextId++}`, placement } = {}) {
  const menu = document.createElement("div");
  menu.id = id;
  menu.className = "ui-menu ui-flyout ui-glass ui-squircle";
  menu.popover = "auto";
  menu.setAttribute("role", "menu");
  if (label) menu.setAttribute("aria-label", label);
  if (placement) menu.dataset.placement = placement;
  const content = document.createElement("div");
  content.className = "ui-flyout-content ui-scroll";
  for (const entry of entries) {
    if (entry === "-") {
      const separator = document.createElement("div");
      separator.className = "ui-menu-separator";
      separator.setAttribute("role", "separator");
      content.append(separator);
    } else {
      content.append(...buildItem(entry, id));
    }
  }
  menu.append(content);
  return menu;
}

/**
 * A <ui-menu-bar> from [{ label, items }] (items as for createMenu), with its accessible name
 * `label`. Each title is a subtle button whose menu follows it in the bar.
 */
export function createMenuBar(menus, { label } = {}) {
  const bar = document.createElement("ui-menu-bar");
  if (label) bar.setAttribute("aria-label", label);
  for (const entry of menus) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "ui-menu-bar-item ui-button ui-subtle ui-squircle";
    button.setAttribute("role", "menuitem");
    button.textContent = entry.label;
    const menu = createMenu(entry.items, { label: entry.label, placement: "block-end-start" });
    button.setAttribute("aria-haspopup", "menu");
    button.setAttribute("aria-expanded", "false");
    button.setAttribute("aria-controls", menu.id);
    if (entry.disabled) button.setAttribute("aria-disabled", "true");
    bar.append(button, menu);
  }
  return bar;
}

// --- Opening and closing ---------------------------------------------------------------------

/**
 * Opens `menu` from `opener`: as a submenu when the opener is a menu item, below a menu bar item,
 * or below a button. focus: "first", "last" or "menu" (the menu itself; a pointer opening).
 */
export function openMenu(menu, opener, { focus = "first", point = null } = {}) {
  clearTimeout(hoverTimer);
  const inMenu = opener?.matches(ITEM);
  const placement = point ? "point" : inMenu ? "inline-end" : (menu.dataset.placement ?? "block-end");
  prepare(menu);
  const items = enabledItems(menu);
  const target = focus === "menu" ? menu : focus === "last" ? items.at(-1) : items[0];
  openFlyout(menu, { anchor: point ? null : opener, opener, point, placement, focus: target ?? menu });
}

/** Closes `menu` and its submenus; focus returns to its opener when it was inside (or with restoreFocus). */
export function closeMenu(menu, { restoreFocus } = {}) {
  clearTimeout(hoverTimer);
  closeFlyout(menu, { restoreFocus });
}

// Items take focus only by script (tabindex -1); the menu itself can hold focus with no item lit.
function prepare(menu) {
  menu.tabIndex = -1;
  for (const item of menuItems(menu)) item.tabIndex = -1;
}

function focusItem(item) {
  if (document.activeElement !== item) item.focus({ preventScroll: true });
  item.scrollIntoView({ block: "nearest" });
}

function move(menu, from, delta) {
  const items = enabledItems(menu);
  if (!items.length) return;
  const i = items.indexOf(from);
  const next = i < 0 ? (delta > 0 ? 0 : items.length - 1) : (i + delta + items.length) % items.length;
  focusItem(items[next]);
}

function typeAhead(menu, from, char) {
  const now = performance.now();
  typed = now - typedAt > TYPE_AHEAD_RESET ? char : typed + char;
  typedAt = now;
  const items = enabledItems(menu);
  const text = (i) => (i.querySelector(".ui-menu-item-header")?.textContent ?? i.textContent).trim().toLowerCase();
  const start = items.indexOf(from);
  // A repeated single letter cycles through the items starting with it.
  const search = typed.length > 1 && [...typed].every((c) => c === typed[0]) ? typed[0] : typed;
  const offset = search.length === 1 ? 1 : 0;
  for (let k = 0; k < items.length; k++) {
    const item = items[(start + offset + k + items.length) % items.length];
    if (text(item).startsWith(search)) return focusItem(item);
  }
}

// The bar menu `delta` titles away from the open one, opened on its first item.
function moveInBar(menu, delta) {
  const root = rootMenu(menu);
  const bar = barOf(root);
  if (!bar) return false;
  const titles = bar.items.filter((t) => !isDisabled(t));
  const current = flyoutOpener(root);
  const next = titles[(titles.indexOf(current) + delta + titles.length) % titles.length];
  if (!next || next === current) return true;
  closeMenu(root, { restoreFocus: false });
  bar.focusItem(next);
  const nextMenu = controlled(next);
  if (nextMenu) openMenu(nextMenu, next, { focus: "first" });
  return true;
}

function onMenuKey(e, menu) {
  const item = e.target.closest(ITEM);
  const fromItem = item && menuOf(item) === menu ? item : null;
  const forward = rtl(menu) ? "ArrowLeft" : "ArrowRight";
  const back = rtl(menu) ? "ArrowRight" : "ArrowLeft";
  switch (e.key) {
    case "ArrowDown":
      move(menu, fromItem, 1);
      break;
    case "ArrowUp":
      move(menu, fromItem, -1);
      break;
    case "Home":
      move(menu, null, 1);
      break;
    case "End":
      move(menu, null, -1);
      break;
    case forward: {
      const sub = fromItem && !isDisabled(fromItem) && submenuOf(fromItem);
      if (sub) openMenu(sub, fromItem, { focus: "first" });
      else if (!moveInBar(menu, 1)) return;
      break;
    }
    case back:
      if (parentMenu(menu)) closeMenu(menu, { restoreFocus: true });
      else if (!moveInBar(menu, -1)) return;
      break;
    case "Escape":
      closeMenu(menu, { restoreFocus: true });
      break;
    case "Tab": {
      // Every menu closes; focus goes back to the first opener, and the Tab moves on from there.
      const root = rootMenu(menu);
      const opener = flyoutOpener(root);
      closeMenu(root, { restoreFocus: false });
      opener?.focus({ preventScroll: true });
      return;
    }
    default:
      if (e.key.length === 1 && e.key !== " " && !e.ctrlKey && !e.metaKey && !e.altKey) typeAhead(menu, fromItem, e.key.toLowerCase());
      else return;
  }
  e.preventDefault();
  e.stopPropagation();
}

// --- Delegated behaviour ---------------------------------------------------------------------

document.addEventListener("keydown", (e) => {
  const target = e.target instanceof Element ? e.target : null;
  if (!target) return;
  const menu = menuOf(target);
  if (menu && (target === menu || target.matches(ITEM))) return onMenuKey(e, menu);

  // A menu button: Down and Up open its menu on the first and last item.
  const opener = target.closest("[aria-haspopup='menu'][aria-controls]");
  if (opener && !opener.matches(ITEM) && (e.key === "ArrowDown" || e.key === "ArrowUp") && !isDisabled(opener)) {
    const m = controlled(opener);
    if (m?.classList.contains("ui-menu")) {
      e.preventDefault();
      openMenu(m, opener, { focus: e.key === "ArrowUp" ? "last" : "first" });
    }
    return;
  }

  // Shift+F10 on an element with a context menu (the context menu key fires contextmenu itself).
  if (e.key === "F10" && e.shiftKey) {
    const host = target.closest("[data-context-menu]");
    if (host && openContextMenu(host, null)) e.preventDefault();
  }
});

document.addEventListener("pointerdown", (e) => {
  const opener = e.target.closest?.("[aria-haspopup='menu'][aria-controls]");
  if (opener) wasOpen.set(opener, !!controlled(opener)?.matches(":popover-open"));
}, true);

// Choosing: disabled items do nothing; check and radio items change before the page's handlers.
document.addEventListener("click", (e) => {
  const item = e.target.closest?.(ITEM);
  if (!item || !menuOf(item)) return;
  if (isDisabled(item)) {
    e.stopImmediatePropagation();
    e.preventDefault();
    return;
  }
  const role = item.getAttribute("role");
  if (role === "menuitemcheckbox") item.setAttribute("aria-checked", String(item.getAttribute("aria-checked") !== "true"));
  if (role === "menuitemradio") {
    const group = item.dataset.group ?? "";
    for (const other of menuItems(menuOf(item))) {
      if (other.getAttribute("role") === "menuitemradio" && (other.dataset.group ?? "") === group) {
        other.setAttribute("aria-checked", String(other === item));
      }
    }
  }
}, true);

document.addEventListener("click", (e) => {
  const target = e.target instanceof Element ? e.target : null;
  const opener = target?.closest("[aria-haspopup='menu'][aria-controls]");
  const keyboard = e.detail === 0;
  if (opener && !isDisabled(opener)) {
    const menu = controlled(opener);
    if (!menu?.classList.contains("ui-menu")) return;
    const open = menu.matches(":popover-open");
    // A submenu item opens its submenu, or with the keyboard moves into the open one.
    if (opener.matches(ITEM)) {
      if (!open) openMenu(menu, opener, { focus: keyboard ? "first" : "menu" });
      else if (keyboard) focusItem(enabledItems(menu)[0] ?? menu);
      return;
    }
    // A click on the opener of an open menu closes it (a pointer's light dismiss already has).
    if (open || (!keyboard && wasOpen.get(opener))) {
      closeMenu(menu, { restoreFocus: false });
      wasOpen.delete(opener);
      return;
    }
    openMenu(menu, opener, { focus: keyboard ? "first" : "menu" });
    return;
  }
  const item = target?.closest(ITEM);
  if (!item || !menuOf(item) || isDisabled(item)) return;
  const root = rootMenu(menuOf(item));
  closeMenu(root, { restoreFocus: true });
});

// The pointer moves focus to the item under it (the highlight); resting on an item opens its
// submenu and closes another open one after SUBMENU_DELAY.
document.addEventListener("pointerover", (e) => {
  const target = e.target instanceof Element ? e.target : null;
  const barItem = target?.closest("ui-menu-bar > .ui-menu-bar-item");
  if (barItem) return onBarHover(barItem);
  const item = target?.closest(ITEM);
  const menu = item && menuOf(item);
  if (!menu || !menu.matches(":popover-open")) return;
  clearTimeout(hoverTimer);
  if (isDisabled(item)) {
    if (menu.contains(document.activeElement)) menu.focus({ preventScroll: true });
    return;
  }
  if (document.activeElement !== item) item.focus({ preventScroll: true });
  const sub = submenuOf(item);
  if (sub?.matches(":popover-open")) return;
  hoverTimer = setTimeout(() => {
    if (!menu.matches(":popover-open")) return;
    for (const other of menuItems(menu)) {
      const s = submenuOf(other);
      if (s && s !== sub && s.matches(":popover-open")) closeMenu(s, { restoreFocus: false });
    }
    if (sub) openMenu(sub, item, { focus: "menu" });
  }, SUBMENU_DELAY);
});

function onBarHover(barItem) {
  const bar = barItem.parentElement;
  const open = bar.items.find((t) => controlled(t)?.matches(":popover-open"));
  if (!open || open === barItem || isDisabled(barItem)) return;
  const menu = controlled(barItem);
  if (!menu) return;
  closeMenu(controlled(open), { restoreFocus: false });
  bar.focusItem(barItem);
  openMenu(menu, barItem, { focus: "menu" });
}

function openContextMenu(host, point) {
  const menu = document.getElementById(host.dataset.contextMenu);
  if (!menu?.classList.contains("ui-menu")) return false;
  if (!point) {
    const r = host.getBoundingClientRect();
    point = { x: rtl(host) ? r.right : r.left, y: r.bottom };
  }
  openMenu(menu, host, { focus: point.keyboard === false ? "menu" : "first", point });
  return true;
}

document.addEventListener("contextmenu", (e) => {
  // A right click inside an open menu does not open it again.
  if (e.target.closest?.(".ui-menu")) return;
  const host = e.target.closest?.("[data-context-menu]");
  if (!host) return;
  // A right click carries the pointer's place; the context menu key does not (button 0).
  const point = e.button === 2 ? { x: e.clientX, y: e.clientY, keyboard: false } : null;
  if (openContextMenu(host, point)) e.preventDefault();
});

// --- Menu bar --------------------------------------------------------------------------------

/**
 * <ui-menu-bar>: role="menubar", a row of .ui-menu-bar-item titles, each followed by its menu.
 * One Tab stop with a roving tabindex; Left and Right move between titles (swapped right to
 * left), wrapping, and Home and End go to the first and last. The menu row of a window (0044:
 * placement, folding, Alt and F10) is the menu row Module's.
 */
export class UiMenuBar extends HTMLElement {
  #observer = new MutationObserver(() => this.#sync());

  connectedCallback() {
    if (!this.hasAttribute("role")) this.setAttribute("role", "menubar");
    this.addEventListener("keydown", this.#onKeyDown);
    this.addEventListener("focusin", this.#onFocusIn);
    this.#observer.observe(this, { childList: true, attributes: true, subtree: false, attributeFilter: ["aria-disabled"] });
    this.#sync();
  }

  disconnectedCallback() {
    this.#observer.disconnect();
  }

  /** The bar's titles, in order. */
  get items() {
    return [...this.children].filter((c) => c.classList.contains("ui-menu-bar-item"));
  }

  /** Moves the Tab stop to `item` and focuses it. */
  focusItem(item) {
    for (const t of this.items) t.tabIndex = t === item ? 0 : -1;
    item.focus({ preventScroll: true });
  }

  #sync() {
    const items = this.items;
    const current = items.find((t) => t.tabIndex === 0 && !isDisabled(t)) ?? items.find((t) => !isDisabled(t));
    for (const t of items) t.tabIndex = t === current ? 0 : -1;
  }

  #onFocusIn = (e) => {
    const item = e.target.closest?.(".ui-menu-bar-item");
    if (item?.parentElement === this) for (const t of this.items) t.tabIndex = t === item ? 0 : -1;
  };

  #onKeyDown = (e) => {
    const item = e.target.closest?.(".ui-menu-bar-item");
    if (!item || item.parentElement !== this) return;
    const titles = this.items.filter((t) => !isDisabled(t));
    const i = titles.indexOf(item);
    const next = rtl(this) ? "ArrowLeft" : "ArrowRight";
    const prev = rtl(this) ? "ArrowRight" : "ArrowLeft";
    let target = null;
    if (e.key === next) target = titles[(i + 1) % titles.length];
    else if (e.key === prev) target = titles[(i - 1 + titles.length) % titles.length];
    else if (e.key === "Home") target = titles[0];
    else if (e.key === "End") target = titles.at(-1);
    else return;
    e.preventDefault();
    if (target) this.focusItem(target);
  };
}

if (!customElements.get("ui-menu-bar")) customElements.define("ui-menu-bar", UiMenuBar);
