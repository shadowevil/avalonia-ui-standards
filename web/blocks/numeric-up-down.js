// NumericUpDown (0029): <ui-numeric-up-down>, a TextInput field around a native
// <input type="number"> (a spinbutton natively: min, max, step, value, the arrow keys and
// valueAsNumber are the browser's), with spin buttons added here. Markup (numeric-up-down.css):
//   <ui-numeric-up-down class="ui-field ui-numeric-up-down ui-squircle ui-focus-ring-within">
//     <input class="ui-text-input" type="number" value="42" min="0" max="100" aria-label="Quantity">
//   </ui-numeric-up-down>
// A spin button steps the input as its arrow keys do (stepUp and stepDown; step="any" steps by
// 1), fires input and change, and keeps focus in the input; typed text is taken up before it
// steps. A button at its limit (the value at max for increase, at min for decrease) takes the
// disabled colour and does nothing. Attributes on the element:
//   button-spinner-location="left"   the buttons at the leading end (default: trailing)
//   increase-label, decrease-label   the buttons' accessible names ("Increase", "Decrease")
// The input's disabled, aria-invalid and placeholder act as on a TextInput (text-input.js).
import { createSpinButtons } from "./button-spinner.js";

export class UiNumericUpDown extends HTMLElement {
  static observedAttributes = ["button-spinner-location", "increase-label", "decrease-label"];

  #spinner = null;
  #observer = new MutationObserver(() => this.#syncLimits());

  get input() {
    return this.querySelector("input");
  }

  connectedCallback() {
    this.#spinner ??= createSpinButtons(this, (d) => this.spin(d), () => this.input);
    this.addEventListener("input", this.#onChange);
    this.addEventListener("change", this.#onChange);
    this.#observer.observe(this, { attributes: true, attributeFilter: ["min", "max", "value", "step"], subtree: true });
    this.#spinner.refresh();
    this.#syncLimits();
  }

  disconnectedCallback() {
    this.removeEventListener("input", this.#onChange);
    this.removeEventListener("change", this.#onChange);
    this.#observer.disconnect();
  }

  attributeChangedCallback() {
    if (this.isConnected && this.#spinner) this.#spinner.refresh();
  }

  /** Steps the value: 1 increases and -1 decreases, by the input's step, within min and max. */
  spin(direction) {
    const input = this.input;
    if (!input || input.disabled || input.readOnly) return;
    const before = input.value;
    const min = input.min === "" ? -Infinity : Number(input.min);
    const max = input.max === "" ? Infinity : Number(input.max);
    const typed = input.valueAsNumber;
    if (typed > max || typed < min) {
      // Out of range (typed text): the first step snaps into range.
      input.value = String(Math.min(max, Math.max(min, typed)));
    } else try {
      if (direction > 0) input.stepUp();
      else input.stepDown();
    } catch {
      // step="any": native stepping is not defined, step by 1.
      const n = (typed || 0) + direction;
      input.value = String(Math.min(max, Math.max(min, n)));
    }
    if (input.value !== before) {
      input.dispatchEvent(new Event("input", { bubbles: true }));
      input.dispatchEvent(new Event("change", { bubbles: true }));
    }
    this.#syncLimits();
  }

  #onChange = () => this.#syncLimits();

  #syncLimits() {
    const input = this.input;
    if (!input || !this.#spinner) return;
    const value = input.valueAsNumber;
    const min = input.min === "" ? -Infinity : Number(input.min);
    const max = input.max === "" ? Infinity : Number(input.max);
    const known = !Number.isNaN(value);
    this.#spinner.setValid(!(known && value >= max), !(known && value <= min));
  }
}

if (!customElements.get("ui-numeric-up-down")) customElements.define("ui-numeric-up-down", UiNumericUpDown);
