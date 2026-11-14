// Button's siblings: ToggleButton, RepeatButton, SplitButton, ToggleSplitButton, DropDownButton and
// HyperlinkButton.
import { page, section } from "./ui.js";
import { stateGrid } from "./state-grid.js";

const template = document.createElement("template");
const make = (html) => () => {
  template.innerHTML = html;
  return template.content.firstElementChild;
};
const toggle = (pressed) => make(`<button type="button" class="ui-button ui-squircle ui-focus-ring" aria-pressed="${pressed}">Toggle</button>`);
const split = (pressed) => make(`<span class="ui-split-button ui-squircle ui-focus-ring-within" role="group" aria-label="Split"><button type="button" class="ui-button"${pressed ? ' aria-pressed="true"' : ""}>Split</button><button type="button" class="ui-button ui-drop-down-button" aria-label="More options" aria-haspopup="menu" aria-expanded="false"></button></span>`);

export default {
  id: "button-family",
  title: "Button family",
  icon: "check-square",
  render: () => page("Button family",
    section("ToggleButton", stateGrid([["Unchecked", toggle("false")], ["Checked", toggle("true")], ["Indeterminate", toggle("mixed")]])),
    section("RepeatButton", stateGrid([["RepeatButton", make('<button type="button" class="ui-button ui-repeat-button ui-squircle ui-focus-ring">Repeat</button>')]])),
    section("SplitButton", stateGrid([["SplitButton", split(false)], ["ToggleSplitButton, checked", split(true)]])),
    section("DropDownButton", stateGrid([["DropDownButton", make('<button type="button" class="ui-button ui-drop-down-button ui-squircle ui-focus-ring" aria-haspopup="menu" aria-expanded="false">Drop down</button>')]])),
    section("HyperlinkButton", stateGrid([["HyperlinkButton", make('<a class="ui-button ui-hyperlink-button ui-squircle ui-focus-ring" href="#button-family">Link</a>')]]))),
};
