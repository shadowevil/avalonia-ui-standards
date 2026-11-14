// TextInput behaviour (0027) that native <input> lacks, delegated from the document so it works
// for fields added at any time:
// - data-filled on a .ui-field while its .ui-text-input is not empty (clear and reveal show by it),
//   and its field buttons disabled with the input. Kept in step on input, change and focus, on
//   fields added to the page and on disabled and value attribute changes; after setting
//   input.value in script, call syncField.
// - Clear (.ui-text-input-clear) empties the input, sends an input event and keeps focus there.
// - Reveal (.ui-text-input-reveal, aria-pressed toggled by button.js) shows the password as text.
// - Field buttons never take focus from the input, and a press on the field outside the input
//   focuses it.
// - Pointer focus: a text input focused by a pointer press gets data-pointer-focus (on its field,
//   or on itself alone), which drops the ring; browsers match :focus-visible on text inputs
//   however they were focused, and the ring marks keyboard focus only (0019, 0027).
import "./button.js";

const FIELD = ".ui-field";
const INPUT = ".ui-text-input";

/** Brings a field (given the field or anything in it) in step with its input's value and state. */
export function syncField(target) {
  const field = target.closest?.(FIELD);
  const input = field?.querySelector(INPUT);
  if (!input) return;
  field.toggleAttribute("data-filled", input.value !== "");
  for (const button of field.querySelectorAll(".ui-field-button")) {
    if (button.disabled !== input.disabled) button.disabled = input.disabled;
  }
  const reveal = field.querySelector(".ui-text-input-reveal");
  if (reveal) {
    const type = reveal.getAttribute("aria-pressed") === "true" ? "text" : "password";
    if (input.type !== type) {
      const { selectionStart, selectionEnd } = input;
      input.type = type;
      input.setSelectionRange(selectionStart, selectionEnd);
    }
  }
}

function syncWithin(node) {
  if (node.nodeType !== Node.ELEMENT_NODE) return;
  if (node.closest(FIELD)) syncField(node);
  for (const field of node.querySelectorAll(FIELD)) syncField(field);
}

new MutationObserver((records) => {
  for (const r of records) {
    if (r.type === "attributes") {
      if (r.target.matches(INPUT)) syncField(r.target);
    } else r.addedNodes.forEach(syncWithin);
  }
}).observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ["disabled", "value"] });
syncWithin(document.documentElement);

for (const type of ["input", "change"]) document.addEventListener(type, (e) => syncField(e.target));

// Pointer or keyboard: the last press decides how the next focus arrived.
let pointer = false;
document.addEventListener("pointerdown", () => { pointer = true; }, true);
document.addEventListener("keydown", () => { pointer = false; }, true);

document.addEventListener("focusin", (e) => {
  if (!e.target.matches?.(INPUT)) return;
  const host = e.target.closest(FIELD) ?? e.target;
  host.toggleAttribute("data-pointer-focus", pointer);
  syncField(e.target);
});
document.addEventListener("focusout", (e) => {
  if (!e.target.matches?.(INPUT)) return;
  (e.target.closest(FIELD) ?? e.target).removeAttribute("data-pointer-focus");
});

document.addEventListener("mousedown", (e) => {
  const field = e.target.closest?.(FIELD);
  if (!field || e.button !== 0 || e.target.closest(INPUT)) return;
  e.preventDefault();
  if (!e.target.closest(".ui-field-button")) field.querySelector(INPUT)?.focus();
});

document.addEventListener("click", (e) => {
  const button = e.target.closest?.(".ui-text-input-clear, .ui-text-input-reveal");
  const input = button?.closest(FIELD)?.querySelector(INPUT);
  if (!input || button.disabled) return;
  if (button.matches(".ui-text-input-clear")) {
    input.value = "";
    input.dispatchEvent(new Event("input", { bubbles: true }));
  }
  syncField(input);
  input.focus();
});
