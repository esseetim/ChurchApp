using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Families;

public sealed class RemoveFamilyMemberEndpoint(ChurchAppDbContext dbContext)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/api/families/{familyId:guid}/members/{memberId:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var familyIdRaw = Route<string>("familyId");
        var memberIdRaw = Route<string>("memberId");

        if (!Guid.TryParse(familyIdRaw, out var familyId) || !Guid.TryParse(memberIdRaw, out var memberId))
        {
            AddError("Invalid route ids.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var existingLink = await dbContext.FamilyMembers
            .SingleOrDefaultAsync(x => x.FamilyId == familyId && x.MemberId == memberId, ct);

        if (existingLink is null)
        {
            AddError("Family member link not found.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        dbContext.FamilyMembers.Remove(existingLink);
        await dbContext.SaveChangesAsync(ct);
        await SendNoContentAsync(ct);
    }
}
