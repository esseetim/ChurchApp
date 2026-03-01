using ChurchApp.Application.Infrastructure;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace ChurchApp.Tests.Integration;

public sealed class ApiTestFactory : WebApplicationFactory<API.Program>
{
    private readonly string _postgresUser;
    private readonly string _postgresPassword;
    private readonly string _postgresHost;
    private readonly int _postgresPort;
    private readonly string _postgresContainerName;
    private readonly string _databaseName;
    private readonly string _connectionString;
    private readonly string _adminConnectionString;

    public ApiTestFactory()
    {
        _postgresContainerName = $"churchapp-tests-postgres-{Guid.NewGuid():N}";
        _databaseName = $"churchapp_tests_{Guid.NewGuid():N}";
        _postgresHost = Environment.GetEnvironmentVariable("TEST_POSTGRES_HOST") ?? "127.0.0.1";
        _postgresUser = Environment.GetEnvironmentVariable("TEST_POSTGRES_USER") ?? "churchapp";
        _postgresPassword = Environment.GetEnvironmentVariable("TEST_POSTGRES_PASSWORD") ?? "churchapp";

        var image = Environment.GetEnvironmentVariable("TEST_POSTGRES_IMAGE") ?? "postgres:16";
        StartPostgresContainer(image);
        _postgresPort = GetMappedPort();

        _connectionString =
            $"Host={_postgresHost};Port={_postgresPort};Database={_databaseName};Username={_postgresUser};Password={_postgresPassword}";
        _adminConnectionString =
            $"Host={_postgresHost};Port={_postgresPort};Database=postgres;Username={_postgresUser};Password={_postgresPassword}";

        WaitForPostgresReadiness(TimeSpan.FromSeconds(60));
        CreateIsolatedDatabase();
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

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ChurchAppDbContext>));
            services.AddDbContext<ChurchAppDbContext>(options => options.UseNpgsql(_connectionString));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ChurchAppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DropIsolatedDatabase();
            StopPostgresContainer();
        }

        base.Dispose(disposing);
    }

    private void StartPostgresContainer(string image)
    {
        var runResult = RunCommand(
            "docker",
            $"run -d --rm --name {_postgresContainerName} -e POSTGRES_USER={_postgresUser} -e POSTGRES_PASSWORD={_postgresPassword} -e POSTGRES_DB=postgres -p 0:5432 {image}");

        if (runResult.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to start PostgreSQL Docker container. StdErr: {runResult.StandardError}");
        }
    }

    private int GetMappedPort()
    {
        var portResult = RunCommand("docker", $"port {_postgresContainerName} 5432/tcp");
        if (portResult.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to resolve PostgreSQL container port mapping. StdErr: {portResult.StandardError}");
        }

        var portLine = portResult.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(portLine))
        {
            throw new InvalidOperationException("PostgreSQL container did not report a mapped host port.");
        }

        var hostPortText = portLine[(portLine.LastIndexOf(':') + 1)..];
        if (!int.TryParse(hostPortText, out var parsedPort))
        {
            throw new InvalidOperationException($"Unable to parse mapped PostgreSQL port from '{portLine}'.");
        }

        return parsedPort;
    }

    private void WaitForPostgresReadiness(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        Exception? lastError = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var conn = new NpgsqlConnection(
                    $"Host={_postgresHost};Port={_postgresPort};Database=postgres;Username={_postgresUser};Password={_postgresPassword}");
                conn.Open();
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                Thread.Sleep(500);
            }
        }

        throw new TimeoutException($"PostgreSQL container was not ready within {timeout.TotalSeconds} seconds. Last error: {lastError?.Message}");
    }

    private void CreateIsolatedDatabase()
    {
        using var conn = new NpgsqlConnection(_adminConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
        cmd.ExecuteNonQuery();
    }

    private void DropIsolatedDatabase()
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
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Database cleanup failed for '{_databaseName}': {ex.Message}");
        }
    }

    private void StopPostgresContainer()
    {
        var stopResult = RunCommand("docker", $"stop {_postgresContainerName}");
        if (stopResult.ExitCode != 0)
        {
            Console.Error.WriteLine($"Failed to stop PostgreSQL container '{_postgresContainerName}': {stopResult.StandardError}");
        }
    }

    private static CommandResult RunCommand(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {fileName} {arguments}");

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(30_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"Process timed out: {fileName} {arguments}");
        }

        return new CommandResult(process.ExitCode, standardOutput, standardError);
    }

    private sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);
}
