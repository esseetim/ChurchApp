
namespace ChurchApp.Application.Features.Summaries;

public interface ISummaryUpsertService
{
    Task UpsertForDonationAsync(DonationCreatedDomainEvent donationEvent, CancellationToken cancellationToken);
    Task AdjustForVoidedDonationAsync(DonationVoidedDomainEvent donationEvent, CancellationToken cancellationToken);
}
