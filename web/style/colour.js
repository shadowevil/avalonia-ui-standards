// Colour derivation, ported from the desktop StandardsTheme and AccessibleColors (0011, 0022, 0037):
// the AA fills, AA text colours, secondary text and backdrop wash for an accent.

export const DEFAULT_ACCENT = "#007CEA";
const DANGER = "#E1332A";
const SUCCESS = "#22A134";
const WARNING = "#CE7900";

const MIN_CONTRAST = 4.5;
const STATE_STEP = 0.04;
const SEARCH_STEP = 0.005;
const GLASS_TINT_OPACITY = 0.68;

// Per theme: page (RegionColor), card (ChromeMediumLow), glass tint, the accent share of the
// wash start, and the secondary-text grey before its AA step.
const THEMES = {
  light: { region: "#F5F5F5", card: "#FFFFFF", glassTint: "#FFFFFF", washAccent: 0.10, secondary: "#737373", onDark: false },
  dark: { region: "#1E1E1E", card: "#2A2A2A", glassTint: "#1E1E1E", washAccent: 0.14, secondary: "#9E9E9E", onDark: true },
};

// .NET Math.Round: midpoints go to the even neighbour.
function round(x) {
  const r = Math.round(x);
  return r - x === 0.5 && r % 2 !== 0 ? r - 1 : r;
}

export function parseHex(hex) {
  const m = /^#?([0-9a-f]{6})$/i.exec(hex);
  if (!m) throw new Error(`not a #RRGGBB colour: ${hex}`);
  const n = parseInt(m[1], 16);
  return [n >> 16, (n >> 8) & 255, n & 255];
}

function toHex([r, g, b]) {
  return "#" + [r, g, b].map((c) => c.toString(16).padStart(2, "0").toUpperCase()).join("");
}

// As Avalonia's Color.ToHsl and HslColor.ToRgb.
function toHsl([r8, g8, b8]) {
  const r = r8 / 255, g = g8 / 255, b = b8 / 255;
  const max = Math.max(Math.max(r, g), b);
  const min = Math.min(Math.min(r, g), b);
  const chroma = max - min;
  let h1;
  if (chroma === 0) h1 = 0;
  else if (max === r) h1 = ((g - b) / chroma + 6) % 6;
  else if (max === g) h1 = 2 + (b - r) / chroma;
  else h1 = 4 + (r - g) / chroma;
  const l = 0.5 * (max + min);
  const s = chroma === 0 ? 0 : chroma / (1 - Math.abs(2 * l - 1));
  return { h: 60 * h1, s, l };
}

function fromHsl(h, s, l) {
  const chroma = (1 - Math.abs(2 * l - 1)) * s;
  const h1 = h / 60;
  const x = chroma * (1 - Math.abs((h1 % 2) - 1));
  const m = l - 0.5 * chroma;
  let rgb;
  if (h1 < 1) rgb = [chroma, x, 0];
  else if (h1 < 2) rgb = [x, chroma, 0];
  else if (h1 < 3) rgb = [0, chroma, x];
  else if (h1 < 4) rgb = [0, x, chroma];
  else if (h1 < 5) rgb = [x, 0, chroma];
  else rgb = [chroma, 0, x];
  return rgb.map((c) => round(255 * (c + m)));
}

const clamp = (v, lo, hi) => Math.min(Math.max(v, lo), hi);
const withLightness = (hsl, l) => fromHsl(hsl.h, hsl.s, clamp(l, 0, 1));

function linear(channel) {
  const s = channel / 255;
  return s <= 0.04045 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
}

function relativeLuminance([r, g, b]) {
  return 0.2126 * linear(r) + 0.7152 * linear(g) + 0.0722 * linear(b);
}

export function contrastRatio(a, b) {
  const la = relativeLuminance(a), lb = relativeLuminance(b);
  return (Math.max(la, lb) + 0.05) / (Math.min(la, lb) + 0.05);
}

/** Colour a with amount of b mixed in. */
function mix(a, b, amount) {
  return a.map((x, i) => round(x + (b[i] - x) * amount));
}

/** Rest, hover and pressed fills that reach 4.5:1 with white text. */
function forWhiteText(brand) {
  const hsl = toHsl(brand);
  const white = [255, 255, 255];
  let l = hsl.l;
  while (contrastRatio(white, withLightness(hsl, l)) < MIN_CONTRAST && l > 0) l = Math.max(0, l - SEARCH_STEP);
  return [withLightness(hsl, l), withLightness(hsl, l - STATE_STEP), withLightness(hsl, l - 2 * STATE_STEP)];
}

/** Text in brand's hue reaching 4.5:1 on every background, and its pressed step. */
function forText(brand, backgrounds, onDark) {
  const hsl = toHsl(brand);
  const direction = onDark ? 1 : -1;
  const lowest = (l) => Math.min(...backgrounds.map((b) => contrastRatio(withLightness(hsl, l), b)));
  let l = hsl.l;
  while (lowest(l) < MIN_CONTRAST && l > 0 && l < 1) l = clamp(l + direction * SEARCH_STEP, 0, 1);
  return [withLightness(hsl, l), withLightness(hsl, l + direction * STATE_STEP)];
}

function themeColours(brand, t) {
  const region = parseHex(t.region);
  const tint = parseHex(t.glassTint);
  const start = mix(region, brand, t.washAccent);
  const underGlass = (backdrop) => mix(backdrop, tint, GLASS_TINT_OPACITY);
  const backgrounds = [region, parseHex(t.card), start, region, underGlass(start), underGlass(region)];
  const [text, textPressed] = forText(brand, backgrounds, t.onDark);
  const textOn = (hex) => toHex(forText(parseHex(hex), backgrounds, t.onDark)[0]);
  return {
    SystemBaseMediumColor: toHex(forText(parseHex(t.secondary), backgrounds, t.onDark)[0]),
    BackdropWashStartColor: toHex(start),
    BackdropWashEndColor: toHex(region),
    AccentTextColor: toHex(text),
    AccentTextColorPressed: toHex(textPressed),
    SuccessTextColor: textOn(SUCCESS),
    WarningTextColor: textOn(WARNING),
    DangerTextColor: textOn(DANGER),
  };
}

/**
 * Every accent-derived colour, keyed by desktop key: `shared` is the same in light and dark,
 * `light` and `dark` differ per theme.
 */
export function deriveColours(accent = DEFAULT_ACCENT) {
  const brand = parseHex(accent);
  const [fill, fillHover, fillPressed] = forWhiteText(brand).map(toHex);
  const [danger, dangerHover, dangerPressed] = forWhiteText(parseHex(DANGER)).map(toHex);
  return {
    shared: {
      SystemAccentColor: toHex(brand),
      AccentFillColor: fill,
      AccentFillColorPointerOver: fillHover,
      AccentFillColorPressed: fillPressed,
      DangerFillColor: danger,
      DangerFillColorPointerOver: dangerHover,
      DangerFillColorPressed: dangerPressed,
    },
    light: themeColours(brand, THEMES.light),
    dark: themeColours(brand, THEMES.dark),
  };
}

/** The custom property for a desktop key (0046): AccentFillColor is --ui-accent-fill-color. */
export function cssName(key) {
  return "--ui-" + key.replace(/([a-z0-9])([A-Z])/g, "$1-$2").toLowerCase();
}
