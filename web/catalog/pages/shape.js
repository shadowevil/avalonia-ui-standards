// Shape: the corner radii drawn as squircles, at the theme's corner smoothing.
import { el, caption, page, section, watchTheme, cssName, token } from "./ui.js";

function sample(key) {
  const shape = el("div", "catalog-shape-sample ui-squircle");
  shape.style.setProperty("--ui-squircle-radius", `var(${cssName(key)})`);
  const value = caption();
  const item = el("div", "catalog-shape", shape, value);
  watchTheme(item, () => (value.textContent = `${key} ${Math.round(parseFloat(token(shape, key)))} px`));
  return item;
}

export default {
  id: "shape",
  title: "Shape",
  icon: "squares-four",
  render() {
    const smoothing = caption();
    const shapes = el("div", "catalog-shapes", sample("CornerRadiusControl"), sample("CornerRadiusOverlay"));
    const content = el("div", "catalog-stack", shapes, smoothing);
    watchTheme(content, () => (smoothing.textContent = `CornerSmoothing ${Number(token(content, "CornerSmoothing"))}`));
    return page("Shape", section("Corner radii", content));
  },
};
