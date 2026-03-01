using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Families;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Families;

public sealed class AddFamilyMemberEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<AddFamilyMemberRequest, AddFamilyMemberResponse>
{
    public override void Configure()
    {
        Post("/api/families/{familyId:guid}/members");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AddFamilyMemberRequest req, CancellationToken ct)
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

        var memberExists = await dbContext.Members.AnyAsync(x => x.Id == req.MemberId, ct);
        if (!memberExists)
        {
            AddError("Member not found.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var existingLink = await dbContext.FamilyMembers
            .AnyAsync(x => x.FamilyId == familyId && x.MemberId == req.MemberId, ct);

        if (!existingLink)
        {
            dbContext.FamilyMembers.Add(new FamilyMember
            {
                FamilyId = familyId,
                MemberId = req.MemberId
            });
            await dbContext.SaveChangesAsync(ct);
        }

        await SendAsync(new AddFamilyMemberResponse(familyId, req.MemberId, !existingLink), cancellation: ct);
    }
}
