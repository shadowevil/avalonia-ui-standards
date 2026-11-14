// Button: every variant in every state, and live buttons.
import { page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

function button(variant) {
  const b = document.createElement("button");
  b.type = "button";
  b.className = `ui-button ui-squircle ui-focus-ring ${variant}`.trim();
  b.textContent = "Button";
  return b;
}

export default {
  id: "button",
  title: "Button",
  icon: "cursor-click",
  render: () => page("Button", section("Variants and states", stateGrid([
    ["Standard", () => button("")],
    ["Accent", () => button("ui-accent")],
    ["Subtle", () => button("ui-subtle")],
    ["Destructive", () => button("ui-destructive")],
  ]))),
};
