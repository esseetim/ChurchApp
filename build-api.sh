#!/bin/zsh

set -euo pipefail

SCRIPT_DIR="${0:A:h}"
cd "$SCRIPT_DIR"

./ensure-ef-compiled-model.sh

echo "🔨 Building API..."
dotnet build ChurchApp.API/ChurchApp.API/ChurchApp.API.csproj --no-restore
