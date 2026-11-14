// Segmented control (0032, 0046, 0048): <ui-segmented-control>, one of a few options of a single
// setting, its segments the .ui-segmented-control-item children. No native element draws one
// row of exclusive options with a sliding pill, so it is a custom element.
//
// Roles: the element is a radiogroup (named by aria-label or aria-labelledby) and each segment a
// radio with aria-checked; aria-disabled marks a disabled control (from its disabled attribute or
// a disabled fieldset) or segment. An icon-only segment takes aria-label, and title for pointer
// users.
//
// A setting always has a value: when no segment is checked, the first enabled one is. A click
// selects its segment whatever the modifier keys. Keys (0032): one Tab stop, landing on the
// selected segment (roving tabindex); Left and Right, Up and Down select the previous or next
// enabled segment, Home and End the first and last; Left and Right swap under dir="rtl"; movement
// stops at the ends; the keys are always handled, so they never scroll the page. Space selects the
// focused segment. Removing the selected segment selects the one taking its place, or the new last
// one; replacing it (replaceWith) selects the new segment at its index.
//
// The pill (a decorative span the element adds) slides to the selected segment over MotionNormal
// (instant under reduce motion, through the CSS duration). Its place is CSS, from the selected
// index and the segment count, since the segments are equal: a resize moves it with the layout at
// once, and so does adding or removing a segment. The element fires `change` when the selection changes from a click, a key or a removal.
// It is form-associated: with `name` it submits the selected segment's data-value (else its
// aria-label, else its text).

const ITEM = "ui-segmented-control-item";
const PILL = "ui-segmented-control-pill";

function setAttr(el, name, value) {
  if (value === null) {
    if (el.hasAttribute(name)) el.removeAttribute(name);
  } else if (el.getAttribute(name) !== value) el.setAttribute(name, value);
}

const checked = (item) => item.getAttribute("aria-checked") === "true";
const valueOf = (item) => item?.dataset.value ?? item?.getAttribute("aria-label") ?? item?.textContent.trim() ?? null;

export class UiSegmentedControl extends HTMLElement {
  static formAssociated = true;
  static observedAttributes = ["disabled", "name"];

  #internals = this.attachInternals();
  #observer = new MutationObserver(() => this.#sync());
  #pill = null;
  #selected = null; // the selected segment at the last sync, and its index
  #selectedIndex = -1;
  #placed = null; // the segment and segment count the pill was last placed for
  #placedCount = 0;
  #initial = null;
  #formDisabled = false;

  connectedCallback() {
    if (!this.#pill) {
      this.#pill = document.createElement("span");
      this.#pill.className = `${PILL} ui-squircle`;
      this.#pill.setAttribute("aria-hidden", "true");
    }
    if (this.#pill.parentNode !== this) this.prepend(this.#pill);
    this.addEventListener("keydown", this.#onKeyDown);
    this.addEventListener("click", this.#onClick);
    this.addEventListener("pointerdown", this.#onPointerDown);
    this.addEventListener("focusin", this.#onFocusIn);
    this.#observer.observe(this, {
      childList: true, subtree: true, attributes: true,
      attributeFilter: ["aria-checked", "aria-disabled", "class"],
    });
    this.#sync();
    this.#initial = this.selectedItem;
  }

  disconnectedCallback() {
    this.#observer.disconnect();
    this.#placed = null;
  }

  attributeChangedCallback() {
    if (this.isConnected) this.#sync();
  }

  /** The segments, in order. */
  get items() {
    return [...this.children].filter((child) => child.classList.contains(ITEM));
  }

  get disabled() {
    return this.hasAttribute("disabled") || this.#formDisabled;
  }

  set disabled(value) {
    this.toggleAttribute("disabled", Boolean(value));
  }

  get selectedItem() {
    return this.items.find(checked) ?? null;
  }

  get selectedIndex() {
    return this.items.findIndex(checked);
  }

  set selectedIndex(index) {
    const item = this.items[index];
    if (item) this.#select(item, false);
  }

  /** The selected segment's value: its data-value, else its aria-label, else its text. */
  get value() {
    return valueOf(this.selectedItem);
  }

  set value(value) {
    const item = this.items.find((i) => valueOf(i) === String(value));
    if (item) this.#select(item, false);
  }

  get name() {
    return this.getAttribute("name");
  }

  get form() {
    return this.#internals.form;
  }

  formResetCallback() {
    if (this.#initial && this.contains(this.#initial)) this.#select(this.#initial, false);
  }

  formDisabledCallback(disabled) {
    this.#formDisabled = disabled;
    this.#sync();
  }

  #enabled(item) {
    return !this.disabled && item.getAttribute("aria-disabled") !== "true";
  }

  // Roles, states, the single selection, the roving tabindex, the form value and the pill.
  #sync() {
    const items = this.items;
    let notify = false;
    // The selected segment left: the one taking its place, or the new last one.
    if (this.#selected && !items.includes(this.#selected) && items.length) {
      const next = items[Math.min(this.#selectedIndex, items.length - 1)];
      for (const item of items) setAttr(item, "aria-checked", item === next ? "true" : "false");
      notify = true;
    }
    const selected = items.find(checked) ?? items.find((i) => i.getAttribute("aria-disabled") !== "true") ?? null;

    setAttr(this, "role", "radiogroup");
    setAttr(this, "aria-disabled", this.disabled ? "true" : null);
    const focused = items.find((item) => item === document.activeElement && this.#enabled(item));
    const active = this.disabled ? null
      : focused ?? (selected && this.#enabled(selected) ? selected : items.find((i) => this.#enabled(i)));
    for (const item of items) {
      setAttr(item, "role", "radio");
      setAttr(item, "aria-checked", item === selected ? "true" : "false");
      setAttr(item, "tabindex", item === active ? "0" : "-1");
    }
    this.#selected = selected;
    this.#selectedIndex = items.indexOf(selected);
    this.#internals.setFormValue(valueOf(selected));
    if (selected !== this.#placed || items.length !== this.#placedCount) {
      this.#place(selected, items.length, this.#placed !== null && items.length === this.#placedCount);
    }
    if (notify) this.dispatchEvent(new Event("change", { bubbles: true }));
  }

  // Moves the pill under the selected segment: sliding when `animate`, else at once.
  #place(item, count, animate) {
    const pill = this.#pill;
    this.#placed = item;
    this.#placedCount = count;
    if (!pill) return;
    pill.hidden = !item;
    if (!item) return;
    if (!animate) pill.style.transition = "none";
    pill.style.setProperty("--ui-segmented-control-index", String(this.#selectedIndex));
    pill.style.setProperty("--ui-segmented-control-count", String(count));
    if (!animate) {
      void getComputedStyle(pill).insetInlineStart; // apply the place before the transition comes back
      pill.style.transition = "";
    }
  }

  #select(item, notify = true) {
    const changed = !checked(item);
    for (const other of this.items) other.setAttribute("aria-checked", other === item ? "true" : "false");
    this.#sync();
    if (changed && notify) this.dispatchEvent(new Event("change", { bubbles: true }));
  }

  #focus(item) {
    for (const other of this.items) setAttr(other, "tabindex", other === item ? "0" : "-1");
    item.focus();
  }

  #itemFrom(event) {
    const item = event.target.closest?.(`.${ITEM}`);
    return item && item.parentElement === this ? item : null;
  }

  #onPointerDown = (event) => {
    const item = this.#itemFrom(event);
    if (item && !this.#enabled(item)) event.preventDefault(); // disabled segments take no focus
  };

  #onFocusIn = (event) => {
    const item = this.#itemFrom(event);
    if (item && this.#enabled(item)) for (const other of this.items) setAttr(other, "tabindex", other === item ? "0" : "-1");
  };

  #onClick = (event) => {
    const item = this.#itemFrom(event);
    if (!item || !this.#enabled(item)) return;
    this.#select(item);
    item.focus();
  };

  #onKeyDown = (event) => {
    const item = this.#itemFrom(event);
    if (!item || this.disabled || event.altKey || event.ctrlKey || event.metaKey) return;
    const enabled = this.items.filter((i) => this.#enabled(i));
    const rtl = getComputedStyle(this).direction === "rtl";
    const steps = { ArrowRight: rtl ? -1 : 1, ArrowLeft: rtl ? 1 : -1, ArrowDown: 1, ArrowUp: -1 };
    let target;
    if (event.key === "Home") target = enabled[0];
    else if (event.key === "End") target = enabled.at(-1);
    else if (event.key in steps) {
      const at = enabled.indexOf(item);
      target = at < 0 ? enabled[0] : enabled[Math.min(Math.max(at + steps[event.key], 0), enabled.length - 1)];
    } else if (event.key === " ") target = this.#enabled(item) ? item : undefined;
    else return;
    event.preventDefault();
    if (!target) return;
    if (target !== item) this.#focus(target);
    this.#select(target);
  };
}

if (!customElements.get("ui-segmented-control")) customElements.define("ui-segmented-control", UiSegmentedControl);
