// Ranges: Slider, ProgressBar and ProgressRing.
import { el, page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

// A slider 160 px long (100 when vertical), as the desktop page's.
function slider(value, { ticks = false, vertical = false } = {}) {
  return (label) => {
    const s = el("input", "ui-slider");
    s.type = "range";
    s.min = 0;
    s.max = 100;
    s.value = value;
    s.setAttribute("aria-label", label);
    if (ticks) {
      s.classList.add("ui-ticks-outside");
      s.dataset.tickFrequency = "10";
    }
    if (vertical) s.classList.add("ui-vertical");
    s.style.inlineSize = vertical ? "100px" : "160px";
    return s;
  };
}

// Hover, press and focus are forced on the input; disabled is its disabled attribute.
function forceSlider(sample, state) {
  const forced = { Hover: "hover", Pressed: "pressed", Focused: "focus" }[state];
  if (forced) sample.dataset.state = forced;
  if (state === "Disabled") sample.disabled = true;
}

function bar(value, { text = false } = {}) {
  return (label) => {
    const p = el("progress", "ui-progress-bar catalog-range-bar");
    if (value !== undefined) {
      p.max = 100;
      p.value = value;
    }
    p.setAttribute("aria-label", label);
    if (!text) return p;
    const t = el("span", "ui-progress-text", `${value}%`);
    t.setAttribute("aria-hidden", "true");
    return el("div", "ui-progress catalog-range-bar", p, t);
  };
}

function ring(value, size) {
  return (label) => {
    const p = el("progress", "ui-progress-ring");
    if (value !== undefined) {
      p.max = 100;
      p.value = value;
    }
    if (size) p.style.setProperty("--ui-progress-ring-size", `${size}px`);
    p.setAttribute("aria-label", label);
    return p;
  };
}

// Disabled is aria-disabled on the <progress> (in a text bar, the bar itself).
function force(sample, state) {
  const target = sample.matches("progress") ? sample : sample.querySelector("progress");
  if (state === "Disabled") target.setAttribute("aria-disabled", "true");
}

export default {
  id: "ranges",
  title: "Ranges",
  icon: "sliders-horizontal",
  render: () => page("Ranges",
    section("Slider", stateGrid([
      ["Value 40", slider(40)],
      ["With ticks", slider(60, { ticks: true })],
      ["Vertical", slider(70, { vertical: true })],
    ], { force: forceSlider })),
    section("ProgressBar", stateGrid([
      ["Value 40", bar(40)],
      ["With text", bar(40, { text: true })],
      ["Indeterminate", bar()],
    ], { force })),
    section("ProgressRing", stateGrid([
      ["Value 70", ring(70)],
      ["Indeterminate", ring()],
      ["32 px, indeterminate", ring(undefined, 32)],
    ], { force }))),
};
