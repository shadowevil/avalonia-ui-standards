// Scrolling (0024, 0025): every element with the class ui-scroll gets the scroll-edge shadows and
// the overlay scrollbar, with nothing to call; this module starts by itself when loaded and
// follows elements as they gain or lose the class.
//
// Why a drawn bar over a styled native one: native scrollbars take layout space (except the OS
// overlay bars, which a page cannot ask for), cannot inset their pills, widen with the pointer's
// closeness or fade, and Chromium ignores most of their styling once scrollbar-width or
// scrollbar-color is set. So the native bars are hidden and a bar is drawn over the content, while
// scrolling stays the browser's: wheel, touch, keys, scroll into view and find in page are
// untouched, and the drawn layer is aria-hidden (assistive technology does not use scrollbars).
//
// The overlay is a child of the scroller, at the scroll origin with the viewport's size, moved by
// the scroll offset so it stays over the viewport. Where the engine has scroll-driven animations
// (Chromium, Safari) CSS moves it and the thumb in step with the compositor's scrolling; elsewhere
// (Firefox) this module moves them on each scroll event.

const TOLERANCE = 0.5; // px out of view before an edge counts as scrolled (0024)
const HIDE_DELAY = 1000; // ms the thumb stays after the last scroll (0025)
const ZONE = 24; // px from the bar within which it reacts to the pointer
const THIN = 3; // thumb width while scrolling, pointer away
const FULL = 8; // pill width on the bar
const MIN_THUMB = 24;
const PAGE_DELAY = 300; // the track's page repeat, as the desktop RepeatButton
const PAGE_INTERVAL = 100;
const SETTLE_TIMEOUT = 100; // ms before scrolling counts when no frame has run

const scrollDriven = CSS.supports("animation-timeline: scroll()");
const states = new Map(); // element -> ScrollState

const clamp = (v, lo, hi) => Math.min(Math.max(v, lo), hi);
const ENDS = ["pointerup", "pointercancel", "lostpointercapture"];

// Pointer capture keeps a drag or a page repeat going outside the bar; a pointer that is no longer
// active (released before this runs) cannot be captured, and needs no capture.
function capture(el, pointerId) {
  try {
    el.setPointerCapture(pointerId);
  } catch {
    // not an active pointer
  }
}

function div(className, attributes = {}) {
  const el = document.createElement("div");
  el.className = className;
  for (const [k, v] of Object.entries(attributes)) el.setAttribute(k, v);
  return el;
}

class ScrollState {
  constructor(el) {
    this.el = el;
    this.overlay = div("ui-scroll-overlay", { "aria-hidden": "true" });
    this.shadows = {};
    for (const edge of ["top", "bottom", "left", "right"]) {
      this.shadows[edge] = div("ui-scroll-shadow", { "data-edge": edge });
      this.overlay.append(this.shadows[edge]);
    }
    this.bars = {};
    for (const axis of ["vertical", "horizontal"]) {
      const bar = div("ui-scroll-bar", { "data-orientation": axis, hidden: "" });
      const track = div("ui-scroll-track");
      const thumb = div("ui-scroll-thumb");
      bar.append(track, thumb);
      this.overlay.append(bar);
      this.bars[axis] = { bar, track, thumb, closeness: 0, hover: false, max: 0, travel: 0, thumbLength: 0, endInset: 0 };
      bar.addEventListener("pointerdown", (e) => this.#onBarDown(e, axis));
      thumb.addEventListener("pointerdown", (e) => this.#onThumbDown(e, axis));
    }
    this.scrolling = false;
    this.drag = null;
    this.page = null;
    this.live = false;
    this.rtl = false;

    el.append(this.overlay);
    el.setAttribute("data-scroll-overlay", "");
    el.addEventListener("scroll", this.#onScroll, { passive: true });
    el.addEventListener("pointermove", this.#onPointerMove);
    el.addEventListener("pointerleave", this.#onPointerLeave);
    this.resizeObserver = new ResizeObserver(() => this.measure());
    this.mutationObserver = new MutationObserver(() => this.#observeChildren());
    this.mutationObserver.observe(el, { childList: true, attributes: true, attributeFilter: ["data-scrollbar-auto-hide", "dir"] });
    this.#observeChildren(); // also measures
    // Offsets set while the page settles are not scrolling; fades start after it (the first frame,
    // or a short timer where no frames run, as in a background tab).
    const goLive = () => {
      if (this.live || states.get(el) !== this) return;
      this.live = true;
      this.overlay.setAttribute("data-live", "");
    };
    requestAnimationFrame(() => setTimeout(goLive));
    setTimeout(goLive, SETTLE_TIMEOUT);
  }

  destroy() {
    clearTimeout(this.hideTimer);
    this.#stopPaging();
    this.resizeObserver.disconnect();
    this.mutationObserver.disconnect();
    this.el.removeEventListener("scroll", this.#onScroll);
    this.el.removeEventListener("pointermove", this.#onPointerMove);
    this.el.removeEventListener("pointerleave", this.#onPointerLeave);
    this.el.removeAttribute("data-scroll-overlay");
    for (const name of ["--ui-scroll-client-width", "--ui-scroll-client-height", "--ui-scroll-max-x", "--ui-scroll-max-y"]) this.el.style.removeProperty(name);
    this.overlay.remove();
  }

  // The scroller and its content children; a child resizing changes the scroll size.
  #observeChildren() {
    this.resizeObserver.disconnect();
    this.resizeObserver.observe(this.el);
    for (const child of this.el.children) if (child !== this.overlay) this.resizeObserver.observe(child);
    if (this.overlay.parentNode !== this.el || this.el.lastElementChild !== this.overlay) this.el.append(this.overlay);
    this.measure();
  }

  measure() {
    const el = this.el;
    const style = getComputedStyle(el);
    this.rtl = style.direction === "rtl";
    const cw = el.clientWidth, ch = el.clientHeight;
    const maxX = Math.max(0, el.scrollWidth - cw), maxY = Math.max(0, el.scrollHeight - ch);
    el.style.setProperty("--ui-scroll-client-width", `${cw}px`);
    el.style.setProperty("--ui-scroll-client-height", `${ch}px`);
    el.style.setProperty("--ui-scroll-max-x", `${this.rtl ? -maxX : maxX}px`);
    el.style.setProperty("--ui-scroll-max-y", `${maxY}px`);
    const scrollable = (overflow) => overflow === "auto" || overflow === "scroll";
    for (const [axis, max, client, overflow] of [["vertical", maxY, ch, style.overflowY], ["horizontal", maxX, cw, style.overflowX]]) {
      const b = this.bars[axis];
      b.max = max;
      b.bar.hidden = !(max > TOLERANCE && scrollable(overflow));
      const track = getComputedStyle(b.track);
      const endInset = parseFloat(axis === "vertical" ? track.insetBlockStart : track.insetInlineStart) || 0;
      b.endInset = endInset;
      const length = Math.max(0, client - 2 * endInset);
      b.thumbLength = clamp(length * client / (client + max), Math.min(MIN_THUMB, length), length);
      b.travel = length - b.thumbLength;
      b.bar.style.setProperty("--ui-scroll-thumb-length", `${b.thumbLength}px`);
      b.bar.style.setProperty("--ui-scroll-thumb-travel", `${axis === "horizontal" && this.rtl ? -b.travel : b.travel}px`);
    }
    this.#update();
    this.render();
  }

  // How far the content runs past each edge.
  #hidden() {
    const el = this.el;
    const maxX = this.bars.horizontal.max, maxY = this.bars.vertical.max;
    const left = this.rtl ? maxX + el.scrollLeft : el.scrollLeft;
    return { top: el.scrollTop, bottom: maxY - el.scrollTop, left, right: maxX - left };
  }

  #update() {
    const hidden = this.#hidden();
    for (const edge of Object.keys(this.shadows)) this.shadows[edge].toggleAttribute("data-shown", hidden[edge] > TOLERANCE);
    if (scrollDriven) return;
    this.overlay.style.translate = `${this.el.scrollLeft}px ${this.el.scrollTop}px`;
    for (const [axis, offset] of [["vertical", this.el.scrollTop], ["horizontal", this.el.scrollLeft]]) {
      const b = this.bars[axis];
      const progress = b.max > 0 ? Math.abs(offset) / b.max : 0;
      const along = progress * b.travel * (axis === "horizontal" && this.rtl ? -1 : 1);
      b.thumb.style.translate = axis === "vertical" ? `0 ${along}px` : `${along}px 0`;
    }
  }

  // Width from the pointer's closeness (full on hover and drag); the thumb shows while scrolling,
  // near the pointer, or always with auto-hide off; the track shows with closeness.
  render() {
    const autoHideOff = this.el.getAttribute("data-scrollbar-auto-hide") === "false";
    for (const [axis, b] of Object.entries(this.bars)) {
      const dragging = this.drag?.axis === axis;
      const closeness = dragging || b.hover ? 1 : b.closeness;
      const shown = this.scrolling || autoHideOff ? 1 : 0;
      b.bar.style.setProperty("--ui-scroll-bar-width", `${THIN + (FULL - THIN) * closeness}px`);
      b.thumb.style.opacity = String(Math.max(shown, Math.min(1, 2 * closeness)));
      b.track.style.opacity = String(closeness);
      if (dragging) b.bar.setAttribute("data-state", "drag");
      else if (b.hover) b.bar.setAttribute("data-state", "hover");
      else b.bar.removeAttribute("data-state");
    }
  }

  #onScroll = () => {
    this.#update();
    if (!this.live || this.forced) return;
    this.scrolling = true;
    clearTimeout(this.hideTimer);
    this.hideTimer = setTimeout(() => {
      this.scrolling = false;
      this.render();
    }, HIDE_DELAY);
    this.render();
  };

  #onPointerMove = (e) => {
    if (e.pointerType === "touch") return;
    for (const b of Object.values(this.bars)) {
      if (b.bar.hidden) continue;
      const r = b.bar.getBoundingClientRect();
      const dx = Math.max(r.left - e.clientX, 0, e.clientX - r.right);
      const dy = Math.max(r.top - e.clientY, 0, e.clientY - r.bottom);
      const distance = Math.hypot(dx, dy);
      b.closeness = clamp(1 - distance / ZONE, 0, 1);
      b.hover = distance === 0;
    }
    this.render();
  };

  #onPointerLeave = () => {
    for (const b of Object.values(this.bars)) {
      b.closeness = 0;
      b.hover = false;
    }
    this.render();
  };

  #onThumbDown(e, axis) {
    if (e.button !== 0) return;
    e.preventDefault();
    e.stopPropagation();
    const thumb = this.bars[axis].thumb;
    const vertical = axis === "vertical";
    capture(thumb, e.pointerId);
    this.drag = { axis, start: vertical ? e.clientY : e.clientX, scroll: vertical ? this.el.scrollTop : this.el.scrollLeft };
    const move = (m) => {
      const b = this.bars[axis];
      const delta = (vertical ? m.clientY : m.clientX) - this.drag.start;
      const value = this.drag.scroll + (b.travel > 0 ? delta * b.max / b.travel : 0);
      if (vertical) this.el.scrollTop = value;
      else this.el.scrollLeft = value;
    };
    const end = () => {
      thumb.removeEventListener("pointermove", move);
      for (const type of ENDS) thumb.removeEventListener(type, end);
      this.drag = null;
      this.render();
    };
    thumb.addEventListener("pointermove", move);
    for (const type of ENDS) thumb.addEventListener(type, end);
    this.render();
  }

  // A press on the track pages towards the pointer, repeating while held, until the thumb reaches
  // it. Presses in the end zones (beyond the track) do nothing.
  #onBarDown(e, axis) {
    if (e.button !== 0) return;
    e.preventDefault();
    const b = this.bars[axis];
    const vertical = axis === "vertical";
    const flip = !vertical && this.rtl;
    // Along the bar from its start (the inline start for a horizontal bar), from the scroller's
    // geometry and scroll offset, not the drawn pills.
    const r = this.el.getBoundingClientRect();
    const along = vertical ? e.clientY - r.top - this.el.clientTop
      : flip ? r.left + this.el.clientLeft + this.el.clientWidth - e.clientX
      : e.clientX - r.left - this.el.clientLeft;
    const size = vertical ? this.el.clientHeight : this.el.clientWidth;
    if (along < b.endInset || along > size - b.endInset) return;
    capture(b.bar, e.pointerId);
    const step = () => {
      const offset = Math.abs(vertical ? this.el.scrollTop : this.el.scrollLeft);
      const start = b.endInset + (b.max > 0 ? offset / b.max : 0) * b.travel;
      const direction = along < start ? -1 : along > start + b.thumbLength ? 1 : 0;
      if (!direction) return this.#stopPaging();
      this.el.scrollBy({ [vertical ? "top" : "left"]: direction * size * (flip ? -1 : 1), behavior: "instant" });
    };
    this.#stopPaging();
    // Set before the first step, so a step that reaches the thumb stops the repeat it would start.
    this.page = { timer: setTimeout(() => { this.page.timer = setInterval(step, PAGE_INTERVAL); }, PAGE_DELAY) };
    step();
    const stop = () => {
      for (const type of ENDS) b.bar.removeEventListener(type, stop);
      this.#stopPaging();
    };
    for (const type of ENDS) b.bar.addEventListener(type, stop);
  }

  #stopPaging() {
    if (!this.page) return;
    clearTimeout(this.page.timer);
    clearInterval(this.page.timer);
    this.page = null;
  }
}

function scan() {
  scanQueued = false;
  const wanted = new Set(document.querySelectorAll(".ui-scroll"));
  for (const [el, state] of states) {
    if (!wanted.has(el)) {
      state.destroy();
      states.delete(el);
    }
  }
  for (const el of wanted) if (!states.has(el)) states.set(el, new ScrollState(el));
}

let scanQueued = false;
function scheduleScan() {
  if (scanQueued) return;
  scanQueued = true;
  queueMicrotask(scan);
}

function start() {
  new MutationObserver(scheduleScan).observe(document.documentElement, { subtree: true, childList: true, attributes: true, attributeFilter: ["class"] });
  scan();
}

if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", start, { once: true });
else start();

/**
 * Test and render hook (not exported from index.js). Attaches pending scrollers and re-measures
 * the element now, without waiting for the observers.
 */
export function refreshScroll(el) {
  scan();
  states.get(el)?.measure();
}

/**
 * Test and render hook (not exported from index.js). Forces a bar's state, as the pointer and
 * scrolling would set it: { scrolling, closeness (0-1), hover, drag }. The hide timer is not
 * started.
 */
export function forceScrollbarState(el, axis, { scrolling = false, closeness = 0, hover = false, drag = false } = {}) {
  scan();
  const state = states.get(el);
  if (!state) return;
  const b = state.bars[axis];
  clearTimeout(state.hideTimer);
  state.forced = true; // scroll events no longer change the forced state
  state.scrolling = scrolling;
  b.closeness = closeness;
  b.hover = hover;
  state.drag = drag ? { axis } : null;
  state.render();
}
