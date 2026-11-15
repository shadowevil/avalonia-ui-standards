// AutoCompleteBox: the field in every state, a live field, and the suggestions open.
import { el, caption, page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

const STATES = ["Rest", "Hover", "Focused", "Keyboard focus", "Error", "Disabled"];
const CITIES = ["Amsterdam", "Athens", "Auckland", "Austin", "Berlin", "Boston", "Brussels", "Budapest"];

function box(label, { value = "", placeholder = "" } = {}) {
  const input = el("input", "ui-text-input");
  input.value = value;
  input.placeholder = placeholder;
  input.setAttribute("aria-label", label);
  const b = el("ui-auto-complete-box", "ui-field ui-auto-complete-box ui-squircle ui-focus-ring-within", input);
  b.items = CITIES;
  b.style.inlineSize = "220px";
  return b;
}

function force(sample, state) {
  const input = sample.querySelector("input");
  if (state === "Hover") sample.dataset.state = "hover";
  if (state === "Focused" || state === "Keyboard focus") sample.dataset.state = "focus";
  if (state === "Focused") sample.setAttribute("data-pointer-focus", "");
  if (state === "Error") input.setAttribute("aria-invalid", "true");
  if (state === "Disabled") input.disabled = true;
}

// The suggestions as they look open: the field typed with "A", the second suggestion hovered, the
// popup shown in place (without the popover attribute) under the field.
function open() {
  const field = box("City", { value: "A" });
  field.dataset.state = "focus";
  field.setAttribute("data-pointer-focus", "");
  const items = CITIES.slice(0, 4).map((c, i) => {
    const item = el("div", "ui-list-item ui-squircle ui-focus-ring", c);
    if (i === 1) item.dataset.state = "hover";
    return item;
  });
  const popup = el("div", "ui-flyout ui-combo-box-popup ui-auto-complete-popup ui-squircle", el("ui-list-box", "", ...items));
  popup.style.inlineSize = "220px";
  popup.style.marginBlockStart = "2px";
  const wrap = el("div", "", field, popup);
  Object.assign(wrap.style, { display: "flex", flexDirection: "column", alignItems: "flex-start", inlineSize: "max-content" });
  wrap.inert = true;
  return wrap;
}

export default {
  id: "autocompletebox",
  title: "AutoCompleteBox",
  icon: "magnifying-glass",
  render: () => page("AutoCompleteBox",
    section("States", stateGrid([
      ["Placeholder", (l) => box(l, { placeholder: "City" })],
      ["Text", (l) => box(l, { value: "Austin" })],
    ], { states: STATES, force })),
    section("Suggestions open", open()),
    caption("Type in a live field to open its suggestions; they use the ComboBox drop-down and list items.")),
};
