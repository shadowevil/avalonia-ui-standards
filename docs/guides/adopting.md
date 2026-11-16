# Adopting the library in an app

## Status
The library is built tier by tier and is a work in progress. Many base components do not exist yet. [The roadmap](../plan/roadmap.md) lists the milestones. `apps/Catalog` shows the controls in use.

## Requirements
Avalonia 12.1.6 and `net10.0`.

## Reference
From a project in a sibling folder of a checkout named `avalonia_ui_standards`:

```xml
<ProjectReference Include="..\avalonia_ui_standards\src\AvaloniaUiStandards\AvaloniaUiStandards.csproj" />
```

In general, the `ProjectReference` points at `src/AvaloniaUiStandards/AvaloniaUiStandards.csproj` in the checkout, by whatever relative path reaches it.

## Setup
- `<StandardsTheme/>` replaces `<FluentTheme/>` in `App.axaml`, and the `AppBuilder` calls `.UseUiStandards()`.
- Windows subclass `StandardsWindow`. Its slots: [`Sidebar`](../decisions/0034-sidebar.md), [`ToolbarBand`](../decisions/0043-toolbar-band.md), [`StatusBar`](../decisions/0047-status-bar.md) (with rows), [`Inspector` and `BottomPanel`](../decisions/0051-split-panes.md) (a `PanelStack` fits the inspector) and `Backdrop`.
- Menus are `NativeMenu`, with `FullScreenMenuItem` for full screen and `InspectorMenuItem` and `BottomPanelMenuItem` for the panels; `StandardsWindow.LayoutMode` sets which OS's layout shows, so an app can offer it as a setting.
- Use the library's controls and Style resource keys, and theme through `StandardsTheme`.
- Content that paints its whole rectangle opaquely, such as a canvas, sets `OpaqueRegion.IsOpaque` ([0017](../decisions/0017-app-usage.md)); glass and the `Backdrop` clip then leave that area out ([0015](../decisions/0015-glass.md)), except for opaque content inside the `Backdrop` slot. The visible rest is `StandardsWindow.BackdropVisibleRegion` ([0022](../decisions/0022-window-layout.md)).
- Test helpers exist for headless tests (`StandardsTheme.CreateForTests`, `StandardsWindow.TitleIconDecoderForTests`).

All of these names are specified in [0017](../decisions/0017-app-usage.md).

## Rules for apps
Requesting missing components, temporary stand-ins, app-specific pieces, lookalikes and visual checks are in [0050](../decisions/0050-adopting-apps.md).

## Reading list
| Decision | For |
|---|---|
| [0017](../decisions/0017-app-usage.md) | The public API and resource keys |
| [0022](../decisions/0022-window-layout.md) | Window layout, slots, backdrop |
| [0044](../decisions/0044-menu-bar.md) | Menus and the menu row |
| [0049](../decisions/0049-full-screen.md) | Full screen |
| [0015](../decisions/0015-glass.md) | Glass and its limits |
| [0016](../decisions/0016-motion.md) | Motion and `FrameTick` |
| [0013](../decisions/0013-spacing-and-density.md) | Spacing and density |
| [0050](../decisions/0050-adopting-apps.md) | Rules for apps |
