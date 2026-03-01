#!/bin/zsh
set -euo pipefail

SCRIPT_DIR="${0:A:h}"
cd "$SCRIPT_DIR"

dotnet run --project ChurchApp.AppHost/ChurchApp.AppHost/ChurchApp.AppHost.csproj
