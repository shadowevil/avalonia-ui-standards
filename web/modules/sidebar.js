// Sidebar (0034, 0042, 0048): <ui-sidebar>, the navigation list of the shell's sidebar slot, with
// section headers, an optional footer and the stages of 0022. See sidebar.css for the markup.
//
// Rows are every .ui-list-item inside, list then footer, as one list: one selection (the row with
// aria-current="page"), one Tab stop (the current row, else the first), and Up, Down, Home and End
// moving focus over headers and from the list into the footer and back. A click, Enter or Space
// chooses a row (a link's own navigation still happens) and fires `change` once. Disabled rows
// (aria-disabled="true", or disabled) are skipped and take no focus.
//
// Stages: the shell's data-sidebar-stage by its width (shell.js): expanded, compact (the icon rail,
// labels as tooltips from the tooltip Block) and hidden. toggle() (the .ui-sidebar-toggle button):
// expanded stage, collapses to the rail and back; compact and hidden, opens and closes the overlay.
// The user's choice holds until the shell crosses a threshold. The overlay closes on choosing a row,
// Escape, a click outside (swallowed, as a light-dismiss popup), the button, a threshold, and
// focus leaving it by keyboard. Opening gives the current (else first) row focus; closing by
// Escape, outside or the button, and choosing in the hidden stage, give focus back to what had it.
//
// Strings: the footer list's name is its own aria-label (default "Sidebar footer"); the button's
// name is the argument of createSidebarToggle. Markup works without script: every row is a link.

import { icon } from "../blocks/icon.js";
import { tooltip, tooltipOf, hideTooltip } from "../blocks/tooltip.js";

const ITEM = ".ui-list-item";
const FOOTER_LABEL = "Sidebar footer";
const TOGGLE = ".ui-sidebar-toggle";
const NAV_KEYS = new Set(["ArrowDown", "ArrowUp", "Home", "End"]);

function setAttr(el, name, value) {
  if (value === null) {
    if (el.hasAttribute(name)) el.removeAttribute(name);
  } else if (el.getAttribute(name) !== value) el.setAttribute(name, value);
}

const isCurrent = (item) => item.hasAttribute("aria-current") && item.getAttribute("aria-current") !== "false";

/** A sidebar button for the title band: subtle, square, with the sidebar icon and a tooltip. */
export function createSidebarToggle(label = "Toggle sidebar") {
  const button = document.createElement("button");
  button.type = "button";
  button.className = "ui-button ui-subtle ui-squircle ui-focus-ring ui-sidebar-toggle";
  button.setAttribute("aria-label", label);
  button.append(icon("sidebar-simple"));
  queueMicrotask(() => tooltip(button, label));
  return button;
}

export class UiSidebar extends HTMLElement {
  #observer = new MutationObserver((records) => this.#onMutation(records));
  #stageObserver = new MutationObserver(() => this.#onStage());
  #shell = null;
  #stage = "expanded";
  #opener = null;
  #animateTimer = 0;
  #syncing = false;

  connectedCallback() {
    this.#shell = this.closest(".ui-shell");
    if (!this.id) this.id = `ui-sidebar-${Math.random().toString(36).slice(2, 8)}`;
    if (!this.querySelector(":scope > .ui-sidebar-surface")) {
      const surface = document.createElement("div");
      surface.className = "ui-sidebar-surface ui-glass ui-squircle";
      surface.setAttribute("aria-hidden", "true");
      this.prepend(surface);
    }
    this.addEventListener("keydown", this.#onKeyDown);
    this.addEventListener("click", this.#onClick);
    this.addEventListener("focusin", this.#onFocusIn);
    this.addEventListener("focusout", this.#onFocusOut);
    document.addEventListener("click", this.#onToggleClick);
    document.addEventListener("keydown", this.#onEscape);
    this.#observer.observe(this, {
      subtree: true, childList: true, attributes: true,
      attributeFilter: ["aria-current", "aria-disabled", "disabled"],
    });
    if (this.#shell) {
      this.#stageObserver.observe(this.#shell, { attributes: true, attributeFilter: ["data-sidebar-stage"] });
      this.#stage = this.#shell.dataset.sidebarStage ?? "expanded";
    }
    this.#sync();
  }

  disconnectedCallback() {
    this.#observer.disconnect();
    this.#stageObserver.disconnect();
    document.removeEventListener("click", this.#onToggleClick);
    document.removeEventListener("keydown", this.#onEscape);
    document.removeEventListener("pointerdown", this.#onOutsidePointerDown, true);
    this.#endLightDismiss();
    clearTimeout(this.#animateTimer);
  }

  /** The rows, list then footer, in order. */
  get items() {
    return [...this.querySelectorAll(ITEM)];
  }

  get selectedItem() {
    return this.items.find(isCurrent) ?? null;
  }

  get selectedIndex() {
    return this.items.findIndex(isCurrent);
  }

  set selectedIndex(index) {
    const item = this.items[index];
    this.#choose(item && this.#enabled(item) ? item : null);
  }

  /** The stage the shell is in: "expanded", "compact" or "hidden". */
  get stage() {
    return this.#stage;
  }

  get isOverlayOpen() {
    return this.hasAttribute("data-overlay");
  }

  /** True when the user has collapsed the expanded sidebar to the rail. */
  get isCollapsed() {
    return this.hasAttribute("data-collapsed");
  }

  #enabled(item) {
    return item.getAttribute("aria-disabled") !== "true" && !item.disabled;
  }

  get #enabledItems() {
    return this.items.filter((item) => this.#enabled(item));
  }

  // The button: expanded stage, collapse or expand inline; otherwise open or close the overlay.
  toggle() {
    if (this.isOverlayOpen) this.close();
    else if (this.#stage === "expanded") {
      this.#animate();
      this.toggleAttribute("data-collapsed");
      this.#sync();
    } else this.open();
  }

  /** Opens the overlay (compact and hidden stages) and focuses the current, else the first, row. */
  open() {
    if (this.isOverlayOpen || this.#stage === "expanded") return;
    this.#opener = document.activeElement instanceof HTMLElement && document.activeElement !== document.body ? document.activeElement : null;
    this.#animate();
    this.setAttribute("data-overlay", "");
    this.#sync();
    document.addEventListener("pointerdown", this.#onOutsidePointerDown, true);
    const target = this.selectedItem && this.#enabled(this.selectedItem) ? this.selectedItem : this.#enabledItems[0];
    target?.focus({ preventScroll: true });
    target?.scrollIntoView({ block: "nearest" });
  }

  /** Closes the overlay; `restoreFocus` gives focus back to what had it when it opened. */
  close({ restoreFocus = true } = {}) {
    if (!this.isOverlayOpen) return;
    this.#animate();
    this.removeAttribute("data-overlay");
    document.removeEventListener("pointerdown", this.#onOutsidePointerDown, true);
    this.#sync();
    const opener = this.#opener;
    this.#opener = null;
    if (restoreFocus && opener?.isConnected) opener.focus({ preventScroll: true });
  }

  // The width and label transitions run only around a toggle.
  #animate() {
    this.setAttribute("data-animating", "");
    clearTimeout(this.#animateTimer);
    const ms = parseFloat(getComputedStyle(this).getPropertyValue("--ui-motion-normal")) || 0;
    this.#animateTimer = setTimeout(() => this.removeAttribute("data-animating"), ms + 50);
  }

  #onStage() {
    const stage = this.#shell?.dataset.sidebarStage ?? "expanded";
    if (stage === this.#stage) return;
    this.#stage = stage;
    // Crossing a threshold returns to the automatic stage.
    this.removeAttribute("data-animating");
    this.removeAttribute("data-collapsed");
    if (this.isOverlayOpen) {
      this.removeAttribute("data-overlay");
      document.removeEventListener("pointerdown", this.#onOutsidePointerDown, true);
      const opener = this.#opener;
      this.#opener = null;
      if (this.contains(document.activeElement) && stage === "hidden" && opener?.isConnected) opener.focus({ preventScroll: true });
    }
    this.#sync();
  }

  // A row marked from outside (aria-current set on another row) takes the selection over.
  #onMutation(records) {
    const set = records.filter((r) => r.type === "attributes" && r.attributeName === "aria-current" && isCurrent(r.target));
    const latest = set.at(-1)?.target;
    if (latest) for (const other of this.items) if (other !== latest) setAttr(other, "aria-current", null);
    this.#sync();
  }

  // States, the roving tabindex, the rail's tooltips and the button's state, from the markup.
  #sync() {
    if (this.#syncing) return;
    this.#syncing = true;
    try {
      const rail = this.#stage === "compact" || (this.#stage === "expanded" && this.isCollapsed);
      this.toggleAttribute("data-rail", rail);
      this.setAttribute("data-stage", this.#stage);

      // One selection (a second mark from markup: the first keeps it).
      const marked = this.items.filter(isCurrent);
      for (const extra of marked.slice(1)) extra.removeAttribute("aria-current");

      const footer = this.querySelector(".ui-sidebar-footer");
      if (footer && !footer.hasAttribute("aria-label") && !footer.hasAttribute("aria-labelledby")) footer.setAttribute("aria-label", FOOTER_LABEL);

      const items = this.items;
      const focused = items.find((item) => item === document.activeElement && this.#enabled(item));
      const active = focused ?? items.find((item) => isCurrent(item) && this.#enabled(item)) ?? items.find((item) => this.#enabled(item));
      for (const item of items) setAttr(item, "tabindex", item === active ? "0" : "-1");

      const tips = rail && !this.isOverlayOpen;
      for (const item of items) {
        const existing = tooltipOf(item);
        if (tips && !existing) {
          const text = item.querySelector(".ui-sidebar-label")?.textContent.trim() || item.getAttribute("aria-label") || item.textContent.trim();
          if (text) tooltip(item, text);
        } else if (!tips && existing) {
          const ids = (item.getAttribute("aria-describedby") ?? "").split(/\s+/).filter((id) => id && id !== existing.id);
          setAttr(item, "aria-describedby", ids.length ? ids.join(" ") : null);
          if (existing.matches(":popover-open")) hideTooltip();
          existing.remove();
        }
      }

      const open = this.isOverlayOpen || (this.#stage === "expanded" && !this.isCollapsed);
      for (const toggle of this.#toggles()) {
        setAttr(toggle, "aria-expanded", String(open));
        setAttr(toggle, "aria-controls", this.id);
      }
    } finally {
      // Observer callbacks for our own changes arrive after this task's records are taken.
      this.#observer.takeRecords();
      this.#syncing = false;
    }
  }

  #toggles() {
    return [...(this.#shell ?? document).querySelectorAll(TOGGLE)];
  }

  // Marks `item` as the current row (and no other), firing change once when it is a change.
  #choose(item) {
    const was = this.selectedItem;
    if (was === item) return;
    for (const other of this.items) if (other !== item) setAttr(other, "aria-current", null);
    if (item) item.setAttribute("aria-current", "page");
    this.#sync();
    this.dispatchEvent(new Event("change", { bubbles: true }));
  }

  #itemFrom(event) {
    return event.target.closest?.(ITEM) ?? null;
  }

  #onClick = (event) => {
    const item = this.#itemFrom(event);
    if (!item) return;
    if (!this.#enabled(item)) {
      event.preventDefault();
      return;
    }
    this.#choose(item);
    if (!this.isOverlayOpen) return;
    // Choosing closes the overlay: the row keeps focus in the compact stage, and in the hidden
    // stage, where the rows go away, focus goes back.
    if (this.#stage === "compact") {
      this.close({ restoreFocus: false });
      item.focus({ preventScroll: true });
    } else this.close();
  };

  #onFocusIn = (event) => {
    const item = this.#itemFrom(event);
    if (item && this.#enabled(item)) for (const other of this.items) setAttr(other, "tabindex", other === item ? "0" : "-1");
  };

  // Focus leaving the open overlay by keyboard closes it, so it never sits over the focused content.
  #onFocusOut = (event) => {
    if (!this.isOverlayOpen) return;
    const to = event.relatedTarget;
    if (!to || this.contains(to) || to.closest?.(TOGGLE)) return;
    this.close({ restoreFocus: false });
  };

  #onKeyDown = (event) => {
    const item = this.#itemFrom(event);
    if (!item || event.altKey || event.ctrlKey || event.metaKey) return;
    if (NAV_KEYS.has(event.key)) {
      const enabled = this.#enabledItems;
      if (!enabled.length) return;
      const at = enabled.indexOf(item);
      const target = event.key === "Home" ? enabled[0]
        : event.key === "End" ? enabled.at(-1)
        : event.key === "ArrowDown" ? enabled[Math.min(at + 1, enabled.length - 1)]
        : enabled[Math.max(at < 0 ? 0 : at - 1, 0)];
      event.preventDefault();
      target.focus({ preventScroll: true });
      target.scrollIntoView({ block: "nearest" });
    } else if (event.key === " " && item.tagName === "A") {
      // Space chooses a link as it does a button.
      event.preventDefault();
      if (this.#enabled(item)) item.click();
    }
  };

  // The sidebar buttons of this shell.
  #onToggleClick = (event) => {
    const button = event.target instanceof Element ? event.target.closest(TOGGLE) : null;
    if (!button || !this.#toggles().includes(button) || event.defaultPrevented) return;
    if (this.#shell?.querySelector("ui-sidebar") !== this) return;
    this.toggle();
  };

  #onEscape = (event) => {
    if (event.key !== "Escape" || event.defaultPrevented || !this.isOverlayOpen) return;
    event.preventDefault();
    this.close();
  };

  // A click outside closes the overlay and is not passed on.
  #onOutsidePointerDown = (event) => {
    const target = event.target instanceof Element ? event.target : null;
    if (!target || this.contains(target) || target.closest(TOGGLE)) return;
    event.preventDefault();
    event.stopPropagation();
    document.addEventListener("click", this.#swallowClick, true);
    document.addEventListener("pointerup", this.#endLightDismissSoon, { once: true, capture: true });
    this.close();
  };

  #swallowClick = (event) => {
    event.preventDefault();
    event.stopPropagation();
  };

  #endLightDismissSoon = () => {
    setTimeout(() => this.#endLightDismiss(), 50);
  };

  #endLightDismiss() {
    document.removeEventListener("click", this.#swallowClick, true);
  }
}

if (!customElements.get("ui-sidebar")) customElements.define("ui-sidebar", UiSidebar);
