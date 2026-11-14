// Motion: the durations; a marker slides along a track with the theme's duration and easing.
import { el, body, caption, page, section, watchTheme, cssName, token } from "./ui.js";

const STEPS = ["Fast", "Normal", "Slow"];

function row(key) {
  const dot = el("span", "catalog-motion-dot");
  dot.style.transitionDuration = `var(${cssName(key)})`;
  const track = el("span", "catalog-motion-track", dot);
  const play = el("button", "ui-button ui-squircle ui-focus-ring", "Play");
  play.type = "button";
  play.setAttribute("aria-label", `Play ${key}`);
  // The duration and easing are the tokens' at the moment of the click, so band changes apply.
  play.addEventListener("click", () => dot.classList.toggle("catalog-motion-end"));
  const value = caption();
  const item = el("div", "catalog-motion-row", body(key), value, track, play);
  watchTheme(item, () => (value.textContent = `${Math.round(parseFloat(token(item, key)))} ms`));
  return item;
}

export default {
  id: "motion",
  title: "Motion",
  icon: "play",
  render: () => page("Motion", section("Durations", el("div", "catalog-stack catalog-motion", ...STEPS.map((s) => row("Motion" + s))))),
};
