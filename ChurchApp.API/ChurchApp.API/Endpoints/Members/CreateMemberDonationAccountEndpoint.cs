using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Members;

public sealed class CreateMemberDonationAccountEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<CreateDonationAccountRequest, DonationAccountDto>
{
    public override void Configure()
    {
        Post("/api/members/{memberId:guid}/accounts");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateDonationAccountRequest req, CancellationToken ct)
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

        if (string.IsNullOrWhiteSpace(req.Handle))
        {
            AddError("Handle is required.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        if (req.Method == DonationMethod.Cash)
        {
            AddError("Cash is not a valid donation account method.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var normalizedHandle = req.Handle.Trim();
        var exists = await dbContext.DonationAccounts.AnyAsync(
            x => x.Method == req.Method && x.Handle == normalizedHandle,
            ct);

        if (exists)
        {
            AddError($"Donation account already exists for method '{req.Method}' and handle '{normalizedHandle}'.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var account = new DonationAccount
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            Method = req.Method,
            Handle = normalizedHandle,
            DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? null : req.DisplayName.Trim(),
            IsActive = true
        };

        dbContext.DonationAccounts.Add(account);
        await dbContext.SaveChangesAsync(ct);

        await SendAsync(new DonationAccountDto(
            account.Id,
            account.MemberId,
            account.Method,
            account.Handle,
            account.DisplayName,
            account.IsActive), 201, ct);
    }
}
