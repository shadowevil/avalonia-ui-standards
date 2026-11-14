// The catalog's animated ribbon backdrop (0020), the web port of apps/Catalog/RibbonBackdrop.cs.
// Flowing, translucent ribbons tinted from the accent: each is a faint band drawn with glowing lines
// (a bright core, and a soft halo on the top edge) along its edges and, on the wider ones, along its
// middle. They stay in place and undulate in a seamless 45 s loop; the light on the lines varies along
// them with dark gaps and travels, and small soft dots twinkle beside the lit edges. Outside the shell's
// glass panel a scrim in the page colour keeps text on the backdrop at AA.
//
// Canvas 2D, at the device pixel ratio. Continuous animation runs at 30 fps (0016), stops under reduce
// motion (--ui-motion-continuous 0), drawn as a still frame without the dots, and pauses while the page
// is hidden. It follows the accent, theme and motion settings on <html> and the browser's preferences.
//
//   const stop = startRibbons(document.querySelector(".ui-shell-backdrop"));
//   stop(); // removes the canvas, listeners and observers

const LOOP_SECONDS = 45;
const FRAME_MS = 1000 / 30;
// A frame may come this much early and still count, so a 60 Hz display draws every other frame.
const FRAME_SLACK_MS = 4;

// Back to front. centre, amplitude and thickness are fractions of the height; wavelength is in px.
const RIBBONS = [
  { wavelength: 1800, centre: 0.30, amplitude: 0.10, waves: 1, shift: 0.4, waves2: 2, shift2: 1.7, thickness: 0.24, swell: 0.07, waves3: 2, strength: 0.8, tint: 0, crest: false, dots: 28, slot: 0.0 },
  { wavelength: 1400, centre: 0.62, amplitude: 0.09, waves: 2, shift: 2.1, waves2: 3, shift2: 0.3, thickness: 0.20, swell: 0.06, waves3: 1, strength: 0.9, tint: 1, crest: true, dots: 36, slot: 1.3 },
  { wavelength: 2200, centre: 0.48, amplitude: 0.12, waves: 1, shift: 4.0, waves2: 3, shift2: 2.6, thickness: 0.17, swell: 0.06, waves3: 2, strength: 1.0, tint: 2, crest: true, dots: 40, slot: 2.6 },
  { wavelength: 1600, centre: 0.78, amplitude: 0.07, waves: 2, shift: 1.1, waves2: 1, shift2: 5.2, thickness: 0.20, swell: 0.07, waves3: 3, strength: 0.8, tint: 0, crest: false, dots: 24, slot: 3.9 },
];

const DOT_SIZES = [1.3, 2.3, 3.6, 5.0];
const LEVELS = 16;
const STOP_SPACING = 20;
const SAMPLE_STEP = 10;
// Measured on desktop over the text areas with every dot and line at full brightness: 5.48:1 and 5.36:1.
const LIGHT_SCRIM = 0.45;
const DARK_SCRIM = 0.64;
const LIGHT_BAND_OPACITY = 0.5;
const SCRIM_RADIUS = 16;

const WHITE = [255, 255, 255];
const BLACK = [0, 0, 0];
const TAU = 2 * Math.PI;

const mix = (a, b, t) => a.map((v, i) => Math.round(v + (b[i] - v) * t));
const luma = (c) => (0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]) / 255;
const rgba = (c, a) => `rgba(${c[0]},${c[1]},${c[2]},${Math.min(1, Math.max(0, a))})`;

// Math.Round of .NET: halves go to the even neighbour.
function roundEven(v) {
  const r = Math.round(v);
  return Math.abs(v % 1) === 0.5 && r % 2 !== 0 ? r - 1 : r;
}

// System.Random(seed) of .NET (the seeded, subtractive generator), so the dots fall where they do on desktop.
function dotnetRandom(seed) {
  const MBIG = 2147483647;
  const a = new Array(56).fill(0);
  let mj = 161803398 - Math.abs(seed);
  a[55] = mj;
  let mk = 1;
  for (let i = 1; i < 55; i++) {
    const ii = (21 * i) % 55;
    a[ii] = mk;
    mk = mj - mk;
    if (mk < 0) mk += MBIG;
    mj = a[ii];
  }
  for (let k = 1; k < 5; k++)
    for (let i = 1; i < 56; i++) {
      a[i] -= a[1 + ((i + 30) % 55)];
      if (a[i] < 0) a[i] += MBIG;
    }
  let next = 0;
  let nextp = 21;
  const sample = () => {
    if (++next >= 56) next = 1;
    if (++nextp >= 56) nextp = 1;
    let v = a[next] - a[nextp];
    if (v === MBIG) v--;
    if (v < 0) v += MBIG;
    a[next] = v;
    return v * (1 / MBIG);
  };
  return { nextDouble: sample, next: (max) => Math.floor(sample() * max) };
}

// "rgb(1, 2, 3)", "rgba(...)" or "color(srgb 0.1 0.2 0.3)" to [r, g, b].
function parseColour(text) {
  const n = (text.match(/-?[\d.]+(e-?\d+)?/g) || []).map(Number);
  if (n.length < 3) return null;
  return text.startsWith("color(") ? n.slice(0, 3).map((v) => Math.round(v * 255)) : n.slice(0, 3).map(Math.round);
}

// The ribbons' shape at phase p. Every motion is a sine of whole cycles per loop, so 1 looks exactly like 0.
function centre(rb, x, height, p) {
  const u = (TAU * x) / rb.wavelength;
  const t = TAU * p;
  const first = Math.sin(rb.waves * u + rb.shift + 0.9 * Math.sin(t + rb.slot)) * (1 + 0.3 * Math.sin(t + rb.slot * 1.7));
  const second = Math.sin(rb.waves2 * u + rb.shift2 + 1.1 * Math.sin(2 * t + rb.slot * 0.6)) * (1 + 0.25 * Math.sin(2 * t + rb.slot * 2.3));
  return height * (rb.centre + (rb.amplitude * (first + 0.5 * second)) / 1.5);
}

function halfWidth(rb, x, height, p) {
  const u = (TAU * x) / rb.wavelength;
  const t = TAU * p;
  const swell = Math.sin(rb.waves3 * u + rb.shift + 1.0 * Math.sin(3 * t + rb.slot));
  return (height * (rb.thickness + rb.swell * swell) * (1 + 0.12 * Math.sin(2 * t + rb.slot * 1.1))) / 2;
}

// -1 is a ribbon's top edge, 1 its bottom edge, 0 its middle (which weaves a little).
function lineY(rb, across, x, c, half, p) {
  const weave = across === 0 ? 0.12 * Math.sin((TAU * 3 * x) / rb.wavelength + rb.shift + TAU * p) : 0;
  return c + half * (across + weave);
}

// How lit a line is at x, 0 to 1: only the crests of three moving waves are lit, so lit stretches
// have dark gaps between them.
function light(ribbon, line, x, p) {
  const rb = RIBBONS[ribbon];
  const u = (TAU * x) / rb.wavelength;
  const a = Math.sin(2 * u + line * 1.7 + ribbon + TAU * p);
  const b = Math.sin(3 * u + line * 2.3 + ribbon * 0.9 - 2 * TAU * p);
  const c = Math.sin(5 * u + line * 0.8 + ribbon * 1.9 + 3 * TAU * p);
  let m = 0.5 + 0.5 * (0.5 * a + 0.3 * b + 0.2 * c);
  m = Math.min(1, Math.max(0, (m - 0.46) / 0.4));
  return m * m * (3 - 2 * m) * rb.strength;
}

const swellCurve = (cycle) => {
  const s = 0.5 + 0.5 * Math.sin(TAU * cycle);
  return s * s * s;
};

const flashCurve = (fraction) => {
  if (fraction >= 0.12) return 0;
  const s = Math.sin((Math.PI * fraction) / 0.12);
  return s * s;
};

// Dots close beside the edges, thinning out with distance, in four sizes: never on a grid.
function makeDots(ribbon, rb, width, height) {
  const random = dotnetRandom(17 + ribbon * 31);
  const count = roundEven((rb.dots * width) / rb.wavelength);
  const dots = [];
  for (let i = 0; i < count; i++) {
    const x = ((i + random.nextDouble()) / count) * width;
    const side = random.next(2) === 0 ? -1 : 1;
    const distance = -Math.log(1 - random.nextDouble()) * height * 0.03;
    const pick = random.nextDouble();
    const size = pick < 0.45 ? 0 : pick < 0.8 ? 1 : pick < 0.95 ? 2 : 3;
    const brightness = (0.45 + random.nextDouble() * 0.55) * (size >= 2 ? 1 : 0.8);
    const rate = roundEven(LOOP_SECONDS / (0.8 + random.nextDouble() * 2.2));
    const flash = random.nextDouble() < 0.2;
    dots.push({
      x, y: side * distance, size, brightness: flash ? Math.min(1, brightness * 1.3) : brightness, rate,
      offset: random.nextDouble(), line: side < 0 ? 0 : 1, flash, drift: 4 + random.nextDouble() * 8,
    });
  }
  return dots;
}

/**
 * Fills a Backdrop slot (.ui-shell-backdrop) with the ribbons and starts them. Returns the stop
 * function, which removes the canvas and every listener and observer. Test hooks on it:
 * stop.drawAt(seconds) stops the loop and draws that moment of it; stop.state() reports
 * { phase, running, reduced, frames, drawMs }.
 */
export function startRibbons(slot) {
  const canvas = document.createElement("canvas");
  canvas.setAttribute("aria-hidden", "true");
  canvas.style.cssText = "position:absolute;inset:0;width:100%;height:100%;display:block;pointer-events:none";
  // The colours are read through the canvas's own properties, so light-dark() resolves to the theme.
  canvas.style.color = "var(--ui-system-accent-color)";
  canvas.style.caretColor = "var(--ui-system-region-color)";
  canvas.style.textDecorationColor = "var(--ui-system-base-high-color)";
  slot.append(canvas);
  const ctx = canvas.getContext("2d");
  const html = document.documentElement;
  const panelElement = slot.closest(".ui-shell")?.querySelector(".ui-shell-panel") ?? null;

  let phase = 0;
  let width = 0;
  let height = 0;
  let dpr = 0;
  let model = null; // what depends on the size and colours
  let colourKey = "";
  let panel = null;
  let reduced = false;
  let autoAdvance = true;
  let raf = 0;
  let lastTime = -1; // time of the previous drawn frame; -1 after a start or resume
  let frames = 0;
  let drawMs = 0;
  let stopped = false;
  let ratioQuery = null; // matches while the pixel ratio stays as it is (another screen changes it)

  function readColours() {
    const s = getComputedStyle(canvas);
    return {
      accent: parseColour(s.color) ?? [0, 124, 234],
      region: parseColour(s.caretColor) ?? WHITE,
      text: parseColour(s.textDecorationColor) ?? BLACK,
      continuous: parseFloat(s.getPropertyValue("--ui-motion-continuous")),
    };
  }

  function build(colours) {
    const { accent, region, text } = colours;
    const dark = luma(region) < 0.5;
    const highlight = dark ? mix(accent, text, 0.35) : mix(accent, WHITE, 0.55);
    const tints = [
      [mix(accent, region, 0.25), highlight],
      [highlight, mix(accent, dark ? BLACK : region, 0.35)],
      [mix(accent, WHITE, dark ? 0.1 : 0.3), accent],
    ];
    const lightBand = (t) => mix(accent, BLACK, t === 0 ? 0.12 : t === 1 ? 0.25 : 0.38);

    const count = Math.ceil((width + 40) / SAMPLE_STEP) + 1;
    const xs = Float64Array.from({ length: count }, (_, i) => -20 + i * SAMPLE_STEP);
    const stops = Math.ceil(width / STOP_SPACING) + 1;
    const ribbons = RIBBONS.map((rb, r) => ({
      band: dark
        ? rgba(mix(tints[rb.tint][0], tints[rb.tint][1], 0.5), 0.16 * rb.strength)
        : rgba(lightBand(rb.tint), LIGHT_BAND_OPACITY * rb.strength),
      across: rb.crest ? [-1, 1, 0] : [-1, 1],
      dots: makeDots(r, rb, width, height),
      centre: new Float64Array(count),
      half: new Float64Array(count),
    }));

    // Soft dots: per size, a few brightness steps of a round glow, centred on the origin.
    const tint = mix(accent, WHITE, dark ? 0.8 : 1);
    const ring = mix(accent, BLACK, 0.1);
    const dotFills = DOT_SIZES.map((radius) =>
      Array.from({ length: LEVELS }, (_, i) => {
        const share = (i + 1) / LEVELS;
        const g = ctx.createRadialGradient(0, 0, 0, 0, 0, radius);
        if (dark) {
          g.addColorStop(0, rgba(tint, share * 0.9));
          g.addColorStop(0.4, rgba(tint, share * 0.45));
          g.addColorStop(1, rgba(tint, 0));
        } else {
          g.addColorStop(0, rgba(tint, share * 0.95));
          g.addColorStop(0.3, rgba(tint, share * 0.7));
          g.addColorStop(0.65, rgba(ring, share * 0.4));
          g.addColorStop(1, rgba(ring, 0));
        }
        return g;
      }));

    return {
      dark, xs, stops, ribbons, dotFills,
      // Dark: light lines glowing on the dark page. Light: lines a little deeper than the bands, in the accent.
      core: dark ? mix(accent, WHITE, 0.85) : mix(accent, BLACK, 0.45),
      halo: dark ? mix(accent, WHITE, 0.4) : mix(accent, BLACK, 0.2),
      scrim: rgba(region, dark ? DARK_SCRIM : LIGHT_SCRIM),
    };
  }

  // Where the panel is in the slot's coordinates, shrunk a pixel so the scrim's edge lies under its border.
  function readPanel() {
    if (!panelElement) return null;
    const p = panelElement.getBoundingClientRect();
    const s = slot.getBoundingClientRect();
    if (p.width <= 2 || p.height <= 2) return null;
    return { x: p.left - s.left + 1, y: p.top - s.top + 1, w: p.width - 2, h: p.height - 2 };
  }

  // Re-reads size, pixel ratio, colours, motion and panel; rebuilds what changed.
  function sync() {
    const colours = readColours();
    reduced = colours.continuous === 0;
    const w = slot.clientWidth;
    const h = slot.clientHeight;
    const ratio = window.devicePixelRatio || 1;
    const key = `${colours.accent}|${colours.region}|${colours.text}`;
    if (w !== width || h !== height || ratio !== dpr || key !== colourKey || !model) {
      if (ratio !== dpr) {
        ratioQuery?.removeEventListener("change", update);
        ratioQuery = matchMedia(`(resolution: ${ratio}dppx)`);
        ratioQuery.addEventListener("change", update);
      }
      width = w;
      height = h;
      dpr = ratio;
      colourKey = key;
      canvas.width = Math.max(1, Math.round(w * ratio));
      canvas.height = Math.max(1, Math.round(h * ratio));
      model = w > 0 && h > 0 ? build(colours) : null;
    }
    panel = readPanel();
  }

  function draw() {
    if (!model) return;
    const begin = performance.now();
    const { xs, ribbons, dark, core, halo } = model;
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, width, height);
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    const p = phase;

    for (let r = 0; r < RIBBONS.length; r++) {
      const rb = RIBBONS[r];
      const m = ribbons[r];
      for (let i = 0; i < xs.length; i++) {
        m.centre[i] = centre(rb, xs[i], height, p);
        m.half[i] = halfWidth(rb, xs[i], height, p);
      }
    }

    // The bands, back to front.
    for (let r = 0; r < RIBBONS.length; r++) {
      const rb = RIBBONS[r];
      const m = ribbons[r];
      ctx.beginPath();
      for (let i = 0; i < xs.length; i++) ctx.lineTo(xs[i], lineY(rb, -1, xs[i], m.centre[i], m.half[i], p));
      for (let i = xs.length - 1; i >= 0; i--) ctx.lineTo(xs[i], lineY(rb, 1, xs[i], m.centre[i], m.half[i], p));
      ctx.closePath();
      ctx.fillStyle = m.band;
      ctx.fill();
    }

    // The lines, then the dots, ribbon by ribbon.
    for (let r = 0; r < RIBBONS.length; r++) {
      const rb = RIBBONS[r];
      const m = ribbons[r];
      for (let l = 0; l < m.across.length; l++) {
        const across = m.across[l];
        ctx.beginPath();
        for (let i = 0; i < xs.length; i++) ctx.lineTo(xs[i], lineY(rb, across, xs[i], m.centre[i], m.half[i], p));

        // The light along the line: a stop every few px with its exact value there.
        const main = across === -1;
        const coreFill = ctx.createLinearGradient(0, 0, width, 0);
        const haloFill = main ? ctx.createLinearGradient(0, 0, width, 0) : null;
        for (let s = 0; s < model.stops; s++) {
          const offset = Math.min(1, (s * STOP_SPACING) / width);
          const value = light(r, l, offset * width, p);
          coreFill.addColorStop(offset, rgba(core, value * (dark ? 0.7 : 1)));
          haloFill?.addColorStop(offset, rgba(halo, value * (dark ? 0.18 : 0.65)));
        }
        // Only the top edge has the soft halo; every line has the bright core.
        if (main) {
          ctx.strokeStyle = haloFill;
          ctx.lineWidth = dark ? 8 : 14;
          ctx.stroke();
        }
        ctx.strokeStyle = coreFill;
        ctx.lineWidth = main ? 1.6 : 1.0;
        ctx.stroke();
      }

      if (!reduced) drawDots(r, p);
    }

    // The scrim in the page colour over everything outside the panel: the whole area less the
    // panel's rounded rectangle, filled even-odd.
    if (panel) {
      const { x, y, w, h } = panel;
      const k = Math.min(SCRIM_RADIUS, w / 2, h / 2);
      ctx.beginPath();
      ctx.rect(0, 0, width, height);
      ctx.moveTo(x + k, y);
      ctx.arcTo(x + w, y, x + w, y + h, k);
      ctx.arcTo(x + w, y + h, x, y + h, k);
      ctx.arcTo(x, y + h, x, y, k);
      ctx.arcTo(x, y, x + w, y, k);
      ctx.closePath();
      ctx.fillStyle = model.scrim;
      ctx.fill("evenodd");
    }

    frames++;
    drawMs = performance.now() - begin;
  }

  // Twinkling in alpha only, brighter where the edge beside the dot is lit; dots drift a little
  // around their place and follow their edge.
  function drawDots(r, p) {
    const rb = RIBBONS[r];
    for (const dot of model.ribbons[r].dots) {
      const cycle = dot.rate * p + dot.offset;
      const twinkle = dot.flash ? flashCurve(cycle - Math.floor(cycle)) : swellCurve(cycle);
      const strength = dot.brightness * twinkle * (0.3 + 0.7 * light(r, dot.line, dot.x, p));
      if (strength < 0.05) continue;
      const x = dot.x + dot.drift * Math.sin(TAU * p + dot.offset * 6.28);
      const side = dot.line === 0 ? -1 : 1;
      const y = centre(rb, x, height, p) + side * halfWidth(rb, x, height, p) + dot.y;
      const level = Math.min(LEVELS - 1, Math.max(0, Math.floor(strength * LEVELS)));
      const radius = DOT_SIZES[dot.size];
      ctx.setTransform(dpr, 0, 0, dpr, x * dpr, y * dpr);
      ctx.fillStyle = model.dotFills[dot.size][level];
      ctx.beginPath();
      ctx.arc(0, 0, radius, 0, TAU);
      ctx.fill();
    }
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }

  const shouldRun = () => autoAdvance && !reduced && !document.hidden && !stopped;

  function tick(now) {
    raf = 0;
    if (!shouldRun()) return;
    raf = requestAnimationFrame(tick);
    if (lastTime >= 0 && now - lastTime < FRAME_MS - FRAME_SLACK_MS) return;
    // Zero on the first frame after a start or resume; a long gap (a stall) must not jump the ribbons.
    const elapsed = lastTime < 0 ? 0 : Math.min((now - lastTime) / 1000, 0.1);
    lastTime = now;
    phase = (phase + elapsed / LOOP_SECONDS) % 1;
    sync();
    draw();
  }

  // Starts or stops the loop to match the state; a still frame is drawn whenever it is not running.
  let stillFrame = 0;
  function update() {
    if (stopped) return;
    sync();
    if (shouldRun()) {
      if (!raf) {
        lastTime = -1;
        raf = requestAnimationFrame(tick);
      }
      return;
    }
    if (raf) cancelAnimationFrame(raf);
    raf = 0;
    if (!document.hidden && !stillFrame)
      stillFrame = requestAnimationFrame(() => {
        stillFrame = 0;
        if (stopped) return;
        sync();
        draw();
      });
  }

  const resizeObserver = new ResizeObserver(update);
  resizeObserver.observe(slot);
  if (panelElement) resizeObserver.observe(panelElement);
  // Accent (inline style), theme, motion and density settings live on <html>.
  const mutationObserver = new MutationObserver(update);
  mutationObserver.observe(html, { attributes: true, attributeFilter: ["style", "class", "data-theme", "data-reduce-motion", "data-density"] });
  const queries = ["(prefers-color-scheme: dark)", "(prefers-reduced-motion: reduce)"].map((q) => matchMedia(q));
  for (const q of queries) q.addEventListener("change", update);
  document.addEventListener("visibilitychange", update);

  update();

  function stop() {
    if (stopped) return;
    stopped = true;
    if (raf) cancelAnimationFrame(raf);
    if (stillFrame) cancelAnimationFrame(stillFrame);
    raf = stillFrame = 0;
    resizeObserver.disconnect();
    mutationObserver.disconnect();
    for (const q of queries) q.removeEventListener("change", update);
    ratioQuery?.removeEventListener("change", update);
    document.removeEventListener("visibilitychange", update);
    canvas.remove();
  }

  stop.drawAt = (seconds) => {
    autoAdvance = false;
    phase = (((seconds / LOOP_SECONDS) % 1) + 1) % 1;
    if (stopped) return;
    if (raf) cancelAnimationFrame(raf);
    raf = 0;
    sync();
    draw();
  };
  stop.state = () => ({ phase, running: raf !== 0, reduced, frames, drawMs });
  return stop;
}
