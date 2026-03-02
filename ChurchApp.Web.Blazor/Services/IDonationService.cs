using ChurchApp.Web.Blazor.Models;

namespace ChurchApp.Web.Blazor.Services;

public interface IDonationService
{
    Task<CreateDonationResponse> CreateDonationAsync(CreateDonationRequest request, CancellationToken cancellationToken = default);
    Task<DonationLedgerResponse> GetDonationsAsync(int page, int pageSize, string? startDate = null, string? endDate = null, 
        Guid? memberId = null, Guid? familyId = null, DonationType? type = null, DonationMethod? method = null, 
        bool includeVoided = false, CancellationToken cancellationToken = default);
    Task VoidDonationAsync(Guid donationId, VoidDonationRequest request, CancellationToken cancellationToken = default);
}
