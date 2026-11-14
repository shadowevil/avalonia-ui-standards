// The catalog's saved settings (0020): the toolbar band's choices, kept in localStorage so the
// catalog reopens as it was left. Saved half a second after the last change, as the desktop's
// settings.json; missing or unreadable storage, or a value outside its list, gives the default.
import { setTheme, setDensity, setAccent, setReduceMotion, setReduceTransparency, setGlassQuality } from "../ui.js";

export const STORAGE_KEY = "AvaloniaUiStandards.Catalog.settings";
export const SAVE_DELAY = 500;

export const DEFAULTS = Object.freeze({
  theme: "system",
  density: "normal",
  accent: "#007CEA",
  reduceMotion: "system",
  reduceTransparency: "system",
  glassQuality: "full",
});

const CHOICES = {
  theme: ["system", "light", "dark"],
  density: ["normal", "compact"],
  reduceMotion: ["system", true, false],
  reduceTransparency: ["system", true, false],
  glassQuality: ["full", "minimal", "solid"],
};

const HEX = /^#[0-9A-Fa-f]{6}$/;

/** The settings in `value` (any parsed JSON), each field checked; anything else is the default. */
export function validate(value) {
  const source = value && typeof value === "object" ? value : {};
  const settings = { ...DEFAULTS };
  for (const [key, allowed] of Object.entries(CHOICES)) if (allowed.includes(source[key])) settings[key] = source[key];
  if (typeof source.accent === "string" && HEX.test(source.accent)) settings.accent = source.accent.toUpperCase();
  return settings;
}

/** The saved settings, or the defaults when there are none or they cannot be read. */
export function load(storage = globalThis.localStorage) {
  try {
    const text = storage?.getItem(STORAGE_KEY);
    return validate(text ? JSON.parse(text) : null);
  } catch {
    return { ...DEFAULTS };
  }
}

/** Writes the settings now; a failure to write (storage full or blocked) is ignored. */
export function save(settings, storage = globalThis.localStorage) {
  try {
    storage?.setItem(STORAGE_KEY, JSON.stringify(validate(settings)));
  } catch {
    // not saved
  }
}

/** Sets every setting on the page through the library's setters. */
export function apply(settings) {
  setTheme(settings.theme);
  setDensity(settings.density);
  setAccent(settings.accent);
  setReduceMotion(settings.reduceMotion);
  setReduceTransparency(settings.reduceTransparency);
  setGlassQuality(settings.glassQuality);
}

/** A saver that writes the latest settings once `delay` ms have passed without another change. */
export function saver(storage = globalThis.localStorage, delay = SAVE_DELAY) {
  let timer = 0;
  let latest = null;
  const flush = () => {
    clearTimeout(timer);
    timer = 0;
    if (latest) save(latest, storage);
    latest = null;
  };
  return {
    schedule(settings) {
      latest = { ...settings };
      clearTimeout(timer);
      timer = setTimeout(flush, delay);
    },
    flush,
    get pending() {
      return timer !== 0;
    },
  };
}
