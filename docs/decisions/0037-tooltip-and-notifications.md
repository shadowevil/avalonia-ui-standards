# 0037 — ToolTip and notification cards

- **Status:** Agreed, 2026-11-13
- **Decision:**
  - ToolTip keeps Fluent's behaviour and placement, and Avalonia's delays: it opens 400 ms after the pointer rests on a control, and 100 ms when moving between controls while one is shown.
    - **Why:** 400 ms sits with the platforms (Windows opens tooltips after the double-click time, 500 ms by default). Changing it for the whole app would take a style on every control, for a difference too small to matter.
  - The tooltip is a small solid card, not glass: the card colour (`ChromeMediumLow`) with a 1 px border in the border colour (`BaseLow`), the control radius 8 (`CornerRadiusControl`), caption text (12 px) in the primary text colour, padding `SpacingS` at the sides and `SpacingXS` above and below. Text wraps at Fluent's 320 px maximum width.
    - **Why:** At tooltip size the glass's 28 px refraction band covers the whole card, so everything behind it smears, and the rim's glow outlines a 23 px tall card heavily. A solid card reads at a glance, as tooltips on macOS and Windows do. This departs from [0015](0015-glass.md), which lists tooltips among the glass overlays. The control radius suits a card one line high; the overlay radius 16 would make it a pill.
  - The tooltip fades in with the fixed `MotionFast` opacity transition (none under reduce motion, [0016](0016-motion.md)), replacing Fluent's own. It closes at once.
  - The notification manager (`WindowNotificationManager`) keeps Fluent's positions and stacking, inset `SpacingS` from the window edges.
  - A notification card is a glass card ([0015](0015-glass.md)) with the overlay radius 16, 360 px wide, `SpacingL` padding and an outer margin of `SpacingS`, so cards sit 16 px from the window edge and 16 px apart.
    - **Why:** It floats over the app like other overlays. 360 is on the grid and close to the banner widths of macOS and Windows; Fluent's 350 is not on the grid.
  - Layout: the type icon (Icon size, 20 px at Normal and 16 px at Compact, [0026](0026-icon.md)) at the top left, `SpacingM` before the text; the title in body strong over the message in body text, `SpacingXS` apart, both wrapping and in the primary text colour; a close button at the top right.
    - **Why:** Title then message is the order both platforms use; the message stays in the primary colour so it reads as easily as the title.
  - Type icons and colours:

    | Type | Icon | Colour |
    |---|---|---|
    | Information | `Icons.Info` | `AccentTextColor` |
    | Success | `Icons.CheckCircle` | `SuccessTextColor` |
    | Warning | `Icons.Warning` | `WarningTextColor` |
    | Error | `Icons.WarningCircle` | `DangerTextColor` |

  - New Style tier colours ([0011](0011-colour.md) has only the accent and the danger colour):
    - `SuccessColor` `#22A134`, between the Windows success green (`#0F7B0F`) and the macOS system green (`#34C759`).
    - `WarningColor` `#CE7900`, between the Windows caution colour (`#9D5D00`) and the macOS system orange (`#FF9500`).
    - `SuccessTextColor`, `WarningTextColor` and `DangerTextColor` are these and the danger colour stepped by the `AccentTextColor` rule of 0011: lightness moves (darker in light, lighter in dark) until the colour reaches 4.5:1 on the page, the card, the backdrop wash ends and the glass over them. They are recomputed with the accent, as the wash changes with it. Defaults:

      | Theme | `SuccessTextColor` | `WarningTextColor` | `DangerTextColor` |
      |---|---|---|---|
      | Light | `#197726` | `#965800` | `#C9251C` |
      | Dark | `#23A736` | `#D37C00` | `#E96962` |

    - The keys join the Style tier key families of [0017](0017-app-usage.md) as `Success*`, `Warning*` and `DangerTextColor`.
    - **Why:** The status colours follow the method 0011 used for the accent and danger colours (between the two platforms' values) and its rule for coloured text, so status icons and any status text meet AA in both themes. Information uses the accent, as Windows does for informational messages. The danger brand colour itself fails 4.5:1 as text on white, hence its text variant.
  - The close button is a subtle Button ([0018](0018-button.md)) holding `Icons.X`, square at Button's density height (32 at Normal, 24 at Compact), pulled into the card's padding so its centre lines up with the type icon. It closes the card through Fluent's `NotificationCard.CloseOnClick`, and its accessible name is "Close".
    - **Why:** It reuses the button family's states and focus ring; the subtle variant keeps the card quiet until the pointer is on the button.
  - Cards fade in over `MotionSlow` (300 ms) when shown, and fade out over `MotionSlow` before they are removed when closed or expired. Under reduce motion they appear and go at once ([0016](0016-motion.md)). The fades are fixed animation objects in the theme's resources. Fluent's slide and collapse are dropped.
    - **Why:** Notifications arrive unasked, so a gentler fade than the user-opened overlays of [0036](0036-menus-and-flyouts.md) draws less attention; 0016 puts dialog-like surfaces under `MotionSlow`.
  - A card's content that is not a notification (a string or a control) is shown as is, in place of the title and message.
- **Why:** Tooltips stay light and legible; notification cards share the overlay glass and carry their type in an icon and colour that meet AA.
