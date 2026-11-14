// Expander (0033): <details> has no disabled state, so an expander with aria-disabled="true" is
// kept from toggling (pointer and keys) and its header leaves the Tab order; removing the
// attribute restores both. Everything else is the browser's.
const SELECTOR = "details.ui-expander";

function update(expander) {
  const header = expander.querySelector(":scope > summary");
  if (!header) return;
  if (expander.getAttribute("aria-disabled") === "true") {
    header.tabIndex = -1;
    header.setAttribute("aria-disabled", "true");
  } else if (header.hasAttribute("aria-disabled")) {
    header.removeAttribute("tabindex");
    header.removeAttribute("aria-disabled");
  }
}

function updateWithin(root) {
  if (root.matches?.(SELECTOR)) update(root);
  root.querySelectorAll?.(SELECTOR).forEach(update);
}

// A click on a disabled header (Enter and Space reach it as a click) does not toggle.
function onClick(event) {
  const header = event.target.closest?.("summary");
  const expander = header?.parentElement;
  if (expander?.matches(SELECTOR) && expander.getAttribute("aria-disabled") === "true") event.preventDefault();
}

const observer = new MutationObserver((records) => {
  for (const record of records) {
    if (record.type === "attributes") {
      if (record.target.matches(SELECTOR)) update(record.target);
    }
    else record.addedNodes.forEach((node) => node.nodeType === 1 && updateWithin(node));
  }
});

function start() {
  updateWithin(document);
  observer.observe(document, { subtree: true, childList: true, attributes: true, attributeFilter: ["aria-disabled"] });
  document.addEventListener("click", onClick, true);
}

if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", start, { once: true });
else start();
