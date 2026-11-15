// ComboBox: the closed field in every kind and state, live ComboBoxes, and fields lined up in a row.
import { createComboBox } from "../../blocks/combobox.js";
import { el, caption, page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

const FOLDERS = ["Documents", "Downloads", "Pictures", "Music"];
const STATES = ["Rest", "Hover", "Focused", "Keyboard focus", "Error", "Disabled"];

const box = (label, options) => createComboBox(FOLDERS, { label, width: 200, ...options });

// The chrome element holds hover and focus; the control (button or input) holds the error.
function force(sample, state) {
  const field = sample.querySelector(".ui-combo-box-field");
  const control = field.matches("button") ? field : field.querySelector(".ui-text-input");
  const editable = sample.hasAttribute("editable");
  if (state === "Hover") field.dataset.state = "hover";
  // Pointer focus changes nothing on a field that is not editable; editable it turns the border accent.
  if (state === "Focused" && editable) {
    field.dataset.state = "focus";
    field.setAttribute("data-pointer-focus", "");
  }
  if (state === "Keyboard focus") field.dataset.state = "focus";
  if (state === "Error") control.setAttribute("aria-invalid", "true");
  if (state === "Disabled") sample.setAttribute("disabled", "");
}

function inRow() {
  const name = el("input", "ui-text-input ui-squircle ui-focus-ring");
  name.placeholder = "Name";
  name.setAttribute("aria-label", "Name");
  name.style.inlineSize = "160px";
  const save = el("button", "ui-button ui-squircle ui-focus-ring", "Save");
  save.type = "button";
  const row = el("div", "", name, createComboBox(FOLDERS, { label: "Folder", width: 160, selected: 0 }), save);
  Object.assign(row.style, { display: "flex", flexWrap: "wrap", alignItems: "center", gap: "var(--ui-spacing-s)" });
  return el("div", "catalog-stack", row, caption("A ComboBox has the TextInput chrome, so fields and buttons line up."));
}

export default {
  id: "combobox",
  title: "ComboBox",
  icon: "list-bullets",
  render: () => page("ComboBox",
    section("Kinds and states", stateGrid([
      ["Placeholder", (l) => box(l, { placeholder: "Choose a folder" })],
      ["Selected", (l) => box(l, { selected: 1 })],
      ["Editable", (l) => box(l, { editable: true, selected: 2 })],
      ["Editable, placeholder", (l) => box(l, { editable: true, placeholder: "Type or choose" })],
    ], { states: STATES, force })),
    section("In a row", inRow())),
};
