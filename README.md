# ChurchApp

A .NET 10 AOT-enabled Web API for church management.

## Project Structure

- **ChurchApp.API** - Web API project with FastEndpoints
- **ChurchApp.Application** - Application layer with business logic

## Technology Stack

### Core Framework
- **.NET 10.0** with AOT (Ahead-of-Time) compilation
- **C# latest** with nullable reference types enabled

### Key Packages
- **Entity Framework Core 10.0.1** - Data access layer
- **SQLite 10.0.1** - Local database provider
- **FastEndpoints 5.35.0** - High-performance endpoint routing
- **ErrorOr 2.0.1** - Functional error handling
- **Microsoft.Extensions.DependencyInjection 10.0.1** - Dependency injection

## Configuration Files

### Directory.Build.props
Centralized build properties for all projects:
- Target framework: net10.0
- AOT compilation enabled
- Trimming and analyzers enabled
- Nullable reference types
- Implicit usings

### Directory.Packages.props
Central package version management:
- All package versions defined in one place
- Consistent versioning across projects
- Transitive dependency pinning enabled

## Getting Started

### Prerequisites
- .NET 10.0 SDK or later
- IDE with .NET support (Rider, Visual Studio, VS Code)

### Build the Solution
```bash
dotnet restore
./build-api.sh
```

### Run the API
```bash
./run-api.sh
```

### Publish AOT
```bash
dotnet publish -c Release
```

## AOT Considerations

This project is configured for Native AOT compilation, which means:
- Faster startup time
- Lower memory footprint
- No JIT compilation required at runtime
- Some reflection-based features are limited
- JSON serialization requires source generation

## EF Core Trimming Guidance

EF Core has limited trimming/NativeAOT compatibility. This project uses a compiled model generation step to align with EF guidance.

Before build/run, use:

```bash
./ensure-ef-compiled-model.sh
```

Or use the enforced wrappers:

```bash
./build-api.sh
./run-api.sh
```

## Database

The application uses SQLite for local data storage. Database migrations can be managed using:

```bash
# Add a migration
dotnet ef migrations add <MigrationName> --project ChurchApp.Application/ChurchApp.Application

# Update database
dotnet ef database update --project ChurchApp.API/ChurchApp.API
```

## Development Guidelines

- Use FastEndpoints for creating API endpoints
- Use ErrorOr for functional error handling
- Follow the clean architecture pattern
- Keep domain logic in the Application project
- Ensure code is AOT-compatible (avoid dynamic reflection)

## Notes

- InvariantGlobalization is enabled for smaller binary size
- JSON serialization does not use reflection by default
- Enable trim analyzers to catch AOT compatibility issues early

