$ErrorActionPreference = "Stop"

Write-Host "🔨 ChurchApp Database Migration Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$StartupProject = "ChurchApp.API/ChurchApp.API/ChurchApp.API.csproj"
$MigrationsProject = "ChurchApp.Application/ChurchApp.Application/ChurchApp.Application.csproj"
$Context = "ChurchApp.Application.Infrastructure.ChurchAppDbContext"
$PostgresImage = if ($env:POSTGRES_IMAGE) { $env:POSTGRES_IMAGE } else { "postgres:16" }
$PostgresContainerName = if ($env:POSTGRES_CONTAINER_NAME) { $env:POSTGRES_CONTAINER_NAME } else { "churchapp-postgres" }
$PostgresVolumeName = if ($env:POSTGRES_VOLUME_NAME) { $env:POSTGRES_VOLUME_NAME } else { "churchapp-postgres-data" }
$PostgresUser = if ($env:POSTGRES_USER) { $env:POSTGRES_USER } else { "churchapp" }
$PostgresPassword = if ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { "churchapp" }
$PostgresDatabase = if ($env:POSTGRES_DATABASE) { $env:POSTGRES_DATABASE } else { "churchapp" }

Write-Host "📡 Checking Docker and EF Core tools..." -ForegroundColor Yellow

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Docker is not installed" -ForegroundColor Red
    exit 1
}

if (-not (docker info *> $null)) {
    if ($IsMacOS) {
        Write-Host "🐳 Starting Docker Desktop..." -ForegroundColor Yellow
        Start-Process -FilePath "open" -ArgumentList "-a Docker" -ErrorAction SilentlyContinue | Out-Null
    }

    for ($i = 0; $i -lt 60; $i++) {
        if (docker info *> $null) {
            break
        }
        Start-Sleep -Seconds 1
    }
}

if (-not (docker info *> $null)) {
    Write-Host "❌ Docker daemon is not running" -ForegroundColor Red
    Write-Host "Start Docker and rerun this script." -ForegroundColor Yellow
    exit 1
}

try {
    dotnet ef --version | Out-Null
} catch {
    Write-Host "❌ EF Core tools not installed" -ForegroundColor Red
    Write-Host "Install with: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
    exit 1
}

if (docker container inspect $PostgresContainerName *> $null) {
    $isRunning = docker inspect -f '{{.State.Running}}' $PostgresContainerName
    if ($isRunning -ne "true") {
        Write-Host "▶️  Starting existing PostgreSQL container '$PostgresContainerName'..." -ForegroundColor Yellow
        docker start $PostgresContainerName | Out-Null
    } else {
        Write-Host "✅ PostgreSQL container '$PostgresContainerName' is already running" -ForegroundColor Green
    }
} else {
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
for ($i = 0; $i -lt 60; $i++) {
    if (docker exec $PostgresContainerName pg_isready -U $PostgresUser -d $PostgresDatabase *> $null) {
        break
    }
    Start-Sleep -Seconds 1
}

if (-not (docker exec $PostgresContainerName pg_isready -U $PostgresUser -d $PostgresDatabase *> $null)) {
    Write-Host "❌ PostgreSQL did not become ready in time" -ForegroundColor Red
    exit 1
}

$connectionString = "Host=127.0.0.1;Port=$hostPort;Database=$PostgresDatabase;Username=$PostgresUser;Password=$PostgresPassword"
Write-Host "🔗 Using connection: Host=127.0.0.1;Port=$hostPort;Database=$PostgresDatabase;Username=$PostgresUser;Password=***" -ForegroundColor Cyan
$env:ConnectionStrings__ChurchApp = $connectionString

$dbExists = (docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$PostgresDatabase';").Trim()
if ($dbExists -ne "1") {
    Write-Host "🛠️  Creating database '$PostgresDatabase'..." -ForegroundColor Yellow
    docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d postgres -v ON_ERROR_STOP=1 `
        -c "CREATE DATABASE `"$PostgresDatabase`";" | Out-Null
}

$hasMigrationHistory = (docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d $PostgresDatabase -tAc "SELECT to_regclass('public.`"__EFMigrationsHistory`"') IS NOT NULL;").Trim()
$publicTableCount = (docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d $PostgresDatabase -tAc "SELECT COUNT(*) FROM pg_tables WHERE schemaname='public' AND tablename <> '__EFMigrationsHistory';").Trim()
$migrationHistoryCount = "0"
if ($hasMigrationHistory -eq "t") {
    $migrationHistoryCount = (docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d $PostgresDatabase -tAc "SELECT COUNT(*) FROM `"__EFMigrationsHistory`";").Trim()
}

if (($hasMigrationHistory -eq "f" -or [int]$migrationHistoryCount -eq 0) -and [int]$publicTableCount -gt 0) {
    Write-Host "⚠️  Database migration history is missing/empty while tables exist. Recreating '$PostgresDatabase' for clean migration state..." -ForegroundColor Yellow
    docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d postgres -v ON_ERROR_STOP=1 `
        -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$PostgresDatabase' AND pid <> pg_backend_pid();" | Out-Null
    docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d postgres -v ON_ERROR_STOP=1 `
        -c "DROP DATABASE IF EXISTS `"$PostgresDatabase`";" | Out-Null
    docker exec -e "PGPASSWORD=$PostgresPassword" $PostgresContainerName psql -U $PostgresUser -d postgres -v ON_ERROR_STOP=1 `
        -c "CREATE DATABASE `"$PostgresDatabase`";" | Out-Null
}

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

Write-Host ""
Write-Host "📚 Available migrations:" -ForegroundColor Cyan
dotnet ef migrations list `
    --startup-project $StartupProject `
    --project $MigrationsProject `
    --context $Context `
    --connection $connectionString `
    --no-build

Write-Host ""
Write-Host "🚀 Applying migrations to database..." -ForegroundColor Yellow
dotnet ef database update `
    --connection $connectionString `
    --startup-project $StartupProject `
    --project $MigrationsProject `
    --context $Context `
    --verbose

Write-Host ""
Write-Host "✅ All migrations applied successfully!" -ForegroundColor Green
Write-Host "📊 Database volume '$PostgresVolumeName' now contains the current schema" -ForegroundColor Cyan
