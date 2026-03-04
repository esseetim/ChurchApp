<#
.SYNOPSIS
    ChurchApp Database Migration Tool (PowerShell Variant)

.DESCRIPTION
    This script ensures the Docker database is running, creates the database if 
    needed, optionally generates a new EF Core migration, applies all 
    pending migrations to the database, and finally outputs an idempotent 
    SQL script for AOT publish.

.PARAMETER MigrationName
    (Optional) The name of a new migration to generate before updating the database.
#>

param (
    [Alias("m", "migration-name")]
    [string]$MigrationName = ""
)

# Tell PowerShell to stop script execution if a native PowerShell cmdlet fails.
# Note: This does NOT automatically stop execution if external tools (like docker or dotnet) fail.
# For external tools, we must manually check the $LASTEXITCODE variable.
$ErrorActionPreference = "Stop"

Write-Host "🔨 ChurchApp Database Migration Tool" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# --- Configuration Variables ---
$StartupProject = "ChurchApp.API/ChurchApp.API/ChurchApp.API.csproj"
$MigrationsProject = "ChurchApp.Application/ChurchApp.Application/ChurchApp.Application.csproj"
$Context = "ChurchApp.Application.Infrastructure.ChurchAppDbContext"

# Container Configuration using environment variables with fallbacks
$PostgresImage = if ($env:POSTGRES_IMAGE) { $env:POSTGRES_IMAGE } else { "postgres:16" }
$PostgresContainerName = if ($env:POSTGRES_CONTAINER_NAME) { $env:POSTGRES_CONTAINER_NAME } else { "churchapp-apphost-postgres" }
$PostgresVolumeName = if ($env:POSTGRES_VOLUME_NAME) { $env:POSTGRES_VOLUME_NAME } else { "churchapp-postgres-data" }
$PostgresUser = if ($env:POSTGRES_USER) { $env:POSTGRES_USER } else { "churchapp" }
$PostgresPassword = if ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { "churchapp" }
$PostgresDatabase = if ($env:POSTGRES_DATABASE) { $env:POSTGRES_DATABASE } else { "churchapp" }

Write-Host "📡 Checking Docker and EF Core tools..." -ForegroundColor Yellow

# Check if Docker is installed
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Docker is not installed" -ForegroundColor Red
    exit 1
}

# --- Docker Daemon Check ---
# '*> $null' hides the output. We then check if the command succeeded using $LASTEXITCODE
docker info *> $null
if ($LASTEXITCODE -ne 0) {
    # Attempt to auto-start Docker Desktop on Mac
    if ($IsMacOS) {
        Write-Host "🐳 Starting Docker Desktop..." -ForegroundColor Yellow
        Start-Process -FilePath "open" -ArgumentList "-a Docker" -ErrorAction SilentlyContinue | Out-Null
    }

    # Loop up to 60 times waiting for Docker daemon to become available
    for ($i = 0; $i -lt 60; $i++) {
        docker info *> $null
        if ($LASTEXITCODE -eq 0) {
            break
        }
        Start-Sleep -Seconds 1
    }
}

# Final check if Docker is running
docker info *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker daemon is not running" -ForegroundColor Red
    Write-Host "Start Docker and rerun this script." -ForegroundColor Yellow
    exit 1
}

# Check if EF Core CLI tools are installed
dotnet ef --version *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ EF Core tools not installed" -ForegroundColor Red
    Write-Host "Install with: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
    exit 1
}

# --- Docker Container Management ---
# Check if our specific database container exists
docker container inspect $PostgresContainerName *> $null
if ($LASTEXITCODE -eq 0) {
    # It exists. Check if it's currently running.
    $isRunning = (docker inspect -f '{{.State.Running}}' $PostgresContainerName).Trim()
    if ($isRunning -ne "true") {
        Write-Host "▶️  Starting existing PostgreSQL container '$PostgresContainerName'..." -ForegroundColor Yellow
        docker start $PostgresContainerName | Out-Null
    } else {
        Write-Host "✅ PostgreSQL container '$PostgresContainerName' is already running" -ForegroundColor Green
    }
} else {
    # Container does not exist, so spin up a new one
    Write-Host "🚀 Starting PostgreSQL container '$PostgresContainerName'..." -ForegroundColor Yellow
    docker run -d `
        --name $PostgresContainerName `
        -e "POSTGRES_USER=$PostgresUser" `
        -e "POSTGRES_PASSWORD=$PostgresPassword" `
        -e "POSTGRES_DB=$PostgresDatabase" `
        -v "${PostgresVolumeName}:/var/lib/postgresql/data" `
        -p "0:5432" `
        $PostgresImage | Out-Null
}

# Extract dynamic port mapped to 5432
$portLine = docker port $PostgresContainerName 5432/tcp | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($portLine)) {
    Write-Host "❌ Failed to resolve PostgreSQL mapped port" -ForegroundColor Red
    exit 1
}

$portParts = $portLine.Split(":")
$hostPort = $portParts[$portParts.Length - 1].Trim()
if ([string]::IsNullOrWhiteSpace($hostPort)) {
    Write-Host "❌ Failed to parse PostgreSQL mapped port from '$portLine'" -ForegroundColor Red
    exit 1
}

Write-Host "⏳ Waiting for PostgreSQL readiness..." -ForegroundColor Yellow
# Wait for PostgreSQL inside the container to accept connections
for ($i = 0; $i -lt 60; $i++) {
    docker exec $PostgresContainerName pg_isready -U $PostgresUser -d $PostgresDatabase *> $null
    if ($LASTEXITCODE -eq 0) {
        break
    }
    Start-Sleep -Seconds 1
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ PostgreSQL did not become ready in time" -ForegroundColor Red
    exit 1
}

# Export the connection string to the environment so EF Core automatically picks it up
$connectionString = "Host=127.0.0.1;Port=$hostPort;Database=$PostgresDatabase;Username=$PostgresUser;Password=$PostgresPassword"
Write-Host "🔗 Using connection: Host=127.0.0.1;Port=$hostPort;Database=$PostgresDatabase;Username=$PostgresUser;Password=***" -ForegroundColor Cyan
$env:ConnectionStrings__ChurchApp = $connectionString

# --- Database Initialization ---
# Check if the database schema exists
$dbExists = (docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$PostgresDatabase';" 2>$null).Trim()
if ($dbExists -ne "1") {
    Write-Host "🛠️  Creating database '$PostgresDatabase'..." -ForegroundColor Yellow
    $sqlCreateDb = "CREATE DATABASE $PostgresDatabase;"
    docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d postgres -v ON_ERROR_STOP=1 -c $sqlCreateDb | Out-Null
}

# --- State Cleanliness Check ---
$sqlCheckHistory = "SELECT to_regclass('public.\`"__EFMigrationsHistory\`"') IS NOT NULL;"
$hasMigrationHistory = (docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d $PostgresDatabase -tAc $sqlCheckHistory 2>$null).Trim()

$sqlTableCount = "SELECT COUNT(*) FROM pg_tables WHERE schemaname='public' AND tablename != '__EFMigrationsHistory';"
$publicTableCount = (docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d $PostgresDatabase -tAc $sqlTableCount 2>$null).Trim()

$migrationHistoryCount = "0"
if ($hasMigrationHistory -eq "t") {
    $sqlCountHistory = "SELECT COUNT(*) FROM \`"__EFMigrationsHistory\`";"
    $migrationHistoryCount = (docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d $PostgresDatabase -tAc $sqlCountHistory 2>$null).Trim()
}

# Wipe database if there are tables but no migration history
if (($hasMigrationHistory -eq "f" -or [int]$migrationHistoryCount -eq 0) -and [int]$publicTableCount -gt 0) {
    Write-Host "⚠️  Database migration history is missing/empty while tables exist. Recreating '$PostgresDatabase' for clean migration state..." -ForegroundColor Yellow
    $sqlTerminate = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$PostgresDatabase' AND pid != pg_backend_pid();"
    docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d postgres -v ON_ERROR_STOP=1 -c $sqlTerminate | Out-Null

    $sqlDrop = "DROP DATABASE IF EXISTS $PostgresDatabase;"
    docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d postgres -v ON_ERROR_STOP=1 -c $sqlDrop | Out-Null

    $sqlCreate = "CREATE DATABASE $PostgresDatabase;"
    docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d postgres -v ON_ERROR_STOP=1 -c $sqlCreate | Out-Null
}

Write-Host ""

# --- 1. Generate New Migration ---
if (-not [string]::IsNullOrWhiteSpace($MigrationName)) {
    Write-Host "➕ Generating new migration: '$MigrationName'..." -ForegroundColor Yellow
    dotnet ef migrations add $MigrationName `
        --startup-project $StartupProject `
        --project $MigrationsProject `
        --context $Context

    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to generate migration '$MigrationName'. Check errors above." -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Migration '$MigrationName' created successfully." -ForegroundColor Green
    Write-Host ""
}

# --- 2. Check Pending Model Changes ---
Write-Host "📋 Checking if model is in sync..." -ForegroundColor Yellow
$pending = dotnet ef migrations has-pending-model-changes `
    --startup-project $StartupProject `
    --project $MigrationsProject `
    --context $Context 2>&1

if ($pending -match "No changes have been made") {
    Write-Host "✅ Model is fully in sync with migrations." -ForegroundColor Green
} else {
    Write-Host "⚠️  Model changes detected! You may need to run this script again with '-MigrationName <Name>' to capture them before updating." -ForegroundColor Red
}

Write-Host ""
Write-Host "📚 Available migrations:" -ForegroundColor Cyan
dotnet ef migrations list `
    --startup-project $StartupProject `
    --project $MigrationsProject `
    --context $Context `
    --connection $connectionString `
    --no-build

# --- 3. Apply Migrations ---
Write-Host ""
Write-Host "🚀 Applying migrations to database..." -ForegroundColor Yellow
dotnet ef database update `
    --connection $connectionString `
    --startup-project $StartupProject `
    --project $MigrationsProject `
    --context $Context `
    --verbose

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to apply migrations to the database. See the error output above." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ All migrations applied successfully!" -ForegroundColor Green
Write-Host "📊 Database volume '$PostgresVolumeName' is up to date." -ForegroundColor Cyan

# --- 4. Migration.Sql script for Prod ---
Write-Host ""
Write-Host "📄 Generating idempotent SQL script for AOT publish..." -ForegroundColor Yellow

# Calculate the directory where the StartupProject lives (ChurchApp.API/ChurchApp.API)
$ApiDir = Split-Path -Parent $StartupProject
$SqlOutputPath = Join-Path $ApiDir "migrations.sql"

dotnet ef migrations script `
    --idempotent `
    --startup-project $StartupProject `
    --project $MigrationsProject `
    --context $Context `
    --output $SqlOutputPath `
    --no-build

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to generate migrations.sql" -ForegroundColor Red
    exit 1
}

Write-Host "✅ migrations.sql generated successfully!" -ForegroundColor Green