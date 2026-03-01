using ChurchApp.Application.Domain.Common;
using ChurchApp.Application.Domain.Donations;
using Microsoft.Extensions.DependencyInjection;

namespace ChurchApp.Application.Infrastructure.DomainEvents;

public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent is DonationCreatedDomainEvent donationCreatedDomainEvent)
        {
            var handlers = serviceProvider.GetServices<IDomainEventHandler<DonationCreatedDomainEvent>>();
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(donationCreatedDomainEvent, cancellationToken);
            }
        }
    }
}
