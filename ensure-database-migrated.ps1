# Database Migration Script (Jez Humble's Idempotent Deployment)
# Ensures database schema matches code, can run multiple times safely

$ErrorActionPreference = "Stop"

Write-Host "🔨 ChurchApp Database Migration Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$StartupProject = "ChurchApp.API\ChurchApp.API\ChurchApp.API.csproj"
$MigrationsProject = "ChurchApp.Application\ChurchApp.Application\ChurchApp.Application.csproj"
$Context = "ChurchApp.Application.Infrastructure.ChurchAppDbContext"

# Check if EF Core tools are installed
Write-Host "📡 Checking EF Core tools..." -ForegroundColor Yellow
try {
    dotnet ef --version | Out-Null
    Write-Host "✅ EF Core tools found" -ForegroundColor Green
} catch {
    Write-Host "❌ EF Core tools not installed" -ForegroundColor Red
    Write-Host "Install with: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
    exit 1
}

# Show pending migrations
Write-Host ""
Write-Host "📋 Checking migration status..." -ForegroundColor Yellow
$pending = dotnet ef migrations has-pending-model-changes `
    --startup-project $StartupProject `
    --project $MigrationsProject `
    --context $Context 2>&1

if ($pending -match "No changes have been made") {
    Write-Host "✅ Database schema is up-to-date" -ForegroundColor Green
} else {
    Write-Host "⚠️  Model changes detected - may need new migration" -ForegroundColor Yellow
}

# List all migrations
Write-Host ""
Write-Host "📚 Available migrations:" -ForegroundColor Cyan
dotnet ef migrations list `
    --startup-project $StartupProject `
    --project $MigrationsProject `
    --context $Context `
    --no-build

# Apply migrations
Write-Host ""
Write-Host "🚀 Applying migrations to database..." -ForegroundColor Yellow
dotnet ef database update `
    --startup-project $StartupProject `
    --project $MigrationsProject `
    --context $Context `
    --verbose

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ All migrations applied successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Database is ready for use" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "❌ Migration failed" -ForegroundColor Red
    Write-Host "Check the error messages above for details" -ForegroundColor Yellow
    exit 1
}
