using ChurchApp.Application.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ChurchApp.Tests.Integration;

public sealed class ApiTestFactory : WebApplicationFactory<ChurchApp.API.Program>
{
    private readonly string _connectionString;
    private readonly string _adminConnectionString;
    private readonly string _databaseName;

    public bool IsDatabaseAvailable { get; }
    public string? UnavailableReason { get; }

    public ApiTestFactory()
    {
        _databaseName = $"churchapp_tests_{Guid.NewGuid():N}";
        var host = Environment.GetEnvironmentVariable("TEST_POSTGRES_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("TEST_POSTGRES_PORT") ?? "5432";
        var user = Environment.GetEnvironmentVariable("TEST_POSTGRES_USER") ?? "churchapp";
        var password = Environment.GetEnvironmentVariable("TEST_POSTGRES_PASSWORD") ?? "churchapp";

        _connectionString = $"Host={host};Port={port};Database={_databaseName};Username={user};Password={password}";
        _adminConnectionString = $"Host={host};Port={port};Database=postgres;Username={user};Password={password}";

        try
        {
            using var conn = new NpgsqlConnection(_adminConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
            cmd.ExecuteNonQuery();
            IsDatabaseAvailable = true;
        }
        catch (Exception ex)
        {
            IsDatabaseAvailable = false;
            UnavailableReason = ex.Message;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ChurchApp"] = _connectionString
            });
        });

        if (!IsDatabaseAvailable)
        {
            return;
        }

        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ChurchAppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && IsDatabaseAvailable)
        {
            try
            {
                using var conn = new NpgsqlConnection(_adminConnectionString);
                conn.Open();
                using var terminate = conn.CreateCommand();
                terminate.CommandText =
                    $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid();";
                terminate.ExecuteNonQuery();
                using var drop = conn.CreateCommand();
                drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
                drop.ExecuteNonQuery();
            }
            catch
            {
            }
        }

        base.Dispose(disposing);
    }
}
