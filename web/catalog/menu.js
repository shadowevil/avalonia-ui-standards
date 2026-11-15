// The catalog's menus (0044, 0020), as the desktop catalog's CatalogMenu: Edit acting on the
// focused text field, View with the theme and density in step with the toolbar band and page
// navigation, and the application menu's About item, which the menu row adds to a Help title.
// The browser owns the window, so the desktop's File (Close window), View's full screen item and
// Help's settings file location (the settings are in localStorage) are not here.

const TEXT_TYPES = ["button", "checkbox", "radio", "range", "color", "file", "submit", "reset", "image"];
const isTextField = (el) =>
  el instanceof Element && (el.isContentEditable || el.localName === "textarea" || (el.localName === "input" && !TEXT_TYPES.includes(el.type)));

/**
 * The catalog's menu definitions for createMenuRow: { menu, appMenu }.
 *   settings     the live settings object (theme, density)
 *   setSetting   (key, value): applies a setting as the toolbar band does
 *   movePage     (step): shows the page `step` away, wrapping
 *   appName, version: for the About item and its dialog
 */
export function createCatalogMenu({ settings, setSetting, movePage, appName, version }) {
  // The text field the Edit entries act on: the focused one; focus moving into the menu row keeps it.
  let text = null;
  document.addEventListener("focusin", (e) => {
    if (isTextField(e.target)) text = e.target;
    else if (!e.target.closest?.(".ui-menu-row")) text = null;
  });

  const textAction = (header, gesture, act) => ({
    header,
    gesture,
    get enabled() {
      return !!text?.isConnected;
    },
    onClick() {
      if (!text?.isConnected) return;
      text.focus({ preventScroll: true });
      act(text);
    },
  });

  const radios = (key, options) =>
    options.map(([header, value]) => ({
      header,
      toggle: "radio",
      get checked() {
        return settings[key] === value;
      },
      onClick() {
        setSetting(key, value);
      },
    }));

  const menu = [
    {
      header: "_Edit",
      items: [
        textAction("_Undo", "Mod+Z", () => document.execCommand("undo")),
        textAction("_Redo", "Mod+Y", () => document.execCommand("redo")),
        "-",
        textAction("Cu_t", "Mod+X", () => document.execCommand("cut")),
        textAction("_Copy", "Mod+C", () => document.execCommand("copy")),
        textAction("_Paste", "Mod+V", (field) =>
          navigator.clipboard?.readText().then((value) => {
            field.focus({ preventScroll: true });
            document.execCommand("insertText", false, value);
          }).catch(() => {})),
        "-",
        textAction("Select _all", "Mod+A", (field) => (field.select ? field.select() : document.execCommand("selectAll"))),
      ],
    },
    {
      header: "_View",
      items: [
        { header: "_Theme", items: radios("theme", [["_System", "system"], ["_Light", "light"], ["_Dark", "dark"]]) },
        { header: "_Density", items: radios("density", [["_Normal", "normal"], ["_Compact", "compact"]]) },
        "-",
        { header: "_Next page", onClick: () => movePage(1) },
        { header: "_Previous page", onClick: () => movePage(-1) },
      ],
    },
  ];

  const appMenu = [{ header: `About ${appName}`, onClick: () => showAbout(appName, version) }];
  return { menu, appMenu };
}

let about = null;

// A small dialog with the app name and the library version, as the desktop's About window.
function showAbout(appName, version) {
  if (!about) {
    about = document.createElement("dialog");
    about.className = "catalog-about ui-glass ui-squircle";
    about.setAttribute("aria-label", `About ${appName}`);
    const name = document.createElement("h2");
    name.textContent = appName;
    const library = document.createElement("p");
    library.textContent = `AvaloniaUiStandards ${version}`;
    const form = document.createElement("form");
    form.method = "dialog";
    const close = document.createElement("button");
    close.className = "ui-button ui-squircle ui-focus-ring";
    close.textContent = "Close";
    form.append(close);
    about.append(name, library, form);
    document.body.append(about);
  }
  if (!about.open) about.showModal();
}
