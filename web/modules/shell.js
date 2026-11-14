// App shell (0022, 0034): the layout is CSS alone (shell.css). This module mirrors the sidebar
// slot's stage into data-sidebar-stage on each .ui-shell ("expanded", "compact" or "hidden"), so
// the Sidebar's script can read and watch it. The widths are those of shell.css's container
// queries, by the shell's own width. Starts by itself when loaded.

const EXPANDED_FROM = 1008;
const COMPACT_FROM = 640;

const shells = new Set();
let resizeObserver = null;

/** The sidebar stage for a shell width. Test hook (not exported from index.js). */
export function sidebarStage(width) {
  return width >= EXPANDED_FROM ? "expanded" : width >= COMPACT_FROM ? "compact" : "hidden";
}

function apply(shell, width) {
  const stage = sidebarStage(width);
  if (shell.getAttribute("data-sidebar-stage") !== stage) shell.setAttribute("data-sidebar-stage", stage);
}

function scan() {
  const found = new Set(document.querySelectorAll(".ui-shell"));
  for (const shell of shells) {
    if (found.has(shell)) continue;
    resizeObserver.unobserve(shell);
    shells.delete(shell);
  }
  for (const shell of found) {
    if (shells.has(shell)) continue;
    shells.add(shell);
    apply(shell, shell.getBoundingClientRect().width);
    resizeObserver.observe(shell);
  }
}

/**
 * Test hook (not exported from index.js). Re-reads every shell's width now, without waiting for
 * the next resize observation (which comes with the next rendered frame).
 */
export function refreshShellStages() {
  if (!resizeObserver) return;
  scan();
  for (const shell of shells) apply(shell, shell.getBoundingClientRect().width);
}

function start() {
  resizeObserver = new ResizeObserver((entries) => {
    for (const entry of entries) apply(entry.target, entry.borderBoxSize[0].inlineSize);
  });
  new MutationObserver(scan).observe(document.body, { subtree: true, childList: true, attributes: true, attributeFilter: ["class"] });
  scan();
}

if (document.body) start();
else document.addEventListener("DOMContentLoaded", start, { once: true });
