// TreeView (0035, 0042, 0048): <ui-tree-view>, nested lists of .ui-tree-view-item elements (markup
// in tree-view.css). No native element is an outline with selection and expand and collapse, so it
// is a custom element (0046), on the ARIA tree pattern: the element is a tree, each item a treeitem
// with aria-level, aria-selected and (with children) aria-expanded, named by its content pill;
// nested lists are groups. The script adds the caret (an Icon, decoration) at each row's start, sets
// --ui-tree-view-level for the indent, and marks items whose node lines are on (ui-tree-view-lined).
//
// Options, on the tree and overridden on an item for itself and its descendants:
//   expand-on / data-expand-on, collapse-on / data-collapse-on: which clicks on a row expand or
//     collapse it, "single", "double", "single double" or "none"; both default to "double".
//   show-lines="false" / data-show-lines="true|false": the node lines (on by default).
//   accent-flag / data-accent-flag="true|false": the accent flag (tree-view.css, list-box.css).
//
// Keys (0035; docs/web/keyboard.md): one Tab stop with a roving tabindex, landing on the selected
// item, else the first. Up and Down move between visible rows, Home and End to the first and last;
// Right expands a collapsed item, then moves to its first child; Left collapses an expanded item,
// then moves to its parent (Left and Right swap under dir="rtl"); Enter toggles. Moving selects
// (single selection). Disabled items (aria-disabled="true", with their subtree) are skipped.
//
// Clicks (0035): a click anywhere on a row selects its item. The caret toggles on each of its own
// clicks whatever the options, and does not select. Other clicks toggle as the options say; a
// double click toggles once even when single clicks toggle too (the click that toggled ends the
// gesture). Rows without children ignore the options and leave their double clicks to the app.
// Clicks with Ctrl, Shift or Cmd only change the selection (Ctrl or Cmd toggles it); clicks on
// interactive content in a row (a button in a header) are left to it. A click that toggles is
// handled (preventDefault, also on the dblclick that follows it).
//
// Events: `change` on the tree when the selection changes; `toggle` on an item (bubbling) when it
// expands or collapses.
import { icon } from "./icon.js";

const ITEM = ".ui-tree-view-item";
const ROW = ".ui-tree-view-row";
const CARET = "ui-tree-view-caret";
const CONTENT = ".ui-tree-view-content";
const LINED = "ui-tree-view-lined";
const INTERACTIVE = "a[href], button, input, select, textarea, [contenteditable], [role='button'], [role='link'], [tabindex]";
const CLICKS = new Set(["single", "double"]);
const DEFAULT_CLICK = "double";
let nextId = 0;

function setAttr(el, name, value) {
  if (value === null) {
    if (el.hasAttribute(name)) el.removeAttribute(name);
  } else if (el.getAttribute(name) !== value) el.setAttribute(name, value);
}

/** The click kinds in an option value: "single", "double", both, or none. */
const parseClicks = (value) => new Set(String(value).toLowerCase().split(/[\s,|]+/).filter((t) => CLICKS.has(t)));

export class UiTreeView extends HTMLElement {
  static observedAttributes = ["disabled", "show-lines"];

  #observer = new MutationObserver(() => this.#sync());
  #pressOnCaret = false;
  #pressHandled = false;
  #toggledByClick = false;
  #doubleHandled = false;

  connectedCallback() {
    this.addEventListener("keydown", this.#onKeyDown);
    this.addEventListener("pointerdown", this.#onPointerDown);
    this.addEventListener("click", this.#onClick);
    this.addEventListener("dblclick", this.#onDoubleClick);
    this.addEventListener("focusin", this.#onFocusIn);
    this.#observer.observe(this, {
      subtree: true, childList: true, attributes: true,
      attributeFilter: ["aria-expanded", "aria-selected", "aria-disabled", "data-show-lines", "class"],
    });
    this.#sync();
  }

  disconnectedCallback() {
    this.#observer.disconnect();
  }

  attributeChangedCallback() {
    if (this.isConnected) this.#sync();
  }

  /** Every item of this tree (not of a tree nested in it), in document order. */
  get items() {
    return [...this.querySelectorAll(ITEM)].filter((item) => item.closest("ui-tree-view") === this);
  }

  /** The items whose rows show: every ancestor item expanded. */
  get visibleItems() {
    return this.items.filter((item) => this.#visible(item));
  }

  get disabled() {
    return this.hasAttribute("disabled");
  }

  get selectedItem() {
    return this.items.find((item) => item.getAttribute("aria-selected") === "true") ?? null;
  }

  set selectedItem(item) {
    this.#select(item ?? null, false);
  }

  /** The item's child items. */
  childItems(item) {
    const group = item.querySelector(":scope > ul, :scope > ol, :scope > .ui-tree-view-group");
    return group ? [...group.children].filter((child) => child.matches(ITEM)) : [];
  }

  /** The item's parent item, or null at the top level. */
  parentItem(item) {
    const parent = item.parentElement?.closest(ITEM);
    return parent && this.contains(parent) && parent.closest("ui-tree-view") === this ? parent : null;
  }

  /** Expands (true), collapses (false) or toggles (undefined) an item with children. */
  toggle(item, expanded) {
    if (!this.childItems(item).length) return false;
    const now = item.getAttribute("aria-expanded") === "true";
    const next = expanded ?? !now;
    if (next === now) return false;
    item.setAttribute("aria-expanded", String(next));
    // Focus inside a collapsing subtree moves to the item, so it is never lost on a hidden row.
    if (!next && item !== document.activeElement && item.contains(document.activeElement)) this.#focus(item);
    this.#sync();
    item.dispatchEvent(new Event("toggle", { bubbles: true }));
    return true;
  }

  #visible(item) {
    for (let p = this.parentItem(item); p; p = this.parentItem(p)) if (p.getAttribute("aria-expanded") !== "true") return false;
    return true;
  }

  #enabled(item) {
    if (this.disabled) return false;
    const off = item.closest("[aria-disabled='true']");
    return !off || !this.contains(off) || off === this;
  }

  #level(item) {
    let level = 0;
    for (let p = this.parentItem(item); p; p = this.parentItem(p)) level++;
    return level;
  }

  // The option value nearest the item: its own or an ancestor item's data attribute, else the tree's.
  #option(item, data, attribute) {
    const set = item.closest(`[${data}]`);
    if (set && this.contains(set)) return set.getAttribute(data);
    return this.getAttribute(attribute);
  }

  #linesOn(item) {
    return (this.#option(item, "data-show-lines", "show-lines") ?? "true") !== "false";
  }

  #clicks(item, attribute) {
    return parseClicks(this.#option(item, `data-${attribute}`, attribute) ?? DEFAULT_CLICK);
  }

  // Roles, levels, carets, lines and the roving tabindex, from the markup.
  #sync() {
    setAttr(this, "role", "tree");
    setAttr(this, "aria-disabled", this.disabled ? "true" : null);
    for (const list of this.querySelectorAll("ul, ol")) {
      if (list.closest("ui-tree-view") !== this) continue;
      setAttr(list, "role", list.parentElement === this ? "none" : "group");
    }
    const items = this.items;
    for (const item of items) {
      const level = this.#level(item);
      setAttr(item, "role", "treeitem");
      setAttr(item, "aria-level", String(level + 1));
      if (item.style.getPropertyValue("--ui-tree-view-level") !== String(level)) item.style.setProperty("--ui-tree-view-level", String(level));
      setAttr(item, "aria-selected", item.getAttribute("aria-selected") === "true" ? "true" : "false");
      setAttr(item, "aria-expanded", this.childItems(item).length ? (item.getAttribute("aria-expanded") === "true" ? "true" : "false") : null);
      if (item.classList.contains(LINED) !== this.#linesOn(item)) item.classList.toggle(LINED);
      const row = item.querySelector(`:scope > ${ROW}`);
      if (row && !row.querySelector(`:scope > .${CARET}`)) {
        const caret = document.createElement("span");
        caret.className = CARET;
        caret.setAttribute("aria-hidden", "true");
        caret.append(icon("caret-right"));
        row.prepend(caret);
      }
      const content = row?.querySelector(CONTENT);
      if (content) {
        if (!content.id) content.id = `ui-tree-view-content-${++nextId}`;
        setAttr(item, "aria-labelledby", content.id);
      }
    }
    const usable = (item) => this.#visible(item) && this.#enabled(item);
    const focused = items.find((item) => item === document.activeElement && usable(item));
    let selected = this.selectedItem;
    while (selected && !this.#visible(selected)) selected = this.parentItem(selected);
    const active = focused ?? (selected && usable(selected) ? selected : null) ?? items.find(usable);
    for (const item of items) setAttr(item, "tabindex", item === active ? "0" : "-1");
  }

  #select(item, notify = true) {
    let changed = false;
    for (const other of this.items) {
      const on = other === item;
      if ((other.getAttribute("aria-selected") === "true") === on) continue;
      other.setAttribute("aria-selected", String(on));
      changed = true;
    }
    this.#sync();
    if (changed && notify) this.dispatchEvent(new Event("change", { bubbles: true }));
  }

  #focus(item) {
    for (const other of this.items) setAttr(other, "tabindex", other === item ? "0" : "-1");
    item.focus({ preventScroll: true });
    (item.querySelector(`:scope > ${ROW}`) ?? item).scrollIntoView({ block: "nearest", inline: "nearest" });
  }

  #move(item) {
    this.#focus(item);
    this.#select(item);
  }

  #itemFrom(event) {
    const item = event.target.closest?.(ITEM);
    return item && item.closest("ui-tree-view") === this ? item : null;
  }

  // The item whose row the event is in (null outside every row).
  #rowItemFrom(event) {
    const row = event.target.closest?.(ROW);
    const item = row?.parentElement;
    return item?.matches(ITEM) && item.closest("ui-tree-view") === this ? item : null;
  }

  #onCaret(event) {
    return !!event.target.closest?.(`.${CARET}`);
  }

  // Interactive content in the row other than the item: it handles its own press.
  #onContent(event, item) {
    const hit = event.target.closest?.(INTERACTIVE);
    return !!hit && hit !== item && item.contains(hit) && hit.closest(ITEM) === item;
  }

  #onPointerDown = (event) => {
    const item = this.#rowItemFrom(event);
    this.#pressOnCaret = false;
    this.#pressHandled = false;
    if (!item) return;
    if (!this.#enabled(item)) {
      event.preventDefault(); // disabled items take no focus
      return;
    }
    if (this.#onCaret(event)) {
      this.#pressOnCaret = true;
      event.preventDefault(); // the caret is not a focus target
      return;
    }
    this.#pressHandled = this.#onContent(event, item);
  };

  #onFocusIn = (event) => {
    const item = this.#itemFrom(event);
    if (item && event.target === item && this.#enabled(item)) {
      for (const other of this.items) setAttr(other, "tabindex", other === item ? "0" : "-1");
    }
  };

  // Toggles the item when the option for its state includes this click kind.
  #toggleBy(item, kind) {
    if (!this.childItems(item).length) return false;
    const expanded = item.getAttribute("aria-expanded") === "true";
    if (!this.#clicks(item, expanded ? "collapse-on" : "expand-on").has(kind)) return false;
    return this.toggle(item, !expanded);
  }

  #onClick = (event) => {
    const item = this.#rowItemFrom(event);
    if (!item) return;
    const odd = event.detail % 2 === 1 || event.detail === 0;
    if (odd) this.#toggledByClick = false;
    this.#doubleHandled = false;
    if (!this.#enabled(item)) return;
    // The caret toggles on each of its own clicks and leaves the selection alone.
    if (this.#pressOnCaret || this.#onCaret(event)) {
      this.#pressOnCaret = false;
      if (this.toggle(item)) event.preventDefault();
      return;
    }
    if (this.#pressHandled || this.#onContent(event, item)) {
      this.#pressHandled = false;
      return;
    }
    // Modifier clicks change the selection only.
    if (event.ctrlKey || event.metaKey) {
      this.#select(item.getAttribute("aria-selected") === "true" ? null : item);
      this.#focus(item);
      return;
    }
    this.#select(item);
    if (document.activeElement !== item) this.#focus(item);
    if (event.shiftKey) return;
    if (odd) {
      if (this.#toggleBy(item, "single")) {
        this.#toggledByClick = true;
        event.preventDefault();
      }
    } else if (this.#toggledByClick) {
      // The first click of this double click toggled: the gesture is done.
      this.#toggledByClick = false;
      this.#doubleHandled = true;
      event.preventDefault();
    } else if (this.#toggleBy(item, "double")) {
      this.#doubleHandled = true;
      event.preventDefault();
    }
  };

  #onDoubleClick = (event) => {
    if (this.#doubleHandled) event.preventDefault();
    this.#doubleHandled = false;
  };

  #onKeyDown = (event) => {
    const item = this.#itemFrom(event);
    // Keys typed into content in a row (a field in a header) are its own.
    if (!item || event.target !== item || this.disabled || event.altKey) return;
    const visible = this.visibleItems.filter((i) => this.#enabled(i));
    if (!visible.length) return;
    const at = visible.indexOf(item);
    const rtl = getComputedStyle(this).direction === "rtl";
    let key = event.key;
    if (rtl && key === "ArrowLeft") key = "ArrowRight";
    else if (rtl && key === "ArrowRight") key = "ArrowLeft";
    const expanded = item.getAttribute("aria-expanded");

    switch (key) {
      case "ArrowDown":
        this.#move(visible[Math.min(at + 1, visible.length - 1)]);
        break;
      case "ArrowUp":
        this.#move(visible[Math.max(at - 1, 0)]);
        break;
      case "Home":
        this.#move(visible[0]);
        break;
      case "End":
        this.#move(visible.at(-1));
        break;
      case "ArrowRight":
        if (expanded === "false") this.toggle(item, true);
        else if (expanded === "true") {
          const first = this.childItems(item).find((c) => this.#enabled(c));
          if (first) this.#move(first);
        }
        break;
      case "ArrowLeft":
        if (expanded === "true") this.toggle(item, false);
        else {
          const parent = this.parentItem(item);
          if (parent && this.#enabled(parent)) this.#move(parent);
        }
        break;
      case "Enter":
        this.toggle(item);
        break;
      case " ":
        this.#select(item);
        break;
      default:
        return;
    }
    event.preventDefault();
  };
}

if (!customElements.get("ui-tree-view")) customElements.define("ui-tree-view", UiTreeView);
