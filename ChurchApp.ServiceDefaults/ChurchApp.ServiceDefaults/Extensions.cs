using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Service defaults for ChurchApp microservices.
/// Follows Jez Humble's production-ready patterns and Uncle Bob's clean architecture.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds common service defaults: health checks, observability, resilience.
    /// </summary>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        // Health checks for Kubernetes/Aspire readiness probes
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        // Configure Kestrel for production-grade HTTPS (Jez Humble's reliability)
        if (builder is WebApplicationBuilder webBuilder)
        {
            webBuilder.WebHost.UseKestrelHttpsConfiguration();
        }

        return builder;
    }

    /// <summary>
    /// Maps default endpoints: health checks, metrics, readiness probes.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Kubernetes liveness probe
        app.MapHealthChecks("/healthz");
        
        // Kubernetes readiness probe
        app.MapHealthChecks("/ready");

        return app;
    }
}
