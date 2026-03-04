using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Transactions;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Transactions;

/// <summary>
/// Endpoint to mark a raw transaction as ignored
/// </summary>
public sealed class IgnoreTransactionEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<IgnoreTransactionRequest, IgnoreTransactionResponse>
{
    public override void Configure()
    {
        Post("/api/transactions/{id}/ignore");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Ignore transaction";
            s.Description = "Marks an unmatched raw transaction as ignored (manual override).";
            s.Response<IgnoreTransactionResponse>(200, "Transaction ignored successfully");
            s.Response(400, "Validation error");
            s.Response(404, "Transaction not found");
        });
    }

    public override async Task HandleAsync(IgnoreTransactionRequest req, CancellationToken ct)
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

        // Verify transaction can be ignored
        if (rawTransaction.Status == RawTransactionStatus.Resolved)
        {
            AddError("Cannot ignore a resolved transaction.");
            await SendErrorsAsync(400, ct);
            return;
        }

        // Mark as ignored
        try
        {
            rawTransaction.MarkIgnored();
            await dbContext.SaveChangesAsync(ct);

            await SendAsync(new IgnoreTransactionResponse
            {
                Message = $"Transaction {rawTransaction.ProviderTransactionId} marked as ignored"
            }, cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await SendErrorsAsync(400, ct);
        }
    }
}
