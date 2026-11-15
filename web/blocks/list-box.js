// ListBox (0024, 0042, 0048): <ui-list-box>, a list of .ui-list-item elements, in two flavours.
//
// Selection (the default): a listbox of options. The element takes role="listbox" (with
// aria-multiselectable when `multiple` is set) and each item role="option" with aria-selected.
// No native element carries the item design (a <select> cannot hold icons or the squircle items),
// so it is a custom element (0046). Keys (APG listbox, as the desktop ListBox): Up, Down, Home
// and End move focus, typing jumps to the next item starting with the typed text. Single: the
// selection follows focus; a click, Space or Enter selects. Multiple: a click, Space or Enter
// toggles an item; Shift with a click or an arrow selects the range from the last toggled item;
// Ctrl+A (Cmd+A on macOS) selects every item. It is form-associated: with `name` it submits the
// selected items' data-value (or text) like a <select>.
//
// Navigation (`navigation`): a native list of links or buttons, `<ui-list-box navigation><ul>
// <li><a class="ui-list-item">`, the current one marked aria-current="page" (as the sidebar, 0048).
// The element adds no role; the links keep theirs. Choosing an item (click, Enter) moves
// aria-current to it. Without script every link stays a Tab stop and works.
//
// Both flavours are one Tab stop with a roving tabindex: Tab lands on the selected (or current)
// item, else the first. Disabled items (aria-disabled="true", or disabled on a button) are skipped
// and take no focus. The element fires `change` when the selection or the current item changes.

import { isMac } from "../style/platform.js";

const ITEM = ".ui-list-item";
const TYPE_AHEAD_RESET = 500; // ms without typing before the typed text starts over (APG)
const NAV_KEYS = new Set(["ArrowDown", "ArrowUp", "Home", "End"]);

const marked = (item) => item.getAttribute("aria-selected") === "true"
  || (item.hasAttribute("aria-current") && item.getAttribute("aria-current") !== "false");

function setAttr(el, name, value) {
  if (value === null) {
    if (el.hasAttribute(name)) el.removeAttribute(name);
  } else if (el.getAttribute(name) !== value) el.setAttribute(name, value);
}

export class UiListBox extends HTMLElement {
  static formAssociated = true;
  static observedAttributes = ["multiple", "navigation", "disabled", "name"];

  #internals = this.attachInternals();
  #observer = new MutationObserver(() => this.#sync());
  #anchor = null;
  #typed = "";
  #typedAt = 0;
  #initial = [];
  #formDisabled = false;

  connectedCallback() {
    this.addEventListener("keydown", this.#onKeyDown);
    this.addEventListener("click", this.#onClick);
    this.addEventListener("pointerdown", this.#onPointerDown);
    this.addEventListener("focusin", this.#onFocusIn);
    this.#observer.observe(this, {
      subtree: true, childList: true, attributes: true,
      attributeFilter: ["aria-selected", "aria-current", "aria-disabled", "disabled", "class"],
    });
    this.#sync();
    this.#initial = this.selectedItems;
  }

  disconnectedCallback() {
    this.#observer.disconnect();
  }

  attributeChangedCallback() {
    if (this.isConnected) this.#sync();
  }

  /** The items of this list (not of a list nested in it), in order. */
  get items() {
    return [...this.querySelectorAll(ITEM)].filter((item) => item.closest("ui-list-box") === this);
  }

  get navigation() {
    return this.hasAttribute("navigation");
  }

  get multiple() {
    return this.hasAttribute("multiple") && !this.navigation;
  }

  get disabled() {
    return this.hasAttribute("disabled") || this.#formDisabled;
  }

  /** The selected items (selection flavour) or the current item (navigation flavour). */
  get selectedItems() {
    return this.items.filter(marked);
  }

  get selectedIndex() {
    return this.items.findIndex(marked);
  }

  set selectedIndex(index) {
    const item = this.items[index];
    this.#select(item ? [item] : [], false);
  }

  get name() {
    return this.getAttribute("name");
  }

  get form() {
    return this.#internals.form;
  }

  formResetCallback() {
    this.#select(this.#initial.filter((item) => this.contains(item)), false);
  }

  formDisabledCallback(disabled) {
    this.#formDisabled = disabled;
    this.#sync();
  }

  #enabled(item) {
    return !this.disabled && item.getAttribute("aria-disabled") !== "true" && !item.disabled;
  }

  // Roles, states, the roving tabindex and the form value, from the markup.
  #sync() {
    const items = this.items;
    const navigation = this.navigation;
    setAttr(this, "role", navigation ? null : "listbox");
    setAttr(this, "aria-multiselectable", this.multiple ? "true" : null);
    setAttr(this, "aria-disabled", this.disabled ? "true" : null);
    for (const item of items) {
      if (navigation) continue;
      setAttr(item, "role", "option");
      setAttr(item, "aria-selected", item.getAttribute("aria-selected") === "true" ? "true" : "false");
    }
    const focused = items.find((item) => item === document.activeElement && this.#enabled(item));
    const active = focused ?? items.find((item) => marked(item) && this.#enabled(item)) ?? items.find((item) => this.#enabled(item));
    for (const item of items) setAttr(item, "tabindex", item === active ? "0" : "-1");
    this.#updateFormValue();
  }

  #updateFormValue() {
    if (this.navigation) return this.#internals.setFormValue(null);
    const values = this.selectedItems.map((item) => item.dataset.value ?? item.textContent.trim());
    if (!this.multiple) return this.#internals.setFormValue(values[0] ?? null);
    const data = new FormData();
    if (this.name) for (const value of values) data.append(this.name, value);
    this.#internals.setFormValue(data);
  }

  // Marks exactly `chosen` (or adds to the selection) and fires change when anything changed.
  #select(chosen, notify = true) {
    let changed = false;
    for (const item of this.items) {
      const on = chosen.includes(item);
      if (marked(item) === on) continue;
      changed = true;
      if (this.navigation) setAttr(item, "aria-current", on ? "page" : null);
      else item.setAttribute("aria-selected", String(on));
    }
    this.#sync();
    if (changed && notify) this.dispatchEvent(new Event("change", { bubbles: true }));
  }

  #focus(item) {
    for (const other of this.items) setAttr(other, "tabindex", other === item ? "0" : "-1");
    item.focus();
    item.scrollIntoView({ block: "nearest", inline: "nearest" });
  }

  #range(from, to) {
    const enabled = this.items.filter((item) => this.#enabled(item));
    const a = enabled.indexOf(from), b = enabled.indexOf(to);
    if (a < 0 || b < 0) return [to];
    return enabled.slice(Math.min(a, b), Math.max(a, b) + 1);
  }

  #choose(item, { range = false } = {}) {
    if (this.navigation || !this.multiple) {
      this.#select([item]);
      this.#anchor = item;
    } else if (range && this.#anchor) {
      this.#select(this.#range(this.#anchor, item));
    } else {
      this.#select(marked(item) ? this.selectedItems.filter((i) => i !== item) : [...this.selectedItems, item]);
      this.#anchor = item;
    }
  }

  #itemFrom(event) {
    const item = event.target.closest?.(ITEM);
    return item && item.closest("ui-list-box") === this ? item : null;
  }

  #onPointerDown = (event) => {
    const item = this.#itemFrom(event);
    if (item && !this.#enabled(item)) event.preventDefault(); // disabled items take no focus
  };

  #onFocusIn = (event) => {
    const item = this.#itemFrom(event);
    if (item && this.#enabled(item)) for (const other of this.items) setAttr(other, "tabindex", other === item ? "0" : "-1");
  };

  #onClick = (event) => {
    const item = this.#itemFrom(event);
    if (!item) return;
    if (!this.#enabled(item)) {
      event.preventDefault();
      return;
    }
    this.#choose(item, { range: event.shiftKey });
    if (!this.navigation) item.focus();
  };

  #onKeyDown = (event) => {
    const item = this.#itemFrom(event);
    if (!item || this.disabled) return;
    const enabled = this.items.filter((i) => this.#enabled(i));
    if (!enabled.length) return;
    const command = isMac ? event.metaKey : event.ctrlKey;

    if (NAV_KEYS.has(event.key) && !event.altKey) {
      const at = enabled.indexOf(item);
      const target = event.key === "Home" ? enabled[0]
        : event.key === "End" ? enabled.at(-1)
        : event.key === "ArrowDown" ? enabled[Math.min(at + 1, enabled.length - 1)]
        : enabled[Math.max(at < 0 ? 0 : at - 1, 0)];
      event.preventDefault();
      this.#focus(target);
      if (this.multiple && event.shiftKey) {
        this.#anchor ??= item;
        this.#select(this.#range(this.#anchor, target));
      } else if (!this.navigation && !this.multiple) {
        this.#select([target]);
        this.#anchor = target;
      }
      return;
    }

    if (!this.navigation && (event.key === " " || event.key === "Enter")) {
      event.preventDefault();
      if (this.#enabled(item)) this.#choose(item, { range: event.shiftKey });
      return;
    }

    if (this.multiple && command && event.key.toLowerCase() === "a") {
      event.preventDefault();
      this.#select(enabled);
      return;
    }

    // Type-ahead: a printable key moves to the next item whose text starts with the typed text.
    if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey && event.key !== " ") {
      const now = event.timeStamp;
      this.#typed = (now - this.#typedAt > TYPE_AHEAD_RESET ? "" : this.#typed) + event.key.toLowerCase();
      this.#typedAt = now;
      const start = enabled.indexOf(item) + (this.#typed.length === 1 ? 1 : 0);
      const ordered = [...enabled.slice(start), ...enabled.slice(0, start)];
      const target = ordered.find((i) => i.textContent.trim().toLowerCase().startsWith(this.#typed));
      if (target) {
        event.preventDefault();
        this.#focus(target);
        if (!this.navigation && !this.multiple) this.#select([target]);
      }
    }
  };
}

if (!customElements.get("ui-list-box")) customElements.define("ui-list-box", UiListBox);
