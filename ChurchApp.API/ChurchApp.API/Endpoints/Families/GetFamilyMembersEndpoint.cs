using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Families;

public sealed class GetFamilyMembersEndpoint(ChurchAppDbContext dbContext)
    : EndpointWithoutRequest<FamilyMembersResponse>
{
    public override void Configure()
    {
        Get("/api/families/{familyId:guid}/members");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var familyIdRaw = Route<string>("familyId");
        if (!Guid.TryParse(familyIdRaw, out var familyId))
        {
            AddError("Invalid family id.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var familyExists = await dbContext.Families.AnyAsync(x => x.Id == familyId, ct);
        if (!familyExists)
        {
            AddError("Family not found.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var members = await dbContext.FamilyMembers
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .Select(x => new FamilyMemberDto(
                x.MemberId,
                x.Member.FirstName,
                x.Member.LastName,
                x.Member.Email,
                x.Member.PhoneNumber))
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync(ct);

        await SendAsync(new FamilyMembersResponse(familyId, members), cancellation: ct);
    }
}
