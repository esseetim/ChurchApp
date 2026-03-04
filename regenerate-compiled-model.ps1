# Regenerate EF Core Compiled Model
# Run this after any entity or configuration changes

Write-Host "🔨 Regenerating EF Core Compiled Model..." -ForegroundColor Cyan

# Use absolute pathing based on the script's location
$TargetDir = Join-Path $PSScriptRoot "ChurchApp.Application\ChurchApp.Application"
Set-Location $TargetDir

# Remove old compiled models first
Remove-Item "Infrastructure\CompiledModels\*" -Force -ErrorAction SilentlyContinue

# Executed as a single continuous line to completely avoid parsing and backtick errors
dotnet ef dbcontext optimize --startup-project "..\..\ChurchApp.API\ChurchApp.API\ChurchApp.API.csproj" --context "ChurchApp.Application.Infrastructure.ChurchAppDbContext" --output-dir "Infrastructure/CompiledModels" --namespace "ChurchApp.Application.Infrastructure.CompiledModels"

# Early exit pattern - completely eliminates the need for an 'else' block
if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Failed to regenerate compiled model" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Compiled model regenerated successfully" -ForegroundColor Green
Write-Host "📝 Files updated in Infrastructure/CompiledModels/`n" -ForegroundColor Yellow
Write-Host "⚠️  Important: Commit these files to Git" -ForegroundColor Yellow
Write-Host "    They are part of your application code, not generated at runtime"