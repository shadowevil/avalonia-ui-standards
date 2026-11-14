// Spacing: the scale as bars, each as wide as its value.
import { el, body, caption, page, section, watchTheme, cssName } from "./ui.js";

const STEPS = ["XS", "S", "M", "L", "XL", "XXL", "XXXL"];

function row(key) {
  const bar = el("span", "catalog-spacing-bar");
  bar.style.inlineSize = `var(${cssName(key)})`;
  const value = caption();
  const item = el("div", "catalog-spacing-row", body(key), value, bar);
  watchTheme(item, () => (value.textContent = `${Math.round(parseFloat(getComputedStyle(bar).inlineSize))} px`));
  return item;
}

export default {
  id: "spacing",
  title: "Spacing",
  icon: "ruler",
  render: () => page("Spacing", section("Scale", el("div", "catalog-stack", ...STEPS.map((s) => row("Spacing" + s))))),
};
