// ToggleSwitch knob drag (0030: drag stays Fluent's), delegated from the document so it works for
// switches added at any time. A primary-button press on an enabled .ui-toggle-switch, then a move
// of more than 3 px along the track, starts a drag: the knob follows the pointer, held within the
// track (data-dragging on the input, the offset in --ui-toggle-switch-drag). At release the switch
// is on when the knob is at or past the middle of its travel, firing input and change when that
// changes it, and the click the release would add does not flip it again. A smaller move stays a
// click. Clicks, Space and the label keep the browser's own behaviour. In right-to-left layouts
// the knob travels leftwards. After a drag the knob slides to its end over MotionNormal (the CSS
// transition, instant under reduce motion).

const SWITCH = ".ui-toggle-switch";
const THRESHOLD = 3; // px a press must move before it drags, as Fluent's ToggleSwitch

let drag = null; // { input, pointerId, startX, from, travel, dir, offset, dragging }

function end(commit) {
  if (!drag) return;
  const { input, pointerId, dragging, offset, travel } = drag;
  drag = null;
  if (input.hasPointerCapture?.(pointerId)) input.releasePointerCapture(pointerId);
  if (!dragging) return;
  input.removeAttribute("data-dragging");
  input.style.removeProperty("--ui-toggle-switch-drag");
  if (!commit) return;
  // Swallow the click the release adds (cancelling it keeps the state set here); if none comes,
  // drop the listener.
  const swallow = (e) => {
    e.preventDefault();
    e.stopImmediatePropagation();
  };
  input.addEventListener("click", swallow, { capture: true, once: true });
  setTimeout(() => input.removeEventListener("click", swallow, { capture: true }));
  const on = offset >= travel / 2;
  if (input.checked !== on) {
    input.checked = on;
    input.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
    input.dispatchEvent(new Event("change", { bubbles: true }));
  }
}

document.addEventListener("pointerdown", (e) => {
  const input = e.target.closest?.(SWITCH);
  if (!input || e.target !== input || e.button !== 0 || input.disabled) return;
  end(false);
  const rect = input.getBoundingClientRect();
  const travel = rect.width - rect.height;
  drag = {
    input, pointerId: e.pointerId, startX: e.clientX, travel,
    from: input.checked ? travel : 0,
    dir: getComputedStyle(input).direction === "rtl" ? -1 : 1,
    offset: input.checked ? travel : 0,
    dragging: false,
  };
  try {
    input.setPointerCapture(e.pointerId);
  } catch {
    // A pointer the browser no longer tracks; the document listeners still follow it.
  }
});

document.addEventListener("pointermove", (e) => {
  if (!drag || e.pointerId !== drag.pointerId) return;
  const dx = (e.clientX - drag.startX) * drag.dir;
  if (!drag.dragging) {
    if (Math.abs(dx) <= THRESHOLD) return;
    drag.dragging = true;
    drag.input.setAttribute("data-dragging", "");
  }
  drag.offset = Math.min(drag.travel, Math.max(0, drag.from + dx));
  drag.input.style.setProperty("--ui-toggle-switch-drag", `${drag.offset}px`);
});

document.addEventListener("pointerup", (e) => {
  if (drag && e.pointerId === drag.pointerId) end(true);
});
for (const type of ["pointercancel", "lostpointercapture"]) {
  document.addEventListener(type, (e) => {
    if (drag && e.pointerId === drag.pointerId) end(false);
  }, true);
}
