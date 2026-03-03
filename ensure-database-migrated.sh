#!/bin/bash
# Database Migration Script (Jez Humble's Idempotent Deployment)
# Ensures database schema matches code, can run multiple times safely

set -e  # Exit on error

echo "🔨 ChurchApp Database Migration Script"
echo "========================================"
echo ""

# Configuration
STARTUP_PROJECT="ChurchApp.API/ChurchApp.API/ChurchApp.API.csproj"
MIGRATIONS_PROJECT="ChurchApp.Application/ChurchApp.Application/ChurchApp.Application.csproj"
CONTEXT="ChurchApp.Application.Infrastructure.ChurchAppDbContext"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if database is accessible
echo "📡 Checking database connectivity..."
if ! dotnet ef database --help > /dev/null 2>&1; then
    echo -e "${RED}❌ EF Core tools not installed${NC}"
    echo "Install with: dotnet tool install --global dotnet-ef"
    exit 1
fi

# Show pending migrations
echo ""
echo "📋 Checking migration status..."
PENDING=$(dotnet ef migrations has-pending-model-changes \
    --startup-project "$STARTUP_PROJECT" \
    --project "$MIGRATIONS_PROJECT" \
    --context "$CONTEXT" 2>&1 || echo "")

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
    --no-build

# Apply migrations
echo ""
echo "🚀 Applying migrations to database..."
dotnet ef database update \
    --startup-project "$STARTUP_PROJECT" \
    --project "$MIGRATIONS_PROJECT" \
    --context "$CONTEXT" \
    --verbose

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✅ All migrations applied successfully!${NC}"
    echo ""
    echo "📊 Database is ready for use"
else
    echo ""
    echo -e "${RED}❌ Migration failed${NC}"
    echo "Check the error messages above for details"
    exit 1
fi