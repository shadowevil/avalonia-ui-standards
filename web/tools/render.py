#!/usr/bin/env python3
"""Render to PNG in light and dark: python3 web/tools/render.py [--only id,...] [page ...]

Without pages, renders every catalog page (the registry in catalog/pages/index.js) whole, at the
desktop's wide and narrow content widths: catalog-page-<id>-<light|dark>[-narrow].png. Pages given
as paths relative to web/ (with any query) are rendered at --width x --height instead.
"""
import argparse
import re
import subprocess
import sys

import common

# Verified in Chrome: preferredColorScheme=0 gives dark, =1 gives light.
SCHEMES = {"light": 1, "dark": 0}
# The desktop's whole-page content widths (1000 and 620) plus the glass panel's 8 px side margins.
CATALOG_WIDTHS = {"": 1016, "-narrow": 636}


def screenshot(chrome, profile, url, target, width, height, scheme, budget=None):
    target.unlink(missing_ok=True)
    cmd = [
        chrome, "--headless=new", f"--user-data-dir={profile}",
        f"--screenshot={target}", f"--window-size={width},{height}",
        "--hide-scrollbars", "--force-device-scale-factor=2",
        f"--blink-settings=preferredColorScheme={SCHEMES[scheme]}",
    ]
    if budget:
        cmd.append(f"--virtual-time-budget={budget}")
    try:
        run = subprocess.run(cmd + [url], capture_output=True, timeout=120)
        return run.returncode == 0 and target.is_file()
    except subprocess.TimeoutExpired:
        return False


def report(ok, target, label):
    if ok:
        print(f"rendered {target.relative_to(common.WEB.parent)}")
    else:
        print(f"FAILED {label}", file=sys.stderr)
    return ok


def render_catalog(chrome, profile, base, renders, only):
    """Every catalog page, full height: the page ids and each page's height come from the catalog's ?render mode."""
    index = f"{base}/catalog/index.html?render"
    ids = (common.html_attribute(common.dump_dom(chrome, profile, index), "data-pages") or "").split()
    if not ids:
        print("FAILED reading the catalog's pages", file=sys.stderr)
        return False
    unknown = [i for i in only if i not in ids]
    if unknown:
        sys.exit("Not a catalog page: " + ", ".join(unknown) + " (pages: " + ", ".join(ids) + ")")
    ok = True
    for page in only or ids:
        for suffix, width in CATALOG_WIDTHS.items():
            url = f"{index}#{page}"
            # A headless run now and then ends before the page reports; it is tried again.
            for _ in range(3):
                height = common.html_attribute(common.dump_dom(chrome, profile, url, width), "data-render-height")
                if height and height.isdigit():
                    break
            if not height or not height.isdigit():
                ok = report(False, None, f"{page}{suffix} (no height)") and ok
                continue
            for scheme in SCHEMES:
                target = renders / f"catalog-page-{page}-{scheme}{suffix}.png"
                ok = report(screenshot(chrome, profile, url, target, width, int(height), scheme, budget=3000), target, f"{page} ({scheme}{suffix})") and ok
    return ok


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("pages", nargs="*", help="paths relative to web/, with any query (default: every catalog page)")
    ap.add_argument("--only", default="", help="comma-separated catalog page ids (default: all)")
    ap.add_argument("--width", type=int, default=1280)
    ap.add_argument("--height", type=int, default=800)
    args = ap.parse_args()

    missing = [p for p in args.pages if not (common.WEB / re.split(r"[?#]", p)[0]).is_file()]
    if missing:
        sys.exit("Page not found under web/: " + ", ".join(missing))
    chrome = common.find_chrome()
    renders = common.OUT / "renders"
    renders.mkdir(parents=True, exist_ok=True)
    ok = True

    with common.serve() as base, common.profile_dir() as profile:
        if not args.pages:
            ok = render_catalog(chrome, profile, base, renders, [i for i in args.only.split(",") if i])
        for page in args.pages:
            name = re.sub(r"\W+", "-", re.sub(r"\.html", "", page)).strip("-")
            for scheme in SCHEMES:
                target = renders / f"{name}-{scheme}.png"
                ok = report(screenshot(chrome, profile, f"{base}/{page}", target, args.width, args.height, scheme), target, f"{page} ({scheme})") and ok
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
