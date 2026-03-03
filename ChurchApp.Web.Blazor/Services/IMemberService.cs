using ChurchApp.Web.Blazor.Models;

namespace ChurchApp.Web.Blazor.Services;

public interface IMemberService
{
    Task<MembersResponse> GetMembersAsync(string? search = null, int page = 1, int pageSize = 200, CancellationToken cancellationToken = default);
    Task<CreateMemberResponse> CreateMemberAsync(CreateMemberRequest request, CancellationToken cancellationToken = default);
    Task UpdateMemberAsync(Guid memberId, UpdateMemberRequest request, CancellationToken cancellationToken = default);
    Task<MemberDonationAccountsResponse> GetDonationAccountsAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<DonationAccount> CreateDonationAccountAsync(Guid memberId, CreateDonationAccountRequest request, CancellationToken cancellationToken = default);
    Task<DonationAccount> UpdateDonationAccountAsync(Guid memberId, Guid accountId, UpdateDonationAccountRequest request, CancellationToken cancellationToken = default);
    Task DeleteDonationAccountAsync(Guid memberId, Guid accountId, CancellationToken cancellationToken = default);
    Task<MemberFamiliesResponse> GetMemberFamiliesAsync(Guid memberId, CancellationToken cancellationToken = default);
}
