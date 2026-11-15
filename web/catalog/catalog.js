// The catalog app (0020): the app shell with the page list in the Sidebar, the selected page in the
// glass panel, the page name and library version in the status bar, the menu row, and the theme
// controls in the toolbar band, saved and restored. Routes are hashes (#button).
//
// ?render shows one page alone for the renders (web/tools/render.py), as the desktop's whole-page
// renders: no sidebar, menu row, toolbar band, status bar, backdrop or saved settings; the shell grows to the
// page's full height, written to data-render-height on <html>, and the page ids to data-pages.
import { icon, createSidebarToggle, createMenuRow } from "../ui.js";
import { pages } from "./pages/index.js";
import { load, apply, saver } from "./settings.js";
import { buildToolbar } from "./toolbar.js";
import { createCatalogMenu } from "./menu.js";

// The library version: the desktop project's <Version> (src/AvaloniaUiStandards/AvaloniaUiStandards.csproj,
// 0023). The catalog is served from web/, which cannot read that file, so it is repeated here.
export const LIBRARY_VERSION = "0.1.0";

const APP_NAME = "Avalonia UI Standards Catalog";
const params = new URLSearchParams(location.search);
const renderMode = params.has("render");
const root = document.documentElement;
const shell = document.querySelector(".catalog");
const main = document.getElementById("main");
const list = document.querySelector(".catalog-sidebar ul");
const statusBar = document.querySelector(".catalog-status ui-status-bar");
const links = new Map();
let current = null;

function pageFor(hash) {
  const id = decodeURIComponent(hash.replace(/^#/, ""));
  return pages.find((p) => p.id === id) ?? null;
}

function show(page) {
  current = page;
  let content;
  try {
    content = page.render();
  } catch (error) {
    console.error(error);
    content = document.createElement("p");
    content.className = "catalog-error";
    content.textContent = `The ${page.title} page failed to render: ${error.message}`;
  }
  main.replaceChildren(content);
  main.scrollTop = 0;
  statusBar.message = page.title;
  document.title = `${page.title} - ${APP_NAME}`;
  for (const [id, link] of links) {
    if (id === page.id) link.setAttribute("aria-current", "page");
    else link.removeAttribute("aria-current");
  }
}

// A hash that names no page (the skip link's #main) leaves the page as it is; at startup it shows
// the first page.
function route() {
  const page = pageFor(location.hash);
  if (page && page !== current) show(page);
}

function buildSidebar() {
  for (const page of pages) {
    const link = document.createElement("a");
    link.className = "ui-list-item ui-squircle ui-focus-ring";
    link.href = `#${page.id}`;
    const label = document.createElement("span");
    label.className = "ui-sidebar-label";
    label.textContent = page.title;
    link.append(icon(page.icon), label);
    const li = document.createElement("li");
    li.append(link);
    list.append(li);
    links.set(page.id, link);
  }
  document.querySelector(".ui-shell-title-band").prepend(createSidebarToggle());
}

function startRender() {
  shell.classList.add("catalog-render");
  document.querySelector(".ui-shell-title").textContent = "Catalog page";
  for (const slot of [".catalog-band", ".ui-shell-menu-row", ".catalog-sidebar", ".catalog-status", ".ui-shell-backdrop", ".ui-skip-link"]) document.querySelector(slot).hidden = true;
  const page = pageFor(location.hash) ?? pages[0];
  // As the desktop renders: motion is reduced (still frames), except on the Motion page, whose values it shows.
  root.dataset.reduceMotion = page.id === "motion" ? "false" : "true";
  show(page);
  root.dataset.pages = pages.map((p) => p.id).join(" ");
  // Measured once the fonts have loaded (reading the size lays the page out).
  document.fonts.ready.then(() => {
    root.dataset.renderHeight = String(Math.ceil(shell.getBoundingClientRect().height));
    root.dataset.ready = "true";
  });
}

function startApp() {
  const settings = load();
  apply(settings);
  const store = saver();
  addEventListener("pagehide", () => store.flush());
  let menuRow = null;
  // A choice in the band, or from the menu row: applied, saved, and the menu's marks refreshed.
  const change = (key, value) => {
    settings[key] = value;
    apply(settings);
    store.schedule(settings);
    menuRow?.refresh();
  };
  const toolbar = buildToolbar(document.querySelector(".catalog-band"), settings, change);
  // The View menu takes the band's path and shows the choice in the band.
  const setSetting = (key, value) => {
    toolbar.set(key, value);
    change(key, value);
  };
  const movePage = (step) => {
    const n = pages.length;
    const i = Math.max(0, pages.indexOf(current));
    location.hash = `#${pages[(i + step + n) % n].id}`;
  };
  const { menu, appMenu } = createCatalogMenu({ settings, setSetting, movePage, appName: APP_NAME, version: LIBRARY_VERSION });
  menuRow = createMenuRow(menu, { appMenu });
  document.querySelector(".ui-shell-menu-row").append(menuRow.element);

  buildSidebar();
  document.querySelector(".catalog-status-version").textContent = `AvaloniaUiStandards ${LIBRARY_VERSION}`;
  // The skip link moves focus to the content without changing the route.
  document.querySelector(".ui-skip-link").addEventListener("click", (e) => {
    e.preventDefault();
    main.focus();
  });
  addEventListener("hashchange", route);
  const first = pageFor(location.hash);
  if (!first) history.replaceState(null, "", `#${pages[0].id}`);
  show(first ?? pages[0]);
  root.dataset.ready = "true";

  // The ribbon backdrop, loaded after the first page is shown so it does not hold up the first
  // paint; the catch keeps the catalog working if it fails to load.
  import("./ribbons.js")
    .then(({ startRibbons }) => startRibbons(document.querySelector(".ui-shell-backdrop")))
    .catch((error) => console.warn(`Ribbon backdrop not loaded: ${error.message}`));
}

if (renderMode) startRender();
else startApp();
