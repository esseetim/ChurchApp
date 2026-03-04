using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Transactions;
using ChurchApp.Application.Infrastructure;
using ChurchApp.Application.Infrastructure.Transactions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Transactions;

/// <summary>
/// Endpoint to manually resolve an unmatched raw transaction
/// </summary>
public sealed class ResolveTransactionEndpoint(
    ChurchAppDbContext dbContext,
    IUnitOfWork unitOfWork)
    : Endpoint<ResolveTransactionRequest, ResolveTransactionResponse>
{
    public override void Configure()
    {
        Post("/api/transactions/{id}/resolve");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Resolve unmatched transaction";
            s.Description = "Manually resolves an unmatched raw transaction by creating a donation and optionally a donation account.";
            s.Response<ResolveTransactionResponse>(200, "Transaction resolved successfully");
            s.Response(400, "Validation error");
            s.Response(404, "Transaction not found");
        });
    }

    public override async Task HandleAsync(ResolveTransactionRequest req, CancellationToken ct)
    {
        // Parse raw transaction ID from route
        if (!Guid.TryParse(Route<string>("id"), out var rawTransactionId))
        {
            AddError("Invalid transaction ID format.");
            await SendErrorsAsync(400, ct);
            return;
        }

        // Load the raw transaction
        var rawTransaction = await dbContext.RawTransactions
            .FirstOrDefaultAsync(rt => rt.Id == rawTransactionId, ct);

        if (rawTransaction == null)
        {
            AddError("Raw transaction not found.");
            await SendErrorsAsync(404, ct);
            return;
        }

        // Verify transaction is in the correct status
        if (rawTransaction.Status != RawTransactionStatus.Unmatched &&
            rawTransaction.Status != RawTransactionStatus.Pending)
        {
            AddError($"Transaction is already {rawTransaction.Status}. Cannot resolve.");
            await SendErrorsAsync(400, ct);
            return;
        }

        // Verify member exists
        var memberExists = await dbContext.Members.AnyAsync(m => m.Id == req.MemberId, ct);
        if (!memberExists)
        {
            AddError("Member not found.");
            await SendErrorsAsync(400, ct);
            return;
        }

        Guid? donationAccountId = req.DonationAccountId;

        // Handle donation account creation if requested
        if (req.SaveAsNewAccount && !string.IsNullOrWhiteSpace(rawTransaction.SenderHandle))
        {
            // Determine donation method from provider
            var method = rawTransaction.Provider switch
            {
                TransactionProvider.CashApp => DonationMethod.CashApp,
                TransactionProvider.Zelle => DonationMethod.Zelle,
                _ => DonationMethod.Other
            };

            // Create PaymentHandle
            var handleResult = PaymentHandle.Create(rawTransaction.SenderHandle, method);
            if (handleResult.IsError)
            {
                AddError($"Invalid payment handle: {handleResult.Errors[0].Description}");
                await SendErrorsAsync(400, ct);
                return;
            }

            // Create donation account
            var account = new DonationAccount
            {
                Id = Guid.CreateVersion7(),
                MemberId = req.MemberId,
                Method = method,
                Handle = handleResult.Value,
                DisplayName = rawTransaction.SenderName,
                IsActive = true
            };

            dbContext.DonationAccounts.Add(account);
            await dbContext.SaveChangesAsync(ct);
            
            donationAccountId = account.Id;
        }

        // Verify donation account if specified
        if (donationAccountId.HasValue)
        {
            var accountExists = await dbContext.DonationAccounts.AnyAsync(
                da => da.Id == donationAccountId.Value && da.MemberId == req.MemberId,
                ct);

            if (!accountExists)
            {
                AddError("Donation account not found for member.");
                await SendErrorsAsync(400, ct);
                return;
            }
        }

        // Verify obligation if specified
        if (req.ObligationId.HasValue)
        {
            var obligationExists = await dbContext.FinancialObligations.AnyAsync(
                o => o.Id == req.ObligationId.Value && o.MemberId == req.MemberId,
                ct);

            if (!obligationExists)
            {
                AddError("Financial obligation not found for member.");
                await SendErrorsAsync(400, ct);
                return;
            }
        }

        // Create the donation
        var donation = Donation.Create(
            memberId: req.MemberId,
            donationAccountId: donationAccountId,
            type: req.DonationType,
            method: rawTransaction.Provider == TransactionProvider.CashApp ? DonationMethod.CashApp : DonationMethod.Zelle,
            donationDate: rawTransaction.TransactionDate,
            amount: rawTransaction.Amount,
            idempotencyKey: rawTransaction.ProviderTransactionId,
            enteredBy: "System (Gmail)",
            serviceName: null,
            notes: req.Notes ?? rawTransaction.Memo,
            obligationId: req.ObligationId);

        dbContext.Donations.Add(donation);

        // Mark raw transaction as resolved
        rawTransaction.MarkResolved(donation.Id);

        // Commit all changes (donation, account, transaction status) in one transaction
        await unitOfWork.SaveChangesAsync(ct);

        await SendAsync(new ResolveTransactionResponse
        {
            DonationId = donation.Id,
            DonationAccountId = donationAccountId,
            Message = "Transaction resolved successfully"
        }, cancellation: ct);
    }
}
