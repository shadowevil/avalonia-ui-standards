// TabControl and TabStrip (0033): <ui-tab-strip> is the tablist of .ui-tab buttons, with the
// sliding underline, the roving tabindex and the keys; <ui-tabs> pairs its strip's tabs with its
// .ui-tab-panel children in order and shows the selected one. See tabs.css for the markup.
//
// Keys (Fluent's, as the ARIA tabs pattern with automatic activation): Left and Right (swapped
// right to left) or, beside the panels, Up and Down move to the previous or next enabled tab and
// select it, stopping at the ends; Home and End go to the first and last. Disabled tabs are skipped.
// The strip fires a bubbling "change" event when the user changes the selection; selectedIndex
// reads and sets it from script.
let uid = 0;
const nextId = (prefix) => `${prefix}-${++uid}`;

const FOCUSABLE = "a[href], button, input, select, textarea, summary, [tabindex]:not([tabindex='-1']), [contenteditable='true']";

function vertical(strip) {
  const tabs = strip.parentElement?.localName === "ui-tabs" ? strip.parentElement : null;
  const placement = tabs?.getAttribute("placement");
  return placement === "start" || placement === "end";
}

class UiTabStrip extends HTMLElement {
  #indicator = null;
  #observer = null;
  #resize = null;
  #placed = false;

  connectedCallback() {
    if (!this.#indicator) {
      this.#indicator = document.createElement("span");
      this.#indicator.className = "ui-tab-indicator";
      this.#indicator.setAttribute("aria-hidden", "true");
      this.addEventListener("click", this.#onClick);
      this.addEventListener("keydown", this.#onKeyDown);
    }
    this.setAttribute("role", "tablist");
    if (!this.#indicator.isConnected) this.append(this.#indicator);
    // Children written in HTML arrive after the element is connected, and tabs may be added later.
    this.#observer = new MutationObserver(() => this.sync());
    this.#observer.observe(this, { childList: true });
    this.#resize = new ResizeObserver(() => this.#place(false));
    this.#resize.observe(this);
    this.sync();
  }

  disconnectedCallback() {
    this.#observer?.disconnect();
    this.#resize?.disconnect();
    this.#placed = false;
  }

  /** The tab buttons, in order. */
  get tabs() {
    return [...this.children].filter((c) => c.classList.contains("ui-tab"));
  }

  /** The selected tab's index, or -1. */
  get selectedIndex() {
    return this.tabs.findIndex((t) => t.getAttribute("aria-selected") === "true");
  }

  set selectedIndex(index) {
    const tab = this.tabs[index];
    if (tab) this.#select(tab, { focus: false, notify: false });
  }

  /** Brings roles, selection, tabindex and the underline in step with the children. */
  sync() {
    const tabs = this.tabs;
    let selected = tabs.find((t) => t.getAttribute("aria-selected") === "true");
    if (!selected) selected = tabs.find((t) => !t.disabled) ?? tabs[0];
    for (const tab of tabs) {
      tab.setAttribute("role", "tab");
      tab.setAttribute("aria-selected", String(tab === selected));
      if (!tab.id) tab.id = nextId("ui-tab");
      if (!tab.hasAttribute("type") && tab.localName === "button") tab.type = "button";
      this.#resize?.observe(tab);
    }
    if (vertical(this)) this.setAttribute("aria-orientation", "vertical");
    else this.removeAttribute("aria-orientation");
    this.#rove(selected);
    if (this.#indicator && this.lastElementChild !== this.#indicator) {
      // Keep the underline last, out of the tabs' order; appending it again is not a tab change.
      this.#observer?.disconnect();
      this.append(this.#indicator);
      this.#observer?.observe(this, { childList: true });
    }
    this.#place(false);
    this.parentElement?.localName === "ui-tabs" && this.parentElement.sync?.();
  }

  // The selected tab, or the first enabled one, is the strip's one Tab stop.
  #rove(selected) {
    const tabs = this.tabs;
    const stop = selected && !selected.disabled ? selected : tabs.find((t) => !t.disabled);
    for (const tab of tabs) tab.tabIndex = tab === stop ? 0 : -1;
  }

  #select(tab, { focus, notify }) {
    const before = this.selectedIndex;
    for (const t of this.tabs) t.setAttribute("aria-selected", String(t === tab));
    this.#rove(tab);
    if (focus) tab.focus();
    this.#place(true);
    this.parentElement?.localName === "ui-tabs" && this.parentElement.sync?.();
    if (notify && this.selectedIndex !== before) this.dispatchEvent(new Event("change", { bubbles: true }));
  }

  #onClick = (event) => {
    const tab = event.target.closest?.(".ui-tab");
    if (!tab || tab.parentElement !== this || tab.disabled) return;
    this.#select(tab, { focus: false, notify: true });
  };

  #onKeyDown = (event) => {
    const tab = event.target.closest?.(".ui-tab");
    if (!tab || tab.parentElement !== this || event.altKey || event.ctrlKey || event.metaKey) return;
    const enabled = this.tabs.filter((t) => !t.disabled);
    const at = enabled.indexOf(tab);
    const rtl = getComputedStyle(this).direction === "rtl";
    const [back, forward] = vertical(this) ? ["ArrowUp", "ArrowDown"] : rtl ? ["ArrowRight", "ArrowLeft"] : ["ArrowLeft", "ArrowRight"];
    let target;
    if (event.key === back) target = enabled[Math.max(0, at - 1)];
    else if (event.key === forward) target = enabled[Math.min(enabled.length - 1, at + 1)];
    else if (event.key === "Home") target = enabled[0];
    else if (event.key === "End") target = enabled[enabled.length - 1];
    else return;
    event.preventDefault();
    if (target) this.#select(target, { focus: true, notify: true });
  };

  // The underline spans the selected header's content (inside its padding) on the edge facing
  // the content. It slides when the selection changes and moves at once on a layout change.
  #place(animate) {
    const indicator = this.#indicator;
    if (!indicator || !this.isConnected) return;
    const tab = this.tabs[this.selectedIndex];
    if (!tab) {
      indicator.style.display = "none";
      return;
    }
    indicator.style.display = "";
    // Measured from layout boxes (fractional), not offset* (whole pixels), so the underline lands
    // exactly on the header at any text metrics or zoom. Origin: the strip's padding box.
    const s = getComputedStyle(tab);
    const thickness = parseFloat(getComputedStyle(this).getPropertyValue("--ui-tab-indicator-thickness")) || 3;
    const placement = this.parentElement?.localName === "ui-tabs" ? this.parentElement.getAttribute("placement") : null;
    const box = this.getBoundingClientRect();
    const strip = getComputedStyle(this);
    const left = box.left + parseFloat(strip.borderLeftWidth);
    const top = box.top + parseFloat(strip.borderTopWidth);
    const inner = box.width - parseFloat(strip.borderLeftWidth) - parseFloat(strip.borderRightWidth);
    const r = tab.getBoundingClientRect();
    const [padTop, padRight, padBottom, padLeft] = [s.paddingTop, s.paddingRight, s.paddingBottom, s.paddingLeft].map(parseFloat);
    // The offset along the strip is from its inline start (logical), so it stays right when the
    // writing direction flips, which resizes nothing and so does not call #place.
    const rtl = strip.direction === "rtl";
    let x, y, w, h;
    let end = "auto";
    if (placement === "start" || placement === "end") {
      // On the strip's inner edge, held there by CSS, so a change in the strip's width needs no update.
      [x, end] = placement === "start" ? ["auto", "0px"] : ["0px", "auto"];
      y = r.top - top + padTop;
      w = thickness;
      h = Math.max(0, r.height - padTop - padBottom);
    } else {
      x = `${rtl ? left + inner - r.right + padRight : r.left - left + padLeft}px`;
      y = placement === "bottom" ? r.top - top : r.bottom - top - thickness;
      w = Math.max(0, r.width - padLeft - padRight);
      h = thickness;
    }
    const still = !animate || !this.#placed;
    if (still) indicator.style.transition = "none";
    indicator.style.insetInlineStart = x;
    indicator.style.insetInlineEnd = end;
    indicator.style.insetBlockStart = `${y}px`;
    indicator.style.width = `${w}px`;
    indicator.style.height = `${h}px`;
    if (still) {
      indicator.getBoundingClientRect();
      indicator.style.transition = "";
    }
    this.#placed = true;
  }
}

class UiTabs extends HTMLElement {
  #observer = null;

  static observedAttributes = ["placement"];

  connectedCallback() {
    this.#observer = new MutationObserver(() => this.sync());
    this.#observer.observe(this, { childList: true });
    this.sync();
  }

  disconnectedCallback() {
    this.#observer?.disconnect();
  }

  attributeChangedCallback() {
    if (this.isConnected) this.strip?.sync();
  }

  /** The strip. */
  get strip() {
    return [...this.children].find((c) => c.localName === "ui-tab-strip") ?? null;
  }

  /** The panels, in order. */
  get panels() {
    return [...this.children].filter((c) => c.classList.contains("ui-tab-panel"));
  }

  /** Pairs each tab with its panel and shows the selected panel only. */
  sync() {
    const strip = this.strip;
    if (!strip || !strip.tabs) return;
    const tabs = strip.tabs;
    const selected = strip.selectedIndex;
    this.panels.forEach((panel, i) => {
      const tab = tabs[i];
      panel.setAttribute("role", "tabpanel");
      // The ring (0019) for when the panel itself has focus.
      panel.classList.add("ui-squircle", "ui-focus-ring");
      if (!panel.id) panel.id = nextId("ui-tab-panel");
      if (tab) {
        tab.setAttribute("aria-controls", panel.id);
        panel.setAttribute("aria-labelledby", tab.id);
      }
      panel.hidden = i !== selected;
      // A panel with nothing focusable in it takes focus itself, so its content can be reached.
      if (!panel.hasAttribute("data-ui-tabindex") && panel.hasAttribute("tabindex")) return;
      const focusable = panel.querySelector(FOCUSABLE);
      if (focusable) {
        panel.removeAttribute("tabindex");
        panel.removeAttribute("data-ui-tabindex");
      } else {
        panel.tabIndex = 0;
        panel.setAttribute("data-ui-tabindex", "");
      }
    });
  }
}

if (!customElements.get("ui-tab-strip")) customElements.define("ui-tab-strip", UiTabStrip);
if (!customElements.get("ui-tabs")) customElements.define("ui-tabs", UiTabs);
