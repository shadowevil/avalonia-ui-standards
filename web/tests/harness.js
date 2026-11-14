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

async function run() {
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
  document.getElementById("results").textContent = lines.join("\n");
}

window.addEventListener("load", () => setTimeout(run));
