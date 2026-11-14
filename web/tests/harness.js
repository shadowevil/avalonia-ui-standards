const tests = [];

export function test(name, fn) {
  tests.push({ name, fn });
}

export function assert(condition, message = "assertion failed") {
  if (!condition) throw new Error(message);
}

export function assertEqual(actual, expected, message) {
  if (!Object.is(actual, expected)) {
    throw new Error(`${message ? message + ": " : ""}expected ${String(expected)}, got ${String(actual)}`);
  }
}

// Headless Chrome gives the page document focus asynchronously; it may arrive after the load
// event and its first task. Until then :focus and :focus-visible never match, even for a focused
// element, so wait for it (at most 1 s) before any test calls focus().
async function documentFocused() {
  for (let i = 0; i < 100 && !document.hasFocus(); i++) {
    await new Promise((resolve) => setTimeout(resolve, 10));
  }
  return document.hasFocus();
}

async function run() {
  const focused = await documentFocused();
  const failures = [];
  for (const { name, fn } of tests) {
    try {
      await fn();
    } catch (e) {
      failures.push(`${name}: ${e.message}`);
    }
  }
  const status = failures.length ? "FAIL" : "PASS";
  const passed = tests.length - failures.length;
  const lines = [`${status} ${passed}/${tests.length}`, ...failures];
  if (failures.length && !focused) lines.push("note: the document never had focus, so :focus-visible checks could not pass");
  document.getElementById("results").textContent = lines.join("\n");
}

window.addEventListener("load", () => setTimeout(run));
