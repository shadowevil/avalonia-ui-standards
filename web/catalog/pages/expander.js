// Expander: closed and open in every state (on the header), and live expanders.
import { el, body, caption, page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

// A column of children, `gap` apart.
function stack(gap, ...children) {
  const s = el("div", "", ...children);
  s.style.cssText = `display: flex; flex-direction: column; gap: ${gap}`;
  return s;
}

function expander(open, width) {
  const content = el("div", "ui-expander-content",
    stack("var(--ui-spacing-s)", body("Keep a history of changes"), caption("The content reveals below the header.")));
  const e = el("details", "ui-expander ui-squircle", el("summary", "ui-expander-header", "Advanced options"), content);
  e.open = open;
  if (width) e.style.inlineSize = `${width}px`;
  return e;
}

// Hover and press are forced on the header, focus on the card (its ring), disabled on the card.
function force(sample, state) {
  const header = sample.querySelector("summary");
  if (state === "Hover") header.dataset.state = "hover";
  if (state === "Pressed") header.dataset.state = "pressed";
  if (state === "Focused") sample.dataset.state = "focus";
  if (state === "Disabled") sample.setAttribute("aria-disabled", "true");
}

export default {
  id: "expander",
  title: "Expander",
  icon: "caret-down",
  render: () => page("Expander",
    section("States", stateGrid([
      ["Closed", () => expander(false, 240)],
      ["Open", () => expander(true, 240)],
    ], { force })),
    section("Live", stack("var(--ui-spacing-s)", expander(false), expander(true)))),
};
