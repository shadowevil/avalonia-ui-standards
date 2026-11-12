# 0008 — .NET 10 and Avalonia 12

- **Status:** Agreed, 2026-11-12
- **Decision:** The library targets `net10.0` (.NET 10 LTS) and Avalonia 12, the current stable line (12.1.6 as of 2026-11-09). The exact Avalonia version is pinned when the solution is created, and upgrades are deliberate changes. Building with a newer SDK is fine.
- **Why:** LTS support for apps that include the library. Avalonia 12 is the current stable line and is built for .NET 10, so a new library starting on 11 would only face a migration. Pinning keeps builds reproducible.
