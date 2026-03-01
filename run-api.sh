#!/bin/zsh

set -euo pipefail

SCRIPT_DIR="${0:A:h}"
cd "$SCRIPT_DIR"

./ensure-ef-compiled-model.sh

echo "🚀 Running API..."
dotnet run --no-build --project ChurchApp.API/ChurchApp.API/ChurchApp.API.csproj
