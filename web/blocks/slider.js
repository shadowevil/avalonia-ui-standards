// Slider (0031): CSS draws the value part of the track from --ui-slider-fraction (0 to 1) and the
// ticks from --ui-slider-tick-count; this keeps both in step with the <input type="range">, whose
// value CSS cannot read in Chromium and Safari. It follows user input, the value, min, max and
// data-tick-frequency attributes, form reset, and value changes made by script (the element's
// value, valueAsNumber, stepUp and stepDown still do what the browser does, then update the
// track). Behaviour and keys stay the browser's.
const SELECTOR = "input.ui-slider";
const hooked = new WeakSet();

function number(text, fallback) {
  const n = parseFloat(text);
  return Number.isFinite(n) ? n : fallback;
}

/** Updates a slider's fraction and tick count from its value, range and tick frequency. */
function updateSlider(slider) {
  const min = number(slider.min, 0);
  const max = number(slider.max, 100);
  const span = max - min;
  const value = number(slider.value, min);
  const fraction = span > 0 ? Math.min(1, Math.max(0, (value - min) / span)) : 0;
  slider.style.setProperty("--ui-slider-fraction", String(fraction));
  const frequency = number(slider.dataset.tickFrequency, 0);
  if (frequency > 0 && span > 0) slider.style.setProperty("--ui-slider-tick-count", String(span / frequency));
  else slider.style.removeProperty("--ui-slider-tick-count");
}

// Value changes made by script fire no event and change no attribute, so the element's own
// setters are wrapped to update the track after the native setter has run.
function hook(slider) {
  if (hooked.has(slider)) return;
  hooked.add(slider);
  const proto = HTMLInputElement.prototype;
  for (const name of ["value", "valueAsNumber"]) {
    const { get, set } = Object.getOwnPropertyDescriptor(proto, name);
    Object.defineProperty(slider, name, {
      configurable: true,
      get() { return get.call(this); },
      set(v) { set.call(this, v); updateSlider(this); },
    });
  }
  for (const name of ["stepUp", "stepDown"]) {
    const native = proto[name];
    Object.defineProperty(slider, name, {
      configurable: true,
      writable: true,
      value(...args) { native.apply(this, args); updateSlider(this); },
    });
  }
}

function adopt(slider) {
  hook(slider);
  updateSlider(slider);
}

function adoptWithin(root) {
  if (root.matches?.(SELECTOR)) adopt(root);
  root.querySelectorAll?.(SELECTOR).forEach(adopt);
}

const observer = new MutationObserver((records) => {
  for (const record of records) {
    if (record.type === "attributes") {
      if (record.target.matches(SELECTOR)) updateSlider(record.target);
    }
    else record.addedNodes.forEach((node) => node.nodeType === 1 && adoptWithin(node));
  }
});

function onInput(event) {
  if (event.target.matches?.(SELECTOR)) updateSlider(event.target);
}

function start() {
  adoptWithin(document);
  observer.observe(document, {
    subtree: true, childList: true, attributes: true,
    attributeFilter: ["value", "min", "max", "data-tick-frequency"],
  });
  document.addEventListener("input", onInput, true);
  document.addEventListener("change", onInput, true);
  // A form reset restores the values after the event, so the track is updated a task later.
  document.addEventListener("reset", (event) => setTimeout(() => adoptWithin(event.target)), true);
}

if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", start, { once: true });
else start();
