using Microsoft.Extensions.DependencyInjection;

namespace ChurchApp.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChurchAppServices(this IServiceCollection services)
    {
        return services;
    }
}