// Colours: the neutral ramp, the accent and the danger colours, with their current hex values.
import { el, body, caption, page, section, watchTheme, hex, cssName } from "./ui.js";

const NEUTRAL = [
  "SystemRegionColor", "SystemAltHighColor", "SystemBaseHighColor", "SystemBaseMediumHighColor",
  "SystemBaseMediumColor", "SystemBaseMediumLowColor", "SystemBaseLowColor", "SystemChromeLowColor",
  "SystemChromeMediumLowColor", "SystemChromeMediumColor", "SystemChromeHighColor", "SystemListLowColor",
  "SystemListMediumColor",
];
const ACCENT = [
  "SystemAccentColor", "AccentFillColor", "AccentFillColorPointerOver", "AccentFillColorPressed",
  "AccentTextColor", "AccentTextColorPressed",
];
const DANGER = ["DangerColor", "DangerFillColor", "DangerFillColorPointerOver", "DangerFillColorPressed", "DangerForeground"];

// A card: the chip (72 x 32, hairline edge, radius 4) and the key with its hex below.
function swatch(key) {
  const chip = el("span", "catalog-swatch-chip");
  chip.style.backgroundColor = `var(${cssName(key)})`;
  const value = caption();
  const card = el("div", "catalog-swatch", chip, el("span", "catalog-swatch-text", body(key), value));
  watchTheme(card, () => (value.textContent = hex(getComputedStyle(chip).backgroundColor)));
  return card;
}

const swatches = (keys) => el("div", "catalog-swatches", ...keys.map(swatch));

export default {
  id: "colours",
  title: "Colours",
  icon: "palette",
  render: () => page("Colours",
    section("Neutral", swatches(NEUTRAL)),
    section("Accent", swatches(ACCENT)),
    section("Danger", swatches(DANGER))),
};
