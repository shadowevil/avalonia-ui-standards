# 0050 — Adopting apps

- **Status:** Agreed, 2026-11-14
- **Decision:**
  - Apps request a missing reusable component, and report issues, as GitHub issues on shadowevil/avalonia-ui-standards, where they are evaluated.
    - **Why:** One public place keeps requests and their outcomes visible, and the library decides what belongs in which tier ([0004](0004-tier-names.md)).
  - Until a component lands, the app uses a temporary stand-in marked with a comment saying it waits on the library, and swaps it for the library's version once that is on main.
    - **Why:** The app is not blocked, and the comment makes every stand-in findable and removable.
  - App-specific pieces stay in the app.
    - **Why:** Only reusable pieces belong in the library.
  - Apps never build a lookalike of a library component and never restyle library controls locally.
    - **Why:** A lookalike or a local restyle drifts from the set look and from the library's states, accessibility and motion ([0007](0007-restyle-scope.md)).
  - Layouts are checked with headless renders ([0009](0009-screenshots-from-tests.md)). Real-window measurements run in one launch that switches scenarios inside it.
    - **Why:** Headless renders are repeatable and independent of the machine. Relaunching windows per scenario is slow and disturbs the desktop, and one launch gives comparable numbers.
- **Why:** Apps that follow the same rules keep the library the single source of the look and the components.
