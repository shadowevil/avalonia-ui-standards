# 0005 — No AI tool files in the repo

- **Status:** Agreed, 2026-11-12
- **Decision:** No AI tool's files live anywhere in the repo. Ignored so far: `.claude/`, `CLAUDE.md` and `working/`. Other tools' files are added to `.gitignore` when they appear, with no new decision needed.
- **Accepted consequence:** AI settings, agents and instruction files exist only locally; a fresh clone has none of them.
- **Why:** The repo states what the library is, not who designed it or how it was built. End users do not need that; they get what they need from a README and, later, a wiki.
