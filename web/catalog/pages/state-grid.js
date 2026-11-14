// The state grid, as the desktop catalog's StateGrid: one row per variant, one column per state,
// each sample forced into its state (data-state hooks, 0048) and inert, then a live sample at the
// end of the row. A 150 px label column, 8 between columns, 4 between rows, each sample padded 12;
// it scrolls sideways when it cannot fit.
import { el, body, caption, sideScroll } from "./ui.js";

export const STATES = Object.freeze(["Rest", "Hover", "Pressed", "Disabled", "Focused"]);

/**
 * Forces `state` on a sample: hover and press on its button (a split button's primary half) or
 * on the sample itself, focus on the sample (its ring, or the ring of a composite), disabled on
 * every button in it, or aria-disabled on anything else.
 */
export function forceState(sample, state) {
  const buttons = sample.matches(".ui-button") ? [sample] : [...sample.querySelectorAll(".ui-button")];
  const target = buttons[0] ?? sample;
  if (state === "Hover") target.dataset.state = "hover";
  if (state === "Pressed") target.dataset.state = "pressed";
  if (state === "Focused") sample.dataset.state = "focus";
  if (state !== "Disabled") return;
  if (!buttons.length) return sample.setAttribute("aria-disabled", "true");
  for (const b of buttons) {
    if (b.localName === "a") {
      b.removeAttribute("href");
      b.setAttribute("role", "link");
      b.setAttribute("aria-disabled", "true");
    } else {
      b.disabled = true;
    }
  }
}

/**
 * variants: [label, create(label, state)] pairs; create returns a new sample. Options: states (the
 * state columns, default STATES) and force(sample, state) (default forceState).
 */
export function stateGrid(variants, { states = STATES, force = forceState } = {}) {
  const grid = el("div", "catalog-state-grid");
  grid.style.setProperty("--catalog-state-columns", states.length + 1);
  grid.append(el("div"));
  for (const s of [...states, "Live"]) grid.append(el("div", "catalog-state-caption", caption(s)));
  for (const [label, create] of variants) {
    grid.append(el("div", "catalog-state-label", body(label)));
    for (const s of states) {
      const sample = create(label, s);
      force(sample, s);
      const cell = el("div", "catalog-state-cell", sample);
      cell.inert = true;
      grid.append(cell);
    }
    grid.append(el("div", "catalog-state-cell", create(label, "Live")));
  }
  return sideScroll(grid);
}
