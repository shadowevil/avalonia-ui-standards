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
import textInput from "./text-input.js";
import toggles from "./toggles.js";
import segmented from "./segmented-control.js";
import ranges from "./ranges.js";
import tabs from "./tabs.js";
import expander from "./expander.js";
import menusAndFlyouts from "./menus-and-flyouts.js";
import tooltipNotification from "./tooltip-notification.js";
import separator from "./separator.js";
import comboBox from "./combobox.js";
import treeView from "./tree-view.js";
import numericUpDown from "./numeric-up-down.js";
import autoCompleteBox from "./auto-complete-box.js";
import pickers from "./pickers.js";
import dataGrid from "./data-grid.js";
import colorPicker from "./color-picker.js";

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
  textInput,
  comboBox,
  toggles,
  segmented,
  treeView,
  ranges,
  numericUpDown,
  autoCompleteBox,
  tabs,
  expander,
  menusAndFlyouts,
  tooltipNotification,
  pickers,
  separator,
  dataGrid,
  colorPicker,
]);
