using ChurchApp.Application.Domain.Common;

namespace ChurchApp.Application.Infrastructure.DomainEvents;

public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
