#!/usr/bin/env python3
"""Run web/tests/*.test.html headlessly: python3 web/tools/test.py"""
import html
import re
import subprocess
import sys

import common


def main():
    pages = sorted(common.WEB.glob("tests/*.test.html"))
    if not pages:
        sys.exit("No test pages found.")
    chrome = common.find_chrome()
    failed = False

    with common.serve() as base, common.profile_dir() as profile:
        for page in pages:
            rel = page.relative_to(common.WEB).as_posix()
            try:
                run = subprocess.run(
                    [chrome, "--headless=new", f"--user-data-dir={profile}", "--dump-dom",
                     "--virtual-time-budget=5000", f"{base}/{rel}"],
                    capture_output=True, text=True, timeout=120)
                m = re.search(r'<pre id="results">(.*?)</pre>', run.stdout, re.S)
                text = html.unescape(m.group(1)).strip() if m else ""
            except subprocess.TimeoutExpired:
                text = "timed out"
            m = re.match(r"PASS (\d+)/(\d+)$", text.split("\n")[0])
            if m and m.group(1) == m.group(2) and int(m.group(1)) > 0:
                print(f"{rel}: {text}")
            else:
                print(f"{rel}: {text or 'no result line found'}", file=sys.stderr)
                failed = True
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
