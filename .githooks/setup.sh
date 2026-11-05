#!/usr/bin/env bash
# Points this clone at the hooks in .githooks. Run it once per clone.
set -euo pipefail

echo "Setting up Git hooks. See .githooks/README.md for more information..."
cd "$(dirname "$0")"
git config core.hooksPath .githooks
echo "Git hooks enabled from .githooks"
