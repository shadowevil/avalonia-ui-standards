// Glass: glass panels over a colourful backdrop. The backdrop scrolls (wheel, scrollbars or
// dragging it), so the blur (and, at the first level, the refraction) changes as it moves.
import { el, caption, page, section } from "./ui.js";

const WIDTH = 1800;
const HEIGHT = 900;

// The desktop GlassBackdrop: a gradient with a grid, stripes and shapes, so blur and bending show.
function backdrop() {
  const canvas = el("canvas", "catalog-glass-backdrop");
  const scale = Math.max(1, Math.ceil(devicePixelRatio));
  canvas.width = WIDTH * scale;
  canvas.height = HEIGHT * scale;
  canvas.setAttribute("aria-hidden", "true");
  const g = canvas.getContext("2d");
  g.scale(scale, scale);
  const gradient = g.createLinearGradient(0, 0, WIDTH, HEIGHT);
  gradient.addColorStop(0, "#FF4D4D");
  gradient.addColorStop(0.3, "#FFC800");
  gradient.addColorStop(0.6, "#3CCB5A");
  gradient.addColorStop(1, "#1E7BFF");
  g.fillStyle = gradient;
  g.fillRect(0, 0, WIDTH, HEIGHT);
  const line = (colour, width, x1, y1, x2, y2) => {
    g.strokeStyle = colour;
    g.lineWidth = width;
    g.beginPath();
    g.moveTo(x1, y1);
    g.lineTo(x2, y2);
    g.stroke();
  };
  for (let x = 0.5; x < WIDTH; x += 16) line("rgba(0,0,0,0.5)", 1, x, 0, x, HEIGHT);
  for (let y = 0.5; y < HEIGHT; y += 16) line("rgba(0,0,0,0.5)", 1, 0, y, WIDTH, y);
  for (let x = -HEIGHT; x < WIDTH; x += 64) line("#FFFFFF", 4, x, HEIGHT, x + HEIGHT, 0);
  for (let y = 60; y < HEIGHT; y += 120) line("#000000", 4, 0, y, WIDTH, y);
  for (let x = 100; x < WIDTH; x += 260) {
    g.fillStyle = "#000080";
    g.fillRect(x, 40, 28, HEIGHT - 80);
    for (const [cx, cy, r, fill] of [[x + 120, HEIGHT * 0.35, 36, "#FF00FF"], [x + 170, HEIGHT * 0.7, 30, "#FFFFFF"]]) {
      g.fillStyle = fill;
      g.beginPath();
      g.arc(cx, cy, r, 0, 2 * Math.PI);
      g.fill();
    }
  }
  return canvas;
}

// Dragging the backdrop scrolls it, as on desktop.
function enableDragging(scroller) {
  let last = null;
  scroller.addEventListener("pointerdown", (e) => {
    if (e.button !== 0 || e.target !== scroller.firstElementChild) return;
    last = [e.clientX, e.clientY];
    scroller.setPointerCapture(e.pointerId);
  });
  scroller.addEventListener("pointermove", (e) => {
    if (!last) return;
    scroller.scrollBy(last[0] - e.clientX, last[1] - e.clientY);
    last = [e.clientX, e.clientY];
  });
  for (const type of ["pointerup", "pointercancel", "lostpointercapture"]) scroller.addEventListener(type, () => (last = null));
}

const panel = (width, height, text) => {
  const p = el("div", "catalog-glass-card ui-glass ui-squircle", text);
  p.style.inlineSize = `${width}px`;
  p.style.blockSize = `${height}px`;
  return p;
};

export default {
  id: "glass",
  title: "Glass",
  icon: "drop",
  render() {
    const scroller = el("div", "catalog-glass-scroller ui-scroll", backdrop());
    scroller.tabIndex = 0;
    scroller.setAttribute("role", "region");
    scroller.setAttribute("aria-label", "Backdrop");
    scroller.classList.add("ui-focus-ring");
    enableDragging(scroller);
    // The panels wrap onto further rows when the page is narrow.
    const cards = el("div", "catalog-glass-cards", panel(180, 140, "Glass card"), panel(220, 160, "Glass panel"), panel(140, 100, "Glass chip"));
    cards.setAttribute("aria-hidden", "true");
    return page("Glass",
      section("Over a backdrop", el("div", "catalog-glass-stage", scroller, cards)),
      caption("Scroll or drag the backdrop. Reduce transparency in the toolbar band draws the panels solid."));
  },
};
