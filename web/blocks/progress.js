// ProgressRing: the arc is drawn in CSS from --ui-progress-fraction (0 to 1); this keeps the
// property in step with the <progress> element's value and max, which CSS cannot read. An
// indeterminate ring has no fraction. The ring needs no other script.
const SELECTOR = "progress.ui-progress-ring";

function update(ring) {
  const position = ring.position;
  if (position < 0) ring.style.removeProperty("--ui-progress-fraction");
  else ring.style.setProperty("--ui-progress-fraction", String(position));
}

function updateWithin(root) {
  if (root.matches?.(SELECTOR)) update(root);
  root.querySelectorAll?.(SELECTOR).forEach(update);
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
  observer.observe(document, { subtree: true, childList: true, attributes: true, attributeFilter: ["value", "max"] });
}

if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", start, { once: true });
else start();
