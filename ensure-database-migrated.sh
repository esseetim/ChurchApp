#!/bin/bash
# Database Migration Script (Jez Humble's Idempotent Deployment)
# Ensures database schema matches code, can run multiple times safely

set -euo pipefail

echo "🔨 ChurchApp Database Migration Script"
echo "========================================"
echo ""

# Configuration
STARTUP_PROJECT="ChurchApp.API/ChurchApp.API/ChurchApp.API.csproj"
MIGRATIONS_PROJECT="ChurchApp.Application/ChurchApp.Application/ChurchApp.Application.csproj"
CONTEXT="ChurchApp.Application.Infrastructure.ChurchAppDbContext"
POSTGRES_IMAGE="${POSTGRES_IMAGE:-postgres:16}"
POSTGRES_CONTAINER_NAME="${POSTGRES_CONTAINER_NAME:-churchapp-postgres}"
POSTGRES_VOLUME_NAME="${POSTGRES_VOLUME_NAME:-churchapp-postgres-data}"
POSTGRES_USER="${POSTGRES_USER:-churchapp}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-churchapp}"
POSTGRES_DATABASE="${POSTGRES_DATABASE:-churchapp}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "📡 Checking Docker and EF Core tools..."
if ! command -v docker >/dev/null 2>&1; then
    echo -e "${RED}❌ Docker is not installed${NC}"
    exit 1
fi

if ! docker info >/dev/null 2>&1; then
    if [ "$(uname -s)" = "Darwin" ]; then
        echo "🐳 Starting Docker Desktop..."
        open -a Docker >/dev/null 2>&1 || true
    fi

    for _ in {1..60}; do
        if docker info >/dev/null 2>&1; then
            break
        fi
        sleep 1
    done
fi

if ! docker info >/dev/null 2>&1; then
    echo -e "${RED}❌ Docker daemon is not running${NC}"
    echo "Start Docker and rerun this script."
    exit 1
fi

if ! dotnet ef --version >/dev/null 2>&1; then
    echo -e "${RED}❌ EF Core tools not installed${NC}"
    echo "Install with: dotnet tool install --global dotnet-ef"
    exit 1
fi

if docker container inspect "$POSTGRES_CONTAINER_NAME" >/dev/null 2>&1; then
    if [ "$(docker inspect -f '{{.State.Running}}' "$POSTGRES_CONTAINER_NAME")" != "true" ]; then
        echo "▶️  Starting existing PostgreSQL container '$POSTGRES_CONTAINER_NAME'..."
        docker start "$POSTGRES_CONTAINER_NAME" >/dev/null
    else
        echo "✅ PostgreSQL container '$POSTGRES_CONTAINER_NAME' is already running"
    fi
else
    echo "🚀 Starting PostgreSQL container '$POSTGRES_CONTAINER_NAME'..."
    docker run -d \
        --name "$POSTGRES_CONTAINER_NAME" \
        -e "POSTGRES_USER=$POSTGRES_USER" \
        -e "POSTGRES_PASSWORD=$POSTGRES_PASSWORD" \
        -e "POSTGRES_DB=$POSTGRES_DATABASE" \
        -v "$POSTGRES_VOLUME_NAME:/var/lib/postgresql/data" \
        -p 0:5432 \
        "$POSTGRES_IMAGE" >/dev/null
fi

HOST_PORT="$(docker port "$POSTGRES_CONTAINER_NAME" 5432/tcp | head -n1 | awk -F: '{print $NF}')"
if [[ -z "${HOST_PORT}" ]]; then
    echo -e "${RED}❌ Failed to resolve PostgreSQL mapped port${NC}"
    exit 1
fi

echo "⏳ Waiting for PostgreSQL readiness..."
for _ in {1..60}; do
    if docker exec "$POSTGRES_CONTAINER_NAME" pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DATABASE" >/dev/null 2>&1; then
        break
    fi
    sleep 1
done

if ! docker exec "$POSTGRES_CONTAINER_NAME" pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DATABASE" >/dev/null 2>&1; then
    echo -e "${RED}❌ PostgreSQL did not become ready in time${NC}"
    exit 1
fi

CONNECTION_STRING="Host=127.0.0.1;Port=${HOST_PORT};Database=${POSTGRES_DATABASE};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
echo "🔗 Using connection: Host=127.0.0.1;Port=${HOST_PORT};Database=${POSTGRES_DATABASE};Username=${POSTGRES_USER};Password=***"
export ConnectionStrings__ChurchApp="$CONNECTION_STRING"

DB_EXISTS="$(docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='${POSTGRES_DATABASE}';" | tr -d '[:space:]')"
if [[ "$DB_EXISTS" != "1" ]]; then
    echo "🛠️  Creating database '${POSTGRES_DATABASE}'..."
    docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
        -c "CREATE DATABASE \"${POSTGRES_DATABASE}\";" >/dev/null
fi

HAS_MIGRATION_HISTORY="$(docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d "$POSTGRES_DATABASE" -tAc "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL;" | tr -d '[:space:]')"
PUBLIC_TABLE_COUNT="$(docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d "$POSTGRES_DATABASE" -tAc "SELECT COUNT(*) FROM pg_tables WHERE schemaname='public' AND tablename <> '__EFMigrationsHistory';" | tr -d '[:space:]')"
MIGRATION_HISTORY_COUNT="0"
if [[ "$HAS_MIGRATION_HISTORY" == "t" ]]; then
    MIGRATION_HISTORY_COUNT="$(docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d "$POSTGRES_DATABASE" -tAc "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";" | tr -d '[:space:]')"
fi

if [[ ("$HAS_MIGRATION_HISTORY" == "f" || "${MIGRATION_HISTORY_COUNT:-0}" -eq 0) && "${PUBLIC_TABLE_COUNT:-0}" -gt 0 ]]; then
    echo -e "${YELLOW}⚠️  Database migration history is missing/empty while tables exist. Recreating '${POSTGRES_DATABASE}' for clean migration state...${NC}"
    docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
        -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '${POSTGRES_DATABASE}' AND pid <> pg_backend_pid();" >/dev/null
    docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
        -c "DROP DATABASE IF EXISTS \"${POSTGRES_DATABASE}\";" >/dev/null
    docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
        -c "CREATE DATABASE \"${POSTGRES_DATABASE}\";" >/dev/null
fi

# Show pending migrations
echo ""
echo "📋 Checking migration status..."
PENDING="$(dotnet ef migrations has-pending-model-changes \
    --startup-project "$STARTUP_PROJECT" \
    --project "$MIGRATIONS_PROJECT" \
    --context "$CONTEXT" 2>&1 || true)"

if echo "$PENDING" | grep -q "No changes have been made"; then
    echo -e "${GREEN}✅ Database schema is up-to-date${NC}"
else
    echo -e "${YELLOW}⚠️  Model changes detected - may need new migration${NC}"
fi

# List all migrations
echo ""
echo "📚 Available migrations:"
dotnet ef migrations list \
    --startup-project "$STARTUP_PROJECT" \
    --project "$MIGRATIONS_PROJECT" \
    --context "$CONTEXT" \
    --connection "$CONNECTION_STRING" \
    --no-build

# Apply migrations
echo ""
echo "🚀 Applying migrations to database..."
dotnet ef database update \
    --connection "$CONNECTION_STRING" \
    --startup-project "$STARTUP_PROJECT" \
    --project "$MIGRATIONS_PROJECT" \
    --context "$CONTEXT" \
    --verbose

echo ""
echo -e "${GREEN}✅ All migrations applied successfully!${NC}"
echo "📊 Database volume '$POSTGRES_VOLUME_NAME' now contains the current schema"
