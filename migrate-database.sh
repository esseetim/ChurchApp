#!/bin/bash

# ==============================================================================
# ChurchApp Database Migration Tool
# ==============================================================================
# This script ensures the Docker database is running, creates the database if 
# needed, optionally generates a new EF Core migration, and applies all 
# pending migrations to the database.
# ==============================================================================

# 'set -euo pipefail' is a bash safety feature:
# -e: Exit immediately if any command returns a non-zero status (fails).
# -u: Exit if an uninitialized variable is used.
# -o pipefail: If a command within a pipeline (like A | B) fails, the whole pipeline fails.
set -euo pipefail

# Initialize an empty variable for the migration name
MIGRATION_NAME=""

# Loop through all arguments passed to the script to check for a migration name.
# $# is the number of arguments. $1 is the first argument, $2 is the second.
while [[ "$#" -gt 0 ]]; do
    case $1 in
        -m|--migration-name|-MigrationName) 
            MIGRATION_NAME="$2"
            shift # Move past the value
            ;;
    esac
    shift # Move to the next argument
done

echo -e "\033[0;36m🔨 ChurchApp Database Migration Tool\033[0m"
echo -e "\033[0;36m========================================\033[0m\n"

# --- Configuration Variables ---
STARTUP_PROJECT="ChurchApp.API/ChurchApp.API/ChurchApp.API.csproj"
MIGRATIONS_PROJECT="ChurchApp.Application/ChurchApp.Application/ChurchApp.Application.csproj"
CONTEXT="ChurchApp.Application.Infrastructure.ChurchAppDbContext"

# Container Configuration using parameter expansion. 
# "${VAR:-default}" means: Use $VAR if it exists, otherwise use "default".
POSTGRES_IMAGE="${POSTGRES_IMAGE:-postgres:16}"
POSTGRES_CONTAINER_NAME="${POSTGRES_CONTAINER_NAME:-churchapp-apphost-postgres}"
POSTGRES_VOLUME_NAME="${POSTGRES_VOLUME_NAME:-churchapp-postgres-data}"
POSTGRES_USER="${POSTGRES_USER:-churchapp}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-churchapp}"
POSTGRES_DATABASE="${POSTGRES_DATABASE:-churchapp}"

# --- ANSI Color Codes for terminal output ---
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${YELLOW}📡 Checking Docker and EF Core tools...${NC}"

# Check if docker is installed. 'command -v' checks for the executable.
# '>/dev/null 2>&1' hides both standard output and error output so it doesn't clutter the screen.
if ! command -v docker >/dev/null 2>&1; then
    echo -e "${RED}❌ Docker is not installed${NC}"
    exit 1
fi

# Check if the docker daemon is running.
if ! docker info >/dev/null 2>&1; then
    # If on macOS, try to start Docker Desktop automatically
    if [ "$(uname -s)" = "Darwin" ]; then
        echo -e "${YELLOW}🐳 Starting Docker Desktop...${NC}"
        # '|| true' prevents the script from crashing if 'open' fails, due to 'set -e'
        open -a Docker >/dev/null 2>&1 || true 
    fi

    # Loop up to 60 times, waiting 1 second each time, for Docker to start
    for _ in {1..60}; do
        if docker info >/dev/null 2>&1; then
            break # Exit the loop once docker is running
        fi
        sleep 1
    done
fi

# Final check if docker is running after the waiting period
if ! docker info >/dev/null 2>&1; then
    echo -e "${RED}❌ Docker daemon is not running${NC}"
    echo "Start Docker and rerun this script."
    exit 1
fi

# Check if EF Core CLI tools are installed
if ! dotnet ef --version >/dev/null 2>&1; then
    echo -e "${RED}❌ EF Core tools not installed${NC}"
    echo "Install with: dotnet tool install --global dotnet-ef"
    exit 1
fi

# --- Docker Container Management ---
# Check if the specific container exists
if docker container inspect "$POSTGRES_CONTAINER_NAME" >/dev/null 2>&1; then
    # Check if the container is currently running
    if [ "$(docker inspect -f '{{.State.Running}}' "$POSTGRES_CONTAINER_NAME")" != "true" ]; then
        echo -e "${YELLOW}▶️  Starting existing PostgreSQL container '$POSTGRES_CONTAINER_NAME'...${NC}"
        docker start "$POSTGRES_CONTAINER_NAME" >/dev/null
    else
        echo -e "${GREEN}✅ PostgreSQL container '$POSTGRES_CONTAINER_NAME' is already running${NC}"
    fi
else
    # Container doesn't exist, so we run a new one
    echo -e "${YELLOW}🚀 Starting PostgreSQL container '$POSTGRES_CONTAINER_NAME'...${NC}"
    docker run -d \
        --name "$POSTGRES_CONTAINER_NAME" \
        -e "POSTGRES_USER=$POSTGRES_USER" \
        -e "POSTGRES_PASSWORD=$POSTGRES_PASSWORD" \
        -e "POSTGRES_DB=$POSTGRES_DATABASE" \
        -v "$POSTGRES_VOLUME_NAME:/var/lib/postgresql/data" \
        -p 0:5432 \
        "$POSTGRES_IMAGE" >/dev/null
fi

# Extract the dynamic host port mapped to 5432 using 'awk' to split the text
HOST_PORT="$(docker port "$POSTGRES_CONTAINER_NAME" 5432/tcp | head -n1 | awk -F: '{print $NF}')"
if [[ -z "${HOST_PORT}" ]]; then
    echo -e "${RED}❌ Failed to resolve PostgreSQL mapped port${NC}"
    exit 1
fi

echo -e "${YELLOW}⏳ Waiting for PostgreSQL readiness...${NC}"
# Wait for postgres to accept connections
for _ in {1..60}; do
    if docker exec "$POSTGRES_CONTAINER_NAME" pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DATABASE" >/dev/null 2>&1; then
        break
    fi
    sleep 1
done

# Final check to ensure postgres is ready
if ! docker exec "$POSTGRES_CONTAINER_NAME" pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DATABASE" >/dev/null 2>&1; then
    echo -e "${RED}❌ PostgreSQL did not become ready in time${NC}"
    exit 1
fi

# Export connection string so EF Core commands use it automatically
CONNECTION_STRING="Host=127.0.0.1;Port=${HOST_PORT};Database=${POSTGRES_DATABASE};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
echo -e "${CYAN}🔗 Using connection: Host=127.0.0.1;Port=${HOST_PORT};Database=${POSTGRES_DATABASE};Username=${POSTGRES_USER};Password=***${NC}"
export ConnectionStrings__ChurchApp="$CONNECTION_STRING"

# --- Database Initialization ---
# Check if the database exists in PostgreSQL
DB_EXISTS="$(docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='${POSTGRES_DATABASE}';" | tr -d '[:space:]')"
if [[ "$DB_EXISTS" != "1" ]]; then
    echo -e "${YELLOW}🛠️  Creating database '${POSTGRES_DATABASE}'...${NC}"
    docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
        -c "CREATE DATABASE \"${POSTGRES_DATABASE}\";" >/dev/null
fi

# --- State Cleanliness Check ---
# Check if __EFMigrationsHistory table exists
HAS_MIGRATION_HISTORY="$(docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d "$POSTGRES_DATABASE" -tAc "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL;" | tr -d '[:space:]')"
# Count existing public tables
PUBLIC_TABLE_COUNT="$(docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d "$POSTGRES_DATABASE" -tAc "SELECT COUNT(*) FROM pg_tables WHERE schemaname='public' AND tablename <> '__EFMigrationsHistory';" | tr -d '[:space:]')"
MIGRATION_HISTORY_COUNT="0"

if [[ "$HAS_MIGRATION_HISTORY" == "t" ]]; then
    MIGRATION_HISTORY_COUNT="$(docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d "$POSTGRES_DATABASE" -tAc "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";" | tr -d '[:space:]')"
fi

# If tables exist but there's no migration history, we need to wipe the DB so EF Core can run cleanly
if [[ ("$HAS_MIGRATION_HISTORY" == "f" || "${MIGRATION_HISTORY_COUNT:-0}" -eq 0) && "${PUBLIC_TABLE_COUNT:-0}" -gt 0 ]]; then
    echo -e "${YELLOW}⚠️  Database migration history is missing/empty while tables exist. Recreating '${POSTGRES_DATABASE}' for clean migration state...${NC}"
    # Terminate active connections before dropping
    docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
        -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '${POSTGRES_DATABASE}' AND pid <> pg_backend_pid();" >/dev/null
    docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
        -c "DROP DATABASE IF EXISTS \"${POSTGRES_DATABASE}\";" >/dev/null
    docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$POSTGRES_CONTAINER_NAME" psql -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
        -c "CREATE DATABASE \"${POSTGRES_DATABASE}\";" >/dev/null
fi

echo ""

# --- 1. Generate New Migration ---
# If the user passed a migration name, add it via EF CLI
if [[ -n "$MIGRATION_NAME" ]]; then
    echo -e "${YELLOW}➕ Generating new migration: '$MIGRATION_NAME'...${NC}"
    
    # We use 'if ! command' to catch errors natively in bash
    if ! dotnet ef migrations add "$MIGRATION_NAME" \
        --startup-project "$STARTUP_PROJECT" \
        --project "$MIGRATIONS_PROJECT" \
        --context "$CONTEXT"; then
        
        echo -e "${RED}❌ Failed to generate migration '$MIGRATION_NAME'. Check errors above.${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ Migration '$MIGRATION_NAME' created successfully.${NC}\n"
fi

# --- 2. Check Pending Model Changes ---
echo -e "${YELLOW}📋 Checking if model is in sync...${NC}"
# Run the check, capturing output. '|| true' stops 'set -e' from killing the script if there are pending changes (which returns a non-zero exit code).
PENDING="$(dotnet ef migrations has-pending-model-changes \
    --startup-project "$STARTUP_PROJECT" \
    --project "$MIGRATIONS_PROJECT" \
    --context "$CONTEXT" 2>&1 || true)"

# 'grep -q' searches for the string silently
if echo "$PENDING" | grep -q "No changes have been made"; then
    echo -e "${GREEN}✅ Model is fully in sync with migrations.${NC}"
else
    echo -e "${RED}⚠️  Model changes detected! You may need to run this script again with '-m <Name>' to capture them before updating.${NC}"
fi

echo ""
echo -e "${CYAN}📚 Available migrations:${NC}"
dotnet ef migrations list \
    --startup-project "$STARTUP_PROJECT" \
    --project "$MIGRATIONS_PROJECT" \
    --context "$CONTEXT" \
    --connection "$CONNECTION_STRING" \
    --no-build

# --- 3. Apply Migrations ---
echo ""
echo -e "${YELLOW}🚀 Applying migrations to database...${NC}"

# If this command fails, the 'if !' block catches it, prints the red text, and exits.
if ! dotnet ef database update \
    --connection "$CONNECTION_STRING" \
    --startup-project "$STARTUP_PROJECT" \
    --project "$MIGRATIONS_PROJECT" \
    --context "$CONTEXT" \
    --verbose; then
    
    echo -e "${RED}❌ Failed to apply migrations to the database. See the error output above.${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}✅ All migrations applied successfully!${NC}"
echo -e "${CYAN}📊 Database volume '$POSTGRES_VOLUME_NAME' is up to date.${NC}"

# --- 4. Migration.Sql script for Prod ---
echo ""
echo -e "${YELLOW}📄 Generating idempotent SQL script for AOT publish...${NC}"

if ! dotnet ef migrations script \
    --idempotent \
    --startup-project "$STARTUP_PROJECT" \
    --project "$MIGRATIONS_PROJECT" \
    --context "$CONTEXT" \
    --output "$STARTUP_PROJECT/../migrations.sql" \
    --no-build; then
    
    echo -e "${RED}❌ Failed to generate migrations.sql${NC}"
    exit 1
fi

echo -e "${GREEN}✅ migrations.sql generated successfully!${NC}"