# Docs index

## Decisions
- [0001 — Built on Avalonia](decisions/0001-avalonia-base.md) — why Avalonia and C#
- [0002 — Desktop first, web decided later](decisions/0002-desktop-first.md) — when the web decision is due and between what
- [0003 — Restyle Fluent, not a theme from scratch](decisions/0003-restyle-fluent.md) — which controls are restyled and which get new templates
- [0004 — Tiers: Style → Blocks → Modules → Screens](decisions/0004-tier-names.md) — what belongs in each tier and where Fluent overrides live
- [0005 — No AI tool files in the repo](decisions/0005-no-ai-files-in-repo.md) — which AI tool files are kept out of git and why
- [0006 — Build, IDE and OS output is gitignored](decisions/0006-tooling-ignores.md) — which tooling output is ignored
- [0007 — Restyle the target, not its surroundings](decisions/0007-restyle-scope.md) — what a restyle may change and what happens when it would drift from the set look
- [0008 — .NET 10 and Avalonia 12](decisions/0008-target-versions.md) — which .NET and Avalonia versions the library targets and how they are pinned
- [0009 — Screenshots rendered by tests](decisions/0009-screenshots-from-tests.md) — how rendered looks are produced, with which test framework, and how test renders relate to the catalog app
- [0010 — One library, src and tests](decisions/0010-solution-layout.md) — project names, folder layout, tiers as folders, central package versions
- [0011 — Colour](decisions/0011-colour.md) — light and dark variants, the default accent and the neutral grey ramps
- [0012 — Type](decisions/0012-type.md) — the typeface and the type ramp
- [0013 — Spacing and density](decisions/0013-spacing-and-density.md) — the 4 px grid, the named steps and the density default
- [0014 — Shape](decisions/0014-shape.md) — continuous corners, the radii and the SquircleBorder primitive
- [0015 — Glass](decisions/0015-glass.md) — the GlassBorder material, where it is used and its fallback
- [0016 — Motion](decisions/0016-motion.md) — the durations, easing and the reduce-motion rule
- [0017 — How apps use the library](decisions/0017-app-usage.md) — the theme and builder entry points and the naming of Style tier keys
- [0018 — Button](decisions/0018-button.md) — the Button variants, size, chrome and state colours
- [0019 — Focus ring](decisions/0019-focus-ring.md) — how keyboard focus is shown on every Block
- [0020 — Catalog app](decisions/0020-catalog-app.md) — the live browsing app, what its pages and toolbar show, and how it relates to test renders
- [0021 — Button siblings](decisions/0021-button-siblings.md) — how ToggleButton, RepeatButton, SplitButton, DropDownButton and HyperlinkButton differ from Button
- [0022 — Window layout](decisions/0022-window-layout.md) — the library-drawn window frame, title band, areas, glass content panel, backdrop and narrow-window behaviour
- [0023 — Versioning](decisions/0023-versioning.md) — the versioning scheme, the starting version and where it is set
- [0024 — ListBox, ScrollViewer and ComboBox drop-down](decisions/0024-lists-and-scrolling.md) — the shared list item design, the drop-down popup and the scroll-edge shadow
- [0025 — Overlay scrollbar](decisions/0025-overlay-scrollbar.md) — the overlay ScrollBar Block: reveal, sizes, colours and motion
- [0026 — Icon](decisions/0026-icon.md) — the icon set and licence, the Icon Block's design box, sizes and colour, and how icons are added
- [0027 — TextInput](decisions/0027-text-input.md) — the TextBox chrome, sizes, state colours, placeholder, selection, and the clear and reveal buttons shared by field Blocks
- [0028 — ComboBox](decisions/0028-combobox.md) — the closed ComboBox field, its caret zone, editable mode and how focus shows on composite fields
- [0029 — NumericUpDown, ButtonSpinner and AutoCompleteBox](decisions/0029-numeric-and-autocomplete.md) — the spin fields, their spin buttons, and the AutoCompleteBox field and suggestions
- [0030 — CheckBox, ToggleSwitch and RadioButton](decisions/0030-checkbox-toggleswitch-radiobutton.md) — the toggles' indicator layers, sizes, check glyph, colours, focus ring and motion
- [0031 — Slider, ProgressBar and ProgressRing](decisions/0031-slider-and-progress.md) — the range controls' shapes, sizes, colours, the added ProgressRing and how indeterminate progress is animated
- [0032 — Segmented control](decisions/0032-segmented-control.md) — the SegmentedControl Block: base, keyboard, shape, sliding pill, colours and focus ring
- [0033 — TabControl, TabStrip and Expander](decisions/0033-tabs-and-expander.md) — the tab headers, sliding underline and content area, and the Expander card, caret and reveal
- [0034 — Sidebar](decisions/0034-sidebar.md) — the Sidebar Module: rows, headers, the collapse stages and thresholds, the title band button, the overlay and motion
- [0035 — TreeView](decisions/0035-treeview.md) — the TreeView Block: rows and the content pill, indentation, the expand caret, keyboard, the click modes and the node lines
- [0036 — Menus and flyouts](decisions/0036-menus-and-flyouts.md) — the glass menu and flyout overlays, menu item columns and states, the menu bar and the open fade
- [0037 — ToolTip and notification cards](decisions/0037-tooltip-and-notifications.md) — the solid tooltip card, the glass notification card, its type icons and the status colours
- [0038 — DatePicker, TimePicker, Calendar and CalendarDatePicker](decisions/0038-pickers-and-calendar.md) — the picker fields, the glass flyouts and drop-down and how their rows stay inside the corners, and the Calendar's cells, today, selection and keyboard ring
- [0039 — Separator and GridSplitter](decisions/0039-separator-and-gridsplitter.md) — the hairline separator and the splitter's hover grip
- [0040 — DataGrid](decisions/0040-datagrid.md) — the DataGrid package and version, row bands, grid lines, headers, sort caret, resize grip, cell focus, editing and check columns

## Plan
- [Roadmap](plan/roadmap.md) — the ordered milestones and the open questions
