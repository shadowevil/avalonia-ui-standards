// ColorView (0041, 0046, 0048): <ui-color-view>, a colour chooser: icon tabs (spectrum, palette,
// components) over pages of one size, then the previewer. It builds its parts from earlier Blocks:
// the tabs (tabs.js), the ColorSpectrum (color-spectrum.js), ColorSliders (native ranges with
// ui-slider ui-color-slider), the ListBox (list-box.js) for the palette, the segmented control
// (segmented-control.js) for the colour model, TextInput fields (text-input.js) for the hex and
// component values, and tooltips (tooltip.js) for the icon tabs and palette colours.
//
//   <ui-color-view value="#007cea" name="colour" aria-label="Colour"></ui-color-view>
//
// No native element is a colour chooser that stays on the page (<input type="color"> opens the
// browser's own dialog), so it is a custom element, form-associated with the native colour
// input's contract: value is "#rrggbb", or "#rrggbbaa" when the colour is not opaque (CSS's hex
// with alpha last, as Fluent's hex box); the value attribute is the initial value (a form reset goes
// back to it; once the user or the value property changes the value it no longer applies); input
// fires on every change by the user and change when a change is committed (a key, the end of a
// drag, a chosen palette colour or accent chip, a typed value taken up). The events of the parts
// stay inside. The default colour is white, as Fluent's.
//
// Pages (0041): the spectrum page is the HSV value slider, the spectrum and the alpha slider; the
// palette page Fluent's palette (the Windows colours, 6 columns) in a ListBox, the colour under
// the keyboard selected, with the grid keys (Left and Right along a row, swapped right to left,
// Up and Down along a column, Home and End, stopping at the edges, always handled); the
// components page the colour model (RGB or HSV) and the hex value, then a row per component
// (letter, number box, slider). Component ranges as Fluent's: RGB and alpha 0-255 in RGB; hue
// 0-360, saturation, value and alpha 0-100 in HSV. The hex box takes 3, 4, 6 or 8 hex digits,
// with or without "#", on Enter or when it loses focus; anything else puts the colour's text back.
// The previewer shows the colour with two darker and two lighter accent chips (the value 10 %
// apart), which pick their colour on a click; it is decoration for assistive technology.
// The accent chips have no keyboard path: the previewer is aria-hidden and not a Tab stop (as on
// the desktop), and the same colours are reached with the value slider, which moves only the value.
//
// Colour names (the palette's names and tooltips, the spectrum's value text) come from colorName,
// a function from { r, g, b, a } to a name; the default is displayName (English, Fluent's names).
// An app in another language sets its own: view.colorName = (c) => names[displayName(c)].
//
// Attributes: value, name, disabled, color-model ("rgb", the default, or "hsv"), spectrum-shape
// ("box" or "ring"), and the strings (0041's string resources, English defaults; see STRINGS),
// each an attribute named after its key: spectrum-text, palette-text, …, red-letter, ….
// Properties: value, color ({ r, g, b, a }, 0-255), hsv ({ h, s, v, a }, h 0-360, others 0-1),
// colorModel, selectedIndex (the page), colorName, parts.
import { icon } from "./icon.js";
import "./tabs.js";
import "./list-box.js";
import "./segmented-control.js";
import "./slider.js";
import { syncField } from "./text-input.js";
import { tooltip, tooltipOf } from "./tooltip.js";
import { el } from "./date-picker.js";
import "./color-spectrum.js";

/** The view's shown and spoken strings (0041), by attribute, with their English defaults. */
export const STRINGS = Object.freeze({
  "spectrum-text": "Spectrum",
  "palette-text": "Palette",
  "components-text": "Components",
  "rgb-text": "RGB",
  "hsv-text": "HSV",
  "hex-text": "Hexadecimal colour",
  "red-text": "Red",
  "green-text": "Green",
  "blue-text": "Blue",
  "hue-text": "Hue",
  "saturation-text": "Saturation",
  "value-text": "Value",
  "alpha-text": "Alpha",
  "red-letter": "R",
  "green-letter": "G",
  "blue-letter": "B",
  "hue-letter": "H",
  "saturation-letter": "S",
  "value-letter": "V",
  "alpha-letter": "A",
  "color-model-text": "Colour model",
});

// --- Colour maths ----------------------------------------------------------------------------

const clamp = (v, lo, hi) => Math.min(hi, Math.max(lo, v));
const byte = (n) => clamp(Math.round(n), 0, 255);

/** HSV (h 0-360, s, v and a 0-1) to { r, g, b, a } bytes. */
export function hsvToRgb({ h, s, v, a = 1 }) {
  const c = v * s;
  const h1 = (((h % 360) + 360) % 360) / 60;
  const x = c * (1 - Math.abs((h1 % 2) - 1));
  const m = v - c;
  const [r, g, b] = h1 < 1 ? [c, x, 0] : h1 < 2 ? [x, c, 0] : h1 < 3 ? [0, c, x] : h1 < 4 ? [0, x, c] : h1 < 5 ? [x, 0, c] : [c, 0, x];
  return { r: byte((r + m) * 255), g: byte((g + m) * 255), b: byte((b + m) * 255), a: byte(a * 255) };
}

/** { r, g, b, a } bytes to HSV (h 0-360, s, v and a 0-1). */
export function rgbToHsv({ r, g, b, a = 255 }) {
  const [rf, gf, bf] = [r / 255, g / 255, b / 255];
  const max = Math.max(rf, gf, bf);
  const chroma = max - Math.min(rf, gf, bf);
  let h = 0;
  if (chroma > 0) {
    if (max === rf) h = 60 * (((gf - bf) / chroma + 6) % 6);
    else if (max === gf) h = 60 * ((bf - rf) / chroma + 2);
    else h = 60 * ((rf - gf) / chroma + 4);
  }
  return { h, s: max === 0 ? 0 : chroma / max, v: max, a: a / 255 };
}

/** A colour from 3, 4, 6 or 8 hex digits (alpha last), with or without "#", or null. */
export function parseColor(text) {
  const m = /^#?([0-9a-f]{3,4}|[0-9a-f]{6}|[0-9a-f]{8})$/i.exec(String(text ?? "").trim());
  if (!m) return null;
  let digits = m[1];
  if (digits.length <= 4) digits = [...digits].map((d) => d + d).join("");
  if (digits.length === 6) digits += "ff";
  const n = (i) => parseInt(digits.slice(i, i + 2), 16);
  return { r: n(0), g: n(2), b: n(4), a: n(6) };
}

const hex2 = (n) => n.toString(16).padStart(2, "0");

/** The value text of a colour: "#rrggbb", or "#rrggbbaa" when not opaque. */
export function formatColor({ r, g, b, a = 255 }) {
  return `#${hex2(r)}${hex2(g)}${hex2(b)}${a === 255 ? "" : hex2(a)}`;
}

/** The colour as a CSS colour. */
export const cssColor = ({ r, g, b, a = 255 }) => `rgb(${r} ${g} ${b} / ${+(a / 255).toFixed(4)})`;

// The named colours (Avalonia's KnownColor, the CSS colour names) for display names. Of two names
// for one colour the one Avalonia shows is kept (Aqua, not Cyan; Magenta, not Fuchsia).
const KNOWN = "AliceBlue F0F8FF AntiqueWhite FAEBD7 Aqua 00FFFF Aquamarine 7FFFD4 Azure F0FFFF Beige F5F5DC Bisque FFE4C4 Black 000000 "
  + "BlanchedAlmond FFEBCD Blue 0000FF BlueViolet 8A2BE2 Brown A52A2A BurlyWood DEB887 CadetBlue 5F9EA0 Chartreuse 7FFF00 "
  + "Chocolate D2691E Coral FF7F50 CornflowerBlue 6495ED Cornsilk FFF8DC Crimson DC143C DarkBlue 00008B DarkCyan 008B8B "
  + "DarkGoldenrod B8860B DarkGray A9A9A9 DarkGreen 006400 DarkKhaki BDB76B DarkMagenta 8B008B DarkOliveGreen 556B2F DarkOrange FF8C00 "
  + "DarkOrchid 9932CC DarkRed 8B0000 DarkSalmon E9967A DarkSeaGreen 8FBC8F DarkSlateBlue 483D8B DarkSlateGray 2F4F4F "
  + "DarkTurquoise 00CED1 DarkViolet 9400D3 DeepPink FF1493 DeepSkyBlue 00BFFF DimGray 696969 DodgerBlue 1E90FF Firebrick B22222 "
  + "FloralWhite FFFAF0 ForestGreen 228B22 Gainsboro DCDCDC GhostWhite F8F8FF Gold FFD700 Goldenrod DAA520 Gray 808080 "
  + "Green 008000 GreenYellow ADFF2F Honeydew F0FFF0 HotPink FF69B4 IndianRed CD5C5C Indigo 4B0082 Ivory FFFFF0 Khaki F0E68C "
  + "Lavender E6E6FA LavenderBlush FFF0F5 LawnGreen 7CFC00 LemonChiffon FFFACD LightBlue ADD8E6 LightCoral F08080 LightCyan E0FFFF "
  + "LightGoldenrodYellow FAFAD2 LightGray D3D3D3 LightGreen 90EE90 LightPink FFB6C1 LightSalmon FFA07A LightSeaGreen 20B2AA "
  + "LightSkyBlue 87CEFA LightSlateGray 778899 LightSteelBlue B0C4DE LightYellow FFFFE0 Lime 00FF00 LimeGreen 32CD32 Linen FAF0E6 "
  + "Magenta FF00FF Maroon 800000 MediumAquamarine 66CDAA MediumBlue 0000CD MediumOrchid BA55D3 MediumPurple 9370DB "
  + "MediumSeaGreen 3CB371 MediumSlateBlue 7B68EE MediumSpringGreen 00FA9A MediumTurquoise 48D1CC MediumVioletRed C71585 "
  + "MidnightBlue 191970 MintCream F5FFFA MistyRose FFE4E1 Moccasin FFE4B5 NavajoWhite FFDEAD Navy 000080 OldLace FDF5E6 Olive 808000 "
  + "OliveDrab 6B8E23 Orange FFA500 OrangeRed FF4500 Orchid DA70D6 PaleGoldenrod EEE8AA PaleGreen 98FB98 PaleTurquoise AFEEEE "
  + "PaleVioletRed DB7093 PapayaWhip FFEFD5 PeachPuff FFDAB9 Peru CD853F Pink FFC0CB Plum DDA0DD PowderBlue B0E0E6 Purple 800080 "
  + "Red FF0000 RosyBrown BC8F8F RoyalBlue 4169E1 SaddleBrown 8B4513 Salmon FA8072 SandyBrown F4A460 SeaGreen 2E8B57 SeaShell FFF5EE "
  + "Sienna A0522D Silver C0C0C0 SkyBlue 87CEEB SlateBlue 6A5ACD SlateGray 708090 Snow FFFAFA SpringGreen 00FF7F SteelBlue 4682B4 "
  + "Tan D2B48C Teal 008080 Thistle D8BFD8 Tomato FF6347 Turquoise 40E0D0 Violet EE82EE Wheat F5DEB3 White FFFFFF WhiteSmoke F5F5F5 "
  + "Yellow FFFF00 YellowGreen 9ACD32";
let known = null;

// .NET Math.Round(x, digits): midpoints go to the even neighbour.
function roundEven(x, digits = 0) {
  const f = 10 ** digits;
  const n = x * f;
  const r = Math.round(n);
  return (Math.abs(n % 1) === 0.5 && r % 2 !== 0 ? r - 1 : r) / f;
}

/**
 * The colour's display name, as Fluent's (Avalonia's ColorHelper.ToDisplayName): the colour's HSV
 * rounded (hue to whole degrees, saturation and value to one decimal), then the named colour
 * nearest to it in HSV (hue in degrees, saturation and value 0-1), the first of equals in the
 * order of their ARGB values; its words spaced ("Steel Blue"). "Transparent" when fully transparent.
 */
export function displayName(color) {
  if (color.a === 0) return "Transparent";
  known ??= KNOWN.split(" ").reduce((list, word, i, words) => {
    if (i % 2 === 0) list.push({ name: word.replace(/([a-z])([A-Z])/g, "$1 $2"), value: parseInt(words[i + 1], 16), hsv: rgbToHsv(parseColor(words[i + 1])) });
    return list;
  }, []).sort((a, b) => a.value - b.value);
  const raw = rgbToHsv(color);
  const hsv = { h: roundEven(raw.h), s: roundEven(raw.s, 1), v: roundEven(raw.v, 1) };
  let best = null;
  let distance = Infinity;
  for (const k of known) {
    const d = Math.hypot(hsv.h - k.hsv.h, hsv.s - k.hsv.s, hsv.v - k.hsv.v);
    if (d < distance) [best, distance] = [k.name, d];
  }
  return best;
}

/** An accent colour, as Fluent's: the value moved by 0.1 per step (rounded to 2 places), clamped. */
export function accentColor(hsv, step) {
  return hsvToRgb({ ...hsv, v: clamp(Math.round((hsv.v + step * 0.1) * 100) / 100, 0, 1) });
}

// Fluent's palette (FluentColorPalette, the Windows colours): 6 columns of 8 shades, row by row.
const PALETTE_COLUMNS = [
  "FF4343 D13438 EF6950 DA3B01 CA5010 F7630C FF8C00 FFB900",
  "E74856 E81123 EA005E C30052 E3008C BF0077 C239B3 9A0089",
  "0078D7 0063B1 8E8CD8 6B69D6 8764B8 744DA9 B146C2 881798",
  "0099BC 2D7D9A 00B7C3 038387 00B294 018574 00CC6A 10893E",
  "7A7574 5D5A58 68768A 515C6B 567C73 486860 498205 107C10",
  "767676 4C4A48 69797E 4A5459 647C64 525E54 847545 7E735F",
].map((column) => column.split(" "));
export const PALETTE_COLUMN_COUNT = PALETTE_COLUMNS.length;
export const PALETTE = Object.freeze(PALETTE_COLUMNS[0].flatMap((_, row) => PALETTE_COLUMNS.map((column) => `#${column[row]}`.toLowerCase())));

// --- The element -----------------------------------------------------------------------------

// The components of each model: [string key, letter key, maximum].
const MODELS = {
  rgb: [["red", 255], ["green", 255], ["blue", 255], ["alpha", 255]],
  hsv: [["hue", 360], ["saturation", 100], ["value", 100], ["alpha", 100]],
};
const WHITE = { r: 255, g: 255, b: 255, a: 255 };
const NAV_KEYS = new Set(["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Home", "End"]);
let nextId = 0;

export class UiColorView extends HTMLElement {
  static formAssociated = true;
  static observedAttributes = ["value", "disabled", "color-model", "spectrum-shape", ...Object.keys(STRINGS)];

  #internals = this.attachInternals();
  #hsv = rgbToHsv(WHITE);
  #model = null; // set by script; else the attribute
  #dirty = false;
  #committed = formatColor(WHITE);
  #formDisabled = false;
  #p = null; // the parts
  #colorName = displayName;
  #namedWith = null; // the colorName the palette's names were made with

  connectedCallback() {
    if (!this.#p) this.#build();
    this.#render();
  }

  attributeChangedCallback(name, old, value) {
    if (name === "value" && !this.#dirty) {
      this.#setColor(parseColor(value) ?? WHITE);
      this.#committed = this.value;
    }
    if (this.#p) this.#render();
  }

  get value() {
    return formatColor(this.color);
  }

  set value(text) {
    const color = parseColor(text);
    if (!color) return;
    this.#dirty = true;
    this.#setColor(color);
    this.#committed = this.value;
    if (this.#p) this.#render();
  }

  get color() {
    return hsvToRgb(this.#hsv);
  }

  set color(color) {
    this.value = formatColor({ a: 255, ...color });
  }

  get hsv() {
    return { ...this.#hsv };
  }

  set hsv(hsv) {
    this.#dirty = true;
    this.#hsv = { ...this.#hsv, ...hsv };
    this.#committed = this.value;
    if (this.#p) this.#render();
  }

  get colorModel() {
    return this.#model ?? (this.getAttribute("color-model") === "hsv" ? "hsv" : "rgb");
  }

  set colorModel(model) {
    this.#model = model === "hsv" ? "hsv" : "rgb";
    if (this.#p) this.#render();
  }

  get selectedIndex() {
    return this.#p?.strip.selectedIndex ?? 0;
  }

  set selectedIndex(index) {
    if (this.#p) this.#p.strip.selectedIndex = index;
  }

  get disabled() {
    return this.hasAttribute("disabled") || this.#formDisabled;
  }

  set disabled(on) {
    this.toggleAttribute("disabled", Boolean(on));
  }

  get name() {
    return this.getAttribute("name");
  }

  get form() {
    return this.#internals.form;
  }

  /** The function naming a colour ({ r, g, b, a }) in the palette and the spectrum's value text. */
  get colorName() {
    return this.#colorName;
  }

  set colorName(fn) {
    this.#colorName = typeof fn === "function" ? fn : displayName;
    if (this.#p) this.#render();
  }

  /** The parts: tabs, strip, spectrum, sliders, palette, model, hex, components, previewer. */
  get parts() {
    return this.#p;
  }

  formResetCallback() {
    this.#dirty = false;
    this.#setColor(parseColor(this.getAttribute("value")) ?? WHITE);
    this.#committed = this.value;
    // After the parts' own resets (they run after this element's, in tree order).
    queueMicrotask(() => this.#render());
  }

  formDisabledCallback(disabled) {
    this.#formDisabled = disabled;
    if (this.#p) this.#render();
  }

  #string(key) {
    return this.getAttribute(key) ?? STRINGS[key];
  }

  // A colour from outside (RGB): its HSV, keeping the hue of a grey and the saturation of black.
  #setColor(color) {
    const hsv = rgbToHsv(color);
    if (hsv.s === 0 || hsv.v === 0) hsv.h = this.#hsv.h;
    if (hsv.v === 0) hsv.s = this.#hsv.s;
    this.#hsv = hsv;
  }

  #build() {
    const id = `ui-color-view-${nextId++}`;
    const tab = (name) => {
      const button = el("button", "ui-tab ui-squircle ui-focus-ring", icon(name));
      button.type = "button";
      return button;
    };
    const strip = el("ui-tab-strip", "", tab("drop"), tab("squares-four"), tab("sliders-horizontal"));
    for (const t of strip.querySelectorAll(".ui-tab")) tooltip(t, "");

    const range = (max, vertical) => {
      const input = el("input", `ui-slider ui-color-slider${vertical ? " ui-vertical" : ""}`);
      Object.assign(input, { type: "range", min: "0", max: String(max), step: "1" });
      return input;
    };

    // Spectrum page.
    const third = range(100, true);
    const spectrum = el("ui-color-spectrum");
    const spectrumAlpha = range(100, true);
    const spectrumPage = el("div", "ui-tab-panel ui-color-view-page ui-color-view-spectrum-page", third, spectrum, spectrumAlpha);

    // Palette page.
    const palette = el("ui-list-box", "ui-squircle ui-color-view-palette");
    palette.style.setProperty("--ui-color-view-palette-columns", String(PALETTE_COLUMN_COUNT));
    for (const value of PALETTE) {
      const color = parseColor(value);
      const chip = el("span", "ui-color-chip ui-squircle");
      chip.style.setProperty("--ui-color-chip-color", cssColor(color));
      const cell = el("div", "ui-list-item ui-squircle ui-focus-ring ui-color-view-palette-cell", chip);
      cell.dataset.value = value;
      palette.append(cell);
      tooltip(cell, "");
    }
    const palettePage = el("div", "ui-tab-panel ui-color-view-page ui-color-view-palette-page", palette);

    // Components page.
    const segments = ["rgb", "hsv"].map((value) => {
      const segment = el("span", "ui-segmented-control-item ui-squircle");
      segment.dataset.value = value;
      return segment;
    });
    const model = el("ui-segmented-control", "ui-squircle ui-focus-ring-within ui-color-view-model", ...segments);
    const hex = el("input", "ui-text-input");
    Object.assign(hex, { type: "text", maxLength: 9, spellcheck: false, autocomplete: "off" });
    hex.setAttribute("autocapitalize", "characters");
    const hash = el("span", "ui-field-leading ui-color-view-label ui-color-view-hash", "#");
    hash.setAttribute("aria-hidden", "true");
    const hexField = el("span", "ui-field ui-squircle ui-focus-ring-within ui-color-view-hex", hash, hex);
    const components = el("div", "ui-tab-panel ui-color-view-page ui-color-view-components", el("div", "ui-color-view-components-top", model, hexField));
    const rows = [];
    for (let i = 0; i < 4; i++) {
      const letter = el("span", "ui-color-view-label ui-color-view-letter");
      letter.setAttribute("aria-hidden", "true");
      const number = el("input", "ui-text-input ui-squircle ui-focus-ring ui-color-view-number");
      Object.assign(number, { type: "number", min: "0", step: "1" });
      const slider = range(255, false);
      components.append(letter, number, slider);
      rows.push({ letter, number, slider });
    }

    const tabs = el("ui-tabs", "ui-color-view-tabs", strip, spectrumPage, palettePage, components);

    // Previewer: the colour between two darker and two lighter accent chips.
    const chips = [-2, -1, 0, 1, 2].map((step) => {
      const chip = el("span", `ui-color-chip ui-squircle${step ? " ui-color-view-accent" : ""}`);
      chip.dataset.step = String(step);
      return chip;
    });
    const previewer = el("div", "ui-color-view-previewer", ...chips);
    previewer.setAttribute("aria-hidden", "true");

    this.replaceChildren(tabs, previewer);
    palette.id = `${id}-palette`;
    this.#p = { tabs, strip, spectrum, third, spectrumAlpha, palette, model, segments, hex, hexField, rows, previewer, chips };

    // The parts' events stay inside; the view fires its own.
    for (const type of ["input", "change"]) {
      this.addEventListener(type, (e) => {
        if (e.target === this) return;
        e.stopImmediatePropagation();
        this.#onPartEvent(e);
      });
    }
    palette.addEventListener("keydown", this.#onPaletteKeyDown, true);
    previewer.addEventListener("click", (e) => {
      const step = Number(e.target.closest?.(".ui-color-view-accent")?.dataset.step);
      if (!step || this.disabled) return;
      this.#setColor(accentColor(this.#hsv, step));
      this.#userChange(null, true);
    });
  }

  // A change by the user: everything else shows it, input fires, and change when committed.
  #userChange(source, commit) {
    this.#dirty = true;
    this.#render(source);
    this.dispatchEvent(new Event("input", { bubbles: true }));
    if (commit) this.#commit();
  }

  #commit() {
    const value = this.value;
    if (value === this.#committed) return;
    this.#committed = value;
    this.dispatchEvent(new Event("change", { bubbles: true }));
  }

  #onPartEvent(event) {
    const p = this.#p;
    const target = event.target;
    const commit = event.type === "change";
    if (target === p.model) {
      if (commit) {
        this.#model = p.model.value === "hsv" ? "hsv" : "rgb";
        this.#render();
      }
      return;
    }
    if (target === p.palette) {
      const value = p.palette.selectedItems[0]?.dataset.value;
      if (commit && value) {
        this.#setColor(parseColor(value));
        this.#userChange(p.palette, true);
      }
      return;
    }
    if (target === p.spectrum) {
      if (!commit) {
        const { h, s } = p.spectrum.hsv;
        this.#hsv = { ...this.#hsv, h, s };
        this.#userChange(p.spectrum, false);
      } else this.#commit();
      return;
    }
    if (target === p.third || target === p.spectrumAlpha) {
      if (!commit) {
        const key = target === p.third ? "v" : "a";
        this.#hsv = { ...this.#hsv, [key]: target.valueAsNumber / 100 };
        this.#userChange(target, false);
      } else this.#commit();
      return;
    }
    if (target === p.hex) {
      if (!commit) return;
      const color = parseColor(p.hex.value);
      if (color && formatColor(color) !== this.value) {
        this.#setColor(color);
        this.#userChange(null, true);
      } else this.#render();
      return;
    }
    const row = p.rows.findIndex((r) => r.number === target || r.slider === target);
    if (row < 0) return;
    const { number, slider } = p.rows[row];
    const max = Number(slider.max);
    const n = target.valueAsNumber;
    if (Number.isNaN(n)) {
      if (commit) this.#render();
      return;
    }
    const value = clamp(Math.round(n), 0, max);
    this.#setComponent(row, value);
    // A number typed out of range or with a fraction shows its value once it is taken up.
    this.#userChange(commit && target === number ? null : target, commit);
  }

  // Sets component `row` (0-3) of the colour model to `value` in its range.
  #setComponent(row, value) {
    if (this.colorModel === "hsv") {
      const key = ["h", "s", "v", "a"][row];
      this.#hsv = { ...this.#hsv, [key]: row === 0 ? value : value / 100 };
      return;
    }
    const color = this.color;
    color[["r", "g", "b", "a"][row]] = value;
    const hue = this.#hsv.h;
    const saturation = this.#hsv.s;
    this.#hsv = rgbToHsv(color);
    if (this.#hsv.s === 0 || this.#hsv.v === 0) this.#hsv.h = hue;
    if (this.#hsv.v === 0) this.#hsv.s = saturation;
  }

  // The palette's grid keys (0041), before the ListBox's own, which moves along one line.
  #onPaletteKeyDown = (event) => {
    if (!NAV_KEYS.has(event.key) || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
    event.preventDefault();
    event.stopPropagation();
    if (this.disabled) return;
    const list = this.#p.palette;
    const items = list.items;
    const columns = PALETTE_COLUMN_COUNT;
    const from = items.indexOf(event.target.closest(".ui-list-item"));
    const rtl = getComputedStyle(list).direction === "rtl";
    const along = (step) => {
      const next = from + step;
      return next >= 0 && next < items.length && Math.floor(next / columns) === Math.floor(from / columns) ? next : from;
    };
    const to = from < 0 ? 0
      : event.key === "Home" ? 0
      : event.key === "End" ? items.length - 1
      : event.key === "ArrowLeft" ? along(rtl ? 1 : -1)
      : event.key === "ArrowRight" ? along(rtl ? -1 : 1)
      : event.key === "ArrowUp" ? (from - columns >= 0 ? from - columns : from)
      : (from + columns < items.length ? from + columns : from);
    if (to === from) return;
    const item = items[to];
    list.selectedIndex = to;
    item.focus();
    item.scrollIntoView({ block: "nearest", inline: "nearest" });
    this.#setColor(parseColor(item.dataset.value));
    this.#userChange(list, true);
  };

  // Brings every part in step with the colour, except `source`, which the user is changing.
  #render(source = null) {
    const p = this.#p;
    if (!p) return;
    const hsv = this.#hsv;
    const color = this.color;
    const value = formatColor(color);
    const disabled = this.disabled;
    const model = this.colorModel;
    this.#internals.setFormValue(value);
    this.style.setProperty("--ui-color-view-color", cssColor(color));

    // Tabs: icon headers named by their tooltips' strings.
    const tabNames = ["spectrum-text", "palette-text", "components-text"].map((k) => this.#string(k));
    p.strip.tabs.forEach((t, i) => {
      t.setAttribute("aria-label", tabNames[i]);
      const tip = tooltipOf(t);
      if (tip && tip.textContent !== tabNames[i]) tip.textContent = tabNames[i];
      t.disabled = disabled;
    });

    // Spectrum page.
    const name = this.#colorName(color);
    const opaque = (c) => cssColor({ ...c, a: 255 });
    if (source !== p.spectrum) p.spectrum.hsv = hsv;
    p.spectrum.disabled = disabled;
    p.spectrum.setAttribute("aria-label", this.#string("spectrum-text"));
    p.spectrum.setAttribute("hue-text", this.#string("hue-text"));
    p.spectrum.setAttribute("saturation-text", this.#string("saturation-text"));
    p.spectrum.setAttribute("value-text", `${name}, ${this.#string("hue-text")} ${Math.round(hsv.h)}°, ${this.#string("saturation-text")} ${Math.round(hsv.s * 100)}%`);
    if (this.getAttribute("spectrum-shape") === "ring") p.spectrum.setAttribute("shape", "ring");
    else p.spectrum.removeAttribute("shape");
    const sliders = [
      [p.third, Math.round(hsv.v * 100), `#000000, ${opaque(hsvToRgb({ h: hsv.h, s: 1, v: 1 }))}`, "value-text"],
      [p.spectrumAlpha, Math.round(hsv.a * 100), `${cssColor({ ...color, a: 0 })}, ${opaque(color)}`, "alpha-text"],
    ];

    // Components page.
    p.model.disabled = disabled;
    p.model.setAttribute("aria-label", this.#string("color-model-text"));
    p.segments.forEach((segment, i) => {
      const text = this.#string(i ? "hsv-text" : "rgb-text");
      if (segment.textContent !== text) segment.textContent = text;
    });
    if (p.model.value !== model) p.model.value = model;
    p.hex.disabled = disabled;
    p.hex.setAttribute("aria-label", this.#string("hex-text"));
    const hexText = `${hex2(color.r)}${hex2(color.g)}${hex2(color.b)}${hex2(color.a)}`.toUpperCase();
    if (source !== p.hex && p.hex.value !== hexText) {
      p.hex.value = hexText;
      syncField(p.hex);
    }
    const values = model === "hsv"
      ? [Math.round(hsv.h), Math.round(hsv.s * 100), Math.round(hsv.v * 100), Math.round(hsv.a * 100)]
      : [color.r, color.g, color.b, color.a];
    const stops = model === "hsv"
      ? [
        "#FF0000, #FFFF00, #00FF00, #00FFFF, #0000FF, #FF00FF, #FF0000",
        `#FFFFFF, ${opaque(hsvToRgb({ h: hsv.h, s: 1, v: 1 }))}`,
        `#000000, ${opaque(hsvToRgb({ h: hsv.h, s: 1, v: 1 }))}`,
      ]
      : ["#000000, #FF0000", "#000000, #00FF00", "#000000, #0000FF"];
    stops.push(`${cssColor({ ...color, a: 0 })}, ${opaque(color)}`);
    MODELS[model].forEach(([key, max], i) => {
      const { letter, number, slider } = p.rows[i];
      letter.textContent = this.#string(`${key}-letter`);
      for (const input of [number, slider]) {
        input.max = String(max);
        input.disabled = disabled;
        input.setAttribute("aria-label", this.#string(`${key}-text`));
      }
      if (source !== number && number.value !== String(values[i])) number.value = String(values[i]);
      sliders.push([slider, values[i], stops[i]]);
    });
    for (const [slider, v, gradient, key] of sliders) {
      if (key) slider.setAttribute("aria-label", this.#string(key));
      slider.disabled = disabled;
      slider.style.setProperty("--ui-color-slider-stops", gradient);
      if (source !== slider && slider.valueAsNumber !== v) slider.value = String(v);
    }

    // Palette: named by colorName; the cell of exactly this colour is selected.
    if (this.#namedWith !== this.#colorName) {
      this.#namedWith = this.#colorName;
      for (const cell of p.palette.items) {
        const cellName = this.#colorName(parseColor(cell.dataset.value));
        cell.setAttribute("aria-label", cellName);
        const tip = tooltipOf(cell);
        if (tip) tip.textContent = cellName;
      }
    }
    p.palette.toggleAttribute("disabled", disabled);
    p.palette.setAttribute("aria-label", this.#string("palette-text"));
    if (source !== p.palette) p.palette.selectedIndex = PALETTE.indexOf(value);

    // Previewer.
    for (const chip of p.chips) {
      const step = Number(chip.dataset.step);
      chip.style.setProperty("--ui-color-chip-color", cssColor(step ? accentColor(hsv, step) : color));
    }
  }
}

if (!customElements.get("ui-color-view")) customElements.define("ui-color-view", UiColorView);
