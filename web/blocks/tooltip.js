// ToolTip (0037, accessibility.md): shows a control's .ui-tooltip (the element its aria-describedby
// names) after the pointer rests on the control, or after it takes keyboard focus, and hides it
// again. Delegated from the document, so controls added at any time work, disabled ones included.
//
// - Delays (Avalonia's): SHOW_DELAY after the pointer comes to a control, BETWEEN_DELAY when a
//   tooltip is already shown (moving from control to control).
// - Placement (Avalonia's Pointer placement): from the pointer, POINTER_OFFSET px below it; above
//   the pointer when there is no room below; slid to stay in the viewport. Shown by keyboard focus,
//   it is centred below the control, SpacingXS away (above it when there is no room).
// - Hoverable (WCAG 1.4.13): the pointer can move from the control onto the tooltip; it hides
//   HIDE_GRACE ms after the pointer has left both, at once when the control loses focus, is
//   pressed, or on Escape (which then does nothing else, so an open menu or flyout stays open).
// - The tooltip is a popover="manual" (set if missing), so it shows in the top layer above every
//   clip. Its text stays the control's description while hidden.
// tooltip(control, text) makes and links one.

/** ms before a tooltip shows. */
export const SHOW_DELAY = 400;
/** ms before a tooltip shows while another is shown. */
export const BETWEEN_DELAY = 100;
/** px from the pointer down to the tooltip. */
export const POINTER_OFFSET = 20;
/** ms a tooltip stays after the pointer leaves its control and itself, so the pointer can reach it. */
export const HIDE_GRACE = 100;

const DESCRIBED = "[aria-describedby]";

let nextId = 0;
let shown = null; // { control, tip }
let pending = null; // { control, timer }
let graceTimer = 0;
let pointer = { x: 0, y: 0 };

/** The .ui-tooltip that `control`'s aria-describedby names, or null. */
export function tooltipOf(control) {
  for (const id of (control.getAttribute("aria-describedby") ?? "").split(/\s+/)) {
    const el = id ? document.getElementById(id) : null;
    if (el?.classList.contains("ui-tooltip")) return el;
  }
  return null;
}

/** Makes a tooltip with `text` for `control`, placed after it and linked with aria-describedby. */
export function tooltip(control, text) {
  const tip = document.createElement("div");
  tip.id = `ui-tooltip-${nextId++}`;
  tip.className = "ui-tooltip ui-squircle";
  tip.setAttribute("role", "tooltip");
  tip.popover = "manual";
  tip.textContent = text;
  control.after(tip);
  const ids = (control.getAttribute("aria-describedby") ?? "").split(/\s+/).filter(Boolean);
  control.setAttribute("aria-describedby", [...ids, tip.id].join(" "));
  return tip;
}

const controlFor = (target) => {
  const control = target instanceof Element ? target.closest(DESCRIBED) : null;
  return control && tooltipOf(control) ? control : null;
};

function cancelPending() {
  if (pending) clearTimeout(pending.timer);
  pending = null;
}

/** Hides the shown tooltip, if any, and cancels one about to show. */
export function hideTooltip() {
  cancelPending();
  hideShown();
}

function hideShown() {
  clearTimeout(graceTimer);
  if (!shown) return;
  const { tip } = shown;
  shown = null;
  if (tip.matches(":popover-open")) tip.hidePopover();
}

function schedule(control, how) {
  if (pending?.control === control || shown?.control === control) return;
  cancelPending();
  const delay = shown ? BETWEEN_DELAY : SHOW_DELAY;
  pending = { control, timer: setTimeout(() => showTooltip(control, how), delay) };
}

/** Shows `control`'s tooltip now, placed from the pointer ("pointer") or below the control ("focus"). */
export function showTooltip(control, how = "focus") {
  cancelPending();
  const tip = tooltipOf(control);
  if (!tip || !control.isConnected) return;
  if (shown && shown.tip !== tip) hideShown();
  clearTimeout(graceTimer);
  if (!tip.hasAttribute("popover")) tip.popover = "manual";
  if (!tip.matches(":popover-open")) tip.showPopover();
  shown = { control, tip };
  place(tip, control, how);
}

function place(tip, control, how) {
  const vw = document.documentElement.clientWidth;
  const vh = document.documentElement.clientHeight;
  const { width: w, height: h } = tip.getBoundingClientRect();
  const rtl = getComputedStyle(control).direction === "rtl";
  let x;
  let y;
  if (how === "pointer") {
    x = rtl ? pointer.x - w : pointer.x;
    y = pointer.y + POINTER_OFFSET + h > vh ? pointer.y - h : pointer.y + POINTER_OFFSET;
  } else {
    const gap = parseFloat(getComputedStyle(tip).getPropertyValue("--ui-spacing-xs")) || 0;
    const a = control.getBoundingClientRect();
    x = a.left + a.width / 2 - w / 2;
    y = a.bottom + gap + h > vh && a.top - gap - h >= 0 ? a.top - gap - h : a.bottom + gap;
  }
  tip.style.left = `${Math.max(0, Math.min(x, vw - w))}px`;
  tip.style.top = `${Math.max(0, Math.min(y, vh - h))}px`;
}

function graceHide() {
  clearTimeout(graceTimer);
  graceTimer = setTimeout(hideShown, HIDE_GRACE);
}

document.addEventListener("pointermove", (e) => {
  pointer = { x: e.clientX, y: e.clientY };
}, { capture: true, passive: true });

document.addEventListener("pointerover", (e) => {
  pointer = { x: e.clientX, y: e.clientY };
  if (e.pointerType === "touch") return;
  const target = e.target instanceof Element ? e.target : null;
  // On the shown tooltip, or back on its control: it stays.
  if (shown && (shown.tip.contains(target) || shown.control.contains(target))) {
    clearTimeout(graceTimer);
    return;
  }
  const control = controlFor(target);
  if (control) schedule(control, "pointer");
});

document.addEventListener("pointerout", (e) => {
  const to = e.relatedTarget instanceof Element ? e.relatedTarget : null;
  if (pending && !pending.control.contains(to)) cancelPending();
  if (shown && !(to && (shown.tip.contains(to) || shown.control.contains(to)))) graceHide();
});

document.addEventListener("focusin", (e) => {
  const control = controlFor(e.target);
  if (control && e.target.matches(":focus-visible")) schedule(control, "focus");
});

document.addEventListener("focusout", (e) => {
  const control = controlFor(e.target);
  if (control && (shown?.control === control || pending?.control === control)) hideTooltip();
});

document.addEventListener("pointerdown", () => hideTooltip(), true);

document.addEventListener("keydown", (e) => {
  if (e.key !== "Escape" || !shown) return;
  hideTooltip();
  e.preventDefault();
  e.stopImmediatePropagation();
}, true);
