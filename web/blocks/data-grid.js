// DataGrid (0040, 0048): <ui-data-grid> around a native <table> (markup in data-grid.css). The
// table carries the data and its semantics; the element is the scroller and the behaviour the
// desktop package gives: the current cell, row selection, sorting, editing, column resizing and
// frozen columns. A custom element holds it because that behaviour needs a lifecycle and a box
// to scroll; the data stays a <table> (semantics.md: tabular data is <table>).
//
// ARIA (the APG data grid): the table is a grid; rows are rows with aria-selected; headers are
// columnheaders with aria-sort on the sorted one; cells are gridcells. The roles are set
// explicitly because the table is laid out with CSS grid, and engines can drop a table's implicit
// roles when its display changes. aria-multiselectable follows selection-mode, aria-readonly
// readonly, aria-disabled disabled. Name the grid on the <table> (aria-label or a <caption>).
//
// Attributes (on the element):
//   frozen-columns="N"     the first N columns stay at the inline start while the rest scroll.
//   can-resize-columns     columns resize by dragging a header edge (off by default, as the package).
//   can-sort-columns="false"  turns header sorting off (on by default).
//   selection-mode="single"   one row at a time; the default is "extended" (the package's).
//   grid-lines             "horizontal" (default), "vertical", "all" or "none".
//   readonly, disabled.
// On a header <th>: data-width (px, "*" / "N*" a star share, "auto"), data-min-width (px, 20 by
// default), data-type ("text", "number", "check", "combo"), data-options (a combo column's
// choices, "|"-separated), data-readonly, data-sortable="false", data-can-resize="false".
// On a <td>: data-sort-value (the value to sort by), aria-invalid="true" (an invalid cell).
//
// Keys (one Tab stop with a roving tabindex, landing on the current cell; keyboard.md): arrows
// move the current cell (Left and Right swap under dir="rtl"), Up from the first row reaches the
// header row; Home and End go to the row's first and last cell, Ctrl or Cmd with them to the
// grid's first and last cell; Ctrl or Cmd with an arrow to that edge; Page Up and Page Down move
// by a page of rows. Moving to another row selects it; Shift extends the selection from the
// anchor (extended); Space selects the current row, Ctrl or Cmd+Space toggles it, Ctrl or Cmd+A
// selects all. On a header, Enter or Space sorts, and Shift+Left or Shift+Right resizes a
// resizable column by 8 px (the keyboard path for the drag). F2 edits the current cell, as a
// double click does; while editing Enter commits and moves down, Escape cancels, and leaving the
// cell commits. Enter outside editing moves down, as the package. A check cell's focus is its
// CheckBox, which Space toggles natively (unless the grid or column is read-only).
//
// Pointer: a press on a cell makes it current and selects its row (Ctrl or Cmd toggles, Shift
// extends); a click on a header sorts; within 5 px of a header edge that resizes a column (the
// end edge its own, the start edge the previous one's) the grip shows and a drag resizes.
//
// Events: `change` when the selection changes; `sorting` (cancelable, detail { column, direction })
// before a sort; `celleditended` (detail { row, column, oldValue, value }) when an edit commits.
import { icon } from "./icon.js";
import "./scroll.js";
import { createComboBox } from "./combobox.js";
import { isMac } from "../style/platform.js";

const RESIZE_ZONE = 5; // px at a header edge that resize a column (the package's)
const MIN_COLUMN_WIDTH = 20; // px, the package's MinColumnWidth
const KEY_RESIZE_STEP = 8; // px per Shift+Left or Shift+Right on a header (SpacingS)
const CHECK = "ui-data-grid-check-cell";
let nextId = 0;

function setAttr(el, name, value) {
  if (value === null || value === false) {
    if (el.hasAttribute(name)) el.removeAttribute(name);
  } else if (el.getAttribute(name) !== String(value)) el.setAttribute(name, String(value));
}

function setProp(el, name, value) {
  if (value === null) {
    if (el.style.getPropertyValue(name)) el.style.removeProperty(name);
  } else if (el.style.getPropertyValue(name) !== value) el.style.setProperty(name, value);
}

const textOf = (cell) => cell.textContent.trim();
const isNumber = (text) => text.trim() !== "" && Number.isFinite(Number(text));

export class UiDataGrid extends HTMLElement {
  static observedAttributes = ["disabled", "readonly", "frozen-columns", "grid-lines", "selection-mode", "can-resize-columns", "can-sort-columns"];

  #observer = new MutationObserver(() => this.#schedule());
  #resizeObserver = new ResizeObserver(() => this.#layout());
  #observed = new Set(); // what the resize observer watches
  #scheduled = false;
  #current = null; // the current cell (td or th)
  #anchor = null; // the row a Shift selection extends from
  #editing = null; // { cell, oldValue, editor, input }
  #resizing = null; // { th, startX, startWidth, hovered }
  #resized = false; // the last press resized, so its click does not sort
  #headerHeight = 0;
  #frozenWidth = 0;

  connectedCallback() {
    this.addEventListener("keydown", this.#onKeyDown);
    this.addEventListener("pointerdown", this.#onPointerDown);
    this.addEventListener("pointermove", this.#onPointerMove);
    this.addEventListener("pointerleave", this.#onPointerLeave);
    this.addEventListener("click", this.#onClick);
    this.addEventListener("dblclick", this.#onDoubleClick);
    this.addEventListener("focusin", this.#onFocusIn);
    this.addEventListener("focusout", this.#onFocusOut);
    this.addEventListener("change", this.#onCheckChange);
    this.addEventListener("scroll", this.#onScroll, { passive: true });
    this.#observer.observe(this, {
      subtree: true, childList: true, attributes: true,
      attributeFilter: ["data-width", "data-min-width", "data-type", "data-readonly", "data-sortable", "data-can-resize", "aria-selected"],
    });
    this.#sync();
  }

  disconnectedCallback() {
    this.#observer.disconnect();
    this.#resizeObserver.disconnect();
    this.#observed.clear();
  }

  attributeChangedCallback() {
    if (this.isConnected) this.#sync();
  }

  get table() {
    return this.querySelector(":scope > table");
  }

  /** The header cells, in column order. */
  get columns() {
    return [...(this.table?.tHead?.rows[0]?.cells ?? [])];
  }

  /** The body rows, in display order. */
  get rows() {
    return [...(this.table?.tBodies ?? [])].flatMap((body) => [...body.rows]);
  }

  get selectedRows() {
    return this.rows.filter((row) => row.getAttribute("aria-selected") === "true");
  }

  /** The first selected row, or null; setting it selects that row alone (no change event). */
  get selectedRow() {
    return this.selectedRows[0] ?? null;
  }

  set selectedRow(row) {
    this.#select(row ? [row] : [], false);
  }

  get disabled() {
    return this.hasAttribute("disabled");
  }

  get readOnly() {
    return this.hasAttribute("readonly");
  }

  get extended() {
    return this.getAttribute("selection-mode") !== "single";
  }

  /** The current cell (a td, or a th on the header row). */
  get currentCell() {
    return this.#current;
  }

  /** The cell being edited, or null. */
  get editingCell() {
    return this.#editing?.cell ?? null;
  }

  #schedule() {
    if (this.#scheduled) return;
    this.#scheduled = true;
    queueMicrotask(() => {
      this.#scheduled = false;
      if (this.isConnected) this.#sync();
    });
  }

  #type(index) {
    return this.columns[index]?.dataset.type ?? "text";
  }

  #frozenCount() {
    return Math.max(0, parseInt(this.getAttribute("frozen-columns") ?? "0", 10) || 0);
  }

  // Roles, header parts, column tracks, check cells, frozen cells and the roving tabindex.
  #sync() {
    const table = this.table;
    if (!table) return;
    this.classList.add("ui-scroll");
    setAttr(this, "data-scroll-shadows", "false");
    setAttr(table, "role", "grid");
    setAttr(table, "aria-multiselectable", this.extended ? "true" : null);
    setAttr(table, "aria-readonly", this.readOnly ? "true" : null);
    setAttr(table, "aria-disabled", this.disabled ? "true" : null);
    for (const group of [table.tHead, ...table.tBodies]) if (group) setAttr(group, "role", "rowgroup");

    const columns = this.columns;
    const frozen = this.#frozenCount();
    const tracks = [];
    let star = false;
    columns.forEach((th, i) => {
      setAttr(th, "role", "columnheader");
      if (!th.id) th.id = `ui-data-grid-header-${++nextId}`;
      if (!th.querySelector(":scope > .ui-data-grid-header-text")) {
        const text = document.createElement("span");
        text.className = "ui-data-grid-header-text";
        text.append(...th.childNodes);
        const up = icon("caret-up");
        up.classList.add("ui-data-grid-sort", "ui-data-grid-sort-ascending");
        const down = icon("caret-down");
        down.classList.add("ui-data-grid-sort", "ui-data-grid-sort-descending");
        const grip = document.createElement("span");
        grip.className = "ui-data-grid-grip";
        grip.setAttribute("aria-hidden", "true");
        th.append(text, up, down, grip);
      }
      setAttr(th, "data-frozen", i < frozen ? "" : null);
      const min = Math.max(0, parseFloat(th.dataset.minWidth) || MIN_COLUMN_WIDTH);
      const width = (th.dataset.width ?? "auto").trim();
      const share = width.match(/^(\d*\.?\d*)\*$/);
      if (share) {
        star = true;
        tracks.push(`minmax(${min}px, ${share[1] || 1}fr)`);
      } else if (isNumber(width)) tracks.push(`${Math.max(min, Number(width))}px`);
      else tracks.push(`minmax(${min}px, max-content)`);
    });
    if (!star) tracks.push("minmax(0, 1fr)"); // the filler, so rows span the grid
    setProp(table, "--ui-data-grid-columns", tracks.join(" "));
    if (table.tHead?.rows[0]) setAttr(table.tHead.rows[0], "role", "row");

    for (const row of this.rows) {
      setAttr(row, "role", "row");
      setAttr(row, "aria-selected", row.getAttribute("aria-selected") === "true" ? "true" : "false");
      [...row.cells].forEach((cell, i) => {
        setAttr(cell, "role", "gridcell");
        setAttr(cell, "data-frozen", i < frozen ? "" : null);
        const check = this.#type(i) === "check";
        cell.classList.toggle(CHECK, check);
        const box = check ? this.#checkBox(cell) : null;
        if (box) {
          if (!box.hasAttribute("aria-label") && !box.hasAttribute("aria-labelledby") && columns[i]) box.setAttribute("aria-labelledby", columns[i].id);
          setAttr(box, "aria-readonly", this.#cellReadOnly(i) ? "true" : null);
          // A disabled grid disables its check boxes, and gives back those it disabled.
          if (this.disabled && !box.disabled) {
            box.disabled = true;
            box.dataset.uiDataGridDisabled = "";
          } else if (!this.disabled && "uiDataGridDisabled" in box.dataset) {
            box.disabled = false;
            delete box.dataset.uiDataGridDisabled;
          }
        }
      });
    }
    this.#rove();
    this.#observeSizes();
    this.#layout();
  }

  #checkBox(cell) {
    return cell.querySelector(":scope > input[type='checkbox']");
  }

  #cellReadOnly(index) {
    return this.readOnly || this.columns[index]?.hasAttribute("data-readonly");
  }

  // The element that takes focus for a cell: a check cell's CheckBox, else the cell.
  #target(cell) {
    return (cell.classList.contains(CHECK) && this.#checkBox(cell)) || cell;
  }

  #cellOf(node) {
    const cell = node?.closest?.("td, th");
    return cell && cell.closest("ui-data-grid") === this && cell.closest("table") === this.table ? cell : null;
  }

  // Every row of the grid in navigation order: the header row, then the body rows.
  #gridRows() {
    const header = this.table?.tHead?.rows[0];
    return header ? [header, ...this.rows] : this.rows;
  }

  #position(cell) {
    const rows = this.#gridRows();
    return { row: rows.indexOf(cell.parentElement), column: cell.cellIndex };
  }

  #cellAt(rowIndex, column) {
    const row = this.#gridRows()[rowIndex];
    if (!row) return null;
    return row.cells[Math.min(column, row.cells.length - 1)] ?? null;
  }

  #rove() {
    const rows = this.#gridRows();
    const cells = rows.flatMap((row) => [...row.cells]);
    if (!this.#current || !cells.includes(this.#current)) {
      const first = this.selectedRow ?? this.rows[0];
      this.#current = first?.cells[0] ?? rows[0]?.cells[0] ?? null;
    }
    for (const cell of cells) {
      const target = this.#target(cell);
      if (target !== cell) setAttr(cell, "tabindex", null);
      setAttr(target, "tabindex", this.disabled ? null : cell === this.#current ? "0" : "-1");
    }
  }

  // Watches the grid, the table and the header cells, whose widths place the frozen and clipped
  // cells; only what joined or left since the last call is observed or released.
  #observeSizes() {
    const wanted = new Set([this, this.table, ...this.columns].filter(Boolean));
    for (const el of this.#observed) if (!wanted.has(el)) this.#resizeObserver.unobserve(el);
    for (const el of wanted) if (!this.#observed.has(el)) this.#resizeObserver.observe(el);
    this.#observed = wanted;
  }

  // The header height and each column's start, for sticky frozen cells and the clips.
  #layout() {
    const columns = this.columns;
    this.#headerHeight = this.table?.tHead?.offsetHeight ?? 0;
    const frozen = this.#frozenCount();
    const starts = [];
    let at = 0;
    for (const th of columns) {
      starts.push(at);
      at += th.offsetWidth;
    }
    this.#frozenWidth = !frozen ? 0 : frozen < columns.length ? starts[frozen] : at;
    setProp(this, "--ui-data-grid-frozen-width", `${this.#frozenWidth}px`);
    for (const row of this.#gridRows()) {
      [...row.cells].forEach((cell, i) => setProp(cell, "--ui-data-grid-cell-start", frozen ? `${starts[i] ?? 0}px` : null));
    }
    this.#onScroll();
  }

  #onScroll = () => {
    setProp(this, "--ui-data-grid-scroll-x", `${Math.abs(this.scrollLeft)}px`);
    setProp(this, "--ui-data-grid-scroll-y", `${Math.max(0, this.scrollTop)}px`);
  };

  // Scrolls the cell into the part of the viewport not covered by the header and frozen columns.
  #reveal(cell) {
    const r = cell.getBoundingClientRect();
    const v = this.getBoundingClientRect();
    const top = v.top + this.clientTop + (cell.tagName === "TH" ? 0 : this.#headerHeight);
    const bottom = v.top + this.clientTop + this.clientHeight;
    if (cell.tagName !== "TH") {
      if (r.top < top) this.scrollTop -= top - r.top;
      else if (r.bottom > bottom) this.scrollTop += r.bottom - bottom;
    }
    if (cell.hasAttribute("data-frozen")) return;
    const rtl = getComputedStyle(this).direction === "rtl";
    const left = v.left + this.clientLeft + (rtl ? 0 : this.#frozenWidth);
    const right = v.left + this.clientLeft + this.clientWidth - (rtl ? this.#frozenWidth : 0);
    if (r.left < left) this.scrollLeft += r.left - left;
    else if (r.right > right) this.scrollLeft += Math.min(r.right - right, r.left - left);
  }

  #focusCell(cell) {
    if (!cell) return;
    this.#current = cell;
    this.#rove();
    this.#target(cell).focus({ preventScroll: true });
    this.#reveal(cell);
  }

  #select(rows, notify = true) {
    const set = new Set(rows);
    let changed = false;
    for (const row of this.rows) {
      const on = set.has(row);
      if ((row.getAttribute("aria-selected") === "true") === on) continue;
      row.setAttribute("aria-selected", String(on));
      changed = true;
    }
    if (changed && notify) this.dispatchEvent(new Event("change", { bubbles: true }));
  }

  // Selection for a move or press onto `row`: alone, extended from the anchor, or toggled.
  #selectFor(row, { extend = false, toggle = false } = {}) {
    if (!row || this.disabled) return;
    const rows = this.rows;
    if (extend && this.extended && this.#anchor && rows.includes(this.#anchor)) {
      const [a, b] = [rows.indexOf(this.#anchor), rows.indexOf(row)].sort((x, y) => x - y);
      this.#select(rows.slice(a, b + 1));
      return;
    }
    if (toggle && this.extended) {
      const on = row.getAttribute("aria-selected") === "true";
      this.#select(on ? this.selectedRows.filter((r) => r !== row) : [...this.selectedRows, row]);
    } else if (toggle) {
      this.#select(row.getAttribute("aria-selected") === "true" ? [] : [row]);
    } else this.#select([row]);
    this.#anchor = row;
  }

  #keyboard(on) {
    setAttr(this, "data-keyboard", on ? "" : null);
  }

  // --- Sorting -----------------------------------------------------------------------------

  #sortable(th) {
    return this.getAttribute("can-sort-columns") !== "false" && th.dataset.sortable !== "false" && !this.disabled;
  }

  #sortValue(cell, type) {
    if (cell.dataset.sortValue !== undefined) return isNumber(cell.dataset.sortValue) ? Number(cell.dataset.sortValue) : cell.dataset.sortValue;
    if (type === "check") return this.#checkBox(cell)?.checked ? 1 : 0;
    const text = textOf(cell);
    return type === "number" && isNumber(text) ? Number(text) : text;
  }

  /** Sorts by a column (a th or its index), toggling ascending and descending unless given. */
  sort(column, direction) {
    const th = typeof column === "number" ? this.columns[column] : column;
    if (!th) return false;
    const index = th.cellIndex;
    const dir = direction ?? (th.getAttribute("aria-sort") === "ascending" ? "descending" : "ascending");
    const event = new CustomEvent("sorting", { bubbles: true, cancelable: true, detail: { column: index, direction: dir } });
    if (!this.dispatchEvent(event)) return false;
    if (this.#editing) this.#commit();
    for (const other of this.columns) setAttr(other, "aria-sort", other === th ? dir : null);
    const type = this.#type(index);
    const collator = new Intl.Collator(undefined, { numeric: true, sensitivity: "base" });
    const sign = dir === "ascending" ? 1 : -1;
    for (const body of this.table.tBodies) {
      const keyed = [...body.rows].map((row, i) => ({ row, i, v: row.cells[index] ? this.#sortValue(row.cells[index], type) : "" }));
      keyed.sort((a, b) => {
        const c = typeof a.v === "number" && typeof b.v === "number" ? a.v - b.v : collator.compare(String(a.v), String(b.v));
        return c * sign || a.i - b.i;
      });
      body.append(...keyed.map((k) => k.row));
    }
    return true;
  }

  // --- Resizing ----------------------------------------------------------------------------

  #resizable(th) {
    return !!th && this.hasAttribute("can-resize-columns") && !this.disabled && th.dataset.canResize !== "false";
  }

  // The column a header edge resizes: the end edge its own, the start edge the previous one's.
  #resizeTarget(th, side) {
    if (side === "end") return this.#resizable(th) ? th : null;
    const previous = this.columns[th.cellIndex - 1];
    return this.#resizable(previous) ? previous : null;
  }

  // Which edge of the header the pointer is within the resize zone of, if that edge resizes.
  #edgeAt(th, clientX) {
    const r = th.getBoundingClientRect();
    const rtl = getComputedStyle(th).direction === "rtl";
    const fromStart = rtl ? r.right - clientX : clientX - r.left;
    const fromEnd = rtl ? clientX - r.left : r.right - clientX;
    if (fromEnd <= RESIZE_ZONE && this.#resizeTarget(th, "end")) return "end";
    if (fromStart <= RESIZE_ZONE && this.#resizeTarget(th, "start")) return "start";
    return null;
  }

  #showGrip(th, side) {
    for (const other of this.columns) {
      if (other === th && side) {
        setAttr(other, "data-resize", side);
        setAttr(other, "data-resize-side", side);
      } else if (!other.dataset.resize?.includes("drag")) setAttr(other, "data-resize", null);
    }
  }

  #setWidth(th, width) {
    const min = Math.max(0, parseFloat(th.dataset.minWidth) || MIN_COLUMN_WIDTH);
    th.dataset.width = String(Math.round(Math.max(min, width)));
    this.#sync();
  }

  // --- Editing -----------------------------------------------------------------------------

  #editable(cell) {
    if (!cell || cell.tagName !== "TD" || this.disabled) return false;
    const type = this.#type(cell.cellIndex);
    return type !== "check" && !this.#cellReadOnly(cell.cellIndex);
  }

  /** Turns the cell into its edit field; returns whether editing started. */
  beginEdit(cell = this.#current) {
    if (this.#editing?.cell === cell) return true;
    if (this.#editing && !this.#commit()) return false;
    if (!this.#editable(cell)) return false;
    const type = this.#type(cell.cellIndex);
    const header = this.columns[cell.cellIndex];
    const oldValue = textOf(cell);
    let editor, input;
    if (type === "combo") {
      const options = (header?.dataset.options ?? "").split("|").map((o) => o.trim()).filter(Boolean);
      editor = createComboBox(options, { selected: options.indexOf(oldValue), label: header ? textOf(header) : "" });
      input = null;
    } else {
      editor = input = document.createElement("input");
      input.type = "text";
      input.className = "ui-text-input ui-squircle";
      input.value = oldValue;
      if (type === "number") input.inputMode = "decimal";
      if (header) input.setAttribute("aria-labelledby", header.id);
    }
    this.#current = cell;
    this.#editing = { cell, oldValue, editor, input };
    cell.setAttribute("data-editing", "");
    cell.replaceChildren(editor);
    if (input) {
      input.focus();
      input.select();
    } else editor.querySelector(".ui-combo-box-field")?.focus();
    return true;
  }

  // Commits the edit; false (still editing, the field marked invalid) when the value is invalid.
  #commit() {
    const editing = this.#editing;
    if (!editing) return true;
    const { cell, oldValue, editor, input } = editing;
    const value = input ? input.value : (editor.value ?? oldValue);
    if (this.#type(cell.cellIndex) === "number" && !isNumber(value)) {
      input.setAttribute("aria-invalid", "true");
      cell.setAttribute("aria-invalid", "true");
      return false;
    }
    this.#end(value);
    if (value !== oldValue) {
      this.dispatchEvent(new CustomEvent("celleditended", {
        bubbles: true,
        detail: { row: cell.parentElement, column: cell.cellIndex, oldValue, value },
      }));
    }
    return true;
  }

  /** Commits the edit in progress; returns false when its value is invalid. */
  commitEdit() {
    return this.#commit();
  }

  /** Cancels the edit in progress, restoring the cell's value. */
  cancelEdit() {
    if (this.#editing) this.#end(this.#editing.oldValue);
  }

  #end(value) {
    const { cell } = this.#editing;
    this.#editing = null;
    const hadFocus = cell.contains(document.activeElement);
    cell.removeAttribute("data-editing");
    cell.removeAttribute("aria-invalid");
    cell.textContent = value;
    if (hadFocus) this.#focusCell(cell);
  }

  // --- Events ------------------------------------------------------------------------------

  #onFocusIn = (event) => {
    const cell = this.#cellOf(event.target);
    if (!cell || this.disabled) return;
    if (event.target === this.#target(cell)) {
      this.#current = cell;
      this.#rove();
      // Keyboard focus (Tab, or script after a key) starts the keyboard ring.
      if (event.target.matches(":focus-visible")) this.#keyboard(true);
    }
  };

  #onFocusOut = (event) => {
    const next = event.relatedTarget;
    const editing = this.#editing;
    if (editing && editing.cell.contains(event.target) && !(next && editing.cell.contains(next))) {
      // Leaving the edited cell commits; an invalid value is dropped, as focus has gone.
      if (!this.#commit()) this.cancelEdit();
    }
    if (!next || !this.contains(next)) this.#keyboard(false);
  };

  #onCheckChange = (event) => {
    const box = event.target;
    if (!(box instanceof HTMLInputElement) || box.type !== "checkbox") return;
    const cell = this.#cellOf(box);
    if (!cell || this.#checkBox(cell) !== box) return;
    this.dispatchEvent(new CustomEvent("celleditended", {
      bubbles: true,
      detail: { row: cell.parentElement, column: cell.cellIndex, oldValue: !box.checked, value: box.checked },
    }));
  };

  #onPointerDown = (event) => {
    this.#keyboard(false);
    this.#resized = false;
    const cell = this.#cellOf(event.target);
    if (!cell) return;
    if (this.disabled) {
      event.preventDefault();
      return;
    }
    if (cell.tagName === "TH") {
      const side = event.button === 0 ? this.#edgeAt(cell, event.clientX) : null;
      // Headers are not focus targets for the pointer: the current cell keeps focus.
      event.preventDefault();
      if (!side) return;
      const th = this.#resizeTarget(cell, side);
      this.#resizing = { th, startX: event.clientX, startWidth: th.offsetWidth, hovered: cell };
      this.#resized = true;
      setAttr(cell, "data-resize", `${side} drag`);
      try {
        cell.setPointerCapture(event.pointerId);
      } catch {
        // not an active pointer
      }
      const end = () => {
        this.#resizing = null;
        setAttr(cell, "data-resize", null);
        cell.removeEventListener("pointerup", end);
        cell.removeEventListener("pointercancel", end);
        cell.removeEventListener("lostpointercapture", end);
      };
      cell.addEventListener("pointerup", end);
      cell.addEventListener("pointercancel", end);
      cell.addEventListener("lostpointercapture", end);
      return;
    }
    if (this.#editing?.cell === cell) return;
    if (cell.tagName === "TD") {
      const toggle = isMac ? event.metaKey : event.ctrlKey;
      this.#selectFor(cell.parentElement, { extend: event.shiftKey, toggle });
      this.#current = cell;
      this.#rove();
      const target = this.#target(cell);
      if (target !== cell && event.target !== target) {
        event.preventDefault();
        target.focus({ preventScroll: true });
      }
    }
  };

  #onPointerMove = (event) => {
    if (this.#resizing) {
      const { th, startX, startWidth } = this.#resizing;
      const rtl = getComputedStyle(this).direction === "rtl";
      this.#setWidth(th, startWidth + (event.clientX - startX) * (rtl ? -1 : 1));
      return;
    }
    const cell = this.#cellOf(event.target);
    const th = cell?.tagName === "TH" ? cell : null;
    this.#showGrip(th, th ? this.#edgeAt(th, event.clientX) : null);
  };

  #onPointerLeave = () => {
    if (!this.#resizing) this.#showGrip(null, null);
  };

  #onClick = (event) => {
    const cell = this.#cellOf(event.target);
    if (!cell) return;
    if (cell.tagName === "TH") {
      if (!this.#resized && this.#sortable(cell)) this.sort(cell);
      this.#resized = false;
      return;
    }
    // A read-only grid or column keeps its check boxes as they are.
    const box = this.#checkBox(cell);
    if (box && event.target === box && this.#cellReadOnly(cell.cellIndex)) event.preventDefault();
  };

  #onDoubleClick = (event) => {
    const cell = this.#cellOf(event.target);
    if (cell && this.#editing?.cell !== cell && this.beginEdit(cell)) event.preventDefault();
  };

  #onEditKey(event) {
    const { cell, editor, input } = this.#editing;
    if (!cell.contains(event.target) || event.defaultPrevented) return;
    if (event.key === "Escape") {
      if (!input && editor.open) return; // the combo's list closes first
      event.preventDefault();
      this.cancelEdit();
    } else if (event.key === "Enter" && input) {
      event.preventDefault();
      if (!this.#commit()) return;
      const { row, column } = this.#position(cell);
      const below = this.#cellAt(row + 1, column);
      if (below) {
        this.#focusCell(below);
        this.#selectFor(below.parentElement);
      }
    }
  }

  #onKeyDown = (event) => {
    if (this.disabled) return;
    this.#keyboard(true);
    if (this.#editing) return this.#onEditKey(event);
    const cell = this.#cellOf(event.target);
    if (!cell || event.target !== this.#target(cell) || event.altKey) return;
    const mod = isMac ? event.metaKey : event.ctrlKey;
    const rtl = getComputedStyle(this).direction === "rtl";
    let key = event.key;
    if (rtl && key === "ArrowLeft") key = "ArrowRight";
    else if (rtl && key === "ArrowRight") key = "ArrowLeft";
    const { row, column } = this.#position(cell);
    const rows = this.#gridRows();
    const header = !!this.table.tHead?.rows[0];
    const firstBody = header ? 1 : 0;
    const last = rows.length - 1;
    const width = rows[row].cells.length - 1;
    const pageRows = Math.max(1, Math.floor((this.clientHeight - this.#headerHeight) / Math.max(1, cell.offsetHeight)));
    const isHeader = cell.tagName === "TH";

    // To another cell; moving to another body row selects it (Shift extends).
    const go = (r, c, { select = true } = {}) => {
      const target = this.#cellAt(Math.max(0, Math.min(last, r)), Math.max(0, c));
      if (!target) return;
      const rowChanged = target.parentElement !== cell.parentElement;
      this.#focusCell(target);
      if (select && target.tagName === "TD" && (rowChanged || event.shiftKey)) this.#selectFor(target.parentElement, { extend: event.shiftKey });
    };

    switch (key) {
      case "ArrowDown":
        go(mod ? last : row + 1, column);
        break;
      case "ArrowUp":
        go(mod ? firstBody : row - 1, column);
        break;
      case "ArrowRight":
        if (isHeader && event.shiftKey) {
          if (!this.#resizable(cell)) return;
          this.#setWidth(cell, cell.offsetWidth + KEY_RESIZE_STEP);
          break;
        }
        go(row, mod ? width : Math.min(width, column + 1), { select: false });
        break;
      case "ArrowLeft":
        if (isHeader && event.shiftKey) {
          if (!this.#resizable(cell)) return;
          this.#setWidth(cell, cell.offsetWidth - KEY_RESIZE_STEP);
          break;
        }
        go(row, mod ? 0 : column - 1, { select: false });
        break;
      case "Home":
        if (mod) go(firstBody, 0);
        else go(row, 0, { select: false });
        break;
      case "End":
        if (mod) go(last, rows[last].cells.length - 1);
        else go(row, width, { select: false });
        break;
      case "PageDown":
        go(Math.max(row, firstBody) + pageRows, column);
        break;
      case "PageUp":
        go(Math.max(firstBody, row - pageRows), column);
        break;
      case "Enter":
        if (isHeader) {
          if (this.#sortable(cell)) this.sort(cell);
        } else go(row + 1, column);
        break;
      case " ":
        if (isHeader) {
          if (this.#sortable(cell)) this.sort(cell);
          break;
        }
        if (this.#target(cell) !== cell) return; // the CheckBox toggles natively
        this.#selectFor(cell.parentElement, { toggle: mod, extend: event.shiftKey });
        break;
      case "F2":
        if (!this.beginEdit(cell)) return;
        break;
      case "a":
      case "A":
        if (!mod || !this.extended) return;
        this.#select(this.rows);
        break;
      default:
        return;
    }
    event.preventDefault();
  };
}

if (!customElements.get("ui-data-grid")) customElements.define("ui-data-grid", UiDataGrid);
