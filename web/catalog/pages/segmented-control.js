// Segmented control: text and icon segments in every state (hover and press on "Week"), and live
// controls.
import { icon } from "../../ui.js";
import { el, caption, page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

function control(label, segments) {
  const c = el("ui-segmented-control", "ui-squircle ui-focus-ring-within", ...segments);
  c.setAttribute("aria-label", label);
  segments[0].setAttribute("aria-checked", "true");
  return c;
}

const segment = (...content) => el("span", "ui-segmented-control-item ui-squircle", ...content);

const text = () => control("Range", ["Day", "Week", "Month", "Year"].map((t) => segment(t)));

function glyphs() {
  return control("View", [["List", "list-bullets"], ["Grid", "squares-four"], ["Sidebar", "sidebar-simple"]].map(([name, key]) => {
    const s = segment(icon(key));
    s.setAttribute("aria-label", name);
    s.title = name;
    return s;
  }));
}

function lastDisabled() {
  const c = text();
  c.lastElementChild.setAttribute("aria-disabled", "true");
  return c;
}

/** Hover and press on the second segment, focus on the control, disabled on the control. */
function force(sample, state) {
  const second = sample.querySelectorAll(".ui-segmented-control-item")[1];
  if (state === "Hover") second.dataset.state = "hover";
  if (state === "Pressed") second.dataset.state = "pressed";
  if (state === "Focused") sample.dataset.state = "focus";
  if (state === "Disabled") sample.setAttribute("disabled", "");
}

export default {
  id: "segmented",
  title: "Segmented",
  icon: "radio-button",
  render: () => page("Segmented",
    section("States", stateGrid([
      ["Text", text],
      ["Icons", glyphs],
      ["Segment disabled", lastDisabled],
    ], { force })),
    section("Use", caption(
      "One of a few options for one setting, about five to seven at most; text or icons, not mixed. Icon segments carry a tooltip " +
      "and a name. Tab reaches the control once; the arrow keys, Home and End move and select."))),
};
