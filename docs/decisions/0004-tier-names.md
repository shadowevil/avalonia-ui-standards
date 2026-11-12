# 0004 — Tiers: Style → Blocks → Modules → Screens

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Tiers: everything the library ships belongs to one of four.
    - **Style**: design values only (colour, spacing, type, corner radius, motion). It is not Avalonia's `Style` class.
    - **Blocks**: single-purpose pieces, some interactive (Button, TextInput, Checkbox, Icon).
    - **Modules**: composite controls, either restyled from Fluent (DatePicker, DataGrid) or composed from Blocks (Dialog).
    - **Screens**: full arrangements of Modules (settings page, form layout, app shell).
  - Each control's Fluent override (Avalonia `Style` / `ControlTheme`) lives in the tier that control belongs to (Blocks or Modules).
- **Why:** Plain names that state intent and avoid clashing with Avalonia's `Control` base class; the shared name with Avalonia's `Style` is accepted.
