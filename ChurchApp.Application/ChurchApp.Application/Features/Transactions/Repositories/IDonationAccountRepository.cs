
namespace ChurchApp.Application.Features.Transactions.Repositories;

/// <summary>
/// Repository abstraction for DonationAccount queries.
/// Follows Repository Pattern and Dependency Inversion Principle.
/// </summary>
public interface IDonationAccountRepository
{
    /// <summary>
    /// Finds a donation account by matching handle or display name.
    /// Returns null if no match found.
    /// </summary>
    Task<DonationAccount?> FindByHandleOrNameAsync(
        DonationMethod method,
        string? handle,
        string? displayName,
        CancellationToken cancellationToken = default);
}
