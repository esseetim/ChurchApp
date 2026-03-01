using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Features.Summaries;
using ChurchApp.Application.Infrastructure.DomainEvents;

namespace ChurchApp.Application.Features.Donations;

public sealed class DonationVoidedDomainEventHandler(ISummaryUpsertService summaryUpsertService)
    : IDomainEventHandler<DonationVoidedDomainEvent>
{
    public async Task HandleAsync(DonationVoidedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await summaryUpsertService.AdjustForVoidedDonationAsync(domainEvent, cancellationToken);
    }
}
