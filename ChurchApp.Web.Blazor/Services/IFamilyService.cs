using ChurchApp.Web.Blazor.Models;

namespace ChurchApp.Web.Blazor.Services;

public interface IFamilyService
{
    Task<FamiliesResponse> GetFamiliesAsync(string? search = null, int page = 1, int pageSize = 200, CancellationToken cancellationToken = default);
    Task<CreateFamilyResponse> CreateFamilyAsync(CreateFamilyRequest request, CancellationToken cancellationToken = default);
    Task UpdateFamilyAsync(Guid familyId, UpdateFamilyRequest request, CancellationToken cancellationToken = default);
    Task AddFamilyMemberAsync(Guid familyId, AddFamilyMemberRequest request, CancellationToken cancellationToken = default);
    Task RemoveFamilyMemberAsync(Guid familyId, Guid memberId, CancellationToken cancellationToken = default);
    Task<FamilyMembersResponse> GetFamilyMembersAsync(Guid familyId, CancellationToken cancellationToken = default);
}
