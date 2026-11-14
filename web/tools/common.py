"""Shared helpers: a static server for web/ and a locally installed Chrome."""
import functools
import http.server
import os
import shutil
import tempfile
import threading
from contextlib import contextmanager
from pathlib import Path

WEB = Path(__file__).resolve().parent.parent
OUT = WEB / "out"

_CHROME_PATHS = [
    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
    "C:/Program Files/Google/Chrome/Application/chrome.exe",
    "C:/Program Files (x86)/Google/Chrome/Application/chrome.exe",
]
_CHROME_NAMES = ["google-chrome", "google-chrome-stable", "chromium", "chromium-browser", "chrome"]


def find_chrome():
    env = os.environ.get("CHROME")
    if env:
        if not Path(env).is_file():
            raise SystemExit(f"CHROME is set but is not a file: {env}")
        return env
    for c in _CHROME_PATHS:
        if Path(c).is_file():
            return c
    for name in _CHROME_NAMES:
        found = shutil.which(name)
        if found:
            return found
    raise SystemExit("Chrome not found; set the CHROME environment variable.")


class _QuietHandler(http.server.SimpleHTTPRequestHandler):
    def log_message(self, *args):
        pass


@contextmanager
def serve():
    """Serve web/ on a free localhost port; yields the base URL."""
    handler = functools.partial(_QuietHandler, directory=str(WEB))
    server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        yield f"http://127.0.0.1:{server.server_address[1]}"
    finally:
        server.shutdown()
        server.server_close()


@contextmanager
def profile_dir():
    """A fresh Chrome profile folder inside web/out/, removed afterwards."""
    OUT.mkdir(exist_ok=True)
    path = tempfile.mkdtemp(prefix="chrome-profile-", dir=OUT)
    try:
        yield path
    finally:
        shutil.rmtree(path, ignore_errors=True)
