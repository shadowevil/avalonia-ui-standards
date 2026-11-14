// Toggles: CheckBox, RadioButton and ToggleSwitch, each check state in every state.
import { el, page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

let radios = 0;

function input(type, className) {
  const i = el("input", className);
  i.type = type;
  return i;
}

function checkBox(state) {
  const box = input("checkbox", "ui-check-box ui-squircle ui-focus-ring");
  box.checked = state === "checked";
  box.indeterminate = state === "indeterminate";
  return el("label", "ui-toggle", box, "Label");
}

function radioButton(checked) {
  const radio = input("radio", "ui-radio-button ui-focus-ring");
  radio.name = `catalog-radio-${++radios}`; // each sample is a group of its own
  radio.checked = checked;
  return el("label", "ui-toggle", radio, "Label");
}

function toggleSwitch(on, header) {
  const toggle = input("checkbox", "ui-toggle-switch ui-focus-ring");
  toggle.setAttribute("role", "switch");
  toggle.checked = on;
  if (!header) toggle.setAttribute("aria-label", "Switch");
  const content = el("span", "ui-toggle-switch-content",
    el("span", "ui-toggle-switch-off", "Off"), el("span", "ui-toggle-switch-on", "On"));
  content.setAttribute("aria-hidden", "true");
  return el("label", "ui-toggle-switch-label",
    ...(header ? [el("span", "ui-toggle-switch-header", header)] : []), toggle, content);
}

/** Forces a state on the sample's input: hover, press and focus through data-state, disabled natively. */
function force(sample, state) {
  const control = sample.querySelector("input");
  if (state === "Hover") control.dataset.state = "hover";
  if (state === "Pressed") control.dataset.state = "pressed";
  if (state === "Focused") control.dataset.state = "focus";
  if (state === "Disabled") control.disabled = true;
}

export default {
  id: "toggles",
  title: "Toggles",
  icon: "toggle-right",
  render: () => page("Toggles",
    section("CheckBox", stateGrid([
      ["Unchecked", () => checkBox("unchecked")],
      ["Checked", () => checkBox("checked")],
      ["Indeterminate", () => checkBox("indeterminate")],
    ], { force })),
    section("RadioButton", stateGrid([
      ["Unchecked", () => radioButton(false)],
      ["Checked", () => radioButton(true)],
    ], { force })),
    section("ToggleSwitch", stateGrid([
      ["Off", () => toggleSwitch(false)],
      ["On", () => toggleSwitch(true)],
      ["With header", () => toggleSwitch(false, "Header")],
    ], { force }))),
};
