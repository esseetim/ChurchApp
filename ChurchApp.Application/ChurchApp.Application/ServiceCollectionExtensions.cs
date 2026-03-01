using ChurchApp.Application.Infrastructure;
using ChurchApp.Application.Infrastructure.CompiledModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChurchApp.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChurchAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ChurchApp")
            ?? "Data Source=churchapp.db";

        services.AddDbContext<ChurchAppDbContext>(options =>
            options.UseSqlite(connectionString)
                .UseModel(ChurchAppDbContextModel.Instance));

        return services;
    }
}
