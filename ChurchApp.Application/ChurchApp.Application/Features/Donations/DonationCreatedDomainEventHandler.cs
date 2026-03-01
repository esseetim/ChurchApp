using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Features.Summaries;
using ChurchApp.Application.Infrastructure.DomainEvents;

namespace ChurchApp.Application.Features.Donations;

public sealed class DonationCreatedDomainEventHandler(ISummaryUpsertService summaryUpsertService)
    : IDomainEventHandler<DonationCreatedDomainEvent>
{
    public async Task HandleAsync(DonationCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await summaryUpsertService.UpsertForDonationAsync(domainEvent, cancellationToken);
    }
}
