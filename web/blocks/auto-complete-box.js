// AutoCompleteBox (0029): <ui-auto-complete-box>, a TextInput field whose suggestions open in the
// ComboBox drop-down: a squircle popover (the flyout's placement and top layer) that clips a
// <ui-list-box>. The input is the combobox of the ARIA pattern (list autocomplete): focus stays in
// it, aria-activedescendant names the highlighted suggestion, which is aria-selected and takes the
// list item's accent fill. Markup (auto-complete-box.css):
//   <ui-auto-complete-box class="ui-field ui-auto-complete-box ui-squircle ui-focus-ring-within">
//     <input class="ui-text-input" aria-label="City" placeholder="City">
//     <datalist><option value="Amsterdam"></option>…</datalist>
//   </ui-auto-complete-box>
// The suggestions come from the <datalist> (its options' values) or from the `items` property
// (an array of strings), which replaces it. The datalist is never connected to the input, so the
// browser shows no suggestions of its own.
//
// Attributes:
//   filter="starts-with" (default) | "contains" | "none"   how typed text matches (case-insensitive)
//   min-prefix-length="1"                                  characters typed before suggestions open
//   accent-flag                                            the accent flag (0042) on the suggestions
// Behaviour as Avalonia's: suggestions open as the user types while some match; Down and Up move
// the highlight (Up from the first returns to the typed text), Enter chooses the highlighted
// suggestion into the input (without one, Enter acts as in any input), Escape closes, Tab and
// leaving the field close. A click on a suggestion chooses it. Choosing fires input and change on
// the input. Down opens the suggestions when closed. Focus never leaves the input.
import "./text-input.js";
import "./list-box.js";
import { openFlyout, closeFlyout } from "./flyout.js";

let nextId = 0;

export class UiAutoCompleteBox extends HTMLElement {
  static observedAttributes = ["filter", "min-prefix-length", "accent-flag"];

  #items = null;
  #popup = null;
  #list = null;
  #matches = [];
  #active = -1;
  #choosing = false;

  get input() {
    return this.querySelector("input");
  }

  /** The suggestions: the `items` assigned, else the datalist's options. */
  get items() {
    return this.#items ?? [...(this.querySelector("datalist")?.options ?? [])].map((o) => o.value);
  }

  set items(value) {
    this.#items = value ? [...value].map(String) : null;
  }

  get filter() {
    const f = this.getAttribute("filter");
    return f === "contains" || f === "none" ? f : "starts-with";
  }

  get minPrefixLength() {
    const n = Number(this.getAttribute("min-prefix-length") ?? 1);
    return Number.isFinite(n) && n >= 0 ? n : 1;
  }

  get open() {
    return this.#popup?.matches(":popover-open") ?? false;
  }

  connectedCallback() {
    const id = `ui-auto-complete-${nextId++}`;
    const input = this.input;
    this.#popup = document.createElement("div");
    this.#popup.className = "ui-flyout ui-combo-box-popup ui-auto-complete-popup ui-squircle";
    this.#popup.popover = "manual";
    this.#list = document.createElement("ui-list-box");
    this.#list.id = `${id}-list`;
    this.#list.className = "ui-scroll";
    this.#list.toggleAttribute("accent-flag", this.hasAttribute("accent-flag"));
    this.#popup.append(this.#list);
    this.append(this.#popup);
    if (input) {
      input.setAttribute("role", "combobox");
      input.setAttribute("aria-autocomplete", "list");
      input.setAttribute("aria-haspopup", "listbox");
      input.setAttribute("aria-expanded", "false");
      input.setAttribute("aria-controls", this.#list.id);
      input.setAttribute("autocomplete", "off");
    }
    this.addEventListener("input", this.#onInput);
    this.addEventListener("keydown", this.#onKeyDown);
    this.addEventListener("focusout", this.#onFocusOut);
    this.#popup.addEventListener("click", this.#onClick, true);
    this.#popup.addEventListener("toggle", this.#onToggle);
    document.addEventListener("pointerdown", this.#onOutside, true);
  }

  attributeChangedCallback(name) {
    if (name === "accent-flag") this.#list?.toggleAttribute("accent-flag", this.hasAttribute("accent-flag"));
  }

  disconnectedCallback() {
    document.removeEventListener("pointerdown", this.#onOutside, true);
    this.removeEventListener("input", this.#onInput);
    this.removeEventListener("keydown", this.#onKeyDown);
    this.removeEventListener("focusout", this.#onFocusOut);
    this.#popup?.remove();
    this.#popup = null;
  }

  /** Opens the suggestions for the input's text now (with the prefix length ignored), if any match. */
  show() {
    this.#update(true);
  }

  /** Closes the suggestions. */
  close() {
    if (!this.#popup) return;
    this.#active = -1;
    if (this.open) closeFlyout(this.#popup, { restoreFocus: false });
    this.#sync();
  }

  #matching(text) {
    const typed = text.toLocaleLowerCase();
    return this.items.filter((item) => {
      const s = item.toLocaleLowerCase();
      return this.filter === "none" || (this.filter === "contains" ? s.includes(typed) : s.startsWith(typed));
    });
  }

  #update(force = false) {
    const input = this.input;
    if (!input || input.disabled || input.readOnly) return this.close();
    const text = input.value;
    const matches = !force && text.length < this.minPrefixLength ? [] : this.#matching(text);
    if (!matches.length) return this.close();
    this.#matches = matches;
    this.#active = -1;
    this.#list.replaceChildren(...matches.map((item, i) => {
      const li = document.createElement("div");
      li.className = "ui-list-item ui-squircle ui-focus-ring";
      li.id = `${this.#list.id}-${i}`;
      li.tabIndex = -1;
      li.textContent = item;
      return li;
    }));
    const popup = this.#popup;
    const width = `${this.getBoundingClientRect().width}px`;
    popup.style.inlineSize = width;
    popup.style.minInlineSize = width;
    openFlyout(popup, { anchor: this, placement: "block-end-start", focus: false });
    this.#sync();
  }

  // The combobox state on the input and the highlighted suggestion.
  #sync() {
    const input = this.input;
    if (!input) return;
    input.setAttribute("aria-expanded", String(this.open));
    const items = [...this.#list.querySelectorAll(".ui-list-item")];
    items.forEach((item, i) => item.setAttribute("aria-selected", String(i === this.#active)));
    const active = items[this.#active];
    if (active && this.open) {
      input.setAttribute("aria-activedescendant", active.id);
      active.scrollIntoView({ block: "nearest" });
    } else input.removeAttribute("aria-activedescendant");
  }

  #move(step) {
    const last = this.#matches.length - 1;
    this.#active = Math.max(-1, Math.min(last, this.#active + step));
    this.#sync();
  }

  #choose(index) {
    const input = this.input;
    const item = this.#matches[index];
    if (!input || item === undefined) return;
    this.#choosing = true;
    input.value = item;
    input.dispatchEvent(new Event("input", { bubbles: true }));
    input.dispatchEvent(new Event("change", { bubbles: true }));
    this.#choosing = false;
    this.close();
    input.setSelectionRange(item.length, item.length);
  }

  #onInput = (e) => {
    if (this.#choosing || e.target !== this.input) return;
    this.#update();
  };

  #onKeyDown = (e) => {
    const input = this.input;
    if (e.target !== input || e.altKey || e.ctrlKey || e.metaKey) return;
    if (e.key === "ArrowDown") {
      e.preventDefault();
      if (this.open) this.#move(1);
      else this.show();
    } else if (e.key === "ArrowUp" && this.open) {
      e.preventDefault();
      this.#move(-1);
    } else if (e.key === "Enter" && this.open && this.#active >= 0) {
      e.preventDefault();
      this.#choose(this.#active);
    } else if (e.key === "Escape" && this.open) {
      e.preventDefault();
      e.stopPropagation();
      this.close();
    } else if (e.key === "Tab") {
      this.close();
    }
  };

  #onFocusOut = (e) => {
    if (e.relatedTarget && this.contains(e.relatedTarget)) return;
    this.close();
  };

  // A press outside the field and its suggestions closes them (the popover is manual).
  #onOutside = (e) => {
    if (this.open && !this.contains(e.target)) this.close();
  };

  #onClick = (e) => {
    const item = e.target.closest?.(".ui-list-item");
    if (!item) return;
    e.stopPropagation(); // the list's own click handling would move focus onto the item
    if (item.getAttribute("aria-disabled") === "true") return;
    this.#choose([...this.#list.querySelectorAll(".ui-list-item")].indexOf(item));
    this.input?.focus();
  };

  #onToggle = (e) => {
    if (e.newState === "closed") this.#sync();
  };
}

if (!customElements.get("ui-auto-complete-box")) customElements.define("ui-auto-complete-box", UiAutoCompleteBox);
