using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Members;

public sealed class UpdateMemberEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<UpdateMemberRequest, EmptyResponse>
{
    public override void Configure()
    {
        Put("/api/members/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateMemberRequest req, CancellationToken ct)
    {
        var idRaw = Route<string>("id");
        if (!Guid.TryParse(idRaw, out var memberId))
        {
            AddError("Invalid member id.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
        {
            AddError("FirstName and LastName are required.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var member = await dbContext.Members.SingleOrDefaultAsync(x => x.Id == memberId, ct);
        if (member is null)
        {
            AddError("Member not found.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
        if (email is not null)
        {
            var exists = await dbContext.Members.AnyAsync(x => x.Id != memberId && x.Email == email, ct);
            if (exists)
            {
                AddError("A member with this email already exists.");
                await SendErrorsAsync(cancellation: ct);
                return;
            }
        }

        member.FirstName = req.FirstName.Trim();
        member.LastName = req.LastName.Trim();
        member.Email = email;
        member.PhoneNumber = string.IsNullOrWhiteSpace(req.PhoneNumber) ? null : req.PhoneNumber.Trim();
        await dbContext.SaveChangesAsync(ct);
        await SendNoContentAsync(ct);
    }
}
