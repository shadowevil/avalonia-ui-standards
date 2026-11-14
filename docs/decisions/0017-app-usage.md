# 0017 — How apps use the library

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Apps replace `<FluentTheme/>` with `<StandardsTheme/>` and call `.UseUiStandards()` on the `AppBuilder`, which registers Inter ([0012](0012-type.md)) and sets Skia's GPU resource budget to 256 MB (`AppBuilderExtensions.GpuResourceBudget`, [0015](0015-glass.md)). An app sets its own budget, or other Skia options, with `.With(new SkiaOptions { ... })` after `.UseUiStandards()`. That call replaces the whole `SkiaOptions`, so an app that sets other options and keeps the budget includes `MaxGpuResourceSizeBytes = AppBuilderExtensions.GpuResourceBudget`.
  - Style tier resource keys use plain names in the style of Avalonia's own keys, for example `FontSizeBody`, `SpacingM`, `CornerRadiusControl`, `MotionFast`.
  - Key families: `FontSize*`, `FontWeight*`, `Spacing*`, `CornerRadius*`, `CornerSmoothing`, `Motion*`, `Glass*`, `BackdropWash*`, `AccentFill*`, `AccentText*`, `DangerColor*`, `DangerFill*`, `DangerForeground`, `DangerTextColor`, `Success*`, `Warning*` (the last three in [0037](0037-tooltip-and-notifications.md)).
  - The string resources `ColorView*Text` and `ColorView*Letter` hold the ColorView's shown and spoken strings with English defaults; an app overrides one by putting its key in its own resources ([0041](0041-colorpicker.md)). `SidebarFooterText` names the Sidebar's footer list for assistive technology, overridden the same way ([0034](0034-sidebar.md)).
  - App windows subclass `StandardsWindow` ([0022](0022-window-layout.md)), which has a public `Backdrop` content slot (empty by default).
  - The attached property `ScrollShadows.IsEnabled` turns the scroll-edge shadow off per scroller ([0024](0024-lists-and-scrolling.md)).
  - The attached properties `TreeViewOptions.ExpandOn` and `TreeViewOptions.CollapseOn` (`TreeViewClickAction`) set which row clicks expand and collapse a tree's items, and `TreeViewOptions.ShowLines` (default true) shows its node lines, per tree or per item ([0035](0035-treeview.md)).
  - The attached property `SelectionOptions.AccentFlag` (default false) shows the accent flag on the selected items of a ListBox, Sidebar, ComboBox, AutoCompleteBox or TreeView, per list or per item ([0042](0042-accent-flag.md)).
  - `StandardsWindow.FrameTick` (with `FrameTickEventArgs`) and `StandardsWindow.HighRefreshRate` pace continuous animation ([0016](0016-motion.md)).
  - `StandardsWindow.ContentPanelBounds` (read-only) is the glass panel's rectangle in the `Backdrop` slot's coordinates ([0022](0022-window-layout.md)).
  - `StandardsWindow.IsContentScrollable` (default true) makes the glass panel scroll its content ([0022](0022-window-layout.md)).
  - `StandardsWindow.ToolbarBand` holds a `ToolbarBand`, whose items take the attached properties `ToolbarBand.Zone` (a `ToolbarZone`), `Priority`, `Label`, `CanOverflow` and `BeginsGroup`; `ToolbarSpacer` is its flexible space ([0043](0043-toolbar-band.md)).
  - `StandardsTheme` properties: `Accent`, `Density`, `ReduceMotion`, `ReduceTransparency`, `HighRefreshRate`, `GlassQuality`.
  - `GlassQuality` (`Full`, the default; `Minimal`; `Solid`) sets how glass draws app-wide; see [0015](0015-glass.md).
- **Why:** Adopting the library takes one line, and the keys read like Avalonia's.
