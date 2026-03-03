#!/bin/bash
# Regenerate EF Core Compiled Model (Jez Humble's automated build pipeline)
# Run this after any entity or configuration changes

echo "🔨 Regenerating EF Core Compiled Model..."

cd ChurchApp.Application/ChurchApp.Application

dotnet ef dbcontext optimize \
    --startup-project ../../ChurchApp.API/ChurchApp.API/ChurchApp.API.csproj \
    --context ChurchApp.Application.Infrastructure.ChurchAppDbContext \
    --output-dir Infrastructure/CompiledModels \
    --namespace ChurchApp.Application.Infrastructure.CompiledModels \
    --force

if [ $? -eq 0 ]; then
    echo "✅ Compiled model regenerated successfully"
    echo "📝 Files updated in Infrastructure/CompiledModels/"
    echo ""
    echo "⚠️  Important: Commit these files to Git"
    echo "    They are part of your application code, not generated at runtime"
else
    echo "❌ Failed to regenerate compiled model"
    exit 1
fi