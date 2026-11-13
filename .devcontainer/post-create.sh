#!/usr/bin/env bash
set -euo pipefail

PRETTIER_VERSION=3.9.10

# Installs CSharpier at the version pinned in .config/dotnet-tools.json.
dotnet tool restore

npm install --global --no-fund --no-audit "prettier@$PRETTIER_VERSION"

tools=(
  dotnet    # .NET SDK for C# language support and the Unity extension (base image)
  node      # runtime for Prettier (node feature)
  python    # runtime for the repo's Python scripts (python feature)
  prettier  # Markdown formatter (installed above)
  ruff      # Python linter and formatter (python feature)
)

for cmd in "${tools[@]}"; do
  printf '%-10s %s\n' "$cmd" "$("$cmd" --version 2>/dev/null || echo "not found")"
done

bash .githooks/setup.sh
