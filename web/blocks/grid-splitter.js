// GridSplitter (0039): the splitter resizes the two grid tracks beside it, by dragging or by the
// arrow keys (Left and Right between columns, swapped right to left; Up and Down between rows),
// 10 px a press as Fluent's KeyboardIncrement. Escape during a drag puts the tracks back. The two
// tracks keep their share as a proportion (minmax(0, Nfr)), as star sizes do on desktop; the other
// tracks keep the grid's inline template when it has one, else their current sizes.
//
// It sets on each .ui-grid-splitter: role="separator", aria-orientation ("vertical" between
// columns, when the handle is taller than wide), aria-valuemin 0, aria-valuemax 100 and
// aria-valuenow (the first track's share of the two, in percent), and tabindex 0, or -1 while
// aria-disabled="true".
const SELECTOR = ".ui-grid-splitter";
const STEP = 10;
const adopted = new WeakSet();

const columns = (splitter) => splitter.getAttribute("aria-orientation") !== "horizontal";
const disabled = (splitter) => splitter.getAttribute("aria-disabled") === "true";
const rtl = (splitter) => getComputedStyle(splitter).direction === "rtl";

// The grid's tracks on the splitter's axis (resolved sizes in px), the template property, and the
// index of the splitter's own track.
function tracks(splitter) {
  const grid = splitter.parentElement;
  if (!grid) return null;
  const cols = columns(splitter);
  const property = cols ? "gridTemplateColumns" : "gridTemplateRows";
  const sizes = getComputedStyle(grid)[property].split(/\s+/).map(parseFloat);
  if (sizes.some((n) => !Number.isFinite(n)) || sizes.length < 3) return null;
  const gap = parseFloat(getComputedStyle(grid)[cols ? "columnGap" : "rowGap"]) || 0;
  const gridBox = grid.getBoundingClientRect();
  const box = splitter.getBoundingClientRect();
  const s = getComputedStyle(grid);
  // The splitter's offset from the grid's content edge, along the axis in track order.
  let offset;
  if (cols) {
    offset = rtl(grid)
      ? gridBox.right - parseFloat(s.borderRightWidth) - parseFloat(s.paddingRight) - box.right
      : box.left - gridBox.left - parseFloat(s.borderLeftWidth) - parseFloat(s.paddingLeft);
  } else {
    offset = box.top - gridBox.top - parseFloat(s.borderTopWidth) - parseFloat(s.paddingTop);
  }
  // Its track starts where it does and is as thick (an empty track beside it may start there too).
  const thickness = cols ? box.width : box.height;
  let start = 0;
  let index = -1;
  for (let i = 0; i < sizes.length; i++) {
    if (Math.abs(start - offset) < 1 && Math.abs(sizes[i] - thickness) < 1) { index = i; break; }
    start += sizes[i] + gap;
  }
  if (index < 1 || index >= sizes.length - 1) return null;
  return { grid, property, sizes, index };
}

function updateValue(splitter) {
  const t = tracks(splitter);
  if (!t) return;
  const before = t.sizes[t.index - 1];
  const after = t.sizes[t.index + 1];
  const total = before + after;
  splitter.setAttribute("aria-valuenow", String(total > 0 ? Math.round((before / total) * 100) : 50));
}

// Sets the tracks before and after the splitter to `before` px of their combined size.
function apply(splitter, t, before) {
  const total = t.sizes[t.index - 1] + t.sizes[t.index + 1];
  const first = Math.min(total, Math.max(0, before));
  const inline = t.grid.style[t.property].trim().split(/\s+(?![^(]*\))/);
  const template = (inline.length === t.sizes.length ? inline : t.sizes.map((n) => `${n}px`)).slice();
  template[t.index - 1] = `minmax(0, ${first}fr)`;
  template[t.index + 1] = `minmax(0, ${total - first}fr)`;
  t.grid.style[t.property] = template.join(" ");
  updateValue(splitter);
}

function onKeyDown(event) {
  const splitter = event.currentTarget;
  if (disabled(splitter) || event.altKey || event.ctrlKey || event.metaKey) return;
  const cols = columns(splitter);
  const keys = cols
    ? (rtl(splitter) ? { ArrowLeft: 1, ArrowRight: -1 } : { ArrowLeft: -1, ArrowRight: 1 })
    : { ArrowUp: -1, ArrowDown: 1 };
  const direction = keys[event.key];
  if (!direction) return;
  event.preventDefault();
  const t = tracks(splitter);
  if (t) apply(splitter, t, t.sizes[t.index - 1] + direction * STEP);
}

function onPointerDown(event) {
  const splitter = event.currentTarget;
  if (event.button !== 0 || disabled(splitter)) return;
  const t = tracks(splitter);
  if (!t) return;
  const cols = columns(splitter);
  const sign = cols && rtl(splitter) ? -1 : 1;
  const origin = cols ? event.clientX : event.clientY;
  const template = t.grid.style[t.property];
  const start = t.sizes[t.index - 1];
  splitter.setPointerCapture(event.pointerId);
  const move = (e) => apply(splitter, t, start + sign * ((cols ? e.clientX : e.clientY) - origin));
  const cancel = (e) => {
    if (e.key !== "Escape") return;
    t.grid.style[t.property] = template;
    updateValue(splitter);
    end();
  };
  const end = () => {
    splitter.removeEventListener("pointermove", move);
    splitter.removeEventListener("pointerup", end);
    splitter.removeEventListener("pointercancel", end);
    splitter.removeEventListener("keydown", cancel);
    if (splitter.hasPointerCapture(event.pointerId)) splitter.releasePointerCapture(event.pointerId);
  };
  splitter.addEventListener("pointermove", move);
  splitter.addEventListener("pointerup", end);
  splitter.addEventListener("pointercancel", end);
  splitter.addEventListener("keydown", cancel);
}

function update(splitter) {
  if (!splitter.hasAttribute("role")) splitter.setAttribute("role", "separator");
  if (!splitter.hasAttribute("aria-orientation")) {
    const box = splitter.getBoundingClientRect();
    if (box.width || box.height) splitter.setAttribute("aria-orientation", box.height >= box.width ? "vertical" : "horizontal");
  }
  splitter.setAttribute("aria-valuemin", "0");
  splitter.setAttribute("aria-valuemax", "100");
  splitter.tabIndex = disabled(splitter) ? -1 : 0;
  updateValue(splitter);
}

function adopt(splitter) {
  if (!adopted.has(splitter)) {
    adopted.add(splitter);
    splitter.addEventListener("keydown", onKeyDown);
    splitter.addEventListener("pointerdown", onPointerDown);
    resize.observe(splitter);
  }
  update(splitter);
}

function adoptWithin(root) {
  if (root.matches?.(SELECTOR)) adopt(root);
  root.querySelectorAll?.(SELECTOR).forEach(adopt);
}

// Layout changes move the tracks (and give a splitter added before layout its orientation).
const resize = new ResizeObserver((entries) => entries.forEach((e) => update(e.target)));

const observer = new MutationObserver((records) => {
  for (const record of records) {
    if (record.type === "attributes") {
      if (record.target.matches(SELECTOR)) update(record.target);
    }
    else record.addedNodes.forEach((node) => node.nodeType === 1 && adoptWithin(node));
  }
});

function start() {
  adoptWithin(document);
  observer.observe(document, { subtree: true, childList: true, attributes: true, attributeFilter: ["aria-disabled"] });
}

if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", start, { once: true });
else start();
