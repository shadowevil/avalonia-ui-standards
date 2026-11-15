# Adopting the library in an app

## Status
The library is built tier by tier (Style, Blocks, Modules, Screens, [0004](../decisions/0004-tier-names.md)) and is a work in progress. Many base components do not exist yet. [The roadmap](../plan/roadmap.md) lists the milestones and open questions. `apps/Catalog` shows the controls in use ([0020](../decisions/0020-catalog-app.md)).

## Requirements
Avalonia 12.1.6 and `net10.0` ([0008](../decisions/0008-target-versions.md)). An app uses the same Avalonia package versions as the library, and no second Fluent theme.

## Reference
From a project in a sibling folder of a checkout named `avalonia_ui_standards`:

```xml
<ProjectReference Include="..\avalonia_ui_standards\src\AvaloniaUiStandards\AvaloniaUiStandards.csproj" />
```

In general, the `ProjectReference` points at `src/AvaloniaUiStandards/AvaloniaUiStandards.csproj` in the checkout, by whatever relative path reaches it.

## Setup
- In `App.axaml`, `<StandardsTheme/>` replaces `<FluentTheme/>`.
- The `AppBuilder` calls `.UseUiStandards()`, which registers Inter and sets Skia's GPU resource budget ([0017](../decisions/0017-app-usage.md)).
- Windows subclass `StandardsWindow` ([0022](../decisions/0022-window-layout.md)). Its slots:
  - `Sidebar`, with `FooterItems` ([0034](../decisions/0034-sidebar.md));
  - `ToolbarBand`, whose items take `ToolbarBand.Zone`, `Priority`, `Label`, `CanOverflow` and `BeginsGroup` ([0043](../decisions/0043-toolbar-band.md));
  - `StatusBar`, with a `Message` and optional `Rows` (`StatusBarRow`, each with `Items`) ([0047](../decisions/0047-status-bar.md));
  - `Backdrop`, drawn behind the glass content panel. `IsContentScrollable` set to false suits a canvas.

## Menus
- Menus are defined once as Avalonia `NativeMenu` ([0044](../decisions/0044-menu-bar.md)). The application's menu holds app items such as About.
- On macOS the menus go to the system menu bar. In the Windows layout the window's menu shows in its menu row, with the application menu's items merged into Help.
- `FullScreenMenuItem` is the View menu's full screen item on macOS ([0049](../decisions/0049-full-screen.md)).
- `StandardsWindow.LayoutMode` (`Automatic`, `MacOS`, `Windows`) sets which OS's layout the window shows. An app can offer it as a setting so the Windows layout can be checked on a Mac.

## Using it
- Use the library controls as they are ([0017](../decisions/0017-app-usage.md) lists the public API). Do not restyle them locally.
- Use the Style resource keys (`Spacing*`, `FontSize*`, `CornerRadius*`, `AccentFill*` and the other families in 0017) instead of hard-coded values.
- Theme through `StandardsTheme`: `Accent`, `Density`, `ReduceMotion`, `ReduceTransparency`, `HighRefreshRate`, `GlassQuality`.
- Glass nests two levels deep: the second level is minimal glass and a third level and deeper is the solid fill. `GlassQuality.Minimal` or `Solid` helps if heavy canvas work costs frame rate ([0015](../decisions/0015-glass.md)).
- Continuous animation uses `StandardsWindow.FrameTick`, which runs at 30 fps unless `HighRefreshRate` is set ([0016](../decisions/0016-motion.md)).
- Headless tests:
  - `StandardsTheme.CreateForTests(AccessibilitySettings)` pins the OS reduce-motion and reduce-transparency settings (`AccessibilitySettings.NotReduced` for neither).
  - `StandardsWindow.TitleIconDecoderForTests` lets a test render the Windows layout's title icon. Apps leave it null.

## Reading list
| Decision | For |
|---|---|
| [0017](../decisions/0017-app-usage.md) | The public API and resource keys |
| [0022](../decisions/0022-window-layout.md) | Window layout, slots, backdrop |
| [0034](../decisions/0034-sidebar.md) | Sidebar |
| [0043](../decisions/0043-toolbar-band.md) | Toolbar band |
| [0047](../decisions/0047-status-bar.md) | Status bar |
| [0044](../decisions/0044-menu-bar.md) | Menu bar and NativeMenu |
| [0036](../decisions/0036-menus-and-flyouts.md) | Menus and flyouts |
| [0049](../decisions/0049-full-screen.md) | Full screen and window buttons |
| [0015](../decisions/0015-glass.md) | Glass and its limits |
| [0016](../decisions/0016-motion.md) | Motion and FrameTick |
| [0026](../decisions/0026-icon.md) | Icons |
| [0013](../decisions/0013-spacing-and-density.md) | Spacing and density |

## Missing components
A reusable control, panel or property the library lacks is requested from the library, not built into the app.

- **Request:** open an issue on the GitHub repository github.com/shadowevil/avalonia-ui-standards. It says what is missing, what it is for, how desktop apps present it, and which Avalonia control it might restyle.
- **Until it lands:** the app uses a temporary stand-in, marked with a comment saying it waits on the library, and swaps it for the library's version when that lands.
- **App-specific pieces** (a canvas, a brush engine, filter maths) stay in the app.
- Never build a lookalike of a library component.

## Visual checks
Layouts built from library pieces are rendered headlessly (Avalonia.Headless, [0009](../decisions/0009-screenshots-from-tests.md)). Real-window measurements run in one launch that switches scenarios inside it.
