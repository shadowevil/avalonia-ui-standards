# Roadmap

Milestones run in order, tier by tier. Each builds only on agreed lower tiers ([0004](../decisions/0004-tier-names.md)), and each milestone's contents are agreed before work starts.

1. **M1 Solution skeleton:** library project, test project with headless rendering ([0008](../decisions/0008-target-versions.md), [0009](../decisions/0009-screenshots-from-tests.md)).
2. **M2 Style tier** ([0004](../decisions/0004-tier-names.md)): the design values (colour [0011](../decisions/0011-colour.md), type [0012](../decisions/0012-type.md), spacing and density [0013](../decisions/0013-spacing-and-density.md), shape [0014](../decisions/0014-shape.md), motion [0016](../decisions/0016-motion.md)), the primitives `SquircleBorder` ([0014](../decisions/0014-shape.md)) and `GlassBorder` ([0015](../decisions/0015-glass.md)), and the app entry points ([0017](../decisions/0017-app-usage.md)). Where Fluent exposes a resource for a value, the Style tier sets it.
3. **M3 First Blocks:** Button, TextInput (Fluent `TextBox`), CheckBox and ToggleSwitch, all restyled Fluent; Icon (restyled Fluent `PathIcon`; needs a vector icon set).
4. **M4 First Modules:** see Open questions.
5. **M5 First Screen:** see Open questions.

Then the web decision is due ([0002](../decisions/0002-desktop-first.md)).

## Open questions

- The icon set (M3).
- The first Modules (M4).
- The first Screen (M5).
