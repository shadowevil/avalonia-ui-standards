// ToolTip and notifications: the tooltip card, a card of each type, and live tooltips and
// notifications.
import { icon } from "../../blocks/icon.js";
import { tooltip } from "../../blocks/tooltip.js";
import { el, caption, page, section, sideScroll, spaced } from "./ui.js";

const SAMPLES = [
  ["information", "Update available", "Restart to finish installing."],
  ["success", "Saved", "The document was saved."],
  ["warning", "Low disk space", "Less than 1 GB is free on the startup disk."],
  ["error", "Sync failed", "Check your connection and try again."],
];
const TYPE_ICONS = { information: "info", success: "check-circle", warning: "warning", error: "warning-circle" };
// Avalonia's default expiration and the desktop page's MaxItems.
const EXPIRATION = 5000;
const MAX_ITEMS = 4;

function wrapRow(...controls) {
  const row = el("div", "", ...controls.map(spaced));
  Object.assign(row.style, { display: "flex", flexWrap: "wrap", alignItems: "center" });
  return row;
}

// A tooltip shown in place, as it looks when open.
function openTip(text) {
  const tip = el("div", "ui-tooltip ui-squircle", text);
  tip.setAttribute("role", "tooltip");
  return tip;
}

function withTip(label, text, { disabled = false } = {}) {
  const button = el("button", "ui-button ui-squircle ui-focus-ring", label);
  button.type = "button";
  button.disabled = disabled;
  const host = el("span", "", button);
  tooltip(button, text);
  return host;
}

function card(type, title, message, onClose) {
  const close = el("button", "ui-button ui-subtle ui-squircle ui-focus-ring ui-notification-close", icon("x"));
  close.type = "button";
  close.setAttribute("aria-label", "Close");
  if (onClose) close.addEventListener("click", onClose);
  const typeIcon = icon(TYPE_ICONS[type]);
  typeIcon.classList.add("ui-notification-icon");
  const c = el("div", "ui-notification ui-glass ui-squircle", typeIcon,
    el("div", "ui-notification-text", el("div", "ui-notification-title", title), el("div", "ui-notification-message", message)),
    close);
  c.dataset.type = type;
  c.setAttribute("role", type === "error" ? "alert" : "status");
  return c;
}

function cards() {
  const column = el("div", "", ...SAMPLES.map(([type, title, message]) => card(type, title, message)));
  Object.assign(column.style, { display: "flex", flexDirection: "column", alignItems: "flex-start" });
  column.inert = true;
  return sideScroll(column);
}

// The live notifications: an area at the top end of the window, shown from the start (a manual
// popover in the top layer, so its cards are first-level glass), so assistive technology
// announces each card as it arrives.
function showButton(area) {
  let next = 0;
  const button = el("button", "ui-button ui-squircle ui-focus-ring", "Show a notification");
  button.type = "button";
  const dismiss = (c) => {
    if (c.dataset.closing) return;
    c.dataset.closing = "true";
    c.addEventListener("animationend", (e) => {
      if (e.animationName === "ui-notification-fade-out") c.remove();
    });
  };
  button.addEventListener("click", () => {
    const [type, title, message] = SAMPLES[next++ % SAMPLES.length];
    const c = card(type, title, message, () => dismiss(c));
    area.append(c);
    const open = [...area.children].filter((x) => !x.dataset.closing);
    for (const old of open.slice(0, Math.max(0, open.length - MAX_ITEMS))) old.remove();
    setTimeout(() => dismiss(c), EXPIRATION);
  });
  return button;
}

export default {
  id: "tooltip-and-notifications",
  title: "ToolTip and notifications",
  icon: "info",
  render: () => {
    const area = el("div", "ui-notification-area");
    area.popover = "manual";
    area.setAttribute("aria-live", "polite");
    // Shown once the page is in the document; it closes when the page is removed.
    queueMicrotask(() => {
      if (area.isConnected) area.showPopover();
    });
    return page("ToolTip and notifications",
      section("ToolTip", el("div", "catalog-stack",
        wrapRow(openTip("Save (Ctrl+S)"), openTip("Saves the document to its current location.")),
        wrapRow(withTip("Hover for a tooltip", "Save (Ctrl+S)"), withTip("Disabled, with a tooltip", "Not available while offline", { disabled: true })),
        caption("A solid card in the card colour with a 1 px border, radius 8 and caption text. It appears after 400 ms (100 ms when moving between controls) and fades in over MotionFast."))),
      section("Notification cards", el("div", "catalog-stack",
        cards(),
        showButton(area),
        caption("Glass cards (radius 16) with an icon in the type's colour and a subtle close button. Shown at the window's top right; they fade in and out over MotionSlow."),
        area)));
  },
};
