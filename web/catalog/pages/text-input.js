// TextInput: every kind in every state, live fields, and a field lined up with buttons.
import { icon } from "../../ui.js";
import { el, caption, page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

const STATES = ["Rest", "Hover", "Focused", "Keyboard focus", "Error", "Disabled"];

function input(label, { value = "", placeholder = "", type = "text", width = 200, chrome = true } = {}) {
  const i = el("input", chrome ? "ui-text-input ui-squircle ui-focus-ring" : "ui-text-input");
  i.type = type;
  i.value = value;
  i.placeholder = placeholder;
  i.setAttribute("aria-label", label);
  if (chrome) i.style.inlineSize = `${width}px`;
  return i;
}

function fieldButton(className, label, ...icons) {
  const b = el("button", `ui-button ui-subtle ui-field-button ${className} ui-squircle`, ...icons);
  b.type = "button";
  b.tabIndex = -1;
  b.setAttribute("aria-label", label);
  return b;
}

/** A field with the clear button, or the reveal button for a password. */
function field(label, { password = false, width = 200, ...options } = {}) {
  const button = password
    ? fieldButton("ui-text-input-reveal", "Show password", icon("eye"), icon("eye-slash"))
    : fieldButton("ui-text-input-clear", "Clear", icon("x-circle"));
  if (password) button.setAttribute("aria-pressed", "false");
  const f = el("span", "ui-field ui-squircle ui-focus-ring-within",
    input(label, { ...options, type: password ? "password" : "text", chrome: false }),
    el("span", "ui-field-trailing", button));
  f.style.inlineSize = `${width}px`;
  return f;
}

function multiLine(label) {
  const t = el("textarea", "ui-text-input ui-squircle ui-focus-ring");
  t.value = "Multi-line text wraps in the same chrome, from the top padding.";
  t.setAttribute("aria-label", label);
  t.style.inlineSize = "200px";
  t.style.blockSize = "76px";
  return t;
}

// The chrome element holds hover and focus; the input holds error and disabled.
function force(sample, state) {
  const text = sample.matches(".ui-text-input") ? sample : sample.querySelector(".ui-text-input");
  if (state === "Hover") sample.dataset.state = "hover";
  if (state === "Focused" || state === "Keyboard focus") sample.dataset.state = "focus";
  if (state === "Focused") sample.setAttribute("data-pointer-focus", "");
  if (state === "Error") text.setAttribute("aria-invalid", "true");
  if (state === "Disabled") text.disabled = true;
}

function linedUp() {
  const cancel = el("button", "ui-button ui-squircle ui-focus-ring", "Cancel");
  const save = el("button", "ui-button ui-accent ui-squircle ui-focus-ring", "Save");
  cancel.type = save.type = "button";
  const row = el("div", "", field("File name", { placeholder: "Name the file", width: 240 }), cancel, save);
  Object.assign(row.style, { display: "flex", flexWrap: "wrap", alignItems: "center", gap: "var(--ui-spacing-s)" });
  return el("div", "catalog-stack", row,
    caption("Fields and buttons share the height (32 Normal, 24 Compact), the corner radius and the side padding."));
}

export default {
  id: "textinput",
  title: "TextInput",
  icon: "chat-text",
  render: () => page("TextInput",
    section("Kinds and states", stateGrid([
      ["Placeholder", (l) => input(l, { placeholder: "Search files" })],
      ["Text", (l) => input(l, { value: "Quarterly report" })],
      ["Clear button", (l) => field(l, { value: "Quarterly report" })],
      ["Password", (l) => field(l, { value: "hunter22", password: true })],
      ["Multi-line", multiLine],
    ], { states: STATES, force })),
    section("Lined up with buttons", linedUp())),
};
