# Roadmap

Work goes tier by tier under the dependency rule of [0004](../decisions/0004-tier-names.md): each piece is built only on finished lower-tier pieces, so a Module may start while later Blocks are still open. Each milestone's contents are agreed before work starts.

1. **M1 Solution skeleton:** library project, test project with headless rendering ([0008](../decisions/0008-target-versions.md), [0009](../decisions/0009-screenshots-from-tests.md)).
2. **M2 Style tier** ([0004](../decisions/0004-tier-names.md)): the design values (colour [0011](../decisions/0011-colour.md), type [0012](../decisions/0012-type.md), spacing and density [0013](../decisions/0013-spacing-and-density.md), shape [0014](../decisions/0014-shape.md), motion [0016](../decisions/0016-motion.md)), the primitives `SquircleBorder` ([0014](../decisions/0014-shape.md)) and `GlassBorder` ([0015](../decisions/0015-glass.md)), and the app entry points ([0017](../decisions/0017-app-usage.md)). Where Fluent exposes a resource for a value, the Style tier sets it.
3. **M3 First Blocks**, in this order:
   1. Button and its siblings (ToggleButton, RepeatButton, SplitButton, DropDownButton, HyperlinkButton, in the same pattern; [0018](../decisions/0018-button.md), [0021](../decisions/0021-button-siblings.md)). Done.
   2. TextInput (Fluent `TextBox`).
   3. ComboBox.
   4. ListBox, used for sidebar navigation lists.
   5. Icon (restyled Fluent `PathIcon`). The Icon set is Phosphor (vector, MIT).
   6. The window buttons (traffic lights, caption buttons; [0022](../decisions/0022-window-layout.md)).
   7. The shell Modules (see M4).
   8. CheckBox and ToggleSwitch.

   Blocks Fluent provides are restyled Fluent. All have the focus ring ([0019](../decisions/0019-focus-ring.md)). The catalog app ([0020](../decisions/0020-catalog-app.md)) starts with the Style pages and Button, and each Block step adds its page.
4. **M4 First Modules:** the app shell's Modules (window frame, title band, sidebar, status bar, content panel; [0022](../decisions/0022-window-layout.md)); they are built at step 7 of M3. See Open questions for the rest.
5. **M5 First Screen:** the app shell ([0022](../decisions/0022-window-layout.md)).

Then the web decision is due ([0002](../decisions/0002-desktop-first.md)).

## Open questions

- Phosphor weight and icon subset (M3).
- First Modules beyond the app shell's (M4).
- Which Screens beyond the app shell (M5).
