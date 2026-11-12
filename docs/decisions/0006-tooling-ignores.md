# 0006 — Build, IDE and OS output is gitignored

- **Status:** Agreed, 2026-11-12
- **Decision:** Standard build, IDE and OS output (for example .NET `bin/` and `obj/`, `.idea/`, `.DS_Store`) is gitignored; new entries of the same kind need no further decision.
- **Why:** Build and editor output never belongs in the repo.
