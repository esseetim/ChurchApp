using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Members;

public sealed class DeleteMemberDonationAccountEndpoint(ChurchAppDbContext dbContext)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/api/members/{memberId:guid}/accounts/{accountId:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var memberIdRaw = Route<string>("memberId");
        var accountIdRaw = Route<string>("accountId");
        if (!Guid.TryParse(memberIdRaw, out var memberId) || !Guid.TryParse(accountIdRaw, out var accountId))
        {
            AddError("Invalid route ids.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var account = await dbContext.DonationAccounts
            .SingleOrDefaultAsync(x => x.Id == accountId && x.MemberId == memberId, ct);

        if (account is null)
        {
            AddError("Donation account not found.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        account.IsActive = false;
        await dbContext.SaveChangesAsync(ct);
        await SendNoContentAsync(ct);
    }
}
