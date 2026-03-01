using ChurchApp.Application.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChurchApp.Tests.Integration;

public sealed class ApiTestFactory : WebApplicationFactory<ChurchApp.API.Program>
{
    private readonly string _dbFilePath;

    public ApiTestFactory()
    {
        _dbFilePath = Path.Combine(Path.GetTempPath(), $"churchapp-tests-{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ChurchApp"] = $"Data Source={_dbFilePath}"
            });
        });

        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ChurchAppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        });
    }
}
