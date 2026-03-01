#!/bin/zsh

set -euo pipefail

SCRIPT_DIR="${0:A:h}"
cd "$SCRIPT_DIR"

echo "🧩 Ensuring EF Core compiled model is up to date..."

echo "🧰 Restoring local .NET tools..."
dotnet tool restore

echo "📦 Restoring dependencies (nuget.org only)..."
dotnet restore --source https://api.nuget.org/v3/index.json

echo "🔨 Building startup project..."
dotnet build ChurchApp.API/ChurchApp.API/ChurchApp.API.csproj --no-restore

dotnet tool run dotnet-ef dbcontext optimize \
  --context ChurchApp.Application.Infrastructure.ChurchAppDbContext \
  --project ChurchApp.Application/ChurchApp.Application/ChurchApp.Application.csproj \
  --startup-project ChurchApp.API/ChurchApp.API/ChurchApp.API.csproj \
  --output-dir Infrastructure/CompiledModels \
  --namespace ChurchApp.Application.Infrastructure.CompiledModels \
  --no-build

echo "✅ EF Core compiled model generation complete"
