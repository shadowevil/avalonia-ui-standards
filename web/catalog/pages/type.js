// Type: the ramp, each style set in its own size and weight, with the values read from the theme.
import { el, caption, page, section, watchTheme } from "./ui.js";

const RAMP = [
  ["Caption", "caption"], ["Body", "body"], ["Body strong", "body-strong"], ["Subtitle", "subtitle"],
  ["Title", "title"], ["Large title", "large-title"], ["Display", "display"],
];

function row(name, style) {
  const sample = el("span", "catalog-type-sample", name);
  sample.style.fontSize = `var(--ui-font-size-${style})`;
  // Caption and Body have no weight key: they use the regular weight.
  if (style !== "caption" && style !== "body") sample.style.fontWeight = `var(--ui-font-weight-${style})`;
  const detail = caption();
  const item = el("div", "catalog-type-row", detail, sample);
  watchTheme(item, () => {
    const s = getComputedStyle(sample);
    detail.textContent = `${Math.round(parseFloat(s.fontSize))} px, weight ${s.fontWeight}`;
  });
  return item;
}

export default {
  id: "type",
  title: "Type",
  icon: "text-aa",
  render: () => page("Type", section("Ramp", el("div", "catalog-stack", ...RAMP.map(([n, s]) => row(n, s))))),
};
