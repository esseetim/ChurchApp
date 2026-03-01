using ChurchApp.Application.Features.Donations;
using ChurchApp.Application.Features.Summaries;
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Infrastructure;
using ChurchApp.Application.Infrastructure.DomainEvents;
using ChurchApp.Application.Infrastructure.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChurchApp.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChurchAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ChurchApp")
            ?? "Host=localhost;Port=5432;Database=churchapp;Username=churchapp;Password=churchapp";

        services.AddDbContext<ChurchAppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<ISummaryUpsertService, SummaryUpsertService>();
        services.AddScoped<IDomainEventHandler<DonationCreatedDomainEvent>, DonationCreatedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<DonationVoidedDomainEvent>, DonationVoidedDomainEventHandler>();

        return services;
    }
}
