using ChurchApp.Application.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Application.Features.Transactions.Repositories;

/// <summary>
/// EF Core implementation of DonationAccount repository.
/// Encapsulates data access logic and query optimization.
/// </summary>
public sealed class DonationAccountRepository : IDonationAccountRepository
{
    private readonly ChurchAppDbContext _dbContext;

    public DonationAccountRepository(ChurchAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DonationAccount?> FindByHandleOrNameAsync(
        DonationMethod method,
        string? handle,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        // Build query with appropriate filters
        var query = _dbContext.DonationAccounts
            .Include(x => x.Member)
            .Where(x => x.Method == method && x.IsActive);

        // Try exact handle match first (most common case)
        if (!string.IsNullOrWhiteSpace(handle))
        {
            var byHandle = await query
                .FirstOrDefaultAsync(x => x.Handle == handle, cancellationToken);
            
            if (byHandle is not null)
                return byHandle;
        }

        // Fallback to display name match (case-insensitive)
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return await query
                .FirstOrDefaultAsync(
                    x => EF.Functions.ILike(x.DisplayName ?? "", displayName), 
                    cancellationToken);
        }

        return null;
    }
}
