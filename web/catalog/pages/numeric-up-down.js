// NumericUpDown and ButtonSpinner: every kind in every state, and live fields.
import { el, caption, page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

const STATES = ["Rest", "Hover", "Focused", "Keyboard focus", "Error", "Disabled"];
const SIZES = ["Small", "Medium", "Large"];

function numeric(label, { value = "", placeholder = "", min, max, left = false } = {}) {
  const input = el("input", "ui-text-input");
  input.type = "number";
  input.value = value;
  input.placeholder = placeholder;
  input.setAttribute("aria-label", label);
  if (min !== undefined) input.min = min;
  if (max !== undefined) input.max = max;
  const n = el("ui-numeric-up-down", "ui-field ui-numeric-up-down ui-squircle ui-focus-ring-within", input);
  if (left) n.setAttribute("button-spinner-location", "left");
  n.style.inlineSize = "200px";
  return n;
}

function spinner(label, value) {
  const content = el("span", "ui-button-spinner-content", value);
  const s = el("ui-button-spinner", "ui-field ui-button-spinner ui-squircle ui-focus-ring ui-focus-ring-within", content);
  s.setAttribute("aria-label", label);
  s.style.inlineSize = "200px";
  // A list of sizes: the page updates the content on spin, and the limits follow.
  const show = (i) => {
    content.textContent = SIZES[i];
    s.validSpinDirection = i === SIZES.length - 1 ? "decrease" : i === 0 ? "increase" : "both";
  };
  s.addEventListener("spin", (e) => {
    const i = SIZES.indexOf(content.textContent) + (e.detail.direction === "increase" ? 1 : -1);
    if (i >= 0 && i < SIZES.length) show(i);
  });
  return s;
}

// The element holds hover and focus; the input (NumericUpDown) or the element holds error and disabled.
function force(sample, state) {
  const target = sample.querySelector("input") ?? sample;
  if (state === "Hover") sample.dataset.state = "hover";
  if (state === "Focused" || state === "Keyboard focus") sample.dataset.state = "focus";
  if (state === "Focused") sample.setAttribute("data-pointer-focus", "");
  if (state === "Error") target.setAttribute("aria-invalid", "true");
  if (state === "Disabled") target.toggleAttribute("disabled", true);
}

export default {
  id: "numericupdown",
  title: "NumericUpDown",
  icon: "plus",
  render: () => page("NumericUpDown",
    section("Kinds and states", stateGrid([
      ["NumericUpDown", (l) => numeric(l, { value: "42" })],
      ["Placeholder", (l) => numeric(l, { placeholder: "Quantity" })],
      ["Buttons left", (l) => numeric(l, { value: "42", left: true })],
      ["At its maximum", (l) => numeric(l, { value: "10", max: "10" })],
      ["ButtonSpinner", (l) => spinner(l, "Medium")],
    ], { states: STATES, force })),
    caption("The spin buttons are field buttons with caret-up and caret-down icons; the Up and Down keys also spin.")),
};
