// ColorPicker: a live picker with a swatch of its colour and the hex value, the field in every
// state, and a ColorView on its own (the desktop catalog's ColorPickerPage).
import "../../blocks/color-picker.js";
import "../../blocks/color-view.js";
import { el, body, caption, page, section, sideScroll, hex } from "./ui.js";
import { stateGrid } from "./state-grid.js";

const SAMPLE = "#007cea";
const TRANSLUCENT = "#e1332a80";

function picker(value, label) {
  const node = document.createElement("ui-color-picker");
  node.setAttribute("value", value);
  node.setAttribute("aria-label", label);
  return node;
}

// Forced states go on the element, which passes them to its field; pressed has the hover look.
function force(sample, state) {
  if (state === "Hover") sample.dataset.state = "hover";
  if (state === "Pressed") sample.dataset.state = "pressed";
  if (state === "Focused") sample.dataset.state = "focus";
  if (state === "Disabled") sample.setAttribute("disabled", "");
}

// A picker and a swatch that shows its colour and hex value (read from the swatch, as desktop).
function live() {
  const control = picker(SAMPLE, "Colour");
  const swatch = el("span", "ui-squircle");
  Object.assign(swatch.style, {
    "--ui-squircle-radius": "var(--ui-corner-radius-control)",
    display: "inline-block", inlineSize: "64px", blockSize: "32px",
  });
  const text = body("");
  const show = () => {
    swatch.style.backgroundColor = control.value;
    text.textContent = hex(getComputedStyle(swatch).backgroundColor);
  };
  control.addEventListener("input", show);
  queueMicrotask(show);
  const row = el("div", "", control, swatch, text);
  Object.assign(row.style, { display: "flex", alignItems: "center", gap: "var(--ui-spacing-m)" });
  return row;
}

function view() {
  const node = document.createElement("ui-color-view");
  node.setAttribute("value", SAMPLE);
  node.setAttribute("aria-label", "Colour");
  return sideScroll(node);
}

export default {
  id: "colorpicker",
  title: "ColorPicker",
  icon: "eyedropper",
  render: () => page("ColorPicker",
    section("Live", live()),
    section("States", stateGrid([
      ["Opaque", (l) => picker(SAMPLE, l)],
      ["Translucent", (l) => picker(TRANSLUCENT, l)],
    ], { force })),
    section("ColorView", view()),
    caption("The field opens the ColorView below it in a glass flyout. Tabs switch between the spectrum, the palette and the components; "
      + "RGB and HSV are a segmented control. The spectrum, the sliders and the palette take the arrow keys and show the focus ring.")),
};
