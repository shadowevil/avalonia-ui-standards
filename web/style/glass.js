// Glass refraction (0015, 0046). Glass draws blur, tint and rim from CSS alone; this module adds
// refraction in Chromium and starts by itself when loaded (web/ui.js and style/index.js load it),
// as the desktop's default GlassQuality Full. Each first-level .ui-glass gets an SVG filter in its
// backdrop-filter that displaces the backdrop within the edge band, with chromatic dispersion. The
// displacement map depends on the element's size, so it is drawn per element and redrawn on
// resize. Glass under 64 px on its shorter side stays minimal (0015). Refraction stops while the
// glass quality is minimal or solid or transparency is reduced, and resumes when they are lifted.
// Safari and Firefox do not apply SVG filters in backdrop-filter, so it never starts there.

const SVG_NS = "http://www.w3.org/2000/svg";
const MINIMAL_BELOW = 64;
const MAP_SCALE = 0.5;
const PROPERTY = "--ui-glass-refraction";

let svg = null;
let resizeObserver = null;
let mutationObserver = null;
let nextId = 0;
const surfaces = new Map(); // element -> { filter, key, size }
// Sizes observed since the last animation frame, so a live resize redraws at most once a frame.
const pendingSizes = new Map();
let frame = 0;

// True where backdrop-filter applies SVG filters: Chromium. navigator.userAgentData exists only in
// secure contexts, so plain-http pages fall back to the user agent string, where every Chromium
// browser (Chrome, Edge, Opera) carries "Chrome/" and Firefox and Safari do not (Chrome on iOS
// is WebKit and says "CriOS/").
const supported = navigator.userAgentData
  ? navigator.userAgentData.brands.some((b) => b.brand === "Chromium")
  : /\b(Chrome|Chromium)\//.test(navigator.userAgent) && !/\bFirefox\//.test(navigator.userAgent);
const reducedTransparency = matchMedia("(prefers-reduced-transparency: reduce)");

// Whether glass is full right now: the quality is full and transparency is not reduced (the
// attribute wins over the browser setting, as in glass.css).
function wanted() {
  const html = document.documentElement;
  const quality = html.getAttribute("data-glass-quality");
  if (quality === "minimal" || quality === "solid") return false;
  const reduce = html.getAttribute("data-reduce-transparency");
  return reduce === "true" ? false : reduce === "false" || !reducedTransparency.matches;
}

function sync() {
  if (wanted()) start();
  else stop();
}

function start() {
  if (svg) return;
  svg = document.createElementNS(SVG_NS, "svg");
  svg.setAttribute("aria-hidden", "true");
  svg.style.cssText = "position:absolute;width:0;height:0;overflow:hidden";
  document.body.append(svg);
  resizeObserver = new ResizeObserver((entries) => {
    for (const entry of entries) pendingSizes.set(entry.target, entry.borderBoxSize[0]);
    frame ||= requestAnimationFrame(flushSizes);
  });
  mutationObserver = new MutationObserver(scheduleScan);
  mutationObserver.observe(document.body, { subtree: true, childList: true, attributes: true, attributeFilter: ["class"] });
  scan();
}

function flushSizes() {
  frame = 0;
  for (const [element, size] of pendingSizes) update(element, size);
  pendingSizes.clear();
}

function stop() {
  if (!svg) return;
  cancelAnimationFrame(frame);
  frame = 0;
  pendingSizes.clear();
  resizeObserver.disconnect();
  mutationObserver.disconnect();
  for (const element of surfaces.keys()) element.style.removeProperty(PROPERTY);
  surfaces.clear();
  svg.remove();
  svg = resizeObserver = mutationObserver = null;
}

if (supported) {
  new MutationObserver(sync).observe(document.documentElement, {
    attributes: true, attributeFilter: ["data-glass-quality", "data-reduce-transparency"],
  });
  reducedTransparency.addEventListener("change", sync);
  if (document.body) sync();
  else document.addEventListener("DOMContentLoaded", sync, { once: true });
}

/**
 * Test hook (not exported from index.js). Re-reads every glass surface's size now, without waiting for the next resize observation
 * (which comes with the next rendered frame). For headless test runs, which render no frames.
 */
export function refreshGlassRefraction() {
  if (!svg) return;
  scan();
  for (const element of surfaces.keys()) {
    update(element, { inlineSize: element.offsetWidth, blockSize: element.offsetHeight });
  }
}

let scanPending = false;
function scheduleScan() {
  if (scanPending) return;
  scanPending = true;
  queueMicrotask(() => {
    scanPending = false;
    if (svg) scan();
  });
}

// First-level glass gets a filter; nested glass is minimal or solid and needs none.
function scan() {
  const wanted = new Set([...document.querySelectorAll(".ui-glass")].filter((e) => !e.parentElement?.closest(".ui-glass")));
  for (const element of surfaces.keys()) if (!wanted.has(element)) release(element);
  for (const element of wanted) {
    if (surfaces.has(element)) continue;
    surfaces.set(element, { filter: null, key: "", size: "" });
    resizeObserver.observe(element);
  }
}

function release(element) {
  resizeObserver.unobserve(element);
  surfaces.get(element).filter?.remove();
  surfaces.delete(element);
  pendingSizes.delete(element);
  element.style.removeProperty(PROPERTY);
}

function update(element, size) {
  const surface = surfaces.get(element);
  if (!surface) return;
  const width = Math.round(size.inlineSize);
  const height = Math.round(size.blockSize);
  // The same size as last time: nothing to redraw (the filter is put back if it was taken off).
  if (`${width},${height}` === surface.size) {
    if (surface.filter) element.style.setProperty(PROPERTY, `url(#${surface.filter.id})`);
    return;
  }
  surface.size = `${width},${height}`;
  if (Math.min(width, height) < MINIMAL_BELOW) {
    surface.filter?.remove();
    surface.filter = null;
    surface.key = "";
    element.style.removeProperty(PROPERTY);
    return;
  }
  const style = getComputedStyle(element);
  const strength = parseFloat(style.getPropertyValue("--ui-glass-refraction-strength"));
  const edge = parseFloat(style.getPropertyValue("--ui-glass-refraction-edge"));
  const dispersion = parseFloat(style.getPropertyValue("--ui-glass-chromatic-dispersion")) || 0;
  const radii = ["TopLeft", "TopRight", "BottomRight", "BottomLeft"].map((c) => parseFloat(style[`border${c}Radius`]) || 0);
  const key = [width, height, strength, edge, dispersion, ...radii].join(",");
  if (key !== surface.key) {
    surface.key = key;
    surface.filter?.remove();
    surface.filter = createFilter(`ui-glass-refraction-${nextId++}`, width, height, radii, strength, edge, dispersion);
    svg.append(surface.filter);
  }
  element.style.setProperty(PROPERTY, `url(#${surface.filter.id})`);
}

// The filter: the map, then the backdrop displaced by it; with dispersion, red, green and blue
// displaced by (1 - dispersion), 1 and (1 + dispersion) times the strength and added together.
// sRGB interpolation keeps the map's values as drawn.
function createFilter(id, width, height, radii, strength, edge, dispersion) {
  const filter = element("filter", { id, "color-interpolation-filters": "sRGB" });
  filter.append(element("feImage", {
    href: displacementMap(width, height, radii, edge, MAP_SCALE), x: 0, y: 0, width, height,
    preserveAspectRatio: "none", result: "map",
  }));
  const displace = (scale, result) => element("feDisplacementMap", {
    in: "SourceGraphic", in2: "map", scale: 2 * strength * scale, xChannelSelector: "R", yChannelSelector: "G", result,
  });
  if (dispersion <= 0) {
    filter.append(displace(1));
    return filter;
  }
  const channels = [["r", 1 - dispersion, "1 0 0 0 0"], ["g", 1, "0 1 0 0 0"], ["b", 1 + dispersion, "0 0 1 0 0"]];
  for (const [name, scale, row] of channels) {
    filter.append(displace(scale, `${name}d`));
    const rows = ["0 0 0 0 0", "0 0 0 0 0", "0 0 0 0 0", "0 0 0 1 0"];
    rows["rgb".indexOf(name)] = row;
    filter.append(element("feColorMatrix", { in: `${name}d`, type: "matrix", values: rows.join(" "), result: name }));
  }
  filter.append(element("feComposite", { in: "r", in2: "g", operator: "arithmetic", k2: 1, k3: 1, result: "rg" }));
  filter.append(element("feComposite", { in: "rg", in2: "b", operator: "arithmetic", k2: 1, k3: 1 }));
  return filter;
}

function element(name, attributes) {
  const e = document.createElementNS(SVG_NS, name);
  for (const [k, v] of Object.entries(attributes)) e.setAttribute(k, String(v));
  return e;
}

/**
 * Test hook (not exported from index.js); glass.js uses it internally.
 * The desktop's refraction map (GlassRefraction.cs) as a PNG data URL: the glass is a slab, flat
 * in the centre and curving down over the edge band; each pixel holds how far and which way light
 * bends there, as 0.5 + d / 2 in red (x) and green (y), with d from -1 to 1 times the strength.
 * The bend is a circular sag, 1 - sqrt(1 - u²), u from 0 at the inner edge of the band to 1 at
 * the rim, along the inward normal of a rounded rectangle whose radius is at least the band.
 * `scale` sets the map's resolution relative to the element (glass.js draws it at half, which the
 * filter scales up, as the desktop runs refraction at half resolution).
 */
export function displacementMap(width, height, radii, edge, scale = 1) {
  const mapW = Math.max(1, Math.round(width * scale));
  const mapH = Math.max(1, Math.round(height * scale));
  const canvas = document.createElement("canvas");
  canvas.width = mapW;
  canvas.height = mapH;
  const context = canvas.getContext("2d");
  const image = context.createImageData(mapW, mapH);
  const data = image.data;
  data.fill(128);
  const halfW = width / 2, halfH = height / 2;
  const limit = Math.min(halfW, halfH);
  const r = radii.map((v) => Math.min(Math.max(v, edge), limit));
  // Only the edge band bends; beyond max(radius, edge) from every side the map is flat.
  const band = Math.ceil((Math.max(edge, ...r) + 1) * mapW / width);
  for (let y = 0; y < mapH; y++) {
    const py = (y + 0.5) * height / mapH - halfH;
    const fullRow = y < band || y >= mapH - band;
    for (let x = 0; x < mapW; x++) {
      if (!fullRow && x === band) x = Math.max(band, mapW - band);
      const px = (x + 0.5) * width / mapW - halfW;
      const radius = px < 0 ? (py < 0 ? r[0] : r[3]) : (py < 0 ? r[1] : r[2]);
      const qx = Math.abs(px) - halfW + radius;
      const qy = Math.abs(py) - halfH + radius;
      const outside = Math.hypot(Math.max(qx, 0), Math.max(qy, 0));
      const inside = -(Math.min(Math.max(qx, qy), 0) + outside - radius);
      let nx, ny;
      if (qx > 0 && qy > 0) {
        nx = qx / outside;
        ny = qy / outside;
      } else if (qx > qy) {
        nx = 1; ny = 0;
      } else {
        nx = 0; ny = 1;
      }
      if (px < 0) nx = -nx;
      if (py < 0) ny = -ny;
      const u = Math.min(Math.max(1 - inside / edge, 0), 1);
      const bend = 1 - Math.sqrt(1 - u * u);
      const i = (y * mapW + x) * 4;
      data[i] = Math.round((0.5 - nx * bend * 0.5) * 255);
      data[i + 1] = Math.round((0.5 - ny * bend * 0.5) * 255);
    }
  }
  for (let i = 3; i < data.length; i += 4) data[i] = 255;
  context.putImageData(image, 0, 0);
  return canvas.toDataURL();
}
