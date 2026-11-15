// ButtonSpinner (0029): <ui-button-spinner>, a field around any content with spin buttons. The
// element is a spinbutton (role, tabindex 0) whose aria-valuetext is the content's text. The
// content does not change by itself: Up and Down (or a spin button) fire a `spin` event whose
// detail.direction is "increase" or "decrease", and the page updates the content.
//
// Markup (button-spinner.css): the field classes and the content in .ui-button-spinner-content (a
// loose content is wrapped in one). The spin buttons are added here:
//   <ui-button-spinner class="ui-field ui-button-spinner ui-squircle ui-focus-ring ui-focus-ring-within"
//                      aria-label="Size"><span class="ui-button-spinner-content">Medium</span></ui-button-spinner>
// Attributes:
//   button-spinner-location="left"   the spin buttons at the leading end (default: trailing)
//   valid-spin-direction             "both" (default), "increase", "decrease" or "none": the other
//                                    direction's button takes the disabled colour and does nothing
//   disabled                         the field is disabled
//   increase-label, decrease-label   the buttons' accessible names ("Increase", "Decrease")
// The spin buttons are repeat buttons (button.js) and not tab stops. Pressing one, or the content,
// moves focus to the field (the buttons never take it themselves, as the other field buttons).
// Up and Down spin from the field. aria-invalid="true" on the element turns the
// border the danger colour.
//
// createSpinButtons is shared with NumericUpDown, which is a field of the same kind.
import { icon } from "./icon.js";
import "./button.js";
import "./text-input.js";

/**
 * Adds the two spin buttons to `host`, at its trailing end or (button-spinner-location="left")
 * its leading end. onSpin(direction) is called with 1 or -1; `focusTarget` takes focus when a
 * button is pressed. Returns { refresh(), setValid(increase, decrease) }: refresh places the group
 * and sets the labels again after an attribute changed.
 */
export function createSpinButtons(host, onSpin, focusTarget) {
  const group = document.createElement("span");
  const make = (name, direction) => {
    const b = document.createElement("button");
    b.type = "button";
    b.tabIndex = -1;
    b.className = `ui-button ui-subtle ui-field-button ui-spin-button ui-spin-${name} ui-repeat-button ui-squircle`;
    b.append(icon(direction > 0 ? "caret-up" : "caret-down"));
    b.dataset.direction = String(direction);
    return b;
  };
  const increase = make("increase", 1);
  const decrease = make("decrease", -1);
  group.append(increase, decrease);

  // The press keeps focus where it is (or on the field), not on the button.
  group.addEventListener("mousedown", (e) => {
    if (e.button === 0 && e.target.closest(".ui-spin-button")) e.preventDefault();
  });
  group.addEventListener("pointerdown", (e) => {
    if (e.button === 0 && e.target.closest(".ui-spin-button")) focusTarget().focus({ preventScroll: true });
  });
  group.addEventListener("click", (e) => {
    const b = e.target.closest(".ui-spin-button");
    if (!b || b.disabled || b.getAttribute("aria-disabled") === "true") return;
    onSpin(Number(b.dataset.direction));
  });

  const refresh = () => {
    const left = host.getAttribute("button-spinner-location") === "left";
    group.className = left ? "ui-field-leading ui-spinner" : "ui-field-trailing ui-spinner";
    if (left && host.firstElementChild !== group) host.prepend(group);
    if (!left && host.lastElementChild !== group) host.append(group);
    increase.setAttribute("aria-label", host.getAttribute("increase-label") ?? "Increase");
    decrease.setAttribute("aria-label", host.getAttribute("decrease-label") ?? "Decrease");
  };

  // A button at its limit is aria-disabled (not disabled, which text-input.js sets for a
  // disabled field): it stays out of the Tab order anyway and keeps its place.
  const setValid = (canIncrease, canDecrease) => {
    for (const [b, valid] of [[increase, canIncrease], [decrease, canDecrease]]) {
      if (valid) b.removeAttribute("aria-disabled");
      else b.setAttribute("aria-disabled", "true");
    }
  };

  refresh();
  return { refresh, setValid, group };
}

const CONTENT = ".ui-button-spinner-content";

export class UiButtonSpinner extends HTMLElement {
  static observedAttributes = ["button-spinner-location", "valid-spin-direction", "disabled", "increase-label", "decrease-label"];

  #spinner = null;
  #observer = new MutationObserver(() => this.#syncValueText());

  connectedCallback() {
    if (!this.querySelector(CONTENT)) {
      const content = document.createElement("span");
      content.className = "ui-button-spinner-content";
      content.append(...this.childNodes);
      this.append(content);
    }
    this.#spinner ??= createSpinButtons(this, (d) => this.spin(d), () => this);
    this.addEventListener("keydown", this.#onKeyDown);
    this.#observer.observe(this, { childList: true, characterData: true, subtree: true });
    this.#sync();
  }

  disconnectedCallback() {
    this.removeEventListener("keydown", this.#onKeyDown);
    this.#observer.disconnect();
  }

  attributeChangedCallback() {
    if (this.isConnected && this.#spinner) this.#sync();
  }

  get disabled() {
    return this.hasAttribute("disabled");
  }

  set disabled(value) {
    this.toggleAttribute("disabled", Boolean(value));
  }

  /** "both", "increase", "decrease" or "none". */
  get validSpinDirection() {
    const v = this.getAttribute("valid-spin-direction");
    return ["increase", "decrease", "none"].includes(v) ? v : "both";
  }

  set validSpinDirection(value) {
    this.setAttribute("valid-spin-direction", value);
  }

  /** Spins one step: 1 increases and -1 decreases. Fires `spin`; does nothing when disabled or not valid. */
  spin(direction) {
    const valid = this.validSpinDirection;
    const ok = valid === "both" || (valid === "increase" && direction > 0) || (valid === "decrease" && direction < 0);
    if (this.disabled || !ok) return;
    this.dispatchEvent(new CustomEvent("spin", { bubbles: true, detail: { direction: direction > 0 ? "increase" : "decrease" } }));
  }

  #sync() {
    this.#spinner.refresh();
    const valid = this.validSpinDirection;
    this.#spinner.setValid(valid === "both" || valid === "increase", valid === "both" || valid === "decrease");
    this.setAttribute("role", "spinbutton");
    if (this.disabled) {
      this.setAttribute("aria-disabled", "true");
      this.removeAttribute("tabindex");
    } else {
      this.removeAttribute("aria-disabled");
      this.tabIndex = 0;
    }
    for (const b of this.querySelectorAll(".ui-spin-button")) b.disabled = this.disabled;
    this.#syncValueText();
  }

  #syncValueText() {
    const text = this.querySelector(CONTENT)?.textContent.trim() ?? "";
    if (this.getAttribute("aria-valuetext") !== text) this.setAttribute("aria-valuetext", text);
  }

  #onKeyDown = (e) => {
    if (e.defaultPrevented || e.altKey || e.ctrlKey || e.metaKey || this.disabled) return;
    if (e.key !== "ArrowUp" && e.key !== "ArrowDown") return;
    e.preventDefault();
    this.spin(e.key === "ArrowUp" ? 1 : -1);
  };
}

if (!customElements.get("ui-button-spinner")) customElements.define("ui-button-spinner", UiButtonSpinner);
