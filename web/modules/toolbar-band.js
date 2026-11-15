// Toolbar band (0043, 0048): <ui-toolbar-band>, one row of commands and settings in three zones,
// whose items fold into a trailing "…" menu when they do not fit. Markup and look are in
// toolbar-band.css. The element keeps its items in the light DOM as written and adds the "…"
// button, its tooltip and its menu as its last children.
//
// Roles: a group named by aria-label ("Toolbar" unless the page sets one), not a toolbar: each item
// is its own Tab stop, with no roving tabindex, so arrow keys stay inside segmented controls. The
// Tab order is the DOM order, so items are written in visual order: leading, centre, trailing.
//
// Items: the element's children, except popovers (an item's menu or flyout may sit beside it) and
// tooltips. Per item:
//   data-zone          "leading" (default), "center" or "trailing"
//   data-priority      an integer (default 0): higher overflows first; ties from the trailing end
//   data-begins-group  SpacingL before it instead of SpacingS
//   data-can-overflow  "false" keeps a trailing item in the band
//   data-label         its label in the "…" menu; else its tooltip, its aria-label, its text
// Separators (<hr class="ui-separator">) show only between two shown items of their zone;
// spacers (<span class="ui-toolbar-spacer">) take the free room on their side (in the centre zone,
// the centre spans the room between the other two). A .ui-toolbar-band-item wraps one control with
// decoration (a leading icon) and is that control in the menu.
//
// Layout: leading items start at the padding, trailing items end before "…", and the centre zone
// is centred in the band, moved aside only as far as the other zones need. Every item is measured
// at its own width; items move into the menu, highest priority first, then from the trailing end,
// until the rest fit. Buttons (with drop-downs), toggles and segmented controls have a menu form;
// anything else stays in the band. The layout runs when the band resizes, its items change, the
// density changes or fonts load, before the next paint, so nothing is drawn in between.
//
// The "…" menu (menu.js), below "…" with end edges lined up, rebuilt each time it opens: a button is an item (its icon cloned into the
// icon column) that clicks the button, or opens its own flyout at "…"; a toggle is a check item; a
// drop-down with a menu is a submenu of copies of its entries, which choose the originals (check,
// radio and click as in their own menu); a segmented control is a group headed by its label, with
// one radio item per segment. Separators, group starts and zone boundaries between two folded items
// become menu separators, and a segmented group has one on each side. The marks follow the band's
// controls while the menu is open. "…" opens with Enter, Space or Down (and Up, menu.js's).
// Strings: data-more-label on the band overrides "More" (the "…" tooltip, name and menu name).

import { icon } from "../blocks/icon.js";
import { createMenu, menuItems } from "../blocks/menu.js";
import { openFlyout } from "../blocks/flyout.js";
import { tooltip, tooltipOf } from "../blocks/tooltip.js";

const NAME = "Toolbar";
const MORE = "More";
const ITEM = ".ui-menu-item";
const LAYOUT_ATTRIBUTES = ["hidden", "class", "data-zone", "data-priority", "data-begins-group", "data-can-overflow"];
const STATE_ATTRIBUTES = ["aria-checked", "aria-pressed", "aria-disabled", "disabled"];
const ZONES = { leading: 0, center: 1, trailing: 2 };

let nextId = 0;

function setAttr(el, name, value) {
  if (value === null) {
    if (el.hasAttribute(name)) el.removeAttribute(name);
  } else if (el.getAttribute(name) !== value) el.setAttribute(name, value);
}

const isDisabled = (el) => el.disabled === true || el.getAttribute("aria-disabled") === "true";
const zoneOf = (el) => ZONES[el.dataset.zone] ?? 0;
const kindOf = (el) => (el.matches("hr, [role='separator']") ? "separator" : el.classList.contains("ui-toolbar-spacer") ? "spacer" : "item");

/** The control an item stands for: the item, or the one control a .ui-toolbar-band-item wraps. */
function controlOf(item) {
  if (item.classList.contains("ui-toolbar-band-item")) return item.querySelector(":scope > ui-segmented-control, :scope > .ui-button");
  return item;
}

function menuForm(item) {
  const control = controlOf(item);
  if (!control) return null;
  if (control.localName === "ui-segmented-control") return "segmented";
  if (!control.classList.contains("ui-button") || control.classList.contains("ui-repeat-button")) return null;
  return control.hasAttribute("aria-pressed") ? "toggle" : "button";
}

const text = (el) => el?.textContent.replace(/\s+/g, " ").trim() || null;

/** An item's label in the menu: data-label, its tooltip, its aria-label, or its text. */
export function labelOf(el) {
  for (const node of new Set([el, controlOf(el) ?? el])) {
    const label = node.dataset?.label ?? text(tooltipOf(node)) ?? (node.title || null) ?? node.getAttribute("aria-label");
    if (label) return label;
  }
  return text(el) ?? "";
}

// The element a button opens: a menu or flyout named by aria-controls or popovertarget.
function popupOf(button) {
  const id = button.getAttribute("aria-controls") ?? button.getAttribute("popovertarget");
  const popup = id ? document.getElementById(id) : null;
  return popup?.classList.contains("ui-flyout") ? popup : null;
}

const cloneIcon = (node) => {
  if (!node) return null;
  const copy = node.cloneNode(true);
  copy.removeAttribute("id");
  copy.classList.add("ui-menu-item-icon");
  copy.setAttribute("aria-hidden", "true");
  return copy;
};

// --- Fit: what shows and where (0043) ---------------------------------------------------------

// SpacingL before an item that begins a group, SpacingS otherwise and on either side of a separator.
const gap = (previous, next, s, l) => (next.begins && next.kind === "item" && previous.kind !== "separator" ? l : s);

// The present entries not folded, without separators lacking a shown item on each side in their zone.
function shownOf(entries, folded) {
  const kept = entries.filter((e) => e.present && !folded.has(e));
  const result = [];
  kept.forEach((e, i) => {
    if (e.kind === "separator") {
      const before = result.findLast((r) => r.kind !== "spacer");
      const after = kept.slice(i + 1).find((r) => r.kind !== "spacer");
      if (before?.kind !== "item" || before.zone !== e.zone || after?.kind !== "item" || after.zone !== e.zone) return;
    }
    result.push(e);
  });
  return result;
}

function zonesOf(shown, s, l) {
  return [0, 1, 2].map((zone) => {
    const list = shown.filter((e) => e.zone === zone);
    let width = 0;
    let previous = null;
    for (const e of list.filter((x) => x.kind !== "spacer")) {
      width += e.width + (previous ? gap(previous, e, s, l) : 0);
      previous = e;
    }
    return { list, width, spacers: list.filter((e) => e.kind === "spacer").length };
  });
}

function required(entries, folded, more, m) {
  const zones = zonesOf(shownOf(entries, folded), m.s, m.l);
  const filled = zones.filter((z) => z.width > 0).length;
  let total = 2 * m.p + zones.reduce((sum, z) => sum + z.width, 0) + m.l * Math.max(0, filled - 1);
  if (more) total += m.more + (filled > 0 ? m.s : 0);
  return total;
}

/** Which entries fold: highest priority first, ties from the trailing end, until the rest fit. */
export function plan(entries, m) {
  const folded = new Set();
  const candidates = entries
    .map((e, i) => [e, i])
    .filter(([e]) => e.kind === "item" && e.present && e.canOverflow)
    .sort((a, b) => b[0].priority - a[0].priority || b[1] - a[1])
    .map(([e]) => e);
  for (const candidate of candidates) {
    if (required(entries, folded, folded.size > 0, m) <= m.width + 0.01) break;
    folded.add(candidate);
  }
  return { folded, shown: shownOf(entries, folded), more: folded.size > 0 };
}

/** The inline start of each shown entry (a Map), and each spacer's width. */
export function place(shown, more, m) {
  const zones = zonesOf(shown, m.s, m.l);
  const [lead, centre, trail] = zones;
  const right = m.width - m.p - (more ? m.more + (zones.some((z) => z.width > 0) ? m.s : 0) : 0);
  const leadEnd = m.p + lead.width;
  const trailStart = right - trail.width;
  let leadFlex = 0;
  let trailFlex = 0;
  let centreFlex = 0;
  const lo = lead.width > 0 ? leadEnd + m.l : m.p;
  const hi = trail.width > 0 ? trailStart - m.l : right;
  let centreX = Math.min(Math.max((m.width - centre.width) / 2, lo), Math.max(lo, hi - centre.width));
  if (centre.spacers > 0) {
    centreX = lo;
    centreFlex = Math.max(0, hi - lo - centre.width);
  } else if (centre.width > 0) {
    leadFlex = Math.max(0, centreX - m.l - leadEnd);
    trailFlex = Math.max(0, trailStart - (centreX + centre.width + m.l));
  } else if (lead.spacers + trail.spacers > 0) {
    const free = Math.max(0, trailStart - leadEnd - (lead.width > 0 && trail.width > 0 ? m.l : 0));
    leadFlex = (free * lead.spacers) / (lead.spacers + trail.spacers);
    trailFlex = free - leadFlex;
  }
  const at = new Map();
  const spacerWidth = new Map();
  const lay = (zone, x, spacer) => {
    let previous = null;
    for (const e of zone.list) {
      if (e.kind === "spacer") {
        at.set(e, x);
        spacerWidth.set(e, spacer);
        x += spacer;
        continue;
      }
      if (previous) x += gap(previous, e, m.s, m.l);
      at.set(e, x);
      x += e.width;
      previous = e;
    }
  };
  lay(lead, m.p, lead.spacers ? leadFlex / lead.spacers : 0);
  lay(centre, centreX, centre.spacers ? centreFlex / centre.spacers : 0);
  lay(trail, trail.spacers ? trailStart - trailFlex : trailStart, trail.spacers ? trailFlex / trail.spacers : 0);
  return { at, spacerWidth, moreAt: m.width - m.p - m.more };
}

// --- The element ------------------------------------------------------------------------------

export class UiToolbarBand extends HTMLElement {
  static observedAttributes = ["data-more-label"];

  #more = null;
  #tip = null;
  #menu = null;
  #folded = new Set();
  #pairs = []; // [menu item, read state from its source] while the menu is built
  #scheduled = false;
  #layoutObserver = new MutationObserver((records) => {
    if (records.some((r) => !r.target.closest?.("[popover]") && !(r.target.parentElement?.closest("[popover]")))) this.#schedule();
  });
  #stateObserver = new MutationObserver(() => this.#syncMarks());
  #densityObserver = new MutationObserver(() => this.#schedule());
  #resizeObserver = new ResizeObserver(() => this.layout());
  #onFonts = () => this.#schedule();

  connectedCallback() {
    if (!this.hasAttribute("role")) this.setAttribute("role", "group");
    if (!this.hasAttribute("aria-label") && !this.hasAttribute("aria-labelledby")) this.setAttribute("aria-label", NAME);
    if (!this.#more) this.#createMore();
    this.#keepMoreLast();
    this.#layoutObserver.observe(this, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: LAYOUT_ATTRIBUTES });
    this.#stateObserver.observe(this, { subtree: true, attributes: true, attributeFilter: STATE_ATTRIBUTES });
    this.#densityObserver.observe(document.documentElement, { attributes: true, attributeFilter: ["data-density"] });
    this.#resizeObserver.observe(this);
    document.fonts?.addEventListener("loadingdone", this.#onFonts);
    this.layout();
  }

  disconnectedCallback() {
    this.#layoutObserver.disconnect();
    this.#stateObserver.disconnect();
    this.#densityObserver.disconnect();
    this.#resizeObserver.disconnect();
    document.fonts?.removeEventListener("loadingdone", this.#onFonts);
  }

  attributeChangedCallback() {
    if (this.#more) this.#applyMoreLabel();
  }

  /** The "…" button. */
  get moreButton() {
    return this.#more;
  }

  /** The "…" menu. */
  get moreMenu() {
    return this.#menu;
  }

  /** The items, in order (not the "…" button, popovers or tooltips). */
  get items() {
    const own = new Set([this.#more, this.#tip, this.#menu]);
    return [...this.children].filter((c) => !own.has(c) && !c.hasAttribute("popover") && !c.classList.contains("ui-tooltip")
      && !["template", "script", "style"].includes(c.localName));
  }

  /** The items now in the "…" menu, in order. */
  get overflowedItems() {
    return this.items.filter((i) => this.#folded.has(i));
  }

  #createMore() {
    const id = `ui-toolbar-band-${nextId++}`;
    const more = document.createElement("button");
    more.type = "button";
    more.className = "ui-button ui-subtle ui-squircle ui-focus-ring ui-toolbar-band-more";
    more.append(icon("dots-three"));
    more.setAttribute("aria-haspopup", "menu");
    more.setAttribute("aria-expanded", "false");
    more.setAttribute("aria-controls", `${id}-more`);
    this.#more = more;
    this.#menu = createMenu([], { id: `${id}-more`, placement: "block-end-end" });
    this.append(more, this.#menu);
    this.#tip = tooltip(more, MORE);
    this.#applyMoreLabel();
    // Rebuilt before menu.js opens it (its listeners are on the document, after these).
    more.addEventListener("click", () => this.#rebuildIfClosed());
    more.addEventListener("keydown", (e) => {
      if (e.key === "ArrowDown" || e.key === "ArrowUp") this.#rebuildIfClosed();
    });
  }

  #applyMoreLabel() {
    const label = this.dataset.moreLabel || MORE;
    this.#more.setAttribute("aria-label", label);
    this.#menu.setAttribute("aria-label", label);
    this.#tip.textContent = label;
  }

  // The "…" button, its tooltip and its menu stay the last children, after items added later.
  #keepMoreLast() {
    const tail = [this.#more, this.#tip, this.#menu];
    const children = [...this.children];
    if (tail.every((el, i) => children[children.length - 3 + i] === el)) return;
    this.append(...tail);
  }

  #schedule() {
    if (this.#scheduled) return;
    this.#scheduled = true;
    queueMicrotask(() => {
      this.#scheduled = false;
      if (this.isConnected) {
        this.#keepMoreLast();
        this.layout();
      }
    });
  }

  /** Measures the items and shows those that fit now (it runs by itself; a test hook). */
  layout() {
    const items = this.items;
    const style = getComputedStyle(this);
    const length = (name) => parseFloat(style.getPropertyValue(name)) || 0;
    const active = document.activeElement;
    // Everything shown at its own width while measuring; no paint happens before the end.
    for (const el of [...items, this.#more]) {
      el.removeAttribute("data-overflowed");
      el.style.marginInlineStart = "";
    }
    for (const el of items) if (kindOf(el) === "spacer") el.style.inlineSize = "";
    const m = {
      width: this.getBoundingClientRect().width,
      p: length("--ui-toolbar-band-padding"),
      s: length("--ui-spacing-s"),
      l: length("--ui-spacing-l"),
      more: this.#more.getBoundingClientRect().width,
    };
    const entries = items.map((el) => {
      const kind = kindOf(el);
      const zone = zoneOf(el);
      const present = !el.hidden;
      return {
        el, kind, zone, present,
        width: present && kind !== "spacer" ? el.getBoundingClientRect().width : 0,
        priority: Number.parseInt(el.dataset.priority, 10) || 0,
        canOverflow: kind === "item" && menuForm(el) !== null && (zone !== 2 || el.dataset.canOverflow !== "false"),
        begins: el.hasAttribute("data-begins-group"),
      };
    });
    const { folded, shown, more } = plan(entries, m);
    const { at, spacerWidth, moreAt } = place(shown, more, m);

    const shownSet = new Set(shown);
    let cursor = m.p;
    for (const e of entries) {
      if (!shownSet.has(e)) {
        if (e.present) e.el.setAttribute("data-overflowed", "");
        continue;
      }
      const x = at.get(e);
      const width = e.kind === "spacer" ? spacerWidth.get(e) : e.width;
      if (e.kind === "spacer") e.el.style.inlineSize = `${width}px`;
      e.el.style.marginInlineStart = `${x - cursor}px`;
      cursor = x + width;
    }
    if (more) this.#more.style.marginInlineStart = `${moreAt - cursor}px`;
    else this.#more.setAttribute("data-overflowed", "");

    this.#folded = new Set([...folded].map((e) => e.el));
    if (!more && this.#menu.matches(":popover-open")) this.#menu.hidePopover();
    const lostFocus = items.some((i) => this.#folded.has(i) && i.contains(active));
    if (lostFocus && more) this.#more.focus({ preventScroll: true });
  }

  // --- The "…" menu ---------------------------------------------------------------------------

  #rebuildIfClosed() {
    if (!this.#menu.matches(":popover-open")) this.#rebuild();
  }

  #rebuild() {
    this.#pairs = [];
    const blocks = []; // { entries, patches, group } and "-" separators
    let emitted = false;
    let boundary = false;
    let lastWasGroup = false;
    let zone = null;
    for (const item of this.items.filter((i) => !i.hidden)) {
      if (zone !== null && zone !== zoneOf(item)) boundary = true;
      zone = zoneOf(item);
      const kind = kindOf(item);
      if (kind === "separator") {
        boundary = true;
        continue;
      }
      if (kind === "spacer") continue;
      if (item.hasAttribute("data-begins-group")) boundary = true;
      if (!this.#folded.has(item)) continue;
      const form = menuForm(item);
      const group = form === "segmented";
      if (emitted && (boundary || group || lastWasGroup)) blocks.push("-");
      blocks.push(this.#blockFor(item, form));
      emitted = true;
      boundary = false;
      lastWasGroup = group;
    }

    const entries = [];
    const patches = [];
    for (const block of blocks) {
      if (block === "-") entries.push("-");
      else {
        entries.push(...block.entries);
        patches.push(...block.patches);
      }
    }
    const fresh = createMenu(entries, { id: `${this.#menu.id}-${nextId++}` });
    this.#patch(fresh, patches);
    // Segmented groups: their radios in a named group, under the heading.
    const own = menuItems(fresh);
    let index = 0;
    for (const block of blocks) {
      if (block === "-") continue;
      const items = own.slice(index, index + block.entries.length);
      index += block.entries.length;
      if (!block.group) continue;
      const wrap = document.createElement("div");
      wrap.className = "ui-toolbar-band-menu-group";
      wrap.setAttribute("role", "group");
      wrap.setAttribute("aria-label", block.group);
      const heading = document.createElement("div");
      heading.className = "ui-toolbar-band-menu-heading";
      heading.setAttribute("aria-hidden", "true");
      const span = document.createElement("span");
      span.textContent = block.group;
      heading.append(span);
      items[0].before(wrap);
      wrap.append(heading, ...items);
    }
    this.#menu.firstElementChild.replaceWith(fresh.firstElementChild);
    this.#syncMarks();
  }

  // One item's entries for createMenu, with the patches (cloned icons, gestures, sources) that
  // createMenu cannot express, one per entry.
  #blockFor(item, form) {
    const control = controlOf(item);
    const label = labelOf(item);
    if (form === "segmented") {
      const group = `${this.#menu.id}-g${nextId++}`;
      const entries = [];
      const patches = [];
      for (const segment of control.items) {
        const read = () => ({ checked: segment.getAttribute("aria-checked") === "true", disabled: control.disabled || isDisabled(segment) });
        entries.push({
          label: segment.dataset.label ?? text(tooltipOf(segment)) ?? (segment.title || null) ?? segment.getAttribute("aria-label") ?? text(segment) ?? "",
          type: "radio", group, ...read(),
          onSelect: () => segment.click(),
        });
        patches.push({ icon: segment.querySelector(".ui-icon"), read });
      }
      return { entries, patches, group: label };
    }
    const iconNode = control.querySelector(":scope > .ui-icon");
    if (form === "toggle") {
      const read = () => ({ checked: control.getAttribute("aria-pressed") === "true", disabled: isDisabled(control) });
      return { entries: [{ label, type: "checkbox", ...read(), onSelect: () => control.click() }], patches: [{ icon: iconNode, read }], group: null };
    }
    const popup = popupOf(control);
    const read = () => ({ disabled: isDisabled(control) });
    if (popup?.classList.contains("ui-menu")) {
      const { entries, patches } = this.#copies(popup);
      return { entries: [{ label, items: entries, disabled: isDisabled(control) }], patches: [{ icon: iconNode, read, children: patches }], group: null };
    }
    const onSelect = popup
      ? () => setTimeout(() => openFlyout(popup, { anchor: this.#more, placement: "block-end-end" }))
      : () => control.click();
    return { entries: [{ label, disabled: isDisabled(control), onSelect }], patches: [{ icon: iconNode, read }], group: null };
  }

  // Copies of a drop-down menu's entries: choosing one chooses the original (menu.js checks a check
  // item or a radio and its group, then the original's click handlers run).
  #copies(menu) {
    if (!this.contains(menu)) this.#stateObserver.observe(menu, { subtree: true, attributes: true, attributeFilter: STATE_ATTRIBUTES });
    const entries = [];
    const patches = [];
    const own = [...menu.querySelectorAll(`${ITEM}, .ui-menu-separator`)].filter((el) => el.closest(".ui-menu") === menu);
    for (const original of own) {
      if (original.classList.contains("ui-menu-separator")) {
        entries.push("-");
        continue;
      }
      const role = original.getAttribute("role");
      const type = role === "menuitemcheckbox" ? "checkbox" : role === "menuitemradio" ? "radio" : undefined;
      const read = () => ({ checked: original.getAttribute("aria-checked") === "true", disabled: isDisabled(original) });
      const entry = {
        label: text(original.querySelector(".ui-menu-item-header")) ?? original.dataset.label ?? original.getAttribute("aria-label") ?? text(original) ?? "",
        type, group: original.dataset.group, ...read(),
      };
      const patch = { icon: original.querySelector(".ui-menu-item-icon"), gesture: original.querySelector(".ui-menu-item-gesture"), keys: original.getAttribute("aria-keyshortcuts"), read };
      const sub = original.getAttribute("aria-haspopup") === "menu" ? popupOf(original) : null;
      if (sub) {
        const copies = this.#copies(sub);
        entry.items = copies.entries;
        patch.children = copies.patches;
      } else {
        entry.onSelect = () => original.click();
      }
      entries.push(entry);
      patches.push(patch);
    }
    return { entries, patches };
  }

  // Adds to each built item what its patch carries, in order, into submenus too.
  #patch(menu, patches) {
    menuItems(menu).forEach((item, i) => {
      const patch = patches[i];
      if (!patch) return;
      const copy = cloneIcon(patch.icon);
      if (copy) item.querySelector(".ui-menu-item-header").before(copy);
      if (patch.gesture) {
        item.querySelector(".ui-menu-item-header").after(patch.gesture.cloneNode(true));
        if (patch.keys) item.setAttribute("aria-keyshortcuts", patch.keys);
      }
      this.#pairs.push([item, patch.read]);
      if (patch.children) {
        const sub = menu.querySelector(`#${CSS.escape(item.getAttribute("aria-controls"))}`);
        if (sub) this.#patch(sub, patch.children);
      }
    });
  }

  // The menu's check marks and disabled states follow their sources while it is open.
  #syncMarks() {
    for (const [item, read] of this.#pairs) {
      const { checked, disabled } = read();
      if (checked !== undefined && item.hasAttribute("aria-checked")) setAttr(item, "aria-checked", String(checked));
      setAttr(item, "aria-disabled", disabled ? "true" : null);
    }
  }
}

if (!customElements.get("ui-toolbar-band")) customElements.define("ui-toolbar-band", UiToolbarBand);
