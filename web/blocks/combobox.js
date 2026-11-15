// ComboBox (0028): <ui-combo-box>, a field with a drop-down list, in two forms (markup in
// combobox.css): not editable (the field is a <button>) and editable (the field is a .ui-field
// around an <input class="ui-text-input">). The drop-down is a flyout (flyout.js) holding a
// <ui-list-box> (list-box.js), so its items, selection and keys are the list's.
//
// ARIA (the APG select-only and editable combobox): the control (button or input) takes
// role="combobox", aria-haspopup="listbox", aria-expanded and aria-controls (the list); the
// editable input also aria-autocomplete="list". `label` gives the control and the list their name
// (otherwise name the control with aria-label or aria-labelledby).
//
// Not editable: focus moves into the open list, on the selected item (the first when none), and
// returns to the button when the list closes, as every flyout (keyboard.md). Keys on the button:
// Alt+Down, Alt+Up, F4, Enter, Space and a click open the list; Down, Up, Home and End change the
// selection without opening (Fluent's ComboBox does the same); typing a letter selects the next
// item starting with the typed text. In the open list: the list's keys (Up, Down, Home, End,
// type-ahead with selection following focus); Enter, Space or a click chooses and closes; Escape
// closes; Tab closes and moves on, so focus is never trapped.
//
// Editable (the APG editable combobox, as the AutoCompleteBox): focus stays in the text field
// while the list is open. aria-activedescendant on the input names the highlighted item, which
// shows as the selected item (aria-selected, the accent fill and the accent flag). Down or Up opens
// the list (Alt+Down and F4 too), highlighting the current item; while open Down and Up move the
// highlight, Enter chooses the highlighted item into the field (and, with none highlighted, closes
// the list), Escape and Alt+Up close it keeping the earlier selection, Tab and leaving the field
// close it. Typing continues with the list open: the text is the value and the highlight goes. A
// click on an item chooses it without taking focus from the field; a click on the caret zone
// toggles the list. Typing never opens the list. When the field's change fires (Enter or leaving
// it) a text equal to an item's, case aside, selects that item.
//
// Attributes: placeholder (text shown while nothing is selected), label, name (form value: the
// selected item's data-value or text, or the typed text), disabled, editable (set in markup, with
// the editable structure), accent-flag (the accent flag of 0042 on the drop-down's items).
// Properties: value (get and set), selectedIndex (get and set), open (get), show() and hide().
// A `change` event fires on the element when the selection or, editable, the text is committed
// changes it (and not for changes made by script).
import { icon } from "./icon.js";
import "./list-box.js";
import { syncField } from "./text-input.js";
import { openFlyout, closeFlyout } from "./flyout.js";

const TYPE_AHEAD_RESET = 500; // ms, as the list
let nextId = 0;

const textOf = (item) => item.dataset.text ?? item.textContent.trim();
const valueOf = (item) => item.dataset.value ?? textOf(item);

export class UiComboBox extends HTMLElement {
  static formAssociated = true;
  static observedAttributes = ["disabled", "placeholder", "accent-flag", "label"];

  #internals = this.attachInternals();
  #observer = new MutationObserver(() => this.#sync());
  #field = null;
  #control = null;
  #popup = null;
  #list = null;
  #valueEl = null;
  #wasOpen = false;
  #typed = "";
  #typedAt = 0;
  #initial = null;
  #committed = null; // editable: the chosen item; the list's selection also shows the highlight
  #formDisabled = false;
  #ready = false;

  connectedCallback() {
    if (!this.#ready) this.#build();
    if (!this.#ready) return;
    this.#observer.observe(this.#list, { subtree: true, childList: true, attributes: true, attributeFilter: ["aria-selected"] });
    this.#sync();
  }

  disconnectedCallback() {
    this.#observer.disconnect();
  }

  attributeChangedCallback() {
    if (this.#ready) this.#applyAttributes();
  }

  get editable() {
    return this.hasAttribute("editable");
  }

  get disabled() {
    return this.hasAttribute("disabled") || this.#formDisabled;
  }

  set disabled(on) {
    this.toggleAttribute("disabled", Boolean(on));
  }

  get open() {
    return Boolean(this.#popup?.matches(":popover-open"));
  }

  get name() {
    return this.getAttribute("name");
  }

  get form() {
    return this.#internals.form;
  }

  /** The items of the drop-down. */
  get items() {
    return this.#list?.items ?? [];
  }

  get selectedIndex() {
    if (this.editable) return this.items.indexOf(this.#committed);
    return this.#list?.selectedIndex ?? -1;
  }

  set selectedIndex(index) {
    if (!this.#ready) return;
    this.#select(this.items[index] ?? null);
  }

  /** The selected item's data-value (or text); editable, the text. */
  get value() {
    if (this.editable) return this.#control?.value ?? "";
    const item = this.#selected();
    return item ? valueOf(item) : "";
  }

  set value(value) {
    if (!this.#ready) return;
    const item = this.items.find((i) => valueOf(i) === String(value));
    if (item || !this.editable) return this.#select(item ?? null);
    this.#committed = null;
    this.#showHighlight(null);
    this.#control.value = String(value);
    syncField(this.#control);
    this.#sync();
  }

  show() {
    if (this.disabled || this.open || !this.#ready) return;
    this.#popup.style.minInlineSize = `${this.#field.getBoundingClientRect().width}px`;
    const enabled = this.#enabled();
    const target = this.#selected() && enabled.includes(this.#selected()) ? this.#selected() : enabled[0];
    if (this.editable) {
      // Focus stays in the text; the highlight starts on the chosen item, if any.
      openFlyout(this.#popup, { anchor: this.#field, opener: this.#control, placement: "block-end-start", focus: false });
      this.#showHighlight(this.#selected() ?? null);
      return;
    }
    openFlyout(this.#popup, { anchor: this.#field, opener: this.#control, placement: "block-end-start", focus: target ?? false });
    target?.scrollIntoView({ block: "nearest" });
  }

  hide() {
    if (this.#ready) closeFlyout(this.#popup);
  }

  formResetCallback() {
    if (!this.#ready) return;
    this.#committed = this.#initial;
    this.#list.selectedIndex = this.items.indexOf(this.#initial);
    if (this.editable && !this.#initial) this.#control.value = "";
    this.#sync();
  }

  formDisabledCallback(disabled) {
    this.#formDisabled = disabled;
    if (this.#ready) this.#applyAttributes();
  }

  #build() {
    this.#field = this.querySelector(".ui-combo-box-field");
    this.#popup = this.querySelector(".ui-combo-box-popup");
    this.#list = this.#popup?.querySelector("ui-list-box");
    if (!this.#field || !this.#list) return;
    this.#control = this.editable ? this.#field.querySelector(".ui-text-input") : this.#field;
    this.#valueEl = this.#field.querySelector(".ui-combo-box-value");
    if (!this.#control) return;
    this.#ready = true;

    const caret = this.#field.querySelector(".ui-combo-box-caret");
    if (caret && !caret.firstElementChild) caret.append(icon("caret-down"));
    this.#list.id ||= `ui-combo-box-list-${nextId++}`;
    const control = this.#control;
    control.setAttribute("role", "combobox");
    control.setAttribute("aria-haspopup", "listbox");
    control.setAttribute("aria-controls", this.#list.id);
    if (!control.hasAttribute("aria-expanded")) control.setAttribute("aria-expanded", "false");
    if (this.editable) control.setAttribute("aria-autocomplete", "list");
    this.#applyAttributes();
    this.#initial = this.#list.selectedItems[0] ?? null;
    this.#committed = this.#initial;

    // The press that light-dismisses an open list must not open it again with its click.
    this.#field.addEventListener("pointerdown", () => { this.#wasOpen = this.open; }, true);
    this.#field.addEventListener("click", this.#onFieldClick);
    this.addEventListener("keydown", this.#onKeyDown);
    this.#popup.addEventListener("keydown", this.#onPopupKeyDown);
    this.#popup.addEventListener("click", this.#onPopupClick);
    this.#popup.addEventListener("change", this.#onListChange);
    this.#popup.addEventListener("toggle", this.#onToggle);
    if (this.editable) {
      control.addEventListener("input", this.#onInput);
      control.addEventListener("change", this.#onTextChange);
      control.addEventListener("focusout", this.#onFocusOut);
      // Focus stays in the text: a press in the list takes none, and a click chooses (capture, so
      // the list does not move focus onto the item).
      this.#popup.addEventListener("mousedown", (e) => e.preventDefault());
      this.#popup.addEventListener("click", this.#onEditableClick, true);
    }
  }

  #applyAttributes() {
    const control = this.#control;
    const label = this.getAttribute("label");
    if (label) {
      if (!control.hasAttribute("aria-labelledby")) control.setAttribute("aria-label", label);
      this.#list.setAttribute("aria-label", label);
    }
    control.disabled = this.disabled;
    this.#list.toggleAttribute("accent-flag", this.hasAttribute("accent-flag"));
    if (this.editable) control.placeholder = this.getAttribute("placeholder") ?? "";
    if (this.disabled) this.hide();
    this.#sync();
  }

  #enabled() {
    return this.items.filter((i) => i.getAttribute("aria-disabled") !== "true" && !i.disabled);
  }

  // The chosen item: the editable form's own record (the list's selection also shows the
  // highlight), the list's selection otherwise.
  #selected() {
    return this.editable ? this.#committed : (this.#list?.selectedItems[0] ?? null);
  }

  // Shows the selection in the field and keeps the form value.
  #sync() {
    if (!this.#ready) return;
    const item = this.#selected();
    if (this.editable) {
      if (item && this.#control.value !== textOf(item)) {
        this.#control.value = textOf(item);
        syncField(this.#control);
      }
    } else if (this.#valueEl) {
      const placeholder = this.getAttribute("placeholder") ?? "";
      if (item) this.#valueEl.replaceChildren(...[...item.childNodes].map((n) => n.cloneNode(true)));
      else this.#valueEl.textContent = placeholder;
      this.#valueEl.toggleAttribute("data-placeholder", !item);
    }
    this.#internals.setFormValue(this.value || null);
  }

  // Marks `item` (or nothing) as the selection, without a change event.
  #select(item) {
    this.#committed = item;
    this.#list.selectedIndex = item ? this.items.indexOf(item) : -1;
    this.#sync();
  }

  #choose(item) {
    if (!item || item === this.#selected()) return;
    this.#select(item);
    this.dispatchEvent(new Event("change", { bubbles: true }));
  }

  #step(delta) {
    const enabled = this.#enabled();
    if (!enabled.length) return;
    const at = enabled.indexOf(this.#selected());
    const target = delta === "first" ? enabled[0]
      : delta === "last" ? enabled.at(-1)
      : enabled[Math.max(0, Math.min(enabled.length - 1, at < 0 ? (delta > 0 ? 0 : enabled.length - 1) : at + delta))];
    this.#choose(target);
  }

  #onFieldClick = (event) => {
    if (this.disabled) return;
    if (!this.editable || event.target.closest(".ui-combo-box-caret")) {
      if (this.#wasOpen) this.#wasOpen = false;
      else if (this.open) this.hide();
      else this.show();
    }
  };

  #onKeyDown = (event) => {
    if (event.target !== this.#control || this.disabled || event.ctrlKey || event.metaKey) return;
    if (this.editable) return this.#onEditableKey(event);
    const { key } = event;
    if ((event.altKey && (key === "ArrowDown" || key === "ArrowUp")) || key === "F4") {
      event.preventDefault();
      this.show();
      return;
    }
    if (event.altKey) return;
    if (key === "ArrowDown" || key === "ArrowUp") {
      event.preventDefault();
      this.#step(key === "ArrowDown" ? 1 : -1);
    } else if (!this.editable && (key === "Home" || key === "End")) {
      event.preventDefault();
      this.#step(key === "Home" ? "first" : "last");
    } else if (!this.editable && key.length === 1 && key !== " ") {
      const now = event.timeStamp;
      this.#typed = (now - this.#typedAt > TYPE_AHEAD_RESET ? "" : this.#typed) + key.toLowerCase();
      this.#typedAt = now;
      const enabled = this.#enabled();
      const start = enabled.indexOf(this.#selected()) + (this.#typed.length === 1 ? 1 : 0);
      const ordered = [...enabled.slice(start), ...enabled.slice(0, start)];
      const target = ordered.find((i) => textOf(i).toLowerCase().startsWith(this.#typed));
      if (target) {
        event.preventDefault();
        this.#choose(target);
      }
    }
  };

  // The editable form's keys (the APG editable combobox): focus stays in the text, the highlight
  // moves with aria-activedescendant, Enter chooses it.
  #onEditableKey(event) {
    const { key } = event;
    const open = this.open;
    if (event.altKey) {
      if (key === "ArrowDown" && !open) {
        event.preventDefault();
        this.show();
      } else if (key === "ArrowUp" && open) {
        event.preventDefault();
        this.hide();
      }
      return;
    }
    if (key === "F4") {
      event.preventDefault();
      if (open) this.hide();
      else this.show();
    } else if (key === "ArrowDown" || key === "ArrowUp") {
      event.preventDefault();
      if (open) return this.#moveHighlight(key === "ArrowDown" ? 1 : -1);
      this.show();
      if (!this.#highlighted()) {
        const enabled = this.#enabled();
        this.#showHighlight(key === "ArrowDown" ? enabled[0] : enabled.at(-1));
      }
    } else if (key === "Enter" && open) {
      const item = this.#highlighted();
      if (item) {
        event.preventDefault();
        this.#commit(item);
      } else this.hide();
    } else if (key === "Escape" && open) {
      event.preventDefault();
      event.stopPropagation();
      this.hide();
    } else if (key === "Tab") {
      this.hide();
    }
  }

  // The highlighted item while the editable list is open: the list's selection.
  #highlighted() {
    return this.open ? (this.#list.selectedItems[0] ?? null) : null;
  }

  // Highlights `item` (or none): it takes the selected look, and the input names it.
  #showHighlight(item) {
    this.#list.selectedIndex = item ? this.items.indexOf(item) : -1;
    if (item && this.open) {
      item.id ||= `${this.#list.id}-${this.items.indexOf(item)}`;
      this.#control.setAttribute("aria-activedescendant", item.id);
      item.scrollIntoView({ block: "nearest" });
    } else this.#control.removeAttribute("aria-activedescendant");
  }

  #moveHighlight(delta) {
    const enabled = this.#enabled();
    if (!enabled.length) return;
    const at = enabled.indexOf(this.#highlighted());
    const to = at < 0 ? (delta > 0 ? 0 : enabled.length - 1) : Math.max(0, Math.min(enabled.length - 1, at + delta));
    this.#showHighlight(enabled[to]);
  }

  // Chooses `item` into the text (change when it differs), closes the list; focus stays in the text.
  #commit(item) {
    const changed = item !== this.#committed;
    this.#select(item);
    this.#control.select();
    this.hide();
    if (changed) this.dispatchEvent(new Event("change", { bubbles: true }));
  }

  #onEditableClick = (event) => {
    const item = event.target.closest(".ui-list-item");
    if (!item) return;
    event.stopPropagation(); // the list's own click handling would move focus onto the item
    if (item.getAttribute("aria-disabled") === "true") return;
    this.#commit(item);
    this.#control.focus();
  };

  // Closed (by any means): the highlight goes, the chosen item shows again.
  #onToggle = (event) => {
    if (event.newState !== "closed" || !this.editable) return;
    this.#list.selectedIndex = this.items.indexOf(this.#committed);
    this.#control.removeAttribute("aria-activedescendant");
  };

  #onFocusOut = (event) => {
    if (!event.relatedTarget || !this.#popup.contains(event.relatedTarget)) this.hide();
  };

  #onPopupKeyDown = (event) => {
    if (event.key === "Tab") {
      closeFlyout(this.#popup, { restoreFocus: true });
    } else if ((event.key === "Enter" || event.key === " ") && event.target.closest(".ui-list-item")) {
      // After the list has chosen the item.
      closeFlyout(this.#popup);
    }
  };

  #onPopupClick = (event) => {
    const item = event.target.closest(".ui-list-item");
    if (item && item.getAttribute("aria-disabled") !== "true") closeFlyout(this.#popup);
  };

  // The list's change becomes the ComboBox's own.
  #onListChange = (event) => {
    if (event.target !== this.#list) return;
    event.stopPropagation();
    if (this.editable) return;
    this.#sync();
    this.dispatchEvent(new Event("change", { bubbles: true }));
  };

  // Typing makes the text the value: the selection goes unless the text still is the item's.
  #onInput = () => {
    const item = this.#committed;
    if (item && textOf(item) !== this.#control.value) this.#committed = null;
    this.#showHighlight(this.open ? null : this.#committed);
    this.#sync();
  };

  // The text's change (Enter or leaving the field): an item of that text is selected.
  #onTextChange = (event) => {
    event.stopPropagation();
    const text = this.#control.value.trim().toLowerCase();
    const match = this.items.find((i) => textOf(i).toLowerCase() === text);
    if (match) this.#select(match);
    this.dispatchEvent(new Event("change", { bubbles: true }));
  };
}

if (!customElements.get("ui-combo-box")) customElements.define("ui-combo-box", UiComboBox);

/**
 * Builds a <ui-combo-box>. items: strings or { text, value, selected, disabled, icon }; options:
 * editable, placeholder, label, name, selected (an index), accentFlag, disabled, width (px).
 */
export function createComboBox(items, { editable = false, placeholder = "", label = "", name, selected, accentFlag = false, disabled = false, width } = {}) {
  const box = document.createElement("ui-combo-box");
  if (editable) box.setAttribute("editable", "");
  if (placeholder) box.setAttribute("placeholder", placeholder);
  if (label) box.setAttribute("label", label);
  if (name) box.setAttribute("name", name);
  if (accentFlag) box.setAttribute("accent-flag", "");
  if (disabled) box.setAttribute("disabled", "");
  if (width) box.style.inlineSize = `${width}px`;

  const caret = document.createElement("span");
  caret.className = "ui-combo-box-caret";
  caret.setAttribute("aria-hidden", "true");
  let field;
  if (editable) {
    field = document.createElement("span");
    field.className = "ui-field ui-combo-box-field ui-squircle ui-focus-ring-within";
    const input = document.createElement("input");
    input.className = "ui-text-input";
    input.type = "text";
    field.append(input, caret);
  } else {
    field = document.createElement("button");
    field.type = "button";
    field.className = "ui-field ui-combo-box-field ui-squircle ui-focus-ring";
    const value = document.createElement("span");
    value.className = "ui-combo-box-value";
    field.append(value, caret);
  }

  const popup = document.createElement("div");
  popup.className = "ui-flyout ui-combo-box-popup ui-squircle";
  popup.setAttribute("popover", "");
  const list = document.createElement("ui-list-box");
  list.className = "ui-squircle ui-scroll";
  items.forEach((entry, i) => {
    const o = typeof entry === "string" ? { text: entry } : entry;
    const item = document.createElement("div");
    item.className = "ui-list-item ui-squircle ui-focus-ring";
    if (o.value !== undefined) item.dataset.value = o.value;
    if (o.icon) item.append(icon(o.icon));
    item.append(o.text);
    if (o.selected || i === selected) item.setAttribute("aria-selected", "true");
    if (o.disabled) item.setAttribute("aria-disabled", "true");
    list.append(item);
  });
  popup.append(list);
  box.append(field, popup);
  return box;
}
