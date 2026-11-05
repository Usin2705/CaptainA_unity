#!/usr/bin/env bash
# Format and lint all C#, Markdown and Python files.
set -euo pipefail

cd "$(dirname "$0")/.."

# Skips a tool that is not installed, so the other tools still run.
run() {
  if command -v "$1" >/dev/null; then
    "$@"
  else
    echo "format.sh: $1 is not installed, skipping" >&2
  fi
}

case "${1:-}" in
  --check)
    run csharpier check Assets/Scripts
    run prettier --check "**/*.md"
    run ruff format --check .
    run ruff check .
    ;;
  --fix)
    run csharpier format Assets/Scripts
    run prettier --write "**/*.md"
    run ruff check --fix .
    run ruff format .
    ;;
  *)
    echo "Usage: $0 --check | --fix" >&2
    exit 2
    ;;
esac
