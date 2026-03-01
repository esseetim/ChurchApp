# ChurchApp - Quick Reference Guide

## 🚀 Quick Start Commands

### Run the Application

```bash
cd ChurchApp.API/ChurchApp.API
dotnet run
```

### Build the Solution

```bash
dotnet build
```

### Clean and Rebuild

```bash
dotnet clean
dotnet build
```

### Run with Watch (Auto-reload on changes)

```bash
cd ChurchApp.API/ChurchApp.API
dotnet watch run
```

## 🧪 Testing Endpoints

### Health Check

```bash
# Using curl
curl http://localhost:5000/health

# Expected response:
# {"status":"Healthy","timestamp":"2026-02-28T20:00:00Z","version":"1.0.0"}
```

### View Swagger UI (Development only)

```bash
# Open in browser
open http://localhost:5000/swagger
# or visit: http://localhost:5000/swagger/index.html
```

## 📝 Creating New Endpoints

### Step 1: Create Endpoint File

Create a new file in `ChurchApp.API/Endpoints/`:

```csharp
using FastEndpoints;

namespace ChurchApp.API.Endpoints;

public class GetMembersEndpoint : EndpointWithoutRequest<List<MemberDto>>
{
    public override void Configure()
    {
        Get("/api/members");
        AllowAnonymous();
        Summary(s => {
            s.Summary = "Get all members";
            s.Description = "Returns a list of all church members";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var members = new List<MemberDto>
        {
            new() { Id = 1, Name = "John Doe" }
        };
        
        await SendAsync(members, cancellation: ct);
    }
}

public class MemberDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

### Step 2: Add DTO to JSON Context

Update `AppJsonSerializerContext.cs`:

```csharp
[JsonSerializable(typeof(MemberDto))]
[JsonSerializable(typeof(List<MemberDto>))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
```

### Step 3: Run and Test

```bash
dotnet run
curl http://localhost:5000/api/members
```

## 🗄️ Database Operations

### Add EF Core Migration

```bash
dotnet ef migrations add MigrationName \
  --project ChurchApp.Application/ChurchApp.Application \
  --startup-project ChurchApp.API/ChurchApp.API
```

### Update Database

```bash
dotnet ef database update \
  --project ChurchApp.API/ChurchApp.API
```

### Remove Last Migration

```bash
dotnet ef migrations remove \
  --project ChurchApp.Application/ChurchApp.Application \
  --startup-project ChurchApp.API/ChurchApp.API
```

### List Migrations

```bash
dotnet ef migrations list \
  --project ChurchApp.API/ChurchApp.API
```

## 📦 Package Management

### Add a New Package

```bash
# 1. Add version to Directory.Packages.props
<PackageVersion Include="PackageName" Version="1.0.0" />

# 2. Reference in project file (without version)
<PackageReference Include="PackageName" />

# 3. Restore
dotnet restore
```

### Update All Packages

```bash
# Update versions in Directory.Packages.props
dotnet restore
```

## 🔍 Useful Commands

### Check .NET Version

```bash
dotnet --version
```

### List All Projects

```bash
dotnet sln list
```

### List Installed Packages

```bash
dotnet list package
```

### Check for Package Updates

```bash
dotnet list package --outdated
```

### Run Tests (when you add tests)

```bash
dotnet test
```

## 🐛 Debugging

### Run with Detailed Logging

```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --verbosity detailed
```

### Check Build Errors

```bash
dotnet build --verbosity normal
```

### Clear NuGet Cache (if package issues)

```bash
dotnet nuget locals all --clear
dotnet restore
```

## 📊 Performance

### Publish for Production (AOT)

```bash
# For macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64

# For macOS (Intel)
dotnet publish -c Release -r osx-x64

# For Linux
dotnet publish -c Release -r linux-x64

# For Windows
dotnet publish -c Release -r win-x64
```

### Check Published Output Size

```bash
ls -lh ChurchApp.API/ChurchApp.API/bin/Release/net10.0/osx-arm64/publish/
```

## 🔒 Common Patterns

### Using ErrorOr for Error Handling

```csharp
using ErrorOr;

public class MyEndpoint : Endpoint<MyRequest, MyResponse>
{
    public override async Task HandleAsync(MyRequest req, CancellationToken ct)
    {
        ErrorOr<Member> result = await GetMemberAsync(req.Id);
        
        if (result.IsError)
        {
            await SendErrorsAsync(result.Errors, cancellation: ct);
            return;
        }
        
        await SendAsync(new MyResponse { Member = result.Value }, cancellation: ct);
    }
}
```

### Dependency Injection in Endpoints

```csharp
public class MyEndpoint : Endpoint<MyRequest, MyResponse>
{
    private readonly ChurchAppDbContext _db;
    
    public MyEndpoint(ChurchAppDbContext db)
    {
        _db = db;
    }
    
    public override async Task HandleAsync(MyRequest req, CancellationToken ct)
    {
        var members = await _db.Members.ToListAsync(ct);
        // ...
    }
}
```

## 🎯 Project Structure Best Practices

```
ChurchApp.API/
├── Endpoints/
│   ├── Members/           # Group by feature
│   │   ├── GetMembersEndpoint.cs
│   │   ├── CreateMemberEndpoint.cs
│   │   └── UpdateMemberEndpoint.cs
│   └── Health/
│       └── HealthCheckEndpoint.cs
└── Program.cs

ChurchApp.Application/
├── Domain/                # Domain entities
│   └── Member.cs
├── Features/              # Feature-based organization
│   └── Members/
│       ├── Commands/
│       └── Queries/
└── Infrastructure/        # DbContext, repositories
    └── ChurchAppDbContext.cs
```

## 📚 Resources

- **FastEndpoints Docs:** <https://fast-endpoints.com/>
- **EF Core Docs:** <https://learn.microsoft.com/ef/core/>
- **ErrorOr GitHub:** <https://github.com/amantinband/error-or>
- **.NET AOT Docs:** <https://learn.microsoft.com/dotnet/core/deploying/native-aot/>

---

**Pro Tips:**

- Use `dotnet watch run` during development for hot reload
- Keep DTOs in the same file as endpoints or in a separate DTOs folder
- Always add new DTOs to `AppJsonSerializerContext.cs` for AOT
- Use ErrorOr for functional error handling instead of exceptions
- Group endpoints by feature for better organization
