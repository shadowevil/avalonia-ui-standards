// The live theme controls in the catalog's toolbar band (0020, 0043): a <ui-toolbar-band> holding
// segmented controls for theme, density, motion, transparency and glass quality, and the accent as
// an icon drop-down showing the accent's chip, whose menu holds the presets (check items with their
// chips) and the hex box. Theme and accent lead, then density as a group; motion, transparency and
// glass quality sit in the trailing zone, as groups. As the band narrows they move into its "…"
// menu: glass quality, transparency, motion, density, accent, then theme. Motion and transparency
// lead with a decorative icon (the motion icon follows the chosen value).
import { icon, createMenu, menuItems, openMenu, tooltip } from "../ui.js";

export const ACCENTS = Object.freeze([
  ["Default", "#007CEA"],
  ["Teal", "#00899E"],
  ["Green", "#2E8B3E"],
  ["Orange", "#E0720B"],
  ["Purple", "#7A4FD6"],
  ["Pink", "#E91E63"],
]);

const TRI = [["Follow OS", "system"], ["Reduced", "true"], ["Full", "false"]];
const MOTION_ICONS = { system: "motion-system", true: "motion-reduced", false: "motion-full" };
const HEX = /^#[0-9A-Fa-f]{6}$/;

const triValue = (text) => (text === "true" ? true : text === "false" ? false : "system");
const triText = (value) => (value === true ? "true" : value === false ? "false" : "system");

function el(tag, className, ...children) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  node.append(...children);
  return node;
}

// A segmented control named `label` (also its tooltip and its "…" menu heading), at `value`.
function segmented(label, options, value) {
  const control = el("ui-segmented-control", "ui-squircle ui-focus-ring-within");
  control.setAttribute("aria-label", label);
  for (const [text, data] of options) {
    const segment = el("span", "ui-segmented-control-item ui-squircle", text);
    segment.dataset.value = data;
    segment.setAttribute("aria-checked", String(data === value));
    control.append(segment);
  }
  return control;
}

// A colour chip at the icon size, with a hairline edge and radius 4 (catalog.css).
function chip(hex) {
  const span = el("span", "ui-icon catalog-chip ui-squircle");
  span.setAttribute("aria-hidden", "true");
  span.style.backgroundColor = hex;
  return span;
}

/**
 * Fills `band` (a <ui-toolbar-band>) with the controls at `settings`; `onChange(key, value)` runs
 * on a choice. Returns the controls (for tests) and set(key, value), which shows a setting chosen
 * elsewhere (the View menu) without calling onChange.
 */
export function buildToolbar(band, settings, onChange) {
  const theme = segmented("Theme", [["System", "system"], ["Light", "light"], ["Dark", "dark"]], settings.theme);
  const density = segmented("Density", [["Normal", "normal"], ["Compact", "compact"]], settings.density);
  density.dataset.priority = "2";
  density.dataset.beginsGroup = "";

  const motion = segmented("Motion", TRI, triText(settings.reduceMotion));
  const motionItem = el("span", "ui-toolbar-band-item", icon("motion-system"), motion);
  Object.assign(motionItem.dataset, { zone: "trailing", priority: "3" });
  const showMotionIcon = () => motionItem.firstElementChild.replaceWith(icon(MOTION_ICONS[motion.value] ?? "motion-system"));

  const transparency = segmented("Transparency", TRI, triText(settings.reduceTransparency));
  const transparencyItem = el("span", "ui-toolbar-band-item", icon("drop"), transparency);
  Object.assign(transparencyItem.dataset, { zone: "trailing", priority: "4", beginsGroup: "" });

  const glass = segmented("Glass quality", [["Full", "full"], ["Minimal", "minimal"], ["Solid", "solid"]], settings.glassQuality);
  Object.assign(glass.dataset, { zone: "trailing", priority: "5", beginsGroup: "" });

  const accent = buildAccent(band, settings, onChange);

  band.append(theme, accent.button, accent.menu, density, motionItem, transparencyItem, glass);
  // Tooltips after the controls are in place (each follows its control).
  for (const control of [theme, density, motion, transparency, glass]) tooltip(control, control.getAttribute("aria-label"));
  tooltip(accent.button, "Accent colour");

  showMotionIcon();
  theme.addEventListener("change", () => onChange("theme", theme.value));
  density.addEventListener("change", () => onChange("density", density.value));
  motion.addEventListener("change", () => {
    showMotionIcon();
    onChange("reduceMotion", triValue(motion.value));
  });
  transparency.addEventListener("change", () => onChange("reduceTransparency", triValue(transparency.value)));
  glass.addEventListener("change", () => onChange("glassQuality", glass.value));

  const controls = { theme, density, reduceMotion: motion, reduceTransparency: transparency, glassQuality: glass };
  const set = (key, value) => {
    if (key === "accent") return accent.show(value);
    const control = controls[key];
    if (!control) return;
    control.value = key === "reduceMotion" || key === "reduceTransparency" ? triText(value) : value;
    if (key === "reduceMotion") showMotionIcon();
  };
  return { band, theme, density, motion, transparency, glass, ...accent, set };
}

// The accent drop-down and its menu: the presets as check items (one-of-many, kept here) with
// their chips, a separator, then the hex box, which stays open when clicked. In the band's "…"
// menu the hex box is "Custom colour…", which opens the whole menu at the band's trailing end.
function buildAccent(band, settings, onChange) {
  const swatch = chip(settings.accent);
  const button = el("button", "ui-button ui-subtle ui-drop-down-button ui-squircle ui-focus-ring", swatch);
  button.type = "button";
  button.setAttribute("aria-label", "Accent colour");
  button.dataset.priority = "1";

  const checkPreset = (hex) => {
    for (const [item, [, value]] of presets) item.setAttribute("aria-checked", String(value.toUpperCase() === hex.toUpperCase()));
  };
  // Shows an accent on the swatch, the presets and (unless it is being typed) the hex box.
  const show = (hex) => {
    const value = hex.toUpperCase();
    swatch.style.backgroundColor = value;
    checkPreset(value);
    if (document.activeElement !== hexInput) {
      hexInput.value = value;
      hexInput.removeAttribute("aria-invalid");
    }
  };
  const setAccent = (hex) => {
    show(hex);
    onChange("accent", hex.toUpperCase());
  };

  const menu = createMenu(ACCENTS.map(([name, hex]) => ({
    label: name,
    type: "checkbox",
    onSelect: () => {
      hexInput.value = hex;
      hexInput.removeAttribute("aria-invalid");
      setAccent(hex);
    },
  })), { label: "Accent colour", placement: "block-end-start" });
  const presets = menuItems(menu).map((item, i) => {
    const swatchIcon = chip(ACCENTS[i][1]);
    swatchIcon.classList.add("ui-menu-item-icon");
    item.querySelector(".ui-menu-item-header").before(swatchIcon);
    return [item, ACCENTS[i]];
  });

  // The hex box: a row the menu's keys reach (a group, so the field inside stays exposed); moving
  // onto it puts the caret in the box.
  const hexInput = el("input", "ui-text-input ui-squircle ui-focus-ring");
  hexInput.maxLength = 9;
  hexInput.spellcheck = false;
  hexInput.value = settings.accent;
  hexInput.setAttribute("aria-label", "Accent colour as #RRGGBB");
  // In the header column, so it lines up with the presets' names.
  hexInput.style.cssText = "inline-size: 120px; grid-column: 3";
  const hexItem = el("div", "ui-menu-item ui-list-item ui-squircle catalog-hex-item", hexInput);
  hexItem.setAttribute("role", "group");
  hexItem.setAttribute("aria-label", "Custom colour");
  hexItem.dataset.label = "Custom colour…";
  hexItem.tabIndex = -1;
  const separator = el("div", "ui-menu-separator");
  separator.setAttribute("role", "separator");
  menu.querySelector(".ui-flyout-content").append(separator, hexItem);
  tooltip(hexInput, "Accent colour as #RRGGBB");

  hexItem.addEventListener("focus", () => hexInput.focus({ preventScroll: true }));
  hexInput.addEventListener("input", () => {
    const text = hexInput.value.trim();
    if (!HEX.test(text)) {
      hexInput.setAttribute("aria-invalid", "true");
      return;
    }
    hexInput.removeAttribute("aria-invalid");
    setAccent(text);
  });
  // Up and Down leave the box for the presets, as in the rest of the menu.
  hexInput.addEventListener("keydown", (e) => {
    if (e.key !== "ArrowUp" && e.key !== "ArrowDown") return;
    e.preventDefault();
    const items = menuItems(menu).filter((i) => i !== hexItem && i.getAttribute("aria-disabled") !== "true");
    (e.key === "ArrowUp" ? items.at(-1) : items[0])?.focus({ preventScroll: true });
  });
  // A click in the row keeps the menu open; chosen from the "…" menu (the accent menu closed), the
  // whole accent menu opens at the band's trailing end once the "…" menu has closed.
  hexItem.addEventListener("click", (e) => {
    e.stopPropagation();
    if (menu.matches(":popover-open")) return;
    setTimeout(() => {
      menu.dataset.placement = "block-end-end";
      openMenu(menu, band.moreButton ?? band, { focus: "first" });
    });
  });
  menu.addEventListener("toggle", (e) => {
    if (e.newState === "closed") menu.dataset.placement = "block-end-start";
  });

  button.setAttribute("aria-haspopup", "menu");
  button.setAttribute("aria-expanded", "false");
  button.setAttribute("aria-controls", menu.id);
  checkPreset(settings.accent);
  return { button, menu, hexInput, hexItem, presets: presets.map(([item]) => item), show };
}
