// Flyout (0036): a glass overlay anchored to the element that opened it, on the native popover
// attribute (top layer, light dismiss, Escape). This module places it, moves focus into it and back
// out, and keeps its opener's aria-expanded in step. Later Blocks (menus, the ComboBox drop-down,
// pickers) build on it.
//
// Markup (flyout.css): <div class="ui-flyout ui-glass ui-squircle" popover> with its content in a
// <div class="ui-flyout-content ui-scroll">. Use popover="auto" (the default value): opening one
// closes the others that are not its ancestors, a click outside closes it (light dismiss), and
// Escape closes the topmost.
//
// Two ways to open one:
// - Declarative: a button with popovertarget="<id>". The button is the anchor and opener; focus
//   moves to the flyout's first focusable element ([autofocus] first) once it is open.
// - Script: openFlyout(flyout, { anchor, point, placement, focus }).
//     anchor     the element it is placed against, and the opener focus returns to
//     point      { x, y } in viewport px: placed at that point (a context menu), placement "point"
//     opener     the element focus returns to, when it is not the anchor
//     placement  sets data-placement (see flyout.css) before it opens
//     focus      "first" (default), "last", an element, or false to leave focus where it is
//
// When a flyout closes while focus is in it (Escape, closeFlyout, an action inside it), focus
// returns to its opener. A light dismiss by clicking elsewhere leaves focus on what was clicked
// (on the opener when that takes no focus).
// The opener's aria-expanded is "true" while the flyout is open and "false" after.
//
// Placement is in flyout.css. Where CSS anchor positioning exists (Chromium, Safari) the anchored
// placements are CSS: the anchor gets an anchor-name and the flyout position-anchor and
// data-anchored. Elsewhere (Firefox), and for a point, this module sets left and top, flipping to
// the other side when there is no room and sliding to stay in the viewport, and places it again
// while it is open as the page scrolls or resizes.

const FOCUSABLE = "button, [href], input, select, textarea, [tabindex], [contenteditable='true']";

const anchorPositioning = CSS.supports("position-area: block-end") && CSS.supports("anchor-name: --a");
let useCss = anchorPositioning;
let nextAnchor = 0;

const state = new WeakMap(); // flyout -> { anchor, opener, point, focus, scripted }
const placedByScript = new Set(); // open flyouts placed by this module

const isFlyout = (el) => el instanceof HTMLElement && el.classList.contains("ui-flyout");

function setExpanded(opener, open) {
  if (opener?.hasAttribute("aria-expanded") || opener?.hasAttribute("aria-haspopup") || opener?.hasAttribute("popovertarget")) {
    opener.setAttribute("aria-expanded", String(open));
  }
}

/** The element focus returns to when `flyout` closes, or null. */
export function flyoutOpener(flyout) {
  return state.get(flyout)?.opener ?? null;
}

/**
 * Opens `flyout` (a .ui-flyout popover) against `anchor`, or at `point`. See the module comment
 * for the options. Opening an open flyout places it again.
 */
export function openFlyout(flyout, { anchor = null, point = null, opener = anchor, placement, focus = "first" } = {}) {
  if (point) placement = "point";
  if (placement) flyout.dataset.placement = placement;
  state.set(flyout, { anchor, opener, point, focus, scripted: true });
  if (flyout.matches(":popover-open")) prepare(flyout);
  else flyout.showPopover();
  placeFlyout(flyout);
  moveFocusIn(flyout);
}

/**
 * Closes `flyout` (and the flyouts opened from it). Focus returns to its opener when it was in
 * the flyout, or always with { restoreFocus: true }; never with { restoreFocus: false }.
 */
export function closeFlyout(flyout, { restoreFocus } = {}) {
  if (!flyout.matches(":popover-open")) return;
  const opener = flyoutOpener(flyout);
  const focusInside = flyout.contains(document.activeElement);
  flyout.hidePopover();
  if (opener && (restoreFocus ?? focusInside) && restoreFocus !== false) opener.focus({ preventScroll: true });
}

function placementOf(flyout) {
  return flyout.dataset.placement ?? "block-end";
}

// Before it shows: anchored by CSS where it can be, otherwise left to placeFlyout.
function prepare(flyout) {
  const s = state.get(flyout);
  const placement = placementOf(flyout);
  if (useCss && s?.anchor && placement !== "point") {
    let name = s.anchor.style.anchorName;
    if (!name || name === "none") {
      name = `--ui-flyout-anchor-${nextAnchor++}`;
      s.anchor.style.anchorName = name;
    }
    flyout.style.positionAnchor = name;
    flyout.style.removeProperty("left");
    flyout.style.removeProperty("top");
    flyout.dataset.anchored = "";
    placedByScript.delete(flyout);
  } else {
    delete flyout.dataset.anchored;
    flyout.style.removeProperty("position-anchor");
    placedByScript.add(flyout);
  }
}

const px = (style, name) => parseFloat(style.getPropertyValue(name)) || 0;
const clamp = (v, lo, hi) => Math.max(lo, Math.min(v, hi));

/** Places an open flyout now (script placement only; CSS placement follows by itself). */
export function placeFlyout(flyout) {
  if (!placedByScript.has(flyout) || !flyout.matches(":popover-open")) return;
  const s = state.get(flyout) ?? {};
  const style = getComputedStyle(flyout);
  const rtl = style.direction === "rtl";
  const offset = px(style, "--ui-flyout-offset");
  const shift = px(style, "--ui-flyout-shift");
  const vw = document.documentElement.clientWidth;
  const vh = document.documentElement.clientHeight;
  const { width: w, height: h } = flyout.getBoundingClientRect();
  const placement = placementOf(flyout);
  let x;
  let y;
  if (placement === "point" || !s.anchor) {
    const p = s.point ?? { x: 0, y: 0 };
    x = rtl ? p.x - w : p.x;
    if (x + w > vw || x < 0) x = rtl ? p.x : p.x - w;
    y = p.y + h > vh ? p.y - h : p.y;
  } else {
    const a = s.anchor.getBoundingClientRect();
    const below = a.bottom + offset;
    const above = a.top - offset - h;
    if (placement === "inline-end") {
      const end = rtl ? a.left - offset - w : a.right + offset;
      const start = rtl ? a.right + offset : a.left - offset - w;
      x = end >= 0 && end + w <= vw ? end : start;
      y = a.top - shift + h > vh ? a.bottom + shift - h : a.top - shift;
    } else {
      if (placement === "block-end-start" || placement === "block-end-end") {
        const startAligned = rtl ? a.right - w + shift : a.left - shift;
        const endAligned = rtl ? a.left - shift : a.right - w + shift;
        const [first, second] = placement === "block-end-end" ? [endAligned, startAligned] : [startAligned, endAligned];
        x = first >= 0 && first + w <= vw ? first : second;
      } else {
        x = a.left + a.width / 2 - w / 2;
      }
      y = below + h > vh && above >= 0 ? above : below;
    }
  }
  flyout.style.left = `${clamp(x, 0, Math.max(0, vw - w))}px`;
  flyout.style.top = `${clamp(y, 0, Math.max(0, vh - h))}px`;
}

function focusables(flyout) {
  // Not inside a nested flyout, not disabled, not hidden from the Tab order by a disabled state.
  return [...flyout.querySelectorAll(FOCUSABLE)].filter((el) =>
    el.closest(".ui-flyout") === flyout && !el.disabled && el.getAttribute("aria-disabled") !== "true"
    && el.tabIndex >= -1 && !el.closest("[inert]"));
}

function moveFocusIn(flyout) {
  const focus = state.get(flyout)?.focus ?? "first";
  if (focus === false) return;
  let target = focus instanceof Element ? focus : null;
  if (!target) {
    const list = focusables(flyout);
    target = focus === "last" ? list.at(-1) : (list.find((el) => el.hasAttribute("autofocus")) ?? list[0]);
  }
  (target ?? flyout).focus({ preventScroll: true });
}

function placeAll() {
  for (const flyout of placedByScript) placeFlyout(flyout);
}

// A popovertarget button opening a flyout is its anchor and opener.
document.addEventListener("click", (e) => {
  const button = e.target.closest?.("[popovertarget]");
  const flyout = button && document.getElementById(button.getAttribute("popovertarget"));
  if (!isFlyout(flyout) || flyout.matches(":popover-open")) return;
  state.set(flyout, { anchor: button, opener: button, point: null, focus: "first", scripted: false });
}, true);

// toggle and beforetoggle do not bubble; capture listeners still see them.
document.addEventListener("beforetoggle", (e) => {
  const flyout = e.target;
  if (!isFlyout(flyout)) return;
  if (e.newState === "open") {
    prepare(flyout);
    setExpanded(flyoutOpener(flyout), true);
  } else {
    setExpanded(flyoutOpener(flyout), false);
    placedByScript.delete(flyout);
  }
}, true);

document.addEventListener("toggle", (e) => {
  const flyout = e.target;
  if (!isFlyout(flyout)) return;
  const s = state.get(flyout);
  if (e.newState === "open") {
    if (!flyout.matches(":popover-open")) return;
    placeFlyout(flyout);
    // A declarative opening moves focus in now; openFlyout already did.
    if (s && !s.scripted && !flyout.contains(document.activeElement)) moveFocusIn(flyout);
  } else {
    // Focus was in the flyout (now hidden) or nowhere: back to the opener.
    const active = document.activeElement;
    if (s?.opener && (!active || active === document.body || flyout.contains(active))) s.opener.focus({ preventScroll: true });
  }
}, true);

addEventListener("resize", placeAll);
addEventListener("scroll", placeAll, { capture: true, passive: true });

/**
 * Test hook (not exported from index.js): false places every flyout by script, as in browsers
 * without CSS anchor positioning; true goes back to CSS where the browser has it.
 */
export function setAnchorPositioning(on) {
  useCss = on && anchorPositioning;
}
