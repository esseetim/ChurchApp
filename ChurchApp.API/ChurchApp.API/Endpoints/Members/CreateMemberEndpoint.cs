using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Members;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Members;

public sealed class CreateMemberEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<CreateMemberRequest, CreateMemberResponse>
{
    public override void Configure()
    {
        Post("/api/members");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateMemberRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
        {
            AddError("FirstName and LastName are required.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
        if (email is not null)
        {
            var exists = await dbContext.Members.AnyAsync(x => x.Email == email, ct);
            if (exists)
            {
                AddError("A member with this email already exists.");
                await SendErrorsAsync(cancellation: ct);
                return;
            }
        }

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            Email = email,
            PhoneNumber = string.IsNullOrWhiteSpace(req.PhoneNumber) ? null : req.PhoneNumber.Trim()
        };

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync(ct);

        await SendAsync(new CreateMemberResponse(member.Id), 201, ct);
    }
}
