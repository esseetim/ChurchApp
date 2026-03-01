using ChurchApp.Application.Domain.Common;

namespace ChurchApp.Application.Infrastructure.DomainEvents;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
