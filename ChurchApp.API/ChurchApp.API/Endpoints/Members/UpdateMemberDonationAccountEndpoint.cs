using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Members;

public sealed class UpdateMemberDonationAccountEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<UpdateDonationAccountRequest, DonationAccountDto>
{
    public override void Configure()
    {
        Put("/api/members/{memberId:guid}/accounts/{accountId:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateDonationAccountRequest req, CancellationToken ct)
    {
        var memberIdRaw = Route<string>("memberId");
        var accountIdRaw = Route<string>("accountId");
        if (!Guid.TryParse(memberIdRaw, out var memberId) || !Guid.TryParse(accountIdRaw, out var accountId))
        {
            AddError("Invalid route ids.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Handle))
        {
            AddError("Handle is required.");
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

        // Validate and create PaymentHandle
        var handleResult = PaymentHandle.Create(req.Handle, account.Method);
        if (handleResult.IsError)
        {
            AddError($"Invalid payment handle: {handleResult.FirstError.Description}");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var handle = handleResult.Value;
        
        // Check if handle already exists for this method (excluding current account)
        var exists = await dbContext.DonationAccounts.AnyAsync(
            x => x.Id != accountId && x.Method == account.Method && x.Handle == handle,
            ct);

        if (exists)
        {
            AddError($"Donation account already exists for method '{account.Method}' and handle '{(string)handle}'.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        account.Handle = handle;
        account.DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? null : req.DisplayName.Trim();
        account.IsActive = req.IsActive;
        await dbContext.SaveChangesAsync(ct);

        await SendAsync(new DonationAccountDto(
            account.Id,
            account.MemberId,
            account.Method,
            account.Handle,
            account.DisplayName,
            account.IsActive), cancellation: ct);
    }
}
