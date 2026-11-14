// The theme entry (0046): settings are attributes on <html>, and the accent-derived colours are
// written there as custom properties.
import { DEFAULT_ACCENT, cssName, deriveColours } from "./colour.js";

const root = () => document.documentElement;

function setAttribute(name, value, allowed) {
  if (!allowed.includes(value)) throw new Error(`${name}: expected one of ${allowed.join(", ")}, got ${value}`);
  if (value === "system") root().removeAttribute(name);
  else root().setAttribute(name, String(value));
}

/** "system" (follow the browser), "light" or "dark". */
export function setTheme(theme) {
  setAttribute("data-theme", theme, ["system", "light", "dark"]);
}

/** "normal" or "compact". */
export function setDensity(density) {
  setAttribute("data-density", density, ["normal", "compact"]);
}

/** "system" (follow the browser), true or false. */
export function setReduceMotion(value) {
  setAttribute("data-reduce-motion", value, ["system", true, false]);
}

/** "system" (follow the browser), true or false. True makes glass its solid fill. */
export function setReduceTransparency(value) {
  setAttribute("data-reduce-transparency", value, ["system", true, false]);
}

/** Sets the accent (#RRGGBB) and every value derived from it, for light and dark. */
export function setAccent(accent = DEFAULT_ACCENT) {
  const { shared, light, dark } = deriveColours(accent);
  const style = root().style;
  for (const [key, value] of Object.entries(shared)) style.setProperty(cssName(key), value);
  for (const key of Object.keys(light)) style.setProperty(cssName(key), `light-dark(${light[key]}, ${dark[key]})`);
}
