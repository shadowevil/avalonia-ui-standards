# 0010 — One library, src and tests

- **Status:** Agreed, 2026-11-12
- **Decision:**
  - The library and its root namespace are named `AvaloniaUiStandards`.
  - One library project. The tiers ([0004](0004-tier-names.md)) are folders and namespaces inside it (`Style/`, `Blocks/`, `Modules/`, `Screens/`), created when the first file for a tier exists.
  - The library lives in `src/AvaloniaUiStandards/` and its tests in `tests/AvaloniaUiStandards.Tests/`.
  - A `.slnx` solution, `AvaloniaUiStandards.slnx`, sits at the repo root.
  - Package versions are pinned centrally in `Directory.Packages.props` at the root.
- **Why:** Apps include a single package. The tier rule (lower tiers only, [0004](0004-tier-names.md)) is checked in review rather than by project boundaries. One file pins every version ([0008](0008-target-versions.md)).
