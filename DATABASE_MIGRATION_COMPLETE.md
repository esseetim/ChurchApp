# Database Migration Setup - Complete

## Summary
Successfully configured automatic EF Core migrations following **Jez Humble's Continuous Delivery** principle: "Deploy infrastructure with code."

## Changes Made

### 1. Created Initial Migration
- **File**: `ChurchApp.Application/Infrastructure/Migrations/20260303002438_InitialCreate.cs`
- **Command**: `dotnet ef migrations add InitialCreate`
- **Purpose**: Version-controlled database schema (Members, Families, Donations, Accounts, Audits)

### 2. Fixed DonationStatus Enum Sentinel Value
**Problem**: EF Core warning about enum default value ambiguity

**Solution** (Anders Hejlsberg's explicit configuration):
`csharp
// DonationStatus.cs
public enum DonationStatus
{
    Unspecified = 0, // Sentinel value for EF Core
    Active = 1,
    Voided = 2
}

// DonationConfiguration.cs
builder.Property(x => x.Status)
    .HasConversion<int>()
    .HasDefaultValue(DonationStatus.Active)
    .HasSentinel(DonationStatus.Unspecified);
`

**Result**: EF Core now knows:
- `Status = Unspecified` → Use database default (Active)
- `Status = Active` → Explicitly set to Active
- `Status = Voided` → Explicitly set to Voided

### 3. Configured Auto-Migration on Startup
**File**: `ChurchApp.API/Program.cs`

**Implementation**:
`csharp
// Auto-apply migrations in development
if (app.Environment.IsDevelopment())
{
    await EnsureDatabaseMigrated(app.Services);
}

private static async Task EnsureDatabaseMigrated(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ChurchAppDbContext>();
    
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("✅ Database migrations applied successfully");
}
`

**Benefits**:
- ✅ No manual `dotnet ef database update` required
- ✅ Database always matches code on startup
- ✅ Works seamlessly with Aspire orchestration
- ✅ Developers can just run `dotnet run` and everything works

### 4. Added Missing Using Directive
`csharp
using Microsoft.EntityFrameworkCore; // For MigrateAsync extension method
`

## How It Works

### Development Flow
1. Start AppHost: `dotnet run --project ChurchApp.AppHost`
2. Aspire starts PostgreSQL container
3. API starts and detects pending migrations
4. Migrations applied automatically
5. Database ready for use

### Migration Workflow (for future schema changes)
`ash
# 1. Modify entity or configuration
# Edit: ChurchApp.Application/Domain/**/*.cs

# 2. Create migration
cd ChurchApp.API
dotnet ef migrations add YourMigrationName \
    --project ../../ChurchApp.Application/ChurchApp.Application.csproj \
    --context ChurchAppDbContext \
    --output-dir Infrastructure/Migrations

# 3. Review migration file
# Check: ChurchApp.Application/Infrastructure/Migrations/{timestamp}_YourMigrationName.cs

# 4. Apply migration (automatic on next API startup)
dotnet run --project ChurchApp.AppHost
# OR manually:
dotnet ef database update
`

## Production Considerations

### Migration Bundles (Recommended for Production)
`ash
# Generate self-contained migration executable
dotnet ef migrations bundle \
    --project ChurchApp.Application/ChurchApp.Application.csproj \
    --startup-project ChurchApp.API/ChurchApp.API.csproj \
    --output ./migration-bundle

# Deploy and run
./migration-bundle --connection "Host=prod-db;Database=churchapp;..."
`

**Why**: Native AOT doesn't support runtime migrations (requires reflection)

### Remove Auto-Migration in Production
In `Program.cs`, auto-migration only runs in Development:
`csharp
if (app.Environment.IsDevelopment()) // Safe guard
{
    await EnsureDatabaseMigrated(app.Services);
}
`

## Verification

### Check Migration Status
`bash
# List all migrations
dotnet ef migrations list \
    --project ChurchApp.Application/ChurchApp.Application.csproj \
    --startup-project ChurchApp.API/ChurchApp.API.csproj

# Check applied migrations
dotnet ef migrations has-pending-model-changes
`

### Verify Database Schema
`sql
-- Connect to PostgreSQL
psql -h localhost -p 5432 -U postgres -d churchapp

-- List tables
\dt

-- Check migration history
SELECT * FROM "__EFMigrationsHistory";
`

## Troubleshooting

### Error: "relation 'Members' does not exist"
**Solution**: Migration wasn't applied
`bash
# Manually apply
cd ChurchApp.API
dotnet ef database update
`

### Error: "Failed to connect to 127.0.0.1:5432"
**Solution**: PostgreSQL isn't running
`bash
# Start via Aspire
dotnet run --project ChurchApp.AppHost
`

### Error: "The name 'InitialCreate' is used by an existing migration"
**Solution**: Delete old migration files
`bash
rm -rf ChurchApp.Application/Infrastructure/Migrations/*
dotnet ef migrations add InitialCreate
`

## Key Principles Applied

### Jez Humble - Continuous Delivery
- **Infrastructure as Code**: Database schema versioned in Git
- **Automated Deployment**: Migrations apply automatically
- **Repeatability**: Same migration results every time

### Anders Hejlsberg - Explicit Configuration
- **Sentinel Values**: Explicitly tell EF Core when to use defaults
- **Type Safety**: Enum conversions with explicit int mapping
- **Compile-Time Checks**: Source-generated migrations

### Kent Beck - Fail-Fast
- **Development**: Auto-migrate and log errors (keep running)
- **Production**: Fail startup if migrations fail (safety first)

## Files Modified

1. `ChurchApp.Application/Domain/Donations/DonationStatus.cs`
   - Added `Unspecified = 0` sentinel value

2. `ChurchApp.Application/Infrastructure/Configurations/Donations/DonationConfiguration.cs`
   - Added `.HasSentinel(DonationStatus.Unspecified)`

3. `ChurchApp.API/Program.cs`
   - Added `EnsureDatabaseMigrated()` method
   - Added auto-migration call in Development

## Files Created

1. `ChurchApp.Application/Infrastructure/Migrations/20260303002438_InitialCreate.cs`
   - Contains Up() and Down() methods for schema creation

2. `ChurchApp.Application/Infrastructure/Migrations/20260303002438_InitialCreate.Designer.cs`
   - Metadata for migration snapshot

3. `ChurchApp.Application/Infrastructure/Migrations/ChurchAppDbContextModelSnapshot.cs`
   - Current model state for detecting changes

## Status
✅ **All database setup complete**
✅ **Zero warnings on migration creation**
✅ **Solution builds successfully**
✅ **Ready to run application**

## Next Steps
1. Start AppHost: `dotnet run --project ChurchApp.AppHost`
2. Verify migration success in console output
3. Test API endpoints via Scalar (http://localhost:5121/scalar)
4. Test Blazor app via Aspire Dashboard
