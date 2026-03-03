using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Donations;
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

        var donationAccounts = new List<DonationAccount>();
        if (req.DonationAccounts is not null)
        {
            foreach (var accountRequest in req.DonationAccounts)
            {
                if (string.IsNullOrWhiteSpace(accountRequest.Handle))
                {
                    AddError("Donation account handle is required.");
                    await SendErrorsAsync(cancellation: ct);
                    return;
                }

                if (accountRequest.Method == DonationMethod.Cash)
                {
                    AddError("Cash is not a valid donation account method.");
                    await SendErrorsAsync(cancellation: ct);
                    return;
                }

                var normalizedHandle = accountRequest.Handle.Trim();
                var methodHandleExists = await dbContext.DonationAccounts.AnyAsync(
                    x => x.Method == accountRequest.Method && x.Handle == normalizedHandle,
                    ct);

                if (methodHandleExists)
                {
                    AddError($"Donation account already exists for method '{accountRequest.Method}' and handle '{normalizedHandle}'.");
                    await SendErrorsAsync(cancellation: ct);
                    return;
                }

                donationAccounts.Add(new DonationAccount
                {
                    Id = Guid.NewGuid(),
                    MemberId = member.Id,
                    Method = accountRequest.Method,
                    Handle = normalizedHandle,
                    DisplayName = string.IsNullOrWhiteSpace(accountRequest.DisplayName) ? null : accountRequest.DisplayName.Trim(),
                    IsActive = true
                });
            }
        }

        dbContext.Members.Add(member);
        if (donationAccounts.Count > 0)
        {
            dbContext.DonationAccounts.AddRange(donationAccounts);
        }
        await dbContext.SaveChangesAsync(ct);

        await SendAsync(new CreateMemberResponse(member.Id), 201, ct);
    }
}
