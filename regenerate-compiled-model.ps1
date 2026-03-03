# Regenerate EF Core Compiled Model (Jez Humble's automated build pipeline)
# Run this after any entity or configuration changes

Write-Host "🔨 Regenerating EF Core Compiled Model..." -ForegroundColor Cyan

cd ChurchApp.Application\ChurchApp.Application

# Remove old compiled models first
Remove-Item "Infrastructure\CompiledModels\*" -Force -ErrorAction SilentlyContinue

dotnet ef dbcontext optimize `
    --startup-project ..\..\ChurchApp.API\ChurchApp.API\ChurchApp.API.csproj `
    --context ChurchApp.Application.Infrastructure.ChurchAppDbContext `
    --output-dir Infrastructure/CompiledModels `
    --namespace ChurchApp.Application.Infrastructure.CompiledModels

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Compiled model regenerated successfully" -ForegroundColor Green
    Write-Host "📝 Files updated in Infrastructure/CompiledModels/" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "⚠️  Important: Commit these files to Git" -ForegroundColor Yellow
    Write-Host "    They are part of your application code, not generated at runtime"
} else {
    Write-Host "❌ Failed to regenerate compiled model" -ForegroundColor Red
    exit 1
}
