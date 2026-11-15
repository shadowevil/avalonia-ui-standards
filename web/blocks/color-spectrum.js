// ColorSpectrum (0041, 0046, 0048): <ui-color-spectrum>, the hue and saturation area of the
// ColorView (markup and look in color-spectrum.css). No native element picks a point in two
// dimensions, so it is a custom element.
//
// Hue (0 to 360) runs along the box from its start edge (around the ring, clockwise from the end
// side), saturation (0 to 1) from the bottom up (from the ring's centre out); the area is drawn at
// full value, and the value (0 to 1) is carried for the owner. Properties: hue, saturation, value, and hsv ({ h, s, v }) to read or set
// them together; setting them fires nothing.
//
// Role: a slider (the element takes focus; one Tab stop), its value the hue, with aria-valuetext
// naming both: "Hue 208°, Saturation 100%". The words are the attributes hue-text and
// saturation-text (defaults Hue, Saturation); value-text replaces the whole text when set (the
// ColorView adds the colour's name).
//
// Keys: Left and Right change the hue (swapped right to left, so the selector moves the way of the
// arrow), Up and Down the saturation, by 1 (1 %); with Ctrl (Cmd on macOS) by 10, stopping at the
// ends. The arrows are always handled, so they never scroll the page. Pointer: a press picks the
// point under it and dragging follows it (the pointer is captured); a touch press shows the large
// selector. Events: input on every change by the user, change when a key press or a drag ends
// with a change.
//
// Attributes: shape ("box", the default, or "ring"), disabled (aria-disabled is set from it; no
// focus, no input). Forced states for renders and tests: data-state="hover", "pressed" or "focus".
import { isMac } from "../style/platform.js";

const clamp = (v, lo, hi) => Math.min(hi, Math.max(lo, v));

export class UiColorSpectrum extends HTMLElement {
  static observedAttributes = ["shape", "disabled", "hue-text", "saturation-text", "value-text"];

  #h = 0;
  #s = 0;
  #v = 1;
  #thumb = null;
  #area = null;
  #dragged = false;

  connectedCallback() {
    if (!this.#thumb) {
      this.#area = document.createElement("span");
      this.#area.className = "ui-color-spectrum-area ui-squircle";
      this.#thumb = document.createElement("span");
      this.#thumb.className = "ui-color-spectrum-thumb";
      for (const part of [this.#area, this.#thumb]) part.setAttribute("aria-hidden", "true");
      this.replaceChildren(this.#area, this.#thumb);
      this.addEventListener("keydown", this.#onKeyDown);
      this.addEventListener("pointerdown", this.#onPointerDown);
      this.addEventListener("pointermove", this.#onPointerMove);
      this.addEventListener("pointerup", this.#onPointerUp);
      this.addEventListener("pointercancel", this.#onPointerUp);
      this.addEventListener("lostpointercapture", this.#onPointerUp);
    }
    this.setAttribute("role", "slider");
    this.#render();
  }

  attributeChangedCallback() {
    if (this.#thumb) this.#render();
  }

  get disabled() {
    return this.hasAttribute("disabled");
  }

  set disabled(on) {
    this.toggleAttribute("disabled", Boolean(on));
  }

  get shape() {
    return this.getAttribute("shape") === "ring" ? "ring" : "box";
  }

  get hue() {
    return this.#h;
  }

  set hue(h) {
    this.hsv = { h };
  }

  get saturation() {
    return this.#s;
  }

  set saturation(s) {
    this.hsv = { s };
  }

  get value() {
    return this.#v;
  }

  set value(v) {
    this.hsv = { v };
  }

  get hsv() {
    return { h: this.#h, s: this.#s, v: this.#v };
  }

  set hsv({ h = this.#h, s = this.#s, v = this.#v }) {
    this.#h = clamp(Number(h) || 0, 0, 360);
    this.#s = clamp(Number(s) || 0, 0, 1);
    this.#v = clamp(Number(v) || 0, 0, 1);
    if (this.#thumb) this.#render();
  }

  #rtl() {
    return getComputedStyle(this).direction === "rtl";
  }

  #render() {
    const disabled = this.disabled;
    if (disabled) this.setAttribute("aria-disabled", "true");
    else this.removeAttribute("aria-disabled");
    if (disabled) this.removeAttribute("tabindex");
    else this.tabIndex = 0;
    if (disabled && this.matches(":focus")) this.blur();

    // The point, from the physical left and top; the area is mirrored right to left.
    let x;
    let y;
    if (this.shape === "ring") {
      const angle = (this.#h * Math.PI) / 180;
      x = 0.5 + 0.5 * this.#s * Math.cos(angle);
      y = 0.5 + 0.5 * this.#s * Math.sin(angle);
    } else {
      x = this.#h / 360;
      y = 1 - this.#s;
    }
    if (this.#rtl()) x = 1 - x;
    this.#thumb.style.setProperty("--_x", String(x));
    this.#thumb.style.setProperty("--_y", String(y));

    const h = Math.round(this.#h);
    const s = Math.round(this.#s * 100);
    this.setAttribute("aria-valuemin", "0");
    this.setAttribute("aria-valuemax", "360");
    this.setAttribute("aria-valuenow", String(h));
    const text = this.getAttribute("value-text")
      ?? `${this.getAttribute("hue-text") ?? "Hue"} ${h}°, ${this.getAttribute("saturation-text") ?? "Saturation"} ${s}%`;
    this.setAttribute("aria-valuetext", text);
  }

  // Sets hue and saturation from the user and fires input when they changed.
  #set(h, s) {
    h = clamp(h, 0, 360);
    s = clamp(s, 0, 1);
    if (h === this.#h && s === this.#s) return false;
    this.#h = h;
    this.#s = s;
    this.#render();
    this.dispatchEvent(new Event("input", { bubbles: true }));
    return true;
  }

  #onKeyDown = (event) => {
    if (this.disabled || event.altKey) return;
    const keys = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, 1], ArrowDown: [0, -1] };
    const step = keys[event.key];
    if (!step) return;
    event.preventDefault();
    const large = isMac ? event.metaKey : event.ctrlKey;
    const amount = large ? 10 : 1;
    const along = this.#rtl() ? -step[0] : step[0];
    // Hue in whole degrees, saturation in whole percent, so steps land on round values.
    const h = along ? Math.round(this.#h) + along * amount : this.#h;
    const s = step[1] ? (Math.round(this.#s * 100) + step[1] * amount) / 100 : this.#s;
    if (this.#set(h, s)) this.dispatchEvent(new Event("change", { bubbles: true }));
  };

  #pick(event) {
    const r = this.getBoundingClientRect();
    let x = clamp((event.clientX - r.left) / r.width, 0, 1);
    const y = clamp((event.clientY - r.top) / r.height, 0, 1);
    if (this.#rtl()) x = 1 - x;
    if (this.shape === "ring") {
      const dx = x - 0.5;
      const dy = y - 0.5;
      const angle = (Math.atan2(dy, dx) * 180) / Math.PI;
      return this.#set((angle + 360) % 360, Math.hypot(dx, dy) * 2);
    }
    return this.#set(x * 360, 1 - y);
  }

  #onPointerDown = (event) => {
    if (this.disabled || event.button !== 0) return;
    this.setPointerCapture(event.pointerId);
    this.toggleAttribute("data-pressed", true);
    this.toggleAttribute("data-large", event.pointerType === "touch");
    this.focus({ preventScroll: true });
    this.#dragged = this.#pick(event);
  };

  #onPointerMove = (event) => {
    if (!this.hasAttribute("data-pressed")) return;
    if (this.#pick(event)) this.#dragged = true;
  };

  #onPointerUp = () => {
    if (!this.hasAttribute("data-pressed")) return;
    this.removeAttribute("data-pressed");
    this.removeAttribute("data-large");
    if (this.#dragged) this.dispatchEvent(new Event("change", { bubbles: true }));
    this.#dragged = false;
  };
}

if (!customElements.get("ui-color-spectrum")) customElements.define("ui-color-spectrum", UiColorSpectrum);
