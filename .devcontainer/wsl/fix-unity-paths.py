#!/usr/bin/env python3

import re
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parents[2]

CONTAINER_UNITY_PATH = "/unity/"

# Windows Unity installation mapped into the container
WINDOWS_UNITY_PATH = re.compile(
    r"[A-Z]:\\Program Files\\Unity\\Hub\\Editor\\([^<\"]+)",
    re.IGNORECASE,
)


def is_wsl() -> bool:
    """Return whether the container runs on a WSL kernel, which it shares with its host."""
    return "microsoft" in Path("/proc/version").read_text(encoding="utf-8").lower()


def convert_path(match):
    """Return the container path for a matched Windows Unity path."""
    windows_suffix = match.group(1)
    linux_suffix = windows_suffix.replace("\\", "/")
    return CONTAINER_UNITY_PATH + linux_suffix


def fix_csproj(csproj: Path):
    """Rewrite Windows Unity paths in a .csproj file, saving it only if something changed."""
    content = csproj.read_text(encoding="utf-8")

    updated = WINDOWS_UNITY_PATH.sub(convert_path, content)

    if updated != content:
        csproj.write_text(updated, encoding="utf-8")
        print(f"[Unity] Fixed paths: {csproj.name}")


def main():
    """Fix every project's .csproj file when running on WSL, and do nothing elsewhere."""

    if not is_wsl():
        print("[Unity] Not running on WSL. No changes made.")
        return

    print("[Unity] Running on WSL. Fixing paths in .csproj files.")

    projects = list(PROJECT_ROOT.rglob("*.csproj"))

    if not projects:
        print("[Unity] No .csproj files found.")
        return

    for project in projects:
        # Ignore generated build directories
        if any(part in ("Library", "Temp", "obj", "bin") for part in project.parts):
            continue

        fix_csproj(project)

    print("[Unity] Finished fixing paths in .csproj files.")


if __name__ == "__main__":
    main()
