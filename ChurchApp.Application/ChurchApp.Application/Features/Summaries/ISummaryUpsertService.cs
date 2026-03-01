using ChurchApp.Application.Domain.Donations;

namespace ChurchApp.Application.Features.Summaries;

public interface ISummaryUpsertService
{
    Task UpsertForDonationAsync(DonationCreatedDomainEvent donationEvent, CancellationToken cancellationToken);
}
