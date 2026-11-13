# Roadmap

Work goes tier by tier under the dependency rule of [0004](../decisions/0004-tier-names.md): each piece is built only on finished lower-tier pieces, so a Module may start while later Blocks are still open. Each milestone's contents are agreed before work starts.

1. **M1 Solution skeleton:** library project, test project with headless rendering ([0008](../decisions/0008-target-versions.md), [0009](../decisions/0009-screenshots-from-tests.md)).
2. **M2 Style tier** ([0004](../decisions/0004-tier-names.md)): the design values (colour [0011](../decisions/0011-colour.md), type [0012](../decisions/0012-type.md), spacing and density [0013](../decisions/0013-spacing-and-density.md), shape [0014](../decisions/0014-shape.md), motion [0016](../decisions/0016-motion.md)), the primitives `SquircleBorder` ([0014](../decisions/0014-shape.md)) and `GlassBorder` ([0015](../decisions/0015-glass.md)), and the app entry points ([0017](../decisions/0017-app-usage.md)). Where Fluent exposes a resource for a value, the Style tier sets it.
3. **M3 First Blocks**, in this order:
   1. Button and its siblings (ToggleButton, RepeatButton, SplitButton, DropDownButton, HyperlinkButton, in the same pattern; [0018](../decisions/0018-button.md), [0021](../decisions/0021-button-siblings.md)). Done.
   2. The window buttons (traffic lights, caption buttons; [0022](../decisions/0022-window-layout.md)). They are Blocks and draw their glyphs themselves, with no Icon dependency. Done.
   3. The shell Module `StandardsWindow` with a sidebar slot but no sidebar (see M4). Done.
   4. The catalog moves into the window of [0022](../decisions/0022-window-layout.md), keeping its current sidebar list for now. Done.
   5. ListBox and ScrollViewer ([0024](../decisions/0024-lists-and-scrolling.md)). ListBox is used for sidebar navigation lists. The ScrollViewer restyle shows a soft shadow on each edge that has content out of view, for every ScrollViewer.
   5b. Frame rate: continuous animation at 30 fps by default ([0016](../decisions/0016-motion.md)). Done.
   5c. Glass rim: a hairline that takes a light tint of what's below ([0015](../decisions/0015-glass.md)). Done.
   5d. Overlay scrollbar: nearly invisible until scrolling or the pointer comes near, then it fades in and widens with proximity for easy grabbing; no arrow buttons; the thumb and track are always fully rounded pills; it floats over content ([0024](../decisions/0024-lists-and-scrolling.md), [0025](../decisions/0025-overlay-scrollbar.md)). Done.
   6. Native window behaviour on macOS: system shadow, native border and chrome roles ([0022](../decisions/0022-window-layout.md)). Windows and Linux are a later step.
   7. TextInput (Fluent `TextBox`).
   8. ComboBox.
   9. Icon (restyled Fluent `PathIcon`). The Icon set is Phosphor Regular (vector, MIT; [0026](../decisions/0026-icon.md)). Done.
   10. The sidebar Module with its collapse stages (see M4; needs ListBox and Icon).
   11. CheckBox and ToggleSwitch.
   12. Segmented control (multi-switch): a Block for choosing one of several options for a single setting, with text and/or icon segments. Design per platform best practices, researched and agreed at that step.
   13. TreeView, reusing the list item design of [0024](../decisions/0024-lists-and-scrolling.md), with a caret to expand and collapse.
   14. The rest of Avalonia's built-in Fluent controls, restyled: RadioButton, Slider, ProgressBar (with ProgressRing), NumericUpDown and ButtonSpinner, AutoCompleteBox, TabControl and TabStrip, Expander, Menu, ContextMenu, MenuFlyout and Flyout, ToolTip, DatePicker, TimePicker, Calendar and CalendarDatePicker (this fixes the known picker overrun), Separator, GridSplitter, notification card. Each gets a catalog page.

   Blocks Fluent provides are restyled Fluent. All have the focus ring ([0019](../decisions/0019-focus-ring.md)). The catalog app ([0020](../decisions/0020-catalog-app.md)) starts with the Style pages and Button, and each Block step adds its page.

   DataGrid and ColorPicker (separate Avalonia packages), after M3.
4. **M4 First Modules:** the shell Module `StandardsWindow` (frame, title band, glass content panel, status bar, sidebar slot; [0022](../decisions/0022-window-layout.md)), built at step 3 of M3, and the sidebar Module that plugs into its slot, built at step 10. See Open questions for the rest.
   - Toolbar band: a Module directly below the title band, holding commands and settings (icons, segmented controls, menus), following toolbar best practices. Researched and agreed at that step. The catalog then moves its theme controls out of the title band into the toolbar band, using segmented controls where a setting has a few options (for example theme System/Light/Dark, density Normal/Compact, motion and transparency Follow OS/Reduced/Full).
5. **M5 First Screen:** the app shell ([0022](../decisions/0022-window-layout.md)).

Then the web decision is due ([0002](../decisions/0002-desktop-first.md)).

## Open questions

- First Modules beyond the app shell's (M4).
- Which Screens beyond the app shell (M5).

## Known items for later steps

- The DatePicker and TimePicker flyout items overrun their rounded corners ([0024](../decisions/0024-lists-and-scrolling.md)).
