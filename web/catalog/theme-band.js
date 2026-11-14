// The live theme controls in the toolbar band slot (0020). Until the segmented control, the accent
// drop-down and the toolbar band Module exist, each setting is a labelled group of subtle
// ToggleButtons with one pressed: theme, accent presets (each a chip), density, then motion,
// transparency and glass quality at the trailing end. Motion and transparency lead with the
// desktop's icons (the motion icon follows the chosen value).
import { icon } from "../ui.js";

export const ACCENTS = Object.freeze([
  ["Default", "#007CEA"],
  ["Teal", "#00899E"],
  ["Green", "#2E8B3E"],
  ["Orange", "#E0720B"],
  ["Purple", "#7A4FD6"],
  ["Pink", "#E91E63"],
]);

const TRI = [["Follow OS", "system"], ["Reduced", true], ["Full", false]];
const MOTION_ICONS = new Map([["system", "motion-system"], [true, "motion-reduced"], [false, "motion-full"]]);

// key: the settings field; options: [label, value]; icon(value): a leading icon name; caption: leading text.
const GROUPS = [
  { key: "theme", label: "Theme", options: [["System", "system"], ["Light", "light"], ["Dark", "dark"]] },
  { key: "accent", label: "Accent colour", options: ACCENTS.map(([name, hex]) => [name, hex]), chips: true },
  { key: "density", label: "Density", options: [["Normal", "normal"], ["Compact", "compact"]] },
  { key: "reduceMotion", label: "Motion", options: TRI, icon: (v) => MOTION_ICONS.get(v), trailing: true },
  { key: "reduceTransparency", label: "Transparency", options: TRI, icon: () => "drop" },
  { key: "glassQuality", label: "Glass quality", options: [["Full", "full"], ["Minimal", "minimal"], ["Solid", "solid"]], caption: "Glass" },
];

function chip(hex) {
  const span = document.createElement("span");
  span.className = "catalog-chip ui-squircle";
  span.style.backgroundColor = hex;
  span.setAttribute("aria-hidden", "true");
  return span;
}

/**
 * Fills `band` with the controls, pressed at `settings`; `onChange(key, value)` runs on a choice.
 */
export function buildThemeBand(band, settings, onChange) {
  const groups = [];
  for (const spec of GROUPS) {
    const group = document.createElement("div");
    group.className = "catalog-band-group";
    group.setAttribute("role", "group");
    group.setAttribute("aria-label", spec.label);
    if (spec.trailing) group.classList.add("catalog-band-trailing");
    let lead = null;
    if (spec.icon) {
      lead = document.createElement("span");
      lead.className = "catalog-band-lead";
      group.append(lead);
    } else if (spec.caption) {
      const caption = document.createElement("span");
      caption.className = "catalog-band-lead catalog-band-caption";
      caption.setAttribute("aria-hidden", "true");
      caption.textContent = spec.caption;
      group.append(caption);
    }
    const buttons = spec.options.map(([text, value]) => {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "ui-button ui-subtle ui-squircle ui-focus-ring";
      button.setAttribute("aria-pressed", "false");
      if (spec.chips) {
        button.classList.add("catalog-chip-button");
        button.append(chip(value));
        button.setAttribute("aria-label", text);
        button.title = `${text} ${value}`;
      } else {
        button.textContent = text;
      }
      // button.js flips aria-pressed in the capture phase; one stays pressed per group.
      button.addEventListener("click", () => {
        press(value);
        onChange(spec.key, value);
      });
      group.append(button);
      return [button, value];
    });
    const press = (chosen) => {
      for (const [button, value] of buttons) button.setAttribute("aria-pressed", String(sameValue(value, chosen)));
      if (lead) lead.replaceChildren(icon(spec.icon(chosen)));
    };
    groups.push([spec.key, press]);
    band.append(group);
  }
  for (const [key, press] of groups) press(settings[key]);
}

const sameValue = (a, b) => (typeof a === "string" && typeof b === "string" ? a.toUpperCase() === b.toUpperCase() : a === b);
