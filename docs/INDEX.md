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

## Plan
- [Roadmap](plan/roadmap.md) — the ordered milestones and the open questions
