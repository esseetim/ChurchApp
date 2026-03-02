#!/usr/bin/env pwsh

Write-Host "🔍 Verifying Blazor Migration..." -ForegroundColor Cyan
Write-Host ""

# Check if Blazor project exists
Write-Host "✓ Checking Blazor project..." -ForegroundColor Yellow
if (Test-Path "D:\ChurchApp\ChurchApp.Web.Blazor\ChurchApp.Web.Blazor.csproj") {
    Write-Host "  ✅ Blazor project found" -ForegroundColor Green
} else {
    Write-Host "  ❌ Blazor project not found" -ForegroundColor Red
    exit 1
}

# Build solution
Write-Host ""
Write-Host "✓ Building solution..." -ForegroundColor Yellow
$buildResult = dotnet build "D:\ChurchApp\ChurchApp.slnx" --no-restore 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ Solution builds successfully" -ForegroundColor Green
} else {
    Write-Host "  ❌ Build failed" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}

# Check key files
Write-Host ""
Write-Host "✓ Checking key files..." -ForegroundColor Yellow

$keyFiles = @(
    "ChurchApp.Web.Blazor\Models\Enums.cs",
    "ChurchApp.Web.Blazor\Models\DonationModels.cs",
    "ChurchApp.Web.Blazor\Services\IDonationService.cs",
    "ChurchApp.Web.Blazor\Services\Implementations\DonationService.cs",
    "ChurchApp.Web.Blazor\Pages\DonationDesk.razor",
    "ChurchApp.Web.Blazor\Pages\Ledger.razor",
    "ChurchApp.Web.Blazor\Pages\Summaries.razor",
    "ChurchApp.Web.Blazor\Pages\Reports.razor",
    "ChurchApp.Web.Blazor\Components\Shared\QuickCreateMember.razor",
    "ChurchApp.Web.Blazor\Layout\MainLayout.razor"
)

$allFilesExist = $true
foreach ($file in $keyFiles) {
    $fullPath = Join-Path "D:\ChurchApp" $file
    if (Test-Path $fullPath) {
        Write-Host "  ✅ $file" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $file missing" -ForegroundColor Red
        $allFilesExist = $false
    }
}

if (-not $allFilesExist) {
    exit 1
}

# Check AppHost integration
Write-Host ""
Write-Host "✓ Checking AppHost integration..." -ForegroundColor Yellow
$appHostContent = Get-Content "D:\ChurchApp\ChurchApp.AppHost\ChurchApp.AppHost\Program.cs" -Raw
if ($appHostContent -match "ChurchApp_Web_Blazor") {
    Write-Host "  ✅ Blazor integrated into AppHost" -ForegroundColor Green
} else {
    Write-Host "  ❌ Blazor not found in AppHost" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🎉 All verification checks passed!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run AppHost: cd ChurchApp.AppHost\ChurchApp.AppHost && dotnet run"
Write-Host "  2. Open Aspire Dashboard"
Write-Host "  3. Click on 'web' endpoint to open Blazor app"
Write-Host "  4. Test all 4 pages (Donation Desk, Ledger, Summaries, Reports)"
Write-Host "  5. If everything works, delete ChurchApp.Web directory"
Write-Host ""
