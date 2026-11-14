// Ranges: ProgressBar and ProgressRing (the Slider joins with its Block).
import { el, page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

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
