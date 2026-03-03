using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Members;

public sealed class GetMemberDonationAccountsEndpoint(ChurchAppDbContext dbContext)
    : EndpointWithoutRequest<MemberDonationAccountsResponse>
{
    public override void Configure()
    {
        Get("/api/members/{memberId:guid}/accounts");
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

        var accounts = await dbContext.DonationAccounts
            .AsNoTracking()
            .Where(x => x.MemberId == memberId && x.IsActive)
            .OrderBy(x => x.Method)
            .ThenBy(x => x.Handle)
            .Select(x => new DonationAccountDto(
                x.Id,
                x.MemberId,
                x.Method,
                x.Handle,
                x.DisplayName,
                x.IsActive))
            .ToListAsync(ct);

        await SendAsync(new MemberDonationAccountsResponse(memberId, accounts), cancellation: ct);
    }
}
