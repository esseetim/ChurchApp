using ChurchApp.Application.Features.Donations;
using ChurchApp.Application.Features.Summaries;
using ChurchApp.Application.Features.Obligations;
using ChurchApp.Application.Features.Transactions;
using ChurchApp.Application.Features.Transactions.Classification;
using ChurchApp.Application.Features.Transactions.Repositories;
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Infrastructure;
using ChurchApp.Application.Infrastructure.CompiledModels;
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

        services.AddDbContext<ChurchAppDbContext>(options => 
            options.UseNpgsql(connectionString)
                .UseModel(ChurchAppDbContextModel.Instance));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<ISummaryUpsertService, SummaryUpsertService>();
        services.AddScoped<IDomainEventHandler<DonationCreatedDomainEvent>, DonationCreatedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<DonationVoidedDomainEvent>, DonationVoidedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<DonationCreatedDomainEvent>, ObligationFulfillmentHandler>();
        
        // Transaction services
        services.AddScoped<IDonationAccountRepository, DonationAccountRepository>();
        services.AddScoped<IIntegrationEventHandler<RawTransactionExtractedEvent>, RawTransactionResolverHandler>();
        
        // Donation type classifiers (Strategy Pattern)
        services.AddScoped<IDonationTypeClassifier, TitheClassifier>();
        services.AddScoped<IDonationTypeClassifier, BuildingFundClassifier>();
        services.AddScoped<DonationTypeClassificationService>();

        return services;
    }
}