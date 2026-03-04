using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Transactions;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Transactions;

/// <summary>
/// Endpoint to retrieve unmatched raw transactions for manual resolution
/// </summary>
public sealed class GetUnmatchedTransactionsEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<GetUnmatchedTransactionsRequest, GetUnmatchedTransactionsResponse>
{
    public override void Configure()
    {
        Get("/api/transactions/unmatched");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get unmatched transactions";
            s.Description = "Retrieves paginated list of raw transactions that couldn't be automatically matched to donation accounts.";
            s.Response<GetUnmatchedTransactionsResponse>(200, "List of unmatched transactions");
        });
    }

    public override async Task HandleAsync(GetUnmatchedTransactionsRequest req, CancellationToken ct)
    {
        // Validate pagination parameters
        var page = Math.Max(1, req.Page);
        var pageSize = Math.Clamp(req.PageSize, 1, 100);

        // Query unmatched transactions with pagination
        var query = dbContext.RawTransactions
            .Where(rt => rt.Status == RawTransactionStatus.Unmatched)
            .OrderByDescending(rt => rt.CreatedAtUtc);

        var totalCount = await query.CountAsync(ct);
        
        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(rt => new RawTransactionDto
            {
                Id = rt.Id,
                ProviderTransactionId = rt.ProviderTransactionId,
                Provider = rt.Provider.ToString(),
                SenderName = rt.SenderName,
                SenderHandle = rt.SenderHandle,
                Amount = rt.Amount,
                TransactionDate = rt.TransactionDate,
                Memo = rt.Memo,
                Status = rt.Status.ToString(),
                CreatedAtUtc = rt.CreatedAtUtc
            })
            .ToListAsync(ct);

        await SendAsync(new GetUnmatchedTransactionsResponse
        {
            Transactions = transactions,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        }, cancellation: ct);
    }
}
