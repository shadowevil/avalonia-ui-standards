#!/usr/bin/env python3
"""Render pages of web/ to PNG in light and dark: python3 web/tools/render.py [page ...]"""
import argparse
import subprocess
import sys
from pathlib import Path

import common

# Verified in Chrome: preferredColorScheme=0 gives dark, =1 gives light.
SCHEMES = {"light": 1, "dark": 0}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("pages", nargs="*", help="paths relative to web/ (default: catalog/*.html)")
    ap.add_argument("--width", type=int, default=1280)
    ap.add_argument("--height", type=int, default=800)
    args = ap.parse_args()

    pages = args.pages or sorted(p.relative_to(common.WEB).as_posix() for p in (common.WEB / "catalog").glob("*.html"))
    if not pages:
        sys.exit("No pages to render.")
    missing = [p for p in pages if not (common.WEB / p).is_file()]
    if missing:
        sys.exit("Page not found under web/: " + ", ".join(missing))
    chrome = common.find_chrome()
    renders = common.OUT / "renders"
    renders.mkdir(parents=True, exist_ok=True)
    failed = False

    with common.serve() as base, common.profile_dir() as profile:
        for page in pages:
            name = Path(page).with_suffix("").as_posix().replace("/", "-")
            for scheme, value in SCHEMES.items():
                target = renders / f"{name}-{scheme}.png"
                target.unlink(missing_ok=True)
                cmd = [
                    chrome, "--headless=new", f"--user-data-dir={profile}",
                    f"--screenshot={target}", f"--window-size={args.width},{args.height}",
                    "--hide-scrollbars", "--force-device-scale-factor=2",
                    f"--blink-settings=preferredColorScheme={value}",
                    f"{base}/{page}",
                ]
                try:
                    run = subprocess.run(cmd, capture_output=True, timeout=120)
                    ok = run.returncode == 0 and target.is_file()
                except subprocess.TimeoutExpired:
                    ok = False
                if ok:
                    print(f"rendered {target.relative_to(common.WEB.parent)}")
                else:
                    print(f"FAILED {page} ({scheme})", file=sys.stderr)
                    failed = True
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
