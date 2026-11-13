# 0004 — Tiers: Style → Blocks → Modules → Screens

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Tiers: everything the library ships belongs to one of four.
    - **Style**: design values (colour, spacing, type, corner radius, motion) and the shape and material primitives that draw them (`SquircleBorder`, `GlassBorder`; [0014](0014-shape.md), [0015](0015-glass.md)). It is not Avalonia's `Style` class.
    - **Blocks**: single-purpose pieces, some interactive (Button, TextInput, CheckBox, ComboBox, ListBox, Icon). A Block may be composite inside when it serves one purpose (ComboBox, ListBox).
    - **Modules**: composite controls that combine several purposes, the Sidebar ([0034](0034-sidebar.md)), or composed from Blocks (Dialog).
    - **Screens**: full arrangements of Modules (settings page, form layout, app shell).
  - Each control's Fluent override (Avalonia `Style` / `ControlTheme`) lives in the tier that control belongs to (Blocks or Modules).
  - `StandardsTheme` ([0017](0017-app-usage.md)) is the library's entry point, in the root namespace `AvaloniaUiStandards`. It assembles every tier and belongs to none.
  - A tier uses only the tiers below it: Blocks use Style; Modules use Blocks and Style; Screens use any lower tier.
- **Why:** Plain names that state intent and avoid clashing with Avalonia's `Control` base class; the shared name with Avalonia's `Style` is accepted. Dependencies run one way, so a higher tier can change without affecting the tiers below it, and each piece is built only on the finished lower-tier pieces it uses.
