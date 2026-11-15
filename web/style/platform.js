// Platform facts shared by the Blocks, with no side effects on load.

/** True on macOS and iOS, where Cmd (Meta) is the command modifier instead of Ctrl. */
export const isMac = /Mac|iPhone|iPad/.test(navigator.platform);
