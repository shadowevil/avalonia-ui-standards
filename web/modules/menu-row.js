// Menu row (0044, 0036): an app's menus, defined once as a plain structure, shown as a row of menu
// titles on the menu bar Block (<ui-menu-bar>, menu.js). In a browser there is no system menu bar,
// so the row shows on every OS whenever the app has a menu. The look is menu-row.css.
//
// A menu definition mirrors Avalonia's NativeMenu: an array of titles, each an item whose `items`
// are its menu. An item is "-" or { separator: true, visible } for a separator, or
//   { header      text, "_" before the access key ("__" for a literal underscore)
//     gesture     a shortcut such as "Mod+O" (Mod is Cmd on macOS, else Ctrl; menu.js formats it)
//     icon        an icon name (icon.js)
//     tooltip     text
//     enabled     false disables it (default true)
//     visible     false leaves it out (default true)
//     toggle      "check" or "radio" (radio siblings in one menu, or one `group`, are a set)
//     checked     its mark
//     items       a submenu's items
//     onClick     called with (item) when it is chosen, by click, key or gesture }
// Properties may be getters: the row reads them again on refresh(), whenever one of its menus
// opens, and after an item is chosen. The definition keeps the check state, as native items do: a
// chosen check or radio item shows what its definition says afterwards, so onClick sets `checked`.
//
// createMenuRow(menu, { appMenu, label, helpHeader, moreLabel }) builds the row and returns a
// MenuRow; put its `element` in the shell's menu row slot (.ui-shell-menu-row). appMenu is the
// application menu (for example About), appended after a separator at the end of the Help menu: the
// last title whose header matches helpHeader without its access key mark and case (default
// "_Help"); without one, a Help title is added at the end to hold it (no separator).
//
// Behaviour (0044, with the browser's limits):
// - Items, separators, submenus, check and radio items, gestures, icons, tooltips, enabled,
//   visible, header changes, and items added or removed: refresh() updates the row in place, so an
//   open menu stays open. Hidden items are left out of the DOM; a separator that would be first,
//   last or next to another separator in a menu is left out too.
// - Narrow: titles that do not fit fold, from the end, into a "…" title (Icons.DotsThree, tooltip
//   and name moreLabel, default "More") right after the last title shown, holding them as submenus.
// - Keys: the row is a menubar with a roving tabindex, reached by Tab (menu.js). Alt with a title's
//   access key opens its menu wherever the browser hands the key to the page; on macOS not while
//   a text field has focus, where Option types characters. In an open menu, an item's access key
//   chooses it (several with the same key: focus moves between them). Holding Alt underlines the
//   access keys. Plain Alt and F10 stay the browser's. Item gestures work from anywhere in the
//   page: a key the focused element has already handled (defaultPrevented), or a text editing key
//   in a text field, is left to it; a gesture does nothing while its item or one of its menus is
//   disabled or hidden; one key chooses one item.
// - After an item is chosen, focus returns to where it was before the row took it.
// - No window dragging: the browser owns the window.

import { createMenuBar, createMenuBarItem, createMenuItem, createMenuSeparator, menuItems, openMenu, formatShortcut } from "../blocks/menu.js";
import { icon } from "../blocks/icon.js";
import { tooltip } from "../blocks/tooltip.js";
import { isMac } from "../style/platform.js";

const HELP_HEADER = "_Help";
const MORE_LABEL = "More";
const LABEL = "Menu";

// Keys a text field uses itself: typing and moving keys, and its editing commands.
const TEXT_EDIT_KEYS = new Set(["a", "c", "v", "x", "z", "y"]);
const TEXT_MOVE_KEYS = new Set(["arrowleft", "arrowright", "arrowup", "arrowdown", "home", "end", "backspace", "delete"]);

const rows = new Set();

/** The text of a header and its access key: "Cu_t" is { text: "Cut", key: "t", index: 2 }. */
export function parseHeader(header = "") {
  let text = "";
  let key = null;
  let index = -1;
  for (let i = 0; i < header.length; i++) {
    const c = header[i];
    if (c !== "_") text += c;
    else if (header[i + 1] === "_") {
      text += "_";
      i++;
    } else if (key === null && i + 1 < header.length) {
      key = header[i + 1].toLowerCase();
      index = text.length;
    }
  }
  return { text, key, index };
}

const plain = (header) => parseHeader(header).text.trim().toLocaleLowerCase();
const isSeparator = (def) => def === "-" || !!def?.separator;
const visible = (def) => (def === "-" ? true : def.visible !== false);
const enabled = (def) => def.enabled !== false;
const toggleOf = (def) => (def.toggle === "check" ? "checkbox" : def.toggle === "radio" ? "radio" : "");

/** A gesture string parsed: { ctrl, meta, alt, shift, key } (key lower case). */
function parseGesture(gesture) {
  const parts = gesture.split("+");
  const key = parts.pop().toLowerCase();
  const g = { ctrl: false, meta: false, alt: false, shift: false, key };
  for (const p of parts) {
    if (p === "Mod") g[isMac ? "meta" : "ctrl"] = true;
    else if (p === "Ctrl") g.ctrl = true;
    else if (p === "Cmd" || p === "Meta") g.meta = true;
    else if (p === "Alt") g.alt = true;
    else if (p === "Shift") g.shift = true;
  }
  return g;
}

// A letter or digit is matched by its physical key, so Shift and Option do not change it.
function keyMatches(key, e) {
  if (/^[a-z]$/.test(key)) return e.code === `Key${key.toUpperCase()}`;
  if (/^[0-9]$/.test(key)) return e.code === `Digit${key}`;
  return e.key.toLowerCase() === key;
}

/** True when keydown `e` is `gesture` ("Shift+Mod+S"). Test hook. */
export function gestureMatches(gesture, e) {
  const g = parseGesture(gesture);
  return g.ctrl === e.ctrlKey && g.meta === e.metaKey && g.alt === e.altKey && g.shift === e.shiftKey && keyMatches(g.key, e);
}

function isTextField(el) {
  if (!(el instanceof Element)) return false;
  if (el.isContentEditable) return true;
  if (el.localName === "textarea") return true;
  return el.localName === "input" && !["button", "checkbox", "radio", "range", "color", "file", "submit", "reset", "image"].includes(el.type);
}

function isTextEditingKey(gesture) {
  const g = parseGesture(gesture);
  if (!g.ctrl && !g.meta && !g.alt) return true;
  if (TEXT_MOVE_KEYS.has(g.key)) return true;
  return (g.ctrl || g.meta) && !g.alt && TEXT_EDIT_KEYS.has(g.key);
}

/**
 * The number of titles that fit in `width` at their `widths`: all of them, or as many from the
 * start as fit beside the "…" title of `moreWidth`. Test hook.
 */
export function titlesThatFit(widths, moreWidth, width) {
  if (widths.reduce((a, b) => a + b, 0) <= width + 0.5) return widths.length;
  let used = moreWidth;
  let count = 0;
  while (count < widths.length && used + widths[count] <= width + 0.5) used += widths[count++];
  return count;
}

// The header's text, with the access key in a span that menu-row.css underlines while shown.
function renderHeader(target, header) {
  const { text, index } = parseHeader(header);
  if (index < 0) {
    target.textContent = text;
    return;
  }
  const mark = document.createElement("span");
  mark.className = "ui-menu-row-access-key";
  mark.textContent = text[index];
  target.replaceChildren(text.slice(0, index), mark, text.slice(index + 1));
}

// Places `nodes` as `parent`'s children, in order, moving only the nodes out of place, so an
// open popover among them stays open (moving one in the DOM closes it).
function placeChildren(parent, nodes) {
  const wanted = new Set(nodes);
  for (const child of [...parent.children]) if (!wanted.has(child)) child.remove();
  let ref = parent.firstElementChild;
  for (const node of nodes) {
    if (node === ref) ref = ref.nextElementSibling;
    else parent.insertBefore(node, ref);
  }
}

/** A menu row built from `menu` (see the module comment). */
export class MenuRow {
  #menu;
  #appMenu;
  #helpHeader;
  #moreLabel;
  #records = new WeakMap(); // menu element or bar -> Map(key -> record)
  #byElement = new WeakMap(); // item or title element -> record
  #views = new WeakMap(); // title definition -> its entry in the "…" menu
  #addedHelp;
  #appSeparator = { separator: true };
  #more;
  #moreMenu;
  #moreTip;
  #widths = new WeakMap(); // title record -> width when last shown
  #lastOutside = null;
  #accessKeysOpen = false;
  #observer;

  constructor(menu = [], { appMenu = [], label = LABEL, helpHeader = HELP_HEADER, moreLabel = MORE_LABEL } = {}) {
    this.#menu = menu;
    this.#appMenu = appMenu;
    this.#helpHeader = helpHeader;
    this.#moreLabel = moreLabel;
    const row = this;
    this.#addedHelp = {
      get header() {
        return row.#helpHeader;
      },
    };
    this.element = createMenuBar([], { label });
    this.element.classList.add("ui-menu-row");
    this.#buildMore();
    this.element.addEventListener("keydown", this.#onMenuKey);
    this.element.addEventListener("click", this.#onClick);
    this.element.addEventListener("beforetoggle", this.#onBeforeToggle, true);
    this.element.addEventListener("toggle", this.#onToggle, true);
    this.#observer = new ResizeObserver(() => this.#fold());
    this.#observer.observe(this.element);
    rows.add(this);
    startDocumentListeners();
    this.refresh();
    document.fonts?.ready.then(() => this.#fold());
  }

  /** The window's menu (an array of titles). */
  get menu() {
    return this.#menu;
  }

  set menu(value) {
    this.#menu = value ?? [];
    this.refresh();
  }

  /** The application menu, appended to Help. */
  get appMenu() {
    return this.#appMenu;
  }

  set appMenu(value) {
    this.#appMenu = value ?? [];
    this.refresh();
  }

  /** The Help header the application menu joins (default "_Help"). */
  get helpHeader() {
    return this.#helpHeader;
  }

  set helpHeader(value) {
    this.#helpHeader = value || HELP_HEADER;
    this.refresh();
  }

  /** The "…" title's tooltip and accessible name (default "More"). */
  get moreLabel() {
    return this.#moreLabel;
  }

  set moreLabel(value) {
    this.#moreLabel = value || MORE_LABEL;
    this.#more.setAttribute("aria-label", this.#moreLabel);
    this.#moreTip.textContent = this.#moreLabel;
    this.#moreMenu.setAttribute("aria-label", this.#moreLabel);
  }

  /** The "…" title. */
  get moreItem() {
    return this.#more;
  }

  /** The title elements, folded or not, in order (not "…"). */
  get titles() {
    const map = this.#records.get(this.element);
    return this.#titleDefs().map((def) => map?.get(def)?.el).filter(Boolean);
  }

  /** Reads the definitions again and updates the row in place. */
  refresh() {
    const map = this.#records.get(this.element) ?? new Map();
    this.#records.set(this.element, map);
    const defs = this.#titleDefs();
    const seen = new Set(defs);
    for (const def of defs) {
      let r = map.get(def);
      if (!r) {
        r = this.#createTitle(def);
        map.set(def, r);
      }
      this.#updateTitle(r);
    }
    for (const [def, r] of map) {
      if (seen.has(def)) continue;
      r.el.remove();
      r.sub.remove();
      map.delete(def);
    }
    this.element.hidden = defs.length === 0;
    this.#fold();
  }

  /** Stops the row's listeners; the element can be removed. */
  destroy() {
    this.#observer.disconnect();
    rows.delete(this);
    this.element.remove();
  }

  // --- Definitions ------------------------------------------------------------------------------

  #help() {
    const target = plain(this.#helpHeader);
    return this.#menu.filter((t) => visible(t) && plain(t.header) === target).at(-1) ?? null;
  }

  #appItems() {
    return this.#appMenu.filter((i) => isSeparator(i) || visible(i));
  }

  // The shown titles: the menu's visible ones, and the added Help when there is no Help to join.
  #titleDefs() {
    const defs = this.#menu.filter(visible);
    if (!this.#help() && this.#appItems().some((i) => !isSeparator(i))) defs.push(this.#addedHelp);
    return defs;
  }

  #entriesOf(title) {
    if (title === this.#addedHelp) return this.#appMenu;
    const own = title.items ?? [];
    if (title === this.#help() && this.#appItems().some((i) => !isSeparator(i))) return [...own, this.#appSeparator, ...this.#appMenu];
    return own;
  }

  // A folded title in the "…" menu: a submenu item standing for it.
  #viewOf(title) {
    let view = this.#views.get(title);
    if (!view) {
      const row = this;
      view = {
        get header() {
          return title.header;
        },
        get enabled() {
          return title.enabled;
        },
        get items() {
          return row.#entriesOf(title);
        },
      };
      this.#views.set(title, view);
    }
    return view;
  }

  // --- Building ---------------------------------------------------------------------------------

  #buildMore() {
    const { item: button, menu } = createMenuBarItem({ label: "…", items: [] });
    button.classList.add("ui-menu-row-more");
    button.replaceChildren(icon("dots-three"));
    button.setAttribute("aria-label", this.#moreLabel);
    this.#more = button;
    this.#moreMenu = menu;
    this.#moreMenu.setAttribute("aria-label", this.#moreLabel);
    this.element.append(button, menu);
    this.#moreTip = tooltip(button, this.#moreLabel);
  }

  #createTitle(def) {
    const { item: el, menu: sub } = createMenuBarItem({ label: "", items: [] });
    const r = { def, el, sub, key: null };
    this.#byElement.set(el, r);
    return r;
  }

  #updateTitle(r) {
    const { def } = r;
    if (r.header !== def.header) {
      r.header = def.header;
      // One span, so the access key mark is not a flex item of its own in the button.
      const label = document.createElement("span");
      renderHeader(label, def.header);
      r.el.replaceChildren(label);
      const { text, key } = parseHeader(def.header);
      r.key = key;
      r.sub.setAttribute("aria-label", text);
      if (key) r.el.setAttribute("aria-keyshortcuts", `Alt+${key.toUpperCase()}`);
      else r.el.removeAttribute("aria-keyshortcuts");
    }
    this.#setDisabled(r.el, !enabled(def));
    this.#fill(r.sub, this.#entriesOf(def));
  }

  #setDisabled(el, disabled) {
    if (disabled) el.setAttribute("aria-disabled", "true");
    else el.removeAttribute("aria-disabled");
  }

  #createItem(def) {
    if (isSeparator(def)) {
      const el = createMenuSeparator();
      return { def, el, sub: null, tip: null, separator: true };
    }
    // Built by menu.js; a submenu starts empty and is filled here.
    const type = toggleOf(def);
    const entry = { label: "", type: type || undefined, group: def.group, items: def.items ? [] : undefined };
    const { item: el, submenu: sub } = createMenuItem(entry);
    const r = { def, el, sub, tip: null, separator: false, sig: `${type}|${!!def.items}` };
    this.#byElement.set(el, r);
    return r;
  }

  #updateItem(r) {
    const { def, el } = r;
    if (r.header !== def.header) {
      r.header = def.header;
      renderHeader(el.querySelector(".ui-menu-item-header"), def.header);
      r.key = parseHeader(def.header).key;
      r.sub?.setAttribute("aria-label", parseHeader(def.header).text);
    }
    this.#setDisabled(el, !enabled(def));
    if (el.hasAttribute("aria-checked")) el.setAttribute("aria-checked", String(!!def.checked));
    if (r.icon !== def.icon) {
      r.icon = def.icon;
      el.querySelector(":scope > .ui-menu-item-icon")?.remove();
      if (def.icon) {
        const i = icon(def.icon);
        i.classList.add("ui-menu-item-icon");
        el.insertBefore(i, el.querySelector(":scope > .ui-menu-item-header"));
      }
    }
    if (r.gesture !== def.gesture) {
      r.gesture = def.gesture;
      el.querySelector(":scope > .ui-menu-item-gesture")?.remove();
      el.removeAttribute("aria-keyshortcuts");
      if (def.gesture) {
        const { text, aria } = formatShortcut(def.gesture);
        const span = document.createElement("span");
        span.className = "ui-menu-item-gesture";
        span.setAttribute("aria-hidden", "true");
        span.textContent = text;
        el.querySelector(":scope > .ui-menu-item-header").after(span);
        el.setAttribute("aria-keyshortcuts", aria);
      }
    }
    if (r.tooltip !== def.tooltip) {
      r.tooltip = def.tooltip;
      if (def.tooltip && r.tip) r.tip.textContent = def.tooltip;
      else if (def.tooltip) r.tip = tooltip(el, def.tooltip);
      else if (r.tip) {
        const ids = el.getAttribute("aria-describedby").split(/\s+/).filter((id) => id !== r.tip.id);
        if (ids.length) el.setAttribute("aria-describedby", ids.join(" "));
        else el.removeAttribute("aria-describedby");
        r.tip.remove();
        r.tip = null;
      }
    }
    if (r.sub) this.#fill(r.sub, def.items ?? []);
  }

  // Fills a menu with `entries`, reusing the elements built for the same definitions.
  #fill(menu, entries) {
    const content = menu.querySelector(":scope > .ui-flyout-content");
    const map = this.#records.get(menu) ?? new Map();
    this.#records.set(menu, map);
    const seen = new Set();
    const list = [];
    let separators = 0;
    for (const def of entries) {
      const key = def === "-" ? `-${separators++}` : def;
      let r = map.get(key);
      if (r && !r.separator && r.sig !== `${toggleOf(def)}|${!!def.items}`) {
        r = null;
      }
      if (!r) {
        r = this.#createItem(def);
        map.set(key, r);
      }
      r.def = def;
      seen.add(key);
      if (!r.separator) this.#updateItem(r);
      list.push(r);
    }
    for (const key of [...map.keys()]) if (!seen.has(key)) map.delete(key);

    // Separators only between shown items (0044).
    const last = list.findLastIndex((r) => !r.separator && visible(r.def));
    const nodes = [];
    let itemSince = false;
    list.forEach((r, i) => {
      let show;
      if (r.separator) {
        show = visible(r.def) && itemSince && i < last;
        if (show) itemSince = false;
      } else {
        show = visible(r.def);
        if (show) itemSince = true;
      }
      if (show) nodes.push(r.el, ...(r.sub ? [r.sub] : []), ...(r.tip ? [r.tip] : []));
    });
    const hadFocus = content.contains(document.activeElement);
    placeChildren(content, nodes);
    if (hadFocus && !content.contains(document.activeElement) && menu.matches(":popover-open")) menu.focus({ preventScroll: true });
  }

  // --- Folding ----------------------------------------------------------------------------------

  #fold() {
    const map = this.#records.get(this.element);
    if (!map) return;
    const titles = this.#titleDefs().map((d) => map.get(d)).filter(Boolean);
    const bar = this.element;
    const moreTip = this.#moreTip;
    const nodesFor = (rs, more) => [
      ...rs.flatMap((r) => [r.el, r.sub]),
      ...(more ? [this.#more, this.#moreMenu, moreTip] : []),
    ];
    // Not laid out (not in the page, or hidden): every title, folded again once it is (ResizeObserver).
    if (!bar.getClientRects().length) {
      placeChildren(bar, nodesFor(titles, false));
      return;
    }
    const hadFocus = bar.items.some((t) => t === document.activeElement);

    // Every title shown, to measure the ones never measured and the ones shown now.
    const shownBefore = new Set(titles.filter((r) => r.el.isConnected));
    if (titles.some((r) => !this.#widths.has(r)) || !this.#more.isConnected) placeChildren(bar, nodesFor(titles, true));
    for (const r of titles) if (r.el.isConnected && (shownBefore.has(r) || !this.#widths.has(r))) this.#widths.set(r, r.el.getBoundingClientRect().width);
    const moreWidth = this.#more.isConnected ? this.#more.getBoundingClientRect().width : (this.#widths.get(this.#more) ?? 0);
    if (moreWidth) this.#widths.set(this.#more, moreWidth);
    const style = getComputedStyle(bar);
    const width = bar.clientWidth - parseFloat(style.paddingInlineStart) - parseFloat(style.paddingInlineEnd);
    const fit = titlesThatFit(titles.map((r) => this.#widths.get(r)), moreWidth, width);

    const folded = titles.slice(fit);
    this.#fill(this.#moreMenu, folded.map((r) => this.#viewOf(r.def)));
    const focused = document.activeElement;
    placeChildren(bar, nodesFor(titles.slice(0, fit), folded.length > 0));
    // A title that folded while focused hands focus to "…".
    if (hadFocus && !bar.contains(focused) && folded.length) bar.focusItem(this.#more);
  }

  // --- Behaviour --------------------------------------------------------------------------------

  #recordOf(el) {
    return el ? this.#byElement.get(el) ?? null : null;
  }

  /** Chooses `def`: its onClick, then the row reads the definitions again. */
  choose(def) {
    if (def.items) return;
    def.onClick?.(def);
    this.refresh();
  }

  #onClick = (e) => {
    const item = e.target.closest?.(".ui-menu-item");
    const r = this.#recordOf(item);
    // menu.js stops clicks on disabled items before they get here.
    if (!r || r.separator || r.def.items) return;
    this.choose(r.def);
    // After menu.js has closed the menus (its document listener runs after this one).
    const back = this.#lastOutside;
    setTimeout(() => {
      const active = document.activeElement;
      const inRow = !active || active === document.body || this.element.contains(active);
      if (back?.isConnected && inRow) back.focus({ preventScroll: true });
    });
  };

  // An open menu's access keys: one match chooses it (a submenu opens); several take turns.
  #onMenuKey = (e) => {
    if (e.key.length !== 1 || e.ctrlKey || e.metaKey || e.altKey || e.defaultPrevented) return;
    const menu = e.target.closest?.(".ui-menu");
    if (!menu || !this.element.contains(menu)) return;
    const char = e.key.toLowerCase();
    const matches = menuItems(menu).filter((i) => i.getAttribute("aria-disabled") !== "true" && this.#recordOf(i)?.key === char);
    if (!matches.length) return;
    e.preventDefault();
    e.stopPropagation();
    if (matches.length === 1) {
      matches[0].focus({ preventScroll: true });
      matches[0].dispatchEvent(new MouseEvent("click", { bubbles: true, cancelable: true, detail: 0 }));
    } else {
      const next = matches[(matches.indexOf(e.target) + 1) % matches.length];
      next.focus({ preventScroll: true });
    }
  };

  // The definitions are read again as one of the row's menus opens.
  #onBeforeToggle = (e) => {
    if (e.newState === "open" && e.target.classList?.contains("ui-menu")) this.refresh();
  };

  #onToggle = () => {
    if (!this.#accessKeysOpen) return;
    if (!this.element.querySelector(".ui-menu:popover-open")) {
      this.#accessKeysOpen = false;
      this.#showAccessKeys(false);
    }
  };

  #showAccessKeys(on) {
    if (on) this.element.dataset.accessKeys = "";
    else delete this.element.dataset.accessKeys;
  }

  // Alt with a shown title's access key opens its menu on the first item.
  handleAccessKey(e) {
    if (!e.altKey || e.ctrlKey || e.metaKey || e.shiftKey || e.defaultPrevented || this.element.hidden) return false;
    if (isMac && isTextField(e.target)) return false;
    const title = this.element.items.find((t) => {
      const r = this.#recordOf(t);
      return r?.key && t.getAttribute("aria-disabled") !== "true" && keyMatches(r.key, e);
    });
    if (!title) return false;
    e.preventDefault();
    this.#accessKeysOpen = true;
    this.#showAccessKeys(true);
    this.element.focusItem(title);
    openMenu(this.#recordOf(title).sub, title, { focus: "first" });
    return true;
  }

  // An item gesture: the first shown and enabled item whose gesture the key is, menus included.
  handleGesture(e) {
    if (e.defaultPrevented) return false;
    const found = this.#findGesture([...this.#menu, { items: this.#appMenu }], e);
    if (!found) return false;
    if (isTextField(e.target) && isTextEditingKey(found.gesture)) return false;
    e.preventDefault();
    this.choose(found);
    return true;
  }

  #findGesture(items, e) {
    for (const def of items) {
      if (isSeparator(def) || !visible(def) || !enabled(def)) continue;
      if (def.items) {
        const found = this.#findGesture(def.items, e);
        if (found) return found;
      } else if (def.gesture && gestureMatches(def.gesture, e)) return def;
    }
    return null;
  }

  noteFocus(target) {
    if (!this.element.contains(target)) this.#lastOutside = target;
  }

  noteAlt(down) {
    if (!this.#accessKeysOpen) this.#showAccessKeys(down);
  }
}

/** Builds a menu row from `menu` with options { appMenu, label, helpHeader, moreLabel }. */
export function createMenuRow(menu, options) {
  return new MenuRow(menu, options);
}

// --- Document listeners ---------------------------------------------------------------------------

let listening = false;

function startDocumentListeners() {
  if (listening) return;
  listening = true;
  // Bubbling: after the focused element (and the menus' own keys) have had the key.
  document.addEventListener("keydown", (e) => {
    if (e.key === "Alt") for (const row of rows) row.noteAlt(true);
    for (const row of rows) if (row.handleAccessKey(e)) return;
    for (const row of rows) if (row.handleGesture(e)) return;
  });
  document.addEventListener("keyup", (e) => {
    if (e.key === "Alt") for (const row of rows) row.noteAlt(false);
  });
  addEventListener("blur", () => {
    for (const row of rows) row.noteAlt(false);
  });
  document.addEventListener("focusin", (e) => {
    for (const row of rows) row.noteFocus(e.target);
  });
}
