using Aspire.Hosting.Testing;
using ChurchApp.Application.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Projects;

namespace ChurchApp.Tests.Integration;

public class AspireAppHostFlowTests
{
    [Fact]
    public async Task AspireAppHost_ShouldServeApi_AndExecuteDatabaseBackedEndpoints()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var appHostAssembly = typeof(ChurchApp_API).Assembly;
        var entryPoint = appHostAssembly.GetType("Program")
            ?? throw new InvalidOperationException("Unable to locate AppHost entry point type.");

        var appBuilder = await DistributedApplicationTestingBuilder.CreateAsync(entryPoint, timeout.Token);
        await using var app = await appBuilder.BuildAsync(timeout.Token);
        await app.StartAsync(timeout.Token);
        
        var postgresConnectionString = await app.GetConnectionStringAsync("churchapp", timeout.Token)
            ?? throw new InvalidOperationException("Aspire did not provide a PostgreSQL connection string for resource 'churchapp'.");
        var dbOptions = new DbContextOptionsBuilder<ChurchAppDbContext>()
            .UseNpgsql(postgresConnectionString)
            .Options;

        await using (var db = new ChurchAppDbContext(dbOptions))
        {
            await db.Database.EnsureCreatedAsync(timeout.Token);
        }

        using var client = app.CreateHttpClient("api", "http");

        var healthResponse = await client.GetAsync("/health", timeout.Token);
        await EnsureSuccessAsync(healthResponse, "GET /health");

        var ledgerResponse = await client.GetAsync("/api/donations", timeout.Token);
        await EnsureSuccessAsync(ledgerResponse, "GET /api/donations");

        var timeRangeResponse = await client.GetAsync(
            "/api/reports/time-range?startDate=2026-01-01&endDate=2026-12-31&persistReport=true",
            timeout.Token);
        await EnsureSuccessAsync(timeRangeResponse, "GET /api/reports/time-range");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"{operation} failed with {(int)response.StatusCode}: {body}");
    }
}
