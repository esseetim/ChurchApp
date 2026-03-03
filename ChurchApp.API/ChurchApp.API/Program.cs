using FastEndpoints;
using FastEndpoints.Swagger;
using ChurchApp.Application;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore; // For MigrateAsync extension method

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
        
        // Configure OpenAPI document (required for Scalar)
        builder.Services.SwaggerDocument(o =>
        {
            o.DocumentSettings = s =>
            {
                s.Title = "ChurchApp API";
                s.Version = "v1";
                s.Description = "Church donation management API with modern .NET architecture";
            };
        });
        
        var app = builder.Build();
        
        // Configure middleware pipeline (order matters - Uncle Bob's sequence principle)
        app.UseHttpsRedirection();
        app.UseFastEndpoints();
        app.MapDefaultEndpoints();
        
        // Modern API documentation with Scalar (replaces Swagger UI)
        // Scalar provides better UX, performance, and modern design
        if (app.Environment.IsDevelopment())
        {
            // Generate OpenAPI spec (required by Scalar)
            app.UseSwaggerGen();
            
            // Scalar UI at /scalar endpoint (Anders Hejlsberg's developer experience principle)
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("ChurchApp API Documentation")
                    .WithTheme(ScalarTheme.Purple)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .WithPreferredScheme("http") // Development uses HTTP
                    .WithModels(true) // Show schemas
                    .WithDownloadButton(true); // Allow OpenAPI spec download
            });
        }
        
        // Auto-apply migrations on startup (Jez Humble's automated deployment)
        // This ensures database is always up-to-date in development
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
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Migrations are not used with Native AOT. In production, use migration bundles instead.")]
    private static async Task EnsureDatabaseMigrated(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChurchApp.Application.Infrastructure.ChurchAppDbContext>();
        
        try
        {
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("✅ Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Database migration failed: {ex.Message}");
            // In development, we log and continue (Kent Beck's fail-fast in the right place)
            // In production, this should fail the startup
        }
    }
}
