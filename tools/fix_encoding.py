"""Strip mojibake / non-ASCII from ZoneShift source so UI never shows accented garbage."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
SKIP_DIRS = {"bin", "obj", "publish", "dist", ".git"}

REPLACEMENTS = {
    "\u2014": " - ",
    "\u2013": "-",
    "\u2192": "->",
    "\u00d7": "x",
    "\u00b7": " - ",
    "\u00a0": " ",
    "\u2022": "*",
    "\u2019": "'",
    "\u2018": "'",
    "\u201c": '"',
    "\u201d": '"',
    "\u2026": "...",
    "\u00c2": "",
    "\u00e2": "",
    "\u2020": "",
}


def clean_text(text: str) -> str:
    for old, new in REPLACEMENTS.items():
        text = text.replace(old, new)
    # Drop remaining non-ASCII only (preserve tabs/spaces/newlines)
    text = re.sub(r"[^\t\n\r\x20-\x7e]", "", text)
    text = text.replace(" - - ", " - ")
    return text


def main() -> None:
    patterns = ("*.cs", "*.md", "*.iss", "*.ps1")
    changed = 0
    for pattern in patterns:
        for path in ROOT.rglob(pattern):
            if any(part in SKIP_DIRS for part in path.parts):
                continue
            original = path.read_text(encoding="utf-8", errors="replace")
            cleaned = clean_text(original)
            if cleaned != original:
                path.write_text(cleaned, encoding="utf-8", newline="\n")
                print(f"cleaned {path.relative_to(ROOT)}")
                changed += 1
            else:
                print(f"ok {path.relative_to(ROOT)}")
    print(f"done, {changed} file(s) updated")


if __name__ == "__main__":
    main()
