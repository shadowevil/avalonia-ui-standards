// Button family behaviour (0021) that native <button> lacks, delegated from the document so it
// works for buttons added at any time:
// - ToggleButton: a click on a .ui-button with aria-pressed flips it ("mixed" becomes "true")
//   before the page's own click handlers run.
// - RepeatButton: a .ui-repeat-button held down with the pointer or Space clicks again after
//   data-delay ms (default 300) and then every data-interval ms (default 100), as Avalonia's
//   RepeatButton. A click is still a single click; after repeats, the release adds none.

const DELAY = 300;
const INTERVAL = 100;

function toggle(event) {
  const button = event.target.closest?.(".ui-button[aria-pressed]");
  if (!button || button.disabled || button.getAttribute("aria-disabled") === "true") return;
  button.setAttribute("aria-pressed", button.getAttribute("aria-pressed") === "true" ? "false" : "true");
}

let held = null; // { button, timer, repeated }

function stop() {
  if (!held) return;
  clearTimeout(held.timer);
  const { button, repeated } = held;
  held = null;
  if (repeated) {
    // Swallow the click the release would add; if none comes, drop the listener.
    const swallow = (e) => e.stopImmediatePropagation();
    button.addEventListener("click", swallow, { capture: true, once: true });
    setTimeout(() => button.removeEventListener("click", swallow, { capture: true }));
  }
}

function start(button) {
  stop();
  const ms = (name, fallback) => {
    const n = Number(button.dataset[name]);
    return Number.isFinite(n) && n > 0 ? n : fallback;
  };
  const interval = ms("interval", INTERVAL);
  const tick = () => {
    if (!held || held.button !== button || button.disabled) return stop();
    held.repeated = true;
    button.click();
    held.timer = setTimeout(tick, interval);
  };
  held = { button, repeated: false, timer: setTimeout(tick, ms("delay", DELAY)) };
}

function repeatButton(target) {
  const button = target.closest?.(".ui-repeat-button");
  return button && !button.disabled && button.getAttribute("aria-disabled") !== "true" ? button : null;
}

document.addEventListener("click", toggle, { capture: true });

document.addEventListener("pointerdown", (e) => {
  if (e.button !== 0) return;
  const button = repeatButton(e.target);
  if (button) start(button);
});
document.addEventListener("keydown", (e) => {
  if (e.key !== " " || e.repeat) return;
  const button = repeatButton(e.target);
  if (button) start(button);
});
for (const type of ["pointerup", "pointercancel", "keyup", "blur"]) document.addEventListener(type, stop, true);
document.addEventListener("pointerout", (e) => {
  if (held && e.target === held.button && !held.button.contains(e.relatedTarget)) stop();
});
