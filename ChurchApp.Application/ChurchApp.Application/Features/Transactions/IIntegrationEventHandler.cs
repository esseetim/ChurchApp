using ErrorOr;

namespace ChurchApp.Application.Features.Transactions;

/// <summary>
/// Handler for integration events.
/// Uses ErrorOr pattern for railway-oriented programming.
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task<ErrorOr<Success>> HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
