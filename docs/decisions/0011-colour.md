# 0011 — Colour

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Light and dark variants. They follow the OS by default; an app can force one.
  - The default accent is `#007CEA`, between the Windows 11 blue (`#0078D4`) and the macOS system blue (`#007AFF`). Apps can override it.
  - Neutral greys with no tint, and a dark grey (not black) dark variant. Region and card values are in the table.
  - The full ramps are set through Fluent's `ColorPaletteResources`:

    | Property | Light | Dark |
    |---|---|---|
    | `RegionColor` | `#F5F5F5` | `#1E1E1E` |
    | `AltHigh` | `#FFFFFF` | `#1E1E1E` |
    | `BaseHigh` (primary text) | `#1A1A1A` | `#F2F2F2` |
    | `BaseMediumHigh` | `#404040` | `#D9D9D9` |
    | `BaseMedium` (secondary text) | `#737373` | `#9E9E9E` |
    | `BaseMediumLow` | `#A6A6A6` | `#6B6B6B` |
    | `BaseLow` (borders, lines) | `#D4D4D4` | `#454545` |
    | `ChromeLow` | `#E8E8E8` | `#2A2A2A` |
    | `ChromeMediumLow` (card) | `#FFFFFF` | `#2A2A2A` |
    | `ChromeMedium` | `#E8E8E8` | `#333333` |
    | `ChromeHigh` | `#D4D4D4` | `#525252` |
    | `ListLow` | `#E8E8E8` | `#2A2A2A` |
    | `ListMedium` | `#D4D4D4` | `#404040` |

- **Why:** The colours feel native on both OSes, each app can carry its own brand accent, and dark mode is a dark grey instead of pure black.
