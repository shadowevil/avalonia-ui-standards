// Icon (0026): inline SVG icons from the library's set (icons.js).
import { icons } from "./icons.js";

const SVG = "http://www.w3.org/2000/svg";

/**
 * An <svg class="ui-icon"> showing the named icon ("caret-down"; see iconNames). Without a label
 * it is decoration, hidden from assistive technology; with one it is an image with that name.
 * size (px) sets width and height, for the 24 px larger use; the default follows density.
 */
export function icon(name, { label, size } = {}) {
  const d = icons[name];
  if (d === undefined) throw new Error(`unknown icon: ${name}`);
  const svg = document.createElementNS(SVG, "svg");
  svg.setAttribute("class", "ui-icon");
  svg.setAttribute("viewBox", "0 0 256 256");
  if (label) {
    svg.setAttribute("role", "img");
    svg.setAttribute("aria-label", label);
  } else {
    svg.setAttribute("aria-hidden", "true");
  }
  if (size !== undefined) {
    svg.style.inlineSize = `${size}px`;
    svg.style.blockSize = `${size}px`;
  }
  const path = document.createElementNS(SVG, "path");
  path.setAttribute("d", d);
  svg.append(path);
  return svg;
}

/** The names of every icon in the set, in the desktop Icons order. */
export const iconNames = Object.freeze(Object.keys(icons));
