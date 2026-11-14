// The catalog's pages, in sidebar order (the desktop catalog's). Adding a page is one module in
// this folder and one line here. A page module's default export is { id, title, icon, render },
// where id names the route (#id) and the render (catalog-page-<id>-*.png), icon is a name from the
// library's icon set, and render() returns the page's element.
import colours from "./colours.js";
import type from "./type.js";
import spacing from "./spacing.js";
import shape from "./shape.js";
import glass from "./glass.js";
import motion from "./motion.js";
import button from "./button.js";
import buttonFamily from "./button-family.js";
import listBox from "./list-box.js";
import icon from "./icon.js";
import ranges from "./ranges.js";

export const pages = Object.freeze([
  colours,
  type,
  spacing,
  shape,
  glass,
  motion,
  button,
  buttonFamily,
  listBox,
  icon,
  ranges,
]);
