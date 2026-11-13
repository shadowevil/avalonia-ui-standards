# 0011 — Colour

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - Light and dark variants. They follow the OS by default; an app can force one.
  - The default accent is `#007CEA`, between the Windows 11 blue (`#0078D4`) and the macOS system blue (`#007AFF`). Apps can override it.
  - The danger colour is `#E1332A`, between the Windows critical red (`#C42B1C`) and the macOS system red (`#FF3B30`). Its foreground, `DangerForeground`, is white. The brand keys `SystemAccentColor` (`#007CEA`) and `DangerColor` (`#E1332A`) keep their values.
  - Filled controls meet WCAG AA contrast with white text, by the AA fill rule. For the accent and the danger colour, rest is the brand colour darkened by the smallest HSL lightness step that reaches 4.5:1 with white text; hover is 0.04 darker; pressed is 0.08 darker. It is recomputed for any app accent, and is the same in light and dark.
  - Keys: `AccentFillColor`, `AccentFillColorPointerOver`, `AccentFillColorPressed`, `DangerFillColor`, `DangerFillColorPointerOver`, `DangerFillColorPressed`.
  - The success and warning colours and the status text keys (`SuccessColor`, `WarningColor`, `SuccessTextColor`, `WarningTextColor`, `DangerTextColor`) are in [0037](0037-tooltip-and-notifications.md).
  - Default results (contrast with white text):

    | | Rest | Hover | Pressed |
    |---|---|---|---|
    | Accent | `#0075DD` 4.58:1 | `#006AC9` 5.38:1 | `#0060B4` 6.30:1 |
    | Danger | `#E02F26` 4.56:1 | `#D4271E` 5.11:1 | `#C2231B` 5.92:1 |

  - Link text uses `AccentTextColor` and `AccentTextColorPressed`. The accent's lightness is stepped (darker in light, lighter in dark) until text reaches 4.5:1 on the page, the card, the backdrop wash ends and the glass panel over the wash ends ([0022](0022-window-layout.md)); pressed is one 0.04 step further. It is recomputed for any app accent. Defaults:

    | Theme | `AccentTextColor` | Pressed |
    |---|---|---|
    | Light | `#0068C4` (at least 4.50:1; the wash corner is the lowest) | `#005DAF` (at least 5.34:1) |
    | Dark | `#1692FF` (4.50:1 on the card) | `#2B9BFF` (4.95:1) |

  - Secondary text (`BaseMedium`) is derived the AccessibleColors way: it starts from the palette grey and steps lightness only as far as needed for 4.5:1 on the page, the card, the wash ends and the glass over each. It is recomputed with the accent. Defaults: light `#686868` (from `#737373`), dark `#9E9E9E` (unchanged). `BaseMedium` also feeds Fluent's borders and strokes, so they follow this value. Disabled text (`BaseMediumLow`) is not forced to AA.
  - Neutral greys with no tint, and a dark grey (not black) dark variant. Region and card values are in the table.
  - The full ramps are set through Fluent's `ColorPaletteResources`:

    | Property | Light | Dark |
    |---|---|---|
    | `RegionColor` | `#F5F5F5` | `#1E1E1E` |
    | `AltHigh` | `#FFFFFF` | `#1E1E1E` |
    | `BaseHigh` (primary text) | `#1A1A1A` | `#F2F2F2` |
    | `BaseMediumHigh` | `#404040` | `#D9D9D9` |
    | `BaseMedium` (secondary text; AA-derived, see above) | `#686868` | `#9E9E9E` |
    | `BaseMediumLow` (disabled text) | `#A6A6A6` | `#6B6B6B` |
    | `BaseLow` (borders, lines) | `#D4D4D4` | `#454545` |
    | `ChromeLow` | `#E8E8E8` | `#2A2A2A` |
    | `ChromeMediumLow` (card) | `#FFFFFF` | `#2A2A2A` |
    | `ChromeMedium` | `#E8E8E8` | `#333333` |
    | `ChromeHigh` | `#D4D4D4` | `#525252` |
    | `ListLow` | `#E8E8E8` | `#2A2A2A` |
    | `ListMedium` | `#D4D4D4` | `#404040` |

- **Why:** The colours feel native on both OSes, each app can carry its own brand accent, and dark mode is a dark grey instead of pure black.
