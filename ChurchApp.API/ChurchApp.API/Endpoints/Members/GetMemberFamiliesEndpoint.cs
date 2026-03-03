using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Members;

public sealed class GetMemberFamiliesEndpoint(ChurchAppDbContext dbContext)
    : EndpointWithoutRequest<MemberFamiliesResponse>
{
    public override void Configure()
    {
        Get("/api/members/{memberId:guid}/families");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var memberIdRaw = Route<string>("memberId");
        if (!Guid.TryParse(memberIdRaw, out var memberId))
        {
            AddError("Invalid member id.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var memberExists = await dbContext.Members.AnyAsync(x => x.Id == memberId, ct);
        if (!memberExists)
        {
            AddError("Member not found.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var families = await dbContext.FamilyMembers
            .AsNoTracking()
            .Where(x => x.MemberId == memberId)
            .OrderBy(x => x.Family.Name)
            .Select(x => new MemberFamilyDto(x.FamilyId, x.Family.Name))
            .ToListAsync(ct);

        await SendAsync(new MemberFamiliesResponse(memberId, families), cancellation: ct);
    }
}
