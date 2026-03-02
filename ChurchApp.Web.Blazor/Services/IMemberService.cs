using ChurchApp.Web.Blazor.Models;

namespace ChurchApp.Web.Blazor.Services;

public interface IMemberService
{
    Task<MembersResponse> GetMembersAsync(string? search = null, int page = 1, int pageSize = 200, CancellationToken cancellationToken = default);
    Task<CreateMemberResponse> CreateMemberAsync(CreateMemberRequest request, CancellationToken cancellationToken = default);
}
