#!/usr/bin/env bash
set -euo pipefail

CSHARPIER_VERSION=1.3.0
PRETTIER_VERSION=3.9.10

# dotnet tool install fails in a directory with more than one project file.
(cd "$HOME" && dotnet tool update --global csharpier --version "$CSHARPIER_VERSION")

npm install --global --no-fund --no-audit "prettier@$PRETTIER_VERSION"

tools=(
  dotnet    # .NET SDK for C# language support and the Unity extension (base image)
  node      # runtime for Prettier (node feature)
  python    # runtime for the repo's Python scripts (python feature)
  csharpier # C# formatter (installed above)
  prettier  # Markdown formatter (installed above)
  ruff      # Python linter and formatter (python feature)
)

for cmd in "${tools[@]}"; do
  printf '%-10s %s\n' "$cmd" "$("$cmd" --version 2>/dev/null || echo "not found")"
done

bash .githooks/setup.sh
