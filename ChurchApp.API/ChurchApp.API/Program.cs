using FastEndpoints;
using FastEndpoints.Swagger;
using ChurchApp.Application;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis; // For MigrateAsync extension method

namespace ChurchApp.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);
        builder.AddServiceDefaults();
        
        // Configure Kestrel for HTTPS (Jez Humble's production-ready principle)
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ConfigureHttpsDefaults(httpsOptions =>
            {
                // Development: Use default development certificate
                // Production: Certificate from configuration/secrets
                httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | 
                                           System.Security.Authentication.SslProtocols.Tls13;
            });
        });
        
        // Configure JSON serialization for AOT (Anders Hejlsberg's performance principle)
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });
        
        // Add Application services (Uncle Bob's Dependency Inversion)
        builder.Services.AddChurchAppServices(builder.Configuration);
        
        // Add FastEndpoints with OpenAPI/Swagger document generation
        builder.Services.AddFastEndpoints();

        // Allow the Blazor WebAssembly frontend to call API endpoints in development.
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DevCors", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

#if DEBUG
        // Add FastEndpoints with OpenAPI/Swagger document generation
        builder.Services.AddOpenApi();
        builder.Services.SwaggerDocument(o =>
        {
            o.DocumentSettings = s =>
            {
                s.Title = "ChurchApp API";
                s.Version = "v1";
                s.Description = "Church donation management API with modern .NET architecture";
            };
        });
#endif
        
        var app = builder.Build();
        
        // Configure middleware pipeline (order matters - Uncle Bob's sequence principle)
        app.UseHttpsRedirection();
        if (app.Environment.IsDevelopment())
        {
            app.UseCors("DevCors");
        }
        app.UseFastEndpoints();
        app.MapDefaultEndpoints();
        
        // Modern API documentation with Scalar (replaces Swagger UI)
        // Scalar provides better UX, performance, and modern design
#if DEBUG
        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerGen();
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("ChurchApp API Documentation")
                    .WithTheme(ScalarTheme.Purple)
                    // ... rest of your Scalar config
                    .WithDownloadButton(true);
            });
        }
#endif
        
        // Auto-apply migrations on startup (Jez Humble's automated deployment)
        // This ensures the database is always up to date in development
        if (app.Environment.IsDevelopment())
        {
            await EnsureDatabaseMigrated(app.Services);
        }
        
        await app.RunAsync();
    }

    /// <summary>
    /// Applies pending EF Core migrations automatically on startup.
    /// Follows Jez Humble's Continuous Delivery principle: "Deploy infrastructure with code."
    /// </summary>
    /// <summary>
    /// Applies pending EF Core migrations automatically on startup.
    /// Uses dynamic migrations in Dev, and AOT-safe raw SQL execution in Production.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    private static async Task EnsureDatabaseMigrated(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Application.Infrastructure.ChurchAppDbContext>();

#if DEBUG
        // DEVELOPMENT: Safe to use dynamic code and reflection
        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("✅ Database migrations applied successfully (Dynamic)");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "⚠️ Database migration failed: {ExMessage}", ex.Message);
        }
#else
        // PRODUCTION / AOT: Must use raw SQL execution to remain AOT-compatible
        try
        {
            // 1. Read the idempotent script embedded in the assembly
            var assembly = typeof(Program).Assembly;
            var resourceName = "ChurchApp.API.migrations.sql"; 

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var sqlScript = await reader.ReadToEndAsync();

                // 2. Execute the raw SQL string
                await dbContext.Database.ExecuteSqlRawAsync(sqlScript);
                logger.LogInformation("✅ Database migrations applied successfully (AOT-Safe Script)");
            }
            else
            {
                logger.LogWarning("⚠️ No embedded migrations.sql script found. Skipping migrations.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "⚠️ AOT Database migration failed: {ExMessage}", ex.Message);
        }
#endif
    }
}
